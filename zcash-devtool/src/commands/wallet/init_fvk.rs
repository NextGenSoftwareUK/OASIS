use anyhow::anyhow;
use clap::Args;

use zcash_address::unified::{Encoding, Ufvk};
use zcash_client_backend::{
    data_api::{AccountPurpose, WalletWrite, Zip32Derivation},
    proto::service,
};
use zcash_keys::{encoding::decode_extfvk_with_network, keys::UnifiedFullViewingKey};
use zcash_protocol::consensus::{self, NetworkType};
use zip32::fingerprint::SeedFingerprint;

use crate::{
    config::WalletConfig,
    data::init_dbs,
    parse_hex,
    remote::{tor_client, Servers},
};

// Options accepted for the `init-fvk` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// A name for the account
    #[arg(long)]
    name: String,

    /// Serialized full viewing key (Unified or Sapling) to initialize the wallet with
    #[arg(long)]
    fvk: String,

    /// The wallet's birthday (default is current chain height)
    #[arg(long)]
    birthday: Option<u32>,

    /// Hex encoding of the ZIP 32 fingerprint for the seed from which the UFVK was derived
    #[arg(long)]
    #[arg(value_parser = parse_hex)]
    seed_fingerprint: Option<std::vec::Vec<u8>>,

    /// ZIP 32 account index corresponding to the UFVK
    #[arg(long)]
    hd_account_index: Option<u32>,

    /// The server to initialize with (default is \"ecc\")
    #[arg(short, long)]
    #[arg(default_value = "ecc", value_parser = Servers::parse)]
    server: Servers,

    /// Disable connections via TOR
    #[arg(long)]
    disable_tor: bool,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let opts = self;

        let (network_type, ufvk) = Ufvk::decode(&opts.fvk)
            .map_err(anyhow::Error::new)
            .and_then(
                |(network, ufvk)| -> Result<(NetworkType, UnifiedFullViewingKey), anyhow::Error> {
                    let ufvk = UnifiedFullViewingKey::parse(&ufvk)?;
                    Ok((network, ufvk))
                },
            )
            .or_else(
                |_| -> Result<(NetworkType, UnifiedFullViewingKey), anyhow::Error> {
                    let (network, sfvk) = decode_extfvk_with_network(&opts.fvk)?;
                    let ufvk = UnifiedFullViewingKey::from_sapling_extended_full_viewing_key(sfvk)?;
                    Ok((network, ufvk))
                },
            )?;

        let network = match network_type {
            NetworkType::Main => consensus::Network::MainNetwork,
            NetworkType::Test => consensus::Network::TestNetwork,
            NetworkType::Regtest => {
                return Err(anyhow!("the regtest network is not supported"));
            }
        };

        let server = opts.server.pick(network)?;
        let mut client = if opts.disable_tor {
            server.connect_direct().await?
        } else {
            server.connect(|| tor_client(wallet_dir.as_ref())).await?
        };

        // Get the current chain height (for the wallet's birthday recover-until height).
        let chain_tip: u32 = client
            .get_latest_block(service::ChainSpec::default())
            .await?
            .into_inner()
            .height
            .try_into()
            .expect("block heights must fit into u32");

        let birthday = super::init::Command::get_wallet_birthday(
            client,
            opts.birthday
                .unwrap_or(chain_tip.saturating_sub(100))
                .into(),
            Some(chain_tip.into()),
        )
        .await?;

        let purpose = match (opts.seed_fingerprint, opts.hd_account_index) {
            (Some(seed_fingerprint), Some(hd_account_index)) => Ok(AccountPurpose::Spending {
                derivation: Some(Zip32Derivation::new(
                    SeedFingerprint::from_bytes(
                        seed_fingerprint
                            .as_slice()
                            .try_into()
                            .map_err(|_| anyhow!("Incorrect seed_fingerprint length"))?,
                    ),
                    zip32::AccountId::try_from(hd_account_index)?,
                )),
            }),
            (None, None) => Ok(AccountPurpose::ViewOnly),
            _ => Err(anyhow!("Need either both (for spending) or neither (for view-only) of seed_fingerprint and hd_account_index")),
        }?;

        // Save the wallet config to disk.
        WalletConfig::init_without_mnemonic(wallet_dir.as_ref(), birthday.height(), network)?;

        let mut wallet_db = init_dbs(network, wallet_dir.as_ref())?;
        wallet_db.import_account_ufvk(&opts.name, &ufvk, &birthday, purpose, None)?;

        Ok(())
    }
}
