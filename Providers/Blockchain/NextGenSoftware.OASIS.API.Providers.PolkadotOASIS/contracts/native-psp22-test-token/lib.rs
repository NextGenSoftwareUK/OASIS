#![cfg_attr(not(feature = "std"), no_std, no_main)]

// Real native acceptance asset, not a production token. Implements the PSP22
// calls used by custody, with explicit owner-controlled failure injection.
// Never selected or deployed automatically by the shipping provider.
#[ink::contract]
mod native_psp22_test_token {
    use ink::{
        prelude::{string::String, vec::Vec},
        storage::Mapping,
    };

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
    #[ink(storage)]
    pub struct NativePsp22TestToken {
        owner: AccountId,
        supply: u128,
        balances: Mapping<AccountId, u128>,
        allowances: Mapping<(AccountId, AccountId), u128>,
        failure: u8,
    }
    #[ink(event)]
    pub struct Transfer {
        #[ink(topic)]
        from: Option<AccountId>,
        #[ink(topic)]
        to: Option<AccountId>,
        value: u128,
    }
    #[ink(event)]
    pub struct Approval {
        #[ink(topic)]
        owner: AccountId,
        #[ink(topic)]
        spender: AccountId,
        value: u128,
    }

    impl NativePsp22TestToken {
        #[ink(constructor, selector = 0x00000001)]
        pub fn new(supply: u128) -> Self {
            let owner = Self::env().caller();
            let mut token = Self {
                owner,
                supply,
                balances: Mapping::default(),
                allowances: Mapping::default(),
                failure: 0,
            };
            token.balances.insert(owner, &supply);
            token.env().emit_event(Transfer {
                from: None,
                to: Some(owner),
                value: supply,
            });
            token
        }
        #[ink(message, selector = 0x162df8c2)]
        pub fn total_supply(&self) -> u128 {
            self.supply
        }
        #[ink(message, selector = 0x6568382f)]
        pub fn balance_of(&self, owner: AccountId) -> u128 {
            self.balances.get(owner).unwrap_or(0)
        }
        #[ink(message, selector = 0x4d47d921)]
        pub fn allowance(&self, owner: AccountId, spender: AccountId) -> u128 {
            self.allowances.get((owner, spender)).unwrap_or(0)
        }
        #[ink(message, selector = 0xb20f1bbd)]
        pub fn approve(&mut self, spender: AccountId, value: u128) -> Result<(), PSP22Error> {
            let owner = self.env().caller();
            if spender == AccountId::from([0; 32]) {
                return Err(PSP22Error::ZeroRecipientAddress);
            }
            if owner == AccountId::from([0; 32]) {
                return Err(PSP22Error::ZeroSenderAddress);
            }
            self.allowances.insert((owner, spender), &value);
            self.env().emit_event(Approval {
                owner,
                spender,
                value,
            });
            Ok(())
        }
        #[ink(message, selector = 0x96d6b57a)]
        pub fn increase_allowance(
            &mut self,
            spender: AccountId,
            delta: u128,
        ) -> Result<(), PSP22Error> {
            let value = self
                .allowance(self.env().caller(), spender)
                .checked_add(delta)
                .ok_or_else(|| PSP22Error::Custom(String::from("Allowance overflow")))?;
            self.approve(spender, value)
        }
        #[ink(message, selector = 0xfecb57d5)]
        pub fn decrease_allowance(
            &mut self,
            spender: AccountId,
            delta: u128,
        ) -> Result<(), PSP22Error> {
            let value = self
                .allowance(self.env().caller(), spender)
                .checked_sub(delta)
                .ok_or(PSP22Error::InsufficientAllowance)?;
            self.approve(spender, value)
        }
        #[ink(message, selector = 0xdb20f9f5)]
        pub fn transfer(
            &mut self,
            to: AccountId,
            value: u128,
            data: Vec<u8>,
        ) -> Result<(), PSP22Error> {
            if !data.is_empty() {
                return Err(PSP22Error::Custom(String::from(
                    "Acceptance asset requires empty receiver data",
                )));
            }
            self.move_tokens(self.env().caller(), to, value)
        }
        #[ink(message, selector = 0x54b3c76e)]
        pub fn transfer_from(
            &mut self,
            from: AccountId,
            to: AccountId,
            value: u128,
            data: Vec<u8>,
        ) -> Result<(), PSP22Error> {
            if !data.is_empty() {
                return Err(PSP22Error::Custom(String::from(
                    "Acceptance asset requires empty receiver data",
                )));
            }
            let spender = self.env().caller();
            let remaining = self
                .allowance(from, spender)
                .checked_sub(value)
                .ok_or(PSP22Error::InsufficientAllowance)?;
            self.move_tokens(from, to, value)?;
            self.allowances.insert((from, spender), &remaining);
            self.env().emit_event(Approval {
                owner: from,
                spender,
                value: remaining,
            });
            Ok(())
        }
        #[ink(message, selector = 0x3d261bd4)]
        pub fn token_name(&self) -> Option<String> {
            Some(String::from("OASIS native custody acceptance"))
        }
        #[ink(message, selector = 0x34205be5)]
        pub fn token_symbol(&self) -> Option<String> {
            Some(String::from("TEST"))
        }
        #[ink(message, selector = 0x7271b782)]
        pub fn token_decimals(&self) -> u8 {
            12
        }
        #[ink(message, selector = 0x00000030)]
        pub fn set_failure(&mut self, mode: u8) -> Result<(), PSP22Error> {
            if self.env().caller() != self.owner || mode > 2 {
                return Err(PSP22Error::Custom(String::from("Invalid fault owner/mode")));
            }
            self.failure = mode;
            Ok(())
        }
        fn move_tokens(
            &mut self,
            from: AccountId,
            to: AccountId,
            value: u128,
        ) -> Result<(), PSP22Error> {
            if from == AccountId::from([0; 32]) {
                return Err(PSP22Error::ZeroSenderAddress);
            }
            if to == AccountId::from([0; 32]) {
                return Err(PSP22Error::ZeroRecipientAddress);
            }
            if self.failure == 1 {
                return Err(PSP22Error::Custom(String::from(
                    "Injected native token error",
                )));
            }
            // Mode2 deliberately transfers one unit less while returning Ok.
            // Escrow must revert this nested write and any allowance mutation.
            let moved = if self.failure == 2 {
                value.checked_sub(1).unwrap_or(0)
            } else {
                value
            };
            let remaining = self
                .balance_of(from)
                .checked_sub(moved)
                .ok_or(PSP22Error::InsufficientBalance)?;
            let receiving = if from == to {
                self.balance_of(to)
            } else {
                self.balance_of(to)
                    .checked_add(moved)
                    .ok_or_else(|| PSP22Error::Custom(String::from("Balance overflow")))?
            };
            if from != to {
                self.balances.insert(from, &remaining);
                self.balances.insert(to, &receiving);
            }
            self.env().emit_event(Transfer {
                from: Some(from),
                to: Some(to),
                value: moved,
            });
            Ok(())
        }
    }
    #[cfg(test)]
    mod tests {
        use super::*;
        #[ink::test]
        fn exact_large_balances_and_allowances_move_atomically() {
            let accounts = ink::env::test::default_accounts::<ink::env::DefaultEnvironment>();
            let amount = 18_446_744_073_709_551_620u128;
            let mut token = NativePsp22TestToken::new(amount + 10);
            assert_eq!(token.approve(accounts.bob, amount), Ok(()));
            ink::env::test::set_caller::<ink::env::DefaultEnvironment>(accounts.bob);
            assert_eq!(
                token.transfer_from(accounts.alice, accounts.charlie, amount, Vec::new()),
                Ok(())
            );
            assert_eq!(token.balance_of(accounts.charlie), amount);
            assert_eq!(token.balance_of(accounts.alice), 10);
            assert_eq!(token.allowance(accounts.alice, accounts.bob), 0);
            assert_eq!(token.total_supply(), amount + 10);
        }
        #[ink::test]
        fn failed_transfers_do_not_consume_allowance_or_balances() {
            let accounts = ink::env::test::default_accounts::<ink::env::DefaultEnvironment>();
            let mut token = NativePsp22TestToken::new(10);
            token.approve(accounts.bob, 20).unwrap();
            ink::env::test::set_caller::<ink::env::DefaultEnvironment>(accounts.bob);
            assert_eq!(
                token.transfer_from(accounts.alice, accounts.charlie, 20, Vec::new()),
                Err(PSP22Error::InsufficientBalance)
            );
            assert_eq!(token.allowance(accounts.alice, accounts.bob), 20);
            assert_eq!(token.balance_of(accounts.alice), 10);
            assert_eq!(
                token.set_failure(1),
                Err(PSP22Error::Custom(String::from("Invalid fault owner/mode")))
            );
        }
        #[ink::test]
        fn explicit_fault_controls_really_change_native_transfer_behavior() {
            let accounts = ink::env::test::default_accounts::<ink::env::DefaultEnvironment>();
            let mut token = NativePsp22TestToken::new(10);
            token.set_failure(1).unwrap();
            assert!(token.transfer(accounts.bob, 5, Vec::new()).is_err());
            assert_eq!(token.balance_of(accounts.alice), 10);
            token.set_failure(2).unwrap();
            token.transfer(accounts.bob, 5, Vec::new()).unwrap();
            assert_eq!(token.balance_of(accounts.bob), 4);
            token.set_failure(0).unwrap();
            token.transfer(accounts.bob, 5, Vec::new()).unwrap();
            assert_eq!(token.balance_of(accounts.bob), 9);
        }
    }
}
