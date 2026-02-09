/**
 * OASIS STAR API - C/C++ Wrapper Implementation
 * 
 * This implementation provides a bridge between C/C++ game code and the
 * STAR API REST service. It uses HTTP requests to communicate with the API.
 */

#include "star_api.h"
#include <string>
#include <vector>
#include <map>
#include <mutex>
#include <cstring>
#include <cstdlib>
#include <algorithm>


#ifdef _WIN32
    #include <windows.h>
    #include <winhttp.h>
    #pragma comment(lib, "winhttp.lib")
#else
    #include <curl/curl.h>
    #include <json/json.h>
#endif

// Internal state
static struct {
    bool initialized = false;
    star_api_config_t config;
    std::string last_error;
    std::mutex mutex;
    star_api_callback_t callback = nullptr;
    void* callback_user_data = nullptr;
} g_state;

// Helper function to set last error
static void set_error(const char* error) {
    std::lock_guard<std::mutex> lock(g_state.mutex);
    g_state.last_error = error ? error : "Unknown error";
}

// HTTP response structure
struct HttpResponse {
    std::string data;
    int status_code = 0;
    bool success = false;
};

#ifdef _WIN32
// Windows HTTP implementation using WinHTTP
static HttpResponse http_request(const std::string& method, const std::string& url, const std::string& body = "") {
    HttpResponse response;
    
    // Parse URL
    URL_COMPONENTS urlComp;
    ZeroMemory(&urlComp, sizeof(urlComp));
    urlComp.dwStructSize = sizeof(urlComp);
    urlComp.dwSchemeLength = -1;
    urlComp.dwHostNameLength = -1;
    urlComp.dwUrlPathLength = -1;
    
    std::wstring wurl(url.begin(), url.end());
    if (!WinHttpCrackUrl(wurl.c_str(), wurl.length(), 0, &urlComp)) {
        set_error("Failed to parse URL");
        return response;
    }
    
    // Connect to server
    HINTERNET hSession = WinHttpOpen(L"OASIS-STAR-API-Client/1.0", WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, NULL, NULL, 0);
    if (!hSession) {
        set_error("Failed to open WinHTTP session");
        return response;
    }
    
    std::wstring host(urlComp.lpszHostName, urlComp.dwHostNameLength);
    HINTERNET hConnect = WinHttpConnect(hSession, host.c_str(), urlComp.nPort, 0);
    if (!hConnect) {
        WinHttpCloseHandle(hSession);
        set_error("Failed to connect to server");
        return response;
    }
    
    // Open request
    std::wstring path(urlComp.lpszUrlPath, urlComp.dwUrlPathLength);
    HINTERNET hRequest = WinHttpOpenRequest(hConnect, 
        method == "GET" ? L"GET" : (method == "POST" ? L"POST" : L"PUT"),
        path.c_str(), NULL, NULL, NULL, 0);
    
    if (!hRequest) {
        WinHttpCloseHandle(hConnect);
        WinHttpCloseHandle(hSession);
        set_error("Failed to open request");
        return response;
    }
    
    // Add headers
    std::wstring headers = L"Authorization: Bearer " + std::wstring(g_state.config.api_key, g_state.config.api_key + strlen(g_state.config.api_key));
    headers += L"\r\nContent-Type: application/json\r\n";
    
    if (!WinHttpAddRequestHeaders(hRequest, headers.c_str(), headers.length(), WINHTTP_ADDREQ_FLAG_ADD)) {
        WinHttpCloseHandle(hRequest);
        WinHttpCloseHandle(hConnect);
        WinHttpCloseHandle(hSession);
        set_error("Failed to add headers");
        return response;
    }
    
    // Send request
    std::wstring wbody(body.begin(), body.end());
    if (!WinHttpSendRequest(hRequest, NULL, 0, body.empty() ? NULL : (LPVOID)body.c_str(), body.length(), body.length(), 0)) {
        WinHttpCloseHandle(hRequest);
        WinHttpCloseHandle(hConnect);
        WinHttpCloseHandle(hSession);
        set_error("Failed to send request");
        return response;
    }
    
    // Receive response
    if (!WinHttpReceiveResponse(hRequest, NULL)) {
        WinHttpCloseHandle(hRequest);
        WinHttpCloseHandle(hConnect);
        WinHttpCloseHandle(hSession);
        set_error("Failed to receive response");
        return response;
    }
    
    // Read response data
    DWORD status_code = 0;
    DWORD status_code_size = sizeof(status_code);
    WinHttpQueryHeaders(hRequest, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, NULL, &status_code, &status_code_size, NULL);
    response.status_code = status_code;
    response.success = (status_code >= 200 && status_code < 300);
    
    DWORD bytes_available = 0;
    do {
        if (WinHttpQueryDataAvailable(hRequest, &bytes_available)) {
            if (bytes_available > 0) {
                std::vector<char> buffer(bytes_available);
                DWORD bytes_read = 0;
                if (WinHttpReadData(hRequest, buffer.data(), bytes_available, &bytes_read)) {
                    response.data.append(buffer.data(), bytes_read);
                }
            }
        }
    } while (bytes_available > 0);
    
    WinHttpCloseHandle(hRequest);
    WinHttpCloseHandle(hConnect);
    WinHttpCloseHandle(hSession);
    
    return response;
}
#else
// Linux/Mac HTTP implementation using libcurl
static size_t WriteCallback(void* contents, size_t size, size_t nmemb, void* userp) {
    ((std::string*)userp)->append((char*)contents, size * nmemb);
    return size * nmemb;
}

