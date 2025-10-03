//! OASIS Web5 STAR API Client implementation

use crate::config::Config;
use crate::error::{Error, Result};
use crate::types::*;
use reqwest::{Client, header::{HeaderMap, HeaderValue, AUTHORIZATION, CONTENT_TYPE}};
use serde::de::DeserializeOwned;
use serde::Serialize;

/// OASIS Web5 STAR API Client
pub struct OASISWeb5STARClient {
    client: Client,
    config: Config,
    auth_token: Option<String>,
}

impl OASISWeb5STARClient {
    /// Create a new OASIS Web5 STAR client
    pub fn new(config: Config) -> Self {
        let client = Client::builder()
            .timeout(config.timeout)
            .build()
            .expect("Failed to build HTTP client");

        Self {
            client,
            config,
            auth_token: None,
        }
    }

    pub fn set_auth_token(&mut self, token: String) {
        self.auth_token = Some(token);
    }

    pub fn clear_auth_token(&mut self) {
        self.auth_token = None;
    }

    fn build_headers(&self) -> HeaderMap {
        let mut headers = HeaderMap::new();
        headers.insert(CONTENT_TYPE, HeaderValue::from_static("application/json"));

        if let Some(token) = &self.auth_token {
            if let Ok(value) = HeaderValue::from_str(&format!("Bearer {}", token)) {
                headers.insert(AUTHORIZATION, value);
            }
        }

        headers
    }

    async fn get<T: DeserializeOwned>(&self, path: &str) -> Result<OASISResult<T>> {
        let url = format!("{}{}", self.config.api_url, path);
        let response = self.client
            .get(&url)
            .headers(self.build_headers())
            .send()
            .await?;

        if response.status().is_success() {
            Ok(response.json().await?)
        } else {
            Err(Error::ApiError(response.text().await?))
        }
    }

    async fn post<T: Serialize, R: DeserializeOwned>(&self, path: &str, body: &T) -> Result<OASISResult<R>> {
        let url = format!("{}{}", self.config.api_url, path);
        let response = self.client
            .post(&url)
            .headers(self.build_headers())
            .json(body)
            .send()
            .await?;

        if response.status().is_success() {
            Ok(response.json().await?)
        } else {
            Err(Error::ApiError(response.text().await?))
        }
    }

    async fn put<T: Serialize, R: DeserializeOwned>(&self, path: &str, body: &T) -> Result<OASISResult<R>> {
        let url = format!("{}{}", self.config.api_url, path);
        let response = self.client
            .put(&url)
            .headers(self.build_headers())
            .json(body)
            .send()
            .await?;

        if response.status().is_success() {
            Ok(response.json().await?)
        } else {
            Err(Error::ApiError(response.text().await?))
        }
    }

    async fn delete<R: DeserializeOwned>(&self, path: &str) -> Result<OASISResult<R>> {
        let url = format!("{}{}", self.config.api_url, path);
        let response = self.client
            .delete(&url)
            .headers(self.build_headers())
            .send()
            .await?;

        if response.status().is_success() {
            Ok(response.json().await?)
        } else {
            Err(Error::ApiError(response.text().await?))
        }
    }

    // STAR Core Operations
    pub async fn ignite_star(&self) -> Result<OASISResult<STARStatus>> {
        self.post("/star/ignite", &()).await
    }

    pub async fn extinguish_star(&self) -> Result<OASISResult<bool>> {
        self.post("/star/extinguish", &()).await
    }

    pub async fn get_star_status(&self) -> Result<OASISResult<STARStatus>> {
        self.get("/star/status").await
    }

    pub async fn light_star(&self) -> Result<OASISResult<STARStatus>> {
        self.post("/star/light", &()).await
    }

    // OAPP Management
    pub async fn get_all_oapps(&self) -> Result<OASISResult<Vec<OAPP>>> {
        self.get("/oapps").await
    }

    pub async fn get_oapp(&self, id: &str) -> Result<OASISResult<OAPP>> {
        self.get(&format!("/oapps/{}", id)).await
    }

    pub async fn create_oapp(&self, request: &CreateOAPPRequest) -> Result<OASISResult<OAPP>> {
        self.post("/oapps", request).await
    }

    pub async fn publish_oapp(&self, id: &str) -> Result<OASISResult<OAPP>> {
        self.post(&format!("/oapps/{}/publish", id), &()).await
    }

