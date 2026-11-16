use anyhow::{Context, Result, ensure};
use holochain_state::{
    conductor::ConductorStore,
    data::{Conductor, DbKey, DbSyncLevel, HolochainDataConfig, open_db},
};
use sha2::{Digest, Sha256};
use std::{env, fs, io, path::Path};

#[tokio::main]
async fn main() -> Result<()> {
    let mut args: Vec<_> = env::args_os().skip(1).collect();
    if args.first().is_some_and(|arg| arg == "--piped") {
        holochain_util::pw::pw_set_piped(true);
        args.remove(0);
    }
    ensure!(
        args.len() == 4,
        "usage: oasis-holo-listener-maintenance [--piped] SNAPSHOT_DIR SNAPSHOT_SHA256 OUTPUT_DIR EXPECTED_REMOVALS"
    );
    let snapshot = Path::new(&args[0]).canonicalize()?;
    let expected_hash = args[1].to_str().context("invalid snapshot fingerprint")?;
    let output = Path::new(&args[2]);
    let expected_removals: usize = args[3]
        .to_str()
        .context("invalid listener count")?
        .parse()
        .context("invalid listener count")?;
    ensure!(expected_removals > 0, "expected removals must be positive");

    let input_db = snapshot.join("conductor.db");
    ensure!(
        !snapshot.join("conductor.db-wal").exists() && !snapshot.join("conductor.db-shm").exists(),
        "input must be a detached, checkpointed conductor snapshot"
    );
    ensure!(
        fs::symlink_metadata(&input_db)?.file_type().is_file(),
        "snapshot database must be a regular file"
    );
    let mut input = fs::File::open(input_db)?;
    let mut hash = Sha256::new();
    io::copy(&mut input, &mut hash)?;
    ensure!(
        format!("{:x}", hash.finalize()) == expected_hash,
        "snapshot fingerprint mismatch"
    );

    let locked_key = fs::read_to_string(snapshot.join("db.key"))?;
    let passphrase = holochain_util::pw::pw_get().context("could not read conductor passphrase")?;
    let key = DbKey::load(locked_key, passphrase)
        .await
        .map_err(|_| anyhow::anyhow!("could not unlock conductor database key"))?;

    fs::create_dir(output).context("output directory must be new")?;
    let output_db = output.join("conductor.db");
    let mut output_file = fs::File::create_new(&output_db)?;
    io::Seek::rewind(&mut input)?;
    let mut copied_hash = Sha256::new();
    io::copy(&mut input, &mut output_file)?;
    output_file.sync_all()?;
    drop(output_file);
    io::copy(&mut fs::File::open(&output_db)?, &mut copied_hash)?;
    ensure!(
        format!("{:x}", copied_hash.finalize()) == expected_hash,
        "copied database fingerprint mismatch"
    );

    let db = open_db(
        output,
        Conductor,
        HolochainDataConfig {
            key: Some(key),
            sync_level: DbSyncLevel::Full,
            max_readers: 1,
        },
    )
    .await?;
    let pool = db.pool().clone();
    let store = ConductorStore::new(db);
    let remaining = store
        .update_state(
            |state| -> Result<_> {
                let mut state = state.context("conductor snapshot has no persisted state")?;
                ensure!(
                    state.installed_apps().contains_key("oasis"),
                    "snapshot is missing the canonical OASIS app"
                );
                let canonical = state
                    .app_interfaces
                    .values()
                    .filter(|interface| interface.installed_app_id.as_deref() == Some("oasis"))
                    .count();
                ensure!(canonical == 1, "expected one canonical OASIS listener");
                let initial = state.app_interfaces.len();
                state.app_interfaces.retain(|_, interface| {
                    interface.installed_app_id.is_some()
                        || !interface.signal_subscriptions.is_empty()
                });
                let remaining = state.app_interfaces.len();
                ensure!(
                    initial - remaining == expected_removals,
                    "actual listener removal count differs from the reviewed count"
                );
                Ok((state, remaining))
            },
            "",
            &Default::default(),
        )
        .await?;
    pool.close().await;
    ensure!(
        !output.join("conductor.db-wal").exists() && !output.join("conductor.db-shm").exists(),
        "output database was not checkpointed"
    );
    println!("Removed {expected_removals} listeners from the copy. {remaining} remain.");
    Ok(())
}
