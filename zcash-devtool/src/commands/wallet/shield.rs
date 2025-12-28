use std::collections::HashSet;
use std::num::NonZeroUsize;

use anyhow::anyhow;
use clap::Args;
use rand::rngs::OsRng;
use secrecy::ExposeSecret;
use transparent::address::TransparentAddress;
use uuid::Uuid;

use zcash_client_backend::{
    data_api::{
        wallet::{
            create_proposed_transactions, input_selection::GreedyInputSelector, propose_shielding,
            ConfirmationsPolicy, SpendingKeys,
        },
        Account, WalletRead,
    },
    fees::{standard::MultiOutputChangeStrategy, DustOutputPolicy, SplitPolicy, StandardFeeRule},
    proto::service,
    wallet::OvkPolicy,
};
use zcash_client_sqlite::{util::SystemClock, WalletDb};
use zcash_keys::{encoding::AddressCodec, keys::UnifiedSpendingKey};
use zcash_proofs::prover::LocalTxProver;
use zcash_protocol::{value::Zatoshis, ShieldedProtocol};

use crate::{
    commands::select_account,
    config::WalletConfig,
    data::get_db_paths,
    error,
    remote::{tor_client, Servers},
};

// Options accepted for the `shield` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// The UUID of the account to shield funds in
    account_id: Option<Uuid>,

    /// The addresses for which to shield funds.
    #[arg(short, long, action = clap::ArgAction::Append)]
    address: Vec<String>,

    /// age identity file to decrypt the mnemonic phrase with
    #[arg(short, long)]
    identity: String,

    /// The server to shield via (default is \"ecc\")
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
        let derivation = account.source().key_derivation().ok_or(anyhow!(
            "Cannot spend from view-only accounts; did you mean to use `pczt shield` instead?"
        ))?;

        let addresses = self
            .address
            .into_iter()
            .map(|address| TransparentAddress::decode(&params, &address))
            .collect::<Result<HashSet<_>, _>>()?;

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

        let txids = create_proposed_transactions(
            &mut db_data,
            &params,
            &prover,
            &prover,
            &SpendingKeys::from_unified_spending_key(usk),
            OvkPolicy::Sender,
            &proposal,
        )
        .map_err(error::Error::Shield)?;

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
