using System.Numerics;
using System.Text;
using Rogue.Monsters;
using Rogue.Objects;
using Rogue.Utils;

namespace Rogue;

public class DungeonGenerator
{
    private readonly int _dungeonWidth;
    private readonly int _dungeonHeight; 
    
    private readonly int _horizontalRooms;
    private readonly int _verticalRooms;
    
    private readonly int _cellWidth;
    private readonly int _cellHeight;

    private const int MinRoomWidth = 5;
    private const int MinRoomHeight = 2;
    private readonly int _maxRoomWidth;
    private readonly int _maxRoomHeight;

    private Room _firstRoom;
    private Room _finalRoom;

    private readonly Random _rng = new();

    private readonly Tile[,] _dungeon;
    private readonly Room[,] _rooms;

    private bool _isStepByStep;

    private readonly Direction[] _dirToCheck =
    [
        Direction.Up,
        Direction.Down,
        Direction.Left,
        Direction.Right
    ];
    
    private readonly Func<int, int, Monster>[] _possibleMonsters =
    [
        (x, y) => new Medusa(x, y),
        (x, y) => new Phantom(x, y),
        (x, y) => new Zombie(x, y)
    ];

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="width">The width, in characters, of the dungeon.</param>
    /// <param name="height">The height, in characters, of the dungeon.</param>
    /// <param name="hRooms">The number of rooms to place horizontally.</param>
    /// <param name="vRooms">The number of rooms to place vertically.</param>
    public DungeonGenerator(int width, int height, int hRooms, int vRooms)
    {
        _dungeonWidth = width;
        _dungeonHeight = height;
        
        _horizontalRooms = hRooms;
        _verticalRooms = vRooms;

        _cellWidth = width / hRooms;
        _cellHeight = height / vRooms;

        _maxRoomWidth = _cellWidth - 4;
        _maxRoomHeight = _cellHeight - 2;
        
        _dungeon = new Tile[_dungeonWidth, _dungeonHeight];
        _rooms = new Room[_horizontalRooms, _verticalRooms];

        _firstRoom = _rooms[0, 0];
        _finalRoom = _rooms[0, 0];
    }

    /// <summary>
    /// Generates a dungeon in multiple steps.
    /// </summary>
    public void Generate(bool isStepByStep = false)
    {
        _isStepByStep = isStepByStep;
        
        InitRooms(); // Steps 1 to 3.
        ConnectNeighbouringRooms(); // Steps 4 and 5.
        ConnectUnconnectedRooms(); // Steps 6 and 7.
        CreateRooms(); // Step 8.
        CreateCorridors(); // Step 9.
        PlaceMonsters(); // Step 10.
        PlaceStairs(); // Step 11.
    }

    /// <summary>
    /// Initialises all the rooms of the dungeon, of which at most 3 are gone-rooms.
    /// </summary>
    private void InitRooms()
    {
        for (var x = 0; x < _horizontalRooms; x++) // Creates the room grid.
        {
            for (var y = 0; y < _verticalRooms; y++) _rooms[x, y] = new Room(0, 0, 0, 0, x, y);
        }
        
        for (var x = 0; x < _dungeonWidth; x++) // Creates the tile grid.
        {
            for (var y = 0; y < _dungeonHeight; y++) _dungeon[x, y] = new Tile(Chars.EmptySpace);
        }
        
        var numGoneRooms = _rng.Next(4); // Max of 3 gone-rooms.
        
        for (var i = 0; i < numGoneRooms ; i++) // Decides which rooms are gone-rooms.
        {
            Room room;
            
            do room = _rooms[_rng.Next(_rooms.GetLength(0)), _rng.Next(_rooms.GetLength(1))];
            while (room.IsGone);

            room.IsGone = true; // Marks room as a gone-room.
        }
    }

    /// <summary>
    /// Selects a random starting room and sequentially connects neighbouring rooms.
    /// </summary>
    private void ConnectNeighbouringRooms()
    {
        var unconnected = _rooms.Cast<Room>().ToList(); // Adds all rooms to the unconnected list.
        _rng.Shuffle(unconnected);
        
        _firstRoom = unconnected.FirstOrDefault(r => !r.IsGone) ?? _rooms[0, 0]; // First non-gone room.
        _finalRoom = unconnected.LastOrDefault(r => !r.IsGone) ?? _rooms[0, 0]; // Final non-gone room.

        foreach (var room in unconnected)
        {
            Extensions.Shuffle(_rng, _dirToCheck);

            foreach (var dir in _dirToCheck)
            {
                var neighbour = GetNeighbour(room, dir);
                
                if (neighbour == null || neighbour.Connections.Count > 0) continue; // Avoids connecting to invalid or non-isolated rooms.
                if (room.Connections.Contains(neighbour)) break; // Already connected to this neighbour.
                
                room.Connections.Add(neighbour);
                room.ConnectionDirections.Add(dir);
                break;
            }
        }
    }

