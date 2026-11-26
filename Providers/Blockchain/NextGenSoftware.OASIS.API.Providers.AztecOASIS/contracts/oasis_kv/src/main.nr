use aztec::macros::aztec;

/// OASIS record commitments on Aztec public state.
///
/// Every record is an encrypted envelope held off-chain (raw-block Kubo, so its CIDv1 is derived from the envelope's
/// SHA-256). Under the record id (the first 31 bytes of SHA-256(key) as a Field) the contract keeps the envelope hash
/// as two 128-bit halves. A zero hash means the record does not exist (never written or removed). `index` lists every
/// record id ever written, once, so the owner's records can be enumerated without an indexer. Only the owner set at
/// deployment may write.
#[aztec]
pub contract OasisKv {
    use aztec::{
        macros::{functions::{external, initializer, view}, storage::storage},
        protocol::address::AztecAddress,
        state_vars::{Map, PublicImmutable, PublicMutable},
    };

    #[storage]
    struct Storage<Context> {
        owner: PublicImmutable<AztecAddress, Context>,
        hash_hi: Map<Field, PublicMutable<Field, Context>, Context>,
        hash_lo: Map<Field, PublicMutable<Field, Context>, Context>,
        known: Map<Field, PublicMutable<bool, Context>, Context>,
        count: PublicMutable<u32, Context>,
        index: Map<Field, PublicMutable<Field, Context>, Context>,
    }

    #[external("public")]
    #[initializer]
    fn constructor(owner: AztecAddress) {
        self.storage.owner.initialize(owner);
    }

    #[external("public")]
    fn put(id: Field, hi: Field, lo: Field) {
        assert(self.storage.owner.read().eq(self.msg_sender()), "caller is not the record owner");
        assert((hi != 0) | (lo != 0), "an envelope hash cannot be zero");
        if !self.storage.known.at(id).read() {
            let position = self.storage.count.read();
            self.storage.index.at(position as Field).write(id);
            self.storage.count.write(position + 1);
            self.storage.known.at(id).write(true);
        }
        self.storage.hash_hi.at(id).write(hi);
        self.storage.hash_lo.at(id).write(lo);
    }

    #[external("public")]
    fn remove(id: Field) {
        assert(self.storage.owner.read().eq(self.msg_sender()), "caller is not the record owner");
        self.storage.hash_hi.at(id).write(0);
        self.storage.hash_lo.at(id).write(0);
    }

    #[external("public")]
    #[view]
    fn get(id: Field) -> pub [Field; 2] {
        [self.storage.hash_hi.at(id).read(), self.storage.hash_lo.at(id).read()]
    }

    #[external("public")]
    #[view]
    fn record_count() -> pub u32 {
        self.storage.count.read()
    }

    #[external("public")]
    #[view]
    fn record_at(position: u32) -> pub Field {
        self.storage.index.at(position as Field).read()
    }

    #[external("public")]
    #[view]
    fn get_owner() -> pub AztecAddress {
        self.storage.owner.read()
    }
}
