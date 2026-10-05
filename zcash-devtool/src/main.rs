//! An example light client wallet based on the `zcash_client_sqlite` crate.
//!
//! This is **NOT IMPLEMENTED SECURELY**, and it is not written to be efficient or usable!
//! It is only intended to show the overall light client workflow using this crate.

use std::env;
use std::io;
use std::sync::atomic::{AtomicUsize, Ordering};

use clap::{Parser, Subcommand};
use iso_currency::Currency;
use tracing_subscriber::{layer::SubscriberExt, Layer};

mod commands;
mod config;
mod data;
mod error;
mod remote;
mod ui;

#[cfg(feature = "tui")]
#[allow(dead_code)]
mod tui;

fn parse_hex(data: &str) -> Result<Vec<u8>, hex::FromHexError> {
    hex::decode(data)
}

fn parse_currency(data: &str) -> Result<Currency, String> {
    Currency::from_code(data).ok_or_else(|| format!("Invalid currency '{data}'"))
}

#[derive(Debug, Parser)]
pub(crate) struct MyOptions {
    #[command(subcommand)]
    pub(crate) command: Option<Command>,
}

#[derive(Debug, Subcommand)]
pub(crate) enum Command {
    /// Inspect Zcash-related data
    Inspect(commands::inspect::Command),

    /// Manipulate a local wallet backed by `zcash_client_sqlite`
    Wallet(commands::Wallet),

    /// Manipulate multisig accounts
    Zip48(commands::Zip48),

    /// Send funds using PCZTs
    Pczt(commands::Pczt),

    /// Emulate a Keystone device
    #[cfg(feature = "pczt-qr")]
    Keystone(commands::Keystone),

    CreateMultisigAddress(commands::create_multisig_address::Command),
}

fn main() -> Result<(), anyhow::Error> {
    let opts = MyOptions::parse();

    let level_filter = env::var("RUST_LOG").unwrap_or_else(|_| "info".to_owned());

    #[cfg(not(feature = "tui"))]
    let tui_logger: Option<()> = None;
    #[cfg(feature = "tui")]
    let tui_logger = match opts.command {
        Some(Command::Wallet(commands::Wallet {
            command:
                commands::wallet::Command::Sync(commands::wallet::sync::Command {
                    defrag: true, ..
                }),
            ..
        })) => {
            tui_logger::init_logger(level_filter.parse().unwrap())?;
            Some(tui_logger::TuiTracingSubscriberLayer)
        }
        #[cfg(feature = "pczt-qr")]
        Some(Command::Pczt(commands::Pczt {
            command: commands::pczt::Command::ToQr(commands::pczt::qr::Send { tui: true, .. }),
            ..
        })) => {
            tui_logger::init_logger(level_filter.parse().unwrap())?;
            Some(tui_logger::TuiTracingSubscriberLayer)
        }
        _ => None,
    };

    let stdout_logger = if tui_logger.is_none() {
        let filter = tracing_subscriber::EnvFilter::from(level_filter);
        Some(
            tracing_subscriber::fmt::layer()
                .with_writer(io::stderr)
                .with_filter(filter),
        )
    } else {
        None
    };

    let subscriber = tracing_subscriber::registry().with(stdout_logger);
    #[cfg(feature = "tui")]
    let subscriber = subscriber.with(tui_logger);
    tracing::subscriber::set_global_default(subscriber).unwrap();

    rayon::ThreadPoolBuilder::new()
        .thread_name(|i| format!("zec-rayon-{i}"))
        .build_global()
        .expect("Only initialized once");

    let runtime = tokio::runtime::Builder::new_multi_thread()
        .enable_all()
        .thread_name_fn(|| {
            static ATOMIC_ID: AtomicUsize = AtomicUsize::new(0);
            let id = ATOMIC_ID.fetch_add(1, Ordering::SeqCst);
            format!("zec-tokio-{id}")
        })
        .build()?;

    runtime.block_on(async {
        #[cfg(feature = "tui")]
        let tui = tui::Tui::new()?.tick_rate(4.0).frame_rate(30.0);

        let shutdown = ShutdownListener::new();

        let Some(cmd) = opts.command else {
            return Ok(());
        };

        match cmd {
            Command::Inspect(command) => command.run().await,
            Command::Wallet(commands::Wallet {
                wallet_dir,
                command,
            }) => match command {
                commands::wallet::Command::Init(command) => command.run(wallet_dir).await,
                commands::wallet::Command::InitFvk(command) => command.run(wallet_dir).await,
                commands::wallet::Command::DisplayMnemonic(command) => command.run(wallet_dir),
                commands::wallet::Command::Reset(command) => command.run(wallet_dir).await,
                commands::wallet::Command::ImportUfvk(command) => command.run(wallet_dir).await,
                commands::wallet::Command::Upgrade(command) => command.run(wallet_dir),
                commands::wallet::Command::Sync(command) => {
                    command
                        .run(
                            shutdown,
                            wallet_dir,
                            #[cfg(feature = "tui")]
                            tui,
                        )
                        .await
                }
                commands::wallet::Command::Enhance(command) => command.run(wallet_dir).await,
                commands::wallet::Command::Balance(command) => command.run(wallet_dir).await,
                commands::wallet::Command::GenerateAccount(command) => {
                    command.run(wallet_dir).await
                }
                commands::wallet::Command::ListAccounts(command) => command.run(wallet_dir),
                commands::wallet::Command::GenerateAddress(command) => command.run(wallet_dir),
                commands::wallet::Command::ListAddresses(command) => command.run(wallet_dir),
                commands::wallet::Command::DerivePath(command) => command.run(wallet_dir),
                commands::wallet::Command::ListTx(command) => command.run(wallet_dir),
                commands::wallet::Command::ListUnspent(command) => command.run(wallet_dir),
                commands::wallet::Command::Shield(command) => command.run(wallet_dir).await,
                commands::wallet::Command::Propose(command) => command.run(wallet_dir).await,
                commands::wallet::Command::Send(command) => command.run(wallet_dir).await,
                commands::wallet::Command::Tree(command) => match command {
                    #[cfg(feature = "tui")]
                    commands::wallet::tree::Command::Explore(command) => {
                        command.run(shutdown, wallet_dir, tui).await
                    }
                    commands::wallet::tree::Command::Fix(command) => command.run(wallet_dir).await,
                },
            },
            Command::Zip48(commands::Zip48 {
                wallet_dir,
                command,
            }) => match command {
                commands::zip48::Command::Init(command) => command.run(wallet_dir),
                commands::zip48::Command::DeriveAccount(command) => command.run(wallet_dir),
                commands::zip48::Command::DeriveAddress(command) => command.run(wallet_dir),
            },
            Command::Pczt(commands::Pczt {
                wallet_dir,
                command,
            }) => match command {
                commands::pczt::Command::Create(command) => command.run(wallet_dir).await,
                commands::pczt::Command::Shield(command) => command.run(wallet_dir).await,
                commands::pczt::Command::CreateManual(command) => command.run(wallet_dir).await,
                commands::pczt::Command::Inspect(command) => command.run(wallet_dir).await,
                commands::pczt::Command::UpdateWithDerivation(command) => {
                    command.run(wallet_dir).await
                }
                commands::pczt::Command::Redact(command) => command.run().await,
                commands::pczt::Command::Prove(command) => command.run(wallet_dir).await,
                commands::pczt::Command::Sign(command) => command.run(wallet_dir).await,
                commands::pczt::Command::Combine(command) => command.run().await,
                commands::pczt::Command::Send(command) => command.run(wallet_dir).await,
                commands::pczt::Command::SendWithoutStoring(command) => {
                    command.run(wallet_dir).await
                }
                #[cfg(feature = "pczt-qr")]
                commands::pczt::Command::ToQr(command) => {
                    command
                        .run(
                            shutdown,
                            #[cfg(feature = "tui")]
                            tui,
                        )
                        .await
                }
                #[cfg(feature = "pczt-qr")]
                commands::pczt::Command::FromQr(command) => command.run(shutdown).await,
            },
            #[cfg(feature = "pczt-qr")]
            Command::Keystone(commands::Keystone {
                wallet_dir,
                command,
            }) => match command {
                commands::keystone::Command::Enroll(command) => {
                    command.run(shutdown, wallet_dir).await
                }
            },

            Command::CreateMultisigAddress(command) => command.run(),
        }
    })
}