    /// <summary>
    /// Connects any rooms that remained unconnected during the neighbour-based connecting.
    /// </summary>
    private void ConnectUnconnectedRooms()
    {
        foreach (var room in _rooms)
        {
            switch (room)
            {
                case { IsGone: false, Connections.Count: > 0 }: // Skips gone rooms with two or more connections.
                case { IsGone: true, Connections.Count: > 1 }: continue; // Skips normal rooms with connections.
            }

            Extensions.Shuffle(_rng, _dirToCheck);

            foreach (var dir in _dirToCheck)
            {
                var neighbour = GetNeighbour(room, dir);
                
                if (neighbour == null || neighbour.Connections.Contains(room)) continue; // Avoids bi-directional connections.
                if (room.IsGone && room.Connections.Contains(neighbour)) continue; // Avoids connecting to an already connected neighbour (for gone rooms).
                
                room.Connections.Add(neighbour);
                room.ConnectionDirections.Add(dir);
                break;
            }
        }
    }

    /// <summary>
    /// Gets a room's neighbour in the specified direction.
    /// </summary>
    /// <param name="room">The room to get a neighbour of.</param>
    /// <param name="dir">The direction to check for a neighbour.</param>
    /// <returns>The room's neighbour in the specified direction, or null if no neighbour was found.</returns>
    private Room? GetNeighbour(Room room, Direction dir)
    {
        var nextX = room.IndexX + dir.GetDeltaX();
        var nextY = room.IndexY + dir.GetDeltaY();

        // Checks if outside cell bounds.
        if (nextX < 0 || nextX >= _horizontalRooms || nextY < 0 || nextY >= _verticalRooms) return null;

        return _rooms[nextX, nextY];
    }

    /// <summary>
    /// Creates every room. This includes only its floor and walls, no doors or contents.
    /// </summary>
    private void CreateRooms()
    {
        for (var x = 0; x < _horizontalRooms; x++)
        {
            for (var y = 0; y < _verticalRooms; y++)
            {
                var startX = Math.Max(_cellWidth * x, 2); 
                var startY = Math.Max(_cellHeight * y, 2); // Makes sure no room is placed at the edge.

                var roomWidth = _rng.Next(MinRoomWidth, _maxRoomWidth);
                var roomHeight = _rng.Next(MinRoomHeight, _maxRoomHeight);

                // Creates a vertical and horizontal gap of 3 chars between rooms.
                if (y > 0) startY = Math.Max(startY, _rooms[x, y - 1].Y + _rooms[x, y - 1].Height + 3); 
                if (x > 0) startX = Math.Max(startX, _rooms[x - 1, y].X + _rooms[x - 1, y].Width + 3);
                
                // Creates a random X and Y offset for the room's position.
                var sxOffset = (int) Math.Round(_rng.Next(_cellWidth - roomWidth) * 0.5f);
                var syOffset = (int) Math.Round(_rng.Next(_cellHeight - roomHeight) * 0.5f);
                
                while (startX + sxOffset + roomWidth >= _dungeonWidth - 1) // Makes sure the rooms fit horizontally.
                {
                    if (sxOffset > 0) sxOffset--;
                    else if (roomWidth > MinRoomWidth) roomWidth--;
                    else break; // Prevents endless loop if minimum room width is still too large.
                }
                
                while (startY + syOffset + roomHeight >= _dungeonHeight - 1) // Makes sure the rooms fit vertically.
                {
                    if (syOffset > 0) syOffset--;
                    else if (roomHeight > MinRoomHeight) roomHeight--;
                    else break; // Prevents endless loop if minimum room height is still too large.
                }

                startX += sxOffset;
                startY += syOffset;

                // Applies position & dimensions to the room.
                _rooms[x, y].X = startX;
                _rooms[x, y].Y = startY;
                _rooms[x, y].Width = roomWidth;
                _rooms[x, y].Height = roomHeight;

                if (!_rooms[x, y].IsGone)
                {
                    for (var xx = startX; xx < startX + roomWidth; xx++)
                        for (var yy = startY; yy < startY + roomHeight; yy++)
                            _dungeon[xx, yy] = new Tile(Chars.Ground, Colours.Ground);
                }

                if (_isStepByStep) PrintStepByStepMessage();
            }
        }

        CreateRoomWalls();
    }