    pub async fn install_oapp(&self, id: &str, avatar_id: &str) -> Result<OASISResult<bool>> {
        let body = serde_json::json!({ "avatarId": avatar_id });
        self.post(&format!("/oapps/{}/install", id), &body).await
    }

    // Missions
    pub async fn get_all_missions(&self) -> Result<OASISResult<Vec<Mission>>> {
        self.get("/missions").await
    }

    pub async fn get_mission(&self, id: &str) -> Result<OASISResult<Mission>> {
        self.get(&format!("/missions/{}", id)).await
    }

    pub async fn start_mission(&self, mission_id: &str, avatar_id: &str) -> Result<OASISResult<serde_json::Value>> {
        let body = serde_json::json!({ "avatarId": avatar_id });
        self.post(&format!("/missions/{}/start", mission_id), &body).await
    }

    pub async fn complete_mission(&self, mission_id: &str, avatar_id: &str) -> Result<OASISResult<serde_json::Value>> {
        let body = serde_json::json!({ "avatarId": avatar_id });
        self.post(&format!("/missions/{}/complete", mission_id), &body).await
    }

    // Quests
    pub async fn get_all_quests(&self) -> Result<OASISResult<Vec<Quest>>> {
        self.get("/quests").await
    }

    pub async fn get_quest(&self, id: &str) -> Result<OASISResult<Quest>> {
        self.get(&format!("/quests/{}", id)).await
    }

    pub async fn start_quest(&self, quest_id: &str, avatar_id: &str) -> Result<OASISResult<serde_json::Value>> {
        let body = serde_json::json!({ "avatarId": avatar_id });
        self.post(&format!("/quests/{}/start", quest_id), &body).await
    }

    pub async fn complete_quest(&self, quest_id: &str, avatar_id: &str) -> Result<OASISResult<serde_json::Value>> {
        let body = serde_json::json!({ "avatarId": avatar_id });
        self.post(&format!("/quests/{}/complete", quest_id), &body).await
    }

    // Chapters
    pub async fn get_all_chapters(&self) -> Result<OASISResult<Vec<Chapter>>> {
        self.get("/chapters").await
    }

    pub async fn get_chapter(&self, id: &str) -> Result<OASISResult<Chapter>> {
        self.get(&format!("/chapters/{}", id)).await
    }

    // Holons
    pub async fn get_all_holons(&self) -> Result<OASISResult<Vec<Holon>>> {
        self.get("/holons").await
    }

    pub async fn get_holon(&self, id: &str) -> Result<OASISResult<Holon>> {
        self.get(&format!("/holons/{}", id)).await
    }

    pub async fn create_holon(&self, holon: &Holon) -> Result<OASISResult<Holon>> {
        self.post("/holons", holon).await
    }

    // Zomes
    pub async fn get_all_zomes(&self) -> Result<OASISResult<Vec<Zome>>> {
        self.get("/zomes").await
    }

    pub async fn get_zome(&self, id: &str) -> Result<OASISResult<Zome>> {
        self.get(&format!("/zomes/{}", id)).await
    }

    pub async fn install_zome(&self, id: &str, oapp_id: &str) -> Result<OASISResult<bool>> {
        let body = serde_json::json!({ "oappId": oapp_id });
        self.post(&format!("/zomes/{}/install", id), &body).await
    }

    // STAR Plugins
    pub async fn get_all_star_plugins(&self) -> Result<OASISResult<Vec<STARPlugin>>> {
        self.get("/star/plugins").await
    }

    pub async fn get_star_plugin(&self, id: &str) -> Result<OASISResult<STARPlugin>> {
        self.get(&format!("/star/plugins/{}", id)).await
    }

    pub async fn install_star_plugin(&self, id: &str) -> Result<OASISResult<bool>> {
        self.post(&format!("/star/plugins/{}/install", id), &()).await
    }

    // STARNET
    pub async fn join_starnet(&self, config: Option<serde_json::Value>) -> Result<OASISResult<serde_json::Value>> {
        self.post("/starnet/join", &config.unwrap_or(serde_json::json!({}))).await
    }

    pub async fn get_starnet_status(&self) -> Result<OASISResult<serde_json::Value>> {
        self.get("/starnet/status").await
    }

    pub async fn get_starnet_nodes(&self) -> Result<OASISResult<Vec<serde_json::Value>>> {
        self.get("/starnet/nodes").await
    }
}
