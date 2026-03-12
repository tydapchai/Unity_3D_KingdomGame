using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using StarterAssets;

public class PlayerControl : MonoBehaviour
{
    [Space]
    [Header("Components")]
    [SerializeField] private Animator anim;
    [SerializeField] private ThirdPersonController thirdPersonController;
    // [SerializeField] private GameControl gameControl;

    [Space]
    [Header("Combat")]
    public Transform target;
    [SerializeField] private Transform attackPos;
    [Tooltip("Offset Stoping Distance")][SerializeField] private float quickAttackDeltaDistance;
    [Tooltip("Offset Stoping Distance")][SerializeField] private float heavyAttackDeltaDistance;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float airknockbackForce = 10f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float reachTime = 0.3f;
    [SerializeField] private LayerMask enemyLayer;
    bool isAttacking = false;

    [Space]
    [Header("Audio & Sound")]
    public AudioSource audioSource;
    public AudioClip[] swingSounds; // Tiếng vung vũ khí (Vút, xé gió)
    public AudioClip[] hitSounds;   // Tiếng chém trúng địch (Phập, Keng)

    [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;

    [Space]
    [Header("Debug")]
    [SerializeField] private bool debug;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }

        if ((Vector3.Distance(transform.position, target.position) >= TargetDetectionControl.instance.detectionRange))
        {
            NoTarget();
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Attack(0);
        }

