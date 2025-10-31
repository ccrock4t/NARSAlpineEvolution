using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static AlpineGridManager;
using Random = UnityEngine.Random;

public class AlpineGridManager : MonoBehaviour
{
    public enum TileType
    {
        Empty,
        Goat,  
        Grass,
        Water
    }

    public enum Direction
    {
        N,
        NE,
        E,
        SE,
        S,
        SW,
        W,
        NW
    }

    public class Agent
    {
        public NARSGenome genome;
        public NARS nars;
        public NARSBody narsBody;

        public Agent()
        {
            genome = new NARSGenome();
            nars = new NARS(genome);
            narsBody = new(nars);
        }
    }

    [Header("Grid Settings")]
    public const int width = 40;
    public const int height = 40;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wolfPrefab;
    public GameObject goatPrefab;
    public GameObject grassPrefab;
    public GameObject waterPrefab;

    [Range(0f, 1f)]
    public float randomOpenDensity = 0.10f;

    public TileType[,] grid;
    public GameObject[,] gridGameobjects; // tracks the single occupant for each tile
    public Agent[,] gridAgents; // tracks the single occupant for each tile

    public Dictionary<Agent,bool> agentsdict = new();


    public static AlpineGridManager Instance;

    public int timestep = 0;

    // --- Tick settings ---
    [Header("Tick Settings")]
    [Tooltip("Seconds between simulation steps.")]
    private float tickSeconds = 0.1f;
    private float _tickTimer = 0f;
    public const int ENERGY_IN_FOOD = 25;

    // Directions: up, down, left, right
    private static readonly Vector2Int[] CardinalDirs =
    { 
        Vector2Int.up,
        Vector2Int.up + Vector2Int.right, 
        Vector2Int.right,
        Vector2Int.down + Vector2Int.right,
        Vector2Int.down, 
        Vector2Int.down + Vector2Int.left,
        Vector2Int.left,
        Vector2Int.up + Vector2Int.left,
    };

    public const int NUM_OF_NARS_AGENTS = 25;
    AnimatTable table;

    public TMP_Text timestepTXT;

    void Awake()
    {
        Instance = this;



        table = new(AnimatTable.SortingRule.sorted,AnimatTable.ScoreType.objective_fitness);
    }

    void Start()
    {
        GenerateGrid();
        GenerateInitialObjectLayout();
    }

    public void UpdateUI()
    {
        timestepTXT.text = "Timestep: " + timestep;
    }

