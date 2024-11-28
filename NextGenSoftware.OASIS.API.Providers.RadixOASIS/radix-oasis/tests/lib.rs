use scrypto::prelude::*;
use radix_engine::ledger::*;
use radix_engine::transaction::*;
use scrypto_unit::*;
use transaction::builder::ManifestBuilder;

#[test]
fn test_create_proposal() {
    let mut test_runner = TestRunner::builder().build();
    
    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_token",
            "OASISToken",
            "new",
            args!["OASIS Token".to_string(), "Test Token".to_string(), "OASIS".to_string(), dec!(1000)],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];

    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "create_proposal",
            args!["Proposal description".to_string(), 10],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_vote_proposal() {
    let mut test_runner = TestRunner::builder().build();
    
    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_token",
            "OASISToken",
            "new",
            args!["OASIS Token".to_string(), "Test Token".to_string(), "OASIS".to_string(), dec!(1000)],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];

    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "create_proposal",
            args!["Proposal description".to_string(), 10],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let proposal_id: u64 = receipt.expect_commit().output(0);

    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "vote_proposal",
            args![proposal_id, true, Bucket::new(dec!(100))],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_execute_proposal() {
    let mut test_runner = TestRunner::builder().build();
    
    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_token",
            "OASISToken",
            "new",
            args!["OASIS Token".to_string(), "Test Token".to_string(), "OASIS".to_string(), dec!(1000)],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];

    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "create_proposal",
            args!["Proposal description".to_string(), 10],
        )
        .call_method(
            component_address,
            "vote_proposal",
            args![0, true, Bucket::new(dec!(100))],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();

    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "execute_proposal",
            args![0],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_send_tokens() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_token",
            "OASISToken",
            "new",
            args!["OASIS Token".to_string(), "Test Token".to_string(), "OASIS".to_string(), dec!(1000)],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];

    let recipient_address = test_runner.new_account();
    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "send_tokens",
            args![recipient_address, dec!(50)],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_burn_tokens() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_token",
            "OASISToken",
            "new",
            args!["OASIS Token".to_string(), "Test Token".to_string(), "OASIS".to_string(), dec!(1000)],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];

    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "burn_tokens",
            args![dec!(100)],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_mint_nft_with_sufficient_payment() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_nft",
            "OASISNFTComponent",
            "new",
            args![Bucket::new(ResourceAddress::new_fungible()), ResourceAddress::new_fungible()],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];
    let token_address = receipt.expect_commit().new_resource_addresses[2];

    let payment = Bucket::new(token_address).with_amount(dec!("10"));
    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "mint_nft",
            args!["Test NFT".to_string(), "A description for Test NFT".to_string(), payment],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_mint_nft_with_insufficient_payment() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_nft",
            "OASISNFTComponent",
            "new",
            args![Bucket::new(ResourceAddress::new_fungible()), ResourceAddress::new_fungible()],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];
    let token_address = receipt.expect_commit().new_resource_addresses[2];

    let insufficient_payment = Bucket::new(token_address).with_amount(dec!("5"));
    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "mint_nft",
            args!["Test NFT".to_string(), "A description for Test NFT".to_string(), insufficient_payment],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    assert!(receipt.expect_commit().is_err(), "Ожидалась ошибка из-за недостатка средств!");
}

#[test]
fn test_send_nft() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_nft",
            "OASISNFTComponent",
            "new",
            args![Bucket::new(ResourceAddress::new_fungible()), ResourceAddress::new_fungible()],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];
    let token_address = receipt.expect_commit().new_resource_addresses[2];

    let payment = Bucket::new(token_address).with_amount(dec!("10"));
    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "mint_nft",
            args!["Test NFT".to_string(), "A description for Test NFT".to_string(), payment],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let nft_id: NonFungibleId = receipt.expect_commit().output(0);

    let recipient_address = test_runner.new_account();
    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "send_nft",
            args![recipient_address, nft_id.clone()],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_send_nft_not_owned() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_nft",
            "OASISNFTComponent",
            "new",
            args![Bucket::new(ResourceAddress::new_fungible()), ResourceAddress::new_fungible()],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];
    let owner_badge = receipt.expect_commit().new_resource_addresses[1];
    let token_address = receipt.expect_commit().new_resource_addresses[2];

    let invalid_nft_id = NonFungibleId::random();
    let recipient_address = test_runner.new_account();
    let manifest = ManifestBuilder::new()
        .create_proof_from_account(owner_badge)
        .call_method(
            component_address,
            "send_nft",
            args![recipient_address, invalid_nft_id],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    assert!(receipt.expect_commit().is_err(), "Ожидалась ошибка из-за отсутствия NFT в хранилище!");
}