static HttpResponse http_request(const std::string& method, const std::string& url, const std::string& body = "") {
    HttpResponse response;
    CURL* curl = curl_easy_init();
    
    if (!curl) {
        set_error("Failed to initialize curl");
        return response;
    }
    
    struct curl_slist* headers = NULL;
    std::string auth_header = "Authorization: Bearer " + std::string(g_state.config.api_key);
    headers = curl_slist_append(headers, auth_header.c_str());
    headers = curl_slist_append(headers, "Content-Type: application/json");
    
    curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
    curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response.data);
    curl_easy_setopt(curl, CURLOPT_TIMEOUT, g_state.config.timeout_seconds);
    
    if (method == "POST") {
        curl_easy_setopt(curl, CURLOPT_POSTFIELDS, body.c_str());
    }
    
    CURLcode res = curl_easy_perform(curl);
    
    if (res == CURLE_OK) {
        long status_code;
        curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &status_code);
        response.status_code = status_code;
        response.success = (status_code >= 200 && status_code < 300);
    } else {
        set_error(curl_easy_strerror(res));
    }
    
    curl_slist_free_all(headers);
    curl_easy_cleanup(curl);
    
    return response;
}
#endif

// Public API implementation
star_api_result_t star_api_init(const star_api_config_t* config) {
    if (!config || !config->base_url) {
        set_error("Invalid configuration");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    std::lock_guard<std::mutex> lock(g_state.mutex);
    
    g_state.config = *config;
    g_state.initialized = true;
    
#ifndef _WIN32
    curl_global_init(CURL_GLOBAL_DEFAULT);
#endif
    
    return STAR_API_SUCCESS;
}

star_api_result_t star_api_authenticate(const char* username, const char* password) {
    if (!g_state.initialized || !username || !password) {
        set_error("Not initialized or invalid parameter");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    std::string oasis_url = std::string(g_state.config.base_url);
    // Remove /api if present to get base OASIS URL
    size_t api_pos = oasis_url.find("/api");
    if (api_pos != std::string::npos) {
        oasis_url = oasis_url.substr(0, api_pos);
    }
    
    std::string json = "{";
    json += "\"username\":\"" + std::string(username) + "\",";
    json += "\"password\":\"" + std::string(password) + "\"";
    json += "}";
    
    std::string url = oasis_url + "/api/avatar/authenticate";
    HttpResponse response = http_request("POST", url, json);
    
    if (!response.success) {
        return STAR_API_ERROR_API_ERROR;
    }
    
    // Parse JWT token from response
    // In production, use proper JSON parsing
    size_t token_pos = response.data.find("\"jwtToken\":\"");
    if (token_pos != std::string::npos) {
        token_pos += 11;
        size_t token_end = response.data.find("\"", token_pos);
        if (token_end != std::string::npos) {
            std::string jwt_token = response.data.substr(token_pos, token_end - token_pos);
            // Store token for future requests
            // Update config with token
        }
    }
    
    // Parse avatar ID
    size_t id_pos = response.data.find("\"id\":\"");
    if (id_pos != std::string::npos) {
        id_pos += 6;
        size_t id_end = response.data.find("\"", id_pos);
        if (id_end != std::string::npos) {
            std::string avatar_id = response.data.substr(id_pos, id_end - id_pos);
            // Update config with avatar ID
            g_state.config.avatar_id = strdup(avatar_id.c_str());
        }
    }
    
    return STAR_API_SUCCESS;
}

void star_api_cleanup(void) {
    std::lock_guard<std::mutex> lock(g_state.mutex);
    g_state.initialized = false;
    
#ifndef _WIN32
    curl_global_cleanup();
#endif
}

bool star_api_has_item(const char* item_name) {
    if (!g_state.initialized || !item_name) {
        set_error("Not initialized or invalid parameter");
        return false;
    }
    
    std::string url = std::string(g_state.config.base_url) + "/api/inventoryitems/user/" + g_state.config.avatar_id;
    HttpResponse response = http_request("GET", url);
    
    if (!response.success) {
        return false;
    }
    
    // Simple JSON parsing to check for item name
    // In production, use a proper JSON library
    std::string search_name = item_name;
    std::transform(search_name.begin(), search_name.end(), search_name.begin(), ::tolower);
    
    std::string lower_data = response.data;
    std::transform(lower_data.begin(), lower_data.end(), lower_data.begin(), ::tolower);
    
    return lower_data.find("\"name\":\"" + search_name) != std::string::npos ||
           lower_data.find("\"description\":\"" + search_name) != std::string::npos;
}

star_api_result_t star_api_get_inventory(star_item_list_t** item_list) {
    if (!g_state.initialized || !item_list) {
        set_error("Not initialized or invalid parameter");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    std::string url = std::string(g_state.config.base_url) + "/api/inventoryitems/user/" + g_state.config.avatar_id;
    HttpResponse response = http_request("GET", url);
    
    if (!response.success) {
        return STAR_API_ERROR_API_ERROR;
    }
    
    // Allocate item list
    *item_list = (star_item_list_t*)malloc(sizeof(star_item_list_t));
    if (!*item_list) {
        set_error("Memory allocation failed");
        return STAR_API_ERROR_INIT_FAILED;
    }
    
    (*item_list)->count = 0;
    (*item_list)->capacity = 10;
    (*item_list)->items = (star_item_t*)malloc(sizeof(star_item_t) * (*item_list)->capacity);
    
    // Simple parsing - in production, use proper JSON library
    // For now, return empty list
    (*item_list)->count = 0;
    
    return STAR_API_SUCCESS;
}

void star_api_free_item_list(star_item_list_t* item_list) {
    if (item_list) {
        if (item_list->items) {
            free(item_list->items);
        }
        free(item_list);
    }
}

star_api_result_t star_api_add_item(
    const char* item_name,
    const char* description,
    const char* game_source,
    const char* item_type
) {
    if (!g_state.initialized || !item_name || !description || !game_source) {
        set_error("Not initialized or invalid parameter");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    // Build JSON request
    std::string json = "{";
    json += "\"Name\":\"" + std::string(item_name) + "\",";
    json += "\"Description\":\"" + std::string(description) + " | Source: " + std::string(game_source) + "\",";
    json += "\"HolonType\":\"InventoryItem\",";
    json += "\"MetaData\":{";
    json += "\"GameSource\":\"" + std::string(game_source) + "\",";
    json += "\"ItemType\":\"" + std::string(item_type ? item_type : "KeyItem") + "\",";
    json += "\"CrossGameItem\":true";
    json += "}";
    json += "}";
    
    std::string url = std::string(g_state.config.base_url) + "/api/inventoryitems";
    HttpResponse response = http_request("POST", url, json);
    
    if (!response.success) {
        return STAR_API_ERROR_API_ERROR;
    }
    
    return STAR_API_SUCCESS;
}

bool star_api_use_item(const char* item_name, const char* context) {
    if (!g_state.initialized || !item_name) {
        set_error("Not initialized or invalid parameter");
        return false;
    }
    
    // First check if item exists
    if (!star_api_has_item(item_name)) {
        return false;
    }
    
    // Build use request JSON
    std::string json = "{";
    json += "\"Context\":\"" + std::string(context ? context : "game_use") + "\"";
    json += "}";
    
    // Note: We need the item ID to use it, which requires parsing the inventory
    // For now, this is a simplified implementation
    // In production, you'd fetch the item ID first
    
    return true;
}

star_api_result_t star_api_start_quest(const char* quest_id) {
    if (!g_state.initialized || !quest_id) {
        set_error("Not initialized or invalid parameter");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    std::string url = std::string(g_state.config.base_url) + "/api/quests/" + std::string(quest_id) + "/start";
    HttpResponse response = http_request("POST", url);
    
    if (!response.success) {
        return STAR_API_ERROR_API_ERROR;
    }
    
    return STAR_API_SUCCESS;
}

star_api_result_t star_api_complete_quest_objective(const char* quest_id, const char* objective_id, const char* game_source) {
    if (!g_state.initialized || !quest_id || !objective_id) {
        set_error("Not initialized or invalid parameter");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    std::string json = "{";
    json += "\"objectiveId\":\"" + std::string(objective_id) + "\",";
    json += "\"completed\":true,";
    json += "\"gameSource\":\"" + std::string(game_source ? game_source : "Unknown") + "\"";
    json += "}";
    
    std::string url = std::string(g_state.config.base_url) + "/api/quests/" + std::string(quest_id) + "/objectives/" + std::string(objective_id);
    HttpResponse response = http_request("PUT", url, json);
    
    if (!response.success) {
        return STAR_API_ERROR_API_ERROR;
    }
    
    return STAR_API_SUCCESS;
}

star_api_result_t star_api_complete_quest(const char* quest_id) {
    if (!g_state.initialized || !quest_id) {
        set_error("Not initialized or invalid parameter");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    std::string url = std::string(g_state.config.base_url) + "/api/quests/" + std::string(quest_id) + "/complete";
    HttpResponse response = http_request("POST", url);
    
    if (!response.success) {
        return STAR_API_ERROR_API_ERROR;
    }
    
    return STAR_API_SUCCESS;
}

star_api_result_t star_api_create_boss_nft(
    const char* boss_name,
    const char* description,
    const char* game_source,
    const char* boss_stats,
    char* nft_id_out
) {
    if (!g_state.initialized || !boss_name || !nft_id_out) {
        set_error("Not initialized or invalid parameter");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    std::string json = "{";
    json += "\"Name\":\"" + std::string(boss_name) + "\",";
    json += "\"Description\":\"" + std::string(description ? description : "Boss from game") + "\",";
    json += "\"Type\":\"Boss\",";
    json += "\"MetaData\":{";
    json += "\"GameSource\":\"" + std::string(game_source ? game_source : "Unknown") + "\",";
    json += "\"BossStats\":" + std::string(boss_stats ? boss_stats : "{}") + ",";
    json += "\"DefeatedAt\":\"" + std::string(__DATE__) + "\",";
    json += "\"Deployable\":true";
    json += "}";
    json += "}";
    
    std::string url = std::string(g_state.config.base_url) + "/api/nfts";
    HttpResponse response = http_request("POST", url, json);
    
    if (!response.success) {
        return STAR_API_ERROR_API_ERROR;
    }
    
    // Parse NFT ID from response
    size_t id_pos = response.data.find("\"id\":\"");
    if (id_pos != std::string::npos) {
        id_pos += 6;
        size_t id_end = response.data.find("\"", id_pos);
        if (id_end != std::string::npos) {
            std::string nft_id = response.data.substr(id_pos, id_end - id_pos);
            strncpy(nft_id_out, nft_id.c_str(), 63);
            nft_id_out[63] = '\0';
        }
    }
    
    return STAR_API_SUCCESS;
}

star_api_result_t star_api_deploy_boss_nft(const char* nft_id, const char* target_game, const char* location) {
    if (!g_state.initialized || !nft_id || !target_game) {
        set_error("Not initialized or invalid parameter");
        return STAR_API_ERROR_INVALID_PARAM;
    }
    
    std::string json = "{";
    json += "\"nftId\":\"" + std::string(nft_id) + "\",";
    json += "\"targetGame\":\"" + std::string(target_game) + "\",";
    json += "\"location\":\"" + std::string(location ? location : "default") + "\"";
    json += "}";
    
    std::string url = std::string(g_state.config.base_url) + "/api/nfts/" + std::string(nft_id) + "/deploy";
    HttpResponse response = http_request("POST", url, json);
    
    if (!response.success) {
        return STAR_API_ERROR_API_ERROR;
    }
    
    return STAR_API_SUCCESS;
}

const char* star_api_get_last_error(void) {
    std::lock_guard<std::mutex> lock(g_state.mutex);
    return g_state.last_error.c_str();
}

void star_api_set_callback(star_api_callback_t callback, void* user_data) {
    std::lock_guard<std::mutex> lock(g_state.mutex);
    g_state.callback = callback;
    g_state.callback_user_data = user_data;
}

