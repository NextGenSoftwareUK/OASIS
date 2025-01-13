use scrypto::prelude::*;

#[blueprint]
mod oasis_token {
    enable_method_auth! {
        methods {
            send_tokens => restrict_to: [OWNER];
            burn_tokens => restrict_to: [OWNER];
        }
    }

    struct OASISToken {
        vault: Vault,
        owner_badge: ResourceAddress
    }

    impl OASISToken {
        pub fn new(name: String, description: String, symbol: String, initial_supply: Decimal) -> (Global<OASISToken>, Bucket) {
            /*
             * NOTE: The owner's badge (owner_badge) is created here. 
             * The problem is that if the badge is not kept by the owner, they will lose control
             * over minting, burning, withdrawal, and deposit operations. It's critical that the owner
             * holds onto this badge in their wallet or secure vault to ensure they can manage the token later.
             * The owner must ensure they do not lose or transfer the badge unintentionally, as it grants 
             * exclusive access to key token operations.
             */
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
                .mint_initial_supply(initial_supply)
                .into(); // Map to Bucket from FungibleBucket

            let component = Self {
                vault: Vault::with_bucket(token_bucket),
                owner_badge: owner_badge_address,
            }
            .instantiate()
            .prepare_to_globalize(OwnerRole::Fixed(rule!(require(
                owner_badge_address
            ))))
            .globalize();

            (component, owner_badge)
        }

        pub fn send_tokens(&mut self, recipient: ComponentAddress, amount: Decimal) {
            assert!(self.vault.amount() >= amount, "Not enough tokens in the vault!");

            // Take the specified amount of tokens from the vault
            let bucket = self.vault.take(amount);

            // Use Runtime::call_method to send tokens to the recipient
            // Runtime::call_method::<()>(
            //     recipient, 
            //     "deposit", 
            //     args!(bucket)
            // );

            // borrow_component!(recipient).call::<()>("deposit", args![bucket]);

            let recipient = Component::from(to);
            recipient.call::<()>("deposit", scrypto_args![bucket]);
        }

        pub fn burn_tokens(&mut self, amount: Decimal) {
            assert!(self.vault.amount() >= amount, "Not enough tokens in the vault to burn!");

            let bucket = self.vault.take(amount);
            bucket.burn();
        }
    }
}