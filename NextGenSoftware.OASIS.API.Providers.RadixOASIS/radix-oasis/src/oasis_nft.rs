use scrypto::prelude::*;
use serde::{Serialize, Deserialize};

#[derive(NonFungibleData, Serialize, Deserialize, ScryptoSbor, Debug)]
pub struct OASISNFTMetadata {
    name: String,
    description: String
}

#[blueprint]
mod oasis_nft {
    enable_method_auth! {
        methods {
            buy => PUBLIC;
        }
    }

    struct OASISNFT {
        nfts: Vault,
        nft_def: ResourceAddress,
        // Vault to contain the collected XRD
        collected_xrd: Vault,
        price: Decimal,
        nb_nft_minted: u64
    }

    impl OASISNFT {
        pub fn instantiate(price: Decimal) -> Global<OASISNFT> {
            let nft_bucket = ResourceBuilder::new_integer_non_fungible(OwnerRole::None)
                .metadata(metadata!(
                    init {
                        "name" => "OASIS Nft".to_owned(), locked;
                    }
                ))
                .mint_initial_supply([IntegerNonFungibleLocalId::new(1u64)]);

            // Create an NFT resource with mutable supply
            let (address_reservation, component_address) =
                Runtime::allocate_component_address(OASISNFT::blueprint_id());

            let nft_resource_manager =
                ResourceBuilder::new_integer_non_fungible::<OASISNFTMetadata>(OwnerRole::None)
                    .metadata(metadata!(
                        init {
                            "name" => "OASIS Nft.".to_owned(), locked;
                        }
                    ))
                    .mint_roles(mint_roles!(
                        minter => rule!(require(global_caller(component_address)));
                        minter_updater => rule!(deny_all);
                    ))
                    .burn_roles(burn_roles!(
                        burner => rule!(require(global_caller(component_address)));
                        burner_updater => rule!(deny_all);
                    ))
                    .create_with_no_initial_supply();

            // Instantiate our component
            Self {
                nfts: Vault::with_bucket(nft_bucket.into()),
                nft_def: nft_bucket,
                price: price,
                collected_xrd: Vault::new(XRD),
                nb_nft_minted: 0
            }
            .instantiate()
            .prepare_to_globalize(OwnerRole::None)
            .with_address(address_reservation)
            .globalize()
        }

        pub fn buy(
            &mut self,
            id: NonFungibleLocalId,
            mut payment: Bucket,
        ) -> (NonFungibleBucket, Bucket) {
            self.collected_xrd.put(payment.take(self.price));

            let nft_bucket = self.nfts.as_non_fungible().take_non_fungible(&id);

            (nft_bucket, payment)
        }
    }
}