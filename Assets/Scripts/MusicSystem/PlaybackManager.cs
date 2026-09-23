
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class PlaybackManager : MonoBehaviour
{
    public enum PlaybackType
    {
        Guitar,
        Bass,
        Drums
    }

    [Header("Reproductores")]
    public MusicPlayer musicPlayer;
    public DrumPlayer drumPlayer;

    [Header("Instrumento")]
    public PlaybackType playbackType;

    [Header("Reproducción")]
    public bool playOnStart = true;

    [Header("Audio Mixer")]
    [Tooltip("Grupo del Mixer al que se enviará el audio de reproducción.")]
    public AudioMixerGroup playbackMixerGroup;

    [Header("AudioSource del Manager")]
    [Tooltip("Se asigna automáticamente si está vacío.")]
    public AudioSource playbackAudioSource;

    [Range(0f, 1f)]
    public float volume = 1f;

    public bool mute = false;

    private void Awake()
    {
        EnsureAudioSource();
        ConfigureManagerAudioSource();

        ConfigurePlayerAudioSources();

        if (musicPlayer != null)
        {
            musicPlayer.enabled = false;
        }

        if (drumPlayer != null)
        {
            drumPlayer.enabled = false;
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }
    private void EnsureAudioSource()
    {
        if (playbackAudioSource == null)
        {
            playbackAudioSource = GetComponent<AudioSource>();
        }

        if (playbackAudioSource == null)
        {
            playbackAudioSource = gameObject.AddComponent<AudioSource>();

            Debug.Log(
                "PlaybackManager: se creó un AudioSource automáticamente."
            );
        }
    }
    private void ConfigureManagerAudioSource()
    {
        if (playbackAudioSource == null)
        {
            Debug.LogError(
                "PlaybackManager: no se encontró el AudioSource."
            );

            return;
        }

        playbackAudioSource.playOnAwake = false;
        playbackAudioSource.loop = false;
        playbackAudioSource.volume = volume;
        playbackAudioSource.mute = mute;

        AssignMixerGroup(playbackAudioSource);
    }

    private void ConfigurePlayerAudioSources()
    {
        if (musicPlayer != null)
        {
            AudioSource[] musicSources =
                musicPlayer.GetComponentsInChildren<AudioSource>(true);

            foreach (AudioSource source in musicSources)
            {
                AssignMixerGroup(source);
            }
        }

        if (drumPlayer != null)
        {
            AudioSource[] drumSources =
                drumPlayer.GetComponentsInChildren<AudioSource>(true);

            foreach (AudioSource source in drumSources)
            {
                AssignMixerGroup(source);
            }
        }
    }

    private void AssignMixerGroup(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        if (playbackMixerGroup == null)
        {
            Debug.LogWarning(
                "PlaybackManager: no hay un AudioMixerGroup asignado."
            );

            return;
        }

        source.outputAudioMixerGroup = playbackMixerGroup;

        Debug.Log(
            "PlaybackManager: AudioSource '" +
            source.name +
            "' conectado al grupo '" +
            playbackMixerGroup.name +
            "'."
        );
    }
    public void Play()
    {
        StopAllPlayback();

        switch (playbackType)
        {
            case PlaybackType.Guitar:

                if (musicPlayer == null)
                {
                    Debug.LogError(
                        "PlaybackManager: MusicPlayer no asignado."
                    );

                    return;
                }

                musicPlayer.instrument =
                    MusicPlayer.InstrumentType.Guitar;

                musicPlayer.enabled = true;

                break;

            case PlaybackType.Bass:

                if (musicPlayer == null)
                {
                    Debug.LogError(
                        "PlaybackManager: MusicPlayer no asignado."
                    );

                    return;
                }

                musicPlayer.instrument =
                    MusicPlayer.InstrumentType.Bass;

                musicPlayer.enabled = true;

                break;

            case PlaybackType.Drums:

                if (drumPlayer == null)
                {
                    Debug.LogError(
                        "PlaybackManager: DrumPlayer no asignado."
                    );

                    return;
                }

                drumPlayer.enabled = true;

                break;
        }
    }

    public void Stop()
    {
        StopAllPlayback();
    }

    private void StopAllPlayback()
    {
        if (musicPlayer != null)
        {
            musicPlayer.enabled = false;
        }

        if (drumPlayer != null)
        {
            drumPlayer.enabled = false;
        }

        if (playbackAudioSource != null)
        {
            playbackAudioSource.Stop();
        }
    }
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (playbackAudioSource != null)
        {
            playbackAudioSource.volume = volume;
        }
    }

    public void SetMute(bool shouldMute)
    {
        mute = shouldMute;

        if (playbackAudioSource != null)
        {
            playbackAudioSource.mute = mute;
        }
    }

    public AudioSource GetPlaybackAudioSource()
    {
        EnsureAudioSource();

        return playbackAudioSource;
    }

    public void RouteAudioSource(AudioSource source)
    {
        AssignMixerGroup(source);
    }
    private void OnValidate()
    {
        if (playbackAudioSource == null)
        {
            playbackAudioSource = GetComponent<AudioSource>();
        }

        if (playbackAudioSource != null)
        {
            playbackAudioSource.volume = volume;
            playbackAudioSource.mute = mute;
        }
    }
}