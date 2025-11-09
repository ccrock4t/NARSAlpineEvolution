using UnityEngine;

public class Directions : MonoBehaviour
{
    /// <summary>
    /// ===============
    /// ==================== 7X7
    /// ===============
    /// </summary>
    //public enum Direction
    //{
    //    n3x_n3y, n2x_n3y, n1x_n3y, p0x_n3y, p1x_n3y, p2x_n3y, p3x_n3y,

    //    n3x_n2y, n2x_n2y, n1x_n2y, p0x_n2y, p1x_n2y, p2x_n2y, p3x_n2y,

    //    n3x_n1y, n2x_n1y, n1x_n1y, p0x_n1y, p1x_n1y, p2x_n1y, p3x_n1y,

    //    n3x_p0y, n2x_p0y, n1x_p0y, p0x_p0y, p1x_p0y, p2x_p0y, p3x_p0y,

    //    n3x_p1y, n2x_p1y, n1x_p1y, p0x_p1y, p1x_p1y, p2x_p1y, p3x_p1y,

    //    n3x_p2y, n2x_p2y, n1x_p2y, p0x_p2y, p1x_p2y, p2x_p2y, p3x_p2y,

    //    n3x_p3y, n2x_p3y, n1x_p3y, p0x_p3y, p1x_p3y, p2x_p3y, p3x_p3y,
    //}

    //// 7x7 offsets: x,y ∈ [-3, 3]
    //public static readonly Vector2Int[] CardinalDirs =
    //{
    //    3*Vector2Int.left + 3*Vector2Int.down,  // n3x_n3y
    //    2*Vector2Int.left + 3*Vector2Int.down,  // n2x_n3y
    //    1*Vector2Int.left + 3*Vector2Int.down,  // n1x_n3y
    //    0*Vector2Int.left + 3*Vector2Int.down,  // p0x_n3y
    //    1*Vector2Int.right + 3*Vector2Int.down, // p1x_n3y
    //    2*Vector2Int.right + 3*Vector2Int.down, // p2x_n3y
    //    3*Vector2Int.right + 3*Vector2Int.down, // p3x_n3y

    //    3*Vector2Int.left + 2*Vector2Int.down,  // n3x_n2y
    //    2*Vector2Int.left + 2*Vector2Int.down,  // n2x_n2y
    //    1*Vector2Int.left + 2*Vector2Int.down,  // n1x_n2y
    //    0*Vector2Int.left + 2*Vector2Int.down,  // p0x_n2y
    //    1*Vector2Int.right + 2*Vector2Int.down, // p1x_n2y
    //    2*Vector2Int.right + 2*Vector2Int.down, // p2x_n2y
    //    3*Vector2Int.right + 2*Vector2Int.down, // p3x_n2y

    //    3*Vector2Int.left + 1*Vector2Int.down,  // n3x_n1y
    //    2*Vector2Int.left + 1*Vector2Int.down,  // n2x_n1y
    //    1*Vector2Int.left + 1*Vector2Int.down,  // n1x_n1y
    //    0*Vector2Int.left + 1*Vector2Int.down,  // p0x_n1y
    //    1*Vector2Int.right + 1*Vector2Int.down, // p1x_n1y
    //    2*Vector2Int.right + 1*Vector2Int.down, // p2x_n1y
    //    3*Vector2Int.right + 1*Vector2Int.down, // p3x_n1y

    //    3*Vector2Int.left + 0*Vector2Int.down,  // n3x_p0y
    //    2*Vector2Int.left + 0*Vector2Int.down,  // n2x_p0y
    //    1*Vector2Int.left + 0*Vector2Int.down,  // n1x_p0y
    //    0*Vector2Int.left + 0*Vector2Int.down,  // p0x_p0y (center)
    //    1*Vector2Int.right + 0*Vector2Int.down, // p1x_p0y
    //    2*Vector2Int.right + 0*Vector2Int.down, // p2x_p0y
    //    3*Vector2Int.right + 0*Vector2Int.down, // p3x_p0y

    //    3*Vector2Int.left + 1*Vector2Int.up,    // n3x_p1y
    //    2*Vector2Int.left + 1*Vector2Int.up,    // n2x_p1y
    //    1*Vector2Int.left + 1*Vector2Int.up,    // n1x_p1y
    //    0*Vector2Int.left + 1*Vector2Int.up,    // p0x_p1y
    //    1*Vector2Int.right + 1*Vector2Int.up,   // p1x_p1y
    //    2*Vector2Int.right + 1*Vector2Int.up,   // p2x_p1y
    //    3*Vector2Int.right + 1*Vector2Int.up,   // p3x_p1y

    //    3*Vector2Int.left + 2*Vector2Int.up,    // n3x_p2y
    //    2*Vector2Int.left + 2*Vector2Int.up,    // n2x_p2y
    //    1*Vector2Int.left + 2*Vector2Int.up,    // n1x_p2y
    //    0*Vector2Int.left + 2*Vector2Int.up,    // p0x_p2y
    //    1*Vector2Int.right + 2*Vector2Int.up,   // p1x_p2y
    //    2*Vector2Int.right + 2*Vector2Int.up,   // p2x_p2y
    //    3*Vector2Int.right + 2*Vector2Int.up,   // p3x_p2y

