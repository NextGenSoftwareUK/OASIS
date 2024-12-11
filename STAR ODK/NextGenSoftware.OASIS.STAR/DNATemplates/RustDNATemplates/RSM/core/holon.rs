impl HolochainEntry for Holon {
    fn entry_type() -> String {
        String::from("{holon}")
    }
}

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct Holon {
}