#[zome_fn("hc_public")]
fn read_my_entry(address: Address) -> ZomeApiResult<Option<MyEntry>> {
    hdk::get_entry(&address)
}