    //    3*Vector2Int.left + 3*Vector2Int.up,    // n3x_p3y
    //    2*Vector2Int.left + 3*Vector2Int.up,    // n2x_p3y
    //    1*Vector2Int.left + 3*Vector2Int.up,    // n1x_p3y
    //    0*Vector2Int.left + 3*Vector2Int.up,    // p0x_p3y
    //    1*Vector2Int.right + 3*Vector2Int.up,   // p1x_p3y
    //    2*Vector2Int.right + 3*Vector2Int.up,   // p2x_p3y
    //    3*Vector2Int.right + 3*Vector2Int.up,   // p3x_p3y
    //};

    /// <summary>
    /// ===============
    /// ==================== 5X5
    /// ===============
    /// </summary>
    //public enum Direction
    //{
    //    n2x_n2y,
    //    n1x_n2y,
    //    p0x_n2y,
    //    p1x_n2y,
    //    p2x_n2y,

    //    n2x_n1y,
    //    n1x_n1y,
    //    p0x_n1y,
    //    p1x_n1y,
    //    p2x_n1y,

    //    n2x_p0y,
    //    n1x_p0y,
    //    p0x_p0y,
    //    p1x_p0y,
    //    p2x_p0y,

    //    n2x_p1y,
    //    n1x_p1y,
    //    p0x_p1y,
    //    p1x_p1y,
    //    p2x_p1y,

    //    n2x_p2y,
    //    n1x_p2y,
    //    p0x_p2y,
    //    p1x_p2y,
    //    p2x_p2y,


    //}

    //// Directions: up, down, left, right
    //public static readonly Vector2Int[] CardinalDirs =
    //{
    //    2*Vector2Int.left + 2*Vector2Int.down,  // n2x_n2y
    //    1*Vector2Int.left + 2*Vector2Int.down,  // n1x_n2y
    //    0*Vector2Int.left + 2*Vector2Int.down,  // p0x_n2y
    //    1*Vector2Int.right + 2*Vector2Int.down, // p1x_n2y
    //    2*Vector2Int.right + 2*Vector2Int.down, // p2x_n2y

    //    2*Vector2Int.left + 1*Vector2Int.down,  // n2x_n1y
    //    1*Vector2Int.left + 1*Vector2Int.down,  // n1x_n1y
    //    0*Vector2Int.left + 1*Vector2Int.down,  // p0x_n1y
    //    1*Vector2Int.right + 1*Vector2Int.down, // p1x_n1y
    //    2*Vector2Int.right + 1*Vector2Int.down, // p2x_n1y

    //    2*Vector2Int.left + 0*Vector2Int.down,  // n2x_p0y
    //    1*Vector2Int.left + 0*Vector2Int.down,  // n1x_p0y
    //    0*Vector2Int.left + 0*Vector2Int.down,  // p0x_p0y (center)
    //    1*Vector2Int.right + 0*Vector2Int.down, // p1x_p0y
    //    2*Vector2Int.right + 0*Vector2Int.down, // p2x_p0y

    //    2*Vector2Int.left + 1*Vector2Int.up,    // n2x_p1y
    //    1*Vector2Int.left + 1*Vector2Int.up,    // n1x_p1y
    //    0*Vector2Int.left + 1*Vector2Int.up,    // p0x_p1y
    //    1*Vector2Int.right + 1*Vector2Int.up,   // p1x_p1y
    //    2*Vector2Int.right + 1*Vector2Int.up,   // p2x_p1y

    //    2*Vector2Int.left + 2*Vector2Int.up,    // n2x_p2y
    //    1*Vector2Int.left + 2*Vector2Int.up,    // n1x_p2y
    //    0*Vector2Int.left + 2*Vector2Int.up,    // p0x_p2y
    //    1*Vector2Int.right + 2*Vector2Int.up,   // p1x_p2y
    //    2*Vector2Int.right + 2*Vector2Int.up,   // p2x_p2y

    //};

    /// <summary>
    /// ===============
    /// ==================== 3X3
    /// ===============
    /// </summary>
    /// 
    public enum Direction
    {
        n1x_n1y, p0x_n1y, p1x_n1y,

        n1x_p0y, p0x_p0y, p1x_p0y,

        n1x_p1y, p0x_p1y, p1x_p1y,
    }

    // 3x3 offsets: x,y ∈ [-1, 1]
    public static readonly Vector2Int[] CardinalDirs =
    {
        1*Vector2Int.left + 1*Vector2Int.down,  // n1x_n1y
        0*Vector2Int.left + 1*Vector2Int.down,  // p0x_n1y
        1*Vector2Int.right + 1*Vector2Int.down, // p1x_n1y

        1*Vector2Int.left + 0*Vector2Int.down,  // n1x_p0y
        0*Vector2Int.left + 0*Vector2Int.down,  // p0x_p0y (center)
        1*Vector2Int.right + 0*Vector2Int.down, // p1x_p0y

        1*Vector2Int.left + 1*Vector2Int.up,    // n1x_p1y
        0*Vector2Int.left + 1*Vector2Int.up,    // p0x_p1y
        1*Vector2Int.right + 1*Vector2Int.up,   // p1x_p1y
    };

}

