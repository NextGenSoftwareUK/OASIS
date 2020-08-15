#![feature(proc_macro_hygiene)]

use hdk::prelude::*;
use hdk_proc_macros::zome;

// see https://developer.holochain.org/api/0.0.50-alpha4/hdk/ for info on using the hdk library

// This is a sample zome that defines an entry type "MyEntry" that can be committed to the
// agent's chain via the exposed function create_my_entry

#[zome]
mod test_zome {

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct test {
test_string: String,
test_int: i32,
test_bool: bool
}
    
    #[init]
    fn init() {
        Ok(())
    }

    #[validate_agent]
    pub fn validate_agent(validation_data: EntryValidationData<
#[zome_fn("hc_public")]
fn delete_test(entry: test) -> ZomeApiResult<Address> {
    /*
    hdk::remove_link(&anchor_address()?, &address, "course_list", "")?;

    let students = get_students(address.clone())?;
    let course: Course = hdk::utils::get_as_type(address.clone())?;

    for student in students {
        hdk::remove_link(&student, &address, "student->course", "")?;
    }
    hdk::remove_link(&course.teacher_address, &address, "teacher->courses", "")?;
    */

    hdk::remove_entry(&address)
}

#[zome_fn("hc_public")]
fn update_test(updated_entry: test, address: &Address) -> ZomeApiResult<Address> {
    let mut test: test = hdk::utils::get_as_type(address.clone())?;
    
    test.test_string=updated_entry.test_string;
	test.test_int=updated_entry.test_int;
	test.test_bool=updated_entry.test_bool;

    hdk::update_entry(test.entry(), address)
}


#[zome_fn("hc_public")]
fn read_test(address: Address) -> ZomeApiResult<Option<test>> {
    hdk::get_entry(&address)
}


#[zome_fn("hc_public")]
fn create_test(entry: test) -> ZomeApiResult<Address> {
    let entry = Entry::App("test".into(), entry.into());
    let address = hdk::commit_entry(&entry)?;
    Ok(address)
}

AgentId>) {
        Ok(())
    }

    #[entry_def]
    fn my_entry_def() -> ValidatingEntryType {
        entry!(
            name: "my_entry",
            description: "this is a same entry defintion",
            sharing: Sharing::Public,
            validation_package: || {
                hdk::ValidationPackageDefinition::Entry
            },
            validation: | _validation_data: hdk::EntryValidationData<MyEntry>| {
                Ok(())
            }
        )
    }
}
