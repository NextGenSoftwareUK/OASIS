#[zome_fn("hc_public")]
fn update_my_entry(updated_entry: MyEntry, address: &Address) -> ZomeApiResult<Address> {
    let mut my_entry: MyEntry = hdk::utils::get_as_type(address.clone())?;
    
    //#CopyFields//
    hdk::update_entry(my_entry.entry(), address)
}
