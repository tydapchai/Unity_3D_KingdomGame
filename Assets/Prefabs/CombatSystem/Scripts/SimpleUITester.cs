using UnityEngine;
using UnityEngine.UI;

public class SimpleUITester : MonoBehaviour
{
    [Header("UI References (Kéo từ UIStat sang)")]
    public Image statBar;
    public Image statBarFade;
    public Text currentValueText;

    [Header("Settings")]
    public float maxHealth = 100f;
    public float damageAmount = 10f; // Đổi tên cho chuẩn nghĩa là lượng sát thương

    [Tooltip("Tốc độ tụt của thanh mờ. Nên để 1 hoặc 2 cho nhanh")]
    public float fadeSpeed = 0.5f;

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;

        if (statBar != null) statBar.fillAmount = 1f;
        if (statBarFade != null) statBarFade.fillAmount = 1f;

        UpdateText();
        Debug.Log("🟢 Khởi tạo UI thành công. Máu hiện tại: " + currentHealth);
    }

    void Update()
    {
        // 1. Logic làm mờ thanh máu (Luôn chạy mỗi frame)
        if (statBarFade != null && statBar != null)
        {
            if (statBarFade.fillAmount > statBar.fillAmount)
            {
                // Ép thanh mờ tụt xuống bằng với thanh chính
                statBarFade.fillAmount -= fadeSpeed * Time.deltaTime;
            }
        }

        // 2. Nhận phím Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("🟡 Đã bấm phím Space!");
            TakeDamage(damageAmount);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Chặn không cho máu âm

        Debug.Log("🔴 Bị trừ " + damage + " máu! Máu còn lại: " + currentHealth);

        // Tụt thanh chính ngay lập tức
        if (statBar != null)
        {
            statBar.fillAmount = currentHealth / maxHealth;
        }

        UpdateText();
    }

    void UpdateText()
    {
        if (currentValueText != null)
        {
            currentValueText.text = currentHealth.ToString("0") + " / " + maxHealth.ToString("0");
        }
    }
}