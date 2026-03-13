using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Animator anim;
    public GameObject swordModel; // Kéo cây kiếm trên tay Player vào đây

    [Header("Settings")]
    public bool isEquipped = false;
    public float transitionSpeed = 5f; // Tốc độ chuyển dáng đứng (càng cao càng nhanh)

    private float combatWeight = 0f;

    void Update()
    {
        // 1. Nhấn phím R để rút hoặc cất kiếm
        if (Input.GetKeyDown(KeyCode.R))
        {
            isEquipped = !isEquipped;

            // Hiện hoặc ẩn model cây kiếm
            if (swordModel != null) swordModel.SetActive(isEquipped);
        }

        // 2. Tính toán giá trị CombatMode (0 sang 1 hoặc ngược lại)
        float targetWeight = isEquipped ? 1f : 0f;

        // Dùng Mathf.Lerp để giá trị chạy mượt mà (tạo hiệu ứng chuyển dáng đứng từ từ)
        combatWeight = Mathf.Lerp(combatWeight, targetWeight, Time.deltaTime * transitionSpeed);

        // 3. Gửi giá trị vào Blend Tree
        // LƯU Ý: Chữ "CombatMode" phải viết ĐÚNG TỪNG CHỮ với Parameter trong Animator
        anim.SetFloat("CombatMode", combatWeight);
    }
}