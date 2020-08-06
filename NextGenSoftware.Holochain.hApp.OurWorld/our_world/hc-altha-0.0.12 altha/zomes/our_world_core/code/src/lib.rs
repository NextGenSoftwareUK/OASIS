#![feature(try_from)]
#[macro_use]
extern crate hdk;
extern crate serde;
#[macro_use]
extern crate holochain_core_types_derive;

#[macro_use]
extern crate serde_derive;
#[macro_use]
extern crate serde_json;

use hdk::{
    entry_definition::ValidatingEntryType,
    error::ZomeApiResult,
};
use hdk::holochain_core_types::{
    cas::content::Address, entry::Entry, dna::entry_types::Sharing, error::HolochainError, json::JsonString,
    validation::EntryValidationData
};

 use hdk::error::ZomeApiError;

// see https://developer.holochain.org/api/0.0.12-alpha1/hdk/ for info on using the hdk library

// This is a sample zome that defines an entry type "MyEntry" that can be committed to the
// agent's chain via the exposed function create_my_entry

#[derive(Serialize, Deserialize, Debug, DefaultJson,Clone)]
pub struct MyEntry {
    content: String,
}

#[derive(Serialize, Deserialize, Debug, DefaultJson,Clone)]
pub struct Profile 
{
	Id: String,
	Username: String,
	Email: String,
	HcAddressHash : String,
	ProviderKey : String,
	Password: String,
    Title: String,
	FirstName: String,
	LastName: String,
	DOB: String,
	PlayerAddress: String,
	Karma: i32,
	Level: i32
}

pub fn handle_save_profile(entry: Profile) -> ZomeApiResult<Address> {
    let entry = Entry::App("profile".into(), entry.into());
    let address = hdk::commit_entry(&entry)?;
	//entry.HcAddressHash = address;
    Ok(address)
	//Ok(entry);
}

//pub fn handle_load_profile(address: Address) -> ZomeApiResult<Option<Entry>> {
//    hdk::get_entry(&address)
//}

pub fn handle_load_profile(address: Address) -> ZomeApiResult<Profile> {
    hdk::utils::get_as_type(address)
}

//pub fn handle_load_profiles() -> ZomeApiResult<Vec<Profile>> {
//    hdk::utils::get_links_and_load_type(Baseaddress?, LinkMatch::Exactly, LinkMatch::Exactly)
//}

//pub fn handle_get_profiles() -> serde_json::Value {
 //  match hdk::get_links(&hdk::AGENT_ADDRESS, None, "has profiles") {
  //      Ok(result) => {
 //           let mut profiles: Vec<Profile> = Vec::with_capacity(result.links.len());
 //           for address in result.links {
 //               let result : Result<Option<Profile>, ZomeApiError> = hdk::get_entry(address);
 //               match result {
 //                   Ok(Some(profile)) => profiles.push(profile),
 //                   Ok(None) =>  {},
 //                   Err(_) => {},
 //               }
//            }
//            json!(profiles)
//        },
//        Err(hdk_error) => hdk_error.to_json(),
//    }
//}



//pub fn handle_load_profile(address: Address) -> Result<Option<Entry>, ZomeApiError>
//{
//    let profile : Result<Option<Entry>, ZomeApiError> = hdk::get_entry(&address);
//	Ok(profile)
//	//Ok(hdk::get_entry(&address))
//	//let result : Result<Option<Profile>, ZomeApiError> = hdk::get_entry(address);
//}

pub fn handle_create_my_entry(entry: MyEntry) -> ZomeApiResult<Address> {
    let entry = Entry::App("my_entry".into(), entry.into());
    let address = hdk::commit_entry(&entry)?;
    Ok(address)
}

pub fn handle_get_my_entry(address: Address) -> ZomeApiResult<Option<Entry>> {
    hdk::get_entry(&address)
}

pub fn handle_test(message: MyEntry) -> ZomeApiResult<String> 
{
    //Ok("Hello " + message + ", welcome to Our World!")
    Ok("Hello, welcome to Our World!".to_string())
}

pub fn handle_test2(message: String) -> ZomeApiResult<String> 
{
    //Ok("Hello " + message + ", welcome to Our World!")
    Ok("Hello, welcome to Our World!".to_string())
}

pub fn handle_test3() -> ZomeApiResult<String> 
{
    //Ok("Hello " + message + ", welcome to Our World!")
    Ok("Hello, welcome to Our World!".to_string())
}

//fn definition() -> ValidatingEntryType {
//    entry!(
//        name: "my_entry",
//        description: "this is a same entry defintion",
//        sharing: Sharing::Public,
//        validation_package: || {
//            hdk::ValidationPackageDefinition::Entry
//        },

//        validation: | _validation_data: hdk::EntryValidationData<MyEntry>| {
//            Ok(())
//        }
//    )
//}

fn definition() -> ValidatingEntryType {
    entry!(
        name: "profile",
        description: "this is a same entry defintion",
        sharing: Sharing::Public,
        validation_package: || {
            hdk::ValidationPackageDefinition::Entry
        },

        validation: | _validation_data: hdk::EntryValidationData<Profile>| {
            Ok(())
        }
    )
}

define_zome! {
    entries: [
       definition()
    ]

    genesis: || { Ok(()) }

    functions: [
        create_my_entry: {
            inputs: |entry: MyEntry|,
            outputs: |result: ZomeApiResult<Address>|,
            handler: handle_create_my_entry
        }
        get_my_entry: {
            inputs: |address: Address|,
            outputs: |result: ZomeApiResult<Option<Entry>>|,
            handler: handle_get_my_entry
        }
		test: {
            inputs: |message: MyEntry|,
            outputs: |result: ZomeApiResult<String>|,
            handler: handle_test
        }
        test2: {
            inputs: |message: String |,
            outputs: |result: ZomeApiResult<String>|,
            handler: handle_test2
        }
        test3: {
            inputs: | |,
            outputs: |result: ZomeApiResult<String>|,
            handler: handle_test3
        }
		save_profile: {
            inputs: |entry: Profile|,
            outputs: |result: ZomeApiResult<Address>|,
            handler: handle_save_profile
        }
        load_profile: {
            inputs: |address: Address|,
            outputs: |result: ZomeApiResult<Profile>|,
            handler: handle_load_profile
        }
		//get_profiles: {
        //    inputs: | |,
        //    outputs: |profiles: serde_json::Value|,
        //    handler: handle_get_profiles
       // }
        
    ]

    traits: {
        hc_public [create_my_entry,get_my_entry,test,test2,test3,load_profile,save_profile]
    }
}
