using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GridPathfinder2D : MonoBehaviour
{
    [Header("Налаштування сітки")]
    public LayerMask obstacleLayer;
    [Min(0.1f)] public float cellSize = 0.5f;
    [Min(0f)] public float obstacleRadius = 0.2f;
    [Min(1)] public int searchMargin = 12;

    [Header("Оновлення маршруту")]
    [Min(0.1f)] public float repathInterval = 0.4f;
    [Min(0.01f)] public float waypointDistance = 0.1f;

    private const int SearchBudget = 1400;
    private const float DiagonalCost = 1.41421356f;
    private const float WaypointClearance = 0.04f;

    private readonly List<Vector2> path = new List<Vector2>();
    private readonly List<Node> open = new List<Node>();
    private readonly Dictionary<Vector2Int, float> costs = new Dictionary<Vector2Int, float>();
    private readonly Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
    private readonly Dictionary<Vector2Int, bool> walkable = new Dictionary<Vector2Int, bool>();
    private readonly Collider2D[] overlap = new Collider2D[1];
    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private ContactFilter2D filter;
    private Vector2 footprintSize;
    private Vector2 footprintOffset;
    private Vector2 lastTarget;
    private float nextSearchTime;
    private int waypoint;

    private struct Node
    {
        public Vector2Int Cell;
        public float Cost;
        public float Estimate;
        public float Total => Cost + Estimate;

        public Node(Vector2Int cell, float cost, float estimate)
        {
            Cell = cell;
            Cost = cost;
            Estimate = estimate;
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        filter.SetLayerMask(obstacleLayer);
        filter.useTriggers = false;
    }

    // Returns the displacement to the next steering point so callers can stop at it.
    public Vector2 GetMovementToTarget(Vector2 target)
    {
        Vector2 start = body != null ? body.position : (Vector2)transform.position;
        UpdateFootprint(start);

        if (CanTraverse(start, target))
        {
            ClearPath();
            return target - start;
        }

        bool targetMoved = (target - lastTarget).sqrMagnitude >= cellSize * cellSize;
        Vector2 step = FollowPath(start);
        if (Time.time >= nextSearchTime && (targetMoved || step == Vector2.zero))
        {
            FindPath(start, target);
            lastTarget = target;
            nextSearchTime = Time.time + repathInterval;
            return FollowPath(start);
        }

        return step;
    }

    private Vector2 FollowPath(Vector2 start)
    {
        float arrival = Mathf.Min(waypointDistance, cellSize * 0.25f);
        while (waypoint < path.Count && (path[waypoint] - start).sqrMagnitude <= arrival * arrival)
            waypoint++;

        // Skip a nearby waypoint only if the actual collider fits on the shortcut.
        for (int i = Mathf.Min(waypoint + 4, path.Count - 1); i >= waypoint; i--)
        {
            if (CanTraverse(start, path[i]))
            {
                waypoint = i;
                return path[i] - start;
            }
        }

        return Vector2.zero;
    }

    public bool HasClearLine(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        return Physics2D.Raycast(from, delta.normalized, filter, hits, delta.magnitude) == 0;
    }

    private void UpdateFootprint(Vector2 start)
    {
        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            footprintSize = bounds.size;
            footprintOffset = (Vector2)bounds.center - start;
        }
        else
        {
            footprintSize = Vector2.one * obstacleRadius * 2f;
            footprintOffset = Vector2.zero;
        }
    }

    private bool CanTraverse(Vector2 from, Vector2 to)
    {
        if (MapBounds.Instance != null &&
            !MapBounds.Instance.Rect.Contains(to + footprintOffset))
            return false;

        Vector2 delta = to - from;
        if (delta.sqrMagnitude <= 0.000001f)
            return true;

        Vector2 direction = delta.normalized;
        Vector2 origin = from + footprintOffset;
        Vector2 clearanceSize = footprintSize + Vector2.one * (2f * WaypointClearance);
        int count = Physics2D.BoxCast(origin, clearanceSize, 0f,
            direction, filter, hits, delta.magnitude);

        // An animal already next to a wall may need to leave the clearance margin first.
        if (count > 0 && hits[0].distance <= 0.001f &&
            Physics2D.OverlapBox(origin, clearanceSize, 0f, filter, overlap) > 0)
            count = Physics2D.BoxCast(origin, footprintSize, 0f, direction, filter, hits, delta.magnitude);

        for (int i = 0; i < count; i++)
        {
            if (Vector2.Dot(direction, hits[i].normal) < -0.001f)
                return false;
        }

        return count < hits.Length;
    }

    private bool IsWalkable(Vector2Int cell)
    {
        if (walkable.TryGetValue(cell, out bool free))
            return free;

        Vector2 center = GridToWorld(cell) + footprintOffset;
        free = (MapBounds.Instance == null || MapBounds.Instance.Rect.Contains(center)) &&
               Physics2D.OverlapBox(center, footprintSize + Vector2.one * (2f * WaypointClearance),
                   0f, filter, overlap) == 0;
        walkable[cell] = free;
        return free;
    }

    private void FindPath(Vector2 start, Vector2 target)
    {
        path.Clear();
        waypoint = 0;
        open.Clear();
        costs.Clear();
        parents.Clear();
        walkable.Clear();

        if (MapBounds.Instance != null)
            target = MapBounds.Instance.Clamp(target, cellSize);

        Vector2Int startCell = WorldToGrid(start);
        Vector2Int targetCell = WorldToGrid(target);
        Vector2Int min = Vector2Int.Min(startCell, targetCell) - Vector2Int.one * searchMargin;
        Vector2Int max = Vector2Int.Max(startCell, targetCell) + Vector2Int.one * searchMargin;
        Vector2Int closest = startCell;
        float closestDistance = (start - target).sqrMagnitude;
        costs[startCell] = 0f;
        open.Add(new Node(startCell, 0f, Heuristic(startCell, targetCell)));

        int expanded = 0;
        while (open.Count > 0 && expanded < SearchBudget)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].Total < open[best].Total ||
                    (Mathf.Approximately(open[i].Total, open[best].Total) &&
                     open[i].Estimate < open[best].Estimate))
                    best = i;
            }

            Node current = open[best];
            open.RemoveAt(best);
            if (current.Cost > costs[current.Cell])
                continue;

            expanded++;
            Vector2 from = current.Cell == startCell ? start : GridToWorld(current.Cell);
            float distance = (from - target).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = current.Cell;
                closestDistance = distance;
            }

            if (current.Cell == targetCell)
            {
                closest = current.Cell;
                break;
            }

            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                Vector2Int neighbor = current.Cell + new Vector2Int(x, y);
                if (neighbor.x < min.x || neighbor.x > max.x ||
                    neighbor.y < min.y || neighbor.y > max.y)
                    continue;

                float cost = current.Cost + (x != 0 && y != 0 ? DiagonalCost : 1f);
                if (costs.TryGetValue(neighbor, out float oldCost) && cost >= oldCost)
                    continue;

                if (!IsWalkable(neighbor) || !CanTraverse(from, GridToWorld(neighbor)))
                    continue;

                costs[neighbor] = cost;
                parents[neighbor] = current.Cell;
                open.Add(new Node(neighbor, cost, Heuristic(neighbor, targetCell)));
            }
        }

        for (Vector2Int cell = closest; cell != startCell; cell = parents[cell])
            path.Add(GridToWorld(cell));

        path.Reverse();
    }

    private Vector2Int WorldToGrid(Vector2 position)
    {
        return new Vector2Int(Mathf.RoundToInt(position.x / cellSize), Mathf.RoundToInt(position.y / cellSize));
    }

    private Vector2 GridToWorld(Vector2Int cell)
    {
        return new Vector2(cell.x * cellSize, cell.y * cellSize);
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + (DiagonalCost - 1f) * Mathf.Min(dx, dy);
    }

    public void ClearPath()
    {
        path.Clear();
        waypoint = 0;
        nextSearchTime = 0f;
    }
}
