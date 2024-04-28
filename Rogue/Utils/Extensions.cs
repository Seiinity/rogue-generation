namespace Rogue.Utils;

public static class Extensions
{
    public static void Shuffle<T>(this Random rng, IList<T> list)
    {
        var n = list.Count;
        
        while (n > 1) 
        {
            var k = rng.Next(n--);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
    
    public static void Shuffle<T>(this Random rng, T[] array)
    {
        var n = array.Length;
        
        while (n > 1) 
        {
            var k = rng.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }

    public static int GetDeltaX(this Direction dir)
    {
        switch (dir)
        {
            default:
            case Direction.Down or Direction.Up:
                return 0;
            case Direction.Left:
                return -1;
            case Direction.Right:
                return 1;
        }
    }
    
    public static int GetDeltaY(this Direction dir)
    {
        switch (dir)
        {
            default:
            case Direction.Left or Direction.Right:
                return 0;
            case Direction.Down:
                return 1;
            case Direction.Up:
                return -1;
        }
    }

    public static Direction Opposite(this Direction dir)
    {
        switch (dir)
        {
            default:
            case Direction.Right:
                return Direction.Left;
            case Direction.Left: 
                return Direction.Right;
            case Direction.Down:
                return Direction.Up;
            case Direction.Up:
                return Direction.Down;
        }
    }
}