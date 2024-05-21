using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace MushroomPocket {
    class Powerup {
        public string id;
        public string name;
        public int positionOnTrack;

        public Powerup(string id, string name, int positionOnTrack) {
            this.id = id;
            this.name = name;
            this.positionOnTrack = positionOnTrack;
        }

        public static List<Powerup> LoadFromFile(string path) {
            List<Powerup> powerups = new List<Powerup>();
            string fileData = FileOps.ReadFrom(path).text;
            powerups = JSON.Deserialize<List<Powerup>>(fileData);
            return powerups;
        }

        #nullable enable
        public static Powerup? FindByPosition(int position, List<Powerup> powerups) {
            return powerups.Find(powerup => powerup.positionOnTrack == position);
        }
        #nullable disable

        public override string ToString() {
            return $"ID: {id}, Name: {name}, Position On Track: {positionOnTrack}";
        }
    }
}