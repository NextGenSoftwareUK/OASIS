use std::{fs, path::PathBuf};

use anyhow::anyhow;
use clap::Args;
use transparent::{keys::NonHardenedChildIndex, zip48};
use zcash_keys::encoding::AddressCodec;
use zcash_script::script::Evaluable;

use crate::config::WalletConfig;

// Options accepted for the `zip48 derive-address` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// A file containing the key information vector.
    #[clap(short, long)]
    key_info: PathBuf,

    /// A threshold value indicating the number of signatures required to spend from the
    /// address.
    #[clap(long)]
    required: u8,

    /// Whether to generate a change address. Default is to generate an external address.
    #[clap(long, required = false)]
    change: bool,

    /// The index of the address to derive.
    #[clap(short, long)]
    address_index: u32,
}

impl Command {
    pub(crate) fn run(self, wallet_dir: Option<String>) -> anyhow::Result<()> {
        let config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        let key_info = fs::read_to_string(self.key_info)?
            .lines()
            .filter(|line| !line.is_empty())
            .map(|line| {
                zip48::AccountPubKey::parse_key_info_expression(line, &params)
                    .ok_or_else(|| anyhow!("Invalid key info expression: {line}"))
            })
            .collect::<Result<Vec<_>, _>>()?;

        let fvk = zip48::FullViewingKey::standard(self.required, key_info)
            .map_err(|e| anyhow!("{e:?}"))?;

        let scope = if self.change {
            zip32::Scope::Internal
        } else {
            zip32::Scope::External
        };

        let address_index = NonHardenedChildIndex::from_index(self.address_index)
            .ok_or_else(|| anyhow!("Invalid address-index"))?;

        let (addr, redeem_script) = fvk.derive_address(scope, address_index);

        println!(
            "Address {}: {}",
            address_index.index(),
            addr.encode(&params),
        );
        println!("script_pubkey: {}", hex::encode(addr.script().to_bytes()));
        println!("Redeem script: {}", hex::encode(redeem_script.to_bytes()));

        Ok(())
    }
}
