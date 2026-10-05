use anyhow::anyhow;
use clap::Args;
use rand::rngs::OsRng;
use zcash_client_backend::{
    data_api::{Account, WalletRead},
    proto::service,
};
use zcash_client_sqlite::{util::SystemClock, WalletDb};

use crate::{
    config::WalletConfig,
    data::{erase_wallet_state, get_db_paths},
    remote::{tor_client, Servers},
};

// Options accepted for the `reset` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// age identity file to decrypt the mnemonic phrase with
    #[arg(short, long)]
    identity: String,

    /// The server to re-initialize with (default is \"ecc\")
    #[arg(short, long)]
    #[arg(default_value = "ecc", value_parser = Servers::parse)]
    server: Servers,

    /// Disable connections via TOR
    #[arg(long)]
    disable_tor: bool,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        // Load the wallet network, seed, and birthday from disk.
        let mut config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        // Connect to the client (for re-initializing the wallet).
        let server = self.server.pick(params)?;
        let mut client = if self.disable_tor {
            server.connect_direct().await?
        } else {
            server.connect(|| tor_client(wallet_dir.as_ref())).await?
        };

        // Get the current chain height (for the wallet's recover-until height).
        let chain_tip = client
            .get_latest_block(service::ChainSpec::default())
            .await?
            .into_inner()
            .height
            .try_into()
            .expect("block heights must fit into u32");

        // Get the account name and key source to preserve them.
        let (account_name, key_source) = {
            let (_, db_data) = get_db_paths(wallet_dir.as_ref());
            let db_data = WalletDb::for_path(db_data, params, SystemClock, OsRng)?;

            let account_id = *db_data
                .get_account_ids()?
                .first()
                .ok_or(anyhow!("Wallet has no accounts"))?;

            let account = db_data.get_account(account_id)?.expect("exists");
            (
                account.name().map(String::from),
                account.source().key_source().map(String::from),
            )
        };

        let birthday =
            super::init::Command::get_wallet_birthday(client, config.birthday(), Some(chain_tip))
                .await?;

        // Erase the wallet state (excluding key material).
        erase_wallet_state(wallet_dir.as_ref()).await;

        // Decrypt the mnemonic to access the seed.
        let identities = age::IdentityFile::from_file(self.identity)?.into_identities()?;

        let seed = config
            .decrypt_seed(identities.iter().map(|i| i.as_ref() as _))?
            .ok_or(anyhow!("Seed is required for database reset"))?;

        // Re-initialize the wallet state.
        super::init::Command::init_dbs(
            params,
            wallet_dir.as_ref(),
            account_name.as_deref().unwrap_or(""),
            &seed,
            birthday,
            key_source.as_deref(),
        )
    }
}
