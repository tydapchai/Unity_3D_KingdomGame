using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Quản lý vùng spawn quái. Gắn vào Empty GameObject đặt ở trung tâm vùng quái.
/// Tự động spawn quái lên NavMesh trong vùng chỉ định.
/// </summary>
public class EnemyZoneSpawner : MonoBehaviour
{
    [Header("=== ENEMY PREFAB ===")]
    [Tooltip("Prefab con quái (phải có NavMeshAgent + EnemyAI)")]
    public GameObject enemyPrefab;

    [Tooltip("Số lượng quái spawn")]
    public int enemyCount = 5;

    [Header("=== VÙNG SPAWN ===")]
    [Tooltip("Kích thước vùng spawn (X, Y không quan trọng, Z)")]
    public Vector3 spawnAreaSize = new Vector3(30f, 5f, 30f);

    [Header("=== WAYPOINTS ===")]
    [Tooltip("Kéo tất cả waypoints vào đây — sẽ chia sẻ cho mọi quái")]
    public Transform[] sharedWaypoints;

    [Header("=== GIỚI HẠN ĐUỔI ===")]
    [Tooltip("Quái đuổi player tối đa bao xa từ trung tâm vùng")]
    public float maxChaseDistance = 25f;

    [Header("=== NAV MESH ===")]
    [Tooltip("Area Mask cho NavMesh — quái chỉ đi trên area này. -1 = tất cả.")]
    public int navMeshAreaMask = -1;  // -1 = AllAreas, hoặc set area cụ thể

    void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemyZoneSpawner] Chưa gán Enemy Prefab!");
            return;
        }

        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        int spawned = 0;
        int maxAttempts = enemyCount * 10; // Tránh vòng lặp vô hạn
        int attempts = 0;

        while (spawned < enemyCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomPos = GetRandomPositionInZone();

            // Tìm điểm hợp lệ trên NavMesh gần vị trí random
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 10f, navMeshAreaMask))
            {
                // Kiểm tra điểm có nằm trong vùng spawn không
                if (IsInsideSpawnArea(hit.position))
                {
                    GameObject enemy = Instantiate(enemyPrefab, hit.position,
                        Quaternion.Euler(0, Random.Range(0, 360), 0), transform);
                    enemy.name = $"Enemy_{spawned:00}";

                    // Cấu hình EnemyAI
                    EnemyAI ai = enemy.GetComponent<EnemyAI>();
                    if (ai != null)
                    {
                        ai.waypoints = sharedWaypoints;
                        ai.zoneCenter = transform;
                        ai.maxChaseDistance = maxChaseDistance;
                    }

                    // Cấu hình NavMeshAgent area mask
                    NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
                    if (agent != null && navMeshAreaMask != -1)
                    {
                        agent.areaMask = navMeshAreaMask;
                    }

                    spawned++;
                }
            }
        }

        Debug.Log($"[EnemyZoneSpawner] Đã spawn {spawned}/{enemyCount} quái tại vùng '{gameObject.name}'");
    }

    Vector3 GetRandomPositionInZone()
    {
        Vector3 center = transform.position;
        float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float z = Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f);
        return center + new Vector3(x, 0, z);
    }

    bool IsInsideSpawnArea(Vector3 position)
    {
        Vector3 local = position - transform.position;
        return Mathf.Abs(local.x) <= spawnAreaSize.x / 2f &&
               Mathf.Abs(local.z) <= spawnAreaSize.z / 2f;
    }

    // Hiển thị vùng spawn trong Editor
    void OnDrawGizmos()
    {
        // Vùng spawn (đỏ mờ)
        Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
        Gizmos.DrawCube(transform.position + Vector3.up * spawnAreaSize.y / 2f, spawnAreaSize);
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * spawnAreaSize.y / 2f, spawnAreaSize);

        // Vùng đuổi tối đa (xanh dương mờ)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.08f);
        Gizmos.DrawWireSphere(transform.position, maxChaseDistance);

        // Waypoints
        if (sharedWaypoints != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
            foreach (var wp in sharedWaypoints)
            {
                if (wp != null)
                {
                    Gizmos.DrawSphere(wp.position, 0.4f);
                    Gizmos.DrawLine(transform.position, wp.position);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Khi chọn, hiện đậm hơn
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position + Vector3.up * spawnAreaSize.y / 2f, spawnAreaSize);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, maxChaseDistance);
    }
}
