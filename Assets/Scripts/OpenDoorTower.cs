using System.Collections;
using UnityEngine;

public class OpenDoorTower : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("Kéo thả vật thể Cua_Luoi từ Hierarchy vào đây")]
    public Transform doorTransform; 
    
    [Tooltip("Độ cao cửa sẽ kéo lên (theo trục Y)")]
    public float openDistance = 5f; 
    
    [Tooltip("Tốc độ đóng/mở cửa")]
    public float speed = 2f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine moveCoroutine; // Biến dùng để kiểm soát Coroutine hiện tại

    void Start()
    {
        // Lưu lại vị trí đóng và tính toán sẵn vị trí mở ngay từ đầu
        if (doorTransform != null)
        {
            closedPosition = doorTransform.localPosition;
            openPosition = closedPosition + Vector3.up * openDistance;
        }
        else
        {
            Debug.LogWarning("Bạn chưa gán Cua_Luoi vào script OpenDoorTower!");
        }
    }

    // Khi nhân vật ĐI VÀO vùng cảm biến -> MỞ CỬA
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && doorTransform != null)
        {
            // Nếu cửa đang đóng dở, dừng việc đóng lại và bắt đầu mở
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveDoor(openPosition));
        }
    }

    // Khi nhân vật ĐI RA khỏi vùng cảm biến -> ĐÓNG CỬA
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && doorTransform != null)
        {
            // Nếu cửa đang mở dở, dừng việc mở lại và bắt đầu đóng
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveDoor(closedPosition));
        }
    }

    // Coroutine dùng chung cho cả hành động Mở và Đóng
    private IEnumerator MoveDoor(Vector3 targetPosition)
    {
        // Chạy vòng lặp cho đến khi cửa gần tới đích
        while (Vector3.Distance(doorTransform.localPosition, targetPosition) > 0.01f)
        {
            doorTransform.localPosition = Vector3.Lerp(doorTransform.localPosition, targetPosition, Time.deltaTime * speed);
            yield return null; 
        }
        
        // Đảm bảo cửa nằm chính xác ở vị trí đích khi kết thúc
        doorTransform.localPosition = targetPosition;
    }
}