    void FixedUpdate()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= tickSeconds)
        {
            timestep++;
            _tickTimer = 0f;
            StepSimulation();

            if(agentsdict.Count < NUM_OF_NARS_AGENTS)
            {
                SpawnNewAgent();
            }
            UpdateUI();
        }
    
    }

    private void SpawnNewAgent()
    {
        if (!TryFindRandomEmptyCell(out var pos)) return; // grid full

        // Place the visual/object
        if (!PlaceObject(goatPrefab, pos, TileType.Goat)) return;

        // Create and register the agent
        var agent = new Agent();
        gridAgents[pos.x, pos.y] = agent;
        agentsdict.Add(agent,true);
    }


    bool IsCellEmpty(Vector2Int p)
    {
        return IsInBounds(p)
            && grid[p.x, p.y] == TileType.Empty
            && gridGameobjects[p.x, p.y] == null
            && gridAgents[p.x, p.y] == null;
    }

    bool TryFindRandomEmptyCell(out Vector2Int pos, int maxAttempts = 2000)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            var candidate = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            if (IsCellEmpty(candidate))
            {
                pos = candidate;
                return true;
            }
        }
        // Fallback: linear scan in case the grid is crowded
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var p = new Vector2Int(x, y);
                if (IsCellEmpty(p))
                {
                    pos = p;
                    return true;
                }
            }

        pos = default;
        return false;
    }

    // Overwrite your PlaceObject to be "safe" (no clearing/replacing).
    // If you prefer, keep the original and add this as TryPlaceObject(...) instead.
    bool PlaceObject(GameObject prefab, Vector2Int gridPos, TileType type)
    {
        if (!IsCellEmpty(gridPos)) return false;

        var obj = Instantiate(prefab, new Vector3(gridPos.x, gridPos.y, 0f), Quaternion.identity);
        gridGameobjects[gridPos.x, gridPos.y] = obj;
        grid[gridPos.x, gridPos.y] = type;
        return true;
    }

    // --------------------------------------------------
    //  GRID CREATION
    // --------------------------------------------------
    void GenerateGrid()
    {
        grid = new TileType[width, height];
        gridGameobjects = new GameObject[width, height];
        gridAgents = new Agent[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Instantiate(floorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                grid[x, y] = TileType.Empty;
            }
        }
    }

    // --------------------------------------------------
    //  MAIN LAYOUT
    // --------------------------------------------------
    void GenerateInitialObjectLayout()
    {
        int placedGrass = 0, placedWater = 0;
        while (placedGrass < 100 && TryFindRandomEmptyCell(out var grassPos))
            if (PlaceObject(grassPrefab, grassPos, TileType.Grass)) placedGrass++;

        while (placedWater < 100 && TryFindRandomEmptyCell(out var waterPos))
            if (PlaceObject(waterPrefab, waterPos, TileType.Water)) placedWater++;

        int spawned = 0;
        while (spawned < NUM_OF_NARS_AGENTS && TryFindRandomEmptyCell(out var goatPos))
        {
            if (PlaceObject(goatPrefab, goatPos, TileType.Goat))
            {
                var agent = new Agent();
                gridAgents[goatPos.x, goatPos.y] = agent;
                agentsdict.Add(agent,true);
                spawned++;
            }
        }
    }


    // --------------------------------------------------
    //  SIMULATION STEP
    // --------------------------------------------------
    void StepSimulation()
    {
        // Collect current positions of all animals (wolves + goats).
        var actor_locations = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var t = grid[x, y];
                if (IsAgent(t))
                {
                    var agent_pos = new Vector2Int(x, y);
           
                    var agent = gridAgents[x, y];

                    if(agent.narsBody.energy <= 0)
                    {
                        KillAgent(agent_pos);
                    }
                    else
                    {
                        actor_locations.Add(agent_pos);
                        agent.narsBody.Sense(agent_pos, this);
                        agent.nars.do_working_cycle();
                    }

             
                }
            }
        }

        // Shuffle so movement order is random each tick (reduces bias).
        FisherYatesShuffle(actor_locations);

        // Try to move each actor one step into a random valid neighboring cell.
        foreach (var fromLocation in actor_locations)
        {
            // If something already moved out/in earlier this tick, skip if empty now.
            var type = grid[fromLocation.x, fromLocation.y];
            if (!IsAgent(type)) continue;

            var agent = gridAgents[fromLocation.x, fromLocation.y];

            // try moves
            Direction? dirtomove = null;
            float max_move_activation = 0;
 
            foreach(var kvp in NARSGenome.move_op_terms)
            {
                var moveTerm = kvp.Value;
                float activation = agent.nars.GetGoalActivation(moveTerm);

                if(activation > max_move_activation)
                {
                    dirtomove = kvp.Key;
                    max_move_activation = activation;
                }
            }


            if (dirtomove != null)
            {
                var toLocation = fromLocation + GetMovementVectorFromDirection(dirtomove);
                MoveEntity(fromLocation, toLocation, type, agent);
            }
    

            // try eat
            Direction? dirtoeat = null;
            float max_eat_activation = 0;

            foreach (var kvp in NARSGenome.eat_op_terms)
            {
                var eatTerm = kvp.Value;
                float activation = agent.nars.GetGoalActivation(eatTerm);

                if (activation > max_eat_activation)
                {
                    dirtoeat = kvp.Key;
                    max_eat_activation = activation;
                }

            }
            if (dirtoeat != null)
            {
                var eatLocation = fromLocation + GetMovementVectorFromDirection(dirtoeat);
                TryEatEntity(agent, eatLocation);
            }
        }
    }

    private void TryEatEntity(Agent agent, Vector2Int eatLocation)
    {
        if (!IsInBounds(eatLocation)) return;
        var obj = gridGameobjects[eatLocation.x, eatLocation.y];
        if (obj == null)
        {
            // eat failed
            return;
        }
        var type = grid[eatLocation.x, eatLocation.y];
        if(type != TileType.Grass)
        {
            //eat failed
            return;
        }

        ClearTileAt(eatLocation);
        agent.narsBody.energy = ENERGY_IN_FOOD;
    }

    void KillAgent(Vector2Int agentPos)
    {
        var agent = gridAgents[agentPos.x, agentPos.y];
        grid[agentPos.x, agentPos.y] = TileType.Empty;
        gridAgents[agentPos.x, agentPos.y] = null;
        table.TryAdd(agent.narsBody.GetFitness(), agent.genome);
        ClearTileAt(agentPos);
        agentsdict.Remove(agent);
    }

    public static Vector2Int GetMovementVectorFromDirection(Direction? dirtomove)
    {
        return dirtomove switch
        {
            Direction.N => new(0, 1),
            Direction.NE => new(1, 1),
            Direction.E => new(1, 0),
            Direction.SE => new(1, -1),
            Direction.S => new(0, -1),
            Direction.SW => new(-1, -1),
            Direction.W => new(-1, 0),
            Direction.NW => new(-1, 1),
            _ => new(0, 0)
        };
    }

    bool TryGetRandomEmptyNeighbor(Vector2Int from, out Vector2Int to)
    {
        // Try directions in random order.
        var dirOrder = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = 0; i < dirOrder.Length; i++)
        {
            int j = Random.Range(i, dirOrder.Length);
            (dirOrder[i], dirOrder[j]) = (dirOrder[j], dirOrder[i]);
        }

        for (int k = 0; k < dirOrder.Length; k++)
        {
            var candidate = from + CardinalDirs[dirOrder[k]];
            if (!IsInBounds(candidate)) continue;
            // Only move into truly empty cells; grass/water block movement with current data model.
            if (grid[candidate.x, candidate.y] == TileType.Empty)
            {
                to = candidate;
                return true;
            }
        }

        to = from;
        return false;
    }

    public bool IsAgent(TileType t)
    {
        return (t == TileType.Goat);
    }

    void MoveEntity(Vector2Int from, Vector2Int to, TileType type, Agent agent)
    {
        if (!IsInBounds(to)) return;
        if (to == from) return;
        var obj = gridGameobjects[from.x, from.y];
        if (obj == null)
        {
            // Data hygiene: if object got destroyed, free the cell.
            grid[from.x, from.y] = TileType.Empty;
            return;
        }
        agent.narsBody.movement++;
        // Update transform
        obj.transform.position = new Vector3(to.x, to.y, 0f);

        // Update tracking arrays
        gridGameobjects[to.x, to.y] = obj;
        gridGameobjects[from.x, from.y] = null;

        grid[to.x, to.y] = type;
        grid[from.x, from.y] = TileType.Empty;
        gridAgents[to.x, to.y] = agent;
        gridAgents[from.x, from.y] =null;
    }

    public static bool IsInBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;
    }

    void FisherYatesShuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // --------------------------------------------------
    //  TILE REMOVAL / REPLACEMENT
    // --------------------------------------------------
    public void ClearTileAt(Vector2Int pos)
    {
        if (gridGameobjects[pos.x, pos.y] != null)
        {
            Destroy(gridGameobjects[pos.x, pos.y]);
            gridGameobjects[pos.x, pos.y] = null;
        }
        grid[pos.x, pos.y] = TileType.Empty;
    }

    // --------------------------------------------------
    //  OBJECT PLACEMENT
    // --------------------------------------------------

    NARSGenome[] GetNewAnimatReproducedFromTable(bool sexual)
    {
        (NARSGenome parent1, int parent1_idx) = table.PeekProbabilistic();

        NARSGenome[] results;
        if (sexual)
        {
            results = new NARSGenome[2];
            // sexual
            int ignore_idx = -1;
            ignore_idx = parent1_idx; // same table, so dont pick the same animat
            (NARSGenome parent2, int parent2_idx) = table.PeekProbabilistic(ignore_idx: ignore_idx);

            NARSGenome offspring1_genome;
            NARSGenome offspring2_genome;
            (offspring1_genome, offspring2_genome) = parent1.Reproduce(parent2);

            results[0] = offspring1_genome;
            results[1] = offspring2_genome;
        }
        else
        {
            results = new NARSGenome[1];
            // asexual
            NARSGenome cloned_genome = parent1.Clone();
            cloned_genome.Mutate();
            results[0] = cloned_genome;
        }
        return results;
    }
}
