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
    private PlayerHealth playerHealthScript; // Dùng để kiểm tra Player còn sống không

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

    [Tooltip("Số đòn đánh chịu được trước khi bị khựng (Stagger)")]
    public int maxPoise = 3;
    private int currentPoise;
    public float staggerDuration = 0.5f;

    [Header("Hit Reaction")]
    [Range(0f, 1f)] public float lightHitKnockbackMultiplier = 0.35f;
    public float lightHitRecoveryTime = 0.08f;
    public float lightHitFriction = 18f;
    public float staggerKnockbackMultiplier = 1.5f;
    public float staggerFriction = 10f;

    [Header("Enemy Attack Settings")]
    public float attackDamage = 15f;
    public Transform attackPos;
    public float attackRadius = 1.2f;
    public LayerMask playerLayer;

    private Vector3 patrolDestination;
    private float idleTimer;
    public float waitTimeAtPoint = 2f;
    private Coroutine knockbackRoutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        enemyBase = GetComponent<EnemyBase>();
        maxPoise = Mathf.Max(1, maxPoise);
        currentPoise = maxPoise;

        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player").transform;

        // Tìm script máu trên người Player để theo dõi
        if (playerTarget != null)
            playerHealthScript = playerTarget.GetComponent<PlayerHealth>();

        agent.speed = chaseSpeed;
        agent.acceleration = 8f;
        agent.stoppingDistance = attackRange - 0.5f;

        currentState = AIState.Idle;
    }

    void Update()
    {
        // 1. Kiểm tra nếu quái chết
        if (enemyBase != null && enemyBase.isDead)
        {
            agent.isStopped = true;
            return;
        }

        // 2. KIỂM TRA NẾU PLAYER CHẾT -> NGỪNG AI
        if (playerHealthScript != null && playerHealthScript.isDead)
        {
            agent.isStopped = true;
            isAttacking = false;
            currentState = AIState.Idle;
            UpdateAnimation(0); // Cho quái về dáng đứng im
            return; // Thoát luôn Update, không làm gì thêm
        }

        // 3. Nếu đang bị khựng thì không làm gì cả
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

    public void TakeHit(Vector3 attackerPos, float knockbackForce)
    {
        if (enemyBase != null && enemyBase.isDead) return;
        if (currentState == AIState.Staggered) return;

        isAttacking = false;
        currentPoise--;

        if (currentPoise <= 0)
        {
            currentPoise = maxPoise;
            StartKnockback(attackerPos, knockbackForce * staggerKnockbackMultiplier, staggerFriction, staggerDuration, "Stagger", true);
        }
        else
        {
            if (lightHitKnockbackMultiplier <= 0f)
            {
                if (anim != null) anim.SetTrigger("GetHit");
                return;
            }
            StartKnockback(attackerPos, knockbackForce * lightHitKnockbackMultiplier, lightHitFriction, lightHitRecoveryTime, "GetHit", false);
        }
    }

    private void StartKnockback(Vector3 attackerPos, float force, float friction, float recoveryTime, string animationTrigger, bool faceAttacker)
    {
        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(KnockbackRoutine(attackerPos, force, friction, recoveryTime, animationTrigger, faceAttacker));
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 attackerPos, float force, float friction, float recoveryTime, string animationTrigger, bool faceAttacker)
    {
        currentState = AIState.Staggered;
        isAttacking = false;
        agent.isStopped = true;
        agent.ResetPath();

        Vector3 knockbackDir = transform.position - attackerPos;
        knockbackDir.y = 0;
        if (knockbackDir.sqrMagnitude <= 0.001f) knockbackDir = -transform.forward;
        knockbackDir.Normalize();

        if (faceAttacker) transform.rotation = Quaternion.LookRotation(-knockbackDir);

        if (anim != null && !string.IsNullOrEmpty(animationTrigger)) anim.SetTrigger(animationTrigger);

        float currentForce = force;
        float damp = Mathf.Max(0.01f, friction);

        while (currentForce > 0.1f)
        {
            agent.Move(knockbackDir * currentForce * Time.deltaTime);
            currentForce = Mathf.Lerp(currentForce, 0, Time.deltaTime * damp);
            yield return null;
        }

        agent.velocity = Vector3.zero;
        if (enemyBase != null && enemyBase.isDead) { knockbackRoutine = null; yield break; }
        if (recoveryTime > 0f) yield return new WaitForSeconds(recoveryTime);

        agent.isStopped = false;
        agent.nextPosition = transform.position;
        currentState = AIState.Chase;
        knockbackRoutine = null;
    }

    #region AI Logic States
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
        anim.SetTrigger("Attack");
        yield return new WaitForSeconds(1.5f);
        isAttacking = false;
    }

    void SearchNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            agent.SetDestination(hit.position);
    }
    #endregion

    public void PerformEnemyAttack()
    {
        if (!isAttacking || currentState == AIState.Staggered) return;
        if (attackPos == null) return;

        Collider[] hitPlayers = Physics.OverlapSphere(attackPos.position, attackRadius, playerLayer);
        foreach (Collider playerCol in hitPlayers)
        {
            PlayerHealth playerStats = playerCol.GetComponent<PlayerHealth>();
            if (playerStats != null && !playerStats.isDead)
            {
                playerStats.TakeDamage(attackDamage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPos != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPos.position, attackRadius);
        }
    }
}