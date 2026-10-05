use anyhow::anyhow;
use bip32::Prefix;
use clap::Args;
use pczt::{roles::updater::Updater, Pczt};
use sapling::zip32::DiversifiableFullViewingKey;
use secrecy::ExposeSecret;
use tokio::io::{stdin, stdout, AsyncReadExt, AsyncWriteExt};
use transparent::{address::TransparentAddress, pczt::Bip32Derivation, zip48};
use zcash_keys::keys::UnifiedSpendingKey;
use zcash_protocol::{
    consensus::{self, NetworkConstants, Parameters},
    PoolType,
};
use zcash_script::solver;
use zip32::fingerprint::SeedFingerprint;

use crate::config::WalletConfig;

// Options accepted for the `pczt update-with-derivation` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// The age identity file to decrypt the mnemonic phrase with.
    #[arg(short, long)]
    identity: String,

    /// The pool to derive within.
    #[arg(value_parser = parse_pool_type)]
    pool: PoolType,

    /// The ZIP 32 or BIP 44 path to derive.
    path: String,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> anyhow::Result<()> {
        let path = parse_path(&self.path)?;

        let mut config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        let mut buf = vec![];
        stdin().read_to_end(&mut buf).await?;

        let pczt = Pczt::parse(&buf).map_err(|e| anyhow!("Failed to read PCZT: {:?}", e))?;

        // Decrypt the mnemonic to access the seed.
        let identities = age::IdentityFile::from_file(self.identity)?.into_identities()?;
        let seed = config
            .decrypt_seed(identities.iter().map(|i| i.as_ref() as _))?
            .ok_or(anyhow!(
                "Seed must be present to enable updating a PCZT with a derivation path"
            ))?;

        let seed_fp = SeedFingerprint::from_seed(seed.expose_secret())
            .ok_or_else(|| anyhow!("Invalid seed length"))?;

        let updater = Updater::new(pczt);

        let updater = match self.pool {
            PoolType::Transparent => {
                let derivation = Bip32Derivation::parse(seed_fp.to_bytes(), path)
                    .map_err(|e| anyhow!("Invalid BIP 32 derivation: {e:?}"))?;

                let expected_coin_type =
                    bip32::ChildNumber(params.coin_type() | bip32::ChildNumber::HARDENED_FLAG);

                if let Some((account, scope, address_index)) =
                    derivation.extract_bip_44_fields(&seed_fp, expected_coin_type)
                {
                    let pubkey =
                        UnifiedSpendingKey::from_seed(&params, seed.expose_secret(), account)?
                            .transparent()
                            .to_account_pubkey()
                            .derive_address_pubkey(scope, address_index)
                            .map_err(|e| anyhow!("{e}"))?;

                    add_transparent(updater, pubkey, derivation)
                } else if let Some((account, scope, address_index)) =
                    derivation.extract_zip_48_fields(&seed_fp, expected_coin_type)
                {
                    let prefix = match params.network_type() {
                        consensus::NetworkType::Main => Prefix::XPUB,
                        consensus::NetworkType::Test => Prefix::TPUB,
                        consensus::NetworkType::Regtest => Prefix::TPUB,
                    };

                    let key =
                        zip48::AccountPrivKey::from_seed(&params, seed.expose_secret(), account)
                            .map_err(|e| anyhow!("{e}"))?
                            .to_account_pubkey()
                            .key_expression_for_address(prefix, scope, address_index);

                    // TODO: Add helper method to `zcash_script`.
                    let pubkey = match key.into_parts().1 {
                        zcash_script::descriptor::Key::Public { key, .. } => key,
                        zcash_script::descriptor::Key::Xpub { key, child, .. } => {
                            let mut curr_key = key;
                            for child_number in child {
                                curr_key = curr_key
                                    .derive_child(child_number)
                                    .map_err(|e| anyhow!("{e}"))?;
                            }
                            *curr_key.public_key()
                        }
                    };

                    add_transparent(updater, pubkey, derivation)
                } else {
                    Err(anyhow!(
                        "Path is not a valid BIP 44 path for this wallet's network"
                    ))
                }
            }
            PoolType::SAPLING => {
                let derivation = sapling::pczt::Zip32Derivation::parse(seed_fp.to_bytes(), path)
                    .map_err(|e| anyhow!("Invalid ZIP 32 derivation: {e:?}"))?;

                let account = derivation
                    .extract_account_index(
                        &seed_fp,
                        zip32::ChildIndex::hardened(params.coin_type()),
                    )
                    .ok_or_else(|| {
                        anyhow!("Path is not a valid ZIP 32 path for this wallet's network")
                    })?;

                let dfvk = UnifiedSpendingKey::from_seed(&params, seed.expose_secret(), account)?
                    .sapling()
                    .to_diversifiable_full_viewing_key();

                add_sapling(updater, dfvk, derivation)
            }
            PoolType::ORCHARD => {
                let derivation = orchard::pczt::Zip32Derivation::parse(seed_fp.to_bytes(), path)
                    .map_err(|e| anyhow!("Invalid ZIP 32 derivation: {e:?}"))?;

                let account = derivation
                    .extract_account_index(
                        &seed_fp,
                        zip32::ChildIndex::hardened(params.coin_type()),
                    )
                    .ok_or_else(|| {
                        anyhow!("Path is not a valid ZIP 32 path for this wallet's network")
                    })?;

                let fvk = UnifiedSpendingKey::from_seed(&params, seed.expose_secret(), account)?
                    .orchard()
                    .into();

                add_orchard(updater, fvk, derivation)
            }
        }
        .map_err(|e| anyhow!("{e:?}"))?;

        let pczt = updater.finish();

        stdout().write_all(&pczt.serialize()).await?;

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

fn parse_path(s: &str) -> anyhow::Result<Vec<u32>> {
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
                .and_then(|index| {
                    bip32::ChildNumber::new(index, hardened)
                        .map_err(|e| anyhow!("Invalid path index: {e}"))
                })
                .map(|i| i.0)
        })
        .collect()
}

