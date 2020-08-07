#[zome_fn("hc_public")]
fn get_my_entry(address: Address) -> ZomeApiResult<Option<Entry>> {
    hdk::get_entry(&address)
}
