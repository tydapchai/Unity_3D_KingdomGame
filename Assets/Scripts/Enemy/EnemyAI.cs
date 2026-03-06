using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// AI cho quái vật: Tuần tra waypoints → Phát hiện player → Đuổi theo → Tấn công → Quay về vùng.
/// Quái chỉ hoạt động trong vùng giới hạn (dùng NavMesh Area).
/// </summary>
public class EnemyAI : MonoBehaviour
{
    // ============================================================
    // CẤU HÌNH TUẦN TRA
    // ============================================================
    [Header("=== TUẦN TRA ===")]
    [Tooltip("Các điểm tuần tra - quái sẽ đi lần lượt/ngẫu nhiên giữa các điểm này")]
    public Transform[] waypoints;

    [Tooltip("Tốc độ đi tuần")]
    public float patrolSpeed = 2f;

    [Tooltip("Thời gian đứng nghỉ ở mỗi waypoint (giây)")]
    public float waitTimeAtWaypoint = 2f;

    [Tooltip("true = đi ngẫu nhiên giữa các waypoint, false = đi theo thứ tự")]
    public bool randomPatrol = true;

    // ============================================================
    // CẤU HÌNH CHIẾN ĐẤU
    // ============================================================
    [Header("=== PHÁT HIỆN & TẤN CÔNG ===")]
    [Tooltip("Bán kính phát hiện player (vòng tròn vàng trong Gizmo)")]
    public float detectRange = 12f;

    [Tooltip("Bán kính tấn công (vòng tròn đỏ trong Gizmo)")]
    public float attackRange = 2.5f;

    [Tooltip("Tốc độ đuổi player")]
    public float chaseSpeed = 5f;

    [Tooltip("Thời gian hồi giữa các đòn đánh (giây)")]
    public float attackCooldown = 1.5f;

    [Tooltip("Sát thương mỗi đòn đánh")]
    public float attackDamage = 10f;

    // ============================================================
    // CẤU HÌNH VÙNG GIỚI HẠN
    // ============================================================
    [Header("=== GIỚI HẠN VÙNG ===")]
    [Tooltip("Trung tâm vùng quái hoạt động (thường là EnemyZone GameObject)")]
    public Transform zoneCenter;

    [Tooltip("Quái đuổi player tối đa bao xa từ zoneCenter rồi sẽ quay về")]
    public float maxChaseDistance = 25f;

    // ============================================================
    // THAM CHIẾU
    // ============================================================
    [Header("=== THAM CHIẾU ===")]
    [Tooltip("Animator của con quái (kéo thả hoặc tự tìm)")]
    public Animator animator;

    // ============================================================
    // BIẾN NỘI BỘ
    // ============================================================
    private NavMeshAgent _agent;
    private Transform _player;
    private int _currentWaypointIndex = 0;
    private float _lastAttackTime = -999f;
    private bool _isWaiting = false;
    private Vector3 _spawnPosition;

    private enum EnemyState { Patrol, Chase, Attack, ReturnToZone }
    private EnemyState _currentState = EnemyState.Patrol;

    // ============================================================
    // KHỞI TẠO
    // ============================================================
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = patrolSpeed;
        _spawnPosition = transform.position;

        // Tự tìm Animator nếu chưa gán
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Tìm player bằng nhiều cách
        FindPlayer();

        // Nếu chưa gán zoneCenter, lấy vị trí spawn làm center
        if (zoneCenter == null)
        {
            Debug.LogWarning($"[{gameObject.name}] zoneCenter chưa gán! Dùng vị trí spawn làm center.");
        }