    /// <summary>
    /// Creates the horizontal, vertical, and corner wall tiles for each non-gone room.
    /// </summary>
    private void CreateRoomWalls()
    {
        foreach (var room in _rooms.Cast<Room>().Where(r => !r.IsGone)) // Avoids gone-rooms.
        {
            for (var x = room.X - 1; x <= room.X + room.Width; x++) // Horizontal walls.
            {
                _dungeon[x, room.Y - 1] = new Tile(Chars.WallHorizontal, Colours.Wall);
                _dungeon[x, room.Y + room.Height] = new Tile(Chars.WallHorizontal, Colours.Wall);
            }
            
            for (var y = room.Y; y < room.Y + room.Height; y++) // Vertical walls.
            {
                _dungeon[room.X - 1, y] = new Tile(Chars.WallVertical, Colours.Wall);
                _dungeon[room.X + room.Width, y] = new Tile(Chars.WallVertical, Colours.Wall);
            }

            // Wall corners.
            _dungeon[room.X - 1, room.Y - 1] = new Tile(Chars.WallTopLeft, Colours.Wall);
            _dungeon[room.X - 1, room.Y + room.Height] = new Tile(Chars.WallBottomLeft, Colours.Wall);
            _dungeon[room.X + room.Width, room.Y - 1] = new Tile(Chars.WallTopRight, Colours.Wall);
            _dungeon[room.X + room.Width, room.Y + room.Height] = new Tile(Chars.WallBottomRight, Colours.Wall);
            
            if (_isStepByStep) PrintStepByStepMessage();
        }
    }

    /// <summary>
    /// Creates corridors to link every connection of every room.
    /// </summary>
    private void CreateCorridors()
    {
        foreach (var room in _rooms)
        {
            foreach (var index in Enumerable.Range(0, room.Connections.Count))
            {
                var connection = room.Connections[index];
                var direction = room.ConnectionDirections[index];
                
                var  startPoint = !room.IsGone // Centre of the room, if gone, door otherwise.
                    ? CreateDoorInWall(room, direction)
                    : new Vector2(room.X + (int)Math.Round(room.Width / 2.0), room.Y + (int)Math.Round(room.Height / 2.0));

                var endPoint = !connection.IsGone // Centre of the destination room, if gone, door otherwise.
                    ? CreateDoorInWall(connection, direction.Opposite())
                    : new Vector2(connection.X + (int)Math.Round(connection.Width / 2.0), connection.Y + (int)Math.Round(connection.Height / 2.0));
                
                DigPath(startPoint, endPoint);
                if (_isStepByStep) PrintStepByStepMessage();
            }
        }
    }
    
    /// <summary>
    /// Creates a door tile for a room in a specified direction. Used to connect rooms to corridors.
    /// </summary>
    /// <param name="room">The room to create a door for.</param>
    /// <param name="dir">The direction in which to create a door.</param>
    /// <returns>The position of the first corridor tile after the new door.</returns>
    private Vector2 CreateDoorInWall(Room room, Direction dir)
    {
        int x, y;
        Vector2 positionAfterDoor;

        switch (dir)
        {
            default:
            case Direction.Left: // Door on the left wall.
                
                y = _rng.Next(room.Y + 1, room.Y + room.Height);
                x = room.X - 1;
                positionAfterDoor = new Vector2(x - 1, y);
                break;
            
            case Direction.Right: // Door on the right wall.
                
                y = _rng.Next(room.Y + 1, room.Y + room.Height);
                x = room.X + room.Width;
                positionAfterDoor = new Vector2(x + 1, y);
                break;
            
            case Direction.Down: // Door on the bottom wall.
                
                x = _rng.Next(room.X + 1, room.X + room.Width);
                y = room.Y + room.Height;
                positionAfterDoor = new Vector2(x, y + 1);
                break;
            
            case Direction.Up: // Door on the top wall.
                
                x = _rng.Next(room.X + 1, room.X + room.Width);
                y = room.Y - 1;
                positionAfterDoor = new Vector2(x, y - 1);
                break;
        }

        _dungeon[x, y] = new Tile(Chars.Door, Colours.Door); // Places the door tile.

        if (_isStepByStep) PrintStepByStepMessage();
        return positionAfterDoor;
    }
    
