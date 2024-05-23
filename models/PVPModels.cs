using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Extensions;

namespace MushroomPocket {
    class RequestGameCodeParams {
        string name;
        string hp;
        string exp;
        string skill;
        string emoji;
        string repName;
        string progressGoal;

        public RequestGameCodeParams(string name, string hp, string exp, string skill, string emoji, string repName, string progressGoal) {
            this.name = name;
            this.hp = hp;
            this.exp = exp;
            this.skill = skill;
            this.emoji = emoji;
            this.repName = repName;
            this.progressGoal = progressGoal;
        }
    }

    class GameServer: NetworkServer {
        public GameServer(): base() {
            this.AddRequestHeader("APIKey", Environment.GetEnvironmentVariable("ServerAPIKey"));
            // this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public bool Connect() {
            Dictionary<string, string>? response = GetJSON<Dictionary<string, string>>("/health");
            if (response == null || !response.ContainsKey("status") || response["status"] != "Healthy") {
                Logger.Log("GAMESERVER CONNECT ERROR: Failed to connect to game server. Health check response was not healthy.");
                return false;
            } else {
                return true;
            }
        }

        public Dictionary<string, string>? RequestGameCode(RequestGameCodeParams player1Data) {
            Dictionary<string, string>? response = PostJSON<Dictionary<string, string>>("/requestGameCode", JSON.Serialize(player1Data));
            if (response == null) {
                Logger.Log($"GAMESERVER REQUESTGAMECODE ERROR: Failed to request game code. Error: Null response received.");
                return null;
            } else if (!response.ContainsKey("code") || response.ContainsKey("error")) {
                string errorMessage = response.ContainsKey("error") ? response["error"] : "Unknown error.";
                Logger.Log($"GAMESERVER REQUESTGAMECODE ERROR: Failed to request game code. Error: {errorMessage}");
                return null;
            } else {
                return response;
            }
        }
    }
}