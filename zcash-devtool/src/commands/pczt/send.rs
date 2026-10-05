use anyhow::anyhow;
use clap::Args;
use pczt::Pczt;
use rand::rngs::OsRng;
use tokio::io::{stdin, AsyncReadExt};
use zcash_client_backend::{
    data_api::{wallet::extract_and_store_transaction_from_pczt, WalletRead},
    proto::service,
};
use zcash_client_sqlite::{util::SystemClock, WalletDb};
use zcash_proofs::prover::LocalTxProver;

use crate::{
    config::WalletConfig,
    data::get_db_paths,
    error,
    remote::{tor_client, Servers},
};

// Options accepted for the `pczt send` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// The server to send via (default is \"ecc\")
    #[arg(short, long)]
    #[arg(default_value = "ecc", value_parser = Servers::parse)]
    server: Servers,

    /// Disable connections via TOR
    #[arg(long)]
    disable_tor: bool,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        let (_, db_data) = get_db_paths(wallet_dir.as_ref());
        let mut db_data = WalletDb::for_path(db_data, params, SystemClock, OsRng)?;

        let server = self.server.pick(params)?;
        let mut client = if self.disable_tor {
            server.connect_direct().await?
        } else {
            server.connect(|| tor_client(wallet_dir.as_ref())).await?
        };

        let mut buf = vec![];
        stdin().read_to_end(&mut buf).await?;

        let pczt = Pczt::parse(&buf).map_err(|e| anyhow!("Failed to read PCZT: {:?}", e))?;

        let prover = LocalTxProver::bundled();
        let (spend_vk, output_vk) = prover.verifying_keys();

        let txid = extract_and_store_transaction_from_pczt::<_, ()>(
            &mut db_data,
            pczt,
            Some((&spend_vk, &output_vk)),
            Some(&orchard::circuit::VerifyingKey::build()),
        )
        .map_err(|e| anyhow!("Failed to extract and store transaction from PCZT: {:?}", e))?;

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
