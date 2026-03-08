using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class LeverInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private Transform leverHandle;
    [SerializeField] private GameObject magicPadPrefab;
    [SerializeField] private Transform magicPadSpawnPoint;

    [Header("Lever")]
    [SerializeField] private float activatedXAngle = -45f;
    [SerializeField] private bool disableAfterUse = true;

    private Quaternion _initialLeverRotation;
    private bool _playerInside;
    private bool _activated;
    private GameObject _spawnedMagicPad;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance;
        }

        if (leverHandle == null)
        {
            leverHandle = transform;
        }

        _initialLeverRotation = leverHandle.localRotation;

        if (magicPadSpawnPoint == null)
        {
            magicPadSpawnPoint = transform;
        }
    }

    private void Update()
    {
        if (_activated || !_playerInside)
        {
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        TryActivateLever();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _playerInside = false;
    }

    private void TryActivateLever()
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance;
        }

        if (missionManager == null || !missionManager.MissionStarted)
        {
            return;
        }

        if (missionManager.CurrentMission != MissionManager.MissionState.Mission2)
        {
            return;
        }

        if (!missionManager.Mission2EnergyCoreCollected)
        {
            return;
        }

        if (missionManager.Mission2LeverActivated)
        {
            return;
        }

        _activated = true;
        leverHandle.localRotation = Quaternion.Euler(activatedXAngle, _initialLeverRotation.eulerAngles.y, _initialLeverRotation.eulerAngles.z);
        SpawnMagicPad();
        missionManager.RegisterMission2LeverActivated();

        if (disableAfterUse)
        {
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }
        }
    }

    private void SpawnMagicPad()
    {
        if (magicPadPrefab == null || _spawnedMagicPad != null)
        {
            return;
        }

        Transform spawnPoint = magicPadSpawnPoint != null ? magicPadSpawnPoint : transform;
        _spawnedMagicPad = Instantiate(magicPadPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
