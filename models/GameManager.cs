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
            Computer
        }

        public GameCharacter player;
        public GameCharacter computer;
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

        public GameManager(Character player, bool debugMode = false) {
            this.player = new GameCharacter(player);
            this.computer = GameCharacter.GenerateComputer();

            // Scale powerup positions in proportional ratio to progress goal
            this.powerups = Powerup.LoadFromFile("data/powerups.json");
            foreach (Powerup powerup in powerups) {
                powerup.positionOnTrack = (int) Math.Floor((double) progressGoal * powerup.positionOnTrack / 100);
            }
            this.debugMode = debugMode;
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
                Console.WriteLine($"{(type == GameCharacterType.Player ? "[PLAYER]" : "[COMPUTER]")}");
                Console.WriteLine(GameManager.Dashes(progressGoal));
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
                Console.WriteLine(GameManager.Dashes(progressGoal));
            }
            

            PrintTrackForCharacter(player, GameCharacterType.Player);
            PrintTrackForCharacter(computer, GameCharacterType.Computer);

            Console.WriteLine("💪 Powerups:");
            foreach (Powerup powerup in powerups) {
                Console.WriteLine($"{powerup.id}: {powerup.name}");
            }
            Console.WriteLine();
        }

        public int rollDice() {
            Random random = new Random();
            return random.Next(1, 7);
        }

        public void landedOnPowerup(GameCharacterType actor, Powerup powerup) {
            switch (powerup.id) {
                case "A":
                    if (actor == GameCharacterType.Player) {
                        player.xpBonus += 30;
                    } else {
                        computer.xpBonus += 30;
                    }
                    Console.WriteLine("Received an additional 30 XP!");
                    break;
                case "B":
                    if (actor == GameCharacterType.Player) {
                        player.progress -= 5;
                    } else {
                        computer.progress -= 5;
                    }
                    Console.WriteLine("Moved back 5 steps on the track!");
                    break;
                case "C":
                    if (actor == GameCharacterType.Player) {
                        player.skipNextTurn = true;
                    } else {
                        computer.skipNextTurn = true;
                    }
                    Console.WriteLine("Next turn will be skipped!");
                    break;
                case "D":
                    int playerProgress = player.progress;
                    player.progress = computer.progress;
                    computer.progress = playerProgress;
                    Console.WriteLine("Swapped progress with the opponent!");
                    break;
                case "E":
                    if (actor == GameCharacterType.Player) {
                        player.progress += 3;
                    } else {
                        computer.progress += 3;
                    }
                    Console.WriteLine("Advanced 3 steps!");
                    break;
                case "F":
                    if (actor == GameCharacterType.Player) {
                        player.doubleXPMultiplier = true;
                    } else {
                        computer.doubleXPMultiplier = true;
                    }
                    Console.WriteLine("XP multiplier doubled!");
                    break;
                case "G":
                    if (actor == GameCharacterType.Player) {
                        computer.hp = 0;
                    } else {
                        player.hp = 0;
                    }
                    Console.WriteLine("Killed opponent! Lucky you!!! 😈");
                    break;
            }
            Console.Read();
        }

        public void mainLoop() {
            if (!debugMode) {
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
                while (toWriteIndex < gameTitle.Length) {
                    Console.Write(gameTitle[toWriteIndex]);
                    Thread.Sleep(100);
                    toWriteIndex++;
                }
                Console.WriteLine();
                Console.WriteLine("Race against a computer opponent to reach the finish line first!");
                Console.WriteLine("You can land on powerups that can help or hinder your progress.");
                Console.WriteLine("The first to reach the finish line or the last one standing wins!");
                Console.WriteLine();

                Console.WriteLine("Introducing your opponent:");
                Console.WriteLine($"Name: {computer.name}");
                Console.WriteLine($"HP: {computer.hp}");
                Console.WriteLine($"XP: {computer.exp}");
                Console.WriteLine($"Skill: {computer.skill}");
                Console.WriteLine($"Emoji: {computer.emoji}");
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


            // Start main flow
            GameCharacterType whoseTurn = GameCharacterType.Player;
            while (player.progress < progressGoal && computer.progress < progressGoal && player.hp > 0 && computer.hp > 0) {
                ProduceVisuals();
                if (whoseTurn == GameCharacterType.Player) {
                    if (!player.skipNextTurn) {
                        playerTurn();
                    } else {
                        Console.WriteLine("[PLAYER] Skipping turn!");
                        player.skipNextTurn = false;
                    }

                    whoseTurn = GameCharacterType.Computer;
                } else {
                    if (!computer.skipNextTurn) {
                        computerTurn();
                    } else {
                        Console.WriteLine("[COMPUTER] Skipping turn!");
                        computer.skipNextTurn = false;
                    }

                    whoseTurn = GameCharacterType.Player;
                }
            }

            if (player.progress >= progressGoal || computer.hp <= 0) {
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
            } else if (computer.progress >= progressGoal || player.hp <= 0) {
                if (computer.progress >= progressGoal) {
                    Console.WriteLine("COMPUTER CROSSES THE CHECKERED FLAG AND WINS!");
                } else {
                    Console.WriteLine("Player lost its HP - COMPUTER WINS!");
                }
                computer.xpBonus += 200;
                winner = GameCharacterType.Computer;
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

        public void playerTurn() {
            bool playerLeading = player.progress > computer.progress;
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

            if (player.progress > computer.progress && (playerLeading != player.progress > computer.progress)) {
                Console.WriteLine("[PLAYER] You are now IN THE LEAD!");
            }

            Console.WriteLine("Turn over. Press enter to continue.");
            Console.Read();
            return;
        }

        public void computerTurn() {
            bool computerLeading = computer.progress > player.progress;
            Console.WriteLine("[COMPUTER] Rolling dice...");
            Thread.Sleep(1000);

            int diceRoll = rollDice();
            computer.progress += diceRoll;
            Powerup? powerup = Powerup.FindByPosition(computer.progress, powerups);
            computer.addHistoryItem(
                actor: HistoryItemActor.Computer,
                roll: diceRoll,
                action: "Rolled dice",
                resultantProgress: computer.progress,
                powerup: powerup
            );

            UpdateVisualsWithStatement($"[COMPUTER] Computer rolled a {diceRoll}!");

            if (powerup != null) {
                Console.WriteLine($"[COMPUTER] Landed on a powerup: {powerup.name}!");
                landedOnPowerup(GameCharacterType.Computer, powerup);
            }

            if (computer.progress > player.progress && (computerLeading != computer.progress > player.progress)) {
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