use clap::Args;
use nonempty::NonEmpty;
use rand::rngs::OsRng;
use zcash_client_backend::data_api::scanning::ScanPriority;
use zcash_client_sqlite::{util::SystemClock, WalletDb};

use crate::{config::get_wallet_network, data::get_db_paths};

#[derive(Debug, Args)]
pub(crate) struct Command {}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let params = get_wallet_network(wallet_dir.as_ref())?;
        let (_, db_path) = get_db_paths(wallet_dir.as_ref());
        let mut db_data = WalletDb::for_path(db_path, params, SystemClock, OsRng)?;

        if let Some(corrupt_ranges) = NonEmpty::from_vec(db_data.check_witnesses()?) {
            let corrupt_ranges_len = corrupt_ranges.len();
            for range in corrupt_ranges.iter() {
                eprintln!("Found corrupt witness, requires rescan of range {range:?}");
            }

            db_data.queue_rescans(corrupt_ranges, ScanPriority::FoundNote)?;

            eprintln!("Updated {corrupt_ranges_len} scan ranges");
        } else {
            eprintln!("No corrupt witnesses found in the tree");
        }

        Ok(())
    }
}
