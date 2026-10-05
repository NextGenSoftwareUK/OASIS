use clap::Subcommand;

pub(crate) mod balance;
pub(crate) mod derive_path;
pub(crate) mod display_mnemonic;
pub(crate) mod enhance;
pub(crate) mod gen_account;
pub(crate) mod gen_addr;
pub(crate) mod import_ufvk;
pub(crate) mod init;
pub(crate) mod init_fvk;
pub(crate) mod list_accounts;
pub(crate) mod list_addresses;
pub(crate) mod list_tx;
pub(crate) mod list_unspent;
pub(crate) mod propose;
pub(crate) mod reset;
pub(crate) mod send;
pub(crate) mod shield;
pub(crate) mod sync;
pub(crate) mod tree;
pub(crate) mod upgrade;

#[derive(Debug, Subcommand)]
pub(crate) enum Command {
    /// Initialise a new light wallet
    Init(init::Command),

    /// Initialise a new view-only light wallet
    InitFvk(init_fvk::Command),

    /// Decrypt and display the wallet's mnemonic recovery phrase, if any.
    DisplayMnemonic(display_mnemonic::Command),

    /// Reset an existing light wallet (does not preserve imported UFVKs)
    Reset(reset::Command),

    /// Import a UFVK
    ImportUfvk(import_ufvk::Command),

    /// Upgrade an existing light wallet
    Upgrade(upgrade::Command),

    /// Scan the chain and sync the wallet
    Sync(sync::Command),

    /// Ensure all transactions have full data available
    Enhance(enhance::Command),

    /// Get the balance in the wallet
    Balance(balance::Command),

    /// Generate a new account in the wallet
    GenerateAccount(gen_account::Command),

    /// List the accounts in the wallet
    ListAccounts(list_accounts::Command),

    /// Generate a new address for an account in the wallet
    GenerateAddress(gen_addr::Command),

    /// List the addresses for an account in the wallet
    ListAddresses(list_addresses::Command),

    /// Derive key material at a particular path below the wallet seed
    DerivePath(derive_path::Command),

    /// List the transactions in the wallet
    ListTx(list_tx::Command),

    /// List the unspent notes in the wallet
    ListUnspent(list_unspent::Command),

    /// Shield transparent funds received by the wallet
    Shield(shield::Command),

    /// Propose a transfer of funds to the given address and display the proposal
    Propose(propose::Command),

    /// Send funds to the given address
    Send(send::Command),

    /// Commands that operate directly on the note commitment trees
    #[command(subcommand)]
    Tree(tree::Command),
}
