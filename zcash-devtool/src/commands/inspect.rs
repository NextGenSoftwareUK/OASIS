use std::io;
use std::io::Cursor;
use std::process;

use bech32::primitives::decode::CheckedHrpstring;
use bech32::Bech32;
use clap::Args;
use lazy_static::lazy_static;
use secrecy::Zeroize;

use zcash_address::{
    unified::{self, Encoding},
    ZcashAddress,
};
use zcash_primitives::{block::BlockHeader, transaction::Transaction};
use zcash_proofs::{default_params_folder, load_parameters, ZcashParameters};
use zcash_protocol::{
    consensus::{BranchId, NetworkType},
    constants,
};

mod context;
use context::{Context, ZUint256};

pub(crate) mod address;
pub(crate) mod block;
pub(crate) mod keys;
pub(crate) mod lookup;
pub(crate) mod script;
pub(crate) mod transaction;

lazy_static! {
    static ref GROTH16_PARAMS: ZcashParameters = {
        let folder = default_params_folder().unwrap();
        load_parameters(
            &folder.join("sapling-spend.params"),
            &folder.join("sapling-output.params"),
            Some(&folder.join("sprout-groth16.params")),
        )
    };
    static ref ORCHARD_VK: orchard::circuit::VerifyingKey = orchard::circuit::VerifyingKey::build();
}

#[derive(Debug, Args)]
pub(crate) struct Command {
    /// Query information from the chain to help determine what the data is
    #[arg(short, long)]
    lookup: bool,

    /// String or hex-encoded bytes to inspect
    data: String,

    /// JSON object with keys corresponding to requested context information
    context: Option<Context>,
}

impl Command {
    pub(crate) async fn run(self) -> Result<(), anyhow::Error> {
        let mut opts = self;

        if let Ok(mnemonic) = bip0039::Mnemonic::from_phrase(&opts.data) {
            opts.data.zeroize();
            keys::inspect_mnemonic(mnemonic, opts.context);
        } else if let Ok(bytes) = hex::decode(&opts.data) {
            inspect_bytes(bytes, opts.context, opts.lookup).await;
        } else if let Ok(addr) = ZcashAddress::try_from_encoded(&opts.data) {
            address::inspect(addr);
        } else if let Ok((network, uivk)) = unified::Uivk::decode(&opts.data) {
            keys::view::inspect_uivk(uivk, network);
        } else if let Ok((network, ufvk)) = unified::Ufvk::decode(&opts.data) {
            keys::view::inspect_ufvk(ufvk, network);
        } else if let Ok(parsed) = CheckedHrpstring::new::<Bech32>(&opts.data) {
            let data = parsed.byte_iter().collect::<Vec<_>>();
            match parsed.hrp().as_str() {
                constants::mainnet::HRP_SAPLING_EXTENDED_FULL_VIEWING_KEY => {
                    keys::view::inspect_sapling_extfvk(data, NetworkType::Main);
                }
                constants::testnet::HRP_SAPLING_EXTENDED_FULL_VIEWING_KEY => {
                    keys::view::inspect_sapling_extfvk(data, NetworkType::Test);
                }
                constants::regtest::HRP_SAPLING_EXTENDED_FULL_VIEWING_KEY => {
                    keys::view::inspect_sapling_extfvk(data, NetworkType::Regtest);
                }
                constants::mainnet::HRP_SAPLING_EXTENDED_SPENDING_KEY => {
                    keys::inspect_sapling_extsk(data, NetworkType::Main);
                }
                constants::testnet::HRP_SAPLING_EXTENDED_SPENDING_KEY => {
                    keys::inspect_sapling_extsk(data, NetworkType::Test);
                }
                constants::regtest::HRP_SAPLING_EXTENDED_SPENDING_KEY => {
                    keys::inspect_sapling_extsk(data, NetworkType::Regtest);
                }
                _ => {
                    // Unknown data format.
                    eprintln!("String does not match known Zcash data formats.");
                    process::exit(2);
                }
            }
        } else if let Ok(decoded) = bs58::decode(&opts.data).with_check(None).into_vec() {
            match decoded.as_slice() {
                // `constants::mainnet::B58_TRANSPARENT_SECRET_KEY_PREFIX`
                [0x80, data @ ..] => keys::inspect_transparent_sk(data, NetworkType::Main),
                // `constants::testnet::B58_TRANSPARENT_SECRET_KEY_PREFIX`
                // (also regtest but we can't distinguish the two)
                [0xEF, data @ ..] => keys::inspect_transparent_sk(data, NetworkType::Test),
                _ => {
                    // Unknown data format.
                    eprintln!("String does not match known Zcash data formats.");
                    process::exit(2);
                }
            }
        } else {
            // Unknown data format.
            eprintln!("String does not match known Zcash data formats.");
            process::exit(2);
        }

        Ok(())
    }
}

