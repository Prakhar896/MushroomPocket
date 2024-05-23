using System;
using System.Collections.Generic;
using Extensions;

namespace MushroomPocket {
    class PVPManager(Character player, bool debugMode): GameManager(player, debugMode) {
        public static GameServer server = new GameServer();
        public bool terminateGame = false;
        public override void mainLoop()
        {
            Console.Clear();

            // Connect to game server
            Console.WriteLine("Connecting to game server...");
            while (!server.Connect()) {
                Console.Clear();
                if (Misc.Input("Failed to connect to game server. Try again? (Y/N)").ToLower() != "y") {
                    return;
                }
            }
            Console.Clear();

            Console.WriteLine("Connected to game server! Requesting new game session...");
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
                    return;
                }
                gameData = server.RequestGameCode(player1Data);
            }
            Console.Clear();
            Console.WriteLine("You're connected to the game server for a PVP game!");
            Console.WriteLine($"Player 2, use this game code to join the game: {gameData["code"]}");
            Console.WriteLine();
            Console.WriteLine("Waiting for Player 2 to join...");
            Console.Read();
        }
    }
}