using UnityEngine;
using UnityEngine.UI; // Cần thiết để dùng UI (Image, Text)
// using TMPro; // BỎ COMMENT DÒNG NÀY NẾU BẠN DÙNG TEXT MESH PRO CHO CHỮ

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;

    [Header("Components")]
    public Animator anim;

    // Gọi script ThirdPersonController (hoặc script di chuyển của bạn) để tắt nó đi khi chết
    private StarterAssets.ThirdPersonController playerController;
    private Collider playerCollider;

    [Header("UI Health Bar")]
    public Image statBar;
    public Image statBarFade;
    public float fadeSpeed = 0.5f;

    [Header("UI Text")]
    // Nếu dùng TextMeshPro thì sửa chữ 'Text' thành 'TextMeshProUGUI'
    public Text currentValueText;
    public Text maxValueText;

    void Start()
    {
        currentHealth = maxHealth;

        if (anim == null) anim = GetComponent<Animator>();
        playerController = GetComponent<StarterAssets.ThirdPersonController>();
        playerCollider = GetComponent<Collider>(); // Thường là CharacterController hoặc CapsuleCollider

        // Khởi tạo UI đầy bình
        UpdateHealthBarInstant();
        UpdateHealthText();

        // Cài đặt số Max Health một lần lúc đầu
        if (maxValueText != null) maxValueText.text = maxHealth.ToString("0");
    }

    void Update()
    {
        // LOGIC LÀM MỜ THANH MÁU TỪ TỪ
        if (statBarFade != null && statBar != null)
        {
            if (statBarFade.fillAmount > statBar.fillAmount)
            {
                statBarFade.fillAmount -= fadeSpeed * Time.deltaTime;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("🔴 Player bị chém trúng! Mất " + damage + " máu. Còn lại: " + currentHealth);

        // Cập nhật UI ngay lập tức
        if (statBar != null) statBar.fillAmount = currentHealth / maxHealth;
        UpdateHealthText();

        // Kiểm tra xem máu đã hết chưa
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Chỉ giật lùi (Hit) nếu chưa chết, để tránh đè lên animation Die
            if (anim != null) anim.SetTrigger("Hit");
        }
    }

    private void UpdateHealthBarInstant()
    {
        if (statBar != null) statBar.fillAmount = currentHealth / maxHealth;
        if (statBarFade != null) statBarFade.fillAmount = currentHealth / maxHealth;
    }

    private void UpdateHealthText()
    {
        if (currentValueText != null)
        {
            // ".ToString("0")" giúp làm tròn số, ví dụ 95.5 thành 96 cho UI đẹp hơn
            currentValueText.text = currentHealth.ToString("0");
        }
    }

    void Die()
    {
        if (isDead) return; // Chống gọi hàm này nhiều lần
        isDead = true;
        Debug.Log("💀 GAME OVER! Player đã ngã xuống.");

        // 1. Chạy Animation Chết
        if (anim != null)
        {
            anim.SetBool("isDead", true); // Bật trạng thái chết
            anim.SetTrigger("Die");       // Dùng Trigger để chuyển sang clip chết ngay lập tức
        }

        // 2. Khóa di chuyển (Tránh lỗi nhân vật nằm dưới đất nhưng bạn bấm nút vẫn trượt đi)
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // 3. Tùy chọn: Tắt Collider để quái vật mất mục tiêu và không đấm vào cái xác nữa
        if (playerCollider != null)
        {
            // playerCollider.enabled = false; 

            // Đẩy Player xuống một Layer khác (VD: Layer "Default" thay vì "Player") 
            // để quả cầu OverlapSphere của quái quét xuyên qua luôn.
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }
}