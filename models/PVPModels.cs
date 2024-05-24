using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Extensions;

namespace MushroomPocket {
    class RequestGameCodeParams {
        public string name;
        public string hp;
        public string exp;
        public string skill;
        public string emoji;
        public string repName;
        public string progressGoal;

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

    class ServerEventUpdate {
        public string player;
        public string eventType;
        public string value;
        public bool acknowledged;
        public string timestamp;
    }

    class ServerPlayer {
        public string characterName;
        public string hp;
        public string exp;
        public string skill;
        public string emoji;
        public string repName;
        public int progress;
        public bool skipNextTurn;
    }

    class ServerGame {
        public string code;
        public string created;
        public string currentTurn;
        public List<ServerEventUpdate> eventUpdates;
        public ServerPlayer? player1;
        public ServerPlayer? player2;
        public string progressGoal;
        public string? winner;

        public override string ToString()
        {
            return $"Game Code: {code}\n" +
                   $"Created: {created}\n" +
                   $"Current Turn: {currentTurn}\n" +
                   $"Player 1: {player1.repName}\n" +
                   $"Player 2: {player2.repName}\n" +
                   $"Progress Goal: {progressGoal}\n" +
                   $"Winner: {winner}";
        }
    }

    class GameServer: NetworkServer {
        public string gameCode = null;
        public string playerID = null;
        public GameServer(): base() {
            this.AddRequestHeader("APIKey", Env.Get("ServerAPIKey"));
            // this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetGameCode(string gameCode) {
            this.gameCode = gameCode;
        }

        public void SetPlayerID(string playerID) {
            this.playerID = playerID;
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

        public ServerGame GetGameStatus() {
            ServerGame? response = PostJSON<ServerGame>("/getGameStatus", JSON.Serialize(new { code = gameCode, playerID = playerID }));
            if (response == null) {
                Logger.Log("GAMESERVER GETGAMESTATUS ERROR: Failed to get game status. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }
    }
}