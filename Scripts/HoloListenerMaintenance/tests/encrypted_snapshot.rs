use anyhow::{Context, Result};
use holochain_conductor_api::state::{
    AppInterfaceConfig, AppInterfaceId, ConductorState, ConductorStateTag,
};
use holochain_state::{
    conductor::ConductorStore,
    data::{Conductor, DbKey, HolochainDataConfig, open_db},
};
use holochain_types::{
    app::{AppManifest, InstalledApp, InstalledAppCommon, InstalledAppMap},
    prelude::{AgentPubKey, Timestamp},
    websocket::AllowedOrigins,
};
use serde_json::json;
use sha2::{Digest, Sha256};
use std::{
    fs,
    io::Write,
    process::{Command, Stdio},
    sync::{Arc, Mutex},
};

const PASSPHRASE: &[u8] = b"disposable encrypted fixture";

#[tokio::test]
async fn prunes_only_unused_interfaces_from_an_encrypted_copy() -> Result<()> {
    let temporary = tempfile::tempdir()?;
    let snapshot = temporary.path().join("snapshot");
    let output = temporary.path().join("output");
    fs::create_dir(&snapshot)?;
    let passphrase = Arc::new(Mutex::new(PASSPHRASE.to_vec().into()));
    let key = DbKey::generate(passphrase.clone()).await?;
    fs::write(snapshot.join("db.key"), &key.locked)?;
    let db = open_db(
        &snapshot,
        Conductor,
        HolochainDataConfig {
            key: Some(key),
            ..Default::default()
        },
    )
    .await?;
    let pool = db.pool().clone();
    let store = ConductorStore::new(db);
    let manifest: AppManifest =
        serde_json::from_value(json!({"manifest_version":"0","name":"oasis","roles":[]}))?;
    let app = InstalledAppCommon::new(
        "oasis",
        AgentPubKey::from_raw_36(vec![7; 36]),
        [],
        manifest,
        Timestamp::from_micros(1),
    )?;
    let expected_app = app.clone();
    let apps = InstalledAppMap::from_iter([("oasis".into(), InstalledApp::new_fresh(app))]);
    let mut listeners = (12_000..12_136)
        .map(|port| {
            let bound = (port == 12_000).then(|| "oasis".into());
            (
                AppInterfaceId::new(port),
                AppInterfaceConfig::websocket(port, None, AllowedOrigins::Any, bound),
            )
        })
        .collect::<std::collections::HashMap<_, _>>();
    let subscription = serde_json::from_value(json!({
        "installed_app_id":"oasis",
        "filters":{"Exclude":{}}
    }))?;
    listeners
        .get_mut(&AppInterfaceId::new(12_001))
        .context("missing subscribed fixture listener")?
        .signal_subscriptions
        .insert("oasis".into(), subscription);
    store
        .update_state(
            |_| -> Result<_> {
                Ok((
                    ConductorState::from_parts(
                        ConductorStateTag("fixture".into()),
                        apps,
                        listeners,
                    ),
                    (),
                ))
            },
            "",
            &Default::default(),
        )
        .await?;
    pool.close().await;
    let original = fs::read(snapshot.join("conductor.db"))?;
    let hash = format!("{:x}", Sha256::digest(&original));

    let mut child = Command::new(env!("CARGO_BIN_EXE_oasis-holo-listener-maintenance"))
        .arg("--piped")
        .arg(&snapshot)
        .arg(hash)
        .arg(&output)
        .arg("134")
        .stdin(Stdio::piped())
        .spawn()?;
    let mut stdin = child
        .stdin
        .take()
        .context("missing fixture passphrase pipe")?;
    stdin.write_all(PASSPHRASE)?;
    stdin.write_all(b"\n")?;
    drop(stdin);
    assert!(child.wait()?.success());
    assert_eq!(fs::read(snapshot.join("conductor.db"))?, original);

    let key = DbKey::load(fs::read_to_string(snapshot.join("db.key"))?, passphrase).await?;
    let result = open_db(
        &output,
        Conductor,
        HolochainDataConfig {
            key: Some(key),
            ..Default::default()
        },
    )
    .await?;
    let pool = result.pool().clone();
    let state = ConductorStore::new(result)
        .as_read()
        .load_state()
        .await?
        .context("migrated state was missing")?;
    assert_eq!(state.tag().0.as_ref(), "fixture");
    assert_eq!(&*state.installed_apps()["oasis"], &expected_app);
    assert_eq!(state.app_interfaces.len(), 2);
    let canonical = &state.app_interfaces[&AppInterfaceId::new(12_000)];
    assert_eq!(canonical.installed_app_id.as_deref(), Some("oasis"));
    let subscribed = &state.app_interfaces[&AppInterfaceId::new(12_001)];
    assert!(subscribed.installed_app_id.is_none());
    assert!(subscribed.signal_subscriptions.contains_key("oasis"));
    pool.close().await;
    temporary.close()?;
    Ok(())
}
