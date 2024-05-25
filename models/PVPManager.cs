using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using Extensions;
using Newtonsoft.Json;

namespace MushroomPocket {
    enum PVPPlayerType {
        Player1,
        Player2
    }
    class PVPManager: GameManager {
        public PVPPlayerType playerType;
        public static GameServer server = new GameServer();
        public ServerGame serverGame;
        public bool terminateGame = false;

        public PVPManager(Character player, PVPPlayerType playerType, bool debugMode): base(player, debugMode) {
            this.playerType = playerType;
        }

        public void CheckConnection() {
            // Connect to game server
            while (!server.Connect()) {
                if (Misc.Input("Failed to connect to game server. Try again? (Y/N) ").ToLower() != "y") {
                    terminateGame = true;
                    return;
                }
            }
        }

        public void CreateGameSession() {
            RequestGameCodeParams player1Data = new RequestGameCodeParams(
                player.name,
                player.hp.ToString(),
                player.exp.ToString(),
                player.skill.ToString(),
                player.emoji,
                player.repName,
                progressGoal.ToString()
            );
            Dictionary<string, string>? gameData = server.RequestGameCode(player1Data);
            while (gameData == null) {
                if (Misc.Input("Failed to request game code. Try again? (Y/N) ").ToLower() != "y") {
                    terminateGame = true;
                    return;
                }
                gameData = server.RequestGameCode(player1Data);
            }

            server.SetGameCode(gameData["code"]);
            server.SetPlayerID("P1");
        }

        public bool FetchGame() {
            try {
                string? gameJSON = server.GetGameStatus();
                // if (debugMode) {
                //     Console.WriteLine(gameJSON);
                // }
                
                // See if an error object was given
                if (gameJSON == null) {
                    throw new Exception("Null response received from server.");
                }
                if (gameJSON.Contains("error")) {
                    if (JSON.Deserialize<Dictionary<string, string>>(gameJSON).ContainsKey("error")) {
                        string err = JSON.Deserialize<Dictionary<string, string>>(gameJSON)["error"];
                        if (err == "Game not found.") {
                            Logger.Log("PVPMANAGER FETCHGAME ERROR: Game not found response received. Game must and will be be aborted.");
                            Console.WriteLine("Game not found. Terminating game...");
                            terminateGame = true;
                            return false;
                        } else {
                            throw new Exception($"Error response received from server: {err}");
                        }
                    }
                }

                serverGame = JSON.Deserialize<ServerGame>(gameJSON);
                return true;
            } catch (Exception e) {
                Logger.Log($"PVPMANAGER FETCHGAME ERROR: {e.Message}");
                Console.WriteLine($"Failed to fetch game status: {e.Message}");

                if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                    terminateGame = true;
                    return false;
                } else {
                    return FetchGame();
                }
            }
        }