        if (Input.GetMouseButtonDown(1))
        {
            Attack(1);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack(0);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Attack(1);
        }

    }

    #region Attack, PerformAttack, Reset Attack, Change Target


    public void Attack(int attackState)
    {
        if (isAttacking)
        {
            return;
        }

        thirdPersonController.canMove = false;
        TargetDetectionControl.instance.canChangeTarget = false;
        RandomAttackAnim(attackState);

    }

    private void RandomAttackAnim(int attackState)
    {
        switch (attackState)
        {
            case 0: //Quick Attack
                QuickAttack();
                break;

            case 1:
                HeavyAttack();
                break;
        }
    }

    void QuickAttack()
    {
        int attackIndex = Random.Range(1, 4);
        if (debug)
        {
            Debug.Log(attackIndex + " attack index");
        }

        switch (attackIndex)
        {
            case 1: //punch
                if (target != null)
                {
                    MoveTowardsTarget(target.position, quickAttackDeltaDistance, "punch");
                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                    TargetDetectionControl.instance.canChangeTarget = true;
                }
                break;

            case 2: //kick
                if (target != null)
                {
                    MoveTowardsTarget(target.position, quickAttackDeltaDistance, "kick");
                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                    TargetDetectionControl.instance.canChangeTarget = true;
                }
                break;

            case 3: //mmakick
                if (target != null)
                {
                    MoveTowardsTarget(target.position, quickAttackDeltaDistance, "mmakick");
                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                    TargetDetectionControl.instance.canChangeTarget = true;
                }
                break;
        }
    }

    void HeavyAttack()
    {
        int attackIndex = Random.Range(1, 3);
        if (debug)
        {
            Debug.Log(attackIndex + " attack index");
        }

        switch (attackIndex)
        {
            case 1: //heavyAttack1
                if (target != null)
                {
                    FaceThis(target.position);
                    anim.SetBool("heavyAttack1", true);
                    isAttacking = true;
                }
                else
                {
                    TargetDetectionControl.instance.canChangeTarget = true;
                    thirdPersonController.canMove = true;
                }
                break;

            case 2: //heavyAttack2
                if (target != null)
                {
                    FaceThis(target.position);
                    anim.SetBool("heavyAttack2", true);
                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                    TargetDetectionControl.instance.canChangeTarget = true;
                }
                break;
        }
    }

    public void ResetAttack() // Animation Event ---- for Reset Attack
    {
        anim.SetBool("punch", false);
        anim.SetBool("kick", false);
        anim.SetBool("mmakick", false);
        anim.SetBool("heavyAttack1", false);
        anim.SetBool("heavyAttack2", false);
        thirdPersonController.canMove = true;
        TargetDetectionControl.instance.canChangeTarget = true;
        isAttacking = false;
    }

    public void PerformAttack() // Animation Event ---- for Attacking Targets
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPos.position, attackRange, enemyLayer);
        bool hasHitSomeone = false;
        HashSet<EnemyBase> processedEnemies = new HashSet<EnemyBase>();

        foreach (Collider enemy in hitEnemies)
        {
            EnemyBase enemyBase = enemy.GetComponentInParent<EnemyBase>();
            if (enemyBase == null || enemyBase.isDead || !processedEnemies.Add(enemyBase))
            {
                continue;
            }

            EnemyAI enemyAI = enemyBase.GetComponent<EnemyAI>();
            Rigidbody enemyRb = enemy.attachedRigidbody != null
                ? enemy.attachedRigidbody
                : enemyBase.GetComponent<Rigidbody>();

            // 1. Xử lý Trừ máu và Sinh hiệu ứng máu (Chỉ chạy khi quái có script EnemyBase)
            enemyBase.TakeDamage(25f, enemyAI == null); // Trừ máu
            enemyBase.SpawnHitVfx(enemyBase.transform.position); // Hiện VFX văng máu
            hasHitSomeone = true; // Ghi nhận là đã chém trúng thịt

            // 2. Enemy có AI dùng pipeline stagger riêng để không bị NavMeshAgent + Rigidbody giành transform
            if (enemyAI != null)
            {
                enemyAI.TakeHit(transform.position, knockbackForce);
            }
            // 3. Fallback cho target dummy không có AI: giữ AddForce như cũ
            else if (enemyRb != null)
            {
                // Tính toán hướng đẩy lùi
                Vector3 knockbackDirection = enemyBase.transform.position - transform.position;
                knockbackDirection.y = airknockbackForce; // Giữ lực đẩy ngang/chếch lên

                // Tác dụng lực đẩy lên quái
                enemyRb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode.Impulse);
            }
        }

        // 3. Phát âm thanh khi chém trúng mục tiêu
        if (hasHitSomeone && hitSounds.Length > 0 && audioSource != null)
        {
            AudioClip hitClip = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(hitClip);
        }
    }

    public void PlaySwingSound() // Animation Event ---- for Swinging Weapon
    {
        if (swingSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = swingSounds[Random.Range(0, swingSounds.Length)];
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip);
        }
    }

    private EnemyBase oldTarget;
    private EnemyBase currentTarget;
    public void ChangeTarget(Transform target_)
    {

        if (target != null)
        {
            oldTarget.ActiveTarget(false);
        }

        target = target_;

        oldTarget = target_.GetComponent<EnemyBase>(); //set current target
        currentTarget = target_.GetComponent<EnemyBase>();
        currentTarget.ActiveTarget(true);

    }

    private void NoTarget() // When player gets out of range of current Target
    {
        currentTarget.ActiveTarget(false);
        currentTarget = null;
        oldTarget = null;
        target = null;
    }

    #endregion


    #region MoveTowards, Target Offset and FaceThis

    public void MoveTowardsTarget(Vector3 target_, float deltaDistance, string animationName_)
    {
        PerformAttackAnimation(animationName_);
        FaceThis(target_);

        // Tính toán tọa độ sẽ lướt tới
        Vector3 finalPos = TargetOffset(target_, deltaDistance);

        // FIX LỖI ĐỘN THỔ: Lấy độ cao thực tế của mặt đất tại điểm đến
        float terrainHeight = Terrain.activeTerrain.SampleHeight(finalPos)
                            + Terrain.activeTerrain.transform.position.y;

        finalPos.y = terrainHeight; // Ép nhân vật đứng trên mặt đất

        // Thực hiện lướt tới
        transform.DOMove(finalPos, reachTime);
    }

    public void GetClose() // Animation Event ---- for Moving Close to Target
    {
        Vector3 getCloseTarget;

        if (target == null && oldTarget != null)
        {
            getCloseTarget = oldTarget.transform.position;
        }
        else if (target != null)
        {
            getCloseTarget = target.position;
        }
        else
        {
            return;
        }

        FaceThis(getCloseTarget);

        // Tính toán tọa độ sẽ lướt tới
        Vector3 finalPos = TargetOffset(getCloseTarget, 1.4f);

        // FIX LỖI ĐỘN THỔ: Lấy độ cao thực tế của mặt đất
        float terrainHeight = Terrain.activeTerrain.SampleHeight(finalPos)
                            + Terrain.activeTerrain.transform.position.y;

        finalPos.y = terrainHeight;

        // Thực hiện lướt tới (nhanh hơn MoveTowardsTarget một chút)
        transform.DOMove(finalPos, 0.2f);
    }

    void PerformAttackAnimation(string animationName_)
    {
        anim.SetBool(animationName_, true);
    }

    public Vector3 TargetOffset(Vector3 target, float deltaDistance)
    {
        Vector3 position = target;
        return Vector3.MoveTowards(position, transform.position, deltaDistance);
    }

    public void FaceThis(Vector3 target)
    {
        // FIX LỖI XOAY MẶT TRÊN DỐC: Ép trục Y của target ngang bằng với trục Y của Player
        // Giúp nhân vật không bị chúi đầu xuống đất hoặc ngửa cổ lên trời khi nhìn quái trên đồi
        Vector3 target_ = new Vector3(target.x, transform.position.y, target.z);

        Quaternion lookAtRotation = Quaternion.LookRotation(target_ - transform.position);
        lookAtRotation.x = 0;
        lookAtRotation.z = 0;
        transform.DOLocalRotateQuaternion(lookAtRotation, 0.2f);
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange); // Visualize the attack range
    }
}
