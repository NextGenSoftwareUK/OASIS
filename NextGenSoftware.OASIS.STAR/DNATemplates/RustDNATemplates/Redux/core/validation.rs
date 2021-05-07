#[entry_def]
fn my_holon_def() -> ValidatingEntryType {
    entry!(
        name: "my_holon",
        description: "holon description",
        sharing: Sharing::Public,
        validation_package: || {
            hdk::ValidationPackageDefinition::Entry
        },
        validation: | _validation_data: hdk::EntryValidationData<MyHolon>| {
            Ok(())
        }
    )
}