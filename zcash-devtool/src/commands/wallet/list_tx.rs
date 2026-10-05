use anyhow::{anyhow, bail};
use clap::Args;
use rusqlite::{named_params, Connection};
use time::macros::format_description;
use uuid::Uuid;

use zcash_protocol::{
    consensus::BlockHeight,
    memo::{Memo, MemoBytes},
    value::{ZatBalance, Zatoshis},
    PoolType, TxId,
};

use crate::{data::get_db_paths, ui::format_zec};

// Options accepted for the `list-tx` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// The UUID of the account for which to get the list of transactions. If omitted, transactions
    /// transactions from all accounts will be returned.
    account_id: Option<Uuid>,

    /// The output mode to use. Options are "text" and "csv". Using "csv" output will produce a CSV
    /// with one record per output row. Defaults to "text".
    #[arg(short, long)]
    mode: Option<String>,
}

#[derive(Clone, Copy, Debug)]
enum ListMode {
    Text,
    Csv,
}

impl ListMode {
    fn parse(value: &str) -> Result<Self, ()> {
        match value {
            "text" => Ok(ListMode::Text),
            "csv" => Ok(ListMode::Csv),
            _ => Err(()),
        }
    }
}

impl Command {
    pub(crate) fn run(self, wallet_dir: Option<String>) -> anyhow::Result<()> {
        let (_, db_data) = get_db_paths(wallet_dir);
        let mode = self
            .mode
            .as_ref()
            .map_or(Ok(ListMode::Text), |s| ListMode::parse(s.as_str()))
            .map_err(|_| anyhow::Error::msg("Invalid printing mode"))?;

        let conn = Connection::open(db_data)?;
        rusqlite::vtab::array::load_module(&conn)?;

        let mut stmt_txs = conn.prepare(
            "SELECT mined_height,
                txid,
                expiry_height,
                account_balance_delta,
                fee_paid,
                sent_note_count,
                received_note_count,
                memo_count,
                block_time,
                expired_unmined,
                -- Fallback order for transaction history ordering:
                COALESCE(
                    -- Block height the transaction was mined at (if mined and known).
                    mined_height,
                    -- Expiry height for the transaction (if non-zero, which is always the
                    -- case for transactions we create).
                    CASE WHEN expiry_height == 0 THEN NULL ELSE expiry_height END
                    -- Mempool height (i.e. chain height + 1, so it appears most recently
                    -- in history). We represent this with NULL.
                ) AS sort_height
            FROM v_transactions
            WHERE (:account_uuid IS NULL OR account_uuid = :account_uuid)
            ORDER BY sort_height ASC NULLS LAST",
        )?;

        let mut stmt_outputs = conn.prepare(
            "SELECT
                output_pool,
                output_index,
                from_account_uuid,
                fa.name AS from_account_name,
                to_account_uuid,
                ta.name AS to_account_name,
                to_address,
                value,
                is_change,
                memo
             FROM v_tx_outputs
             LEFT OUTER JOIN accounts fa ON from_account_uuid = fa.uuid
             LEFT OUTER JOIN accounts ta ON to_account_uuid = ta.uuid
             WHERE txid = :txid",
        )?;

        match mode {
            ListMode::Text => {
                println!("Transactions:");
            }
            ListMode::Csv => {
                println!(
                    "Date,Action,Symbol,Volume,Currency,Account,Total,Price,Fee,FeeCurrency,Memo"
                );
            }
        }
        for row in stmt_txs.query_and_then(
            named_params! {":account_uuid": self.account_id },
            |row| -> anyhow::Result<_> {
                let txid = row.get::<_, Vec<u8>>("txid")?;

                let tx_outputs = stmt_outputs
                    .query_and_then(named_params![":txid": txid], |out_row| {
                        let from_account_name: Option<String> = out_row.get("from_account_name")?;
                        let to_account_name: Option<String> = out_row.get("to_account_name")?;
                        WalletTxOutput::new(
                            out_row.get("output_pool")?,
                            out_row.get("output_index")?,
                            out_row
                                .get::<_, Option<Uuid>>("from_account_uuid")?
                                .map(|uuid| (uuid, from_account_name)),
                            out_row
                                .get::<_, Option<Uuid>>("to_account_uuid")?
                                .map(|uuid| (uuid, to_account_name)),
                            out_row.get("to_address")?,
                            out_row.get("value")?,
                            out_row.get("is_change")?,
                            out_row.get("memo")?,
                        )
                    })?
                    .collect::<Result<Vec<_>, _>>()?;

                WalletTx::from_parts(
                    row.get("mined_height")?,
                    txid,
                    row.get("expiry_height")?,
                    row.get("account_balance_delta")?,
                    row.get("fee_paid")?,
                    row.get("sent_note_count")?,
                    row.get("received_note_count")?,
                    row.get("memo_count")?,
                    row.get("block_time")?,
                    row.get("expired_unmined")?,
                    tx_outputs,
                )
            },
        )? {
            let tx = row?;
            tx.print(mode)?;
        }

        Ok(())
    }
}

