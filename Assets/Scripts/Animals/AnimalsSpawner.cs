using System.Collections.Generic;
using UnityEngine;

// Спавнер оленів. Кладемо на порожній об'єкт Spawner у сцені.
// На старті розставляє оленів по карті, потім раз на respawnInterval
// досіває до targetCount. Нових оленів ставить поза кадром камери.
// Потрібні: MapBounds у сцені і префаб оленя з DeerAI.
public class AnimalSpawner : MonoBehaviour
{
    [Header("Що спавнити")]
    [SerializeField] private DeerAI deerPrefab;
    [Tooltip("Гравець. Його передаємо кожному оленеві, бо префаб не може посилатися на об'єкт зі сцени.")]
    [SerializeField] private Transform player;

    [Header("Кількість")]
    [SerializeField] private int targetCount = 7;
    [Tooltip("Через скільки секунд досіюється один олень, якщо їх менше за targetCount.")]
    [SerializeField] private float respawnInterval = 60f;

    [Header("Де можна з'являтись")]
    [Tooltip("Відступ від краю карти. Має бути трохи більшим за Alert Radius оленя (5), щоб він не ходив у стіну.")]
    [SerializeField] private float edgeMargin = 6f;
    [SerializeField] private float minDistanceFromPlayer = 10f;
    [Tooltip("Табір. Необов'язково: поруч із табором тварин не ставимо.")]
    [SerializeField] private Transform camp;
    [SerializeField] private float minDistanceFromCamp = 15f;
    [Tooltip("Шари дерев, каміння та інших перешкод.")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float freeRadius = 0.6f;
    [SerializeField] private int maxAttempts = 40;

    [Header("Поза кадром (для досівання)")]
    [Tooltip("Камера гравця. Якщо порожньо, береться Camera.main.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Запас за краєм екрана, у частках екрана.")]
    [SerializeField] private float offscreenPadding = 0.15f;

    private readonly List<DeerAI> deer = new List<DeerAI>();
    private float respawnTimer;

    private void Start()
    {
        if (viewCamera == null) viewCamera = Camera.main;

        if (deerPrefab == null)
        {
            Debug.LogError("AnimalSpawner: не вказано префаб оленя.", this);
            enabled = false;
            return;
        }

        if (MapBounds.Instance == null)
        {
            Debug.LogError("AnimalSpawner: у сцені немає MapBounds.", this);
            enabled = false;
            return;
        }

        // Початкова розстановка: камера вже на місці, тому поза кадром не вимагаємо.
        for (int i = 0; i < targetCount; i++)
        {
            SpawnOne(false);
        }

        respawnTimer = respawnInterval;
    }

    private void Update()
    {
        if (AliveCount() >= targetCount)
        {
            respawnTimer = respawnInterval;
            return;
        }

        respawnTimer -= Time.deltaTime;
        if (respawnTimer > 0f) return;

        // Якщо вдалого місця не знайшли, спробуємо ще раз через секунду.
        respawnTimer = SpawnOne(true) ? respawnInterval : 1f;
    }

    private int AliveCount()
    {
        deer.RemoveAll(d => d == null || d.currentState == DeerAI.DeerState.Dead);
        return deer.Count;
    }

    private bool SpawnOne(bool outsideCamera)
    {
        if (!TryGetSpawnPoint(outsideCamera, out Vector2 point)) return false;

        DeerAI newDeer = Instantiate(deerPrefab, point, Quaternion.identity, transform);
        newDeer.player = player;
        deer.Add(newDeer);
        return true;
    }

    private bool TryGetSpawnPoint(bool outsideCamera, out Vector2 point)
    {
        Rect area = MapBounds.Instance.Rect;
        float xMin = area.xMin + edgeMargin;
        float xMax = area.xMax - edgeMargin;
        float yMin = area.yMin + edgeMargin;
        float yMax = area.yMax - edgeMargin;

        point = Vector2.zero;
        if (xMin >= xMax || yMin >= yMax)
        {
            Debug.LogWarning("AnimalSpawner: edgeMargin завеликий для цієї карти.", this);
            return false;
        }

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));

            if (player != null && Vector2.Distance(candidate, player.position) < minDistanceFromPlayer) continue;
            if (camp != null && Vector2.Distance(candidate, camp.position) < minDistanceFromCamp) continue;
            if (Physics2D.OverlapCircle(candidate, freeRadius, obstacleMask) != null) continue;
            if (outsideCamera && IsVisible(candidate)) continue;

            point = candidate;
            return true;
        }

        return false;
    }

    private bool IsVisible(Vector2 worldPoint)
    {
        if (viewCamera == null) return false;

        Vector3 v = viewCamera.WorldToViewportPoint(worldPoint);
        float p = offscreenPadding;
        return v.z > 0f && v.x > -p && v.x < 1f + p && v.y > -p && v.y < 1f + p;
    }

    private void OnDrawGizmosSelected()
    {
        if (MapBounds.Instance == null) return;

        Rect area = MapBounds.Instance.Rect;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(area.center, new Vector3(area.width - edgeMargin * 2f, area.height - edgeMargin * 2f, 0f));

        if (camp != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(camp.position, minDistanceFromCamp);
        }
    }
}