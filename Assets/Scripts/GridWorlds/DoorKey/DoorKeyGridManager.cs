using System.Collections.Generic;
using UnityEngine;

public class DoorKeyGridManager : MonoBehaviour
{
    public enum TileType
    {
        Empty,
        Wall,
        Key,
        Door,
        Goal,
        Player
    }

    [Header("Grid Settings")]
    public int width = 20;
    public int height = 20;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject playerPrefab;
    public GameObject keyPrefab;
    public GameObject doorPrefab;
    public GameObject goalPrefab;
    public GameObject wallPrefab;

    [Range(0f, 1f)]
    public float randomOpenDensity = 0.10f;

    public TileType[,] grid;
    public GameObject[,] tileObjects; // <---- NEW: all object tracking (not just walls)

    public static DoorKeyGridManager Instance;

    private Vector2Int playerPos, keyPos, doorPos, goalPos;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GenerateGrid();
        GenerateDoorKeyLayout();
    }

    // --------------------------------------------------
    //  GRID CREATION
    // --------------------------------------------------
    void GenerateGrid()
    {
        grid = new TileType[width, height];
        tileObjects = new GameObject[width, height]; // Track all tiles

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Always create a floor underneath
                Instantiate(floorPrefab, new Vector3(x, y, 0), Quaternion.identity);

                // Initialize as wall
                var wall = Instantiate(wallPrefab, new Vector3(x, y, 0), Quaternion.identity);
                tileObjects[x, y] = wall;
                grid[x, y] = TileType.Wall;
            }
        }
    }

    // --------------------------------------------------
    //  MAIN LAYOUT
    // --------------------------------------------------
    void GenerateDoorKeyLayout()
    {
        // Choose major positions
        playerPos = new Vector2Int(Random.Range(1, width / 3), Random.Range(1, height - 2));
        goalPos = new Vector2Int(Random.Range(width / 2, width - 2), Random.Range(1, height - 2));
        doorPos = new Vector2Int((playerPos.x + goalPos.x) / 2, (playerPos.y + goalPos.y) / 2);

        // Key closer to player
        keyPos = new Vector2Int(
            Mathf.Clamp(playerPos.x + Random.Range(1, width / 4), 1, width - 2),
            Mathf.Clamp(playerPos.y + Random.Range(-2, 2), 1, height - 2)
        );

        // Carve guaranteed sequential paths
        HashSet<Vector2Int> carved = new HashSet<Vector2Int>();
        CarvePath(playerPos, keyPos, carved);
        CarvePath(keyPos, doorPos, carved);
        CarvePath(doorPos, goalPos, carved);

        // Place special objects
        PlaceObject(playerPrefab, playerPos, TileType.Player);
        PlaceObject(keyPrefab, keyPos, TileType.Key);
        PlaceObject(doorPrefab, doorPos, TileType.Door);
        PlaceObject(goalPrefab, goalPos, TileType.Goal);

        // Optional: open some random empty tiles for variety
        AddRandomOpenings(carved, randomOpenDensity);
    }

    // --------------------------------------------------
    //  PATH CARVING
    // --------------------------------------------------
    void CarvePath(Vector2Int start, Vector2Int end, HashSet<Vector2Int> carved)
    {
        Vector2Int current = start;
        carved.Add(current);
        ClearTileAt(current);
        grid[current.x, current.y] = TileType.Empty;

        while (current != end)
        {
            Vector2Int step = new Vector2Int(
                current.x < end.x ? 1 : (current.x > end.x ? -1 : 0),
                current.y < end.y ? 1 : (current.y > end.y ? -1 : 0)
            );

            // Randomly alternate between x/y movement
            if (Random.value < 0.5f)
                step = new Vector2Int(step.x, 0);
            else
                step = new Vector2Int(0, step.y);

            Vector2Int next = current + step;
            if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                break;

            ClearTileAt(next);
            carved.Add(next);
            grid[next.x, next.y] = TileType.Empty;
            current = next;
        }
    }

    // --------------------------------------------------
    //  TILE REMOVAL / REPLACEMENT
    // --------------------------------------------------
    public void ClearTileAt(Vector2Int pos)
    {
        if (tileObjects[pos.x, pos.y] != null)
        {
            Destroy(tileObjects[pos.x, pos.y]);
            tileObjects[pos.x, pos.y] = null;
        }
    }

    // --------------------------------------------------
    //  RANDOM OPENINGS
    // --------------------------------------------------
    void AddRandomOpenings(HashSet<Vector2Int> carved, float density)
    {
        int openings = (int)(width * height * density);
        for (int i = 0; i < openings; i++)
        {
            Vector2Int rand = new Vector2Int(Random.Range(1, width - 1), Random.Range(1, height - 1));
            if (grid[rand.x, rand.y] == TileType.Wall)
            {
                grid[rand.x, rand.y] = TileType.Empty;
                ClearTileAt(rand);
            }
        }
    }

    // --------------------------------------------------
    //  OBJECT PLACEMENT
    // --------------------------------------------------
    void PlaceObject(GameObject prefab, Vector2Int gridPos, TileType type)
    {
        // Destroy anything currently occupying that tile (no overlaps!)
        ClearTileAt(gridPos);

        var obj = Instantiate(prefab, new Vector3(gridPos.x, gridPos.y, 0), Quaternion.identity);
        tileObjects[gridPos.x, gridPos.y] = obj;
        grid[gridPos.x, gridPos.y] = type;
    }
}
