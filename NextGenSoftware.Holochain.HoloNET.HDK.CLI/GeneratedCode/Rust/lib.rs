#![feature(proc_macro_hygiene)]

use hdk::prelude::*;
use hdk_proc_macros::zome;
use holochain_entry_utils::HolochainEntry;

// see https://developer.holochain.org/api/0.0.50-alpha4/hdk/ for info on using the hdk library
impl HolochainEntry for MyTestClass {
    fn entry_type() -> String {
        String::from("my_test_class")
    }
}

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct MyTestClass {
test_string: String,
test_int: i32,
test_bool: bool
}

impl HolochainEntry for Test {
    fn entry_type() -> String {
        String::from("test")
    }
}

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct Test {
test_string: String,
test_int: i32,
test_bool: bool
}


#[zome]
mod my_zome {
  
#[entry_def]
fn my_test_class_def() -> ValidatingEntryType {
    entry!(
        name: "my_test_class",
        description: "this is a same entry defintion",
        sharing: Sharing::Public,
        validation_package: || {
            hdk::ValidationPackageDefinition::Entry
        },
        validation: | _validation_data: hdk::EntryValidationData<MyTestClass>| {
            Ok(())
        }
    )
}

#[zome_fn("hc_public")]
fn delete_my_test_class(address: Address) -> ZomeApiResult<Address> {
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
fn update_my_test_class(updated_entry: MyTestClass, address: Address) -> ZomeApiResult<Address> {
    let mut my_test_class: MyTestClass = hdk::utils::get_as_type(address.clone())?;
    
    my_test_class.test_string=updated_entry.test_string;
	my_test_class.test_int=updated_entry.test_int;
	my_test_class.test_bool=updated_entry.test_bool;


    let updated_address = hdk::update_entry(my_test_class.clone().entry(), &address)?;
    Ok(updated_address)
}


#[zome_fn("hc_public")]
fn read_my_test_class(address: Address) -> ZomeApiResult<MyTestClass> {
    //hdk::get_entry(&address)
    hdk::utils::get_as_type(address)
}

#[zome_fn("hc_public")]
fn create_my_test_class(entry: MyTestClass) -> ZomeApiResult<Address> {
    let entry = Entry::App("my_test_class".into(), entry.into());
    let address = hdk::commit_entry(&entry)?;
    Ok(address)
}


#[entry_def]
fn test_def() -> ValidatingEntryType {
    entry!(
        name: "test",
        description: "this is a same entry defintion",
        sharing: Sharing::Public,
        validation_package: || {
            hdk::ValidationPackageDefinition::Entry
        },
        validation: | _validation_data: hdk::EntryValidationData<Test>| {
            Ok(())
        }
    )
}

#[zome_fn("hc_public")]
fn delete_test(address: Address) -> ZomeApiResult<Address> {
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
fn update_test(updated_entry: Test, address: Address) -> ZomeApiResult<Address> {
    let mut test: Test = hdk::utils::get_as_type(address.clone())?;
    
    test.test_string=updated_entry.test_string;
	test.test_int=updated_entry.test_int;
	test.test_bool=updated_entry.test_bool;


    let updated_address = hdk::update_entry(test.clone().entry(), &address)?;
    Ok(updated_address)
}


#[zome_fn("hc_public")]
fn read_test(address: Address) -> ZomeApiResult<Test> {
    //hdk::get_entry(&address)
    hdk::utils::get_as_type(address)
}

#[zome_fn("hc_public")]
fn create_test(entry: Test) -> ZomeApiResult<Address> {
    let entry = Entry::App("test".into(), entry.into());
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
