use std::fs;
use std::io::{self, Write};

use age::secrecy::{ExposeSecret, SecretString};
use bip0039::{Count, English, Mnemonic};
use clap::Args;

use zcash_protocol::consensus::{self, Parameters};

use crate::{
    config::WalletConfig,
    data::{init_dbs, Network},
};

// Options accepted for the `zip48 init` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// age identity file to encrypt the mnemonic phrase to (generated if it doesn't exist)
    #[arg(short, long)]
    identity: String,

    /// Initialise the wallet with a new mnemonic phrase (default is to ask for a phrase)
    #[arg(long, required = false)]
    new: bool,

    /// The network the wallet will be used with: \"test\" or \"main\"
    #[arg(short, long)]
    #[arg(value_parser = Network::parse)]
    network: Network,
}

impl Command {
    pub(crate) fn run(self, wallet_dir: Option<String>) -> Result<(), anyhow::Error> {
        let params = consensus::Network::from(self.network);

        let recipients = if fs::exists(&self.identity)? {
            age::IdentityFile::from_file(self.identity)?.to_recipients()?
        } else {
            eprintln!("Generating a new age identity to encrypt the mnemonic phrase");
            let identity = age::x25519::Identity::generate();
            let recipient = identity.to_public();

            // Write it to the provided path so we have it for next time.
            let mut f = fs::File::create_new(self.identity)?;
            f.write_all(
                format!(
                    "# created: {}\n",
                    chrono::Local::now().to_rfc3339_opts(chrono::SecondsFormat::Secs, true)
                )
                .as_bytes(),
            )?;
            f.write_all(format!("# public key: {recipient}\n").as_bytes())?;
            f.write_all(format!("{}\n", identity.to_string().expose_secret()).as_bytes())?;
            f.flush()?;

            vec![Box::new(recipient) as _]
        };

        // Parse or create the wallet's mnemonic phrase.
        let mnemonic = if self.new {
            eprintln!("Generating a new mnemonic phrase");
            Mnemonic::generate(Count::Words24)
        } else {
            eprintln!("Please enter the mnemonic phrase:");
            let mut buf = String::with_capacity(1024);
            let res = io::stdin().read_line(&mut buf);
            let phrase = SecretString::new(buf.into_boxed_str());
            res?;
            <Mnemonic<English>>::from_phrase(phrase.expose_secret())?
        };

        // Save the wallet keys to disk.
        WalletConfig::init_with_mnemonic(
            wallet_dir.as_ref(),
            recipients.iter().map(|r| r.as_ref() as _),
            &mnemonic,
            params
                .activation_height(consensus::NetworkUpgrade::Nu6)
                .expect("active"),
            self.network.into(),
        )?;

        // Initialise the block and wallet DBs.
        let _ = init_dbs(params, wallet_dir.as_ref())?;

        Ok(())
    }
}