    /// <summary>
    /// Digs a corridor path between two points.
    /// </summary>
    /// <param name="start">The point to start the corridor at.</param>
    /// <param name="end">The point to end the corridor at.</param>
    private void DigPath(Vector2 start, Vector2 end)
    {
        var xOffset = (int) end.X - (int) start.X;
        var yOffset = (int) end.Y - (int) start.Y;
        var xAbs = Math.Abs(xOffset);
        var yAbs = Math.Abs(yOffset);
        
        var xPos = (int) start.X;
        var yPos = (int) start.Y;

        var firstHalf = _rng.NextDouble(); // Halves used for zig-zag in path.
        var secondHalf = 1 - firstHalf;

        var xDir = xOffset < 0 ? Direction.Left : Direction.Right;
        var yDir = yOffset > 0 ? Direction.Down : Direction.Up;

        var moves = new Queue<PathMove>();

        if (xAbs < yAbs) // Digs a mostly vertical path.
        {
            moves.Enqueue(new PathMove(yDir, (int)Math.Ceiling(yAbs * firstHalf)));
            moves.Enqueue(new PathMove(xDir, xAbs));
            moves.Enqueue(new PathMove(yDir, (int)Math.Floor(yAbs * secondHalf)));
        }
        else // Digs a mostly horizontal path.
        {
            moves.Enqueue(new PathMove(xDir, (int)Math.Ceiling(xAbs * firstHalf)));
            moves.Enqueue(new PathMove(yDir, yAbs));
            moves.Enqueue(new PathMove(xDir, (int)Math.Floor(xAbs * secondHalf)));
        }
        
        _dungeon[xPos, yPos] = new Tile(Chars.Corridor, Colours.Corridor);

        while (moves.Count > 0) // Draws all the corridor tiles.
        {
            var move = moves.Dequeue();
            
            for (var dist = move.Distance; dist > 0; dist--)
            {
                xPos += move.Direction.GetDeltaX();
                yPos += move.Direction.GetDeltaY();
                _dungeon[xPos, yPos] = new Tile(Chars.Corridor, Colours.Corridor);
            }
        }
    }
    
    /// <summary>
    /// Places random monsters. Each room has a 20% chance to contain a monster.
    /// </summary>
    private void PlaceMonsters()
    {
        foreach (var room in _rooms.Cast<Room>().Where(r => !r.IsGone)) // Avoids gone-rooms.
        {
            if (_rng.Next(100) >= 20) continue;
            
            var monsterX = _rng.Next(room.X, room.X + room.Width);
            var monsterY = _rng.Next(room.Y, room.Y + room.Height);

            // Chooses a random type of monster to place.
            var monster = _possibleMonsters[_rng.Next(_possibleMonsters.Length)](monsterX, monsterY);
            _dungeon[monster.X, monster.Y] = new Tile(monster.Char, Colours.Monster);
            
            if (!_isStepByStep) continue;
        
            PrintStepByStepMessage();
        }
    }

    /// <summary>
    /// Places staircases going up (initial room) and down (final room).
    /// </summary>
    private void PlaceStairs()
    {
        var upStairsX = _rng.Next(_firstRoom.X, _firstRoom.X + _firstRoom.Width);
        var upStairsY = _rng.Next(_firstRoom.Y, _firstRoom.Y + _firstRoom.Height);
        
        var downStairsX = _rng.Next(_finalRoom.X, _finalRoom.X + _finalRoom.Width);
        var downStairsY = _rng.Next(_finalRoom.Y, _finalRoom.Y + _finalRoom.Height);

        _dungeon[upStairsX, upStairsY] = new Tile(Chars.StaircaseUp, Colours.StaircaseUp);
        _dungeon[downStairsX, downStairsY] = new Tile(Chars.StaircaseDown, Colours.StaircaseDown);
    }

    /// <summary>
    /// Prints the dungeon.
    /// </summary>
    public void PrintDungeon()
    {
        Console.Clear();
        var sb = new StringBuilder();

        for (var y = 0; y < _dungeonHeight; y++)
        {
            var lastColor = Colours.Default;
            
            for (var x = 0; x < _dungeonWidth; x++)
            {
                if (_dungeon[x, y].Colour != lastColor)
                {
                    if (sb.Length > 0)
                    {
                        Console.ForegroundColor = lastColor;
                        Console.Write(sb.ToString());
                        sb.Clear();
                    }
                    lastColor = _dungeon[x, y].Colour;
                }
                sb.Append(_dungeon[x, y].Char);
            }

            if (sb.Length > 0)
            {
                Console.ForegroundColor = lastColor;
                Console.Write(sb.ToString());
                sb.Clear();
            }

            Console.Write("\n");
        }

        Console.ForegroundColor = Colours.Default;
    }

    /// <summary>
    /// Prints the step-by-step "press any key" message, as well as the dungeon.
    /// </summary>
    private void PrintStepByStepMessage()
    {
        PrintDungeon();
        Console.WriteLine("Press any key to advance the dungeon generation.");
        Console.ReadKey();
    }
}