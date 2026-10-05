use anyhow::anyhow;
use clap::Args;
use pczt::{
    roles::{prover::Prover, updater::Updater},
    Pczt,
};
use sapling::ProofGenerationKey;
use secrecy::{ExposeSecret, SecretVec};
use tokio::io::{stdin, stdout, AsyncReadExt, AsyncWriteExt};
use zcash_keys::keys::UnifiedSpendingKey;
use zcash_proofs::prover::LocalTxProver;
use zcash_protocol::consensus::{NetworkConstants, Parameters};
use zip32::fingerprint::SeedFingerprint;

use crate::{config::WalletConfig, parse_hex};

// Options accepted for the `pczt prove` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// Hex encoding of the Sapling proof generation key
    #[arg(long)]
    #[arg(value_parser = parse_hex)]
    sapling_proof_generation_key: Option<std::vec::Vec<u8>>,

    /// age identity file to decrypt the mnemonic phrase with for deriving the Sapling proof generation key
    #[arg(short, long)]
    identity: Option<String>,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let mut buf = vec![];
        stdin().read_to_end(&mut buf).await?;

        let pczt = Pczt::parse(&buf).map_err(|e| anyhow!("Failed to read PCZT: {:?}", e))?;

        // If we have Sapling spends, we need Sapling proof generation keys.
        let pczt = if !pczt.sapling().spends().is_empty() {
            enum PgkSource {
                Provided(ProofGenerationKey),
                Wallet {
                    config: WalletConfig,
                    seed: SecretVec<u8>,
                },
            }

            impl PgkSource {
                fn proof_generation_key(
                    &self,
                    derivation: Option<([u8; 32], Vec<zip32::ChildIndex>)>,
                ) -> anyhow::Result<ProofGenerationKey> {
                    match self {
                        PgkSource::Provided(proof_generation_key) => {
                            Ok(proof_generation_key.clone())
                        }
                        PgkSource::Wallet { config, seed } => {
                            if let Some((seed_fingerprint, derivation_path)) = derivation {
                                let params = config.network();

                                let seed_fp = SeedFingerprint::from_seed(seed.expose_secret())
                                    .ok_or_else(|| anyhow!("Invalid seed length"))?;

                                if seed_fingerprint == seed_fp.to_bytes()
                                    && derivation_path.len() == 3
                                    && derivation_path[0] == zip32::ChildIndex::hardened(32)
                                    && derivation_path[1]
                                        == zip32::ChildIndex::hardened(
                                            params.network_type().coin_type(),
                                        )
                                {
                                    let account_index = zip32::AccountId::try_from(
                                        derivation_path[2].index() - (1 << 31),
                                    )
                                    .expect("valid");

                                    let usk = UnifiedSpendingKey::from_seed(
                                        &params,
                                        seed.expose_secret(),
                                        account_index,
                                    )?;

                                    Ok(usk.sapling().expsk.proof_generation_key())
                                } else {
                                    Err(anyhow!(
                                        "Invalid ZIP 32 derivation path for PCZT Sapling spend"
                                    ))
                                }
                            } else {
                                Err(anyhow!(
                                    "Missing ZIP 32 derivation path for PCZT Sapling spend"
                                ))
                            }
                        }
                    }
                }
            }

            let pkg_source = match (self.sapling_proof_generation_key, self.identity) {
                (Some(proof_generation_key), _) => {
                    if proof_generation_key.len() == 64 {
                        Ok(PgkSource::Provided(sapling::keys::ProofGenerationKey {
                            ak: sapling::keys::SpendValidatingKey::temporary_zcash_from_bytes(
                                &proof_generation_key[..32],
                            )
                            .ok_or_else(|| anyhow!("Invalid Sapling proof generation key"))?,
                            nsk: jubjub::Scalar::from_bytes(
                                &proof_generation_key[32..].try_into().unwrap(),
                            )
                            .into_option()
                            .ok_or_else(|| anyhow!("Invalid Sapling proof generation key"))?,
                        }))
                    } else {
                        Err(anyhow!("Invalid Sapling proof generation key"))
                    }
                }
                (None, Some(identity)) => {
                    // Try to load it from the wallet config.
                    let mut config = WalletConfig::read(wallet_dir.as_ref())?;

                    // Decrypt the mnemonic to access the seed.
                    let identities = age::IdentityFile::from_file(identity)?.into_identities()?;

                    // Cache the seed fingerprint for matching.
                    let seed = config
                        .decrypt_seed(identities.iter().map(|i| i.as_ref() as _))?
                        .ok_or(anyhow!("Seed must be present to enable signing"))?;

                    Ok(PgkSource::Wallet { config, seed })
                }
                (None, None) => Err(anyhow!(
                    "Cannot create Sapling proofs without a proof generation key"
                )),
            }?;

            // Add Sapling proof generation key.
            Updater::new(pczt)
                .update_sapling_with(|mut updater| {
                    let non_dummy_spends = updater
                        .bundle()
                        .spends()
                        .iter()
                        .enumerate()
                        // Dummy spends will already have a proof generation key.
                        .filter(|(_, spend)| spend.proof_generation_key().is_none())
                        .map(|(index, spend)| {
                            (
                                index,
                                spend
                                    .zip32_derivation()
                                    .as_ref()
                                    .map(|d| (*d.seed_fingerprint(), d.derivation_path().clone())),
                            )
                        })
                        .collect::<Vec<_>>();

                    // Assume all non-dummy spent notes are from the same account.
                    for (index, derivation) in non_dummy_spends {
                        updater.update_spend_with(index, |mut spend_updater| {
                            spend_updater.set_proof_generation_key(
                                pkg_source.proof_generation_key(derivation).unwrap(),
                            )
                        })?;
                    }

                    Ok(())
                })
                .map_err(|e| anyhow!("Failed to add Sapling proof generation key: {:?}", e))?
                .finish()
        } else {
            pczt
        };

        let prover = LocalTxProver::bundled();

        let pczt = Prover::new(pczt)
            .create_orchard_proof(&orchard::circuit::ProvingKey::build())
            .map_err(|e| anyhow!("Failed to create Orchard proof: {:?}", e))?
            .create_sapling_proofs(&prover, &prover)
            .map_err(|e| anyhow!("Failed to create Sapling proofs: {:?}", e))?
            .finish();

        stdout().write_all(&pczt.serialize()).await?;

        Ok(())
    }
}
