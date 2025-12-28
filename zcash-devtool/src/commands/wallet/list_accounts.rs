use clap::Args;
use zcash_client_backend::data_api::{Account, WalletRead};
use zcash_client_sqlite::WalletDb;

use crate::{config::get_wallet_network, data::get_db_paths};

// Options accepted for the `list-accounts` command
#[derive(Debug, Args)]
pub(crate) struct Command {}

impl Command {
    pub(crate) fn run(self, wallet_dir: Option<String>) -> anyhow::Result<()> {
        let params = get_wallet_network(wallet_dir.as_ref())?;
        let (_, db_data) = get_db_paths(wallet_dir.as_ref());
        let db_data = WalletDb::for_path(db_data, params, (), ())?;

        for account_id in db_data.get_account_ids()?.iter() {
            let account = db_data.get_account(*account_id)?.unwrap();

            println!("Account {}", account_id.expose_uuid());
            if let Some(name) = account.name() {
                println!("     Name: {name}");
            }
            println!("     UIVK: {}", account.uivk().encode(&params));
            println!(
                "     UFVK: {}",
                account
                    .ufvk()
                    .map_or("None".to_owned(), |k| k.encode(&params))
            );
            println!("     Source: {:?}", account.source());
        }
        Ok(())
    }
}
