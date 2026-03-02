using System.Collections;
using UnityEngine;

public class OpenDoorTower : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("Kéo thả vật thể Cua_Luoi từ Hierarchy vào đây")]
    public Transform doorTransform; 
    
    [Tooltip("Độ cao cửa sẽ kéo lên (theo trục Y)")]
    public float openDistance = 5f; 
    
    [Tooltip("Tốc độ mở cửa")]
    public float speed = 2f;

    private Vector3 closedPosition;
    private bool isOpened = false;

    void Start()
    {
        // Lưu lại vị trí ban đầu của cửa khi game bắt đầu
        if (doorTransform != null)
        {
            closedPosition = doorTransform.localPosition;
        }
        else
        {
            Debug.LogWarning("Bạn chưa gán Cua_Luoi vào script OpenDoorTower!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem thứ bước vào có phải là nhân vật của bạn không (dựa vào Tag)
        if (other.CompareTag("Player"))
        {
            OpenDoor(); // Gọi lệnh mở cửa!
        }
    }

    // Hàm này sẽ được gọi khi bạn muốn mở cửa (ví dụ: nhân vật đi vào vùng trigger)
    public void OpenDoor()
    {
        if (!isOpened && doorTransform != null)
        {
            // Tính toán vị trí đích (vị trí cũ + dịch lên theo trục Y)
            Vector3 targetPosition = closedPosition + Vector3.up * openDistance;
            
            // Chạy Coroutine để di chuyển cửa mượt mà qua từng frame
            StartCoroutine(MoveDoor(targetPosition));
            isOpened = true;
        }
    }

    // Coroutine xử lý animation di chuyển
    private IEnumerator MoveDoor(Vector3 target)
    {
        // Chạy vòng lặp cho đến khi cửa gần tới đích
        while (Vector3.Distance(doorTransform.localPosition, target) > 0.01f)
        {
            // Vector3.Lerp giúp chuyển động mượt: nhanh ở đầu và chậm dần về cuối
            doorTransform.localPosition = Vector3.Lerp(doorTransform.localPosition, target, Time.deltaTime * speed);
            
            // Tạm dừng ở frame hiện tại và tiếp tục vòng lặp ở frame tiếp theo
            yield return null; 
        }
        
        // Đảm bảo cửa nằm chính xác ở vị trí đích khi kết thúc
        doorTransform.localPosition = target;
    }
}