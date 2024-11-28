use scrypto::prelude::*;
use std::collections::HashMap;
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, ScryptoSbor, Debug)]
pub struct Proposal {
    id: u64,
    description: String,
    votes_for: Decimal,
    votes_against: Decimal,
    end_time: u64,
    executed: bool,
}

#[blueprint]
mod oasis_token {
    enable_method_auth! {
        methods {
            send_tokens => restrict_to: [OWNER];
            burn_tokens => restrict_to: [OWNER];
            create_proposal => restrict_to: [OWNER];
            vote_proposal => PUBLIC;
            execute_proposal => restrict_to: [OWNER];
        }
    }

    struct OASISToken {
        vault: Vault,
        owner_badge: ResourceAddress,
        proposals: HashMap<u64, Proposal>,
        next_proposal_id: u64
    }

    impl OASISToken {
        pub fn new(name: String, description: String, symbol: String, initial_supply: Decimal) -> (Global<OASISToken>, Bucket) {
            // NOTE: The owner's badge (owner_badge) is created here. 
            // The problem is that if the badge is not kept by the owner, they will lose control
            // over minting, burning, withdrawal, and deposit operations. It's critical that the owner
            // holds onto this badge in their wallet or secure vault to ensure they can manage the token later.
            // The owner must ensure they do not lose or transfer the badge unintentionally, as it grants 
            // exclusive access to key token operations.
            let owner_badge: Bucket = ResourceBuilder::new_fungible(OwnerRole::None)
                .metadata(metadata!(init{
                    "name" => "OASIS Owner Badge", locked;
                }))
                .divisibility(DIVISIBILITY_NONE)
                .mint_initial_supply(1)
                .into();

            let owner_badge_address = owner_badge.resource_address();

            let token_bucket: Bucket = ResourceBuilder::new_fungible(OwnerRole::None)
                .metadata(metadata!(
                    init {
                        "name" => name.clone(), locked;
                        "description" => description.clone(), locked;
                        "symbol" => symbol.clone(), locked;
                    }
                ))
                .mint_roles(mint_roles!{ 
                    minter => rule!(require(owner_badge_address));
                    minter_updater => rule!(deny_all);
                })
                .burn_roles(burn_roles!{
                    burner => rule!(require(owner_badge_address));
                    burner_updater => rule!(deny_all);
                })
                .withdraw_roles(withdraw_roles!{
                    withdrawer => rule!(require(owner_badge_address));
                    withdrawer_updater => rule!(deny_all);
                })
                .deposit_roles(deposit_roles!{
                    depositor => rule!(require(owner_badge_address));
                    depositor_updater => rule!(deny_all);
                })
                .mint_initial_supply(initial_supply);

            let component = Self {
                vault: Vault::with_bucket(token_bucket),
                owner_badge: owner_badge_address,
                proposals: HashMap::new(),
                next_proposal_id: 1,
            }
            .instantiate()
            .prepare_to_globalize(OwnerRole::Fixed(rule!(require(
                owner_badge_address
            ))))
            .globalize();

            (component, owner_badge)
        }

        pub fn create_proposal(&mut self, description: String, duration: u64) -> u64 {
            let proposal_id = self.next_proposal_id;
            self.next_proposal_id += 1;

            let proposal = Proposal {
                id: proposal_id,
                description,
                votes_for: Decimal::zero(),
                votes_against: Decimal::zero(),
                end_time: Runtime::current_epoch() + duration,
                executed: false,
            };

            self.proposals.insert(proposal_id, proposal);

            proposal_id
        }

        pub fn vote_proposal(&mut self, proposal_id: u64, support: bool, voter_tokens: Bucket) {
            let proposal = self
                .proposals
                .get_mut(&proposal_id)
                .expect("Proposal does not exist");

            assert!(
                Runtime::current_epoch() <= proposal.end_time,
                "Voting period has ended for this proposal"
            );

            let vote_weight = voter_tokens.amount();

            if support {
                proposal.votes_for += vote_weight;
            } else {
                proposal.votes_against += vote_weight;
            }

            // Возврат токенов после учета голосов
            self.vault.put(voter_tokens);
        }

        pub fn execute_proposal(&mut self, proposal_id: u64) {
            let proposal = self
                .proposals
                .get_mut(&proposal_id)
                .expect("Proposal does not exist");

            assert!(
                Runtime::current_epoch() > proposal.end_time,
                "Voting period has not ended"
            );
            assert!(!proposal.executed, "Proposal has already been executed");

            if proposal.votes_for > proposal.votes_against {
                // Здесь вы можете добавить логику для исполнения предложений, например,
                // обновление параметров контракта или вызов других методов
                info!("Proposal {} has been approved and executed", proposal.id);
            } else {
                info!("Proposal {} was not approved", proposal.id);
            }

            proposal.executed = true;
        }

        pub fn send_tokens(&mut self, recipient: ComponentAddress, amount: Decimal) {
            if !Component::exists(recipient) {
                panic!("Recipient address is not a valid component!");
            }

            assert!(self.vault.amount() >= amount, "Not enough tokens in the vault!");

            let bucket = self.vault.take(amount);
            Component::from(recipient).call::<()>("deposit", vec![scrypto::component::Argument::Bucket(bucket)]);
        }

