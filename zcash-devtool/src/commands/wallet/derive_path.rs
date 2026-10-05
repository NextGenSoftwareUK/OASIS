use anyhow::anyhow;
use bip32::PublicKey;
use clap::Args;
use secrecy::ExposeSecret;
use transparent::address::TransparentAddress;
use zcash_address::{ToAddress, ZcashAddress};
use zcash_keys::{address::UnifiedAddress, encoding::AddressCodec};
use zcash_protocol::{
    consensus::{NetworkConstants, Parameters},
    PoolType,
};

use crate::config::WalletConfig;

// Options accepted for the `derive-path` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// age identity file to decrypt the mnemonic phrase with
    #[arg(short, long)]
    identity: String,

    /// The pool to derive within.
    #[arg(value_parser = parse_pool_type)]
    pool: PoolType,

    /// The ZIP 32 or BIP 44 path to derive.
    path: String,
}

impl Command {
    pub(crate) fn run(self, wallet_dir: Option<String>) -> anyhow::Result<()> {
        let path = parse_path(&self.path)?;

        let mut config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        // Decrypt the mnemonic to access the seed.
        let identities = age::IdentityFile::from_file(self.identity)?.into_identities()?;
        let seed = config
            .decrypt_seed(identities.iter().map(|i| i.as_ref() as _))?
            .ok_or(anyhow!(
                "Seed must be present to enable generating a new account"
            ))?;

        match self.pool {
            PoolType::Transparent => {
                let mut xprv =
                    bip32::ExtendedPrivateKey::<secp256k1::SecretKey>::new(seed.expose_secret())
                        .map_err(|e| anyhow!("{e}"))?;

                for (index, hardened) in &path {
                    let child_number =
                        bip32::ChildNumber::new(*index, *hardened).map_err(|e| anyhow!("{e}"))?;
                    xprv = xprv
                        .derive_child(child_number)
                        .map_err(|e| anyhow!("{e}"))?;
                }

                let xpub = xprv.public_key();

                // Print out transparent information.
                println!("Transparent derivation at {}:", self.path);
                let show_address = match path.as_slice() {
                    [(44, true), subpath @ ..] => {
                        println!(" - BIP 44 derivation path");
                        match subpath {
                            [] => {
                                println!("  ⚠️  Missing coin type");
                                false
                            }
                            [_] => {
                                println!("  ⚠️  Missing account");
                                false
                            }
                            [(coin_type, coin_type_hardened), (account, account_hardened), subpath @ ..] =>
                            {
                                if !*coin_type_hardened {
                                    println!("  ⚠️  Coin type is not hardened");
                                }
                                let network_match = *coin_type == params.coin_type();
                                if !network_match {
                                    println!(
                                        "  ⚠️  Coin type ({}) does not match the wallet's network ({})",
                                        coin_type,
                                        params.coin_type(),
                                    );
                                }
                                println!("   - Account: {account}");
                                if !*account_hardened {
                                    println!("  ⚠️  Account is not hardened");
                                }
                                match subpath {
                                    [(kind, kind_hardened), (address_index, address_index_hardened)] =>
                                    {
                                        match kind {
                                            0 => println!("   - External chain"),
                                            1 => println!("   - Internal chain (change addresses)"),
                                            2 => println!(
                                                "   - Ephemeral chain (TEX address payments)"
                                            ),
                                            _ => println!("  ⚠️  Unknown address kind"),
                                        }
                                        if *kind_hardened {
                                            println!("  ⚠️  Address kind is hardened");
                                        }
                                        println!("   - Address index: {address_index}");
                                        if *address_index_hardened {
                                            println!("  ⚠️  Address index is hardened");
                                        }
                                        // Only encode as an address if the network
                                        // matches the wallet.
                                        network_match
                                    }
                                    _ => false,
                                }
                            }
                        }
                    }
                    _ => {
                        println!("⚠️  Not a BIP 44 derivation path");
                        false
                    }
                };
                println!(
                    " - Extended public key: {}",
                    xpub.to_extended_key(bip32::Prefix::XPUB),
                );
                println!("   - Depth: {}", xpub.attrs().depth);
                println!("   - Child number: {}", xpub.attrs().child_number);
                println!(
                    "   - Public key: {}",
                    hex::encode(xpub.public_key().to_bytes())
                );
                if show_address {
                    println!(
                        " - P2PKH address: {}",
                        TransparentAddress::from_pubkey(xpub.public_key()).encode(&params),
                    );
                }
            }
            PoolType::SAPLING => {
                // Print out Sapling information.
                println!("Sapling derivation at {}:", self.path);
                let address = match path.as_slice() {
                    [(32, true), subpath @ ..] => {
                        println!(" - ZIP 32 derivation path");
                        match subpath {
                            [] => {
                                println!("  ⚠️  Missing coin type");
                                None
                            }
                            [_] => {
                                println!("  ⚠️  Missing account");
                                None
                            }
                            [(coin_type, coin_type_hardened), (account, account_hardened), subpath @ ..] =>
                            {
                                if !*coin_type_hardened {
                                    println!("  ⚠️  Coin type is not hardened");
                                }
                                let network_match = *coin_type == params.coin_type();
                                if !network_match {
                                    println!(
                                        "  ⚠️  Coin type ({}) does not match the wallet's network ({})",
                                        coin_type,
                                        params.coin_type(),
                                    );
                                }
                                println!("   - Account: {account}");
                                if !*account_hardened {
                                    println!("  ⚠️  Account is not hardened");
                                }
                                match subpath {
                                    [] => {
                                        // Only encode as an address if the network
                                        // matches the wallet.
                                        network_match.then(|| {
                                            sapling::zip32::ExtendedSpendingKey::master(
                                                seed.expose_secret(),
                                            )
                                            .derive_child(zip32::ChildIndex::hardened(32))
                                            .derive_child(zip32::ChildIndex::hardened(*coin_type))
                                            .derive_child(zip32::ChildIndex::hardened(*account))
                                            .default_address()
                                            .1
                                        })
                                    }
                                    _ => None,
                                }
                            }
                        }
                    }
                    _ => {
                        println!("⚠️  Not a ZIP 32 derivation path");
                        None
                    }
                };
                if let Some(addr) = address {
                    println!(
                        " - Default address: {}",
                        ZcashAddress::from_sapling(params.network_type(), addr.to_bytes())
                    );
                }
            }
            PoolType::ORCHARD => {
                // Print out Orchard information.
                println!("Orchard derivation at {}:", self.path);
                let address = match path.as_slice() {
                    [(32, true), subpath @ ..] => {
                        println!(" - ZIP 32 derivation path");
                        match subpath {
                            [] => {
                                println!("  ⚠️  Missing coin type");
                                None
                            }
                            [_] => {
                                println!("  ⚠️  Missing account");
                                None
                            }
                            [(coin_type, coin_type_hardened), (account, account_hardened), subpath @ ..] =>
                            {
                                if !*coin_type_hardened {
                                    println!("  ⚠️  Coin type is not hardened");
                                }
                                let network_match = *coin_type == params.coin_type();
                                if !network_match {
                                    println!(
                                        "  ⚠️  Coin type ({}) does not match the wallet's network ({})",
                                        coin_type,
                                        params.coin_type(),
                                    );
                                }
                                println!("   - Account: {account}");
                                if !*account_hardened {
                                    println!("  ⚠️  Account is not hardened");
                                }
                                match subpath {
                                    [] => {
                                        // Only encode as an address if the network
                                        // matches the wallet.
                                        network_match
                                            .then(|| {
                                                orchard::keys::SpendingKey::from_zip32_seed(
                                                    seed.expose_secret(),
                                                    *coin_type,
                                                    zip32::AccountId::try_from(*account)
                                                        .expect("in range"),
                                                )
                                                .ok()
                                            })
                                            .flatten()
                                            .map(|sk| {
                                                let fvk = orchard::keys::FullViewingKey::from(&sk);
                                                fvk.address_at(0u32, zip32::Scope::External)
                                            })
                                    }
                                    _ => None,
                                }
                            }
                        }
                    }
                    _ => {
                        println!("⚠️  Not a ZIP 32 derivation path");
                        None
                    }
                };
                if let Some(addr) = address {
                    println!(
                        " - Default address: {}",
                        UnifiedAddress::from_receivers(Some(addr), None, None)
                            .expect("valid")
                            .encode(&params),
                    );
                }
            }
        }

        Ok(())
    }
}

fn parse_pool_type(s: &str) -> anyhow::Result<PoolType> {
    match s {
        "transparent" => Ok(PoolType::Transparent),
        "sapling" => Ok(PoolType::SAPLING),
        "orchard" => Ok(PoolType::ORCHARD),
        _ => Err(anyhow!(
            "Invalid pool type '{s}', must be one of ['transparent', 'sapling', 'orchard']"
        )),
    }
}

fn parse_path(s: &str) -> anyhow::Result<Vec<(u32, bool)>> {
    s.strip_prefix("m/")
        .ok_or_else(|| anyhow!("Path does not start with m/"))?
        .split('/')
        .map(|index| {
            let (index, hardened) = if let Some(index) = index.strip_suffix('\'') {
                (index, true)
            } else {
                (index, false)
            };
            index
                .parse::<u32>()
                .map_err(|e| anyhow!("Invalid path index: {e}"))
                .map(|index| (index, hardened))
        })
        .collect()
}
