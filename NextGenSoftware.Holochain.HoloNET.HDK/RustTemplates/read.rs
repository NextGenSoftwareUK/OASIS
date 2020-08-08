#[zome_fn("hc_public")]
fn get_my_entry(address: Address) -> ZomeApiResult<Option<MyEntry>> {
    hdk::get_entry(&address)
}
