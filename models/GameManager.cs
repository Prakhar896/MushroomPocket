using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Extensions;

#nullable enable
namespace MushroomPocket {
    class GameManager {
        public enum GameCharacterType {
            Player,
            Player2
        }

        public GameCharacter player;
        public GameCharacter player2;
        public int progressGoal = Console.WindowWidth / 2 - 10;
        public List<Powerup> powerups;
        public GameCharacterType? winner = null;
        public bool debugMode = false;

        // static List<Powerup> loadedPowerups = Powerup.LoadFromFile("data/powerups.json");
        // [
        //     "30 XP Boost",
        //     "Move Back 5",
        //     "Skip Turn",
        //     "Swap Progress",
        //     "Advance 3",
        //     "Damage 10",
        //     "Heal 10",
        //     "50 XP Boost"
        // ];

        public GameManager(Character player, bool debugMode = false, bool computerMode=true) {
            this.player = new GameCharacter(player);
            if (computerMode) {
                this.player2 = GameCharacter.GenerateComputer();
            } else {
                // Temporary placeholder for multiplayer mode
                this.player2 = new GameCharacter(player);
            }

            // Scale powerup positions in proportional ratio to progress goal
            this.powerups = Powerup.LoadFromFile("data/powerups.json");
            RePositionPowerups();
            this.debugMode = debugMode;
        }

        public void RePositionPowerups() {
            this.powerups = Powerup.LoadFromFile("data/powerups.json");
            foreach (Powerup powerup in powerups) {
                powerup.positionOnTrack = (int) Math.Floor((double) progressGoal * powerup.positionOnTrack / 100);
            }
        }

        public static string Dashes(int progressGoal) {
            string dashes = "";
            for (int i = 0; i < progressGoal; i++) {
                dashes += "- ";
            }
            return dashes;
        }

        public void ProduceVisuals() {
            Misc.ClearConsole();
            while (Console.WindowWidth < (progressGoal + 10) * 2) {
                Console.WriteLine("Please resize your console window to fit the game board. Do not resize your window mid-game. Hit enter when done.");
                Console.Read();
            }
            
            void PrintTrackForCharacter(GameCharacter character, GameCharacterType type) {
                Console.WriteLine($"{(type == GameCharacterType.Player ? $"[{player.repName.ToUpper()}]" : $"[{player2.repName.ToUpper()}]")}");
                Console.WriteLine(Dashes(progressGoal));
                Console.Write("START ");
                for (int i = 0; i < progressGoal; i++) {
                    Powerup? powerup = Powerup.FindByPosition(i, powerups);
                    if (i == character.progress) {
                        Console.Write($"{character.emoji} ");
                    } else if (powerup != null) {
                        Console.Write($"{powerup.id} ");
                    } else {
                        Console.Write("| ");
                    }
                }
                Console.WriteLine("END");
                Console.WriteLine(Dashes(progressGoal));
            }

            PrintTrackForCharacter(player, GameCharacterType.Player);
            PrintTrackForCharacter(player2, GameCharacterType.Player2);

            Console.WriteLine("💪 Powerups:");
            foreach (Powerup powerup in powerups) {
                Console.WriteLine($"{powerup.id}: {powerup.name}");
            }
            Console.WriteLine();
        }

        public int rollDice() {
            Random random = new Random();
            return powerups.Find((p) => p.id == "G").positionOnTrack;
            // return random.Next(1, 7);
        }