struct ShutdownListener {
    signal_rx: tokio::sync::oneshot::Receiver<()>,
    #[cfg(feature = "tui")]
    tui_tx: Option<tokio::sync::oneshot::Sender<()>>,
    #[cfg(feature = "tui")]
    tui_rx: tokio::sync::oneshot::Receiver<()>,
}

impl ShutdownListener {
    fn new() -> Self {
        let (signal_tx, signal_rx) = tokio::sync::oneshot::channel();
        tokio::spawn(async move {
            if let Err(e) = tokio::signal::ctrl_c().await {
                tracing::error!("Failed to listen for Ctrl-C event: {}", e);
            }
            let _ = signal_tx.send(());
        });

        #[cfg(feature = "tui")]
        let (tui_tx, tui_rx) = tokio::sync::oneshot::channel();

        Self {
            signal_rx,
            #[cfg(feature = "tui")]
            tui_tx: Some(tui_tx),
            #[cfg(feature = "tui")]
            tui_rx,
        }
    }

    #[cfg(feature = "tui")]
    fn tui_quit_signal(&mut self) -> tokio::sync::oneshot::Sender<()> {
        self.tui_tx.take().expect("should only call this once")
    }

    fn requested(&mut self) -> bool {
        const NOT_TRIGGERED: Result<(), tokio::sync::oneshot::error::TryRecvError> =
            Err(tokio::sync::oneshot::error::TryRecvError::Empty);

        let signal = self.signal_rx.try_recv();

        #[cfg(feature = "tui")]
        let tui = self.tui_rx.try_recv();
        #[cfg(not(feature = "tui"))]
        let tui = NOT_TRIGGERED;

        match (signal, tui) {
            (NOT_TRIGGERED, NOT_TRIGGERED) => false,
            // If either has been triggered, then a shutdown has been requested.
            _ => true,
        }
    }
}
