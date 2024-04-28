namespace Rogue.Objects;

public class Tile(char ch, ConsoleColor col = ConsoleColor.White)
{
    public char Char { get; private set; } = ch;
    public ConsoleColor Colour { get; private set; } = col;
}