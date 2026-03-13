using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumeSlider;

    private const string VOLUME_PREF_KEY = "MasterVolumePref";

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 0.75f);
        
        volumeSlider.value = savedVolume;
        
        SetVolume(savedVolume);

        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float sliderValue)
    {
        float volume = Mathf.Clamp(sliderValue, 0.0001f, 1f);

        float dbVolume = Mathf.Log10(volume) * 20;

        bool result = audioMixer.SetFloat("Audio", dbVolume);

        if (!result) Debug.LogError("Không tìm thấy tham số mang tên 'Audio' trong Mixer!");

        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, sliderValue);
    }
}