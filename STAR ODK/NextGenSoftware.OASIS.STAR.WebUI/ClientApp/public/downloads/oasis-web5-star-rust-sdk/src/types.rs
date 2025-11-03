//! Type definitions for OASIS Web5 STAR API

use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct OASISResult<T> {
    pub is_error: bool,
    pub message: String,
    pub result: Option<T>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub is_saved: Option<bool>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct STARStatus {
    pub is_ignited: bool,
    pub is_lit: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub uptime: Option<u64>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub version: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub oapps_running: Option<u32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub total_oapps: Option<u32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub starnet_connected: Option<bool>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub active_nodes: Option<u32>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct OAPP {
    pub id: String,
    pub name: String,
    pub description: String,
    pub version: String,
    pub author: String,
    pub category: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub icon: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub screenshots: Option<Vec<String>>,
    pub is_published: bool,
    pub is_installed: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub downloads: Option<u64>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub rating: Option<f32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub size: Option<f64>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub created_date: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub updated_date: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub holons: Option<Vec<String>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub zomes: Option<Vec<String>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub dependencies: Option<Vec<String>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub permissions: Option<Vec<String>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub config: Option<HashMap<String, serde_json::Value>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CreateOAPPRequest {
    pub name: String,
    pub description: String,
    pub category: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub version: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub icon: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub template_id: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub config: Option<HashMap<String, serde_json::Value>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Difficulty {
    #[serde(rename = "easy")]
    Easy,
    #[serde(rename = "medium")]
    Medium,
    #[serde(rename = "hard")]
    Hard,
    #[serde(rename = "expert")]
    Expert,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Mission {
    pub id: String,
    pub title: String,
    pub description: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub chapter_id: Option<String>,
    pub difficulty: Difficulty,
    pub estimated_time: u32,
    pub karma_reward: i64,
    pub xp_reward: i64,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub prerequisites: Option<Vec<String>>,
    pub objectives: Vec<Objective>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub quests_count: Option<u32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub completion_percentage: Option<f32>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub status: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Quest {
    pub id: String,
    pub title: String,
    pub description: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub mission_id: Option<String>,
    #[serde(rename = "type")]
    pub quest_type: String,
    pub difficulty: Difficulty,
    pub karma_reward: i64,
    pub xp_reward: i64,
    pub objectives: Vec<Objective>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub sub_quests: Option<Vec<Quest>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub status: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub progress: Option<f32>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Objective {
    pub id: String,
    pub description: String,
    #[serde(rename = "type")]
    pub objective_type: String,
    pub target: u32,
    pub current: u32,
    pub completed: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Chapter {
    pub id: String,
    pub number: u32,
    pub title: String,
    pub description: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub image_url: Option<String>,
    pub missions_count: u32,
    pub total_quests_count: u32,
    pub total_sub_quests_count: u32,
    pub difficulty: String,
    pub status: String,
    pub completion_percentage: f32,
    pub estimated_time: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Holon {
    pub id: String,
    pub name: String,
    pub description: String,
    pub version: String,
    pub category: String,
    #[serde(rename = "type")]
    pub holon_type: String,
    pub author: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub image_url: Option<String>,
    pub downloads: u64,
    pub rating: f32,
    pub size: f64,
    pub last_updated: String,
    pub is_public: bool,
    pub is_featured: bool,
    pub tags: Vec<String>,
    pub data_schema: HashMap<String, serde_json::Value>,
    pub properties: Vec<String>,
    pub methods: Vec<String>,
    pub events: Vec<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub documentation: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub repository: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub license: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub price: Option<f64>,
    pub is_free: bool,
    pub is_installed: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Zome {
    pub id: String,
    pub name: String,
    pub description: String,
    pub version: String,
    pub category: String,
    #[serde(rename = "type")]
    pub zome_type: String,
    pub language: String,
    pub framework: String,
    pub author: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub image_url: Option<String>,
    pub downloads: u64,
    pub rating: f32,
    pub size: f64,
    pub last_updated: String,
    pub is_public: bool,
    pub is_featured: bool,
    pub tags: Vec<String>,
    pub functions: Vec<String>,
    pub dependencies: Vec<String>,
    pub apis: Vec<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub documentation: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub repository: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub license: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub price: Option<f64>,
    pub is_free: bool,
    pub is_installed: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct STARPlugin {
    pub id: String,
    pub name: String,
    pub description: String,
    pub version: String,
    pub author: String,
    pub category: String,
    #[serde(rename = "type")]
    pub plugin_type: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub image_url: Option<String>,
    pub downloads: u64,
    pub rating: f32,
    pub size: f64,
    pub last_updated: String,
    pub is_installed: bool,
    pub is_enabled: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub config: Option<HashMap<String, serde_json::Value>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub dependencies: Option<Vec<String>>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub documentation: Option<String>,
}
