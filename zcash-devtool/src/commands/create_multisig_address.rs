//! Command for managing multisig addresses in Zcash Devtool.
//!
//! Accepts a threshold number of signatures to be required by the generated multisig address and a list
//! of public keys in hex format, separated by commas, that should be able to participate in signing spends from the multi-sig
//! address produced by this command.
//!
//! Generates a multi-sig P2SH address that can be used to send and receive funds, requiring the specified threshold number of
//! signatures to authorize spending from the generated address.

// TODO: Add a wallet subcommand that accepts known p2pkh addresses and which adds the generated multisig address to the wallet.

use clap::Args;
use secp256k1::PublicKey;
use sha2::{Digest, Sha256};

use transparent::address::TransparentAddress;
use zcash_keys::encoding::AddressCodec;
use zcash_protocol::consensus::Network;
use zcash_script::{
    pattern::check_multisig,
    script::{self, Evaluable},
};

/// Maximum size of a script element in bytes
// TODO: Move this constant to `zcash_transparent` if it's needed.
const MAX_SCRIPT_ELEMENT_SIZE: usize = 520;

/// Commands for managing multisig addresses.
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// Add a nrequired-to-sign transparent multisignature address to the wallet

    /// A threshold `k` value indicating the number of signatures required to spend from the address
    #[clap(short, long, required = true)]
    threshold: u8,

    /// A list of comma-separated hex-encoded public keys.
    /// Must contain at least the threshold number of keys.
    #[clap(short, long, required = true, value_delimiter = ',')]
    pub_keys: Vec<PublicKey>,

    /// The network to use for the multisig address.
    #[clap(short, long, default_value = "test")]
    #[arg(value_parser = crate::data::Network::parse)]
    network: crate::data::Network,
}

impl Command {
    pub(crate) fn run(self) -> anyhow::Result<()> {
        let Self {
            threshold,
            pub_keys,
            network,
        } = self;

        let (multisig_script, addr) = multisig_script(threshold, pub_keys)?;
        let addr = addr.encode(&Network::from(network));
        println!("Created multisig address: {addr}");
        println!("Redeem script: {}", hex::encode(multisig_script.to_bytes()));

        Ok(())
    }
}

fn multisig_script(
    threshold: u8,
    pub_keys: Vec<PublicKey>,
) -> anyhow::Result<(script::PubKey, TransparentAddress)> {
    validate_args(threshold, &pub_keys)?;

    let pks = pub_keys.iter().map(|pk| pk.serialize()).collect::<Vec<_>>();
    let pks = pks.iter().map(|pk| pk.as_slice()).collect::<Vec<_>>();
    let multisig_redeem_script = script::Component(check_multisig(threshold, &pks, false)?);

    if multisig_redeem_script.0.len() > MAX_SCRIPT_ELEMENT_SIZE {
        return Err(anyhow::anyhow!(
            "the multisig script is too large, it must be less than {MAX_SCRIPT_ELEMENT_SIZE} bytes",
        ));
    }

    let script_id = ripemd::Ripemd160::digest(Sha256::digest(multisig_redeem_script.to_bytes()));
    let address = TransparentAddress::ScriptHash(script_id.into());

    Ok((multisig_redeem_script, address))
}

fn validate_args(threshold: u8, pub_keys: &[PublicKey]) -> anyhow::Result<()> {
    if threshold < 1 {
        return Err(anyhow::anyhow!("a multisignature address must require at least one key to redeem, threshold must be at least 1"));
    }

    if pub_keys.len() < threshold as usize {
        return Err(anyhow::anyhow!(
            "not enough keys supplied, (got {} keys, but need at least {threshold} to redeem)",
            pub_keys.len()
        ));
    }

    // TODO: Find out if this is correct (in zcashd, the max script size is 520 bytes, but the max number of public keys is 16, 3 + 16*34 == 547 > 520).
    if pub_keys.len() > 16 {
        return Err(anyhow::anyhow!(
            "number of addresses involved in the multisignature address creation > 16\nreduce the number"
        ));
    }

    Ok(())
}
