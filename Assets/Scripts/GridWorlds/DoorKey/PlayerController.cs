using UnityEngine;
using static DoorKeyGridManager;

public class PlayerController : MonoBehaviour
{
    public float moveDelay = 0.2f;
    private float lastMoveTime = 0f;

    private Vector2Int gridPos;
    public bool hasKey = false;

    void Start()
    {
        var ix = Mathf.RoundToInt(transform.position.x);
        var iy = Mathf.RoundToInt(transform.position.y);
        transform.position = new Vector3(ix, iy, 0);
        gridPos = new Vector2Int(ix, iy);
    }

    void Update()
    {
        if (Time.time - lastMoveTime > moveDelay)
        {
            Vector2Int direction = Vector2Int.zero;
            if (Input.GetKey(KeyCode.W)) direction = Vector2Int.up;
            else if (Input.GetKey(KeyCode.S)) direction = Vector2Int.down;
            else if (Input.GetKey(KeyCode.A)) direction = Vector2Int.left;
            else if (Input.GetKey(KeyCode.D)) direction = Vector2Int.right;

            if (direction != Vector2Int.zero)
            {
                Vector2Int targetPos = gridPos + direction;

                if (CanMove(targetPos))
                {
                    MoveTo(targetPos);
                    lastMoveTime = Time.time;
                }
            }
        }
    }

    // --------------------------------------------------
    //  CAN MOVE CHECK
    // --------------------------------------------------
    bool CanMove(Vector2Int targetPos)
    {
        // Stay in bounds
        if (targetPos.x < 0 || targetPos.x >= DoorKeyGridManager.Instance.width ||
            targetPos.y < 0 || targetPos.y >= DoorKeyGridManager.Instance.height)
            return false;

        TileType tile = DoorKeyGridManager.Instance.grid[targetPos.x, targetPos.y];

        // Handle door logic
        if (tile == TileType.Door && !hasKey)
        {
            Debug.Log("Door is locked.");
            return false;
        }

        // Only walkable/interactable tiles are allowed
        return tile == TileType.Empty || tile == TileType.Key || tile == TileType.Goal || (tile == TileType.Door && hasKey);
    }

    // --------------------------------------------------
    //  MOVEMENT & INTERACTION
    // --------------------------------------------------
    void MoveTo(Vector2Int newPos)
    {
        TileType tile = DoorKeyGridManager.Instance.grid[newPos.x, newPos.y];

        switch (tile)
        {
            case TileType.Wall:
                Debug.Log("Hit a wall");
                return;

            case TileType.Key:
                hasKey = true;
                Debug.Log("Picked up the key!");
                // Remove the key object from grid
                DoorKeyGridManager.Instance.ClearTileAt(newPos);
                break;

            case TileType.Door:
                if (hasKey)
                {
                    Debug.Log("Unlocked the door!");
                    // Remove the door object from the grid
                    DoorKeyGridManager.Instance.ClearTileAt(newPos);
                }
                else
                {
                    Debug.Log("The door is locked.");
                    return;
                }
                break;

            case TileType.Goal:
                Debug.Log("Reached the goal!");
                break;
        }

        // Clear old position
        DoorKeyGridManager.Instance.grid[gridPos.x, gridPos.y] = TileType.Empty;
        DoorKeyGridManager.Instance.tileObjects[gridPos.x, gridPos.y] = null;

        // Move player
        gridPos = newPos;
        transform.position = new Vector3(gridPos.x, gridPos.y, 0);

        // Update grid
        DoorKeyGridManager.Instance.grid[gridPos.x, gridPos.y] = TileType.Player;
        DoorKeyGridManager.Instance.tileObjects[gridPos.x, gridPos.y] = gameObject;
    }
}
