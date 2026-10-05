use anyhow::anyhow;
use clap::Args;
use secrecy::ExposeSecret;
use transparent::zip48;

use crate::config::WalletConfig;

// Options accepted for the `zip48 derive-account` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    /// age identity file to decrypt the mnemonic phrase with
    #[arg(short, long)]
    identity: String,

    /// ZIP 48 account index to derive.
    #[arg(long)]
    hd_account_index: Option<u32>,
}

impl Command {
    pub(crate) fn run(self, wallet_dir: Option<String>) -> anyhow::Result<()> {
        let mut config = WalletConfig::read(wallet_dir.as_ref())?;
        let params = config.network();

        let account = zip32::AccountId::try_from(self.hd_account_index.unwrap_or(0))?;

        // Decrypt the mnemonic to access the seed.
        let identities = age::IdentityFile::from_file(self.identity)?.into_identities()?;
        let seed = config
            .decrypt_seed(identities.iter().map(|i| i.as_ref() as _))?
            .ok_or(anyhow!(
                "Seed must be present to enable generating a new account"
            ))?;

        let privkey = zip48::AccountPrivKey::from_seed(&params, seed.expose_secret(), account)
            .map_err(|e| anyhow!("{e}"))?;

        let pubkey = privkey.to_account_pubkey();

        println!("{}", pubkey.key_info_expression(&params));

        Ok(())
    }
}
