#![cfg_attr(not(feature = "std"), no_std, no_main)]

// PSP34 selectors and SCALE types follow the published w3f PSP34 ABI.
// Mint/burn are explicit OASIS extensions; storage cells are not NFT ownership.
#[ink::contract]
mod oasis_psp34 {
    use ink::{
        prelude::{string::String, vec::Vec},
        storage::Mapping,
    };

    #[derive(Debug, Clone, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(
        feature = "std",
        derive(scale_info::TypeInfo, ink::storage::traits::StorageLayout)
    )]
    pub enum Id {
        U8(u8),
        U16(u16),
        U32(u32),
        U64(u64),
        U128(u128),
        Bytes(Vec<u8>),
    }

    #[derive(Debug, Clone, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub enum PSP34Error {
        Custom(String),
        SelfApprove,
        NotApproved,
        TokenExists,
        TokenNotExists,
        SafeTransferCheckFailed(String),
    }

    #[ink(storage)]
    pub struct OasisPsp34 {
        minter: AccountId,
        owners: Mapping<Id, AccountId>,
        balances: Mapping<AccountId, u32>,
        operators: Mapping<(AccountId, AccountId), bool>,
        approvals: Mapping<(Id, AccountId), u64>,
        epochs: Mapping<Id, u64>,
        supply: u128,
        attributes: Mapping<(Id, Vec<u8>), Vec<u8>>,
        attribute_keys: Mapping<Id, Vec<Vec<u8>>>,
    }

    #[ink(event)]
    pub struct Transfer {
        #[ink(topic)]
        from: Option<AccountId>,
        #[ink(topic)]
        to: Option<AccountId>,
        #[ink(topic)]
        id: Id,
    }
    #[ink(event)]
    pub struct Approval {
        #[ink(topic)]
        owner: AccountId,
        #[ink(topic)]
        operator: AccountId,
        #[ink(topic)]
        id: Option<Id>,
        approved: bool,
    }
    #[ink(event)]
    pub struct AttributeSet {
        id: Id,
        key: Vec<u8>,
        data: Vec<u8>,
    }

    impl OasisPsp34 {
        #[ink(constructor, selector = 0x00000001)]
        pub fn new(name: Vec<u8>, symbol: Vec<u8>, description: Vec<u8>, image: Vec<u8>) -> Self {
            assert!([name.len(), symbol.len(), description.len(), image.len()]
                .iter()
                .all(|size| *size <= 8192));
            let mut contract = Self {
                minter: Self::env().caller(),
                owners: Mapping::default(),
                balances: Mapping::default(),
                operators: Mapping::default(),
                approvals: Mapping::default(),
                epochs: Mapping::default(),
                supply: 0,
                attributes: Mapping::default(),
                attribute_keys: Mapping::default(),
            };
            let id = contract.collection_id();
            for (key, data) in [
                (b"name".to_vec(), name),
                (b"symbol".to_vec(), symbol),
                (b"description".to_vec(), description),
                (b"image".to_vec(), image),
            ] {
                contract
                    .attributes
                    .insert(&(id.clone(), key.clone()), &data);
                contract.env().emit_event(AttributeSet {
                    id: id.clone(),
                    key,
                    data,
                });
            }
            contract
        }

        #[ink(message, selector = 0xffa27a5f)]
        pub fn collection_id(&self) -> Id {
            let address = self.env().account_id();
            let bytes: &[u8] = address.as_ref();
            Id::Bytes(bytes.to_vec())
        }
        #[ink(message, selector = 0xcde7e55f)]
        pub fn balance_of(&self, owner: AccountId) -> u32 {
            self.balances.get(owner).unwrap_or(0)
        }
        #[ink(message, selector = 0x1168624d)]
        pub fn owner_of(&self, id: Id) -> Option<AccountId> {
            self.owners.get(id)
        }
        #[ink(message, selector = 0x628413fe)]
        pub fn total_supply(&self) -> Balance {
            self.supply
        }
        #[ink(message, selector = 0xf19d48d1)]
        pub fn get_attribute(&self, id: Id, key: Vec<u8>) -> Option<Vec<u8>> {
            self.attributes.get((id, key))
        }

        #[ink(message, selector = 0x4790f55a)]
        pub fn allowance(&self, owner: AccountId, operator: AccountId, id: Option<Id>) -> bool {
            if let Some(token) = id {
                if self.owners.get(&token) != Some(owner) {
                    return false;
                }
                let epoch = self.epochs.get(&token);
                self.operators.get((owner, operator)).unwrap_or(false)
                    || epoch.is_some() && self.approvals.get((token, operator)) == epoch
            } else {
                self.operators.get((owner, operator)).unwrap_or(false)
            }
        }

        #[ink(message, selector = 0x1932a8b0)]
        pub fn approve(
            &mut self,
            operator: AccountId,
            id: Option<Id>,
            approved: bool,
        ) -> Result<(), PSP34Error> {
            let caller = self.env().caller();
            let owner = match &id {
                Some(token) => self.owners.get(token).ok_or(PSP34Error::TokenNotExists)?,
                None => caller,
            };
            if owner == operator {
                return Err(PSP34Error::SelfApprove);
            }
            if caller != owner && !self.allowance(owner, caller, id.clone()) {
                return Err(PSP34Error::NotApproved);
            }
            if let Some(token) = &id {
                if approved {
                    let epoch = self.epochs.get(token).ok_or_else(Self::invalid_state)?;
                    self.approvals.insert(&(token.clone(), operator), &epoch);
                } else {
                    self.approvals.remove(&(token.clone(), operator));
                }
            } else if approved {
                self.operators.insert(&(owner, operator), &true);
            } else {
                self.operators.remove(&(owner, operator));
            }
            self.env().emit_event(Approval {
                owner,
                operator,
                id,
                approved,
            });
            Ok(())
        }

        #[ink(message, selector = 0x3128d61b)]
        pub fn transfer(
            &mut self,
            to: AccountId,
            id: Id,
            _data: Vec<u8>,
        ) -> Result<(), PSP34Error> {
            Self::recipient(to)?;
            let owner = self.owners.get(&id).ok_or(PSP34Error::TokenNotExists)?;
            let caller = self.env().caller();
            if caller != owner && !self.allowance(owner, caller, Some(id.clone())) {
                return Err(PSP34Error::NotApproved);
            }
            let epoch = self.next_epoch(&id)?;
            if owner != to {
                let from_balance = self
                    .balance_of(owner)
                    .checked_sub(1)
                    .ok_or_else(Self::invalid_state)?;
                let to_balance = self
                    .balance_of(to)
                    .checked_add(1)
                    .ok_or_else(Self::invalid_state)?;
                self.set_balance(owner, from_balance);
                self.set_balance(to, to_balance);
            }
            self.epochs.insert(&id, &epoch);
            self.owners.insert(&id, &to);
            self.env().emit_event(Transfer {
                from: Some(owner),
                to: Some(to),
                id,
            });
            Ok(())
        }

        // Constructor minter authorizes mint; burn requires token owner/approval.
        #[ink(message, selector = 0x00000010)]
        pub fn mint(
            &mut self,
            to: AccountId,
            id: Id,
            attributes: Vec<(Vec<u8>, Vec<u8>)>,
        ) -> Result<(), PSP34Error> {
            if self.env().caller() != self.minter {
                return Err(PSP34Error::NotApproved);
            }
            Self::recipient(to)?;
            if self.owners.contains(&id) || id == self.collection_id() {
                return Err(PSP34Error::TokenExists);
            }
            if attributes.len() > 16 || matches!(&id,Id::Bytes(bytes) if bytes.len()>1024) {
                return Err(Self::invalid_metadata());
            }
            for (index, (key, value)) in attributes.iter().enumerate() {
                if key.is_empty()
                    || key.len() > 128
                    || value.len() > 8192
                    || attributes[..index].iter().any(|(prior, _)| prior == key)
                {
                    return Err(Self::invalid_metadata());
                }
            }
            let balance = self
                .balance_of(to)
                .checked_add(1)
                .ok_or_else(Self::invalid_state)?;
            let supply = self.supply.checked_add(1).ok_or_else(Self::invalid_state)?;
            let epoch = self.next_epoch(&id)?;
            let keys = attributes
                .iter()
                .map(|(key, _)| key.clone())
                .collect::<Vec<_>>();
            self.set_balance(to, balance);
            self.supply = supply;
            self.epochs.insert(&id, &epoch);
            self.owners.insert(&id, &to);
            self.attribute_keys.insert(&id, &keys);
            for (key, data) in attributes {
                self.attributes.insert(&(id.clone(), key.clone()), &data);
                self.env().emit_event(AttributeSet {
                    id: id.clone(),
                    key,
                    data,
                });
            }
            self.env().emit_event(Transfer {
                from: None,
                to: Some(to),
                id,
            });
            Ok(())
        }

        #[ink(message, selector = 0x00000011)]
        pub fn burn(&mut self, id: Id) -> Result<(), PSP34Error> {
            let owner = self.owners.get(&id).ok_or(PSP34Error::TokenNotExists)?;
            let caller = self.env().caller();
            if caller != owner && !self.allowance(owner, caller, Some(id.clone())) {
                return Err(PSP34Error::NotApproved);
            }
            let balance = self
                .balance_of(owner)
                .checked_sub(1)
                .ok_or_else(Self::invalid_state)?;
            let supply = self.supply.checked_sub(1).ok_or_else(Self::invalid_state)?;
            let epoch = self.next_epoch(&id)?;
            for key in self.attribute_keys.get(&id).unwrap_or_default() {
                self.attributes.remove(&(id.clone(), key));
            }
            self.attribute_keys.remove(&id);
            self.owners.remove(&id);
            self.set_balance(owner, balance);
            self.supply = supply;
            self.epochs.insert(&id, &epoch);
            self.env().emit_event(Transfer {
                from: Some(owner),
                to: None,
                id,
            });
            Ok(())
        }

        fn next_epoch(&self, id: &Id) -> Result<u64, PSP34Error> {
            self.epochs
                .get(id)
                .unwrap_or(0)
                .checked_add(1)
                .ok_or_else(Self::invalid_state)
        }
        fn set_balance(&mut self, owner: AccountId, balance: u32) {
            if balance == 0 {
                self.balances.remove(owner);
            } else {
                self.balances.insert(owner, &balance);
            }
        }
        fn recipient(to: AccountId) -> Result<(), PSP34Error> {
            if to == AccountId::from([0u8; 32]) {
                Err(PSP34Error::SafeTransferCheckFailed(String::from(
                    "Zero recipient",
                )))
            } else {
                Ok(())
            }
        }
        fn invalid_state() -> PSP34Error {
            PSP34Error::Custom(String::from("Counter or ownership invariant"))
        }
        fn invalid_metadata() -> PSP34Error {
            PSP34Error::Custom(String::from("Invalid metadata bounds or duplicate key"))
        }
    }

    #[cfg(test)]
    mod tests {
        use super::*;
        use ink::env::{test, DefaultEnvironment};
        fn accounts() -> test::DefaultAccounts<DefaultEnvironment> {
            test::default_accounts::<DefaultEnvironment>()
        }
        fn caller(account: AccountId) {
            test::set_caller::<DefaultEnvironment>(account);
        }
        fn contract() -> OasisPsp34 {
            OasisPsp34::new(b"Park".to_vec(), b"TREE".to_vec(), Vec::new(), Vec::new())
        }

        #[ink::test]
        fn collection_attributes_do_not_invent_a_token() {
            let token = contract();
            let id = token.collection_id();
            assert_eq!(
                token.get_attribute(id.clone(), b"name".to_vec()),
                Some(b"Park".to_vec())
            );
            assert_eq!(
                token.get_attribute(id.clone(), b"description".to_vec()),
                Some(Vec::new())
            );
            assert_eq!(token.owner_of(id), None);
            assert_eq!(token.total_supply(), 0);
        }
        #[ink::test]
        fn unauthorized_mint_and_invalid_metadata_leave_no_partial_state() {
            let a = accounts();
            let mut token = contract();
            let id = Id::U128(7);
            caller(a.bob);
            assert_eq!(
                token.mint(a.bob, id.clone(), Vec::new()),
                Err(PSP34Error::NotApproved)
            );
            caller(a.alice);
            let duplicate = vec![
                (b"name".to_vec(), b"a".to_vec()),
                (b"name".to_vec(), b"b".to_vec()),
            ];
            assert!(token.mint(a.bob, id.clone(), duplicate).is_err());
            assert_eq!(token.owner_of(id.clone()), None);
            assert_eq!(token.balance_of(a.bob), 0);
            assert_eq!(token.total_supply(), 0);
            assert_eq!(token.get_attribute(id, b"name".to_vec()), None);
        }
        #[ink::test]
        fn approved_transfer_clears_token_allowance_even_after_owner_round_trip() {
            let a = accounts();
            let mut token = contract();
            let id = Id::U128(7);
            assert_eq!(token.mint(a.bob, id.clone(), Vec::new()), Ok(()));
            caller(a.bob);
            assert_eq!(token.approve(a.charlie, Some(id.clone()), true), Ok(()));
            caller(a.charlie);
            assert_eq!(token.transfer(a.django, id.clone(), Vec::new()), Ok(()));
            assert!(!token.allowance(a.bob, a.charlie, Some(id.clone())));
            caller(a.django);
            assert_eq!(token.transfer(a.bob, id.clone(), Vec::new()), Ok(()));
            assert!(!token.allowance(a.bob, a.charlie, Some(id.clone())));
            caller(a.charlie);
            assert_eq!(
                token.transfer(a.eve, id.clone(), Vec::new()),
                Err(PSP34Error::NotApproved)
            );
            assert_eq!(token.owner_of(id), Some(a.bob));
            assert_eq!(token.balance_of(a.bob), 1);
            assert_eq!(token.balance_of(a.django), 0);
            assert_eq!(token.total_supply(), 1);
        }
        #[ink::test]
        fn burn_and_remint_cannot_revive_token_approval_or_old_metadata() {
            let a = accounts();
            let mut token = contract();
            let id = Id::U8(1);
            assert_eq!(
                token.mint(
                    a.bob,
                    id.clone(),
                    vec![(b"old".to_vec(), b"value".to_vec())]
                ),
                Ok(())
            );
            caller(a.bob);
            assert_eq!(token.approve(a.charlie, Some(id.clone()), true), Ok(()));
            assert_eq!(token.burn(id.clone()), Ok(()));
            assert_eq!(token.owner_of(id.clone()), None);
            assert_eq!(token.balance_of(a.bob), 0);
            assert_eq!(token.total_supply(), 0);
            assert_eq!(token.get_attribute(id.clone(), b"old".to_vec()), None);
            caller(a.alice);
            assert_eq!(token.mint(a.bob, id.clone(), Vec::new()), Ok(()));
            assert!(!token.allowance(a.bob, a.charlie, Some(id)));
        }
        #[ink::test]
        fn global_approval_is_owner_scoped_and_can_be_revoked() {
            let a = accounts();
            let mut token = contract();
            let id = Id::U16(1);
            assert_eq!(token.mint(a.bob, id.clone(), Vec::new()), Ok(()));
            caller(a.bob);
            assert_eq!(token.approve(a.charlie, None, true), Ok(()));
            assert!(token.allowance(a.bob, a.charlie, None));
            assert_eq!(token.approve(a.charlie, None, false), Ok(()));
            assert!(!token.allowance(a.bob, a.charlie, Some(id.clone())));
            caller(a.charlie);
            assert_eq!(token.burn(id), Err(PSP34Error::NotApproved));
            assert_eq!(token.total_supply(), 1);
        }
        #[ink::test]
        fn id_variants_and_full_u128_remain_distinct() {
            let a = accounts();
            let mut token = contract();
            for id in [Id::U8(1), Id::U16(1), Id::U128(u128::MAX)] {
                assert_eq!(token.mint(a.bob, id.clone(), Vec::new()), Ok(()));
                assert_eq!(token.owner_of(id), Some(a.bob));
            }
            assert_eq!(token.balance_of(a.bob), 3);
            assert_eq!(token.total_supply(), 3);
        }
        #[ink::test]
        fn zero_recipient_duplicate_and_self_approval_are_rejected() {
            let a = accounts();
            let mut token = contract();
            let id = Id::Bytes(vec![1, 2, 3]);
            assert!(token
                .mint(AccountId::from([0u8; 32]), id.clone(), Vec::new())
                .is_err());
            assert_eq!(token.mint(a.bob, id.clone(), Vec::new()), Ok(()));
            assert_eq!(
                token.mint(a.eve, id.clone(), Vec::new()),
                Err(PSP34Error::TokenExists)
            );
            caller(a.bob);
            assert_eq!(
                token.approve(a.bob, Some(id.clone()), true),
                Err(PSP34Error::SelfApprove)
            );
            assert!(token
                .transfer(AccountId::from([0u8; 32]), id.clone(), Vec::new())
                .is_err());
            assert_eq!(token.owner_of(id), Some(a.bob));
            assert_eq!(token.total_supply(), 1);
        }

        #[ink::test]
        fn mint_counter_exhaustion_does_not_create_partial_ownership() {
            let a = accounts();
            let mut token = contract();
            let id = Id::U128(7);
            token.supply = u128::MAX;
            assert!(token.mint(a.bob, id.clone(), Vec::new()).is_err());
            assert_eq!(token.owner_of(id.clone()), None);
            assert_eq!(token.balance_of(a.bob), 0);
            assert_eq!(token.epochs.get(&id), None);
            token.supply = 0;
            token.balances.insert(a.bob, &u32::MAX);
            assert!(token.mint(a.bob, id.clone(), Vec::new()).is_err());
            assert_eq!(token.owner_of(id.clone()), None);
            assert_eq!(token.total_supply(), 0);
            assert_eq!(token.epochs.get(id), None);
        }

        #[ink::test]
        fn transfer_counter_exhaustion_does_not_debit_or_move_the_token() {
            let a = accounts();
            let mut token = contract();
            let id = Id::U128(7);
            assert_eq!(token.mint(a.bob, id.clone(), Vec::new()), Ok(()));
            token.balances.insert(a.eve, &u32::MAX);
            caller(a.bob);
            assert!(token.transfer(a.eve, id.clone(), Vec::new()).is_err());
            assert_eq!(token.owner_of(id), Some(a.bob));
            assert_eq!(token.balance_of(a.bob), 1);
            assert_eq!(token.balance_of(a.eve), u32::MAX);
            assert_eq!(token.total_supply(), 1);
        }

        #[ink::test]
        fn burn_counter_failure_preserves_metadata_and_owner() {
            let a = accounts();
            let mut token = contract();
            let id = Id::U128(7);
            assert_eq!(
                token.mint(
                    a.bob,
                    id.clone(),
                    vec![(b"name".to_vec(), b"Tree".to_vec())]
                ),
                Ok(())
            );
            token.supply = 0;
            caller(a.bob);
            assert!(token.burn(id.clone()).is_err());
            assert_eq!(token.owner_of(id.clone()), Some(a.bob));
            assert_eq!(token.balance_of(a.bob), 1);
            assert_eq!(
                token.get_attribute(id, b"name".to_vec()),
                Some(b"Tree".to_vec())
            );
        }
    }
}
