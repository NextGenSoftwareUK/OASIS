// OASIS key-value record store. Anyone may read; only the holder of the Admin resource (saved in the deploying
// account's storage) may write or remove. Values are opaque strings: FlowOASIS stores encrypted record envelopes.
access(all) contract OasisKv {

    access(all) entitlement Write

    access(all) event Put(key: String)
    access(all) event Removed(key: String)

    access(self) let records: {String: String}

    access(all) let AdminStoragePath: StoragePath

    access(all) resource Admin {
        access(Write) fun put(key: String, value: String) {
            pre { key.length > 0: "A record key is required." }
            OasisKv.records[key] = value
            emit Put(key: key)
        }

        access(Write) fun remove(key: String) {
            if OasisKv.records.remove(key: key) != nil {
                emit Removed(key: key)
            }
        }
    }

    access(all) view fun get(key: String): String? {
        return self.records[key]
    }

    access(all) fun keys(prefix: String): [String] {
        let matches: [String] = []
        for key in self.records.keys {
            if key.length >= prefix.length && key.slice(from: 0, upTo: prefix.length) == prefix {
                matches.append(key)
            }
        }
        return matches
    }

    init() {
        self.records = {}
        self.AdminStoragePath = /storage/oasisKvAdmin
        self.account.storage.save(<-create Admin(), to: self.AdminStoragePath)
    }
}
