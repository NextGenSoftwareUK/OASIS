#![cfg_attr(not(feature = "std"), no_std, no_main)]

#[ink::contract]
mod oasis {
    use ink::prelude::string::String;
    use ink::prelude::vec::Vec;
    use ink::storage::Mapping;

    /// Avatar structure
    #[derive(Debug, Clone, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub struct Avatar {
        pub id: String,
        pub username: String,
        pub email: String,
        pub first_name: String,
        pub last_name: String,
        pub created_date: u64,
        pub modified_date: u64,
    }

    /// AvatarDetail structure
    #[derive(Debug, Clone, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub struct AvatarDetail {
        pub id: String,
        pub username: String,
        pub email: String,
        pub karma_akashic_records: String,
        pub xp: u64,
        pub level: u64,
        pub created_date: u64,
        pub modified_date: u64,
    }

    /// Holon structure
    #[derive(Debug, Clone, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub struct Holon {
        pub id: String,
        pub name: String,
        pub description: String,
        pub parent_id: String,
        pub holon_type: u8,
        pub created_date: u64,
        pub modified_date: u64,
    }

    #[ink(storage)]
    pub struct Oasis {
        avatars: Mapping<String, Avatar>,
        avatars_by_username: Mapping<String, String>, // username -> avatar_id
        avatars_by_email: Mapping<String, String>, // email -> avatar_id
        avatar_details: Mapping<String, AvatarDetail>,
        avatar_details_by_username: Mapping<String, String>, // username -> avatar_detail_id
        holons: Mapping<String, Holon>,
        holons_by_parent: Mapping<String, Vec<String>>, // parent_id -> vec of holon_ids
        owner: AccountId,
    }

    #[ink(event)]
    pub struct AvatarCreated {
        #[ink(topic)]
        avatar_id: String,
        username: String,
    }

    #[ink(event)]
    pub struct AvatarDetailCreated {
        #[ink(topic)]
        avatar_id: String,
        username: String,
    }

    #[ink(event)]
    pub struct HolonCreated {
        #[ink(topic)]
        holon_id: String,
        name: String,
    }

    impl Oasis {
        #[ink(constructor)]
        pub fn new() -> Self {
            Self {
                avatars: Mapping::default(),
                avatars_by_username: Mapping::default(),
                avatars_by_email: Mapping::default(),
                avatar_details: Mapping::default(),
                avatar_details_by_username: Mapping::default(),
                holons: Mapping::default(),
                holons_by_parent: Mapping::default(),
                owner: Self::env().caller(),
            }
        }

        /// Entry function: Oasis_getAvatarByEmail (called via state_call RPC)
        #[ink(message)]
        pub fn get_avatar_by_email(&self, email: String) -> Option<Avatar> {
            let avatar_id = self.avatars_by_email.get(&email)?;
            self.avatars.get(&avatar_id)
        }

        /// Entry function: Oasis_getAvatarByUsername (called via state_call RPC)
        #[ink(message)]
        pub fn get_avatar_by_username(&self, username: String) -> Option<Avatar> {
            let avatar_id = self.avatars_by_username.get(&username)?;
            self.avatars.get(&avatar_id)
        }

        /// Entry function: Oasis_getAllAvatars (called via state_call RPC)
        #[ink(message)]
        pub fn get_all_avatars(&self) -> Vec<Avatar> {
            // In production, would maintain a list of all avatar IDs
            Vec::new()
        }

        /// Entry function: Oasis_getAvatarDetail (called via state_call RPC)
        #[ink(message)]
        pub fn get_avatar_detail(&self, avatar_id: String) -> Option<AvatarDetail> {
            self.avatar_details.get(&avatar_id)
        }

        /// Entry function: Oasis_getAvatarDetailByUsername (called via state_call RPC)
        #[ink(message)]
        pub fn get_avatar_detail_by_username(&self, username: String) -> Option<AvatarDetail> {
            let avatar_detail_id = self.avatar_details_by_username.get(&username)?;
            self.avatar_details.get(&avatar_detail_id)
        }

        /// Entry function: Oasis_getAvatarDetailByEmail (called via state_call RPC)
        #[ink(message)]
        pub fn get_avatar_detail_by_email(&self, email: String) -> Option<AvatarDetail> {
            // Would need email -> avatar_detail_id mapping
            None
        }

        /// Entry function: Oasis_getAllAvatarDetails (called via state_call RPC)
        #[ink(message)]
        pub fn get_all_avatar_details(&self) -> Vec<AvatarDetail> {
            // In production, would maintain a list of all avatar detail IDs
            Vec::new()
        }

        /// Entry function: Oasis_getHolon (called via state_call RPC)
        #[ink(message)]
        pub fn get_holon(&self, holon_id: String) -> Option<Holon> {
            self.holons.get(&holon_id)
        }

        /// Entry function: Oasis_getHolonByProviderKey (called via state_call RPC)
        #[ink(message)]
        pub fn get_holon_by_provider_key(&self, provider_key: String) -> Option<Holon> {
            self.holons.get(&provider_key)
        }

        /// Entry function: Oasis_getHolonsForParent (called via state_call RPC)
        #[ink(message)]
        pub fn get_holons_for_parent(&self, parent_id: String) -> Vec<Holon> {
            let holon_ids = self.holons_by_parent.get(&parent_id).unwrap_or_default();
            holon_ids
                .iter()
                .filter_map(|id| self.holons.get(id))
                .collect()
        }

        /// Entry function: Oasis_getHolonsByMetaData (called via state_call RPC)
        #[ink(message)]
        pub fn get_holons_by_metadata(&self, _meta_key: String, _meta_value: String) -> Vec<Holon> {
            // In production, would maintain metadata index
            Vec::new()
        }

        /// Entry function: Oasis_getAllHolons (called via state_call RPC)
        #[ink(message)]
        pub fn get_all_holons(&self) -> Vec<Holon> {
            // In production, would maintain a list of all holon IDs
            Vec::new()
        }

        /// Entry function: Oasis_search (called via state_call RPC)
        #[ink(message)]
        pub fn search(&self, _query: String) -> Vec<Holon> {
            // In production, would implement full-text search
            Vec::new()
        }

        /// Entry function: Oasis_getNFT (called via state_call RPC)
        #[ink(message)]
        pub fn get_nft(&self, _token_address: String) -> Option<String> {
            // NFT implementation would go here
            None
        }

        /// Entry function: Oasis_saveAvatarDetail (called via extrinsic)
        #[ink(message)]
        pub fn save_avatar_detail(
            &mut self,
            avatar_id: String,
            username: String,
            email: String,
            karma_akashic_records: String,
            xp: u64,
            level: u64,
        ) {
            let now = self.env().block_timestamp();
            let avatar_detail = AvatarDetail {
                id: avatar_id.clone(),
                username: username.clone(),
                email,
                karma_akashic_records,
                xp,
                level,
                created_date: now,
                modified_date: now,
            };

            self.avatar_details.insert(&avatar_id, &avatar_detail);
            self.avatar_details_by_username.insert(&username, &avatar_id);

            self.env().emit_event(AvatarDetailCreated {
                avatar_id,
                username,
            });
        }

        /// Entry function: Oasis_deleteAvatar (called via extrinsic)
        #[ink(message)]
        pub fn delete_avatar(&mut self, avatar_id: String) {
            self.avatars.remove(&avatar_id);
        }

        /// Entry function: Oasis_saveHolon (called via extrinsic)
        #[ink(message)]
        pub fn save_holon(
            &mut self,
            holon_id: String,
            name: String,
            description: String,
            parent_id: String,
            holon_type: u8,
        ) {
            let now = self.env().block_timestamp();
            let holon = Holon {
                id: holon_id.clone(),
                name: name.clone(),
                description,
                parent_id: parent_id.clone(),
                holon_type,
                created_date: now,
                modified_date: now,
            };

            self.holons.insert(&holon_id, &holon);

            // Add to parent index
            let mut parent_holons = self.holons_by_parent.get(&parent_id).unwrap_or_default();
            parent_holons.push(holon_id.clone());
            self.holons_by_parent.insert(&parent_id, &parent_holons);

            self.env().emit_event(HolonCreated {
                holon_id,
                name,
                parent_id,
            });
        }

        /// Entry function: Oasis_deleteHolon (called via extrinsic)
        #[ink(message)]
        pub fn delete_holon(&mut self, holon_id: String) {
            self.holons.remove(&holon_id);
        }
    }

    impl Default for Oasis {
        fn default() -> Self {
            Self::new()
        }
    }
}

