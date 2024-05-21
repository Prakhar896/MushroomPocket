using System;
using System.Collections.Generic;

namespace MushroomPocket {
    enum HistoryItemActor {
        Player,
        Computer
    }
    struct HistoryItem {
        public HistoryItemActor actor;
        public int roll;
        public string action;
        public int resultantProgress;        
        public Powerup? powerup;
        public DateTime timestamp;
    }

    class GameCharacter: Character {
        public int progress = 0;
        public List<HistoryItem> history = [];
        public int xpBonus = 0;
        public bool skipNextTurn = false;
        public bool doubleXPMultiplier = false;
        
        public GameCharacter(string name, int hp, int exp, string skill, string emoji): base(name, hp, exp, skill, emoji) {}

        public GameCharacter(Character character): base(character.name, character.hp, character.exp, character.skill, character.emoji) {}

        public void addHistoryItem(HistoryItemActor actor, int roll, string action, int resultantProgress, Powerup? powerup=null) {
            this.history.Add(new HistoryItem {
                actor = actor,
                roll = roll,
                action = action,
                resultantProgress = resultantProgress,
                powerup = powerup,
                timestamp = DateTime.Now
            });
        }

        public static GameCharacter GenerateComputer() {
            Random random = new Random();
            string[] names = ["Waluigi", "Daisy", "Wario", "Luigi", "Peach", "Mario"];
            string name = names[random.Next(0, names.Length)];
            int hp = random.Next(50, 100);
            int exp = random.Next(0, 100);
            string skill = "Skill";
            string[] computerEmojiOptions = [
                "👮‍♀️",
                "👻",
                "👽",
                "😈",
                "👼",
                "💁‍♂️",
                "🤵",
                "🥷"
            ];
            string emoji = computerEmojiOptions[random.Next(0, computerEmojiOptions.Length)];
            return new GameCharacter(name, hp, exp, skill, emoji);
        }

        public int NewXP() {
            int newXP = this.exp + this.xpBonus;
            foreach(HistoryItem historyItem in this.history) {
                if (historyItem.actor == HistoryItemActor.Player && historyItem.powerup != null) {
                    newXP += 20;
                }
            }
            
            if (this.doubleXPMultiplier) {
                newXP *= 2;
            }
            return newXP;
        }
    }
}