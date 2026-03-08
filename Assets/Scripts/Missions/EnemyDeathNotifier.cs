using UnityEngine;

public class EnemyDeathNotifier : MonoBehaviour
{
    [SerializeField] private CombatWaveController owner;

    private bool _notified;

    public void SetOwner(CombatWaveController combatWaveController)
    {
        owner = combatWaveController;
    }

    private void OnDisable()
    {
        NotifyOwner();
    }

    private void OnDestroy()
    {
        NotifyOwner();
    }

    private void NotifyOwner()
    {
        if (_notified || owner == null)
        {
            return;
        }

        _notified = true;
        owner.NotifyEnemyDeath(this);
    }
}
