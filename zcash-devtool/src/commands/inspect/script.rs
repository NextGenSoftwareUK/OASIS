use zcash_script::{
    opcode::{push_value::LargeValue, PossiblyBad, PushValue},
    script, solver, Opcode,
};

pub(crate) fn inspect(script: script::FromChain) {
    match script.refine().ok().as_ref().and_then(solver::standard) {
        Some(script) => match script {
            solver::ScriptKind::PubKeyHash { .. } => {
                eprintln!("Pay-to-PubKey-Hash (P2PKH) transparent script");
            }
            solver::ScriptKind::ScriptHash { .. } => {
                eprintln!("Pay-to-Script-Hash (P2SH) transparent script");
            }
            solver::ScriptKind::MultiSig { required, pubkeys } => {
                eprintln!(
                    "{required}-of-{} Pay-to-MultiSig (P2MS) transparent script",
                    pubkeys.len()
                );
                for pubkey in pubkeys {
                    eprintln!("- {}", hex::encode(&pubkey));
                }
            }
            solver::ScriptKind::NullData { .. } => {
                eprintln!("Null data (OP_RETURN) transparent script")
            }
            solver::ScriptKind::PubKey { .. } => {
                eprintln!("Pay-to-PubKey (P2PK) transparent script")
            }
        },
        None => {
            eprintln!("Non-standard transparent script");
            for pb in script.0 {
                match pb {
                    PossiblyBad::Good(opcode) => match opcode {
                        Opcode::PushValue(push_value) => match push_value {
                            PushValue::SmallValue(small_value) => eprintln!("- {small_value:?}"),
                            PushValue::LargeValue(large_value) => match large_value {
                                LargeValue::PushdataBytelength(data) => {
                                    eprintln!("- PUSHDATA {}", hex::encode(data))
                                }
                                LargeValue::OP_PUSHDATA1(data) => {
                                    eprintln!("- OP_PUSHDATA1 {}", hex::encode(data))
                                }
                                LargeValue::OP_PUSHDATA2(data) => {
                                    eprintln!("- OP_PUSHDATA2 {}", hex::encode(data))
                                }
                                LargeValue::OP_PUSHDATA4(data) => {
                                    eprintln!("- OP_PUSHDATA4 {}", hex::encode(data))
                                }
                            },
                        },
                        Opcode::Control(control) => eprintln!("- {control:?}"),
                        Opcode::Operation(operation) => eprintln!("- {operation:?}"),
                    },
                    PossiblyBad::Bad(bad) => eprintln!("- {bad:?}"),
                }
            }
        }
    }
}