fn add_transparent(
    updater: Updater,
    pubkey: secp256k1::PublicKey,
    derivation: Bip32Derivation,
) -> anyhow::Result<Updater> {
    let pubkey_bytes = pubkey.serialize();
    let p2pkh_addr = TransparentAddress::from_pubkey(&pubkey);

    let mut found_none = true;

    let updater = updater
        .update_transparent_with(|mut updater| {
            // Match pubkey to the inputs that use it.
            let inputs_to_update = updater
                .bundle()
                .inputs()
                .iter()
                .enumerate()
                .filter_map(|(index, input)| {
                    input
                        .redeem_script()
                        .as_ref()
                        .unwrap_or(input.script_pubkey())
                        .refine()
                        .ok()
                        .as_ref()
                        .and_then(solver::standard)
                        .and_then(|script| {
                            match script {
                                solver::ScriptKind::PubKeyHash { hash } => {
                                    TransparentAddress::PublicKeyHash(hash) == p2pkh_addr
                                }
                                solver::ScriptKind::MultiSig { pubkeys, .. } => {
                                    pubkeys.iter().any(|pk| pk.as_slice() == pubkey_bytes)
                                }
                                solver::ScriptKind::PubKey { data } => {
                                    data.as_slice() == pubkey_bytes
                                }
                                _ => false,
                            }
                            .then_some(index)
                        })
                })
                .collect::<Vec<_>>();

            found_none = inputs_to_update.is_empty();

            for index in inputs_to_update {
                updater.update_input_with(index, |mut input_updater| {
                    input_updater.set_bip32_derivation(
                        pubkey_bytes,
                        // TODO: `impl Clone for Bip32Derivation`
                        Bip32Derivation::parse(
                            *derivation.seed_fingerprint(),
                            derivation.derivation_path().iter().map(|i| i.0).collect(),
                        )
                        .expect("valid"),
                    );
                    Ok(())
                })?;
            }

            Ok(())
        })
        .map_err(|e| anyhow!("{e:?}"))?;

    if found_none {
        Err(anyhow!("No inputs matched the given derivation path"))
    } else {
        Ok(updater)
    }
}

fn add_sapling(
    updater: Updater,
    dfvk: DiversifiableFullViewingKey,
    derivation: sapling::pczt::Zip32Derivation,
) -> anyhow::Result<Updater> {
    let mut found_none = true;

    let updater = updater
        .update_sapling_with(|mut updater| {
            // Find inputs received at addresses derived from this FVK.
            let inputs_to_update = updater
                .bundle()
                .spends()
                .iter()
                .enumerate()
                .filter_map(|(index, spend)| {
                    spend
                        .recipient()
                        .as_ref()
                        .and_then(|addr| dfvk.decrypt_diversifier(addr))
                        .map(|_| index)
                })
                .collect::<Vec<_>>();

            found_none = inputs_to_update.is_empty();

            for index in inputs_to_update {
                updater.update_spend_with(index, |mut input_updater| {
                    input_updater.set_zip32_derivation(
                        // TODO: `impl Clone for Zip32Derivation`
                        sapling::pczt::Zip32Derivation::parse(
                            *derivation.seed_fingerprint(),
                            derivation
                                .derivation_path()
                                .iter()
                                .map(|i| i.index())
                                .collect(),
                        )
                        .expect("valid"),
                    );
                    Ok(())
                })?;
            }

            Ok(())
        })
        .map_err(|e| anyhow!("{e:?}"))?;

    if found_none {
        Err(anyhow!("No spends matched the given derivation path"))
    } else {
        Ok(updater)
    }
}

fn add_orchard(
    updater: Updater,
    fvk: orchard::keys::FullViewingKey,
    derivation: orchard::pczt::Zip32Derivation,
) -> anyhow::Result<Updater> {
    let mut found_none = true;

    let updater = updater
        .update_orchard_with(|mut updater| {
            // Find inputs received at addresses derived from this FVK.
            let inputs_to_update = updater
                .bundle()
                .actions()
                .iter()
                .enumerate()
                .filter_map(|(index, action)| {
                    action
                        .spend()
                        .recipient()
                        .as_ref()
                        .and_then(|addr| fvk.scope_for_address(addr))
                        .map(|_| index)
                })
                .collect::<Vec<_>>();

            found_none = inputs_to_update.is_empty();

            for index in inputs_to_update {
                updater.update_action_with(index, |mut action_updater| {
                    action_updater.set_spend_zip32_derivation(
                        // TODO: `impl Clone for Zip32Derivation`
                        orchard::pczt::Zip32Derivation::parse(
                            *derivation.seed_fingerprint(),
                            derivation
                                .derivation_path()
                                .iter()
                                .map(|i| i.index())
                                .collect(),
                        )
                        .expect("valid"),
                    );
                    Ok(())
                })?;
            }

            Ok(())
        })
        .map_err(|e| anyhow!("{e:?}"))?;

    if found_none {
        Err(anyhow!("No spends matched the given derivation path"))
    } else {
        Ok(updater)
    }
}
