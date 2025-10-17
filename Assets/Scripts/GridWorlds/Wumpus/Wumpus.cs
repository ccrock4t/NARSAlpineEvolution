using System;
using System.Collections.Generic;
using System.Linq;

public class Room
{
    public int Id;
    public HashSet<int> Connections = new HashSet<int>();
}

public class WumpusWorldGame
{
    public Dictionary<int, Room> Rooms = new Dictionary<int, Room>();
    public int WumpusRoom;
    public List<int> PitRooms = new List<int>();
    public List<int> BatRooms = new List<int>();
    public int PlayerRoom;
    public bool IsPlayerAlive = true;
    public bool IsWumpusAlive = true;
    public int Arrows = 3;

    private int roomCount;
    private Random rand = new Random();

    public WumpusWorldGame(int roomCount = 20)
    {
        this.roomCount = roomCount;
        GenerateRooms();
        PlaceHazards();
    }

    private void GenerateRooms()
    {
        bool success = false;

        while (!success)
        {
            Rooms.Clear();
            for (int i = 0; i < roomCount; i++)
                Rooms[i] = new Room { Id = i };

            List<(int roomId, int stubIndex)> stubs = new List<(int, int)>();

            for (int i = 0; i < roomCount; i++)
                for (int j = 0; j < 3; j++)
                    stubs.Add((i, j));

            stubs = stubs.OrderBy(_ => rand.Next()).ToList();

            var attemptedEdges = new HashSet<(int, int)>();

            for (int i = 0; i < stubs.Count; i += 2)
            {
                int a = stubs[i].roomId;
                int b = stubs[i + 1].roomId;

                if (a == b || Rooms[a].Connections.Contains(b))
                {
                    // Invalid edge: self-loop or duplicate
                    success = false;
                    break;
                }

                Rooms[a].Connections.Add(b);
                Rooms[b].Connections.Add(a);
                attemptedEdges.Add((Math.Min(a, b), Math.Max(a, b)));

                success = true;
            }

            // Final check: All rooms must have 3 connections
            if (success && Rooms.All(r => r.Value.Connections.Count == 3) && IsConnected())
            {
                success = true;
            }
            else
            {
                // Reset and retry
                success = false;
            }
        }
    }


    private bool IsConnected()
    {
        var visited = new HashSet<int>();
        void DFS(int current)
        {
            if (visited.Contains(current)) return;
            visited.Add(current);
            foreach (var neighbor in Rooms[current].Connections)
                DFS(neighbor);
        }

        DFS(0);
        return visited.Count == roomCount;
    }

    private void PlaceHazards()
    {
        var available = Rooms.Keys.OrderBy(x => rand.Next()).ToList();
        WumpusRoom = available[0];
        PitRooms = available.GetRange(1, 2);
        BatRooms = available.GetRange(3, 2);
        PlayerRoom = available[5];

        while (PlayerRoom == WumpusRoom || PitRooms.Contains(PlayerRoom) || BatRooms.Contains(PlayerRoom))
        {
            PlayerRoom = available[rand.Next(6, available.Count)];
        }
    }

    public string Perceive()
    {
        var adjacent = Rooms[PlayerRoom].Connections;
        bool smell = adjacent.Contains(WumpusRoom);
        bool breeze = adjacent.Any(r => PitRooms.Contains(r));
        bool bats = adjacent.Any(r => BatRooms.Contains(r));

        List<string> clues = new List<string>();
        if (smell) clues.Add("You smell something terrible nearby.");
        if (breeze) clues.Add("You feel a breeze.");
        if (bats) clues.Add("You hear flapping wings.");

        return clues.Count > 0 ? string.Join(" ", clues) : "It's quiet. No danger sensed.";
    }

    public string Move(int roomId)
    {
        if (!Rooms[PlayerRoom].Connections.Contains(roomId))
            return "You can't move there. It's not connected.";

        PlayerRoom = roomId;

        // Check for death
        if (PlayerRoom == WumpusRoom)
        {
            IsPlayerAlive = false;
            return "You walked into the Wumpus! You’ve been eaten!";
        }
        if (PitRooms.Contains(PlayerRoom))
        {
            IsPlayerAlive = false;
            return "You fell into a pit!";
        }
        if (BatRooms.Contains(PlayerRoom))
        {
            int newRoom = rand.Next(roomCount);
            PlayerRoom = newRoom;
            return "Bats whisk you away to room " + newRoom + "!";
        }

        return "You moved to room " + PlayerRoom;
    }

    public string Shoot(int targetRoom)
    {
        if (Arrows == 0)
            return "You're out of arrows!";

        Arrows--;

        if (!Rooms[PlayerRoom].Connections.Contains(targetRoom))
            return "You can only shoot into adjacent rooms.";

        if (targetRoom == WumpusRoom)
        {
            IsWumpusAlive = false;
            return "Thwack! You killed the Wumpus!";
        }

        //// Optional: 25% chance Wumpus moves
        //if (rand.NextDouble() < 0.25)
        //{
        //    var wumpusMoves = Rooms[WumpusRoom].Connections.ToList();
        //    WumpusRoom = wumpusMoves[rand.Next(wumpusMoves.Count)];
        //}

        return "Missed! The arrow thuds against a wall.";
    }

    public void PrintCurrentState()
    {
        Console.WriteLine($"\nYou are in Room {PlayerRoom}");
        Console.WriteLine($"Connected to: {string.Join(", ", Rooms[PlayerRoom].Connections)}");
        Console.WriteLine(Perceive());
        Console.WriteLine($"Arrows remaining: {Arrows}");
    }
}
