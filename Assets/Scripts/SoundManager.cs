using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundFXSource;

    public float MusicVolume => musicSource.volume;
    public float SFXVolume   => soundFXSource.volume;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void PlaySoundFX(AudioClip[] clips, float volumeScale)
    {
        int index = Random.Range(0, clips.Length);
        soundFXSource.PlayOneShot(clips[index], volumeScale);
    }

    public void PlayMusic(AudioClip clip, float volume)
    {
        musicSource.volume = volume;
        musicSource.clip   = clip;
        musicSource.Play();
    }

    public void SetMusicVolume(float volume) => musicSource.volume = volume;
    public void SetSFXVolume(float volume)   => soundFXSource.volume = volume;
    public void StopSFX()                    => soundFXSource.Stop();
}