        public void DeclareReady() {
            string readyUpdateJSON = server.SendReadyEventUpdate();
            while (true) {
                Dictionary<string, string> readyUpdateResponse = JSON.Deserialize<Dictionary<string, string>>(readyUpdateJSON);
                if (readyUpdateResponse.ContainsKey("error")) {
                    Logger.Log($"PVPMANAGER READYUPDATE ERROR: Error response: {readyUpdateResponse["error"]}");
                    Console.WriteLine("Failed to tell game server that you are ready.");
                    if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                        terminateGame = true;
                        return;
                    } else {
                        Console.WriteLine("Re-trying...");
                        readyUpdateJSON = server.SendReadyEventUpdate();
                    }
                } else {
                    return;
                }
            }
        }

        public void Player1Setup() {
            // Request new game session from server
            Console.Clear();
            Console.WriteLine("Connected to game server! Requesting new game session...");
            string userRepName = Misc.SafeInputWithPredicate("Enter your name: ", (string input) => !string.IsNullOrWhiteSpace(input));
            player.repName = userRepName;
            CreateGameSession();
            if (terminateGame) {
                return;
            }
            
            // Display game code for player 2 to join
            Console.Clear();
            Console.WriteLine("You're connected to the game server for a PVP game!");
            Console.WriteLine($"Player 2, use this game code to join the game: {server.gameCode}");
            Console.WriteLine();
            Console.WriteLine("Waiting for Player 2 to join...");
            while (true) {
                Thread.Sleep(1000);
                if (debugMode) {
                    Console.WriteLine("Fetching updates...");
                }
                FetchGame();
                if (terminateGame) {
                    return;
                }
                
                // Check if player 2 has joined on the server
                if (serverGame.player2Joined()) {
                    // Update PVPManager player 2
                    player2 = new GameCharacter(new Character(serverGame.player2.characterName, int.Parse(serverGame.player2.hp), int.Parse(serverGame.player2.exp), serverGame.player2.skill, serverGame.player2.emoji));
                    player2.repName = serverGame.player2.repName;
                    Console.Clear();
                    Console.WriteLine($"{serverGame.player2.repName} has joined the game!");
                    break;
                }
            }

            // Send Ready event update
            DeclareReady();
            if (terminateGame) {
                return;
            }

            // Check if Player 2 is ready
            Console.WriteLine();
            Console.WriteLine($"{serverGame.player2.repName}'s game might take a second to get ready.");
            Console.WriteLine("Waiting for Player 2 to get ready...");
            while (true) {
                Thread.Sleep(1000);
                FetchGame();
                if (terminateGame) {
                    return;
                }

                bool p2Ready = serverGame.eventUpdates.Any(e => e.player == "Player2" && e.eventType == "Ready");
                if (p2Ready) {
                    Console.Clear();
                    Console.WriteLine($"{serverGame.player2.repName} is ready!");
                    break;
                }
            }

            // Start the game
            Console.WriteLine("Starting game...");
            Thread.Sleep(2000);
            return;
        }

        public void Player2Setup() {
            // Prompt player 2 for game code
            Console.Clear();
            string gameCode;
            JoinGameResult joinGameResult;

            string userRepName = Misc.SafeInputWithPredicate("Enter your name: ", (string input) => !string.IsNullOrWhiteSpace(input));
            player.repName = userRepName;

            // Join the game
            while (true) {
                gameCode = Misc.Input("Enter the game code: ");
                Console.WriteLine();
                Console.WriteLine("Joining game...");
                Thread.Sleep(1000);
                try {
                    string? joinGameResponse = server.JoinGameSession(gameCode, new RequestGameCodeParams(
                        player.name,
                        player.hp.ToString(),
                        player.exp.ToString(),
                        player.skill.ToString(),
                        player.emoji,
                        player.repName,
                        progressGoal.ToString()
                    ));

                    // Check if null response was received
                    if (joinGameResponse == null) {
                        throw new Exception("Null response received from server.");
                    }

                    // Check if error response was received
                    if (joinGameResponse.Contains("error")) {
                        if (JSON.Deserialize<Dictionary<string, string>>(joinGameResponse).ContainsKey("error")) {
                            string err = JSON.Deserialize<Dictionary<string, string>>(joinGameResponse)["error"];
                            if (err == "Game not found.") {
                                throw new Exception("Game not found.");
                            } else {
                                throw new Exception($"Error response received from server: {err}");
                            }
                        }
                    }

                    // Decode join game response
                    joinGameResult = JSON.Deserialize<JoinGameResult>(joinGameResponse);
                    break;
                } catch (Exception e) {
                    Logger.Log($"PVPMANAGER PLAYER2SETUP ERROR: {e.Message}");
                    Console.WriteLine($"Failed to join game: {e.Message}");
                    if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                        terminateGame = true;
                        return;
                    } else {
                        continue;
                    }
                }
            }

            // Process game parameters and player 1 details
            Console.WriteLine();
            Console.WriteLine("Processing game parameters...");
            server.SetGameCode(gameCode);
            server.SetPlayerID("P2"); // on the server, user is Player 2

            // Set local player 2 (opponent) as game player 1 details as given by server
            this.player2 = new GameCharacter(new Character(
                joinGameResult.gameParameters.player1.characterName,
                int.Parse(joinGameResult.gameParameters.player1.hp),
                int.Parse(joinGameResult.gameParameters.player1.exp),
                joinGameResult.gameParameters.player1.skill,
                joinGameResult.gameParameters.player1.emoji
            ));
            this.player2.repName = joinGameResult.gameParameters.player1.repName;

            // Enforce local progress window
            progressGoal = int.Parse(joinGameResult.gameParameters.progressGoal);
            while (Console.WindowWidth < (progressGoal + 10) * 2) {
                Console.WriteLine($"You need at least {(progressGoal + 10) * 2} columns to play this game. Your console window has {Console.WindowWidth} columns.");
                Console.WriteLine("Please resize your console window to fit the game board. Do not resize your window mid-game. Hit enter when done.");
                Console.Read();
            }
            RePositionPowerups();

            // Send Ready event update
            DeclareReady();
            if (terminateGame) {
                return;
            }

            // Check if Player 1 is ready
            Console.Clear();
            Console.WriteLine($"Joined game successfully! You're ready to play. You're playing against {player2.repName}.");
            Console.WriteLine();
            Console.WriteLine($"Waiting for {player2.repName} to get ready...");
            while (true) {
                Thread.Sleep(1000);
                FetchGame();
                if (terminateGame) {
                    return;
                }

                bool p1Ready = serverGame.GetUnseenEvents().Any(e => e.player == "Player1" && e.eventType == "Ready");
                if (p1Ready) {
                    Console.Clear();
                    Console.WriteLine($"{serverGame.player1.repName} is ready!");
                    break;
                }
            }

            // Start the game
            Console.WriteLine("Starting game...");
            Thread.Sleep(2000);
            return;
        }

        public override void mainLoop()
        {
            // Check connection to game server
            Console.Clear();
            Console.WriteLine("Connecting to game server...");
            CheckConnection();
            if (terminateGame) {
                return;
            }

            if (playerType == PVPPlayerType.Player1) {
                Player1Setup();
                if (terminateGame) {
                    return;
                }
            } else {
                Player2Setup();
                if (terminateGame) {
                    return;
                }
            }

            DisplayStartingAnimation();
            ProduceVisuals();
            Console.WriteLine("reached!");
        }
    }
}