using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Health System")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;

    [Header("Components")]
    public Animator anim;
    public Collider enemyCollider;

    [Header("VFX & UI")]
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private GameObject activeTargetObject;
    private EnemyAI enemyAI;

    // Start is called before the first frame update
    void Start()
    {
        ActiveTarget(false);

        // Khởi tạo máu đầy khi mới xuất hiện
        currentHealth = maxHealth;

        // Tự động tìm Component nếu bạn quên kéo thả trong Inspector
        if (anim == null) anim = GetComponent<Animator>();
        if (enemyCollider == null) enemyCollider = GetComponent<Collider>();
        enemyAI = GetComponent<EnemyAI>();
    }

    // Hàm nhận sát thương (Player sẽ gọi hàm này khi chém trúng)
    public void TakeDamage(float damageAmount, bool playDefaultHitReaction = true)
    {
        if (isDead) return; // Nếu chết rồi thì bỏ qua

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + " nhận " + damageAmount + " sát thương! Máu còn: " + currentHealth);

        // Enemy có AI sẽ dùng hit reaction riêng trong EnemyAI để tránh animation chồng lên stagger/knockback.
        if (playDefaultHitReaction && enemyAI == null && anim != null)
        {
            anim.SetTrigger("Hit");
        }

        // Kiểm tra xem máu đã hết chưa
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Hàm xử lý khi quái chết
    void Die()
    {
        isDead = true;
        Debug.Log(gameObject.name + " đã bị tiêu diệt!");

        // 1. Chạy Animation gục ngã (nhớ tạo parameter bool "isDead" trong Animator của quái nhé)
        if (anim != null)
        {
            anim.SetBool("isDead", true);
        }

        // 2. Tắt Collider để Player không chém trúng cái xác nữa
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // 3. Tắt luôn vòng tròn Target
        ActiveTarget(false);

        // 4. Hủy object sau 3 giây để dọn rác (bạn có thể chỉnh số 3f này)
        Destroy(gameObject, 3f);
    }

    public void SpawnHitVfx(Vector3 Pos_)
    {
        if (hitVfx != null)
        {
            Instantiate(hitVfx, Pos_, Quaternion.identity);
        }
    }

    public void ActiveTarget(bool bool_)
    {
        if (activeTargetObject != null)
        {
            activeTargetObject.SetActive(bool_);
        }
    }
}
