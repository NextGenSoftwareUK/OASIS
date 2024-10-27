use scrypto::prelude::*;
use serde::{Deserialize, Serialize}

#[blueprint]
mod oasis_token {
    enable_method_auth! {
        methods {
            send_tokens => OWNER;
            burn_tokens => OWNER;
        }
    }

    struct OASISToken {
        vault: Vault,
        owner_badge: ResourceAddress
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
                owner_badge: owner_badge_address
            }
            .instantiate()
            .prepare_to_globalize(OwnerRole::Fixed(rule!(require(
                owner_badge_address
            ))))
            .globalize();

            (component, owner_badge)
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
            mint_nft => OWNER;
            send_nft => OWNER;
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

            let nft_vault = Vault::new_empty(ResourceAddress::new_non_fungible());
            let token_vault = Vault::new_empty(token_address);

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
            let new_nft = ResourceBuilder::new_non_fungible::<OASISNFT>()
                .metadata(metadata!(
                    init {
                        "name" => "OASIS NFT Collection", locked;
                    }
                ))
                .mint_initial_supply([(NonFungibleId::random(), nft_data)]);

            self.vault.put(new_nft);

            NonFungibleId::random()
        }

        pub fn send_nft(&mut self, recipient: ComponentAddress, nft_id: NonFungibleId) {
            assert!(self.vault.contains_non_fungible(&nft_id), "This NFT does not exist in the vault.");
    
            let nft_bucket = self.vault.take_non_fungible(&nft_id);
            let recipient_component = Component::from(recipient);
            recipient_component.call::<()>("deposit", vec![scrypto::component::Argument::Bucket(nft_bucket)]);
        }
    }
}

#[blueprint]
mod oasis_contract {
    struct OASISContract {

    }

    impl OASISContract {

    }
}
