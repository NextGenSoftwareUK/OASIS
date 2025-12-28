use anyhow::anyhow;
use clap::Args;
use rand::rngs::OsRng;
use zcash_client_sqlite::{
    chain::init::init_blockmeta_db,
    util::SystemClock,
    wallet::init::{init_wallet_db, WalletMigrationError},
    FsBlockDb, WalletDb,
};

use crate::{
    config::{get_wallet_network, get_wallet_seed},
    data::get_db_paths,
    error,
};

// Options accepted for the `upgrade` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// age identity file to decrypt the mnemonic phrase with
    #[arg(short, long)]
    identity: Option<String>,
}

impl Command {
    pub(crate) fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let params = get_wallet_network(wallet_dir.as_ref())?;

        let (fsblockdb_root, db_data) = get_db_paths(wallet_dir.as_ref());
        let mut db_cache = FsBlockDb::for_path(fsblockdb_root).map_err(error::Error::from)?;
        let mut db_data = WalletDb::for_path(db_data, params, SystemClock, OsRng)?;

        init_blockmeta_db(&mut db_cache)?;

        if let Err(e) = init_wallet_db(&mut db_data, None) {
            if matches!(&e, schemerz::MigratorError::Migration {
                error, ..
            } if matches!(error, WalletMigrationError::SeedRequired))
            {
                let identities = age::IdentityFile::from_file(
                    self.identity
                        .ok_or(anyhow!("Identity file required to decrypt mnemonic phrase"))?,
                )?
                .into_identities()?;

                init_wallet_db(
                    &mut db_data,
                    get_wallet_seed(wallet_dir, identities.iter().map(|i| i.as_ref() as _))?,
                )?;
            } else {
                return Err(e.into());
            }
        }

        println!("Wallet successfully upgraded!");
        Ok(())
    }
}
