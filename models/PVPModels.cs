using System;
using System.Collections.Generic;
using System.Linq;
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

    class JoinGameParameters {
        public ServerPlayer player1;
        public string progressGoal;
    }

    class JoinGameResult {
        public JoinGameParameters gameParameters;
        public string message;
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

    #nullable enable
    class ServerGame {
        public required string code;
        public required string created;
        public required string currentTurn;
        public List<ServerEventUpdate> eventUpdates;
        public ServerPlayer? player1;
        public ServerPlayer? player2;
        public required string progressGoal;
        public string? winner;

        public bool player2Joined() {
            return this.player2 != null;
        }

        public List<ServerEventUpdate> GetUnseenEvents() {
            return this.eventUpdates.Where(e => !e.acknowledged).ToList();
        }

        public override string ToString()
        {
            return $"Game Code: {code}\n" +
                   $"Created: {created}\n" +
                   $"Current Turn: {currentTurn}\n" +
                   $"Player 1: {player1?.repName}\n" +
                   $"Player 2: {player2?.repName}\n" +
                   $"Progress Goal: {progressGoal}\n" +
                   $"Winner: {winner?.ToString() ?? "None"}";
        }
    }
    #nullable disable

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
            Dictionary<string, string>? response = PostJSONTResult<Dictionary<string, string>>("/requestGameCode", JSON.Serialize(player1Data));

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

        public string JoinGameSession(string gameCode, RequestGameCodeParams player2Data) {
            string? response = PostJSONStringResult("/joinGame", JSON.Serialize(new {
                code = gameCode,
                name = player2Data.name,
                hp = player2Data.hp,
                exp = player2Data.exp,
                skill = player2Data.skill,
                emoji = player2Data.emoji,
                repName = player2Data.repName,
            }));

            if (response == null) {
                Logger.Log("GAMESERVER JOINGAMESESSION ERROR: Failed to join game session. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }

        public string GetGameStatus() {
            string? response = PostJSONStringResult("/getGameStatus", JSON.Serialize(new {
                code = gameCode, 
                playerID = playerID 
            }));

            if (response == null) {
                Logger.Log("GAMESERVER GETGAMESTATUS ERROR: Failed to get game status. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }

        public string SendReadyEventUpdate() {
            string playerNum = playerID == "P1" ? "1" : "2";
            string? response = PostJSONStringResult("/sendEventUpdate", JSON.Serialize(new {
                code = gameCode, 
                playerID = playerID, 
                eventType = "Ready",
                value = $"Player {playerNum} is ready to start!",
                progress = 0
            }));

            if (response == null) {
                Logger.Log("GAMESERVER SENDREADYEVENTUPDATE ERROR: Failed to send ready event update. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }

        public string SendRollingDiceEventUpdate(int playerProgress) {
            string? response = PostJSONStringResult("/sendEventUpdate", JSON.Serialize(new {
                code = gameCode,
                playerID = playerID,
                eventType = "RollingDice",
                value = "(F)Rolling dice...",
                progress = playerProgress
            }));

            if (response == null) {
                Logger.Log("GAMESERVER SENDROLLINGDICEEVENTUPDATE ERROR: Failed to send rolling dice event update. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }

        public string SendDiceRolledUpdate(int playerProgress, int diceRoll) {
            string? response = PostJSONStringResult("/sendEventUpdate", JSON.Serialize(new {
                code = gameCode,
                playerID = playerID,
                eventType = "DiceRolled",
                value = $"(F)Rolled a {diceRoll}!",
                progress = playerProgress
            }));

            if (response == null) {
                Logger.Log("GAMESERVER SENDDICEROLLEDUPDATE ERROR: Failed to send dice rolled event update. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }

        public string SendPowerupActivatedUpdate(int playerProgress, int p2Progress, int p1HP, int p2HP, Powerup powerup, string powerupOutput) {
            string? response = PostJSONStringResult("/sendEventUpdate", JSON.Serialize(new {
                code = gameCode,
                playerID = playerID,
                eventType = "PowerupActivated",
                value = $"Landed on a powerup: {powerup.name}! {powerupOutput}",
                progress = playerProgress,
                p2Progress = p2Progress,
                p1HP = p1HP,
                p2HP = p2HP,
                skipNextTurn = powerup.id == "C"
            }));

            if (response == null) {
                Logger.Log("GAMESERVER SENDPOWERUPACTIVATEDUPDATE ERROR: Failed to send landed on powerup update. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }

        public string SendTurnOverUpdate(int playerProgress, int p2Progress) {
            string? response = PostJSONStringResult("/sendEventUpdate", JSON.Serialize(new {
                code = gameCode,
                playerID = playerID,
                eventType = "TurnOver",
                value = "Turn over!",
                progress = playerProgress,
                p2Progress = p2Progress
            }));

            if (response == null) {
                Logger.Log("GAMESERVER SENDTURNOVERUPDATE ERROR: Failed to send turn over update. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }

        public string GameOverAck(int playerProgress, int p2Progress, bool won, string reason) {
            string? response = PostJSONStringResult("/sendEventUpdate", JSON.Serialize(new {
                code = gameCode,
                playerID = playerID,
                eventType = "GameOverAck",
                progress = playerProgress,
                p2Progress = p2Progress,
                value = reason,
                won = won
            }));
            
            if (response == null) {
                Logger.Log("GAMESERVER GAMEOVERACK ERROR: Failed to send game over ack. Error: Null response received.");
                return null;
            } else {
                return response;
            }
        }
    }
}