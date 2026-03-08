using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    public enum MissionState
    {
        Mission1,
        Mission2,
        Mission3,
        Completed
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI objectiveText;

    [Header("Mission 1")]
    [SerializeField] private GameObject m1PedestalVfx;
    [SerializeField] private GameObject m1Fragment;
    [SerializeField] private int mission1RequiredSigils = 3;

    [Header("Mission 2")]
    [SerializeField] private GameObject m2EnergyCore;
    [SerializeField] private GameObject m2Fragment;

    [Header("Mission 3")]
    [SerializeField] private GameObject m3BarrierBlockers;
    [SerializeField] private GameObject m3Fragment;

    [Header("Debug")]
    [SerializeField] private bool autoStartOnPlay;

    [SerializeField] private MissionState currentMission = MissionState.Mission1;

    private readonly HashSet<string> _collectedSigils = new();
    private bool missionStarted;
    private bool mission1FragmentRevealed;
    private bool mission1FragmentCollected;
    private bool mission2EnergyCoreCollected;
    private bool mission2LeverActivated;
    private bool mission2LaunchCompleted;
    private bool mission2FragmentCollected;
    private int mission3CurrentWave;
    private bool mission3CombatStarted;
    private bool mission3Completed;

    public MissionState CurrentMission => currentMission;
    public bool MissionStarted => missionStarted;
    public int Mission1CollectedSigils => _collectedSigils.Count;
    public bool Mission1FragmentCollected => mission1FragmentCollected;
    public bool Mission2EnergyCoreCollected => mission2EnergyCoreCollected;
    public bool Mission2LeverActivated => mission2LeverActivated;
    public bool Mission2LaunchCompleted => mission2LaunchCompleted;
    public int Mission3CurrentWave => mission3CurrentWave;
    public bool Mission3CombatStarted => mission3CombatStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        AutoAssignSceneReferences();
        ApplyInitialSceneState();
        RefreshObjectiveText();
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartMission();
        }
    }

    public void StartMission()
    {
        if (missionStarted)
        {
            return;
        }

        missionStarted = true;
        currentMission = MissionState.Mission1;
        RefreshObjectiveText();
    }

    public void RegisterMission1Sigil(string sigilId)
    {
        if (!missionStarted || currentMission != MissionState.Mission1)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sigilId))
        {
            sigilId = $"Sigil_{_collectedSigils.Count + 1}";
        }

        if (!_collectedSigils.Add(sigilId))
        {
            return;
        }

        if (_collectedSigils.Count >= mission1RequiredSigils)
        {
            RevealMission1Fragment();
        }

        RefreshObjectiveText();
    }

    public void RegisterMission1FragmentCollected()
    {
        if (!missionStarted || currentMission != MissionState.Mission1)
        {
            return;
        }

        if (!mission1FragmentRevealed || mission1FragmentCollected)
        {
            return;
        }

        mission1FragmentCollected = true;
        RefreshObjectiveText();
    }

    public void CompleteMission1()
    {
        if (!missionStarted || currentMission != MissionState.Mission1)
        {
            return;
        }

        if (!mission1FragmentCollected)
        {
            return;
        }

        currentMission = MissionState.Mission2;
        RefreshObjectiveText();
    }

    public void RegisterMission2EnergyCoreCollected()
    {
        if (!missionStarted || currentMission != MissionState.Mission2 || mission2EnergyCoreCollected)
        {
            return;
        }

        mission2EnergyCoreCollected = true;
        RefreshObjectiveText();
    }

    public void RegisterMission2LeverActivated()
    {
        if (!missionStarted || currentMission != MissionState.Mission2 || !mission2EnergyCoreCollected)
        {
            return;
        }

        if (mission2LeverActivated)
        {
            return;
        }

        mission2LeverActivated = true;
        RefreshObjectiveText();
    }

    public void RegisterMission2LaunchCompleted()
    {
        if (!missionStarted || currentMission != MissionState.Mission2 || !mission2LeverActivated)
        {
            return;
        }

        if (mission2LaunchCompleted)
        {
            return;
        }

        mission2LaunchCompleted = true;

        if (m2Fragment != null)
        {
            m2Fragment.SetActive(true);
        }

        RefreshObjectiveText();
    }

    public void RegisterMission2FragmentCollected()
    {
        if (!missionStarted || currentMission != MissionState.Mission2 || !mission2LaunchCompleted)
        {
            return;
        }

        if (mission2FragmentCollected)
        {
            return;
        }

        mission2FragmentCollected = true;
        currentMission = MissionState.Mission3;
        RefreshObjectiveText();
    }

    public void StartMission3Combat()
    {
        if (!missionStarted || currentMission != MissionState.Mission3)
        {
            return;
        }

        if (mission3CombatStarted)
        {
            return;
        }

        mission3CombatStarted = true;
        mission3CurrentWave = 1;

        if (m3BarrierBlockers != null)
        {
            m3BarrierBlockers.SetActive(true);
        }

        RefreshObjectiveText();
    }

    public void SetMission3Wave(int waveNumber)
    {
        if (!missionStarted || currentMission != MissionState.Mission3 || !mission3CombatStarted)
        {
            return;
        }

        mission3CurrentWave = Mathf.Clamp(waveNumber, 1, 2);
        RefreshObjectiveText();
    }

    public void CompleteMission3Combat()
    {
        if (!missionStarted || currentMission != MissionState.Mission3 || !mission3CombatStarted)
        {
            return;
        }

        mission3Completed = true;
        mission3CurrentWave = 2;

        if (m3BarrierBlockers != null)
        {
            m3BarrierBlockers.SetActive(false);
        }

        if (m3Fragment != null)
        {
            m3Fragment.SetActive(true);
        }

        RefreshObjectiveText();
    }

    public void RegisterMission3FragmentCollected()
    {
        if (!missionStarted || currentMission != MissionState.Mission3 || !mission3Completed)
        {
            return;
        }

        currentMission = MissionState.Completed;
        RefreshObjectiveText();
    }

    private void RevealMission1Fragment()
    {
        if (mission1FragmentRevealed)
        {
            return;
        }

        mission1FragmentRevealed = true;

        if (m1PedestalVfx != null)
        {
            m1PedestalVfx.SetActive(true);
        }

        if (m1Fragment != null)
        {
            m1Fragment.SetActive(true);
        }
    }

    private void ApplyInitialSceneState()
    {
        if (m1PedestalVfx != null)
        {
            m1PedestalVfx.SetActive(false);
        }

        if (m1Fragment != null)
        {
            m1Fragment.SetActive(false);
        }

        if (m2Fragment != null)
        {
            m2Fragment.SetActive(false);
        }

        if (m3BarrierBlockers != null)
        {
            m3BarrierBlockers.SetActive(false);
        }

        if (m3Fragment != null)
        {
            m3Fragment.SetActive(false);
        }
    }

    private void AutoAssignSceneReferences()
    {
        if (objectiveText == null)
        {
            GameObject objectiveObject = GameObject.Find("ObjectiveText");
            if (objectiveObject != null)
            {
                objectiveText = objectiveObject.GetComponent<TextMeshProUGUI>();
            }
        }

        if (m1PedestalVfx == null) m1PedestalVfx = FindSceneObject("M1_Pedestal_VFX");
        if (m1Fragment == null) m1Fragment = FindSceneObject("M1_Fragment");
        if (m2EnergyCore == null) m2EnergyCore = FindSceneObject("M2_EnergyCore");
        if (m2Fragment == null) m2Fragment = FindSceneObject("M2_Fragment");
        if (m3BarrierBlockers == null) m3BarrierBlockers = FindSceneObject("M3_Barrier_Blockers");
        if (m3Fragment == null) m3Fragment = FindSceneObject("M3_Fragment");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found;
    }

    private void RefreshObjectiveText()
    {
        if (objectiveText == null)
        {
            return;
        }

        objectiveText.text = BuildObjectiveText();
    }

    private string BuildObjectiveText()
    {
        if (!missionStarted)
        {
            return string.Empty;
        }

        switch (currentMission)
        {
            case MissionState.Mission1:
                if (_collectedSigils.Count < mission1RequiredSigils)
                {
                    return $"M1: Tìm 3 Ấn Tín Phố Cổ ({_collectedSigils.Count}/{mission1RequiredSigils})";
                }

                return "M1 done: Mang mảnh ghép về điểm hợp nhất";

            case MissionState.Mission2:
                if (!mission2EnergyCoreCollected)
                {
                    return "M2: Nhặt Lõi Năng Lượng";
                }

                if (!mission2LeverActivated)
                {
                    return "M2 step2: Gạt cần để kích hoạt bệ ma pháp";
                }

                return "M2 step3: Leo lên và lấy Mảnh Ghép Can Đảm";

            case MissionState.Mission3:
                if (!mission3CombatStarted)
                {
                    return "M3: Đánh bại quân canh bóng tối - Wave 1/2";
                }

                if (!mission3Completed)
                {
                    return $"M3: Đánh bại quân canh bóng tối - Wave {Mathf.Clamp(mission3CurrentWave, 1, 2)}/2";
                }

                return "M3 done: Nhặt Mảnh Ghép Sức Mạnh";

            case MissionState.Completed:
                return "Hoàn thành tuyến nhiệm vụ";

            default:
                return string.Empty;
        }
    }
}
