use anyhow::anyhow;
use clap::Args;
use pczt::{
    roles::redactor::{
        orchard::ActionRedactor,
        sapling::{OutputRedactor as SaplingOutputRedactor, SpendRedactor},
        transparent::{InputRedactor, OutputRedactor as TransparentOutputRedactor},
        Redactor,
    },
    Pczt,
};
use tokio::io::{stdin, stdout, AsyncReadExt, AsyncWriteExt};

// Options accepted for the `pczt redact` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// A list of PCZT keys to redact, in foo.bar.baz notation
    #[arg(short, long)]
    key: Vec<String>,
}

impl Command {
    pub(crate) async fn run(self) -> Result<(), anyhow::Error> {
        let mut buf = vec![];
        stdin().read_to_end(&mut buf).await?;

        let pczt = Pczt::parse(&buf).map_err(|e| anyhow!("Failed to read PCZT: {:?}", e))?;

        let pczt = self
            .key
            .into_iter()
            .try_fold(Redactor::new(pczt), |r, key| {
                redact_pczt(r, key.split('.')).map_err(|e| anyhow!("Failed to redact '{key}': {e}"))
            })?
            .finish();

        stdout().write_all(&pczt.serialize()).await?;

        Ok(())
    }
}

fn redact_pczt<'a>(
    r: Redactor,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    match key.next() {
        Some("global") => redact_global(r, key),
        Some("transparent") => redact_transparent(r, key),
        Some("sapling") => redact_sapling(r, key),
        Some("orchard") => redact_orchard(r, key),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_global<'a>(
    r: Redactor,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    match key.next() {
        Some(
            field @ ("tx_version"
            | "version_group_id"
            | "consensus_branch_id"
            | "fallback_lock_time"
            | "expiry_height"
            | "coin_type"
            | "tx_modifiable"),
        ) => Err(anyhow!("Cannot redact '{}'", field)),
        Some("proprietary") => Ok(r.redact_global_with(|mut global| match key.next() {
            Some(key) => global.redact_proprietary(key),
            None => global.clear_proprietary(),
        })),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_transparent<'a>(
    r: Redactor,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    match key.next() {
        Some("inputs") => redact_transparent_input(r, None, key),
        Some("outputs") => redact_transparent_output(r, None, key),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_transparent_input<'a>(
    r: Redactor,
    index: Option<usize>,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    fn redact<F>(r: Redactor, index: Option<usize>, f: F) -> Redactor
    where
        F: FnMut(InputRedactor),
    {
        r.redact_transparent_with(|mut transparent| match index {
            Some(index) => transparent.redact_input(index, f),
            None => transparent.redact_inputs(f),
        })
    }

    fn redact_map<F, const N: usize>(
        r: Redactor,
        index: Option<usize>,
        key: &str,
        mut f: F,
    ) -> Result<Redactor, anyhow::Error>
    where
        F: FnMut(InputRedactor, [u8; N]),
    {
        match hex::decode(key) {
            Ok(key) => match key[..].try_into() {
                Ok(key) => Ok(redact(r, index, |input| f(input, key))),
                _ => Err(anyhow!("Invalid map key length")),
            },
            _ => Err(anyhow!("Invalid hex '{key}'")),
        }
    }

    match key.next() {
        Some(
            field @ ("prevout_txid"
            | "prevout_index"
            | "sequence"
            | "required_time_lock_time"
            | "required_height_lock_time"
            | "value"
            | "script_pubkey"
            | "sighash_type"),
        ) => Err(anyhow!("Cannot redact '{}'", field)),
        Some("script_sig") => Ok(redact(r, index, |mut output| output.clear_script_sig())),
        Some("redeem_script") => Ok(redact(r, index, |mut output| output.clear_redeem_script())),
        Some("partial_signatures") => match key.next() {
            Some(pubkey) => redact_map(r, index, pubkey, |mut input, pubkey| {
                input.redact_partial_signature(pubkey)
            }),
            None => Ok(redact(r, index, |mut input| {
                input.clear_partial_signatures()
            })),
        },
        Some("bip32_derivation") => match key.next() {
            Some(pubkey) => redact_map(r, index, pubkey, |mut input, pubkey| {
                input.redact_bip32_derivation(pubkey)
            }),
            None => Ok(redact(r, index, |mut input| input.clear_bip32_derivation())),
        },
        Some("ripemd160_preimages") => match key.next() {
            Some(pubkey) => redact_map(r, index, pubkey, |mut input, pubkey| {
                input.redact_ripemd160_preimage(pubkey)
            }),
            None => Ok(redact(r, index, |mut input| {
                input.clear_ripemd160_preimages()
            })),
        },
        Some("sha256_preimages") => match key.next() {
            Some(pubkey) => redact_map(r, index, pubkey, |mut input, pubkey| {
                input.redact_sha256_preimage(pubkey)
            }),
            None => Ok(redact(r, index, |mut input| input.clear_sha256_preimages())),
        },
        Some("hash160_preimages") => match key.next() {
            Some(pubkey) => redact_map(r, index, pubkey, |mut input, pubkey| {
                input.redact_hash160_preimage(pubkey)
            }),
            None => Ok(redact(r, index, |mut input| {
                input.clear_hash160_preimages()
            })),
        },
        Some("hash256_preimages") => match key.next() {
            Some(pubkey) => redact_map(r, index, pubkey, |mut input, pubkey| {
                input.redact_hash256_preimage(pubkey)
            }),
            None => Ok(redact(r, index, |mut input| {
                input.clear_hash256_preimages()
            })),
        },
        Some("proprietary") => Ok(redact(r, index, |mut input| match key.next() {
            Some(key) => input.redact_proprietary(key),
            None => input.clear_proprietary(),
        })),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_transparent_output<'a>(
    r: Redactor,
    index: Option<usize>,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    fn redact<F>(r: Redactor, index: Option<usize>, f: F) -> Redactor
    where
        F: FnMut(TransparentOutputRedactor),
    {
        r.redact_transparent_with(|mut transparent| match index {
            Some(index) => transparent.redact_output(index, f),
            None => transparent.redact_outputs(f),
        })
    }

    match key.next() {
        Some(field @ ("value" | "script_pubkey")) => Err(anyhow!("Cannot redact '{}'", field)),
        Some("redeem_script") => Ok(redact(r, index, |mut output| output.clear_redeem_script())),
        Some("bip32_derivation") => match key.next() {
            Some(data) => match hex::decode(data) {
                Ok(pubkey) => match pubkey[..].try_into() {
                    Ok(pubkey) => Ok(redact(r, index, |mut output| {
                        output.redact_bip32_derivation(pubkey)
                    })),
                    _ => Err(anyhow!("Invalid pubkey length")),
                },
                _ => Err(anyhow!("Invalid hex pubkey '{data}'")),
            },
            None => Ok(redact(r, index, |mut output| {
                output.clear_bip32_derivation()
            })),
        },
        Some("user_address") => Ok(redact(r, index, |mut output| output.clear_user_address())),
        Some("proprietary") => Ok(redact(r, index, |mut output| match key.next() {
            Some(key) => output.redact_proprietary(key),
            None => output.clear_proprietary(),
        })),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_sapling<'a>(
    r: Redactor,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    match key.next() {
        Some("spends") => redact_sapling_spend(r, None, key),
        Some("spend") => match key.next() {
            Some(index) => match index.parse() {
                Ok(index) => redact_sapling_spend(r, Some(index), key),
                Err(_) => Err(anyhow!("Invalid index '{index}'")),
            },
            None => Err(anyhow!("Missing index")),
        },
        Some("outputs") => redact_sapling_output(r, None, key),
        Some("output") => match key.next() {
            Some(index) => match index.parse() {
                Ok(index) => redact_sapling_output(r, Some(index), key),
                Err(_) => Err(anyhow!("Invalid index '{index}'")),
            },
            None => Err(anyhow!("Missing index")),
        },
        Some(field @ ("value_sum" | "anchor")) => Err(anyhow!("Cannot redact '{}'", field)),
        Some("bsk") => Ok(r.redact_sapling_with(|mut sapling| sapling.clear_bsk())),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_sapling_spend<'a>(
    r: Redactor,
    index: Option<usize>,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    fn redact<F>(r: Redactor, index: Option<usize>, f: F) -> Redactor
    where
        F: FnMut(SpendRedactor),
    {
        r.redact_sapling_with(|mut sapling| match index {
            Some(index) => sapling.redact_spend(index, f),
            None => sapling.redact_spends(f),
        })
    }

    match key.next() {
        Some(field @ ("cv" | "nullifier" | "rk")) => Err(anyhow!("Cannot redact '{}'", field)),
        Some("zkproof") => Ok(redact(r, index, |mut spend| spend.clear_zkproof())),
        Some("spend_auth_sig") => Ok(redact(r, index, |mut spend| spend.clear_spend_auth_sig())),
        Some("recipient") => Ok(redact(r, index, |mut spend| spend.clear_recipient())),
        Some("value") => Ok(redact(r, index, |mut spend| spend.clear_value())),
        Some("rcm") => Ok(redact(r, index, |mut spend| spend.clear_rcm())),
        Some("rseed") => Ok(redact(r, index, |mut spend| spend.clear_rseed())),
        Some("rcv") => Ok(redact(r, index, |mut spend| spend.clear_rcv())),
        Some("proof_generation_key") => Ok(redact(r, index, |mut spend| {
            spend.clear_proof_generation_key()
        })),
        Some("witness") => Ok(redact(r, index, |mut spend| spend.clear_witness())),
        Some("alpha") => Ok(redact(r, index, |mut spend| spend.clear_alpha())),
        Some("zip32_derivation") => {
            Ok(redact(r, index, |mut spend| spend.clear_zip32_derivation()))
        }
        Some("dummy_ask") => Ok(redact(r, index, |mut spend| spend.clear_dummy_ask())),
        Some("proprietary") => Ok(redact(r, index, |mut spend| match key.next() {
            Some(key) => spend.redact_proprietary(key),
            None => spend.clear_proprietary(),
        })),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_sapling_output<'a>(
    r: Redactor,
    index: Option<usize>,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    fn redact<F>(r: Redactor, index: Option<usize>, f: F) -> Redactor
    where
        F: FnMut(SaplingOutputRedactor),
    {
        r.redact_sapling_with(|mut sapling| match index {
            Some(index) => sapling.redact_output(index, f),
            None => sapling.redact_outputs(f),
        })
    }

    match key.next() {
        Some(field @ ("cv" | "cmu" | "ephemeral_key" | "enc_ciphertext" | "out_ciphertext")) => {
            Err(anyhow!("Cannot redact '{}'", field))
        }
        Some("zkproof") => Ok(redact(r, index, |mut output| output.clear_zkproof())),
        Some("recipient") => Ok(redact(r, index, |mut output| output.clear_recipient())),
        Some("value") => Ok(redact(r, index, |mut output| output.clear_value())),
        Some("rseed") => Ok(redact(r, index, |mut output| output.clear_rseed())),
        Some("rcv") => Ok(redact(r, index, |mut output| output.clear_rcv())),
        Some("ock") => Ok(redact(r, index, |mut output| output.clear_ock())),
        Some("zip32_derivation") => Ok(redact(r, index, |mut output| {
            output.clear_zip32_derivation()
        })),
        Some("user_address") => Ok(redact(r, index, |mut output| output.clear_user_address())),
        Some("proprietary") => Ok(redact(r, index, |mut output| match key.next() {
            Some(key) => output.redact_proprietary(key),
            None => output.clear_proprietary(),
        })),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_orchard<'a>(
    r: Redactor,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    match key.next() {
        Some("actions") => redact_orchard_action(r, None, key),
        Some("action") => match key.next() {
            Some(index) => match index.parse() {
                Ok(index) => redact_orchard_action(r, Some(index), key),
                Err(_) => Err(anyhow!("Invalid index '{index}'")),
            },
            None => Err(anyhow!("Missing index")),
        },
        Some(field @ ("flags" | "value_sum" | "anchor")) => {
            Err(anyhow!("Cannot redact '{}'", field))
        }
        Some("zkproof") => Ok(r.redact_orchard_with(|mut orchard| orchard.clear_zkproof())),
        Some("bsk") => Ok(r.redact_orchard_with(|mut orchard| orchard.clear_bsk())),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_orchard_action<'a>(
    r: Redactor,
    index: Option<usize>,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    fn redact<F>(r: Redactor, index: Option<usize>, f: F) -> Redactor
    where
        F: FnMut(ActionRedactor),
    {
        r.redact_orchard_with(|mut orchard| match index {
            Some(index) => orchard.redact_action(index, f),
            None => orchard.redact_actions(f),
        })
    }

    match key.next() {
        Some(field @ "cv_net") => Err(anyhow!("Cannot redact '{}'", field)),
        Some("spend") => redact_orchard_spend(r, index, key),
        Some("output") => redact_orchard_output(r, index, key),
        Some("rcv") => Ok(redact(r, index, |mut action| action.clear_rcv())),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_orchard_spend<'a>(
    r: Redactor,
    index: Option<usize>,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    fn redact<F>(r: Redactor, index: Option<usize>, f: F) -> Redactor
    where
        F: FnMut(ActionRedactor),
    {
        r.redact_orchard_with(|mut orchard| match index {
            Some(index) => orchard.redact_action(index, f),
            None => orchard.redact_actions(f),
        })
    }

    match key.next() {
        Some(field @ ("nullifier" | "rk")) => Err(anyhow!("Cannot redact '{}'", field)),
        Some("spend_auth_sig") => Ok(redact(r, index, |mut action| action.clear_spend_auth_sig())),
        Some("recipient") => Ok(redact(r, index, |mut action| {
            action.clear_spend_recipient()
        })),
        Some("value") => Ok(redact(r, index, |mut action| action.clear_spend_value())),
        Some("rho") => Ok(redact(r, index, |mut action| action.clear_spend_rho())),
        Some("rseed") => Ok(redact(r, index, |mut action| action.clear_spend_rseed())),
        Some("fvk") => Ok(redact(r, index, |mut action| action.clear_spend_fvk())),
        Some("witness") => Ok(redact(r, index, |mut action| action.clear_spend_witness())),
        Some("alpha") => Ok(redact(r, index, |mut action| action.clear_spend_alpha())),
        Some("zip32_derivation") => Ok(redact(r, index, |mut action| {
            action.clear_spend_zip32_derivation()
        })),
        Some("dummy_sk") => Ok(redact(r, index, |mut action| action.clear_spend_dummy_sk())),
        Some("proprietary") => Ok(r.redact_orchard_with(|mut orchard| match index {
            Some(index) => orchard.redact_action(index, |mut action| match key.next() {
                Some(key) => action.redact_spend_proprietary(key),
                None => action.clear_spend_proprietary(),
            }),
            None => orchard.redact_actions(|mut actions| match key.next() {
                Some(key) => actions.redact_spend_proprietary(key),
                None => actions.clear_spend_proprietary(),
            }),
        })),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}

fn redact_orchard_output<'a>(
    r: Redactor,
    index: Option<usize>,
    mut key: impl Iterator<Item = &'a str>,
) -> Result<Redactor, anyhow::Error> {
    fn redact<F>(r: Redactor, index: Option<usize>, f: F) -> Redactor
    where
        F: FnMut(ActionRedactor),
    {
        r.redact_orchard_with(|mut orchard| match index {
            Some(index) => orchard.redact_action(index, f),
            None => orchard.redact_actions(f),
        })
    }

    match key.next() {
        Some(field @ ("cmx" | "ephemeral_key" | "enc_ciphertext" | "out_ciphertext")) => {
            Err(anyhow!("Cannot redact '{}'", field))
        }
        Some("recipient") => Ok(redact(r, index, |mut action| {
            action.clear_output_recipient()
        })),
        Some("value") => Ok(redact(r, index, |mut action| action.clear_output_value())),
        Some("rseed") => Ok(redact(r, index, |mut action| action.clear_output_rseed())),
        Some("ock") => Ok(redact(r, index, |mut action| action.clear_output_ock())),
        Some("zip32_derivation") => Ok(redact(r, index, |mut action| {
            action.clear_output_zip32_derivation()
        })),
        Some("user_address") => Ok(redact(r, index, |mut action| {
            action.clear_output_user_address()
        })),
        Some("proprietary") => Ok(redact(r, index, |mut action| match key.next() {
            Some(key) => action.redact_output_proprietary(key),
            None => action.clear_output_proprietary(),
        })),
        Some(field) => Err(anyhow!("Unknown field '{}'", field)),
        None => Err(anyhow!("Empty field")),
    }
}
