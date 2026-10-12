//! Ledger-simulator tests for the OasisStorage blueprint. Run with `scrypto test` (or `cargo test`).

use scrypto_test::prelude::*;

struct Fixture {
    ledger: DefaultLedgerSimulator,
    public_key: Secp256k1PublicKey,
    account: ComponentAddress,
    component: ComponentAddress,
    owner_badge: ResourceAddress,
}

fn setup() -> Fixture {
    let mut ledger = LedgerSimulatorBuilder::new().build();
    let (public_key, _private_key, account) = ledger.new_allocated_account();
    let package = ledger.compile_and_publish(this_package!());

    let receipt = ledger.execute_manifest(
        ManifestBuilder::new()
            .lock_fee_from_faucet()
            .call_function(package, "OasisStorage", "instantiate", manifest_args!())
            .try_deposit_entire_worktop_or_abort(account, None)
            .build(),
        vec![NonFungibleGlobalId::from_public_key(&public_key)],
    );
    let result = receipt.expect_commit_success();
    let component = result.new_component_addresses()[0];
    let owner_badge = result.new_resource_addresses()[0];

    Fixture { ledger, public_key, account, component, owner_badge }
}

impl Fixture {
    /// Calls a write method with a proof of the owner badge (as RadixOASIS does).
    fn write(&mut self, method: &str, args: ManifestArgs) -> TransactionReceipt {
        let manifest = ManifestBuilder::new()
            .lock_fee_from_faucet()
            .create_proof_from_account_of_amount(self.account, self.owner_badge, dec!(1))
            .call_method(self.component, method, args)
            .build();
        self.ledger.execute_manifest(manifest, vec![NonFungibleGlobalId::from_public_key(&self.public_key)])
    }

    /// Calls a write method without the badge.
    fn write_without_badge(&mut self, method: &str, args: ManifestArgs) -> TransactionReceipt {
        let manifest = ManifestBuilder::new()
            .lock_fee_from_faucet()
            .call_method(self.component, method, args)
            .build();
        self.ledger.execute_manifest(manifest, vec![])
    }

    fn read<T: ScryptoDecode>(&mut self, method: &str, args: ManifestArgs) -> T {
        let manifest = ManifestBuilder::new()
            .lock_fee_from_faucet()
            .call_method(self.component, method, args)
            .build();
        let receipt = self.ledger.execute_manifest(manifest, vec![]);
        receipt.expect_commit_success().output(1)
    }
}

const ID: &str = "3f2c6a8e-1d4b-4c1a-9e7f-2b5d8c0a1e93";
const OTHER_ID: &str = "b7a1d2c3-4e5f-4a6b-8c9d-0e1f2a3b4c5d";

#[test]
fn save_and_read_avatar_by_id_username_and_email() {
    let mut f = setup();
    f.write("save_avatar", manifest_args!(ID, "{\"Username\":\"david\"}", "david", "d@example.com"))
        .expect_commit_success();

    assert_eq!(f.read::<Option<String>>("get_avatar", manifest_args!(ID)), Some("{\"Username\":\"david\"}".to_string()));
    assert_eq!(f.read::<Option<String>>("get_avatar_by_username", manifest_args!("david")).is_some(), true);
    assert_eq!(f.read::<Option<String>>("get_avatar_by_email", manifest_args!("d@example.com")).is_some(), true);
    assert_eq!(f.read::<u64>("get_avatar_count", manifest_args!()), 1);
}

#[test]
fn saving_again_updates_and_moves_the_username_index() {
    let mut f = setup();
    f.write("save_avatar", manifest_args!(ID, "{\"v\":1}", "old-name", "d@example.com")).expect_commit_success();
    f.write("save_avatar", manifest_args!(ID, "{\"v\":2}", "new-name", "d@example.com")).expect_commit_success();

    assert_eq!(f.read::<Option<String>>("get_avatar", manifest_args!(ID)), Some("{\"v\":2}".to_string()));
    assert_eq!(f.read::<Option<String>>("get_avatar_by_username", manifest_args!("old-name")), None);
    assert_eq!(f.read::<Option<String>>("get_avatar_by_username", manifest_args!("new-name")), Some("{\"v\":2}".to_string()));
    assert_eq!(f.read::<u64>("get_avatar_count", manifest_args!()), 1);
}

#[test]
fn a_username_used_by_another_avatar_is_rejected() {
    let mut f = setup();
    f.write("save_avatar", manifest_args!(ID, "{}", "david", "a@example.com")).expect_commit_success();
    f.write("save_avatar", manifest_args!(OTHER_ID, "{}", "david", "b@example.com")).expect_commit_failure();
}

#[test]
fn deleting_an_avatar_removes_its_indexes() {
    let mut f = setup();
    f.write("save_avatar", manifest_args!(ID, "{}", "david", "d@example.com")).expect_commit_success();
    f.write("delete_avatar", manifest_args!(ID)).expect_commit_success();

    assert_eq!(f.read::<Option<String>>("get_avatar", manifest_args!(ID)), None);
    assert_eq!(f.read::<Option<String>>("get_avatar_by_username", manifest_args!("david")), None);
    assert_eq!(f.read::<u64>("get_avatar_count", manifest_args!()), 0);
    // The username is free again.
    f.write("save_avatar", manifest_args!(OTHER_ID, "{}", "david", "")).expect_commit_success();
}

#[test]
fn avatar_details_and_holons_round_trip() {
    let mut f = setup();
    f.write("save_avatar_detail", manifest_args!(ID, "{\"Karma\":1}")).expect_commit_success();
    assert_eq!(f.read::<Option<String>>("get_avatar_detail", manifest_args!(ID)), Some("{\"Karma\":1}".to_string()));

    f.write("save_holon", manifest_args!(ID, "{\"Name\":\"h\"}", "pk-1")).expect_commit_success();
    assert_eq!(f.read::<Option<String>>("get_holon_by_provider_key", manifest_args!("pk-1")), Some("{\"Name\":\"h\"}".to_string()));
    f.write("delete_holon", manifest_args!(ID)).expect_commit_success();
    assert_eq!(f.read::<Option<String>>("get_holon_by_provider_key", manifest_args!("pk-1")), None);
    assert_eq!(f.read::<u64>("get_holon_count", manifest_args!()), 0);
}

#[test]
fn writes_without_the_owner_badge_are_rejected() {
    let mut f = setup();
    f.write_without_badge("save_avatar", manifest_args!(ID, "{}", "mallory", "")).expect_commit_failure();
    f.write("save_avatar", manifest_args!(ID, "{}", "david", "")).expect_commit_success();
    f.write_without_badge("delete_avatar", manifest_args!(ID)).expect_commit_failure();
    assert!(f.read::<Option<String>>("get_avatar", manifest_args!(ID)).is_some());
}
