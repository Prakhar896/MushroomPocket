using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MushroomPocket
{
    class Program
    {
        static List<Character> pocket = [];
        static bool debugMode = false;

        static List<MushroomMaster> mushroomMasters = [
            new MushroomMaster("Daisy", 2, "Peach"),
            new MushroomMaster("Wario", 3, "Mario"),
            new MushroomMaster("Waluigi", 1, "Luigi")
        ];

        static void SavePocket() {
            using (var db = new DatabaseContext()) {
                db.Database.EnsureCreated();
                foreach (var character in db.Characters) {
                    db.Characters.Remove(character);
                }
                db.Characters.AddRange(pocket);
                db.SaveChanges();
                db.Database.ExecuteSqlInterpolated($"PRAGMA wal_checkpoint(FULL)");
                db.Dispose();
                FileOps.DeleteTempDBFiles();
            }
        }

        static void LoadPocket() {
            using (var db = new DatabaseContext()) {
                db.Database.EnsureCreated();
                pocket = db.Characters.ToList();
                db.Dispose();
                FileOps.DeleteTempDBFiles();
            }
        }

        static void Main(string[] args)
        {
            if (args.Contains("debug") || args.Contains("d")) {
                pocket.Add(new Daisy(99, 23));
                pocket.Add(new Daisy(99, 23));
                pocket.Add(new Wario(87, 34));
                pocket.Add(new Wario(87, 34));
                pocket.Add(new Wario(87, 34));
                pocket.Add(new Waluigi(23, 11));

                if (!args.Contains("anim")) {
                    debugMode = true;
                }
            } else {
                LoadPocket();
            }
            //Use "Environment.Exit(0);" if you want to implement an exit of the console program
            //Start your assignment 1 requirements below.
            mainLoop();
        }

        static void addMushroomCharacter() {
            string characterName = Misc.SafeInputWithPredicate(
                message: "Enter Character's Name: ",
                predicate: x => new string[] {"daisy", "wario", "waluigi"}.Contains(x.ToLower()),
                errMessage: "Invalid character name. Please enter either Daisy, Wario, or Waluigi."
            );
            int hp = Misc.SafeInputAndParse<int>("Enter Character's HP: ");
            int exp = Misc.SafeInputAndParse<int>("Enter Character's EXP: ");

            switch (characterName.ToLower()) {
                case "daisy":
                    pocket.Add(new Daisy(hp, exp));
                    break;
                case "wario":
                    pocket.Add(new Wario(hp, exp));
                    break;
                case "waluigi":
                    pocket.Add(new Waluigi(hp, exp));
                    break;
            }

            characterName = characterName.First().ToString().ToUpper() + characterName.Substring(1);
            SavePocket();
            Console.WriteLine($"{characterName} has been added.");
            return;
        }

        static void listCharacters(bool showCharacterNumber=false) {
            if (pocket.Count == 0) {
                Console.WriteLine("Pocket is empty.");
                return;
            }

            int characterNumber = 1;
            foreach (Character character in pocket) {
                if (showCharacterNumber) {
                    Console.WriteLine($"Character: {characterNumber}");
                    characterNumber++;
                }
                Console.WriteLine(character);
            }
        }

        static void checkCharactersForTransformation() {
            if (pocket.Count == 0) {
                Console.WriteLine("Pocket is empty. Please add more characters before checking for transformation.");
                return;
            }

            foreach(MushroomMaster mushroomMaster in mushroomMasters) {
                int count = pocket.Count(x => x.name.ToLower() == mushroomMaster.Name.ToLower());
                if (count >= mushroomMaster.NoToTransform) {
                    Console.WriteLine($"{mushroomMaster.Name} --> {mushroomMaster.TransformTo}");
                }
            }
        }
        
        static void transformCharacters() {
            if (pocket.Count == 0) {
                Console.WriteLine("Pocket is empty. Please add more characters before transforming.");
                return;
            }

            foreach (MushroomMaster mushroomMaster in mushroomMasters) {
                int count = pocket.Count(x => x.name.ToLower() == mushroomMaster.Name.ToLower());
                int toTransform = count / mushroomMaster.NoToTransform;
                int toKeep = count - (mushroomMaster.NoToTransform * toTransform);

                if (count >= mushroomMaster.NoToTransform) {
                    List<int> indexesToRemove = [];
                    for (int i=0; i < pocket.Count; i++) {
                        if (pocket[i].name.ToLower() == mushroomMaster.Name.ToLower()) {
                            if (toTransform > 0) {
                                switch (pocket[i].name.ToLower()) {
                                    case "daisy":
                                        pocket[i].transformTo(new Peach(100, 0));
                                        break;
                                    case "wario":
                                        pocket[i].transformTo(new Mario(100, 0));
                                        break;
                                    case "waluigi":
                                        pocket[i].transformTo(new Luigi(100, 0));
                                        break;
                                }
                                toTransform -= 1;
                                Console.WriteLine($"{mushroomMaster.Name} has been transformed to {mushroomMaster.TransformTo}");
                            } else if (toKeep > 0) {
                                toKeep -= 1;
                            } else {
                                pocket.RemoveAt(i);
                            }
                        }
                    }
                }
            }

            SavePocket();
        }

        static void startGame() {
            if (pocket.Count == 0) {
                Console.WriteLine("Pocket is empty. Please add at least 1 character before playing MushroomKart.");
                return;
            }
            // Game mode
            Console.WriteLine(@"Game Mode:
(1). Play against computer
(2). Play against another player (PVP)
(3). Join a PVP game with a code
(4). Back to main menu
            ");
            int gameMode = Misc.SafeInputAndParse<int>("Enter game mode: ", errMessage: "Invalid game mode. Please enter a valid game mode.");
            while (!new int[] {1, 2, 3, 4}.Contains(gameMode)) {
                gameMode = Misc.SafeInputAndParse<int>("Invalid game mode. Please enter a valid game mode: ");
            }

            if (gameMode == 4) {
                return;
            }

            Console.WriteLine();
            // Get player to choose their character
            listCharacters(showCharacterNumber: true);
            Console.WriteLine();
            int playerNumber = int.Parse(Misc.SafeInputWithPredicate(
                message: "Enter the number of your chosen character: ",
                predicate: x => Misc.TryParse<int>(x) != null && Misc.TryParse<int>(x) <= pocket.Count && Misc.TryParse<int>(x) > 0,
                errMessage: "Invalid character number. Please enter a valid character number."
            )) - 1;

            // Check console window
            if (Console.WindowWidth < 40) {
                Console.WriteLine("Please resize your console window to at least 40 characters wide for the game to work properly.");
                return;
            }

            // Start game based on game mode
            if (gameMode == 1) {
                GameManager gameManager = new GameManager(pocket[playerNumber], debugMode: debugMode);
                gameManager.mainLoop();
                gameManager.playerPerformance();

                pocket[playerNumber].exp = gameManager.player.NewXP();

                SavePocket();
                return;
            } else if (gameMode == 2 || gameMode == 3) {
                PVPManager pvpManager = new PVPManager(pocket[playerNumber], playerType: gameMode == 2 ? PVPPlayerType.Player1: PVPPlayerType.Player2, debugMode: debugMode);
                pvpManager.mainLoop();
                Console.WriteLine("------");
                pvpManager.playerPerformance();

                pocket[playerNumber].exp = pvpManager.player.NewXP();

                if (pvpManager.winner == GameManager.GameCharacterType.Player) {
                    switch (pocket[playerNumber].name.ToLower()) {
                        case "daisy":
                            pocket.Add(new Daisy(100, 50));
                            break;
                        case "wario":
                            pocket.Add(new Wario(100, 50));
                            break;
                        case "waluigi":
                            pocket.Add(new Waluigi(100, 50));
                            break;
                        case "peach":
                            pocket.Add(new Peach(100, 50));
                            break;
                        case "mario":
                            pocket.Add(new Mario(100, 50));
                            break;
                        case "luigi":
                            pocket.Add(new Luigi(100, 50));
                            break;
                    }
                    Console.WriteLine($"As a reward for winning the PVP game, a new {pocket[playerNumber].name} has been added to your pocket!");
                }

                SavePocket();
                return;
            }
        }

        static void RemoveCharacter() {
            if (pocket.Count == 0) {
                Console.WriteLine("Pocket is empty. Please add more characters before removing.");
                return;
            }

            listCharacters(showCharacterNumber: true);
            int characterNumber = Misc.SafeInputAndParse<int>("Enter the number of the character you want to remove (0 for all): ");
            while (characterNumber > pocket.Count || characterNumber < 0) {
                characterNumber = Misc.SafeInputAndParse<int>("Invalid character number. Please enter a valid character number: ");
            }

            if (characterNumber == 0) {
                pocket.Clear();
                SavePocket();
                Console.WriteLine("All characters have been removed.");
                return;
            }

            pocket.RemoveAt(characterNumber - 1);
            SavePocket();
            Console.WriteLine("Character has been removed.");
        }

        static void mainLoop() {
            while (true) {
                Console.WriteLine(@"******************************
Welcome to Mushroom Pocket App
******************************
(1). Add Mushroom's character to my pocket
(2). List character(s) in my Pocket
(3). Check if I can transform my characters
(4). Transform character(s)
(5). Remove character(s)
(6). Play MushroomKart
                ");
                Console.Write("Please only enter [1,2,3,4,5,6] or Q to exit: ");
                string input = Console.ReadLine().ToLower();
                while (!new string[] { "1", "2", "3", "4", "5", "6", "q" }.Contains(input)) {
                    Console.Write("Please only enter [1,2,3,4,5] or Q to exit: ");
                    input = Console.ReadLine().ToLower();
                }

                if (input == "q") {
                    break;
                }
                int choice = int.Parse(input);

                switch (choice) {
                    case 1:
                        addMushroomCharacter();
                        break;
                    case 2:
                        listCharacters();
                        break;
                    case 3:
                        checkCharactersForTransformation();
                        break;
                    case 4:
                        transformCharacters();
                        break;
                    case 5:
                        RemoveCharacter();
                        break;
                    case 6:
                        startGame();
                        break;
                }
                
                Console.WriteLine();
            }
        }
    }
}
