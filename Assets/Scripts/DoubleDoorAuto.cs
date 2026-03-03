using System.Collections;
using UnityEngine;

public class DoubleDoorAuto : MonoBehaviour
{
    [Header("Cài đặt Cửa Đôi")]
    [Tooltip("Kéo cục Cha của Cánh Trái vào đây")]
    public Transform leftDoor;
    [Tooltip("Kéo cục Cha của Cánh Phải vào đây")]
    public Transform rightDoor;

    [Header("Thông số Xoay")]
    [Tooltip("Góc khi cửa đóng (thường là 0)")]
    public float closedAngle = 0f;
    [Tooltip("Góc khi cửa mở (cánh trái sẽ quay -90, cánh phải quay +90)")]
    public float openAngle = 90f;
    public float speed = 3f;

    private Coroutine leftCoroutine;
    private Coroutine rightCoroutine;

    // Khi nhân vật đi VÀO vùng cảm biến -> MỞ CỬA
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Dừng hành động cũ (nếu cửa đang đóng dở)
            if (leftCoroutine != null) StopCoroutine(leftCoroutine);
            if (rightCoroutine != null) StopCoroutine(rightCoroutine);

            // Xoay cánh trái ngược chiều (-openAngle), cánh phải thuận chiều (openAngle)
            leftCoroutine = StartCoroutine(RotateDoor(leftDoor, -openAngle));
            rightCoroutine = StartCoroutine(RotateDoor(rightDoor, openAngle));
        }
    }

    // Khi nhân vật đi RA KHỎI vùng cảm biến -> ĐÓNG CỬA
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (leftCoroutine != null) StopCoroutine(leftCoroutine);
            if (rightCoroutine != null) StopCoroutine(rightCoroutine);

            // Gọi cả 2 cánh quay về góc 0
            leftCoroutine = StartCoroutine(RotateDoor(leftDoor, closedAngle));
            rightCoroutine = StartCoroutine(RotateDoor(rightDoor, closedAngle));
        }
    }

    // Hàm thực hiện việc xoay mượt mà (Slerp)
    private IEnumerator RotateDoor(Transform door, float targetYAngle)
    {
        if (door == null) yield break;

        Quaternion targetRotation = Quaternion.Euler(door.localEulerAngles.x, targetYAngle, door.localEulerAngles.z);

        while (Quaternion.Angle(door.localRotation, targetRotation) > 0.1f)
        {
            door.localRotation = Quaternion.Slerp(door.localRotation, targetRotation, Time.deltaTime * speed);
            yield return null;
        }

        door.localRotation = targetRotation; // Đảm bảo đóng khít
    }
}