use scrypto::prelude::*;
use serde::{Serialize, Deserialize, Serializer, Deserializer};

#[derive(Serialize, Deserialize, ScryptoSbor, Debug)]
pub struct OASISProposal {
    id: u64,
    description: String,
    #[serde(serialize_with = "decimal_to_string", deserialize_with = "string_to_decimal")]
    votes_for: Decimal,
    #[serde(serialize_with = "decimal_to_string", deserialize_with = "string_to_decimal")]
    votes_against: Decimal,
    end_time: u64,
    executed: bool,
}

#[derive(ScryptoSbor, Clone, PartialEq)]
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
mod oasis_component {

    enable_method_auth! {
        methods {
            create_proposal => restrict_to: [OWNER];
            vote_proposal => PUBLIC;
            execute_proposal => restrict_to: [OWNER];
            create => PUBLIC;
            update => PUBLIC;
            get => PUBLIC;
            get_all => PUBLIC;
            delete => PUBLIC;
            get_all_by_entity_type => PUBLIC;
        }
    }

    struct OASISComponent {
        vault: Vault,
        proposals: std::collections::HashMap<u64, OASISProposal>,
        entities: std::collections::HashMap<u64, OASISEntity>,
        next_proposal_id: u64
    }

    impl OASISComponent {

        pub fn instantiate(tokens: Bucket) -> Global<OASISComponent> {
            Self {
                vault: Vault::with_bucket(tokens),
                entities: std::collections::HashMap::new(),
                proposals: std::collections::HashMap::new(),
                next_proposal_id: 1
            }
            .instantiate()
            .prepare_to_globalize(OwnerRole::None)
            .globalize()
        }

        pub fn create(
            &mut self,
            numeric_id: u64,
            guid_id: String,
            info_json: String,
            entity_type: OASISEntityType,
        ) -> bool {
            let entity = OASISEntity {
                numeric_id,
                guid_id,
                info_json,
                entity_type,
            };

            if self.entities.contains_key(&numeric_id) {
                return false;
            }

            self.entities.insert(numeric_id, entity);
            true
        }

        pub fn update(
            &mut self,
            numeric_id: u64,
            info_json: Option<String>,
            entity_type: Option<OASISEntityType>,
        ) -> bool {
            if let Some(contract) = self.entities.get_mut(&numeric_id) {
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

        pub fn get(&self, numeric_id: u64) -> Option<OASISEntity> {
            self.entities.get(&numeric_id).cloned()
        }

        pub fn get_all(&self) -> Vec<OASISEntity> {
            self.entities.values().cloned().collect()
        }

        pub fn delete(&mut self, numeric_id: u64) -> bool {
            self.entities.remove(&numeric_id).is_some()
        }

        pub fn get_all_by_entity_type(&self, entity_type: OASISEntityType) -> Vec<OASISEntity> {
            self.entities
                .values()
                .filter(|&entity| entity.entity_type == entity_type)
                .cloned()
                .collect()
        }

        pub fn create_proposal(&mut self, description: String, duration: u64) -> u64 {
            let proposal_id = self.next_proposal_id;
            let current_epoch = Runtime::current_epoch().number();

            self.next_proposal_id += 1;

            let proposal = OASISProposal {
                id: proposal_id,
                description,
                votes_for: Decimal::zero(),
                votes_against: Decimal::zero(),
                end_time: current_epoch + duration,
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

            let current_epoch = Runtime::current_epoch().number(); // .number() returs epoch value as u64.

            assert!(
                current_epoch <= proposal.end_time,
                "Voting period has ended for this proposal"
            );

            let vote_weight = voter_tokens.amount();

            if support {
                proposal.votes_for += vote_weight;
            } else {
                proposal.votes_against += vote_weight;
            }

            self.vault.put(voter_tokens);
        }

        pub fn execute_proposal(&mut self, proposal_id: u64) {
            let proposal = self
                .proposals
                .get_mut(&proposal_id)
                .expect("Proposal does not exist");

            let current_epoch = Runtime::current_epoch().number(); // .number() returs epoch value as u64.

            assert!(
                current_epoch > proposal.end_time,
                "Voting period has not ended"
            );
            assert!(!proposal.executed, "Proposal has already been executed");

            if proposal.votes_for > proposal.votes_against {
                info!("Proposal {} has been approved and executed", proposal.id);
            } else {
                info!("Proposal {} was not approved", proposal.id);
            }

            proposal.executed = true;
        }
    }
}

fn decimal_to_string<S>(decimal: &Decimal, serializer: S) -> Result<S::Ok, S::Error>
where
    S: Serializer,
{
    serializer.serialize_str(&decimal.to_string())
}

fn string_to_decimal<'de, D>(deserializer: D) -> Result<Decimal, D::Error>
where
    D: Deserializer<'de>,
{
    let s = String::deserialize(deserializer)?;
    s.parse().map_err(serde::de::Error::custom)
}