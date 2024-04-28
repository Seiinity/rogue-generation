using Rogue.Utils;

namespace Rogue.Objects;

public class Room(int x, int y, int width, int height, int ix, int iy)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;

    public int Width { get; set; } = width;
    public int Height { get; set; } = height;

    public List<Room> Connections { get; private set; } = new();
    public List<Direction> ConnectionDirections { get; private set; } = new();
    
    public bool IsGone { get; set; }

    public int IndexX { get; private set; } = ix;
    public int IndexY { get; private set; } = iy;
}