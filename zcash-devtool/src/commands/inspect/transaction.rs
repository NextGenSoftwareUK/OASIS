use std::{
    collections::HashMap,
    convert::{TryFrom, TryInto},
};

use bellman::groth16;
use group::GroupEncoding;
use sapling::{note_encryption::SaplingDomain, SaplingVerificationContext};
use secp256k1::{Secp256k1, VerifyOnly};

use ::transparent::{
    address::{Script, TransparentAddress},
    bundle as transparent,
    sighash::{SighashType, TransparentAuthorizingContext},
};
use orchard::note_encryption::OrchardDomain;
use zcash_address::{
    unified::{self, Encoding},
    ToAddress, ZcashAddress,
};
use zcash_note_encryption::try_output_recovery_with_ovk;
#[allow(deprecated)]
use zcash_primitives::transaction::{
    components::sapling as sapling_serialization,
    sighash::{signature_hash, SignableInput},
    txid::TxIdDigester,
    Authorization, Transaction, TransactionData, TxId, TxVersion,
};
use zcash_protocol::{
    consensus::BlockHeight,
    memo::{Memo, MemoBytes},
    value::Zatoshis,
};
use zcash_script::{script, solver};

use super::{
    context::{Context, ZTxOut},
    GROTH16_PARAMS, ORCHARD_VK,
};

pub fn is_coinbase(tx: &Transaction) -> bool {
    tx.transparent_bundle()
        .map(|b| b.is_coinbase())
        .unwrap_or(false)
}

pub fn extract_height_from_coinbase(tx: &Transaction) -> Option<BlockHeight> {
    tx.transparent_bundle()
        .and_then(|bundle| bundle.vin.first())
        .and_then(|input| script::Sig::parse(&input.script_sig().0).ok())
        .as_ref()
        .and_then(|script_sig| script_sig.0.first())
        .and_then(|opcode| {
            opcode.to_num().ok().and_then(|v| match v {
                // 0 will never occur as the first byte of a coinbase scriptSig.
                0 => None,
                v => v.try_into().ok(),
            })
        })
}

fn render_value(value: u64) -> String {
    format!(
        "{} zatoshis ({} ZEC)",
        value,
        (value as f64) / 1_0000_0000f64
    )
}

fn render_memo(memo_bytes: MemoBytes) -> String {
    match Memo::try_from(memo_bytes) {
        Ok(Memo::Empty) => "No memo".to_string(),
        Ok(Memo::Text(memo)) => format!("Text memo: '{}'", String::from(memo)),
        Ok(memo) => format!("{memo:?}"),
        Err(e) => format!("Invalid memo: {e}"),
    }
}

#[derive(Clone, Debug)]
pub(crate) struct TransparentAuth {
    all_prev_outputs: Vec<transparent::TxOut>,
}

impl transparent::Authorization for TransparentAuth {
    type ScriptSig = Script;
}

impl TransparentAuthorizingContext for TransparentAuth {
    fn input_amounts(&self) -> Vec<Zatoshis> {
        self.all_prev_outputs
            .iter()
            .map(|prevout| prevout.value())
            .collect()
    }

    fn input_scriptpubkeys(&self) -> Vec<Script> {
        self.all_prev_outputs
            .iter()
            .map(|prevout| prevout.script_pubkey().clone())
            .collect()
    }
}

struct MapTransparent {
    auth: TransparentAuth,
}

impl transparent::MapAuth<transparent::Authorized, TransparentAuth> for MapTransparent {
    fn map_script_sig(
        &self,
        s: <transparent::Authorized as transparent::Authorization>::ScriptSig,
    ) -> <TransparentAuth as transparent::Authorization>::ScriptSig {
        s
    }

    fn map_authorization(&self, _: transparent::Authorized) -> TransparentAuth {
        // TODO: This map should consume self, so we can move self.auth
        self.auth.clone()
    }
}

pub(crate) struct PrecomputedAuth;

