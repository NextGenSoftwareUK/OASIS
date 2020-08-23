impl HolochainEntry for MyEntry {
    fn entry_type() -> String {
        String::from("my_holon")
    }
}

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct MyHolon {
}