/// Ensures that the given reader completely consumes the given bytes.
fn complete<F, T>(bytes: &[u8], f: F) -> Option<T>
where
    F: FnOnce(&mut Cursor<&[u8]>) -> io::Result<T>,
{
    let mut cursor = Cursor::new(bytes);
    let res = f(&mut cursor);
    res.ok().and_then(|t| {
        if cursor.position() >= bytes.len() as u64 {
            Some(t)
        } else {
            None
        }
    })
}

async fn inspect_bytes(bytes: Vec<u8>, context: Option<Context>, lookup: bool) {
    if let Some(block) = complete(&bytes, |r| block::Block::read(r)) {
        block::inspect(&block, context);
    } else if let Some(header) = complete(&bytes, |r| BlockHeader::read(r)) {
        block::inspect_header(&header, context);
    } else if let Some(tx) = complete(&bytes, |r| Transaction::read(r, BranchId::Nu5)) {
        // TODO: Take the branch ID used above from the context if present.
        // https://github.com/zcash/zcash/issues/6831
        transaction::inspect(tx, context, None);
    } else if let Ok(script) =
        zcash_script::script::FromChain::parse(&zcash_script::script::Code(bytes.clone()))
    {
        script::inspect(script);
    } else {
        // It's not a known variable-length format. check fixed-length data formats.
        match bytes.len() {
            32 => inspect_possible_hash(bytes.try_into().unwrap(), context, lookup).await,
            64 => {
                // Could be a signature
                eprintln!("This is most likely a signature.");
            }
            _ => {
                eprintln!("Binary data does not match known Zcash data formats.");
                process::exit(2);
            }
        }
    }
}

async fn inspect_possible_hash(bytes: [u8; 32], context: Option<Context>, lookup: bool) {
    let mut maybe_mainnet_block_hash = bytes.iter().take(4).all(|c| c == &0);

    if lookup {
        // Block hashes and txids are byte-reversed; we didn't do this when parsing the
        // original hex because other hex byte encodings are not byte-reversed.
        let mut candidate = bytes;
        candidate.reverse();

        let found = async {
            match lookup::Lightwalletd::mainnet().await {
                Err(e) => eprintln!("Error: Failed to connect to mainnet lightwalletd: {e:?}"),
                Ok(mut mainnet) => {
                    if let Some(block) = mainnet.lookup_block_hash(candidate).await {
                        block::inspect_block_hash(&block, "main");
                        return true;
                    } else {
                        maybe_mainnet_block_hash = false;
                    }

                    if let Some((tx, mined_height)) = mainnet.lookup_txid(candidate).await {
                        transaction::inspect(tx, context, mined_height.map(|h| ("mainnet", h)));
                        return true;
                    }
                }
            };

            match lookup::Lightwalletd::testnet().await {
                Err(e) => eprintln!("Error: Failed to connect to testnet lightwalletd: {e:?}"),
                Ok(mut testnet) => {
                    if let Some(block) = testnet.lookup_block_hash(candidate).await {
                        block::inspect_block_hash(&block, "test");
                        return true;
                    }

                    if let Some((tx, mined_height)) = testnet.lookup_txid(candidate).await {
                        transaction::inspect(tx, context, mined_height.map(|h| ("testnet", h)));
                        return true;
                    }
                }
            };

            false
        }
        .await;

        if found {
            return;
        }
    }

    eprintln!("This is most likely a hash of some sort, or maybe a commitment or nullifier.");
    if maybe_mainnet_block_hash {
        eprintln!("- It could be a mainnet block hash.");
    }
}
