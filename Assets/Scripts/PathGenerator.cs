using System.Collections.Generic;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    public enum TileType
    {
        Start,
        Straight,
        TurnLeftStart,
        TurnLeftEnd,
        TurnRightStart,
        TurnRightEnd,
        End
    }

    public struct PathSegment
    {
        public Vector2Int Position;
        public TileType Type;

        public PathSegment(Vector2Int position, TileType type)
        {
            Position = position;
            Type = type;
        }
    }

    [Header("Path Settings")]
    public int initialLength = 20;
    [Range(0f, 1f)] public float turnChance = 0.15f;
    [Tooltip("Bias toward returning to +Z after turns")]
    [Range(0f, 1f)] public float forwardBias = 0.7f;

    private List<PathSegment> path = new List<PathSegment>();
    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    // Directions: 0 = +Z, 1 = +X, 2 = -Z, 3 = -X
    private static readonly Vector2Int[] directions =
    {
        new Vector2Int(0, 1),  // +Z
        new Vector2Int(1, 0),  // +X
        new Vector2Int(0, -1), // -Z
        new Vector2Int(-1, 0)  // -X
    };

    private int currentDir = 0; // start facing +Z
    private Vector2Int currentPos = Vector2Int.zero;
    private bool justTurned = false;

    void Start()
    {
        GeneratePath(initialLength);
    }

    public List<PathSegment> GetPath() => path;

    public void GenerateMorePath(int segments = 10)
    {
        // Turn last End into a normal Straight tile
        if (path.Count > 0)
        {
            int lastIndex = path.Count - 1;
            var last = path[lastIndex];
            path[lastIndex] = new PathSegment(last.Position, TileType.Straight);
        }

        GeneratePath(segments);
    }

    private void GeneratePath(int count)
    {
        if (path.Count == 0)
        {
            path.Add(new PathSegment(currentPos, TileType.Start));
            occupiedPositions.Add(currentPos);
        }

        for (int i = 0; i < count; i++)
        {
            Vector2Int next = currentPos + directions[currentDir];

            // Avoid overlaps
            if (occupiedPositions.Contains(next))
            {
                if (!TryTurnToAvoidCollision())
                {
                    Debug.LogWarning("Path blocked — cannot extend further.");
                    break;
                }
                next = currentPos + directions[currentDir];
            }

            occupiedPositions.Add(next);
            TileType type = TileType.Straight;
            float roll = Random.value;

            // Chance to turn — but not two turns in a row, and not backward
            if (!justTurned && roll < turnChance)
            {
                bool left = Random.value < 0.5f;
                type = left ? TileType.TurnLeftStart : TileType.TurnRightStart;
                path.Add(new PathSegment(next, type));

                Turn(left);
                justTurned = true;

                Vector2Int turnEnd = next + directions[currentDir];
                if (!occupiedPositions.Contains(turnEnd))
                {
                    occupiedPositions.Add(turnEnd);
                    path.Add(new PathSegment(turnEnd, left ? TileType.TurnLeftEnd : TileType.TurnRightEnd));
                    currentPos = turnEnd;
                }
                else
                {
                    Debug.LogWarning("Turn collided — stopping early.");
                    break;
                }

                continue;
            }

            // Occasionally reorient back to +Z
            if (currentDir != 0 && Random.value < forwardBias)
            {
                ReturnToForward();
            }

            // Add straight tile
            path.Add(new PathSegment(next, type));
            currentPos = next;
            justTurned = false;
        }

        // Mark last tile as End
        if (path.Count > 0)
        {
            int lastIndex = path.Count - 1;
            var last = path[lastIndex];
            path[lastIndex] = new PathSegment(last.Position, TileType.End);
        }
    }

    private void Turn(bool left)
    {
        currentDir = (currentDir + (left ? 3 : 1)) % 4;
    }

    private void ReturnToForward()
    {
        // Try to realign toward +Z direction (0)
        if (currentDir == 1) Turn(true);     // if facing +X, turn left
        else if (currentDir == 3) Turn(false); // if facing -X, turn right
        else if (currentDir == 2) Turn(Random.value < 0.5f); // if going -Z, random correction
    }

    private bool TryTurnToAvoidCollision()
    {
        int leftDir = (currentDir + 3) % 4;
        Vector2Int leftNext = currentPos + directions[leftDir];
        if (!occupiedPositions.Contains(leftNext))
        {
            currentDir = leftDir;
            return true;
        }

        int rightDir = (currentDir + 1) % 4;
        Vector2Int rightNext = currentPos + directions[rightDir];
        if (!occupiedPositions.Contains(rightNext))
        {
            currentDir = rightDir;
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (path == null) return;

        foreach (var segment in path)
        {
            Color c = Color.white;
            switch (segment.Type)
            {
                case TileType.Start:
                    c = Color.blue; break;
                case TileType.End:
                    c = Color.red; break;
                case TileType.TurnLeftStart:
                case TileType.TurnRightStart:
                    c = Color.yellow; break;
                case TileType.TurnLeftEnd:
                case TileType.TurnRightEnd:
                    c = Color.green; break;
            }

            Gizmos.color = c;
            Gizmos.DrawCube(new Vector3(segment.Position.x, 0, segment.Position.y), Vector3.one * 0.8f);
        }
    }

    public void GenerateImmediate()
    {
        path.Clear();
        occupiedPositions.Clear();
        currentDir = 0;
        currentPos = Vector2Int.zero;
        GeneratePath(initialLength);
    }

}
