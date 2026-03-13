#[zome_fn("hc_public")]
fn list() -> ZomeApiResult<Vec<Address>> {
   let addresses = hdk::get_links(
        &anchor_address()?,
        LinkMatch::Exactly("course_list"),
        LinkMatch::Any,
    )?
    .addresses();

    Ok(addresses)
}