using Rogue;

var dungeonGenerator = new DungeonGenerator(80, 25, 3, 3);

Console.Clear();
Console.WriteLine("""

                  ┌────────────────── ROGUE DUNGEON GENERATOR ──────────────────┐
                  │                                                             │
                  │           Welcome to the Rogue Dungeon Generator!           │
                  │                                                             │
                  │                                                             │
                  │    This demo showcases how the dungeon generation in the    │
                  │                 original 1980 Rogue worked.                 │
                  │                                                             │
                  │             Press [G] to generate new dungeons.             │
                  │      Press [S] to generate a new dungeon step-by-step.      │
                  │               Press [Esc] to quit the program.              │
                  │                                                             │
                  │                                                             │
                  │    Created for the ProcGen class of BA6 in Digital Games    │
                  │                                                             │
                  └─────────────────────────────────────────────────────────────┘
                  """);


ConsoleKey pressedKey;
do
{
    pressedKey = Console.ReadKey(true).Key;
    if (pressedKey != ConsoleKey.G && pressedKey != ConsoleKey.S) continue;
    
    if (pressedKey == ConsoleKey.G) dungeonGenerator.Generate();
    else dungeonGenerator.Generate(true);
    
    dungeonGenerator.PrintDungeon();
    Console.WriteLine("Press [G] to generate a new dungeon. Press [S] to generate a new dungeon step-by-step.\nPress [Esc] to quit the program.");
}
while (pressedKey != ConsoleKey.Escape);