        pub fn burn_tokens(&mut self, amount: Decimal) {
            assert!(self.vault.amount() >= amount, "Not enough tokens in the vault to burn!");

            let bucket = self.vault.take(amount);
            bucket.burn();
        }
    }
}

#[derive(NonFungibleData)]
struct OASISNFT {
    name: String,
    description: String,
}

#[blueprint]
mod oasis_nft {
    enable_method_auth! {
        methods {
            mint_nft => restrict_to: [OWNER];
            send_nft => restrict_to: [OWNER];
        }
    }

    struct OASISNFTComponent {
        vault: Vault,
        owner_badge: ResourceAddress,
        token_vault: Vault,
        token_address: ResourceAddress
    }

    impl OASISNFTComponent {
        pub fn new(owner_badge: Bucket, token_address: ResourceAddress) -> (Global<OASISNFTComponent>, Bucket) {
            let owner_badge_address = owner_badge.resource_address();

            let nft_vault = Vault::new(ResourceAddress::new_non_fungible());
            let token_vault = Vault::new(token_address);

            let component = Self {
                vault: nft_vault,
                owner_badge: owner_badge_address,
                token_vault, 
                token_address,
            }
            .instantiate()
            .prepare_to_globalize(OwnerRole::Fixed(rule!(require(owner_badge_address))))
            .globalize();

            (component, owner_badge)
        }

        pub fn mint_nft(&mut self, name: String, description: String, payment: Bucket) -> NonFungibleId {
            assert!(payment.resource_address() == self.token_address, "Invalid token type for payment. Please use OASISToken.");
            assert!(payment.amount() >= dec!("10"), "Insufficient OASISToken for minting NFT.");

            self.token_vault.put(payment);

            let nft_data = OASISNFT { name, description };
            let new_nft = ResourceBuilder::new_integer_non_fungible::<OASISNFT>()
                .metadata(metadata!(
                    init {
                        "name" => "OASIS NFT Collection", locked;
                    }
                ))
                .mint_initial_supply([(IntegerNonFungibleLocalId::new(1u64), nft_data)]);

            self.vault.put(new_nft);

            NonFungibleId::random()
        }

        pub fn send_nft(&mut self, recipient: ComponentAddress, nft_id: NonFungibleId) {
            assert!(self.vault.as_non_fungible().contains_non_fungible(&nft_id),
                "This NFT does not exist in the vault.");
    
            let nft_bucket = self
                .vault
                .as_non_fungible()
                .take_non_fungible(&nft_id);
            let recipient_component = Component::from(recipient);
            recipient_component.call::<()>("deposit", vec![scrypto::component::Argument::Bucket(nft_bucket)]);
        }
    }
}

#[derive(ScryptoSbor, Clone)]
pub enum OASISEntityType {
    Avatar,
    Holon,
    AvatarDetail,
}

#[derive(ScryptoSbor, Clone)]
pub struct OASISEntity {
    pub numeric_id: u64,
    pub guid_id: String,
    pub info_json: String,
    pub entity_type: OASISEntityType,
}

#[blueprint]
mod oasis_contract {
    use super::*;

    struct OASISContract {
        contracts: HashMap<u64, OASISEntity>,
    }

    impl OASISContract {
        pub fn new() -> Self {
            Self {
                contracts: HashMap::new(),
            }
        }

        pub fn create(
            &mut self,
            numeric_id: u64,
            guid_id: Uuid,
            info_json: String,
            entity_type: OASISEntityType,
        ) -> bool {
            let contract = OASISContract {
                numeric_id,
                guid_id,
                info_json,
                entity_type,
            };

            if self.contracts.contains_key(&numeric_id) {
                return false;
            }

            self.contracts.insert(numeric_id, contract);
            true
        }

        pub fn update(
            &mut self,
            numeric_id: u64,
            info_json: Option<String>,
            entity_type: Option<OASISEntityType>,
        ) -> bool {
            if let Some(contract) = self.contracts.get_mut(&numeric_id) {
                if let Some(new_info_json) = info_json {
                    contract.info_json = new_info_json;
                }
                if let Some(new_entity_type) = entity_type {
                    contract.entity_type = new_entity_type;
                }
                return true;
            }
            false
        }

        pub fn get(&self, numeric_id: u64) -> Option<OASISContract> {
            self.contracts.get(&numeric_id).cloned()
        }

        pub fn get_all(&self) -> Vec<OASISContract> {
            self.contracts.values().cloned().collect()
        }

        pub fn delete(&mut self, numeric_id: u64) -> bool {
            self.contracts.remove(&numeric_id).is_some()
        }

        pub fn get_all_by_entity_type(&self, entity_type: OASISEntityType) -> Vec<OASISContract> {
            self.contracts
                .values()
                .filter(|&contract| contract.entity_type == entity_type)
                .cloned()
                .collect()
        }
    }
}