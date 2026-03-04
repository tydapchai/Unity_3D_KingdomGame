using UnityEngine;

namespace Unity.FantasyKingdom
{
    public class PickupItem : MonoBehaviour
    {
        [Header("Cài đặt vật phẩm")]
        public string itemName;

        [Header("Đối tượng hiển thị (Đồ mới)")]
        public GameObject playerTargetItem;

        [Header("Danh sách đối tượng cần ẩn")]
        public GameObject[] bodyPartsToHide; // Sử dụng mảng để ẩn nhiều món cùng lúc

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // 1. Hiện món đồ mới
                if (playerTargetItem != null)
                {
                    playerTargetItem.SetActive(true);
                    Debug.Log("<color=lime>Đã mặc: </color>" + playerTargetItem.name);
                }

                // 2. Vòng lặp để ẩn tất cả các đối tượng trong danh sách
                if (bodyPartsToHide != null && bodyPartsToHide.Length > 0)
                {
                    foreach (GameObject part in bodyPartsToHide)
                    {
                        if (part != null)
                        {
                            part.SetActive(false);
                            Debug.Log("<color=red>Đã ẩn: </color>" + part.name);
                        }
                    }
                }

                // 3. Tiêu hủy món đồ dưới đất
                Destroy(gameObject);
            }
        }
    }
}