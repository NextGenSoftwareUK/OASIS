use clap::Subcommand;

pub(crate) mod derive_account;
pub(crate) mod derive_address;
pub(crate) mod init;

#[derive(Debug, Subcommand)]
pub(crate) enum Command {
    /// Initialise a new ZIP 48 multisig wallet
    Init(init::Command),

    /// Derives a ZIP 48 account.
    DeriveAccount(derive_account::Command),

    /// Derives a ZIP 48 address.
    DeriveAddress(derive_address::Command),
}
