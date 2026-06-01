using UnityEngine;
using UnityEngine.UI;

public class SoundSettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void OnEnable()
    {
        if (SoundManager.instance == null) return;

        // Postavljamo slider pozicije na trenutne vrednosti bez okidanja OnValueChanged
        musicSlider.SetValueWithoutNotify(SoundManager.instance.MusicVolume);
        sfxSlider.SetValueWithoutNotify(SoundManager.instance.SFXVolume);
    }

    public void OnMusicVolumeChanged(float value) => SoundManager.instance.SetMusicVolume(value);
    public void OnSFXVolumeChanged(float value)   => SoundManager.instance.SetSFXVolume(value);
}
