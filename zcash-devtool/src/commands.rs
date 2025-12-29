use anyhow::anyhow;
use clap::Args;
use uuid::Uuid;
use zcash_client_backend::data_api::WalletRead;
use zcash_client_sqlite::AccountUuid;

pub(crate) mod create_multisig_address;
pub(crate) mod inspect;
pub(crate) mod pczt;
pub(crate) mod wallet;
pub(crate) mod zip48;

#[cfg(feature = "pczt-qr")]
pub(crate) mod keystone;

#[derive(Debug, Args)]
pub(crate) struct Wallet {
    /// Path to the wallet directory
    #[arg(short, long)]
    pub(crate) wallet_dir: Option<String>,

    #[command(subcommand)]
    pub(crate) command: wallet::Command,
}

#[derive(Debug, Args)]
pub(crate) struct Zip48 {
    /// Path to the wallet directory
    #[arg(short, long)]
    pub(crate) wallet_dir: Option<String>,

    #[command(subcommand)]
    pub(crate) command: zip48::Command,
}

#[derive(Debug, Args)]
pub(crate) struct Pczt {
    /// Path to a wallet directory
    #[arg(short, long)]
    pub(crate) wallet_dir: Option<String>,

    #[command(subcommand)]
    pub(crate) command: pczt::Command,
}

#[cfg(feature = "pczt-qr")]
#[derive(Debug, Args)]
pub(crate) struct Keystone {
    /// Path to a wallet directory
    #[arg(short, long)]
    pub(crate) wallet_dir: Option<String>,

    #[command(subcommand)]
    pub(crate) command: keystone::Command,
}

pub(crate) fn select_account<DbT: WalletRead<AccountId = AccountUuid>>(
    db_data: &DbT,
    account_uuid: Option<Uuid>,
) -> Result<DbT::Account, anyhow::Error>
where
    DbT::Error: std::error::Error + Sync + Send + 'static,
{
    let account_id = match account_uuid {
        Some(uuid) => Ok(AccountUuid::from_uuid(uuid)),
        None => {
            let account_ids = db_data.get_account_ids()?;
            match &account_ids[..] {
                [] => Err(anyhow!("Wallet contains no accounts.")),
                [account_id] => Ok(*account_id),
                _ => Err(anyhow!(
                    "More than one account is available; please specify the account UUID."
                )),
            }
        }
    }?;

    db_data
        .get_account(account_id)?
        .ok_or(anyhow!("Account missing: {:?}", account_id))
}
