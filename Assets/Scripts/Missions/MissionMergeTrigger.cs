using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MissionMergeTrigger : MonoBehaviour
{
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private bool disableAfterComplete = true;

    private bool _completed;

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

    private void OnTriggerEnter(Collider other)
    {
        if (_completed || !other.CompareTag("Player"))
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

        if (missionManager.CurrentMission != MissionManager.MissionState.Mission1)
        {
            return;
        }

        if (!missionManager.Mission1FragmentCollected)
        {
            return;
        }

        missionManager.CompleteMission1();
        _completed = true;

        if (disableAfterComplete)
        {
            gameObject.SetActive(false);
        }
    }
}
