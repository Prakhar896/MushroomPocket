using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using Extensions;
using Newtonsoft.Json;

namespace MushroomPocket {
    class PVPManager(Character player, bool debugMode): GameManager(player, debugMode) {
        public static GameServer server = new GameServer();
        public ServerGame serverGame;
        public bool terminateGame = false;

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
                if (gameJSON.Contains("error")) {
                    if (JSON.Deserialize<Dictionary<string, string>>(gameJSON).ContainsKey("error")) {
                        string err = JSON.Deserialize<Dictionary<string, string>>(gameJSON)["error"];
                        Logger.Log($"PVPMANAGER FETCHGAME ERROR: Error response received from server: {err}");
                        if (err == "Game not found.") {
                            Console.WriteLine("Game not found. Terminating game...");
                            terminateGame = true;
                            return false;
                        } else {
                            throw new Exception(err);
                        }
                    }
                }

                if (gameJSON != null) {
                    serverGame = JSON.Deserialize<ServerGame>(gameJSON);
                }
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

        public override void mainLoop()
        {
            // Check connection to game server
            Console.Clear();
            Console.WriteLine("Connecting to game server...");
            CheckConnection();
            if (terminateGame) {
                return;
            }

            // Request new game session from server
            Console.Clear();
            Console.WriteLine("Connected to game server! Requesting new game session...");
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

                bool p2Ready = serverGame.GetUnseenEvents().Any(e => e.player == "Player2" && e.eventType == "Ready");
                if (p2Ready) {
                    Console.Clear();
                    Console.WriteLine($"{serverGame.player2.repName} is ready!");
                    break;
                }
            }

            // Start the game
            Console.WriteLine("Starting game...");
            Thread.Sleep(2000);

            DisplayStartingAnimation();
            ProduceVisuals();
            Console.WriteLine("reached!");
        }
    }
}