use std::collections::HashSet;
use std::num::NonZeroUsize;

use anyhow::anyhow;
use clap::Args;
use rand::rngs::OsRng;
use tokio::io::{stdout, AsyncWriteExt};
use transparent::address::TransparentAddress;
use uuid::Uuid;
use zcash_client_backend::{
    data_api::{
        wallet::{
            create_pczt_from_proposal, input_selection::GreedyInputSelector, propose_shielding,
            ConfirmationsPolicy,
        },
        Account as _, WalletRead,
    },
    fees::{standard::MultiOutputChangeStrategy, DustOutputPolicy, SplitPolicy, StandardFeeRule},
    wallet::OvkPolicy,
};
use zcash_client_sqlite::{util::SystemClock, WalletDb};
use zcash_keys::encoding::AddressCodec;
use zcash_protocol::{value::Zatoshis, ShieldedProtocol};

use crate::{commands::select_account, config::WalletConfig, data::get_db_paths, error};

// Options accepted for the `pczt shield` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// The UUID of the account to shield funds in
    account_id: Option<Uuid>,

    /// The addresses for which to shield funds.
    #[arg(short, long, action = clap::ArgAction::Append)]
    address: Vec<String>,

    /// Note management: the number of notes to maintain in the wallet
    #[arg(long)]
    #[arg(default_value_t = 4)]
    target_note_count: usize,

    /// Note management: the minimum allowed value for split change amounts
    #[arg(long)]
    #[arg(default_value_t = 10000000)]
    min_split_output_value: u64,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        let (_, db_data) = get_db_paths(wallet_dir.as_ref());
        let mut db_data = WalletDb::for_path(db_data, params, SystemClock, OsRng)?;
        let account = select_account(&db_data, self.account_id)?;

        let addresses = self
            .address
            .into_iter()
            .map(|address| TransparentAddress::decode(&params, &address))
            .collect::<Result<HashSet<_>, _>>()?;

        // Create the PCZT.
        let change_strategy = MultiOutputChangeStrategy::new(
            StandardFeeRule::Zip317,
            None,
            ShieldedProtocol::Orchard,
            DustOutputPolicy::default(),
            SplitPolicy::with_min_output_value(
                NonZeroUsize::new(self.target_note_count)
                    .ok_or(anyhow!("target note count must be nonzero"))?,
                Zatoshis::from_u64(self.min_split_output_value)?,
            ),
        );
        let input_selector = GreedyInputSelector::new();

        // For this dev tool, shield all funds immediately.
        let target_height = match db_data.chain_height()? {
            Some(chain_height) => (chain_height + 1).into(),
            // If we haven't scanned anything, there's nothing to do.
            None => return Ok(()),
        };
        let confirmations_policy = ConfirmationsPolicy::MIN;
        let transparent_balances =
            db_data.get_transparent_balances(account.id(), target_height, confirmations_policy)?;
        let from_addrs = transparent_balances
            .into_keys()
            .filter(|a| addresses.is_empty() || addresses.contains(a))
            .collect::<Vec<_>>();

        let proposal = propose_shielding(
            &mut db_data,
            &params,
            &input_selector,
            &change_strategy,
            Zatoshis::ZERO,
            &from_addrs,
            account.id(),
            confirmations_policy,
        )
        .map_err(error::Error::Shield)?;

        let pczt = create_pczt_from_proposal(
            &mut db_data,
            &params,
            account.id(),
            OvkPolicy::Sender,
            &proposal,
        )
        .map_err(error::Error::Shield)?;

        stdout().write_all(&pczt.serialize()).await?;

        Ok(())
    }
}
