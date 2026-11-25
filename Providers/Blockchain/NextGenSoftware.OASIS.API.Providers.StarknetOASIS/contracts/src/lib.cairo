// OASIS record index: each caller owns its own keys. A value is the (key, envelope CID, envelope SHA-256) commitment
// for an encrypted record held off-chain; the record content never touches the chain.
#[starknet::interface]
pub trait IOasisKv<TContractState> {
    fn put(ref self: TContractState, key: felt252, value: ByteArray);
    fn remove(ref self: TContractState, key: felt252);
    fn get(self: @TContractState, owner: starknet::ContractAddress, key: felt252) -> ByteArray;
    fn keys(self: @TContractState, owner: starknet::ContractAddress) -> Array<felt252>;
}

#[starknet::contract]
pub mod OasisKv {
    use starknet::ContractAddress;
    use starknet::get_caller_address;
    use starknet::storage::{
        Map, StorageMapReadAccess, StorageMapWriteAccess, StoragePathEntry, StoragePointerReadAccess,
        StoragePointerWriteAccess, Vec, VecTrait, MutableVecTrait,
    };

    #[storage]
    struct Storage {
        values: Map<(ContractAddress, felt252), ByteArray>,
        present: Map<(ContractAddress, felt252), bool>,
        key_list: Map<ContractAddress, Vec<felt252>>,
        listed: Map<(ContractAddress, felt252), bool>,
    }

    #[event]
    #[derive(Drop, starknet::Event)]
    pub enum Event {
        Put: Put,
        Removed: Removed,
    }

    #[derive(Drop, starknet::Event)]
    pub struct Put {
        #[key]
        pub owner: ContractAddress,
        #[key]
        pub key: felt252,
    }

    #[derive(Drop, starknet::Event)]
    pub struct Removed {
        #[key]
        pub owner: ContractAddress,
        #[key]
        pub key: felt252,
    }

    #[abi(embed_v0)]
    impl OasisKvImpl of super::IOasisKv<ContractState> {
        fn put(ref self: ContractState, key: felt252, value: ByteArray) {
            assert(value.len() > 0, 'empty value');
            let owner = get_caller_address();
            self.values.write((owner, key), value);
            self.present.write((owner, key), true);
            if !self.listed.read((owner, key)) {
                self.listed.write((owner, key), true);
                self.key_list.entry(owner).push(key);
            }
            self.emit(Put { owner, key });
        }

        fn remove(ref self: ContractState, key: felt252) {
            let owner = get_caller_address();
            assert(self.present.read((owner, key)), 'missing key');
            self.values.write((owner, key), "");
            self.present.write((owner, key), false);
            self.emit(Removed { owner, key });
        }

        fn get(self: @ContractState, owner: ContractAddress, key: felt252) -> ByteArray {
            self.values.read((owner, key))
        }

        fn keys(self: @ContractState, owner: ContractAddress) -> Array<felt252> {
            let mut out = array![];
            let list = self.key_list.entry(owner);
            let mut i = 0;
            while i < list.len() {
                let key = list.at(i).read();
                if self.present.read((owner, key)) {
                    out.append(key);
                }
                i += 1;
            };
            out
        }
    }
}
