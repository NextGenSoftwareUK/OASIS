#[zome_fn("hc_public")]
fn create_my_entry(holon: MyHolon) -> ZomeApiResult<Address> {
    let holon = Entry::App("my_holon".into(), holon.into());
    let address = hdk::commit_entry(&entry)?;
    Ok(address)
}
