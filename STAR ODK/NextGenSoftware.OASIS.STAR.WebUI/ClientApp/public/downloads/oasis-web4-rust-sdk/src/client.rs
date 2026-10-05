//! OASIS Web4 API Client implementation

use crate::config::Config;
use crate::error::{Error, Result};
use crate::types::*;
use reqwest::{Client, header::{HeaderMap, HeaderValue, AUTHORIZATION, CONTENT_TYPE}};
use serde::de::DeserializeOwned;
use serde::Serialize;
use std::collections::HashMap;

/// OASIS Web4 API Client
pub struct OASISWeb4Client {
    client: Client,
    config: Config,
    auth_token: Option<String>,
}

impl OASISWeb4Client {
    /// Create a new OASIS Web4 client
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

    /// Set authentication token
    pub fn set_auth_token(&mut self, token: String) {
        self.auth_token = Some(token);
    }

    /// Clear authentication token
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

    // Authentication Methods
    pub async fn authenticate(&mut self, provider: &str, credentials: Option<HashMap<String, String>>) -> Result<OASISResult<AuthResponse>> {
        let mut body = HashMap::new();
        body.insert("provider".to_string(), provider.to_string());

        if let Some(creds) = credentials {
            body.extend(creds);
        }

        let result: OASISResult<AuthResponse> = self.post("/avatar/authenticate", &body).await?;

        if let Some(ref auth_response) = result.result {
            self.auth_token = Some(auth_response.token.clone());
        }

        Ok(result)
    }

    pub async fn logout(&mut self) -> Result<OASISResult<bool>> {
        let result = self.post("/avatar/logout", &()).await?;
        self.auth_token = None;
        Ok(result)
    }

    // Avatar Methods
    pub async fn get_avatar(&self, id: &str) -> Result<OASISResult<Avatar>> {
        self.get(&format!("/avatar/{}", id)).await
    }

    pub async fn get_avatar_by_username(&self, username: &str) -> Result<OASISResult<Avatar>> {
        self.get(&format!("/avatar/username/{}", username)).await
    }

    pub async fn get_avatar_by_email(&self, email: &str) -> Result<OASISResult<Avatar>> {
        self.get(&format!("/avatar/email/{}", email)).await
    }

    pub async fn create_avatar(&self, request: &CreateAvatarRequest) -> Result<OASISResult<Avatar>> {
        self.post("/avatar", request).await
    }

    pub async fn update_avatar(&self, id: &str, request: &UpdateAvatarRequest) -> Result<OASISResult<Avatar>> {
        self.put(&format!("/avatar/{}", id), request).await
    }

    pub async fn delete_avatar(&self, id: &str) -> Result<OASISResult<bool>> {
        self.delete(&format!("/avatar/{}", id)).await
    }

    pub async fn search_avatars(&self, query: &str) -> Result<OASISResult<Vec<Avatar>>> {
        self.get(&format!("/avatar/search?q={}", urlencoding::encode(query))).await
    }

    // Karma Methods
    pub async fn get_karma(&self, avatar_id: &str) -> Result<OASISResult<Karma>> {
        self.get(&format!("/avatar/{}/karma", avatar_id)).await
    }

    pub async fn add_karma(&self, avatar_id: &str, request: &AddKarmaRequest) -> Result<OASISResult<Karma>> {
        self.post(&format!("/avatar/{}/karma", avatar_id), request).await
    }

    pub async fn get_karma_history(&self, avatar_id: &str, limit: u32) -> Result<OASISResult<Vec<KarmaEntry>>> {
        self.get(&format!("/avatar/{}/karma/history?limit={}", avatar_id, limit)).await
    }

    pub async fn get_karma_leaderboard(&self, time_range: &str, limit: u32) -> Result<OASISResult<Vec<Avatar>>> {
        self.get(&format!("/karma/leaderboard?range={}&limit={}", time_range, limit)).await
    }

    // NFT Methods
    pub async fn get_nfts(&self, avatar_id: &str) -> Result<OASISResult<Vec<NFT>>> {
        self.get(&format!("/nft?avatarId={}", avatar_id)).await
    }

    pub async fn get_nft(&self, nft_id: &str) -> Result<OASISResult<NFT>> {
        self.get(&format!("/nft/{}", nft_id)).await
    }

    pub async fn mint_nft(&self, avatar_id: &str, request: &MintNFTRequest) -> Result<OASISResult<NFT>> {
        let mut body = serde_json::to_value(request)?;
        body["avatarId"] = serde_json::Value::String(avatar_id.to_string());
        self.post("/nft/mint", &body).await
    }

    pub async fn transfer_nft(&self, nft_id: &str, to_avatar_id: &str) -> Result<OASISResult<NFT>> {
        let body = serde_json::json!({ "toAvatarId": to_avatar_id });
        self.post(&format!("/nft/{}/transfer", nft_id), &body).await
    }

    pub async fn burn_nft(&self, nft_id: &str) -> Result<OASISResult<bool>> {
        self.delete(&format!("/nft/{}", nft_id)).await
    }

    // Provider Management
    pub async fn get_available_providers(&self) -> Result<OASISResult<Vec<Provider>>> {
        self.get("/providers").await
    }

    pub async fn get_current_provider(&self) -> Result<OASISResult<Provider>> {
        self.get("/providers/current").await
    }

    pub async fn switch_provider(&self, provider_name: &str) -> Result<OASISResult<Provider>> {
        let body = serde_json::json!({ "provider": provider_name });
        self.post("/providers/switch", &body).await
    }

    // Messaging
    pub async fn get_chat_messages(&self, chat_id: &str, limit: u32) -> Result<OASISResult<Vec<Message>>> {
        self.get(&format!("/chat/{}/messages?limit={}", chat_id, limit)).await
    }

    pub async fn send_message(&self, chat_id: &str, avatar_id: &str, content: &str) -> Result<OASISResult<Message>> {
        let body = serde_json::json!({
            "chatId": chat_id,
            "avatarId": avatar_id,
            "content": content
        });
        self.post("/chat/messages", &body).await
    }

    // Social Features
    pub async fn get_friends(&self, avatar_id: &str) -> Result<OASISResult<Vec<Avatar>>> {
        self.get(&format!("/avatar/{}/friends", avatar_id)).await
    }

    pub async fn add_friend(&self, avatar_id: &str, friend_id: &str) -> Result<OASISResult<bool>> {
        let body = serde_json::json!({ "friendId": friend_id });
        self.post(&format!("/avatar/{}/friends", avatar_id), &body).await
    }

    pub async fn remove_friend(&self, avatar_id: &str, friend_id: &str) -> Result<OASISResult<bool>> {
        self.delete(&format!("/avatar/{}/friends/{}", avatar_id, friend_id)).await
    }
}
