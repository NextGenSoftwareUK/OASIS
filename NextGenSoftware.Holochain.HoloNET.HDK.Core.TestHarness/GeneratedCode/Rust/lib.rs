#![feature(proc_macro_hygiene)]

use hdk::prelude::*;
use hdk_proc_macros::zome;
use holochain_entry_utils::HolochainEntry;

// see https://developer.holochain.org/api/0.0.50-alpha4/hdk/ for info on using the hdk library
impl HolochainEntry for SuperClass {
    fn entry_type() -> String {
        String::from("super_class")
    }
}

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct SuperClass {
super_test_string: String,
super_test_int: i32,
super_test_bool: bool
}

impl HolochainEntry for SuperTest {
    fn entry_type() -> String {
        String::from("super_test")
    }
}

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct SuperTest {
test_string: String,
test_int: i32,
test_bool: bool
}


#[zome]
mod super_zome {
#[entry_def]
fn super_class_def() -> ValidatingEntryType {
    entry!(
        name: "super_class",
        description: "this is a same entry defintion",
        sharing: Sharing::Public,
        validation_package: || {
            hdk::ValidationPackageDefinition::Entry
        },
        validation: | _validation_data: hdk::EntryValidationData<SuperClass>| {
            Ok(())
        }
    )
}

#[zome_fn("hc_public")]
fn delete_super_class(address: Address) -> ZomeApiResult<Address> {
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
fn update_super_class(updated_entry: SuperClass, address: Address) -> ZomeApiResult<Address> {
    let mut super_class: SuperClass = hdk::utils::get_as_type(address.clone())?;
    
    super_class.super_test_string=updated_entry.super_test_string;
	super_class.super_test_int=updated_entry.super_test_int;
	super_class.super_test_bool=updated_entry.super_test_bool;


    let updated_address = hdk::update_entry(super_class.clone().entry(), &address)?;
    Ok(updated_address)
}


#[zome_fn("hc_public")]
fn read_super_class(address: Address) -> ZomeApiResult<SuperClass> {
    //hdk::get_entry(&address)
    hdk::utils::get_as_type(address)
}

#[zome_fn("hc_public")]
fn create_super_class(entry: SuperClass) -> ZomeApiResult<Address> {
    let entry = Entry::App("super_class".into(), entry.into());
    let address = hdk::commit_entry(&entry)?;
    Ok(address)
}


#[entry_def]
fn super_test_def() -> ValidatingEntryType {
    entry!(
        name: "super_test",
        description: "this is a same entry defintion",
        sharing: Sharing::Public,
        validation_package: || {
            hdk::ValidationPackageDefinition::Entry
        },
        validation: | _validation_data: hdk::EntryValidationData<SuperTest>| {
            Ok(())
        }
    )
}

#[zome_fn("hc_public")]
fn delete_super_test(address: Address) -> ZomeApiResult<Address> {
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
fn update_super_test(updated_entry: SuperTest, address: Address) -> ZomeApiResult<Address> {
    let mut super_test: SuperTest = hdk::utils::get_as_type(address.clone())?;
    
    super_test.test_string=updated_entry.test_string;
	super_test.test_int=updated_entry.test_int;
	super_test.test_bool=updated_entry.test_bool;


    let updated_address = hdk::update_entry(super_test.clone().entry(), &address)?;
    Ok(updated_address)
}


#[zome_fn("hc_public")]
fn read_super_test(address: Address) -> ZomeApiResult<SuperTest> {
    //hdk::get_entry(&address)
    hdk::utils::get_as_type(address)
}

#[zome_fn("hc_public")]
fn create_super_test(entry: SuperTest) -> ZomeApiResult<Address> {
    let entry = Entry::App("super_test".into(), entry.into());
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
}
