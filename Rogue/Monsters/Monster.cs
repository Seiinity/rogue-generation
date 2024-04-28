namespace Rogue.Monsters;

public abstract class Monster(int x, int y)
{
    public char Char { get; protected init; }
    
    public int X { get; } = x;
    public int Y { get; } = y;
}