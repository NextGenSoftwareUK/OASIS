#[zome_fn("hc_public")]
fn read_my_entry(address: Address) -> ZomeApiResult<MyEntry> {
    //hdk::get_entry(&address)
    hdk::utils::get_as_type(address)
}