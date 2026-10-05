use anyhow::anyhow;
use clap::Args;

use rand::rngs::OsRng;
use zcash_address::unified::{self, Encoding};
use zcash_client_backend::{
    data_api::{AccountBirthday, AccountPurpose, WalletWrite, Zip32Derivation},
    proto::service,
};
use zcash_client_sqlite::{util::SystemClock, WalletDb};
use zcash_keys::keys::UnifiedFullViewingKey;
use zcash_protocol::consensus;
use zip32::fingerprint::SeedFingerprint;

use crate::{
    data::get_db_paths,
    error, parse_hex,
    remote::{tor_client, Servers},
};

// Options accepted for the `import-ufvk` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// A name for the account
    #[arg(long)]
    name: String,

    /// The Unified Full Viewing Key to import
    ufvk: String,

    /// The UFVK's birthday
    birthday: u32,

    /// Hex encoding of the ZIP 32 fingerprint for the seed from which the UFVK was derived
    #[arg(long)]
    #[arg(value_parser = parse_hex)]
    seed_fingerprint: Option<std::vec::Vec<u8>>,

    /// ZIP 32 account index corresponding to the UFVK
    #[arg(long)]
    hd_account_index: Option<u32>,

    /// The server to initialize with (default is \"ecc\")
    #[arg(short, long)]
    #[arg(default_value = "ecc", value_parser = Servers::parse)]
    server: Servers,

    /// Disable connections via TOR
    #[arg(long)]
    disable_tor: bool,
}

impl Command {
    pub(crate) async fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let (network, ufvk) = unified::Ufvk::decode(&self.ufvk)?;
        let ufvk = UnifiedFullViewingKey::parse(&ufvk).map_err(|e| anyhow!("{e}"))?;

        let params = match network {
            consensus::NetworkType::Main => Ok(consensus::Network::MainNetwork),
            consensus::NetworkType::Test => Ok(consensus::Network::TestNetwork),
            consensus::NetworkType::Regtest => {
                Err(anyhow!("UFVK is for regtest, which is unsupported"))
            }
        }?;

        let (_, db_data) = get_db_paths(wallet_dir.as_ref());
        let mut db_data = WalletDb::for_path(db_data, params, SystemClock, OsRng)?;

        // Construct an `AccountBirthday` for the account's birthday.
        let birthday = {
            // Fetch the tree state corresponding to the last block prior to the wallet's
            // birthday height. NOTE: THIS APPROACH LEAKS THE BIRTHDAY TO THE SERVER!
            let server = self.server.pick(params)?;
            let mut client = if self.disable_tor {
                server.connect_direct().await?
            } else {
                server.connect(|| tor_client(wallet_dir)).await?
            };

            let tip_height = client
                .get_latest_block(service::ChainSpec::default())
                .await?
                .get_ref()
                .height
                .try_into()
                .expect("block heights must fit into u32");

            let request = service::BlockId {
                height: (self.birthday - 1).into(),
                ..Default::default()
            };
            let treestate = client.get_tree_state(request).await?.into_inner();

            AccountBirthday::from_treestate(treestate, Some(tip_height))
                .map_err(error::Error::from)?
        };

        let purpose = match (self.seed_fingerprint, self.hd_account_index) {
            (Some(seed_fingerprint), Some(hd_account_index)) => Ok(AccountPurpose::Spending {
                derivation: Some(Zip32Derivation::new(
                    SeedFingerprint::from_bytes(
                        seed_fingerprint
                            .as_slice()
                            .try_into()
                            .map_err(|_| anyhow!("Incorrect seed_fingerprint length"))?,
                    ),
                    zip32::AccountId::try_from(hd_account_index)?,
                )),
            }),
            (None, None) => Ok(AccountPurpose::ViewOnly),
            _ => Err(anyhow!("Need either both (for spending) or neither (for view-only) of seed_fingerprint and hd_account_index")),
        }?;

        // Import the UFVK.
        db_data.import_account_ufvk(&self.name, &ufvk, &birthday, purpose, None)?;

        Ok(())
    }
}
