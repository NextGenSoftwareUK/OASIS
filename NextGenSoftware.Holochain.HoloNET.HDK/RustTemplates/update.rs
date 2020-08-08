#[zome_fn("hc_public")]
fn update_my_entry(entry: MyEntry) -> ZomeApiResult<Address> 
{
    hdk::update_entry(
            Entry::App("course".into(), course_entry.into()),
            course_address,
        )
}
