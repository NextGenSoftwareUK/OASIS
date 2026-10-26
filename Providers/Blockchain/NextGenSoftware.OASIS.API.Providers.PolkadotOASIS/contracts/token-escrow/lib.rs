#![cfg_attr(not(feature = "std"), no_std, no_main)]

// Explicit PSP22 custody ABI. A deposit is owned by its actual caller, never
// by an application GUID, token minter, historical recipient or service key.
#[ink::contract]
mod oasis_token_escrow {
    use ink::{
        env::call::{build_call, ExecutionInput, Selector},
        prelude::{string::String, vec::Vec},
        storage::Mapping,
    };

    #[derive(Debug, Clone, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(
        feature = "std",
        derive(scale_info::TypeInfo, ink::storage::traits::StorageLayout)
    )]
    pub struct Deposit {
        owner: AccountId,
        token: AccountId,
        amount: u128,
        released: bool,
    }

    #[derive(Debug, Clone, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub enum PSP22Error {
        Custom(String),
        InsufficientBalance,
        InsufficientAllowance,
        ZeroRecipientAddress,
        ZeroSenderAddress,
        SafeTransferCheckFailed(String),
    }

    #[derive(Debug, Clone, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub enum Error {
        InvalidDeposit,
        AlreadyExists,
        Missing,
        NotOwner,
        Released,
        TokenCallFailed,
    }

    #[ink(storage)]
    pub struct OasisTokenEscrow {
        deposits: Mapping<[u8; 32], Deposit>,
    }

    #[ink(event)]
    pub struct Locked {
        #[ink(topic)]
        id: [u8; 32],
        #[ink(topic)]
        owner: AccountId,
        token: AccountId,
        amount: u128,
    }
    #[ink(event)]
    pub struct Unlocked {
        #[ink(topic)]
        id: [u8; 32],
        #[ink(topic)]
        owner: AccountId,
        token: AccountId,
        amount: u128,
    }

    impl OasisTokenEscrow {
        #[ink(constructor, selector = 0x00000001)]
        pub fn new() -> Self {
            Self {
                deposits: Mapping::default(),
            }
        }

        #[ink(message, selector = 0x00000020)]
        pub fn lock(&mut self, id: [u8; 32], token: AccountId, amount: u128) -> Result<(), Error> {
            let caller = self.env().caller();
            if id == [0; 32]
                || amount == 0
                || token == AccountId::from([0; 32])
                || token == self.env().account_id()
            {
                return Err(Error::InvalidDeposit);
            }
            if self.deposits.contains(id) {
                return Err(Error::AlreadyExists);
            }
            let deposit = Deposit {
                owner: caller,
                token,
                amount,
                released: false,
            };
            let before = self.balance(token, self.env().account_id());
            let expected = before.checked_add(amount).unwrap_or_else(|| Self::revert());
            // Default call flags reject reentry. Any failure after an external
            // call reverts the entire transaction, including the token's writes.
            let transferred = build_call::<ink::env::DefaultEnvironment>()
                .call(token)
                .exec_input(
                    ExecutionInput::new(Selector::new(ink::selector_bytes!(
                        "PSP22::transfer_from"
                    )))
                    .push_arg(caller)
                    .push_arg(self.env().account_id())
                    .push_arg(amount)
                    .push_arg(Vec::<u8>::new()),
                )
                .returns::<Result<(), PSP22Error>>()
                .try_invoke();
            if !matches!(transferred, Ok(Ok(Ok(())))) {
                Self::revert();
            }
            if self.balance(token, self.env().account_id()) != expected {
                Self::revert();
            }
            self.deposits.insert(id, &deposit);
            self.env().emit_event(Locked {
                id,
                owner: caller,
                token,
                amount,
            });
            Ok(())
        }

        #[ink(message, selector = 0x00000021)]
        pub fn unlock(&mut self, id: [u8; 32]) -> Result<(), Error> {
            let mut deposit = self.deposits.get(id).ok_or(Error::Missing)?;
            if self.env().caller() != deposit.owner {
                return Err(Error::NotOwner);
            }
            if deposit.released {
                return Err(Error::Released);
            }
            let before = self.balance(deposit.token, deposit.owner);
            let expected = before
                .checked_add(deposit.amount)
                .unwrap_or_else(|| Self::revert());
            let transferred = build_call::<ink::env::DefaultEnvironment>()
                .call(deposit.token)
                .exec_input(
                    ExecutionInput::new(Selector::new(ink::selector_bytes!("PSP22::transfer")))
                        .push_arg(deposit.owner)
                        .push_arg(deposit.amount)
                        .push_arg(Vec::<u8>::new()),
                )
                .returns::<Result<(), PSP22Error>>()
                .try_invoke();
            if !matches!(transferred, Ok(Ok(Ok(())))) {
                Self::revert();
            }
            if self.balance(deposit.token, deposit.owner) != expected {
                Self::revert();
            }
            deposit.released = true;
            self.deposits.insert(id, &deposit); // Tombstone prevents ID reuse.
            self.env().emit_event(Unlocked {
                id,
                owner: deposit.owner,
                token: deposit.token,
                amount: deposit.amount,
            });
            Ok(())
        }

        #[ink(message, selector = 0x00000022)]
        pub fn deposit(&self, id: [u8; 32]) -> Option<Deposit> {
            self.deposits.get(id)
        }

        fn balance(&self, token: AccountId, owner: AccountId) -> u128 {
            match build_call::<ink::env::DefaultEnvironment>()
                .call(token)
                .exec_input(
                    ExecutionInput::new(Selector::new(ink::selector_bytes!("PSP22::balance_of")))
                        .push_arg(owner),
                )
                .returns::<u128>()
                .try_invoke()
            {
                Ok(Ok(value)) => value,
                _ => Self::revert(),
            }
        }
        fn revert() -> ! {
            ink::env::return_value(ink::env::ReturnFlags::REVERT, &Self::failure_result())
        }
        fn failure_result() -> ink::MessageResult<Result<(), Error>> {
            Ok(Err(Error::TokenCallFailed))
        }

        #[cfg(test)]
        fn fixture(&mut self, id: [u8; 32], owner: AccountId, released: bool) {
            self.deposits.insert(
                id,
                &Deposit {
                    owner,
                    token: AccountId::from([9; 32]),
                    amount: 42,
                    released,
                },
            );
        }
    }

    #[cfg(test)]
    mod tests {
        use super::*;
        #[ink::test]
        fn manual_revert_preserves_the_ink_language_result_wrapper() {
            use scale::Encode;
            assert_eq!(OasisTokenEscrow::failure_result().encode(), vec![0, 1, 5]);
        }
        #[ink::test]
        fn invalid_deposits_do_not_call_tokens_or_create_state() {
            let mut escrow = OasisTokenEscrow::new();
            let token = AccountId::from([9; 32]);
            assert_eq!(escrow.lock([0; 32], token, 1), Err(Error::InvalidDeposit));
            assert_eq!(escrow.lock([1; 32], token, 0), Err(Error::InvalidDeposit));
            assert_eq!(
                escrow.lock([1; 32], AccountId::from([0; 32]), 1),
                Err(Error::InvalidDeposit)
            );
            assert_eq!(escrow.deposit([1; 32]), None);
        }
        #[ink::test]
        fn foreign_owner_cannot_release_or_replace_deposit() {
            let mut escrow = OasisTokenEscrow::new();
            let accounts = ink::env::test::default_accounts::<ink::env::DefaultEnvironment>();
            escrow.fixture([1; 32], accounts.bob, false);
            assert_eq!(escrow.unlock([1; 32]), Err(Error::NotOwner));
            assert_eq!(
                escrow.lock([1; 32], AccountId::from([9; 32]), 99),
                Err(Error::AlreadyExists)
            );
            let retained = escrow.deposit([1; 32]).unwrap();
            assert_eq!(retained.amount, 42);
            assert!(!retained.released);
        }
        #[ink::test]
        fn released_id_is_a_permanent_tombstone() {
            let mut escrow = OasisTokenEscrow::new();
            let caller = ink::env::caller::<ink::env::DefaultEnvironment>();
            escrow.fixture([2; 32], caller, true);
            assert_eq!(escrow.unlock([2; 32]), Err(Error::Released));
            assert_eq!(
                escrow.lock([2; 32], AccountId::from([9; 32]), 1),
                Err(Error::AlreadyExists)
            );
            assert_eq!(escrow.unlock([3; 32]), Err(Error::Missing));
        }
    }
}
