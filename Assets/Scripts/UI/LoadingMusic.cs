using UnityEngine;
using System.Collections;

public class LoadingMusic : MonoBehaviour
{
    public AudioSource audioSource;

    [Range(0f, 1f)]
    public float maxVolume = 0.3f;

    public float fadeInDuration = 2f;
    public float fadeOutDuration = 1f;

    private void Start()
    {
        audioSource.volume = 0f;
        audioSource.Play();

        StartCoroutine(FadeIn());
    }

    public void FadeOut()
    {
        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeIn()
    {
        float time = 0f;

        while (time < fadeInDuration)
        {
            time += Time.deltaTime;

            audioSource.volume = Mathf.Lerp(
                0f,
                maxVolume,
                time / fadeInDuration
            );

            yield return null;
        }

        audioSource.volume = maxVolume;
    }

    private IEnumerator FadeOutCoroutine()
    {
        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;

            audioSource.volume = Mathf.Lerp(
                startVolume,
                0f,
                time / fadeOutDuration
            );

            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }
}