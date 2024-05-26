using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic;

namespace Extensions {
    public class NetworkServer {
        public string baseAddress = Env.Get("GameServerAddress") ?? "http://localhost:8000";
        public HttpClient client = new HttpClient();
        public NetworkServer() {
            client.BaseAddress = new Uri(baseAddress);
        }

        public void AddRequestHeader(string key, string value) {
            client.DefaultRequestHeaders.Add(key, value);
        }

        public void RemoveRequestHeader(string key) {
            client.DefaultRequestHeaders.Remove(key);
        }

        public void UpdateRequestHeader(string key, string value) {
            RemoveRequestHeader(key);
            AddRequestHeader(key, value);
        }

        public void ClearRequestHeaders() {
            client.DefaultRequestHeaders.Clear();
        }

        #nullable enable
        public T? GetJSON<T>(string path) {
            try {
                var response = client.GetAsync(path).Result;
                if (response.IsSuccessStatusCode) {
                    var options = new JsonSerializerOptions {
                        PropertyNameCaseInsensitive = true
                    };
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    return JsonSerializer.Deserialize<T>(responseContent, options);
                } else {
                    return default;
                }
            } catch (Exception e) {
                Logger.Log("NETWORKSERVER GETJSON ERROR: " + e.Message);
                return default;
            }
        }
        #nullable disable

        public string Get(string path) {
            try {
                var response = client.GetAsync(path).Result;
                if (response.IsSuccessStatusCode) {
                    var options = new JsonSerializerOptions {
                        PropertyNameCaseInsensitive = true
                    };
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    return responseContent;
                } else {
                    return default;
                }
            } catch (Exception e) {
                Logger.Log("NETWORKSERVER GET ERROR: " + e.Message);
                return default;
            }
        }

        #nullable enable
        public T? PostJSONTResult<T>(string path, string serializedJSON) {
            try {
                // var json = JsonSerializer.Serialize(data);
                var content = new StringContent(serializedJSON, Encoding.UTF8, "application/json");
                var response = client.PostAsync(path, content).Result;

                if (response.IsSuccessStatusCode) {
                    var options = new JsonSerializerOptions {
                        PropertyNameCaseInsensitive = true
                    };
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    return JsonSerializer.Deserialize<T>(responseContent, options);
                } else {
                    return default;
                }
            } catch (Exception e) {
                Logger.Log("NETWORKSERVER POSTJSONTRESULT ERROR: " + e.Message);
                return default;
            }
        }
        #nullable disable

        #nullable enable
        public string? PostJSONStringResult(string path, string serializedJSON) {
            try {
                // var json = JsonSerializer.Serialize(data);
                var content = new StringContent(serializedJSON, Encoding.UTF8, "application/json");
                var response = client.PostAsync(path, content).Result;

                if (response.IsSuccessStatusCode) {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    return responseContent;
                } else {
                    return default;
                }
            } catch (Exception e) {
                Logger.Log("NETWORKSERVER POSTJSONSTRINGRESULT ERROR: " + e.Message);
                return default;
            }
        }
        #nullable disable
    }
}