struct WalletTxOutput {
    pool: PoolType,
    output_index: u32,
    from_account: Option<(Uuid, Option<String>)>,
    to_account: Option<(Uuid, Option<String>)>,
    to_address: Option<String>,
    value: Zatoshis,
    is_change: bool,
    memo: Option<Memo>,
}

impl WalletTxOutput {
    fn parse_pool_code(pool_code: i64) -> Option<PoolType> {
        match pool_code {
            0 => Some(PoolType::Transparent),
            2 => Some(PoolType::SAPLING),
            3 => Some(PoolType::ORCHARD),
            _ => None,
        }
    }

    #[allow(clippy::too_many_arguments)]
    fn new(
        pool_code: i64,
        output_index: u32,
        from_account: Option<(Uuid, Option<String>)>,
        to_account: Option<(Uuid, Option<String>)>,
        to_address: Option<String>,
        value: i64,
        is_change: bool,
        memo: Option<Vec<u8>>,
    ) -> Result<Self, anyhow::Error> {
        Ok(Self {
            pool: Self::parse_pool_code(pool_code)
                .ok_or(anyhow!("Pool code not recognized: {}", pool_code))?,
            output_index,
            from_account,
            to_account,
            to_address,
            value: Zatoshis::from_nonnegative_i64(value)?,
            is_change,
            memo: memo
                .as_ref()
                .map(|b| MemoBytes::from_bytes(b).and_then(Memo::try_from))
                .transpose()
                .map_err(|e| anyhow!("{}", e))?,
        })
    }

    fn print_text(&self) {
        println!("  Output {} ({})", self.output_index, self.pool);
        println!(
            "    Value: {}{}",
            format_zec(self.value),
            if self.is_change {
                " (Change)"
            } else if self.from_account.is_some() && self.to_account.is_some() {
                " (Wallet Internal Transfer)"
            } else {
                ""
            }
        );

        if self.from_account != self.to_account {
            if let Some((account_id, account_name)) = &self.to_account {
                let name = account_name
                    .as_ref()
                    .map_or("".to_string(), |n| format!(" ({n})"));
                println!("    Received by account: {account_id}{name}");
            }
            if let Some((account_id, account_name)) = &self.from_account {
                let name = account_name
                    .as_ref()
                    .map_or("".to_string(), |n| format!(" ({n})"));
                println!("    Sent from account: {account_id}{name}");
            }
        }

        if let Some(addr) = &self.to_address {
            println!("    To: {addr}");
        }

        if let Some(memo) = &self.memo {
            println!("    Memo: {memo:?}");
        }
    }

