using System.Collections.Generic;
using UnityEngine;

// Спавнер тварин (олені й кабани). Кладемо на порожній об'єкт Spawner у сцені.
// На старті розставляє тварин по карті, потім раз на respawnInterval
// досіває до потрібної кількості. Нових тварин ставить поза кадром камери.
// Потрібні: MapBounds у сцені і префаби з DeerAI та/або BoarAI.
// Префаб, якого не вказано, просто не спавниться.
public class AnimalSpawner : MonoBehaviour
{
    [Header("Олені")]
    [SerializeField] private DeerAI deerPrefab;
    [SerializeField] private int targetCount = 7;

    [Header("Кабани")]
    [SerializeField] private BoarAI boarPrefab;
    [SerializeField] private int boarTargetCount = 3;

    [Header("Загальне")]
    [Tooltip("Гравець. Його передаємо кожній тварині, бо префаб не може посилатися на об'єкт зі сцени.")]
    [SerializeField] private Transform player;
    [Tooltip("Через скільки секунд досіюється одна тварина (кожного виду окремо), якщо їх менше за потрібну кількість.")]
    [SerializeField] private float respawnInterval = 60f;

    [Header("Де можна з'являтись")]
    [Tooltip("Відступ від краю карти. Має бути трохи більшим за Alert Radius тварини (5 у оленя), щоб вона не ходила в стіну.")]
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
    private readonly List<BoarAI> boars = new List<BoarAI>();
    private float deerTimer;
    private float boarTimer;

    private void Start()
    {
        if (viewCamera == null) viewCamera = Camera.main;

        if (deerPrefab == null && boarPrefab == null)
        {
            Debug.LogError("AnimalSpawner: не вказано жодного префаба тварини.", this);
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
        if (deerPrefab != null)
        {
            for (int i = 0; i < targetCount; i++) SpawnDeer(false);
        }

        if (boarPrefab != null)
        {
            for (int i = 0; i < boarTargetCount; i++) SpawnBoar(false);
        }

        deerTimer = respawnInterval;
        boarTimer = respawnInterval;
    }

    private void Update()
    {
        if (deerPrefab != null)
        {
            TickRespawn(ref deerTimer, AliveDeerCount() < targetCount, () => SpawnDeer(true));
        }

        if (boarPrefab != null)
        {
            TickRespawn(ref boarTimer, AliveBoarCount() < boarTargetCount, () => SpawnBoar(true));
        }
    }

    private void TickRespawn(ref float timer, bool needMore, System.Func<bool> spawn)
    {
        if (!needMore)
        {
            timer = respawnInterval;
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        // Якщо вдалого місця не знайшли, спробуємо ще раз через секунду.
        timer = spawn() ? respawnInterval : 1f;
    }

    private int AliveDeerCount()
    {
        deer.RemoveAll(d => d == null || d.currentState == DeerAI.DeerState.Dead);
        return deer.Count;
    }

    private int AliveBoarCount()
    {
        boars.RemoveAll(b => b == null || b.currentState == BoarAI.BoarState.Dead);
        return boars.Count;
    }

    private bool SpawnDeer(bool outsideCamera)
    {
        if (!TryGetSpawnPoint(outsideCamera, out Vector2 point)) return false;

        DeerAI newDeer = Instantiate(deerPrefab, point, Quaternion.identity, transform);
        newDeer.player = player;
        deer.Add(newDeer);
        return true;
    }

    private bool SpawnBoar(bool outsideCamera)
    {
        if (!TryGetSpawnPoint(outsideCamera, out Vector2 point)) return false;

        BoarAI newBoar = Instantiate(boarPrefab, point, Quaternion.identity, transform);
        newBoar.player = player;
        boars.Add(newBoar);
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