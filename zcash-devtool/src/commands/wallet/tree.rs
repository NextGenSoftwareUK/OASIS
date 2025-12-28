use clap::Subcommand;

#[cfg(feature = "tui")]
pub(crate) mod explore;

pub(crate) mod fix;

#[derive(Debug, Subcommand)]
pub(crate) enum Command {
    /// Explore a tree
    #[cfg(feature = "tui")]
    Explore(explore::Command),
    Fix(fix::Command),
}
