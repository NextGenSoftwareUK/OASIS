impl HolochainEntry for MyEntry {
    fn entry_type() -> String {
        String::from("my_entry")
    }
}

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct MyEntry {
}