        // Bắt đầu tuần tra
        if (waypoints != null && waypoints.Length > 0)
            GoToNextWaypoint();
    }

    /// <summary>
    /// Tìm player qua class name ThirdPersonController hoặc PlayerController, hoặc tag "Player"
    /// </summary>
    void FindPlayer()
    {
        // Cách 1: Tìm PlayerController (component chính điều khiển nhân vật)
        var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in allMonoBehaviours)
        {
            string typeName = mb.GetType().Name;
            if (typeName == "PlayerController" || typeName == "ThirdPersonController")
            {
                _player = mb.transform;
                Debug.Log($"[{gameObject.name}] Tìm thấy Player qua component: {typeName}");
                return;
            }
        }

        // Cách 2: Tìm qua Tag
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
        {
            _player = tagged.transform;
            Debug.Log($"[{gameObject.name}] Tìm thấy Player qua tag 'Player'");
            return;
        }

        Debug.LogError($"[{gameObject.name}] KHÔNG tìm thấy Player! Hãy đảm bảo player có component PlayerController hoặc tag 'Player'.");
    }

    // ============================================================
    // VÒNG LẶP CHÍNH
    // ============================================================
    void Update()
    {
        if (_player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, _player.position);
        float distToZone = GetDistanceToZone();

        switch (_currentState)
        {
            case EnemyState.Patrol:
                HandlePatrol(distToPlayer);
                break;

            case EnemyState.Chase:
                HandleChase(distToPlayer, distToZone);
                break;

            case EnemyState.Attack:
                HandleAttack(distToPlayer, distToZone);
                break;

            case EnemyState.ReturnToZone:
                HandleReturnToZone(distToPlayer);
                break;
        }

        UpdateAnimator();
    }

    float GetDistanceToZone()
    {
        Vector3 center = zoneCenter != null ? zoneCenter.position : _spawnPosition;
        return Vector3.Distance(transform.position, center);
    }

    // ============================================================
    // STATE: TUẦN TRA
    // ============================================================
    void HandlePatrol(float distToPlayer)
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (_isWaiting) return;

        // Phát hiện player → chuyển sang đuổi
        if (distToPlayer <= detectRange)
        {
            SwitchState(EnemyState.Chase);
            return;
        }

        // Đã đến waypoint → đứng nghỉ rồi đi tiếp
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f)
        {
            StartCoroutine(WaitAtWaypoint());
        }
    }

    IEnumerator WaitAtWaypoint()
    {
        _isWaiting = true;
        _agent.isStopped = true;

        // Đứng nghỉ random thời gian
        float waitTime = Random.Range(waitTimeAtWaypoint * 0.5f, waitTimeAtWaypoint * 1.5f);
        yield return new WaitForSeconds(waitTime);

        _agent.isStopped = false;
        GoToNextWaypoint();
        _isWaiting = false;
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (randomPatrol)
        {
            int newIndex;
            int attempts = 0;
            do
            {
                newIndex = Random.Range(0, waypoints.Length);
                attempts++;
            } while (newIndex == _currentWaypointIndex && waypoints.Length > 1 && attempts < 10);
            _currentWaypointIndex = newIndex;
        }
        else
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        }

        _agent.speed = patrolSpeed;
        _agent.isStopped = false;

        if (waypoints[_currentWaypointIndex] != null)
            _agent.SetDestination(waypoints[_currentWaypointIndex].position);
    }

    // ============================================================
    // STATE: ĐUỔI THEO
    // ============================================================
    void HandleChase(float distToPlayer, float distToZone)
    {
        // Quá xa vùng → quay về
        if (distToZone > maxChaseDistance)
        {
            SwitchState(EnemyState.ReturnToZone);
            return;
        }

        // Player chạy ra ngoài tầm detect (thêm 20% buffer tránh giật)
        if (distToPlayer > detectRange * 1.3f)
        {
            SwitchState(EnemyState.ReturnToZone);
            return;
        }

        // Đủ gần → tấn công
        if (distToPlayer <= attackRange)
        {
            SwitchState(EnemyState.Attack);
            return;
        }

        // Đuổi theo player
        _agent.speed = chaseSpeed;
        _agent.isStopped = false;
        _agent.SetDestination(_player.position);
    }

    // ============================================================
    // STATE: TẤN CÔNG
    // ============================================================
    void HandleAttack(float distToPlayer, float distToZone)
    {
        // Quay mặt về phía player
        Vector3 lookDir = _player.position - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
        }

        _agent.isStopped = true;

        // Player chạy ra khỏi tầm đánh
        if (distToPlayer > attackRange * 1.4f)
        {
            SwitchState(EnemyState.Chase);
            return;
        }

        // Quá xa vùng → bỏ cuộc
        if (distToZone > maxChaseDistance)
        {
            SwitchState(EnemyState.ReturnToZone);
            return;
        }

        // Thực hiện đánh theo cooldown
        if (Time.time - _lastAttackTime >= attackCooldown)
        {
            _lastAttackTime = Time.time;
            PerformAttack();
        }
    }

    void PerformAttack()
    {
        // Trigger animation tấn công
        if (animator != null)
            animator.SetTrigger("Attack");

        // TODO: Gây sát thương cho player
        // Bạn có thể implement PlayerHealth component và gọi:
        // var health = _player.GetComponent<PlayerHealth>();
        // if (health != null) health.TakeDamage(attackDamage);

        Debug.Log($"<color=red>[{gameObject.name}] ĐÃ ĐÁNH Player! Damage: {attackDamage}</color>");
    }

    // ============================================================
    // STATE: QUAY VỀ VÙNG
    // ============================================================
    void HandleReturnToZone(float distToPlayer)
    {
        _agent.speed = patrolSpeed;
        _agent.isStopped = false;

        // Nếu đang quay về mà player lại đến gần → đuổi tiếp (nếu còn trong vùng)
        float distToZone = GetDistanceToZone();
        if (distToPlayer <= detectRange && distToZone <= maxChaseDistance)
        {
            SwitchState(EnemyState.Chase);
            return;
        }

        // Đi về waypoint gần nhất
        Transform nearest = GetNearestWaypoint();
        if (nearest != null && _agent.destination != nearest.position)
        {
            _agent.SetDestination(nearest.position);
        }

        // Đã về đến nơi
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.5f)
        {
            SwitchState(EnemyState.Patrol);
        }
    }

    Transform GetNearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var wp in waypoints)
        {
            if (wp == null) continue;
            float d = Vector3.Distance(transform.position, wp.position);
            if (d < minDist) { minDist = d; nearest = wp; }
        }
        return nearest;
    }

    // ============================================================
    // CHUYỂN STATE
    // ============================================================
    void SwitchState(EnemyState newState)
    {
        // Debug.Log($"[{gameObject.name}] {_currentState} → {newState}");
        _currentState = newState;

        switch (newState)
        {
            case EnemyState.Patrol:
                _agent.isStopped = false;
                GoToNextWaypoint();
                break;
            case EnemyState.Chase:
                _agent.isStopped = false;
                break;
            case EnemyState.Attack:
                _agent.isStopped = true;
                break;
            case EnemyState.ReturnToZone:
                _agent.isStopped = false;
                break;
        }
    }

    // ============================================================
    // ANIMATOR
    // ============================================================
    void UpdateAnimator()
    {
        if (animator == null) return;

        // Speed parameter cho Idle/Walk/Run blend
        float speed = _agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        // Bool cho trạng thái tấn công
        animator.SetBool("IsAttacking", _currentState == EnemyState.Attack);

        // Bool cho trạng thái đuổi
        animator.SetBool("IsChasing", _currentState == EnemyState.Chase);
    }

    // ============================================================
    // GIZMOS (Hiển thị trong Scene View để debug)
    // ============================================================
    void OnDrawGizmosSelected()
    {
        // Tầm phát hiện (vàng)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Tầm tấn công (đỏ)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Giới hạn vùng (xanh dương)
        Vector3 center = zoneCenter != null ? zoneCenter.position : transform.position;
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(center, maxChaseDistance);

        // Đường đến waypoints (xanh lá)
        if (waypoints != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.5f);

                // Vẽ đường nối giữa các waypoint
                if (i > 0 && waypoints[i - 1] != null)
                    Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
            }
            // Nối waypoint cuối với waypoint đầu
            if (waypoints.Length > 1 && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
                Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }
    }
}
