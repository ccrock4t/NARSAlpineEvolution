using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using static AlpineGridManager;
using static Directions;
using static UnityEngine.EventSystems.EventTrigger;
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

        public Agent(NARSGenome genome)
        {
            this.genome = genome;
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

    Dictionary<Agent, Vector2Int> agentToPos = new();

    public static AlpineGridManager Instance;

    public int timestep = 0;
    public float high_score;
    public int largest_genome;

    // --- Tick settings ---
    public const int ENERGY_IN_FOOD = 10;


    // How many FixedUpdate steps per simulation tick
    private int updatesPerTick = 5;

    private int _fixedUpdateCounter = 0;


    public const int NUM_OF_NARS_AGENTS = 25;
    public const int NUM_OF_GRASS = 200;
    AnimatTable table;

    public TMP_Text timestepTXT;
    public TMP_Text scoreTXT;
    public TMP_Text genomeSizeTXT;

    private string _csvPath;
    private StreamWriter _csv;


    void Awake()
    {
        Instance = this;
                table = new(AnimatTable.SortingRule.sorted,AnimatTable.ScoreType.objective_fitness);
    }
    // --- CSV logging ---



    void Start()
    {
        GenerateGrid();
        GenerateInitialObjectLayout();
        InitCsv();   // <-- add this
    }

    string GetLogRootDirectory()
    {
#if UNITY_EDITOR
        // Editor: project root (one level up from Assets)
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
    // Standalone Win/Linux: parent of "<Game>_Data"
    return Directory.GetParent(Application.dataPath).FullName;
#elif UNITY_STANDALONE_OSX
    // macOS: Application.dataPath = ".../MyGame.app/Contents"
    // Install dir = parent of the .app bundle
    return Directory.GetParent(Application.dataPath).Parent.Parent.FullName;
#else
    // Mobile, WebGL, consoles, etc. — use the safe location
    return Application.persistentDataPath;
#endif
    }
    void InitCsv()
    {
        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var root = GetLogRootDirectory();
  

        _csvPath = Path.Combine(root, $"stats_{stamp}.csv");
        _csv = new StreamWriter(_csvPath, false);
        _csv.WriteLine("max_table_score,mean_table_score,median_table_score");

        _csv.Flush();

        Debug.Log($"CSV logging to: {_csvPath}");
    }

    float GetMedianTableScore()
    {
        int n = table.Count();
        if (n == 0) return 0f;

        // Use the already-sorted table if you're keeping it sorted;
        // otherwise copy & sort scores defensively.
        // Since AnimatTable sorts ascending (lowest at 0), this is O(1) to read.
        // If you ever switch to unsorted, uncomment the defensive path.

        // FAST PATH (sorted table):
        var list = table.table; // public List<TableEntry>
        if ((n & 1) == 1)          // odd
            return list[n / 2].score;
        else                       // even
            return 0.5f * (list[(n / 2) - 1].score + list[n / 2].score);

        // DEFENSIVE PATH (unsorted):
        // var scores = list.Select(e => e.score).ToList();
        // scores.Sort();
        // return (n % 2 == 1) ? scores[n/2]
        //                     : 0.5f * (scores[n/2 - 1] + scores[n/2]);
    }


    void WriteCsvRow()
    {
        if (_csv == null) return;

        // max table score
        float maxTable = 0f;
        var best = table.GetBest();
        if (best.HasValue) maxTable = best.Value.score;

        // mean (average) and median
        int count = table.Count();
        float mean = (count > 0) ? (table.total_score / count) : 0f;
        float median = GetMedianTableScore();

        string line = string.Join(",",
            maxTable.ToString(CultureInfo.InvariantCulture),
            mean.ToString(CultureInfo.InvariantCulture),
            median.ToString(CultureInfo.InvariantCulture)
        );

        _csv.WriteLine(line);
        _csv.Flush();
    }


    void OnDestroy()
    {
        if (_csv != null)
        {
            _csv.Flush();
            _csv.Dispose();
            _csv = null;
        }
    }

    void OnApplicationQuit()
    {
        OnDestroy(); // ensure it’s closed on quit, too
    }

    public void UpdateUI()
    {
        timestepTXT.text = "Timestep: " + timestep;
        scoreTXT.text = "High Score: " + high_score;
        genomeSizeTXT.text = "Largest genome: " + largest_genome;
    }

    void FixedUpdate()
    {
        _fixedUpdateCounter++;

        if (_fixedUpdateCounter >= updatesPerTick)
        {
            _fixedUpdateCounter = 0;   // reset for next tick
            timestep++;

            StepSimulation();

            int guard = 0;
            while (agentToPos.Count < NUM_OF_NARS_AGENTS)
            {
                if (!SpawnNewAgent()) break;
                if (++guard > 2000) break; // extra safety guard
            }
            guard = 0;
            while (placedGrass < NUM_OF_GRASS)
            {
                if (!SpawnNewGrass()) break;
                if (++guard > 2000) break; // extra safety guard
            }

            UpdateUI();
            WriteCsvRow();   // still runs on each tick
        }
    }


    private bool SpawnNewGrass()
    {
        if (!TryFindRandomEmptyCell(out var grassPos)) return false;
        if (!PlaceObject(grassPrefab, grassPos, TileType.Grass)) return false;
        placedGrass++;
        return true;
    }

    private bool SpawnNewAgent()
    {
        // New agent was placed?
        bool placedAny = false;

        int newagent = UnityEngine.Random.Range(0, 50);
        if (newagent == 0 || table.Count() < 2)
        {
            if (!TryFindRandomEmptyCell(out var pos)) return false;
            if (!PlaceObject(goatPrefab, pos, TileType.Goat)) return false;

            var agent = new Agent();
            agentToPos.Add(agent, pos);
            placedAny = true;
        }
        else
        {
            int sexual = UnityEngine.Random.Range(0, 2);
            NARSGenome[] new_genomes = GetNewAnimatReproducedFromTable(sexual == 1);

            foreach (var genome in new_genomes)
            {
                if (!TryFindRandomEmptyCell(out var pos)) return placedAny; // no more room
                if (!PlaceObject(goatPrefab, pos, TileType.Goat)) continue;

                var agent = new Agent(genome);
                agentToPos.Add(agent, pos);
                placedAny = true;
            }
        }
        return placedAny;
    }

    bool IsCellEmpty(Vector2Int p)
    {
        return IsInBounds(p)
            && grid[p.x, p.y] == TileType.Empty
            && gridGameobjects[p.x, p.y] == null;
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
    int placedGrass = 0;
    void GenerateInitialObjectLayout()
    {
        int placedWater = 0;
        while (placedGrass < NUM_OF_GRASS && TryFindRandomEmptyCell(out var grassPos))
            if (PlaceObject(grassPrefab, grassPos, TileType.Grass)) placedGrass++;

        while (placedWater < 100 && TryFindRandomEmptyCell(out var waterPos))
            if (PlaceObject(waterPrefab, waterPos, TileType.Water)) placedWater++;

        int spawned = 0;
        while (spawned < NUM_OF_NARS_AGENTS && TryFindRandomEmptyCell(out var goatPos))
        {
            if (PlaceObject(goatPrefab, goatPos, TileType.Goat))
            {
                var agent = new Agent();
                agentToPos.Add(agent, goatPos);
                spawned++;
            }
        }
    }

    List<(Vector2Int, Agent)> actor_locations = new();
    // --------------------------------------------------
    //  SIMULATION STEP
    // --------------------------------------------------
    void StepSimulation()
    {
        // Collect current positions of all animals (wolves + goats).
        actor_locations.Clear();
        var keys = agentToPos.Keys.ToArray();
        foreach (var key in keys)
        {
            var agent_pos = agentToPos[key];
            var agent = key;

            if (agent.narsBody.energy <= 0 || agent.narsBody.remaining_life <= 0)
            {
                KillAgent(agent,agent_pos);
            }
            else
            {
                actor_locations.Add((agent_pos, agent));
                for (int i =0; i<4; i++)
                {
                    agent.narsBody.Sense(agent_pos, this);
                    // enter instinctual goals
                    foreach (var goal_data in agent.nars.genome.goals)
                    {
                        var goal = new Goal(agent.nars, goal_data.statement, goal_data.evidence, occurrence_time: agent.nars.current_cycle_number);
                        agent.nars.SendInput(goal);
                    }
                    agent.nars.do_working_cycle();
                }

                agent.narsBody.timesteps_alive++;
                agent.narsBody.energy--;
                agent.narsBody.remaining_life--;

            }
        }   
 

        // Shuffle so movement order is random each tick (reduces bias).
        FisherYatesShuffle(actor_locations);


        // Try to move each actor one step into a random valid neighboring cell.
        foreach (var actor in actor_locations)
        {
            // If something already moved out/in earlier this tick, skip if empty now.
            var fromLocation = actor.Item1;
            var agent = actor.Item2;
            var type = grid[fromLocation.x, fromLocation.y];
            if (!IsAgent(type)) continue;

            // try eat
            Direction? dirtoeat = null;
            float max_eat_activation = 0.0f;

            foreach (var kvp in NARSGenome.eat_op_terms)
            {
                var eatTerm = kvp.Value;
                float activation = agent.nars.GetGoalActivation(eatTerm);
                if (activation < agent.nars.config.T) continue;
                var eatLocation = fromLocation + GetMovementVectorFromDirection((Direction)kvp.Key);
                TryEatEntity(agent, eatLocation);
            }

            // try moves
            Direction? dirtomove = null;
            float max_move_activation = 0.0f;
 
            foreach(var kvp in NARSGenome.move_op_terms)
            {
                var moveTerm = kvp.Value;
                float activation = agent.nars.GetGoalActivation(moveTerm);
                if (activation < agent.nars.config.T) continue;
                if (activation > max_move_activation)
                {
                    dirtomove = kvp.Key;
                    max_move_activation = activation;
                }
            }


            if (dirtomove != null)
            {
                var toLocation = fromLocation + GetMovementVectorFromDirection((Direction)dirtomove);
                MoveEntity(fromLocation, toLocation, agent);
            }
    


        }
    }



    void KillAgent(Agent agent, Vector2Int agentPos)
    {
        grid[agentPos.x, agentPos.y] = TileType.Empty;
        var score = agent.narsBody.GetFitness();
        table.TryAdd(score, agent.genome);
        if(score > high_score)
        {
            high_score = score;
            UpdateUI();
        }

        if(agent.genome.beliefs.Count > largest_genome)
        {
            largest_genome = agent.genome.beliefs.Count;
            UpdateUI();
        }
     
        ClearTileAt(agentPos);
        agentToPos.Remove(agent);
    }

    public static Vector2Int GetMovementVectorFromDirection(Direction dirtomove)
    {
        return CardinalDirs[(int)dirtomove];
    }


    public bool IsAgent(TileType t)
    {
        return (t == TileType.Goat);
    }
    private void TryEatEntity(Agent agent, Vector2Int eatLocation)
    {
        if (!IsInBounds(eatLocation)) return;
        var type = grid[eatLocation.x, eatLocation.y];
        if (type != TileType.Grass)
        {
            //eat failed
            return;
        }

        ClearTileAt(eatLocation);
        agent.narsBody.energy = ENERGY_IN_FOOD;
        agent.narsBody.food_eaten++;
        placedGrass--;
    }
    void MoveEntity(Vector2Int from, Vector2Int to, Agent agent)
    {
        if (!IsInBounds(to)) return;
        if (to == from) return;
      
        var to_type = grid[to.x, to.y];
        if (IsBlocked(to_type))
        {
            //move failed
            return;
        }
        var from_type = grid[from.x, from.y];
        agent.narsBody.movement++;
        // Update transform
        var obj = gridGameobjects[from.x, from.y];
        obj.transform.position = new Vector3(to.x, to.y, 0f);

        // Update tracking arrays
        gridGameobjects[to.x, to.y] = obj;
        gridGameobjects[from.x, from.y] = null;

        grid[to.x, to.y] = from_type;
        grid[from.x, from.y] = TileType.Empty;

        agentToPos[agent] = to;
    }

    public bool IsBlocked(TileType type)
    {
        return type == TileType.Water || type == TileType.Goat || type == TileType.Grass;
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

    public static string GetRandomDirectionString()
    {
        Array values = Enum.GetValues(typeof(Direction));
        int index = Random.Range(0, values.Length);  // using UnityEngine.Random
        Direction randomDir = (Direction)values.GetValue(index);
        return randomDir.ToString();
    }

}
