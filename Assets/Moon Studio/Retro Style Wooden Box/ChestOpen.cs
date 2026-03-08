using UnityEngine;

public class ChestOpen : MonoBehaviour
{
    [Header("Chest Models")]
    public GameObject closedChest;
    public GameObject openedChest;

    private void Start()
    {
        Debug.Log("=== Script ChestOpen đã khởi chạy trên object: " + gameObject.name + " ===");
        if (closedChest != null) closedChest.SetActive(true);
        if (openedChest != null) openedChest.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OpenChest();
        }
        else
        {
            Debug.Log("-> Vật chạm vào không có tag Player. Bỏ qua.");
        }
    }

    private void OpenChest()
    {
        if (closedChest != null && openedChest != null)
        {
            closedChest.SetActive(false);
            openedChest.SetActive(true);
        }
        else
        {
            Debug.LogError("-> LỖI: Bạn chưa gán đủ model cho closedChest hoặc openedChest trong Inspector!");
        }
    }
}