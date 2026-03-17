#![feature(proc_macro_hygiene)]

use hdk::prelude::*;
use hdk_proc_macros::zome;
use holochain_entry_utils::HolochainEntry;

// see https://developer.holochain.org/api/0.0.50-alpha4/hdk/ for info on using the hdk library

#[zome]
mod zome_name {
    
    #[init]
    fn init() {
        Ok(())
    }

    #[validate_agent]
    pub fn validate_agent(validation_data: EntryValidationData<AgentId>) {
        Ok(())
    }
}
