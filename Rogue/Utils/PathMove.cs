namespace Rogue.Utils;

public class PathMove(Direction direction, int distance)
{
    public Direction Direction { get; private set; } = direction;
    public int Distance { get; private set; } = distance;
}