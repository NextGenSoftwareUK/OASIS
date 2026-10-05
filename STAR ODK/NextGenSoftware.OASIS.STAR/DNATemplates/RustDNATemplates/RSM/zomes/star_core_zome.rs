#[zome_fn("hc_public")]
fn create_holon(holon: Holon) -> ZomeApiResult<Address> {
    let holon = Entry::App("{holon}".into(), holon.into());
    let address = hdk::commit_entry(&holon)?;
    Ok(address)
}
