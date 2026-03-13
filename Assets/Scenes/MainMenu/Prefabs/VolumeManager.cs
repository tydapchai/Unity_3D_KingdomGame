using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumeSlider;

    private const string VOLUME_PREF_KEY = "MasterVolumePref";

    void Awake()
    {
        if (volumeSlider == null)
        {
            volumeSlider = FindFirstObjectByType<Slider>(FindObjectsInactive.Include);
        }
    }

    void Start()
    {
        if (volumeSlider == null)
        {
            Debug.LogError($"{nameof(VolumeManager)} on {name} could not find a Slider reference.", this);
            return;
        }

        if (audioMixer == null)
        {
            Debug.LogError($"{nameof(VolumeManager)} on {name} is missing an AudioMixer reference.", this);
            return;
        }

        float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 0.75f);

        volumeSlider.SetValueWithoutNotify(savedVolume);
        SetVolume(savedVolume);
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
        }
    }

    public void SetVolume(float sliderValue)
    {
        if (sliderValue <= 0.0001f) 
        {
            audioMixer.SetFloat("Audio", -80f);
        }
        else 
        {
            float dbVolume = Mathf.Log10(sliderValue) * 20;
            audioMixer.SetFloat("Audio", dbVolume);
        }

        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, sliderValue);
    }
}