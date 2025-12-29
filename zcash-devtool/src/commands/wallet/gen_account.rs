use anyhow::anyhow;
use clap::Args;
use rand::rngs::OsRng;
use zcash_client_backend::{data_api::WalletWrite, proto::service};
use zcash_client_sqlite::{util::SystemClock, WalletDb};

use crate::{
    commands::wallet,
    config::WalletConfig,
    data::get_db_paths,
    remote::{tor_client, Servers},
};

// Options accepted for the `generate-account` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// age identity file to decrypt the mnemonic phrase with
    #[arg(short, long)]
    identity: String,

    /// A name for the account
    #[arg(long)]
    name: String,

    /// The server to initialize with (default is \"ecc\")
    #[arg(short, long)]
    #[arg(default_value = "ecc", value_parser = Servers::parse)]
    server: Servers,

    /// Disable connections via TOR
    #[arg(long)]
    disable_tor: bool,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> anyhow::Result<()> {
        let mut config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        let (_, db_data) = get_db_paths(wallet_dir.as_ref());
        let mut db_data = WalletDb::for_path(db_data, params, SystemClock, OsRng)?;

        // Decrypt the mnemonic to access the seed.
        let identities = age::IdentityFile::from_file(self.identity)?.into_identities()?;
        let seed = config
            .decrypt_seed(identities.iter().map(|i| i.as_ref() as _))?
            .ok_or(anyhow!(
                "Seed must be present to enable generating a new account"
            ))?;

        let server = self.server.pick(params)?;
        let mut client = if self.disable_tor {
            server.connect_direct().await?
        } else {
            server.connect(|| tor_client(wallet_dir.as_ref())).await?
        };

        // Get the current chain height (for the wallet's birthday and/or recover-until height).
        let chain_tip: u32 = client
            .get_latest_block(service::ChainSpec::default())
            .await?
            .into_inner()
            .height
            .try_into()
            .expect("block heights must fit into u32");

        let birthday = wallet::init::Command::get_wallet_birthday(
            client,
            chain_tip.saturating_sub(100).into(),
            None,
        )
        .await?;

        db_data.create_account(&self.name, &seed, &birthday, None)?;

        Ok(())
    }
}
