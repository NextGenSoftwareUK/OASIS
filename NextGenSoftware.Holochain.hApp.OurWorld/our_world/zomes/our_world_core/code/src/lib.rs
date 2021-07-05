#![feature(proc_macro_hygiene)]

use hdk::prelude::*;
use hdk_proc_macros::zome;

// see https://developer.holochain.org/api/0.0.50-alpha4/hdk/ for info on using the hdk library

// This is a sample zome that defines an entry type "MyEntry" that can be committed to the
// agent's chain via the exposed function create_my_entry

#[derive(Serialize, Deserialize, Debug, DefaultJson, Clone)]
pub struct MyEntry {
    content: String,
}

#[derive(Debug, Serialize, Deserialize, DefaultJson)]
struct SignalPayload {
    message: String
}


#[derive(Serialize, Deserialize, Debug, DefaultJson,Clone)]
pub struct Avatar 
{
    id: String,
	username: String,
	email: String,
	hc_address_hash : String,
	provider_key : String,
	password: String,
    title: String,
	first_name: String,
	last_name: String,
	dob: String,
	player_address: String,
	karma: i32,
    level: i32,
    holon_type: i32
}

#[zome]
mod our_world_core {

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
            name: "Avatar",
            description: "this is a same entry defintion",
            sharing: Sharing::Public,
            validation_package: || {
                hdk::ValidationPackageDefinition::Entry
            },
            validation: | _validation_data: hdk::EntryValidationData<Avatar>| {
                Ok(())
            }
        )
    }

    #[zome_fn("hc_public")]
    fn create_my_entry(entry: MyEntry) -> ZomeApiResult<Address> {
        let entry = Entry::App("my_entry".into(), entry.into());
        let address = hdk::commit_entry(&entry)?;
        Ok(address)
    }

    #[zome_fn("hc_public")]
    fn get_my_entry(address: Address) -> ZomeApiResult<Option<Entry>> {
        hdk::get_entry(&address)
    }

    #[zome_fn("hc_public")]
    fn test_signals() -> ZomeApiResult<bool> {
        let message = "Hello World".to_string();
        let _ = hdk::emit_signal("message_received", SignalPayload{message});
        Ok(true)
    }

    #[zome_fn("hc_public")]
    fn save_Avatar(entry: Avatar) -> ZomeApiResult<Address> {
        let entry = Entry::App("Avatar".into(), entry.into());
        let address = hdk::commit_entry(&entry)?;
        //entry.HcAddressHash = address;
        Ok(address)
        //Ok(entry);
    }
    
    //pub fn handle_load_Avatar(address: Address) -> ZomeApiResult<Option<Entry>> {
    //    hdk::get_entry(&address)
    //}

    #[zome_fn("hc_public")]
    fn load_Avatar(address: Address) -> ZomeApiResult<Avatar> {
        //hdk::get_entry(&address)
        hdk::utils::get_as_type(address)
    }

    #[zome_fn("hc_public")]
    fn test() -> ZomeApiResult<String> 
    {
        //Ok("Hello " + message + ", welcome to Our World!")
        Ok("Hello, welcome to Our World!".to_string())
    }

    // fn test(_message: MyEntry) -> ZomeApiResult<String> 
    // {
    //     //Ok("Hello " + message + ", welcome to Our World!")
    //     Ok("Hello, welcome to Our World!".to_string())
    // }

    // #[zome_fn("hc_public")]
    // fn test2(_message: String) -> ZomeApiResult<String> 
    // {
    //     //let mut owned_string: String = "hello ".to_owned();
    //     let borrowed_string: &str = "world";

    //     //owned_string.push_str(borrowed_string);
    //     _message.push_str(borrowed_string);
    //     println!("{}", _message);
        
    //     Ok(_message);
        
    //     //Ok("Hello " + _message + ", welcome to Our World!")
    //    // Ok("Hello, welcome to Our World!".to_string())
    // }

    #[zome_fn("hc_public")]
    fn test2(_message: String) -> ZomeApiResult<String> 
    {   
       // Ok("Hello " + _message + ", welcome to Our World!")
        Ok("Hello, welcome to Our World!".to_string())
    }

    #[zome_fn("hc_public")]
    fn test3() -> ZomeApiResult<String> 
    {
        //Ok("Hello " + message + ", welcome to Our World!")
        Ok("Hello, welcome to Our World!".to_string())
    }
}