    fn print_csv(&self, context: &WalletTx) -> Result<(), anyhow::Error> {
        if self.is_change {
            //neither send nor receive, skip
            return Ok(());
        }

        let format = format_description!(
            "[year]-[month]-[day] [hour]:[minute]:[second] [offset_hour sign:mandatory]:[offset_minute]:[offset_second]"
        );

        if let Some(action) = match (&self.from_account, &self.to_account) {
            (Some(_), Some(_)) => None, // wallet-internal transfer, skip
            (None, None) => {
                bail!("we should not encounter a state where neither source nor destination are known");
            }
            (None, Some(_)) => Some("RECEIVE"),
            (Some(_), None) => Some("SEND"),
        } {
            let date = context
                .block_time
                .map(time::OffsetDateTime::from_unix_timestamp)
                .transpose()?
                .map_or(Ok("".to_string()), |t| t.format(format))?;
            let symbol = "ZEC";
            let volume = format_zec(self.value);
            let currency = "USD";
            let (account_id, account_name) = self
                .to_account
                .as_ref()
                .or(self.from_account.as_ref())
                .unwrap();
            let aname_str = account_name
                .as_ref()
                .map_or("".to_string(), |n| format!(" ({n})"));
            let total = "";
            let price = "";
            let fee = context.fee_paid.map(format_zec).unwrap_or("".to_string());
            let fee_currency = "ZEC";
            let memo = self.memo.as_ref().map_or("".to_string(), |m| match m {
                Memo::Empty => "".to_string(),
                Memo::Text(text_memo) => text_memo.to_string(),
                Memo::Future(_) => "".to_string(),
                Memo::Arbitrary(_) => "".to_string(),
            });
            println!("{date},{action},{symbol},{volume},{currency},{account_id}{aname_str},{total},{price},{fee},{fee_currency},{memo}");
        }

        Ok(())
    }
}

struct WalletTx {
    mined_height: Option<BlockHeight>,
    txid: TxId,
    expiry_height: Option<BlockHeight>,
    account_balance_delta: ZatBalance,
    fee_paid: Option<Zatoshis>,
    sent_note_count: usize,
    received_note_count: usize,
    memo_count: usize,
    block_time: Option<i64>,
    expired_unmined: bool,
    outputs: Vec<WalletTxOutput>,
}

impl WalletTx {
    #[allow(clippy::too_many_arguments)]
    fn from_parts(
        mined_height: Option<u32>,
        txid: Vec<u8>,
        expiry_height: Option<u32>,
        account_balance_delta: i64,
        fee_paid: Option<u64>,
        sent_note_count: usize,
        received_note_count: usize,
        memo_count: usize,
        block_time: Option<i64>,
        expired_unmined: bool,
        outputs: Vec<WalletTxOutput>,
    ) -> anyhow::Result<Self> {
        Ok(WalletTx {
            mined_height: mined_height.map(BlockHeight::from_u32),
            txid: TxId::from_bytes(txid.try_into().map_err(|_| anyhow!("Invalid TxId"))?),
            expiry_height: expiry_height.map(BlockHeight::from_u32),
            account_balance_delta: ZatBalance::from_i64(account_balance_delta)
                .map_err(|_| anyhow!("Amount out of range"))?,
            fee_paid: fee_paid
                .map(|v| Zatoshis::from_u64(v).map_err(|_| anyhow!("Fee out of range")))
                .transpose()?,
            sent_note_count,
            received_note_count,
            memo_count,
            block_time,
            expired_unmined,
            outputs,
        })
    }

    fn print(&self, mode: ListMode) -> Result<(), anyhow::Error> {
        match mode {
            ListMode::Text => {
                self.print_text();
                Ok(())
            }
            ListMode::Csv => self.print_csv(),
        }
    }

    fn print_text(&self) {
        let height_to_str = |height: Option<BlockHeight>, def: &str| {
            height.map(|h| h.to_string()).unwrap_or(def.to_owned())
        };

        println!("{}", self.txid);
        if let Some((height, block_time)) = self.mined_height.zip(self.block_time) {
            match time::OffsetDateTime::from_unix_timestamp(block_time) {
                Ok(block_time) => println!("     Mined: {height} ({block_time})"),
                Err(e) => println!("     Mined: {height} (invalid block time: {e})"),
            }
        } else {
            println!(
                "  {} (expiry height: {})",
                if self.expired_unmined {
                    " Expired"
                } else {
                    " Unmined"
                },
                height_to_str(self.expiry_height, "Unknown"),
            );
        }
        println!("    Amount: {}", format_zec(self.account_balance_delta));
        println!(
            "  Fee paid: {}",
            self.fee_paid
                .map(format_zec)
                .as_deref()
                .unwrap_or("Unknown"),
        );
        println!(
            "  Sent {} notes, received {} notes, {} memos",
            self.sent_note_count, self.received_note_count, self.memo_count,
        );
        for output in &self.outputs {
            output.print_text()
        }
    }

    fn print_csv(&self) -> Result<(), anyhow::Error> {
        for output in &self.outputs {
            output.print_csv(self)?;
        }

        Ok(())
    }
}
