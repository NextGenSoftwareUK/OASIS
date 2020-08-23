/*
#[zome_fn("hc_public")]
fn update_my_entry(address: Address, my_updated_entry MyEntry) -> ZomeApiResult<Address> 
{
    let mut my_entry: MyEntry = hdk::utils::get_as_type(address.clone())?;
    my_entry.id = my_updated_entry.id;
    my_entry. = name;
    my_entry.url = url;
    hdk::update_entry(my_entry.entry(), &content_address)
}
*/

/*
#[zome_fn("hc_public")]
fn update_my_entry(entry: MyEntry) -> ZomeApiResult<Address> 
{
    hdk::update_entry(
            Entry::App("my_entry".into(), course_entry.into()),
            course_address,
        )
}*/

pub fn update(updated_entry: MyHolon, address: &Address) -> ZomeApiResult<Address> {
    let mut my_holon: MyHolon = hdk::utils::get_as_type(address.clone())?;

  // my_entry.title = title;

    hdk::update_entry(my_holon.entry(), module_address)
}
