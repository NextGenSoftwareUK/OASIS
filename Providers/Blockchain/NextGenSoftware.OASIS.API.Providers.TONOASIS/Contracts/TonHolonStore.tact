// Native TVM storage. Logical keys/values must be protected by the provider's
// shared private-storage layer before submission to a public network.
message(0x434f5350) PutRecord {
    key: Int as uint256;
    value: Cell;
}

message(0x434f5344) DeleteRecord {
    key: Int as uint256;
}

contract TonHolonStore {
    owner: Address;
    records: map<Int as uint256, Cell>;

    init(owner: Address) {
        self.owner = owner;
        self.records = emptyMap();
    }

    // Empty deployment messages cannot change the owner or existing records.
    receive() {}

    receive(message: PutRecord) {
        require(sender() == self.owner, "OWNER_ONLY");
        self.records.set(message.key, message.value);
    }

    receive(message: DeleteRecord) {
        require(sender() == self.owner, "OWNER_ONLY");
        self.records.del(message.key);
    }

    get fun record(key: Int): Cell? {
        return self.records.get(key);
    }

    get fun storageOwner(): Address {
        return self.owner;
    }

    get fun storedRecords(): map<Int as uint256, Cell> {
        return self.records;
    }
}
