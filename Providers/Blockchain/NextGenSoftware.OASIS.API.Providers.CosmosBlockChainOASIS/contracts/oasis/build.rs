use std::{env, path::PathBuf};

fn main() {
    println!("cargo:rerun-if-changed=cosmwasm-host-imports.txt");
    if env::var("CARGO_CFG_TARGET_ARCH").as_deref() == Ok("wasm32") {
        // CosmWasm 2.2 predates Rust 1.96's explicit wasm-import requirement.
        // These are required env imports implemented by wasmvm, not missing
        // application functions. Unknown unresolved symbols must still fail.
        let allowed = PathBuf::from(env::var("CARGO_MANIFEST_DIR").unwrap())
            .join("cosmwasm-host-imports.txt");
        println!("cargo:rustc-link-arg=--allow-undefined-file={}", allowed.display());
    }
}
