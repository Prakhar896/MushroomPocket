using Microsoft.EntityFrameworkCore;

namespace MushroomPocket {
    class Character {
        // public static int createdCharacters = 0;
        public string id { get; set; }
        public string name { get; set; }
        public int hp { get; set; }
        public int exp { get; set; }
        public string skill { get; set; }
        public string emoji { get; set; }

        public Character(string name, int hp, int exp, string skill, string emoji) {
            this.id = System.Guid.NewGuid().ToString();
            this.name = name;
            this.hp = hp;
            this.exp = exp;
            this.skill = skill;
            this.emoji = emoji;
        }

        public override string ToString() {
            return @$"-------------------------
Name: {name}
HP: {hp}
EXP: {exp}
Skill: {skill}
Emoji: {emoji}
-------------------------";
        }

        public virtual void transformTo(Character character) {
            this.name = character.name;
            this.hp = character.hp;
            this.exp = character.exp;
            this.skill = character.skill;
        }
    }

    // Waluigi, Daisy, Wario, Luigi, Peach, Mario
    class Waluigi: Character {
        public Waluigi(int hp, int exp): base("Waluigi", hp, exp, "Speed", "👨") {}
    }

    class Daisy: Character {
        public Daisy(int hp, int exp) : base("Daisy", hp, exp, "Leadership", "👩") {
        }
    }

    class Wario: Character {
        public Wario(int hp, int exp) : base("Wario", hp, exp, "Strength", "🥷") {
        }
    }

    class Luigi: Character {
        public Luigi(int hp, int exp) : base("Luigi", hp, exp, "Precision and Accuracy", "🤴") {
        }
    }

    class Peach: Character {
        public Peach(int hp, int exp) : base("Peach", hp, exp, "Magic Abilities", "👸") {
        }
    }

    class Mario: Character {
        public Mario(int hp, int exp) : base("Mario", hp, exp, "Combat Skills", "👲") {
        }
    }

    class DatabaseContext: DbContext {
        public DbSet<Character> Characters { get; set; }
        public DbSet<Waluigi> Waluigis { get; set; }
        public DbSet<Daisy> Daisys { get; set; }
        public DbSet<Wario> Warios { get; set; }
        public DbSet<Luigi> Luigis { get; set; }
        public DbSet<Peach> Peaches { get; set; }
        public DbSet<Mario> Marios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=database.db");
        }
    }
}