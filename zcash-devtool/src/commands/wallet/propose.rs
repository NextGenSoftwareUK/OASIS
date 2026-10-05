use std::{num::NonZeroUsize, str::FromStr};

use anyhow::anyhow;
use clap::Args;
use rand::rngs::OsRng;
use uuid::Uuid;
use zcash_address::ZcashAddress;
use zcash_client_backend::{
    data_api::{
        wallet::{input_selection::GreedyInputSelector, propose_transfer, ConfirmationsPolicy},
        Account as _,
    },
    fees::{zip317::MultiOutputChangeStrategy, DustOutputPolicy, SplitPolicy, StandardFeeRule},
};
use zcash_client_sqlite::{util::SystemClock, WalletDb};
use zcash_protocol::{value::Zatoshis, ShieldedProtocol};
use zip321::{Payment, TransactionRequest};

use crate::{commands::select_account, config::get_wallet_network, data::get_db_paths, error};

// Options accepted for the `propose` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// The UUID of the account to send funds from
    account_id: Option<Uuid>,

    /// The recipient's Unified, Sapling or transparent address
    #[arg(long)]
    address: String,

    /// The amount in zatoshis
    #[arg(long)]
    value: u64,

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
        let params = get_wallet_network(wallet_dir.as_ref())?;

        let (_, db_data) = get_db_paths(wallet_dir.as_ref());
        let mut db_data = WalletDb::for_path(db_data, params, SystemClock, OsRng)?;
        let account = select_account(&db_data, self.account_id)?;

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

        let request = TransactionRequest::new(vec![Payment::without_memo(
            ZcashAddress::from_str(&self.address).map_err(|_| error::Error::InvalidRecipient)?,
            Zatoshis::from_u64(self.value).map_err(|_| error::Error::InvalidAmount)?,
        )])
        .map_err(error::Error::from)?;

        let proposal = propose_transfer(
            &mut db_data,
            &params,
            account.id(),
            &input_selector,
            &change_strategy,
            request,
            ConfirmationsPolicy::default(),
        )
        .map_err(error::Error::from)?;

        // Display the proposal
        println!("Proposal: {proposal:#?}");

        Ok(())
    }
}