        public string? landedOnPowerup(GameCharacterType actor, Powerup powerup, bool returnString = false) {
            string toBeReturned = "";
            switch (powerup.id) {
                case "A":
                    if (actor == GameCharacterType.Player) {
                        player.xpBonus += 30;
                    } else {
                        player2.xpBonus += 30;
                    }
                    toBeReturned = "Received an additional 30 XP!";
                    Console.WriteLine("Received an additional 30 XP!");
                    break;
                case "B":
                    if (actor == GameCharacterType.Player) {
                        player.progress -= 5;
                    } else {
                        player2.progress -= 5;
                    }
                    toBeReturned = "Moved back 5 steps on the track!";
                    Console.WriteLine("Moved back 5 steps on the track!");
                    break;
                case "C":
                    if (actor == GameCharacterType.Player) {
                        player.skipNextTurn = true;
                    } else {
                        player2.skipNextTurn = true;
                    }
                    toBeReturned = "Next turn will be skipped!";
                    Console.WriteLine("Next turn will be skipped!");
                    break;
                case "D":
                    int playerProgress = player.progress;
                    player.progress = player2.progress;
                    player2.progress = playerProgress;
                    toBeReturned = "Swapped progress with the opponent!";
                    Console.WriteLine("Swapped progress with the opponent!");
                    break;
                case "E":
                    if (actor == GameCharacterType.Player) {
                        player.progress += 3;
                    } else {
                        player2.progress += 3;
                    }
                    toBeReturned = "Advanced 3 steps!";
                    Console.WriteLine("Advanced 3 steps!");
                    break;
                case "F":
                    if (actor == GameCharacterType.Player) {
                        player.doubleXPMultiplier = true;
                    } else {
                        player2.doubleXPMultiplier = true;
                    }
                    toBeReturned = "XP multiplier doubled!";
                    Console.WriteLine("XP multiplier doubled!");
                    break;
                case "G":
                    if (actor == GameCharacterType.Player) {
                        player2.hp = 0;
                    } else {
                        player.hp = 0;
                    }
                    toBeReturned = "Killed opponent! Lucky you!!! 😈";
                    Console.WriteLine("Killed opponent! Lucky you!!! 😈");
                    break;
            }
            return returnString ? toBeReturned: null;
        }

        public virtual void DisplayStartingAnimation() {
            // Introductory animation
            Console.Clear();
            Console.WriteLine("In the desolate sands of Sahara...");
            Thread.Sleep(2000);
            Console.WriteLine("Two warriors are about to face off with their grit and mighty speed...");
            Thread.Sleep(2000);
            Console.WriteLine("In the EPIC battle of...");
            Thread.Sleep(2000);
            Console.Clear();
            int toWriteIndex = 0;
            string gameTitle = "🎮 MushroomKart 🎮";
            while (toWriteIndex < gameTitle.Length)
            {
                Console.Write(gameTitle[toWriteIndex]);
                Thread.Sleep(100);
                toWriteIndex++;
            }
            Console.WriteLine();
            Console.WriteLine("Race against a computer opponent to reach the finish line first!");
            Console.WriteLine("You can land on powerups that can help or hinder your progress.");
            Console.WriteLine("The first to reach the finish line or the last one standing wins!");
            Console.WriteLine();

            Console.WriteLine($"Introducing your opponent: {player2.repName.ToUpper()}");
            Console.WriteLine($"Character Name: {player2.name}");
            Console.WriteLine($"HP: {player2.hp}");
            Console.WriteLine($"XP: {player2.exp}");
            Console.WriteLine($"Skill: {player2.skill}");
            Console.WriteLine($"Emoji: {player2.emoji}");
            Console.WriteLine();

            Thread.Sleep(1000);
            Console.Write("Press enter to start the game!");
            Console.Read();

            Console.Clear();
            Console.Write("Ready?");
            Thread.Sleep(1000);
            Console.Write(" Set?");
            Thread.Sleep(1000);
            Console.Write(" GO! 🚗💨");
            Thread.Sleep(1000);
        }

