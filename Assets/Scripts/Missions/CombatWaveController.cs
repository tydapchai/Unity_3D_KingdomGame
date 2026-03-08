using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CombatWaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private Transform wave1SpawnPoints;
    [SerializeField] private Transform wave2SpawnPoints;
    [SerializeField] private GameObject enemyPrefab;

    private readonly HashSet<EnemyDeathNotifier> _aliveEnemies = new();
    private bool _combatTriggered;
    private int _activeWave;

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
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (!CanStartCombat())
        {
            return;
        }

        _combatTriggered = true;
        missionManager.StartMission3Combat();
        _activeWave = 1;
        SpawnWave(wave1SpawnPoints);
    }

    public void NotifyEnemyDeath(EnemyDeathNotifier notifier)
    {
        if (notifier == null)
        {
            return;
        }

        if (!_aliveEnemies.Remove(notifier))
        {
            return;
        }

        if (_aliveEnemies.Count > 0)
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

        if (missionManager.CurrentMission != MissionManager.MissionState.Mission3)
        {
            return;
        }

        if (_activeWave == 1)
        {
            _activeWave = 2;
            missionManager.SetMission3Wave(2);
            SpawnWave(wave2SpawnPoints);
            return;
        }

        if (_activeWave == 2)
        {
            missionManager.CompleteMission3Combat();
        }
    }

    private bool CanStartCombat()
    {
        if (_combatTriggered)
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

        if (missionManager.CurrentMission != MissionManager.MissionState.Mission3)
        {
            return false;
        }

        if (missionManager.Mission3CombatStarted || missionManager.Mission3Completed)
        {
            return false;
        }

        return enemyPrefab != null && wave1SpawnPoints != null && wave2SpawnPoints != null;
    }

    private void SpawnWave(Transform spawnRoot)
    {
        _aliveEnemies.Clear();

        List<Transform> spawnPoints = GetSpawnPoints(spawnRoot);
        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            enemy.transform.SetParent(transform);

            EnemyDeathNotifier notifier = enemy.GetComponent<EnemyDeathNotifier>();
            if (notifier == null)
            {
                notifier = enemy.AddComponent<EnemyDeathNotifier>();
            }

            notifier.SetOwner(this);
            _aliveEnemies.Add(notifier);
        }

        if (_aliveEnemies.Count == 0)
        {
            if (_activeWave == 1)
            {
                _activeWave = 2;
                missionManager.SetMission3Wave(2);
                SpawnWave(wave2SpawnPoints);
                return;
            }

            if (_activeWave == 2)
            {
                missionManager.CompleteMission3Combat();
            }
        }
    }

    private static List<Transform> GetSpawnPoints(Transform spawnRoot)
    {
        List<Transform> spawnPoints = new();
        if (spawnRoot == null)
        {
            return spawnPoints;
        }

        if (spawnRoot.childCount == 0)
        {
            spawnPoints.Add(spawnRoot);
            return spawnPoints;
        }

        for (int i = 0; i < spawnRoot.childCount; i++)
        {
            Transform child = spawnRoot.GetChild(i);
            if (child != null)
            {
                spawnPoints.Add(child);
            }
        }

        return spawnPoints;
    }
}
