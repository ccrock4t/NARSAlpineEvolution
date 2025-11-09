using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AlpineGridManager;
using static Directions;

public class NARSBody
{
    static IEnumerable<Direction> _directions;
    public NARS nars;
    public int timesteps_alive = 0;
    public int energy = ENERGY_IN_FOOD;
    public int food_eaten = 0;
    public int movement = 0;
    public const int MAX_LIFE = 100;
    public int remaining_life = MAX_LIFE;
    public NARSBody(NARS nars)
    {
        this.nars = nars;
    }

    public void Sense(Vector2Int position, AlpineGridManager gridManager)
    {
        if (_directions == null) _directions = Enum.GetValues(typeof(Direction)).Cast<Direction>();
        foreach (var direction in _directions)
        {
            Vector2Int neighbor_pos = position + GetMovementVectorFromDirection(direction);
            if (!IsInBounds(neighbor_pos)) continue;
            var neighbor_type = gridManager.grid[neighbor_pos.x, neighbor_pos.y];
            var sensor_term = GetSensorTermForTileTypeAndDirection(neighbor_type, direction);
            if(sensor_term == null) continue;
            var sensation = new Judgment(this.nars, sensor_term, new(1.0f, 0.99f));
            nars.SendInput(sensation);
        }


    }

    public StatementTerm GetSensorTermForTileTypeAndDirection(TileType type, Direction direction)
    {
        if(type == TileType.Empty)
        {
            return null;
        }else if(type == TileType.Grass)
        {
            return NARSGenome.grass_seen_terms[direction];
        }
        else if (type == TileType.Goat)
        {
            return NARSGenome.goat_seen_terms[direction];
        }
        else if (type == TileType.Water)
        {
            return NARSGenome.water_seen[direction];
        }
        return null;
    }

 
    public float GetFitness()
    {
        if(food_eaten > 0)
        {
            return food_eaten;
        }
        else
        {
            float move_score = ((float)movement / timesteps_alive);
            return move_score;
        }

    }
}
