#![feature(proc_macro_hygiene)]

use hdk::prelude::*;
use hdk_proc_macros::zome;

// see https://developer.holochain.org/api/0.0.50-alpha4/hdk/ for info on using the hdk library

// This is a sample zome that defines an entry type "MyEntry" that can be committed to the
// agent's chain via the exposed function create_my_entry

#[zome]
mod test2_zome {
#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct test2 {
test_string: String,
test_int: i32,
test_bool: bool
}

#[zome_fn("hc_public")]
fn delete_test2(entry: test2) -> ZomeApiResult<Address> {
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
fn update_test2(updated_entry: test2, address: &Address) -> ZomeApiResult<Address> {
    let mut test2: test2 = hdk::utils::get_as_type(address.clone())?;
    
    test2.test_string=updated_entry.test_string;
	test2.test_int=updated_entry.test_int;
	test2.test_bool=updated_entry.test_bool;

    hdk::update_entry(test2.entry(), address)
}


#[zome_fn("hc_public")]
fn read_test2(address: Address) -> ZomeApiResult<Option<test2>> {
    hdk::get_entry(&address)
}


#[zome_fn("hc_public")]
fn create_test2(entry: test2) -> ZomeApiResult<Address> {
    let entry = Entry::App("test2".into(), entry.into());
    let address = hdk::commit_entry(&entry)?;
    Ok(address)
}


    
    #[init]
    fn init() {
        Ok(())
    }

    #[validate_agent]
    pub fn validate_agent(validation_data: EntryValidationData<AgentId>) {
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
