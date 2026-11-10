using UnityEngine;

public class MapBounds : MonoBehaviour
{
    public static MapBounds Instance { get; private set; }

    [SerializeField] private Vector2 size = new Vector2(64f, 48f);
    [SerializeField] private Vector2 center = Vector2.zero;
    [SerializeField] private float wallThickness = 2f;
    [SerializeField] private string wallLayer = "Obstacles";

    public Rect Rect => new Rect(center - size * 0.5f, size);

    private void Awake()
    {
        Instance = this;
        BuildWalls();
    }

    public Vector2 Clamp(Vector2 point, float margin)
    {
        Rect r = Rect;
        return new Vector2(
            Mathf.Clamp(point.x, r.xMin + margin, r.xMax - margin),
            Mathf.Clamp(point.y, r.yMin + margin, r.yMax - margin));
    }

    private void BuildWalls()
    {
        int layer = LayerMask.NameToLayer(wallLayer);
        if (layer < 0)
        {
            Debug.LogWarning("MapBounds: шару '" + wallLayer + "' не існує. Створи його в Layers.", this);
        }

        Rect r = Rect;
        float t = wallThickness;

        CreateWall("Wall_Left", new Vector2(r.xMin - t * 0.5f, center.y), new Vector2(t, size.y + t * 2f), layer);
        CreateWall("Wall_Right", new Vector2(r.xMax + t * 0.5f, center.y), new Vector2(t, size.y + t * 2f), layer);
        CreateWall("Wall_Bottom", new Vector2(center.x, r.yMin - t * 0.5f), new Vector2(size.x + t * 2f, t), layer);
        CreateWall("Wall_Top", new Vector2(center.x, r.yMax + t * 0.5f), new Vector2(size.x + t * 2f, t), layer);
    }

    private void CreateWall(string wallName, Vector2 position, Vector2 wallSize, int layer)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform);
        wall.transform.position = position;
        if (layer >= 0) wall.layer = layer;

        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = wallSize;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}