        public virtual void mainLoop() {
            if (!debugMode) {
                DisplayStartingAnimation();
            }

            // Start main flow
            GameCharacterType whoseTurn = GameCharacterType.Player;
            while (player.progress < progressGoal && player2.progress < progressGoal && player.hp > 0 && player2.hp > 0) {
                ProduceVisuals();
                if (whoseTurn == GameCharacterType.Player) {
                    if (!player.skipNextTurn) {
                        playerTurn();
                    } else {
                        Console.WriteLine($"[{player.repName}] Skipping turn!");
                        player.skipNextTurn = false;
                    }

                    whoseTurn = GameCharacterType.Player2;
                } else {
                    if (!player2.skipNextTurn) {
                        computerTurn();
                    } else {
                        Console.WriteLine($"[{player2.repName}] Skipping turn!");
                        player2.skipNextTurn = false;
                    }

                    whoseTurn = GameCharacterType.Player;
                }
            }

            if (player.progress >= progressGoal || player2.hp <= 0) {
                if (player.progress >= progressGoal) {
                    Console.WriteLine("PLAYER CROSSES THE CHECKERED FLAG AND WINS!");
                } else {
                    Console.WriteLine("Computer lost its HP - PLAYER WINS!");
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
                    Console.WriteLine("COMPUTER CROSSES THE CHECKERED FLAG AND WINS!");
                } else {
                    Console.WriteLine("Player lost its HP - COMPUTER WINS!");
                }
                player2.xpBonus += 200;
                winner = GameCharacterType.Player2;
            }

            Console.WriteLine();
            Console.WriteLine("GAME OVER!");
            Console.WriteLine();
            Console.WriteLine();
            return;
        }

        public void UpdateVisualsWithStatement(string statement) {
            ProduceVisuals();
            Console.WriteLine(statement);
        }

        public virtual void playerTurn() {
            bool playerLeading = player.progress > player2.progress;
            Console.WriteLine("[PLAYER] It's your turn! Roll a dice by pressing enter.");
            Console.Read();

            ProduceVisuals();
            Console.WriteLine("[PLAYER] Rolling dice...");
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

            UpdateVisualsWithStatement($"[PLAYER] You rolled a {diceRoll}!");

            if (powerup != null) {
                Console.WriteLine($"[PLAYER] Landed on a powerup: {powerup.name}!");
                landedOnPowerup(GameCharacterType.Player, powerup);
            }

            if (player.progress > player2.progress && (playerLeading != player.progress > player2.progress)) {
                Console.WriteLine("[PLAYER] You are now IN THE LEAD!");
            }

            Console.WriteLine("Turn over. Press enter to continue.");
            Console.Read();
            return;
        }

        public virtual void computerTurn() {
            bool computerLeading = player2.progress > player.progress;
            Console.WriteLine("[COMPUTER] Rolling dice...");
            Thread.Sleep(1000);

            int diceRoll = rollDice();
            player2.progress += diceRoll;
            Powerup? powerup = Powerup.FindByPosition(player2.progress, powerups);
            player2.addHistoryItem(
                actor: HistoryItemActor.Computer,
                roll: diceRoll,
                action: "Rolled dice",
                resultantProgress: player2.progress,
                powerup: powerup
            );

            UpdateVisualsWithStatement($"[COMPUTER] Computer rolled a {diceRoll}!");

            if (powerup != null) {
                Console.WriteLine($"[COMPUTER] Landed on a powerup: {powerup.name}!");
                landedOnPowerup(GameCharacterType.Player2, powerup);
            }

            if (player2.progress > player.progress && (computerLeading != player2.progress > player.progress)) {
                Console.WriteLine("[COMPUTER] Computer is now IN THE LEAD!");
            }

            Console.WriteLine("Turn over. Press enter to continue.");
            Console.Read();
            return;
        }

        public void playerPerformance() {
            int diceRolls = player.history.Count;
            int powerups = player.history.Count(historyItem => historyItem.powerup != null);
            int xpGained = player.NewXP() - player.exp;

            Console.WriteLine("Player Performance:");
            Console.WriteLine($"{player.emoji} {player.name}");
            Console.WriteLine($"- Dice Rolls: {diceRolls}");
            Console.WriteLine($"- Powerups landed on: {powerups}");
            Console.WriteLine($"- XP Gained: {xpGained}");
            Console.WriteLine($"- Total NEW XP: {player.NewXP()}");
            Console.WriteLine();
        }
    }
}