use std::{collections::BTreeMap, convert::Infallible};

use anyhow::anyhow;
use clap::Args;
use pczt::{
    roles::{signer::Signer, verifier::Verifier},
    Pczt,
};
use secrecy::ExposeSecret;
use tokio::io::{stdin, stdout, AsyncReadExt, AsyncWriteExt};

use ::transparent::{
    keys::{NonHardenedChildIndex, TransparentKeyScope},
    zip48,
};
use zcash_keys::keys::UnifiedSpendingKey;
use zcash_protocol::consensus::{NetworkConstants, Parameters};
use zip32::fingerprint::SeedFingerprint;

use crate::config::WalletConfig;

// Options accepted for the `pczt sign` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// age identity file to decrypt the mnemonic phrase with
    #[arg(short, long)]
    identity: String,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let mut config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        let mut buf = vec![];
        stdin().read_to_end(&mut buf).await?;

        let pczt = Pczt::parse(&buf).map_err(|e| anyhow!("Failed to read PCZT: {:?}", e))?;

        // Decrypt the mnemonic to access the seed.
        let identities = age::IdentityFile::from_file(self.identity)?.into_identities()?;

        let seed = config
            .decrypt_seed(identities.iter().map(|i| i.as_ref() as _))?
            .ok_or(anyhow!("Seed must be present to enable signing"))?;
        let seed_fp = SeedFingerprint::from_seed(seed.expose_secret())
            .ok_or_else(|| anyhow!("Invalid seed length"))?;

        // Find all the spends matching our seed.
        enum KeyRef {
            Orchard {
                index: usize,
            },
            Sapling {
                index: usize,
            },
            Transparent {
                index: usize,
                scope: TransparentKeyScope,
                address_index: NonHardenedChildIndex,
            },
            TransparentMultisig {
                index: usize,
                scope: zip32::Scope,
                address_index: NonHardenedChildIndex,
            },
        }
        let mut keys = BTreeMap::<zip32::AccountId, Vec<KeyRef>>::new();
        let pczt = Verifier::new(pczt)
            .with_orchard::<Infallible, _>(|bundle| {
                for (index, action) in bundle.actions().iter().enumerate() {
                    if let Some(account_index) = action
                        .spend()
                        .zip32_derivation()
                        .as_ref()
                        .and_then(|derivation| {
                            derivation.extract_account_index(
                                &seed_fp,
                                zip32::ChildIndex::hardened(params.network_type().coin_type()),
                            )
                        })
                    {
                        keys.entry(account_index)
                            .or_default()
                            .push(KeyRef::Orchard { index });
                    }
                }
                Ok(())
            })
            .map_err(|e| anyhow!("Invalid PCZT: {:?}", e))?
            .with_sapling::<Infallible, _>(|bundle| {
                for (index, spend) in bundle.spends().iter().enumerate() {
                    if let Some(account_index) =
                        spend.zip32_derivation().as_ref().and_then(|derivation| {
                            derivation.extract_account_index(
                                &seed_fp,
                                zip32::ChildIndex::hardened(params.network_type().coin_type()),
                            )
                        })
                    {
                        keys.entry(account_index)
                            .or_default()
                            .push(KeyRef::Sapling { index });
                    }
                }
                Ok(())
            })
            .map_err(|e| anyhow!("Invalid PCZT: {:?}", e))?
            .with_transparent::<Infallible, _>(|bundle| {
                let expected_coin_type = bip32::ChildNumber(
                    params.network_type().coin_type() | bip32::ChildNumber::HARDENED_FLAG,
                );
                for (index, input) in bundle.inputs().iter().enumerate() {
                    for derivation in input.bip32_derivation().values() {
                        if let Some((account_index, scope, address_index)) =
                            derivation.extract_bip_44_fields(&seed_fp, expected_coin_type)
                        {
                            keys.entry(account_index)
                                .or_default()
                                .push(KeyRef::Transparent {
                                    index,
                                    scope,
                                    address_index,
                                });
                        } else if let Some((account_index, scope, address_index)) =
                            derivation.extract_zip_48_fields(&seed_fp, expected_coin_type)
                        {
                            keys.entry(account_index).or_default().push(
                                KeyRef::TransparentMultisig {
                                    index,
                                    scope,
                                    address_index,
                                },
                            );
                        }
                    }
                }
                Ok(())
            })
            .map_err(|e| anyhow!("Invalid PCZT: {:?}", e))?
            .finish();

        let mut signer =
            Signer::new(pczt).map_err(|e| anyhow!("Failed to initialize Signer: {:?}", e))?;
        for (account_index, spends) in keys {
            let usk = UnifiedSpendingKey::from_seed(&params, seed.expose_secret(), account_index)?;
            let msk =
                zip48::AccountPrivKey::from_seed(&params, seed.expose_secret(), account_index)
                    .map_err(|e| anyhow!("Failed to derive ZIP 48 account private key: {e:?}"))?;
            for keyref in spends {
                match keyref {
                    KeyRef::Orchard { index } => {
                        signer
                            .sign_orchard(
                                index,
                                &orchard::keys::SpendAuthorizingKey::from(usk.orchard()),
                            )
                            .map_err(|e| {
                                anyhow!("Failed to sign Orchard spend {index}: {:?}", e)
                            })?;
                    }
                    KeyRef::Sapling { index } => {
                        signer
                            .sign_sapling(index, &usk.sapling().expsk.ask)
                            .map_err(|e| {
                                anyhow!("Failed to sign Sapling spend {index}: {:?}", e)
                            })?;
                    }
                    KeyRef::Transparent {
                        index,
                        scope,
                        address_index,
                    } => signer
                        .sign_transparent(
                            index,
                            &usk.transparent()
                                .derive_secret_key(scope, address_index)
                                .map_err(|e| {
                                    anyhow!(
                                        "Failed to derive transparent key at .../{:?}/{:?}: {:?}",
                                        scope,
                                        address_index,
                                        e,
                                    )
                                })?,
                        )
                        .map_err(|e| {
                            anyhow!("Failed to sign transparent input {index}: {:?}", e)
                        })?,
                    KeyRef::TransparentMultisig {
                        index,
                        scope,
                        address_index,
                    } => signer
                        .sign_transparent(index, &msk.derive_signing_key(scope, address_index))
                        .map_err(|e| {
                            anyhow!("Failed to sign transparent input {index}: {:?}", e)
                        })?,
                }
            }
        }

        let pczt = signer.finish();

        stdout().write_all(&pczt.serialize()).await?;

        Ok(())
    }
}
