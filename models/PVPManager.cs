using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public PVPManager(Character player, PVPPlayerType playerType, bool debugMode): base(player, debugMode, computerMode: false) {
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
                try {
                    Dictionary<string, string> readyUpdateResponse = JSON.Deserialize<Dictionary<string, string>>(readyUpdateJSON);
                    if (readyUpdateResponse.ContainsKey("error")) {
                        throw new Exception($"Error response received from server: {readyUpdateResponse["error"]}");
                    } else {
                        return;
                    }
                } catch (Exception err) {
                    Logger.Log($"PVPMANAGER READYUPDATE ERROR: {err.Message}");
                    Console.WriteLine("Failed to tell game server that you are ready.");
                    if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                        terminateGame = true;
                        return;
                    } else {
                        Console.WriteLine("Re-trying...");
                        readyUpdateJSON = server.SendReadyEventUpdate();
                    }
                }
            }
        }

        public void RollingDiceNow() {
            string rollingUpdateJSON = server.SendRollingDiceEventUpdate(player.progress);
            while (true) {
                try {
                    Dictionary<string, string> rollingUpdateResponse = JSON.Deserialize<Dictionary<string, string>>(rollingUpdateJSON);
                    if (rollingUpdateResponse.ContainsKey("error")) {
                        throw new Exception($"Error response received from server: {rollingUpdateResponse["error"]}");
                    } else {
                        return;
                    }
                } catch (Exception err) {
                    Logger.Log($"PVPMANAGER ROLLINGDICENOW ERROR: {err.Message}");
                    Console.WriteLine("Failed to tell game server that you are rolling dice now.");
                    if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                        terminateGame = true;
                        return;
                    } else {
                        Console.WriteLine("Re-trying...");
                        rollingUpdateJSON = server.SendRollingDiceEventUpdate(player.progress);
                    }
                }
            }
        }

        public void DiceRolled(int diceRoll) {
            string diceRolledUpdateJSON = server.SendDiceRolledUpdate(player.progress, diceRoll);
            while (true) {
                try {
                    Dictionary<string, string> diceRolledUpdateResponse = JSON.Deserialize<Dictionary<string, string>>(diceRolledUpdateJSON);
                    if (diceRolledUpdateResponse.ContainsKey("error")) {
                        throw new Exception($"Error response received from server: {diceRolledUpdateResponse["error"]}");
                    } else {
                        return;
                    }
                } catch (Exception err) {
                    Logger.Log($"PVPMANAGER DICEROLLED ERROR: {err.Message}");
                    Console.WriteLine("Failed to tell game server that you have rolled the dice.");
                    if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                        terminateGame = true;
                        return;
                    } else {
                        Console.WriteLine("Re-trying...");
                        diceRolledUpdateJSON = server.SendDiceRolledUpdate(player.progress, diceRoll);
                    }
                }
            }
        }

        public void PowerupActivated(Powerup powerup, string output) {
            string powerupActivatedJSON = server.SendPowerupActivatedUpdate(player.progress, player2.progress, player.hp, player2.hp, powerup, output);
            while (true) {
                try {
                    Dictionary<string, string> powerupActivatedResponse = JSON.Deserialize<Dictionary<string, string>>(powerupActivatedJSON);
                    if (powerupActivatedResponse.ContainsKey("error")) {
                        throw new Exception($"Error response received from server: {powerupActivatedResponse["error"]}");
                    } else {
                        return;
                    }
                } catch (Exception err) {
                    Logger.Log($"PVPMANAGER POWERUPACTIVATED ERROR: {err.Message}");
                    Console.WriteLine("Failed to tell game server that you have activated a powerup.");
                    if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                        terminateGame = true;
                        return;
                    } else {
                        Console.WriteLine("Re-trying...");
                        powerupActivatedJSON = server.SendPowerupActivatedUpdate(player.progress, player2.progress, player.hp, player2.hp, powerup, output);
                    }
                }
            }
        }

        public void TurnOver() {
            string turnOverJSON = server.SendTurnOverUpdate(player.progress, player2.progress);
            while (true) {
                try {
                    Dictionary<string, string> turnOverResponse = JSON.Deserialize<Dictionary<string, string>>(turnOverJSON);
                    if (turnOverResponse.ContainsKey("error")) {
                        throw new Exception($"Error response received from server: {turnOverResponse["error"]}");
                    } else {
                        return;
                    }
                } catch (Exception err) {
                    Logger.Log($"PVPMANAGER TURNOVER ERROR: {err.Message}");
                    Console.WriteLine("Failed to tell game server that your turn is over.");
                    if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                        terminateGame = true;
                        return;
                    } else {
                        Console.WriteLine("Re-trying...");
                        turnOverJSON = server.SendTurnOverUpdate(player.progress, player2.progress);
                    }
                }
            }
        }

        public void GameOverUpdate(bool won, string reason) {
            string gameOverAckJSON = server.GameOverAck(player.progress, player2.progress, won, reason);
            while (true) {
                try {
                    Dictionary<string, string> gameOverAckResponse = JSON.Deserialize<Dictionary<string, string>>(gameOverAckJSON);
                    if (gameOverAckResponse.ContainsKey("error")) {
                        throw new Exception($"Error response received from server: {gameOverAckResponse["error"]}");
                    } else {
                        return;
                    }
                } catch (Exception err) {
                    Logger.Log($"PVPMANAGER GAMEOVERUPDATE ERROR: {err.Message}");
                    Console.WriteLine("Failed to tell game server that the game is over.");
                    if (Misc.Input("Try again? (Y/N) ").ToLower() != "y") {
                        terminateGame = true;
                        return;
                    } else {
                        Console.WriteLine("Re-trying...");
                        gameOverAckJSON = server.GameOverAck(player.progress, player2.progress, won, reason);
                    }
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

                bool p1Ready = serverGame.eventUpdates.Any(e => e.player == "Player1" && e.eventType == "Ready");
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

        public string TranslatedCurrentPlayerID() {
            return server.playerID == "P1" ? "Player1" : "Player2";
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
            
            if (!debugMode) {
                DisplayStartingAnimation();
            }

            while (player.progress < progressGoal && player2.progress < progressGoal && player.hp > 0 && player2.hp > 0 && serverGame.winner == null) {
                FetchGame();
                if (terminateGame) {
                    return;
                }
                ProduceVisuals();

                string translatedCurrentPlayerID = server.playerID == "P1" ? "Player1" : "Player2";
                if (serverGame.currentTurn == translatedCurrentPlayerID) {
                    playerTurn();
                    if (terminateGame) {
                        return;
                    }
                } else {
                    player2Turn();
                    if (terminateGame) {
                        return;
                    }
                }
            }

            // Get latest updates to process how the game ended (while loop condition covers a lot of cases)
            FetchGame();
            if (terminateGame) {
                return;
            }
            player.progress = server.playerID == "P1" ? serverGame.player1.progress : serverGame.player2.progress;
            player.hp = server.playerID == "P1" ? int.Parse(serverGame.player1.hp) : int.Parse(serverGame.player2.hp);

            player2.progress = server.playerID == "P1" ? serverGame.player2.progress : serverGame.player1.progress;
            player2.hp = server.playerID == "P1" ? int.Parse(serverGame.player2.hp) : int.Parse(serverGame.player1.hp);
            
            Console.WriteLine();
            if (player.progress >= progressGoal || player2.hp <= 0) {
                if (player.progress >= progressGoal) {
                    GameOverUpdate(true, $"{player.repName} CROSSES THE CHECKERED FLAG AND WINS!");
                    Console.WriteLine($"{player.repName} CROSSES THE CHECKERED FLAG AND WINS!");
                } else {
                    GameOverUpdate(true, $"{player2.repName} lost their HP - {player.repName} WINS!");
                    Console.WriteLine($"{player2.repName} lost their HP - {player.repName} WINS!");
                }
                player.xpBonus += 200;

                for (int i = 0; i < 5; i++) {
                    Console.Write("🎉");
                    Thread.Sleep(500);
                }
                Console.WriteLine();
                winner = GameCharacterType.Player;
            } else if (player2.progress >= progressGoal || player.hp <= 0) {
                if (player2.progress >= progressGoal) {
                    GameOverUpdate(false, $"{player2.repName} CROSSES THE CHECKERED FLAG AND WINS!");
                    Console.WriteLine($"{player2.repName} CROSSES THE CHECKERED FLAG AND WINS!");
                } else {
                    GameOverUpdate(false, $"{player.repName} lost their HP - {player2.repName} WINS!");
                    Console.WriteLine($"{player.repName} lost their HP - {player2.repName} WINS!");
                }
                Console.WriteLine("You lost! Better luck next time. ;(");
                player2.xpBonus += 200;
                winner = GameCharacterType.Player2;
            }

            Console.WriteLine();
            Console.WriteLine("GAME OVER!");
            Console.WriteLine();
            Console.WriteLine();
        }

        public override void playerTurn()
        {
            bool playerLeading = player.progress > player2.progress;
            Console.WriteLine(server.playerID);
            Console.WriteLine($"[{player.repName}] It's your turn! Roll a dice by pressing enter.");
            Console.Read();

            // Send rolling event update
            RollingDiceNow();
            if (terminateGame) {
                return;
            }
            UpdateVisualsWithStatement($"[{player.repName}] Rolling dice...");
            Thread.Sleep(1000);
            int diceRoll = rollDice();

            player.progress += diceRoll;
            Powerup? powerup = Powerup.FindByPosition(player.progress, powerups);
            player.addHistoryItem(
                actor: HistoryItemActor.Player,
                roll: diceRoll,
                action: "Rolled dice",
                resultantProgress: player.progress,
                powerup: powerup
            );

            // Update server with dice roll and player progress
            DiceRolled(diceRoll);
            if (terminateGame) {
                return;
            }
            UpdateVisualsWithStatement($"[{player.repName}] You rolled a {diceRoll}!");
            
            if (powerup != null) {
                Console.WriteLine($"[{player.repName}] Landed on a powerup: {powerup.name}");
                string output = landedOnPowerup(GameCharacterType.Player, powerup, true);
                PowerupActivated(powerup, output);
                if (terminateGame) {
                    return;
                }
            }

            Console.WriteLine();
            if (player.progress > player2.progress && (playerLeading != player.progress > player2.progress)) {
                Console.WriteLine($"[{player.repName}] You are now IN THE LEAD!");
            }

            Console.WriteLine("Turn over. Press enter to continue.");
            Console.Read();
            TurnOver();
            return;
        }

        public void player2Turn() {
            Console.WriteLine($"[{player2.repName}] It's {player2.repName}'s turn! Please wait.");
            while (true) {
                Thread.Sleep(1000);
                FetchGame();
                if (terminateGame || serverGame.winner != null) {
                    return;
                }
                
                player2.progress = serverGame.player1.progress;
                player2.skipNextTurn = serverGame.player1.skipNextTurn;

                List<ServerEventUpdate> unseenEvents = serverGame.GetUnseenEvents();
                foreach (var unseenEvent in unseenEvents) {
                    if (unseenEvent.player != TranslatedCurrentPlayerID()) {
                        if (unseenEvent.value.StartsWith("(F)")) {
                            UpdateVisualsWithStatement($"[{player2.repName}] {unseenEvent.value.Substring(3)}");
                        } else {
                            Console.WriteLine($"[{player2.repName}] {unseenEvent.value}");
                        }
                    }
                }

                if (serverGame.currentTurn == TranslatedCurrentPlayerID()) {
                    Console.WriteLine("it's your turn now!");
                    break;
                }
            }
        }
    }
}