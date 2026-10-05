#![allow(deprecated)]
use std::{num::NonZeroUsize, str::FromStr};

use anyhow::anyhow;
use clap::Args;
use rand::rngs::OsRng;
use secrecy::ExposeSecret;
use uuid::Uuid;

use zcash_address::ZcashAddress;
use zcash_client_backend::{
    data_api::{
        wallet::{
            create_proposed_transactions, input_selection::GreedyInputSelector, propose_transfer,
            ConfirmationsPolicy, SpendingKeys,
        },
        Account, WalletRead,
    },
    fees::{standard::MultiOutputChangeStrategy, DustOutputPolicy, SplitPolicy, StandardFeeRule},
    proto::service,
    wallet::OvkPolicy,
};
use zcash_client_sqlite::{util::SystemClock, WalletDb};
use zcash_keys::keys::UnifiedSpendingKey;
use zcash_proofs::prover::LocalTxProver;
use zcash_protocol::{
    memo::{Memo, MemoBytes},
    value::Zatoshis,
    ShieldedProtocol,
};
use zip321::{Payment, TransactionRequest};

use crate::{
    commands::select_account,
    config::WalletConfig,
    data::get_db_paths,
    error,
    remote::{tor_client, Servers},
};

// Options accepted for the `send` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// The UUID of the account to send funds from
    account_id: Option<Uuid>,

    /// age identity file to decrypt the mnemonic phrase with
    #[arg(short, long)]
    identity: String,

    /// The recipient's Unified, Sapling or transparent address
    #[arg(long)]
    address: String,

    /// The amount in zatoshis
    #[arg(long)]
    value: u64,

    /// A memo to be sent to the recipient.
    #[arg(long)]
    memo: Option<String>,

    /// The server to send via (default is \"ecc\")
    #[arg(short, long)]
    #[arg(default_value = "ecc", value_parser = Servers::parse)]
    server: Servers,

    /// Disable connections via TOR
    #[arg(long)]
    disable_tor: bool,

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
        let mut config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        let (_, db_data) = get_db_paths(wallet_dir.as_ref());
        let mut db_data = WalletDb::for_path(db_data, params, SystemClock, OsRng)?;
        let account = select_account(&db_data, self.account_id)?;
        let derivation = account
            .source()
            .key_derivation()
            .ok_or(anyhow!("Cannot spend from view-only accounts"))?;

        // Decrypt the mnemonic to access the seed.
        let identities = age::IdentityFile::from_file(self.identity)?.into_identities()?;
        let seed = config
            .decrypt_seed(identities.iter().map(|i| i.as_ref() as _))?
            .ok_or(anyhow!("Seed must be present to enable sending"))?;

        let usk = UnifiedSpendingKey::from_seed(
            &params,
            seed.expose_secret(),
            derivation.account_index(),
        )
        .map_err(error::Error::from)?;

        let server = self.server.pick(params)?;
        let mut client = if self.disable_tor {
            server.connect_direct().await?
        } else {
            server.connect(|| tor_client(wallet_dir.as_ref())).await?
        };

        // Create the transaction.
        println!("Creating transaction...");
        let prover = LocalTxProver::bundled();
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

        let payment = Payment::new(
            ZcashAddress::from_str(&self.address).map_err(|_| error::Error::InvalidRecipient)?,
            Zatoshis::from_u64(self.value).map_err(|_| error::Error::InvalidAmount)?,
            self.memo
                .as_ref()
                .map(|m| Memo::from_str(m))
                .transpose()
                .map_err(|_| error::Error::InvalidMemo)?
                .map(MemoBytes::from),
            None,
            None,
            vec![],
        )
        .ok_or_else(|| error::Error::TransparentMemo(0))?;
        let request = TransactionRequest::new(vec![payment]).map_err(error::Error::from)?;

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

        let txids = create_proposed_transactions(
            &mut db_data,
            &params,
            &prover,
            &prover,
            &SpendingKeys::from_unified_spending_key(usk),
            OvkPolicy::Sender,
            &proposal,
        )
        .map_err(error::Error::from)?;

        if txids.len() > 1 {
            return Err(anyhow!(
                "Multi-transaction proposals are not yet supported."
            ));
        }

        let txid = *txids.first();

        // Send the transaction.
        println!("Sending transaction...");
        let (txid, raw_tx) = db_data
            .get_transaction(txid)?
            .map(|tx| {
                let mut raw_tx = service::RawTransaction::default();
                tx.write(&mut raw_tx.data).unwrap();
                (tx.txid(), raw_tx)
            })
            .ok_or(anyhow!("Transaction not found for id {:?}", txid))?;
        let response = client.send_transaction(raw_tx).await?.into_inner();

        if response.error_code != 0 {
            Err(error::Error::SendFailed {
                code: response.error_code,
                reason: response.error_message,
            }
            .into())
        } else {
            println!("{txid}");
            Ok(())
        }
    }
}
