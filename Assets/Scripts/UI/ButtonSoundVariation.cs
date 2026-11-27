using UnityEngine;

public class SoundButton : MonoBehaviour
{
    [Header("Sonido")]
    public AudioSource audioSource;
    public AudioClip buttonSound;

    [Header("Variación de tono")]
    [Range(0.5f, 1.5f)]
    public float minPitch = 0.92f;

    [Range(0.5f, 1.5f)]
    public float maxPitch = 1.08f;

    [Header("Variación de volumen")]
    [Range(0f, 1f)]
    public float minVolume = 0.90f;

    [Range(0f, 1f)]
    public float maxVolume = 1f;

    public void PlayButtonSound()
    {
        if (audioSource == null || buttonSound == null)
            return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        float volume = Random.Range(minVolume, maxVolume);

        audioSource.PlayOneShot(buttonSound, volume);
    }
}
