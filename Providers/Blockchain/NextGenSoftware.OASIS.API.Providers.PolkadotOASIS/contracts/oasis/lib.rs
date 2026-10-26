#![cfg_attr(not(feature = "std"), no_std, no_main)]

/// Opaque encrypted record cells. Canonical OASIS serialization, encryption,
/// query indexes and history belong to the shared provider layer, not this ABI.
#[ink::contract]
mod oasis {
    use ink::{prelude::vec::Vec, storage::Mapping};

    #[derive(Debug, Clone, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(
        feature = "std",
        derive(scale_info::TypeInfo, ink::storage::traits::StorageLayout)
    )]
    pub struct Cell {
        pub value: Vec<u8>,
        pub revision: u64,
    }

    #[derive(Debug, Clone, Copy, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub enum Expected {
        Any,
        Missing,
        Revision(u64),
    }

    #[derive(Debug, Clone, Copy, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub enum Error {
        Unauthorized,
        InvalidKey,
        ValueTooLarge,
        Conflict,
        Exhausted,
        InvalidPage,
    }

    #[derive(Debug, PartialEq, Eq, scale::Encode, scale::Decode)]
    #[cfg_attr(feature = "std", derive(scale_info::TypeInfo))]
    pub struct Page {
        pub generation: u64,
        pub keys: Vec<Vec<u8>>,
        pub next: Option<u64>,
    }

    #[ink(storage)]
    pub struct Oasis {
        owner: AccountId,
        cells: Mapping<Vec<u8>, Cell>,
        slots: Mapping<u64, Vec<u8>>,
        positions: Mapping<Vec<u8>, u64>,
        count: u64,
        generation: u64,
    }

    #[ink(event)]
    pub struct Mutation {
        #[ink(topic)]
        operation: [u8; 32],
        key: Vec<u8>,
        revision: u64,
        deleted: bool,
    }

    impl Oasis {
        #[ink(constructor, selector = 0x00000001)]
        pub fn new() -> Self {
            Self {
                owner: Self::env().caller(),
                cells: Mapping::default(),
                slots: Mapping::default(),
                positions: Mapping::default(),
                count: 0,
                generation: 0,
            }
        }

        fn validate(key: &[u8]) -> Result<(), Error> {
            if key.is_empty() || key.len() > 1024 {
                Err(Error::InvalidKey)
            } else {
                Ok(())
            }
        }

        fn authorize(&self, key: &[u8], expected: Expected) -> Result<Option<Cell>, Error> {
            if self.env().caller() != self.owner {
                return Err(Error::Unauthorized);
            }
            Self::validate(key)?;
            let cell = self.cells.get(key.to_vec());
            let matches = match expected {
                Expected::Any => true,
                Expected::Missing => cell.is_none(),
                Expected::Revision(revision) => cell.as_ref().map(|v| v.revision) == Some(revision),
            };
            if matches {
                Ok(cell)
            } else {
                Err(Error::Conflict)
            }
        }

        #[ink(message, selector = 0x00000002)]
        pub fn get(&self, key: Vec<u8>) -> Result<Option<Cell>, Error> {
            Self::validate(&key)?;
            Ok(self.cells.get(key))
        }

        /// The 8 KiB cell bound keeps encoded cells/messages within ink's
        /// default 16 KiB static buffer. Larger records require shared framing.
        #[ink(message, selector = 0x00000003)]
        pub fn put(
            &mut self,
            key: Vec<u8>,
            value: Vec<u8>,
            expected: Expected,
            operation: [u8; 32],
        ) -> Result<u64, Error> {
            let previous = self.authorize(&key, expected)?;
            if value.len() > 8192 {
                return Err(Error::ValueTooLarge);
            }
            let revision = self.generation.checked_add(1).ok_or(Error::Exhausted)?;
            let count = if previous.is_none() {
                self.count.checked_add(1).ok_or(Error::Exhausted)?
            } else {
                self.count
            };
            // Every fallible admission check precedes the first storage write.
            if previous.is_none() {
                self.slots.insert(self.count, &key);
                self.positions.insert(&key, &self.count);
            }
            self.cells.insert(&key, &Cell { value, revision });
            self.count = count;
            self.generation = revision;
            self.env().emit_event(Mutation {
                operation,
                key,
                revision,
                deleted: false,
            });
            Ok(revision)
        }

        #[ink(message, selector = 0x00000004)]
        pub fn delete(
            &mut self,
            key: Vec<u8>,
            expected: Expected,
            operation: [u8; 32],
        ) -> Result<Option<u64>, Error> {
            if self.authorize(&key, expected)?.is_none() {
                return Ok(None);
            }
            let revision = self.generation.checked_add(1).ok_or(Error::Exhausted)?;
            let position = self.positions.get(&key).expect("cell has index");
            let last = self
                .count
                .checked_sub(1)
                .expect("cell implies nonempty index");
            let last_key = self.slots.get(last).expect("dense index");
            if position != last {
                self.slots.insert(position, &last_key);
                self.positions.insert(&last_key, &position);
            }
            self.slots.remove(last);
            self.positions.remove(&key);
            self.cells.remove(&key);
            self.count = last;
            self.generation = revision;
            self.env().emit_event(Mutation {
                operation,
                key,
                revision,
                deleted: true,
            });
            Ok(Some(revision))
        }

        /// Scan at most eight slots: even maximal keys fit the default buffer.
        /// Clients must pin one finalized block for a multi-page read.
        #[ink(message, selector = 0x00000005)]
        pub fn keys(&self, prefix: Vec<u8>, start: u64, limit: u8) -> Result<Page, Error> {
            if prefix.len() > 1024 || start > self.count || limit == 0 || limit > 8 {
                return Err(Error::InvalidPage);
            }
            let end = start.saturating_add(u64::from(limit)).min(self.count);
            let mut keys = Vec::new();
            for slot in start..end {
                let key = self.slots.get(slot).expect("dense index");
                if key.starts_with(&prefix) {
                    keys.push(key);
                }
            }
            Ok(Page {
                generation: self.generation,
                keys,
                next: if end < self.count { Some(end) } else { None },
            })
        }
    }

    #[cfg(test)]
    mod tests {
        use super::*;
        use scale::Encode;
        fn key(n: u8) -> Vec<u8> {
            ink::prelude::vec![n]
        }

        #[ink::test]
        fn scale_vectors_match_public_sdk_codec() {
            let mut put = ink::prelude::vec![4, 0xab, 4, 0xcd, 2, 2, 1];
            put.extend_from_slice(&[0; 6]);
            put.extend_from_slice(&[0; 32]);
            assert_eq!(
                (key(0xab), key(0xcd), Expected::Revision(258), [0u8; 32]).encode(),
                put
            );
            let output: ink::MessageResult<Result<Option<Cell>, Error>> = Ok(Ok(Some(Cell {
                value: key(0xab),
                revision: 258,
            })));
            assert_eq!(
                output.encode(),
                ink::prelude::vec![0, 0, 1, 4, 0xab, 2, 1, 0, 0, 0, 0, 0, 0]
            );
            let language: ink::MessageResult<Result<u64, Error>> =
                Err(ink::LangError::CouldNotReadInput);
            assert_eq!(language.encode(), ink::prelude::vec![1, 1]);
        }

        #[ink::test]
        fn conditional_writes_and_aba() {
            let mut db = Oasis::new();
            assert_eq!(db.put(key(1), key(2), Expected::Missing, [1; 32]), Ok(1));
            assert_eq!(
                db.put(key(1), key(3), Expected::Missing, [2; 32]),
                Err(Error::Conflict)
            );
            assert_eq!(
                db.delete(key(1), Expected::Revision(1), [3; 32]),
                Ok(Some(2))
            );
            assert_eq!(db.put(key(1), key(4), Expected::Missing, [4; 32]), Ok(3));
            assert_eq!(
                db.delete(key(1), Expected::Revision(1), [5; 32]),
                Err(Error::Conflict)
            );
            assert_eq!(db.get(key(1)).unwrap().unwrap().value, key(4));
        }

        #[ink::test]
        fn deletion_keeps_dense_pages() {
            let mut db = Oasis::new();
            for n in 1..=4 {
                db.put(key(n), key(n), Expected::Missing, [n; 32]).unwrap();
            }
            db.delete(key(2), Expected::Any, [5; 32]).unwrap();
            let first = db.keys(Vec::new(), 0, 2).unwrap();
            assert_eq!(first.keys, ink::prelude::vec![key(1), key(4)]);
            assert_eq!(first.next, Some(2));
            assert_eq!(
                db.keys(Vec::new(), 2, 2).unwrap().keys,
                ink::prelude::vec![key(3)]
            );
            assert_eq!(
                db.keys(key(3), 0, 8).unwrap().keys,
                ink::prelude::vec![key(3)]
            );
        }

        #[ink::test]
        fn admission_has_no_partial_writes() {
            let mut db = Oasis::new();
            assert_eq!(
                db.put(Vec::new(), key(1), Expected::Any, [0; 32]),
                Err(Error::InvalidKey)
            );
            assert_eq!(
                db.put(key(1), ink::prelude::vec![0;8193], Expected::Any, [0; 32]),
                Err(Error::ValueTooLarge)
            );
            db.generation = u64::MAX;
            assert_eq!(
                db.put(key(1), key(2), Expected::Any, [0; 32]),
                Err(Error::Exhausted)
            );
            assert_eq!(db.count, 0);
            assert_eq!(db.get(key(1)), Ok(None));
        }

        #[ink::test]
        fn only_owner_can_mutate() {
            let mut db = Oasis::new();
            let accounts = ink::env::test::default_accounts::<ink::env::DefaultEnvironment>();
            ink::env::test::set_caller::<ink::env::DefaultEnvironment>(accounts.bob);
            assert_eq!(
                db.put(key(1), key(2), Expected::Any, [0; 32]),
                Err(Error::Unauthorized)
            );
            assert_eq!(
                db.delete(key(1), Expected::Any, [0; 32]),
                Err(Error::Unauthorized)
            );
        }

        #[ink::test]
        fn missing_delete_and_invalid_pages_are_read_only() {
            let mut db = Oasis::new();
            assert_eq!(db.delete(key(1), Expected::Missing, [0; 32]), Ok(None));
            assert_eq!(
                db.delete(key(1), Expected::Revision(1), [0; 32]),
                Err(Error::Conflict)
            );
            assert_eq!(db.keys(Vec::new(), 0, 0), Err(Error::InvalidPage));
            assert_eq!(db.keys(Vec::new(), 0, 9), Err(Error::InvalidPage));
            assert_eq!(db.keys(Vec::new(), 1, 1), Err(Error::InvalidPage));
            assert_eq!(db.generation, 0);
            assert_eq!(ink::env::test::recorded_events().count(), 0);
        }

        #[ink::test]
        fn delete_overflow_preserves_record_and_index() {
            let mut db = Oasis::new();
            db.put(key(1), key(2), Expected::Missing, [0; 32]).unwrap();
            db.generation = u64::MAX;
            assert_eq!(
                db.delete(key(1), Expected::Any, [0; 32]),
                Err(Error::Exhausted)
            );
            assert_eq!(db.get(key(1)).unwrap().unwrap().revision, 1);
            assert_eq!(
                db.keys(Vec::new(), 0, 8).unwrap().keys,
                ink::prelude::vec![key(1)]
            );
        }
    }
}