use scrypto::prelude::*;
use radix_engine::ledger::*;
use radix_engine::transaction::*;
use scrypto_unit::*;
use transaction::builder::ManifestBuilder;

#[test]
fn test_create_contract_success() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_contract",
            "OASISContract",
            "new",
            args![],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];

    let manifest = ManifestBuilder::new()
        .call_method(
            component_address,
            "create",
            args![1u64, Uuid::new_v4(), "Test info".to_string(), OASISEntityType::Avatar],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_create_contract_duplicate_id() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_contract",
            "OASISContract",
            "new",
            args![],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];

    let manifest = ManifestBuilder::new()
        .call_method(
            component_address,
            "create",
            args![1u64, Uuid::new_v4(), "Test info".to_string(), OASISEntityType::Avatar],
        )
        .build();

    test_runner.execute_manifest(manifest, vec![]).expect_commit_success();

    let manifest = ManifestBuilder::new()
        .call_method(
            component_address,
            "create",
            args![1u64, Uuid::new_v4(), "Duplicate".to_string(), OASISEntityType::Holon],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    assert!(receipt.expect_commit().is_err(), "Ожидалась ошибка из-за дублирования id");
}

#[test]
fn test_update_contract() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_contract",
            "OASISContract",
            "new",
            args![],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];

    let manifest = ManifestBuilder::new()
        .call_method(
            component_address,
            "create",
            args![1u64, Uuid::new_v4(), "Test info".to_string(), OASISEntityType::Avatar],
        )
        .build();

    test_runner.execute_manifest(manifest, vec![]).expect_commit_success();

    let manifest = ManifestBuilder::new()
        .call_method(
            component_address,
            "update",
            args![1u64, Some("Updated info".to_string()), Some(OASISEntityType::Holon)],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_get_contract() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_contract",
            "OASISContract",
            "new",
            args![],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];

    let manifest = ManifestBuilder::new()
        .call_method(
            component_address,
            "create",
            args![1u64, Uuid::new_v4(), "Test info".to_string(), OASISEntityType::Avatar],
        )
        .build();

    test_runner.execute_manifest(manifest, vec![]).expect_commit_success();

    let manifest = ManifestBuilder::new()
        .call_method(component_address, "get", args![1u64])
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let result: Option<OASISEntity> = receipt.expect_commit().output(0);
    assert!(result.is_some(), "Контракт должен существовать");
}

#[test]
fn test_delete_contract() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_contract",
            "OASISContract",
            "new",
            args![],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];

    let manifest = ManifestBuilder::new()
        .call_method(
            component_address,
            "create",
            args![1u64, Uuid::new_v4(), "Test info".to_string(), OASISEntityType::Avatar],
        )
        .build();

    test_runner.execute_manifest(manifest, vec![]).expect_commit_success();

    let manifest = ManifestBuilder::new()
        .call_method(component_address, "delete", args![1u64])
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    receipt.expect_commit_success();
}

#[test]
fn test_get_all_by_entity_type() {
    let mut test_runner = TestRunner::builder().build();

    let manifest = ManifestBuilder::new()
        .call_function(
            "oasis_contract",
            "OASISContract",
            "new",
            args![],
        )
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let component_address = receipt.expect_commit().new_component_addresses[0];

    let manifest = ManifestBuilder::new()
        .call_method(
            component_address,
            "create",
            args![1u64, Uuid::new_v4(), "Test info 1".to_string(), OASISEntityType::Avatar],
        )
        .call_method(
            component_address,
            "create",
            args![2u64, Uuid::new_v4(), "Test info 2".to_string(), OASISEntityType::Holon],
        )
        .call_method(
            component_address,
            "create",
            args![3u64, Uuid::new_v4(), "Test info 3".to_string(), OASISEntityType::Avatar],
        )
        .build();

    test_runner.execute_manifest(manifest, vec![]).expect_commit_success();

    let manifest = ManifestBuilder::new()
        .call_method(component_address, "get_all_by_entity_type", args![OASISEntityType::Avatar])
        .build();

    let receipt = test_runner.execute_manifest(manifest, vec![]);
    let avatars: Vec<OASISEntity> = receipt.expect_commit().output(0);
    assert_eq!(avatars.len(), 2, "Ожидалось 2 контракта типа Avatar");
}