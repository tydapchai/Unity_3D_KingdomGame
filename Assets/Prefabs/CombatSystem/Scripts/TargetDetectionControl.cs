using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TargetDetectionControl : MonoBehaviour
{

    public static TargetDetectionControl instance;

    [Header("Components")]
    public PlayerControl playerControl;

    [Header("Scene")]
    public List<Transform> allTargetsInScene = new List<Transform>();
    
    [Space]
    [Header("Target Detection")]
    public LayerMask whatIsEnemy;
    public bool canChangeTarget = true;

    [Tooltip("Detection Range: \n Player range for detecting potential targets.")]
    [Range(0f, 15f)] public float detectionRange = 10f;

    [Tooltip("Dot Product Threshold \nHigher Values: More strict alignment required \nLower Values: Allows for broader targeting")]
    [Range(0f, 1f)] public float dotProductThreshold = 0.15f;

    [Space]
    [Header("Debug")]
    public bool debug;
    public Transform checkPos;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        PopulateTargetInScene();
        StartCoroutine(RunEveryXms());
    }

    private void PopulateTargetInScene()
    {
        // Find all active GameObjects in the scene
        EnemyBase[] allGameObjects = FindObjectsOfType<EnemyBase>();

        // Convert the array to a list
        List<EnemyBase> gameObjectList = new List<EnemyBase>(allGameObjects);

        // Output the number of GameObjects found
        if (debug)
            Debug.Log("Number of targets found: " + gameObjectList.Count);

        // Optionally, iterate over the list and do something with each GameObject
        foreach (EnemyBase obj in gameObjectList)
        {
            allTargetsInScene.Add(obj.transform);
        }
    }

    private IEnumerator RunEveryXms()
    {
        while (true)
        {
            yield return new WaitForSeconds(.1f); // Wait for 'x' milliseconds
            GetEnemyInInputDirection();
        }
    }

    #region Get Enemy In Input Direction

    public void GetEnemyInInputDirection()
    {
        if (canChangeTarget)
        {
            Vector3 inputDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;

            if (inputDirection != Vector3.zero)
            {
                inputDirection = Camera.main.transform.TransformDirection(inputDirection);
                inputDirection.y = 0;
                inputDirection.Normalize();

                Transform closestEnemy = GetClosestEnemyInDirection(inputDirection);

                // Vì khoảng cách đã được check ở hàm dưới, ở đây ta chỉ cần check null
                if (closestEnemy != null)
                {
                    playerControl.ChangeTarget(closestEnemy);
                    if (debug) Debug.Log("Targeting: " + closestEnemy.name);
                }
            }
        }
    }

    Transform GetClosestEnemyInDirection(Vector3 inputDirection)
    {
        Transform closestEnemy = null;
        float maxDotProduct = dotProductThreshold;

        // Dùng for ngược hoặc foreach đều được, ở đây giữ nguyên foreach cho bạn dễ nhìn
        foreach (Transform enemy in allTargetsInScene)
        {
            // 1. FIX LỖI QUÁI CHẾT: Bỏ qua nếu enemy đã bị destroy hoặc tắt SetActive
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            // 2. FIX LOGIC: Chỉ xét những con quái nằm trong tầm nhận diện (Detection Range)
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.position);
            if (distanceToEnemy > detectionRange) continue;

            // 3. FIX LỖI ĐỒI NÚI: Triệt tiêu trục Y để so sánh 2D ngang cho chuẩn
            Vector3 dirToEnemy = enemy.position - transform.position;
            dirToEnemy.y = 0;
            Vector3 enemyDirection = dirToEnemy.normalized;

            // Tính điểm Dot Product (1 = Nhìn thẳng mặt, 0 = Vuông góc, -1 = Xoay lưng lại)
            float dotProduct = Vector3.Dot(inputDirection, enemyDirection);

            // Tìm ra kẻ địch có hướng input "chuẩn" nhất
            if (dotProduct > maxDotProduct)
            {
                maxDotProduct = dotProduct;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    #endregion

    #region Unused Code/ Might Delete Later


    #endregion
}