impl Authorization for PrecomputedAuth {
    type TransparentAuth = TransparentAuth;
    type SaplingAuth = sapling::bundle::Authorized;
    type OrchardAuth = orchard::bundle::Authorized;
}

pub(crate) fn inspect(
    tx: Transaction,
    context: Option<Context>,
    mined_height: Option<(&'static str, BlockHeight)>,
) {
    eprintln!("Zcash transaction");
    eprintln!(" - ID: {}", tx.txid());
    if let Some((chain, height)) = mined_height {
        eprintln!(" - Mined in {chain} block {height}");
    }
    eprintln!(" - Version: {:?}", tx.version());
    match tx.version() {
        // TODO: If pre-v5 and no branch ID provided in context, disable signature checks.
        TxVersion::Sprout(_) | TxVersion::V3 | TxVersion::V4 => (),
        TxVersion::V5 => {
            eprintln!(" - Consensus branch ID: {:?}", tx.consensus_branch_id());
        }
    }

    let is_coinbase = is_coinbase(&tx);
    let height = if is_coinbase {
        eprintln!(" - Coinbase");
        extract_height_from_coinbase(&tx)
    } else {
        None
    };

    let transparent_coins = match (
        tx.transparent_bundle().is_some_and(|b| !b.vin.is_empty()),
        context.as_ref().and_then(|ctx| ctx.transparent_coins()),
    ) {
        (true, coins) => coins,
        (false, Some(_)) => {
            eprintln!("⚠️  Context was given \"transparentcoins\" but this transaction has no transparent inputs");
            Some(vec![])
        }
        (false, None) => Some(vec![]),
    };

    let sighash_params = transparent_coins.as_ref().map(|coins| {
        let f_transparent = MapTransparent {
            auth: TransparentAuth {
                all_prev_outputs: coins.clone(),
            },
        };

        // We don't have tx.clone()
        let mut buf = vec![];
        tx.write(&mut buf).unwrap();
        let tx = Transaction::read(&buf[..], tx.consensus_branch_id()).unwrap();

        let tx: TransactionData<PrecomputedAuth> =
            tx.into_data().map_authorization(f_transparent, (), ());
        let txid_parts = tx.digest(TxIdDigester);
        (tx, txid_parts)
    });

    let common_sighash = sighash_params
        .as_ref()
        .map(|(tx, txid_parts)| signature_hash(tx, &SignableInput::Shielded, txid_parts));
    if let Some(sighash) = &common_sighash {
        if tx.sprout_bundle().is_some()
            || tx.sapling_bundle().is_some()
            || tx.orchard_bundle().is_some()
        {
            eprintln!(
                " - Sighash for shielded signatures: {}",
                hex::encode(sighash.as_ref()),
            );
        }
    }

    if let Some(bundle) = tx.transparent_bundle() {
        assert!(!(bundle.vin.is_empty() && bundle.vout.is_empty()));
        if !(bundle.vin.is_empty() || is_coinbase) {
            eprintln!(" - {} transparent input(s)", bundle.vin.len());
            if let Some(coins) = &transparent_coins {
                if bundle.vin.len() != coins.len() {
                    eprintln!("  ⚠️  \"transparentcoins\" has length {}", coins.len());
                }

                let (tx, txid_parts) = sighash_params.as_ref().unwrap();
                let ctx = Secp256k1::<VerifyOnly>::gen_new();

                for (i, (txin, coin)) in bundle.vin.iter().zip(coins).enumerate() {
                    eprintln!(
                        "   - prevout: txid {}, index {}",
                        TxId::from_bytes(*txin.prevout().hash()),
                        txin.prevout().n()
                    );
                    match script::PubKey::parse(&coin.script_pubkey().0)
                        .ok()
                        .as_ref()
                        .and_then(solver::standard)
                    {
                        Some(solver::ScriptKind::PubKeyHash { hash }) => {
                            let addr = TransparentAddress::PublicKeyHash(hash);
                            // Format is PushData(sig || [hash_type]) || PushData(pubkey)
                            // where [x] encodes a single byte.
                            let (sig_and_type, pubkey) = script::Sig::parse(&txin.script_sig().0)
                                .ok()
                                .as_ref()
                                .and_then(|script_sig| match script_sig.0.as_slice() {
                                    [sig_and_type, pubkey] => {
                                        Some((sig_and_type.value(), pubkey.value()))
                                    }
                                    _ => None,
                                })
                                .unzip();

                            if let Some(((&hash_type, sig), pubkey_bytes)) = sig_and_type
                                .as_ref()
                                .and_then(|b| b.split_last())
                                .zip(pubkey)
                            {
                                let sig = secp256k1::ecdsa::Signature::from_der(sig);
                                let hash_type = SighashType::parse(hash_type);
                                let pubkey = secp256k1::PublicKey::from_slice(&pubkey_bytes);

                                if let Err(e) = sig {
                                    eprintln!(
                                        "    ⚠️  Txin {i} has invalid signature encoding: {e}"
                                    );
                                }
                                if hash_type.is_none() {
                                    eprintln!("    ⚠️  Txin {i} has invalid sighash type");
                                }
                                if let Err(e) = pubkey {
                                    eprintln!("    ⚠️  Txin {i} has invalid pubkey encoding: {e}");
                                }
                                if let (Ok(sig), Some(hash_type), Ok(pubkey)) =
                                    (sig, hash_type, pubkey)
                                {
                                    if TransparentAddress::from_pubkey(&pubkey) != addr {
                                        eprintln!("    ⚠️  Txin {i} pubkey does not match coin's script_pubkey");
                                    }

                                    let sighash = signature_hash(
                                        tx,
                                        &SignableInput::Transparent(
                                            ::transparent::sighash::SignableInput::from_parts(
                                                hash_type,
                                                i,
                                                // For P2PKH these are the same.
                                                coin.script_pubkey(),
                                                coin.script_pubkey(),
                                                coin.value(),
                                            ),
                                        ),
                                        txid_parts,
                                    );
                                    let msg =
                                        secp256k1::Message::from_digest_slice(sighash.as_ref())
                                            .expect("signature_hash() returns correct length");

                                    if let Err(e) = ctx.verify_ecdsa(&msg, &sig, &pubkey) {
                                        eprintln!("    ⚠️  Spend {i} is invalid: {e}");
                                        eprintln!(
                                            "     - sighash is {}",
                                            hex::encode(sighash.as_ref())
                                        );
                                        eprintln!("     - pubkey is {}", hex::encode(pubkey_bytes));
                                    }
                                }
                            }
                        }
                        // TODO: Check P2SH structure.
                        Some(solver::ScriptKind::ScriptHash { .. }) => {
                            eprintln!("  🔎 \"transparentcoins\"[{i}] is a P2SH coin.");
                        }
                        Some(solver::ScriptKind::MultiSig { required, pubkeys }) => {
                            eprintln!("  🔎 \"transparentcoins\"[{i}] is a direct (non-P2SH) {required}-of-{} multi-sig coin.", pubkeys.len());
                        }
                        Some(solver::ScriptKind::NullData { data }) => {
                            eprintln!("  🔎 \"transparentcoins\"[{i}] is a null data output with {} PushDatas.", data.len());
                        }
                        Some(solver::ScriptKind::PubKey { .. }) => {
                            eprintln!("  🔎 \"transparentcoins\"[{i}] is a P2PK (not P2PKH) coin.");
                        }
                        // TODO: Check arbitrary scripts.
                        None => {
                            eprintln!(
                                "  🔎 \"transparentcoins\"[{i}] has a script we can't check yet."
                            );
                        }
                    }
                }
            } else {
                eprintln!(
                    "  🔎 To check transparent inputs, add \"transparentcoins\" array to context."
                );
                eprintln!("     The following transparent inputs are required: ");
                for txin in &bundle.vin {
                    eprintln!(
                        "     - txid {}, index {}",
                        TxId::from_bytes(*txin.prevout().hash()),
                        txin.prevout().n()
                    )
                }
            }
        }
        if !bundle.vout.is_empty() {
            eprintln!(" - {} transparent output(s)", bundle.vout.len());
            for txout in &bundle.vout {
                eprintln!(
                    "     - {}",
                    serde_json::to_string(&ZTxOut::from(txout.clone())).unwrap()
                );
            }
        }
    }

    if let Some(bundle) = tx.sprout_bundle() {
        eprintln!(" - {} Sprout JoinSplit(s)", bundle.joinsplits.len());

        // TODO: Verify Sprout proofs once we can access the Sprout bundle parts.

        match ed25519_zebra::VerificationKey::try_from(bundle.joinsplit_pubkey) {
            Err(e) => eprintln!("  ⚠️  joinsplitPubkey is invalid: {e}"),
            Ok(vk) => {
                if let Some(sighash) = &common_sighash {
                    if let Err(e) = vk.verify(
                        &ed25519_zebra::Signature::from(bundle.joinsplit_sig),
                        sighash.as_ref(),
                    ) {
                        eprintln!("  ⚠️  joinsplitSig is invalid: {e}");
                    }
                } else {
                    eprintln!(
                        "  🔎 To check Sprout JoinSplit(s), add \"transparentcoins\" array to context"
                    );
                }
            }
        }
    }

    if let Some(bundle) = tx.sapling_bundle() {
        assert!(!(bundle.shielded_spends().is_empty() && bundle.shielded_outputs().is_empty()));

        // TODO: Separate into checking proofs, signatures, and other structural details.
        let mut ctx = SaplingVerificationContext::new();

        if !bundle.shielded_spends().is_empty() {
            eprintln!(" - {} Sapling Spend(s)", bundle.shielded_spends().len());
            if let Some(sighash) = &common_sighash {
                for (i, spend) in bundle.shielded_spends().iter().enumerate() {
                    if !ctx.check_spend(
                        spend.cv(),
                        *spend.anchor(),
                        &spend.nullifier().0,
                        *spend.rk(),
                        sighash.as_ref(),
                        *spend.spend_auth_sig(),
                        groth16::Proof::read(&spend.zkproof()[..]).unwrap(),
                        &GROTH16_PARAMS.spend_vk,
                    ) {
                        eprintln!("  ⚠️  Spend {i} is invalid");
                    }
                }
            } else {
                eprintln!(
                    "  🔎 To check Sapling Spend(s), add \"transparentcoins\" array to context"
                );
            }
        }

        if !bundle.shielded_outputs().is_empty() {
            eprintln!(" - {} Sapling Output(s)", bundle.shielded_outputs().len());
            for (i, output) in bundle.shielded_outputs().iter().enumerate() {
                if is_coinbase {
                    if let Some((params, addr_net)) = context
                        .as_ref()
                        .and_then(|ctx| ctx.network().zip(ctx.addr_network()))
                    {
                        if let Some((note, addr, memo)) = try_output_recovery_with_ovk(
                            &SaplingDomain::new(sapling_serialization::zip212_enforcement(
                                &params,
                                height.unwrap(),
                            )),
                            &sapling::keys::OutgoingViewingKey([0; 32]),
                            output,
                            output.cv(),
                            output.out_ciphertext(),
                        ) {
                            if note.value().inner() == 0 {
                                eprintln!("   - Output {i} (dummy output):");
                            } else {
                                let zaddr = ZcashAddress::from_sapling(addr_net, addr.to_bytes());

                                eprintln!("   - Output {i}:");
                                eprintln!("     - {zaddr}");
                                eprintln!("     - {}", render_value(note.value().inner()));
                            }
                            let memo = MemoBytes::from_bytes(&memo).expect("correct length");
                            eprintln!("     - {}", render_memo(memo));
                        } else {
                            eprintln!("  ⚠️  Output {i} is not recoverable with the all-zeros OVK");
                        }
                    } else {
                        eprintln!("  🔎 To check Sapling coinbase rules, add \"network\" to context (either \"main\" or \"test\")");
                    }
                }

                if !ctx.check_output(
                    output.cv(),
                    *output.cmu(),
                    jubjub::ExtendedPoint::from_bytes(&output.ephemeral_key().0).unwrap(),
                    groth16::Proof::read(&output.zkproof()[..]).unwrap(),
                    &GROTH16_PARAMS.output_vk,
                ) {
                    eprintln!("  ⚠️  Output {i} is invalid");
                }
            }
        }

        if let Some(sighash) = &common_sighash {
            if !ctx.final_check(
                *bundle.value_balance(),
                sighash.as_ref(),
                bundle.authorization().binding_sig,
            ) {
                eprintln!("⚠️  Sapling bindingSig is invalid");
            }
        } else {
            eprintln!("🔎 To check Sapling bindingSig, add \"transparentcoins\" array to context");
        }
    }

    if let Some(bundle) = tx.orchard_bundle() {
        eprintln!(" - {} Orchard Action(s)", bundle.actions().len());

        // Orchard nullifiers must not be duplicated within a transaction.
        let mut nullifiers = HashMap::<[u8; 32], Vec<usize>>::default();
        for (i, action) in bundle.actions().iter().enumerate() {
            nullifiers
                .entry(action.nullifier().to_bytes())
                .or_insert_with(Vec::new)
                .push(i);
        }
        for (_, indices) in nullifiers {
            if indices.len() > 1 {
                eprintln!("⚠️  Nullifier is duplicated between actions {indices:?}");
            }
        }

        if is_coinbase {
            // All coinbase outputs must be decryptable with the all-zeroes OVK.
            for (i, action) in bundle.actions().iter().enumerate() {
                let ovk = orchard::keys::OutgoingViewingKey::from([0u8; 32]);
                if let Some((note, addr, memo)) = try_output_recovery_with_ovk(
                    &OrchardDomain::for_action(action),
                    &ovk,
                    action,
                    action.cv_net(),
                    &action.encrypted_note().out_ciphertext,
                ) {
                    if note.value().inner() == 0 {
                        eprintln!("   - Output {i} (dummy output):");
                    } else {
                        eprintln!("   - Output {i}:");

                        if let Some(net) = context.as_ref().and_then(|ctx| ctx.addr_network()) {
                            assert_eq!(note.recipient(), addr);
                            // Construct a single-receiver UA.
                            let zaddr = ZcashAddress::from_unified(
                                net,
                                unified::Address::try_from_items(vec![unified::Receiver::Orchard(
                                    addr.to_raw_address_bytes(),
                                )])
                                .unwrap(),
                            );
                            eprintln!("     - {zaddr}");
                        } else {
                            eprintln!("    🔎 To show recipient address, add \"network\" to context (either \"main\" or \"test\")");
                        }

                        eprintln!("     - {}", render_value(note.value().inner()));
                    }
                    eprintln!(
                        "     - {}",
                        render_memo(MemoBytes::from_bytes(&memo).unwrap())
                    );
                } else {
                    eprintln!("  ⚠️  Output {i} is not recoverable with the all-zeros OVK");
                }
            }
        }

        if let Some(sighash) = &common_sighash {
            for (i, action) in bundle.actions().iter().enumerate() {
                if let Err(e) = action.rk().verify(sighash.as_ref(), action.authorization()) {
                    eprintln!("  ⚠️  Action {i} spendAuthSig is invalid: {e}");
                }
            }
        } else {
            eprintln!(
                "🔎 To check Orchard Action signatures, add \"transparentcoins\" array to context"
            );
        }

        if let Err(e) = bundle.verify_proof(&ORCHARD_VK) {
            eprintln!("⚠️  Orchard proof is invalid: {e:?}");
        }
    }
}
