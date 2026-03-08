using UnityEngine;

public class ChestController : MonoBehaviour
{
    [Header("Chest Models")]
    public GameObject closedChest; // Kéo thả Wooden_Box_Merge vào đây
    public GameObject openedChest; // Kéo thả Wooden_Box vào đây

    private void Start()
    {
        // Đảm bảo trạng thái ban đầu khi chạy game: rương đóng hiện, rương mở ẩn
        if (closedChest != null) closedChest.SetActive(true);
        if (openedChest != null) openedChest.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem đối tượng chạm vào rương có phải là nhân vật không
        // Đảm bảo nhân vật của bạn đã được gán Tag là "Player"
        if (other.CompareTag("Player"))
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        if (closedChest != null && openedChest != null)
        {
            closedChest.SetActive(false); // Ẩn rương đóng
            openedChest.SetActive(true);  // Hiện rương mở
        }
    }
}