using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum AIState { Idle, Patrol, Chase, Attack, Staggered }

    [Header("AI State")]
    public AIState currentState;

    [Header("References")]
    public Transform playerTarget;
    public NavMeshAgent agent;
    public Animator anim;
    private EnemyBase enemyBase;

    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float chaseSpeed = 5.335f;
    public float speedSmoothTime = 0.1f;
    private float _currentAnimSpeed;
    private float _speedVelocity;

    [Header("Ranges")]
    public float detectionRange = 10f;
    public float attackRange = 2.5f;
    public float patrolRadius = 8f;

    [Header("Combat & Souls Mechanics")]
    public float attackCooldown = 2f;
    private float lastAttackTime;
    private bool isAttacking = false;
    public Transform attackPoint;
    public float attackHitRadius = 1.2f;
    public float playerDamage = 15f;
    public float playerKnockbackForce = 6f;
    public float playerKnockbackDuration = 0.25f;
    public float attackHitDelay = 0.55f;
    public float attackAnimationDuration = 1.5f;

    [Tooltip("Số đòn đánh chịu được trước khi bị khựng (Stagger)")]
    public int maxPoise = 3;
    private int currentPoise;
    public float staggerDuration = 0.5f; // Thời gian khựng
    private bool hasHitThisSwing;

    private Vector3 patrolDestination;
    private float idleTimer;
    public float waitTimeAtPoint = 2f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        enemyBase = GetComponent<EnemyBase>();
        currentPoise = maxPoise;

        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player").transform;

        if (attackPoint == null)
            attackPoint = transform;

        agent.speed = chaseSpeed;
        agent.acceleration = 8f;
        agent.stoppingDistance = attackRange - 0.5f;

        currentState = AIState.Idle;
    }

    void Update()
    {
        if (enemyBase != null && enemyBase.isDead)
        {
            agent.isStopped = true;
            return;
        }

        // Nếu đang bị khựng thì không làm gì cả
        if (currentState == AIState.Staggered) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (isAttacking)
        {
            UpdateAnimation(0);
            return;
        }

        switch (currentState)
        {
            case AIState.Idle: IdleState(distanceToPlayer); break;
            case AIState.Patrol: PatrolState(distanceToPlayer); break;
            case AIState.Chase: ChaseState(distanceToPlayer); break;
            case AIState.Attack: AttackState(distanceToPlayer); break;
        }

        UpdateAnimation(agent.velocity.magnitude);
    }

    void UpdateAnimation(float targetSpeed)
    {
        _currentAnimSpeed = Mathf.SmoothDamp(_currentAnimSpeed, targetSpeed, ref _speedVelocity, speedSmoothTime);
        anim.SetFloat("Speed", _currentAnimSpeed);
    }

    // --- CƠ CHẾ KNOCKBACK & STAGGER CHUẨN SOULS ---
    public void TakeHit(Vector3 attackerPos, float knockbackForce)
    {
        if ((enemyBase != null && enemyBase.isDead) || currentState == AIState.Staggered)
        {
            return;
        }

        currentPoise--;

        if (currentPoise <= 0)
        {
            // Bị phá thế (Stagger)
            currentPoise = maxPoise; // Reset lại poise
            StartCoroutine(KnockbackRoutine(attackerPos, knockbackForce));
        }
        else
        {
            // Chỉ phát hiệu ứng bị thương nhẹ (nếu bạn có animation GetHit nhẹ)
            anim.SetTrigger("GetHit");
        }
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 attackerPos, float force)
    {
        currentState = AIState.Staggered;
        isAttacking = false;
        agent.isStopped = true;

        // Tính hướng đẩy lùi (từ Player hướng về Enemy)
        Vector3 knockbackDir = (transform.position - attackerPos).normalized;
        knockbackDir.y = 0;

        // Quay mặt về phía người chơi
        transform.rotation = Quaternion.LookRotation(-knockbackDir);

        anim.SetTrigger("Stagger");

        // MÔ PHỎNG RIGIDBODY IMPULSE
        // Chúng ta đặt vận tốc ban đầu rất lớn rồi giảm dần theo thời gian
        float currentForce = force * 1.5f; // Nhân hệ số để cú đánh có sức nặng
        float friction = 10f;             // Độ ma sát (càng cao dừng càng nhanh)

        while (currentForce > 0.1f)
        {
            // Gán vận tốc trực tiếp cho NavMeshAgent
            agent.velocity = knockbackDir * currentForce;

            // Giảm dần lực theo thời gian (Deceleration)
            currentForce = Mathf.Lerp(currentForce, 0, Time.deltaTime * friction);

            yield return null;
        }

        // Dừng hẳn trước khi trả lại quyền điều khiển cho AI
        agent.velocity = Vector3.zero;
        yield return new WaitForSeconds(0.1f);

        agent.isStopped = false;
        currentState = AIState.Chase;
    }

    #region AI Logic States (Giữ nguyên các hàm cũ của bạn nhưng tối ưu hóa một chút)
    void IdleState(float distanceToPlayer)
    {
        agent.isStopped = true;
        if (distanceToPlayer <= detectionRange) { currentState = AIState.Chase; return; }
        idleTimer += Time.deltaTime;
        if (idleTimer >= waitTimeAtPoint) { SearchNewPatrolPoint(); currentState = AIState.Patrol; idleTimer = 0f; }
    }

    void PatrolState(float distanceToPlayer)
    {
        agent.isStopped = false;
        agent.speed = walkSpeed;
        if (distanceToPlayer <= detectionRange) { currentState = AIState.Chase; return; }
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) currentState = AIState.Idle;
    }

    void ChaseState(float distanceToPlayer)
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(playerTarget.position);
        if (distanceToPlayer <= attackRange) currentState = AIState.Attack;
        else if (distanceToPlayer > detectionRange + 2f) currentState = AIState.Idle;
    }

    void AttackState(float distanceToPlayer)
    {
        agent.isStopped = true;
        Vector3 dir = (playerTarget.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);

        if (distanceToPlayer > attackRange + 0.5f) { isAttacking = false; currentState = AIState.Chase; return; }

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(PlayAttackAnimation());
            lastAttackTime = Time.time;
        }
    }

    System.Collections.IEnumerator PlayAttackAnimation()
    {
        isAttacking = true;
        hasHitThisSwing = false;
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackHitDelay);
        if (currentState != AIState.Staggered && (enemyBase == null || !enemyBase.isDead))
        {
            DealDamageToPlayer();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackAnimationDuration - attackHitDelay));
        isAttacking = false;
    }

    public void DealDamageToPlayer()
    {
        if (hasHitThisSwing)
        {
            return;
        }

        Vector3 hitOrigin = attackPoint != null
            ? attackPoint.position
            : transform.position + transform.forward * Mathf.Max(attackRange * 0.6f, 0.8f);

        Collider[] hitPlayers = Physics.OverlapSphere(hitOrigin, attackHitRadius);
        foreach (Collider hitPlayer in hitPlayers)
        {
            PlayerHitReceiver receiver = hitPlayer.GetComponentInParent<PlayerHitReceiver>();
            if (receiver == null || receiver.isDead)
            {
                continue;
            }

            receiver.TakeHit(transform.position, playerDamage, playerKnockbackForce, playerKnockbackDuration);
            hasHitThisSwing = true;
            break;
        }
    }

    void SearchNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            agent.SetDestination(hit.position);
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 hitOrigin = attackPoint != null
            ? attackPoint.position
            : transform.position + transform.forward * Mathf.Max(attackRange * 0.6f, 0.8f);
        Gizmos.DrawWireSphere(hitOrigin, attackHitRadius);
    }
}
