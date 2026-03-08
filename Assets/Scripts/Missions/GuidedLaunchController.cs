using System.Collections;
using GinjaGaming.FinalCharacterController;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GuidedLaunchController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private Transform landingTarget;
    [SerializeField] private Collider landingConfirmTrigger;

    [Header("Guided Launch")]
    [SerializeField] private float launchDuration = 1.2f;
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private float arrivalThreshold = 0.5f;

    private CharacterController _playerCharacterController;
    private PlayerController _playerController;
    private PlayerLocomotionInput _playerLocomotionInput;
    private Transform _playerTransform;
    private bool _isLaunching;
    private bool _reachedCorrectLandingTarget;

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
    }

    private void Update()
    {
        if (!_reachedCorrectLandingTarget)
        {
            return;
        }

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

        if (missionManager.Mission2LaunchCompleted || landingConfirmTrigger == null || _playerTransform == null)
        {
            return;
        }

        if (!IsPlayerInsideConfirmTrigger())
        {
            return;
        }

        missionManager.RegisterMission2LaunchCompleted();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (!CanStartLaunch())
        {
            return;
        }

        CachePlayerReferences(other);
        if (_playerTransform == null || _playerCharacterController == null)
        {
            return;
        }

        StartCoroutine(PerformGuidedLaunch());
    }

    private bool CanStartLaunch()
    {
        if (_isLaunching)
        {
            return false;
        }

        if (missionManager == null)
        {
            missionManager = MissionManager.Instance;
        }

        if (missionManager == null || !missionManager.MissionStarted)
        {
            return false;
        }

        if (missionManager.CurrentMission != MissionManager.MissionState.Mission2)
        {
            return false;
        }

        if (!missionManager.Mission2LeverActivated || missionManager.Mission2LaunchCompleted)
        {
            return false;
        }

        return landingTarget != null;
    }

    private void CachePlayerReferences(Collider playerCollider)
    {
        _playerTransform = playerCollider.transform;
        _playerCharacterController = playerCollider.GetComponent<CharacterController>();
        _playerController = playerCollider.GetComponent<PlayerController>();
        _playerLocomotionInput = playerCollider.GetComponent<PlayerLocomotionInput>();
    }

    private IEnumerator PerformGuidedLaunch()
    {
        _isLaunching = true;
        _reachedCorrectLandingTarget = false;

        SetPlayerControlEnabled(false);

        bool hadCharacterController = _playerCharacterController != null;
        if (hadCharacterController)
        {
            _playerCharacterController.enabled = false;
        }

        Vector3 startPosition = _playerTransform.position;
        Vector3 targetPosition = landingTarget.position;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, launchDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 horizontalPosition = Vector3.Lerp(startPosition, targetPosition, t);
            float arcOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            Vector3 desiredPosition = horizontalPosition + Vector3.up * arcOffset;
            _playerTransform.position = desiredPosition;

            Vector3 lookDirection = targetPosition - _playerTransform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                _playerTransform.rotation = Quaternion.LookRotation(lookDirection.normalized);
            }

            yield return null;
        }

        _playerTransform.position = targetPosition;

        if (hadCharacterController)
        {
            _playerCharacterController.enabled = true;
        }

        _reachedCorrectLandingTarget = Vector3.Distance(_playerTransform.position, targetPosition) <= Mathf.Max(0.01f, arrivalThreshold);
        _isLaunching = false;
        SetPlayerControlEnabled(true);
    }

    private bool IsPlayerInsideConfirmTrigger()
    {
        Vector3 closestPoint = landingConfirmTrigger.ClosestPoint(_playerTransform.position);
        return Vector3.SqrMagnitude(closestPoint - _playerTransform.position) <= 0.0001f;
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        if (_playerController != null)
        {
            _playerController.enabled = enabled;
        }

        if (_playerLocomotionInput != null)
        {
            _playerLocomotionInput.enabled = enabled;
        }
    }
}
