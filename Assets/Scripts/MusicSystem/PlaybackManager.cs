using UnityEngine;
using UnityEngine.Audio;

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
    public PlaybackType playbackType =
        PlaybackType.Guitar;

    [Header("Reproducción")]
    public bool playOnStart = false;

    [Header("Audio Mixer")]
    public AudioMixerGroup playbackMixerGroup;

    [Header("AudioSource del Manager")]
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
            playbackAudioSource =
                GetComponent<AudioSource>();
        }

        if (playbackAudioSource == null)
        {
            playbackAudioSource =
                gameObject.AddComponent<AudioSource>();
        }
    }


    private void ConfigureManagerAudioSource()
    {
        if (playbackAudioSource == null)
        {
            return;
        }

        playbackAudioSource.playOnAwake = false;
        playbackAudioSource.loop = false;
        playbackAudioSource.volume = volume;
        playbackAudioSource.mute = mute;

        AssignMixerGroup(
            playbackAudioSource
        );
    }


    private void ConfigurePlayerAudioSources()
    {
        if (musicPlayer != null)
        {
            AudioSource[] sources =
                musicPlayer.GetComponentsInChildren<AudioSource>(
                    true
                );

            foreach (AudioSource source in sources)
            {
                AssignMixerGroup(source);
            }
        }

        if (drumPlayer != null)
        {
            AudioSource[] sources =
                drumPlayer.GetComponentsInChildren<AudioSource>(
                    true
                );

            foreach (AudioSource source in sources)
            {
                AssignMixerGroup(source);
            }
        }
    }


    private void AssignMixerGroup(
        AudioSource source
    )
    {
        if (source == null)
        {
            return;
        }

        if (playbackMixerGroup != null)
        {
            source.outputAudioMixerGroup =
                playbackMixerGroup;
        }
    }

    public void SetPlaybackType(
        PlaybackType type
    )
    {
        playbackType = type;

        Debug.Log(
            "PlaybackManager → Instrumento seleccionado: " +
            playbackType
        );
    }


    public void SetGuitar()
    {
        SetPlaybackType(
            PlaybackType.Guitar
        );
    }


    public void SetBass()
    {
        SetPlaybackType(
            PlaybackType.Bass
        );
    }


    public void SetDrums()
    {
        SetPlaybackType(
            PlaybackType.Drums
        );
    }

    public void Play()
    {
        // PLAY siempre empieza una reproducción nueva
        StopAllPlayback();

        switch (playbackType)
        {
            case PlaybackType.Guitar:

                if (musicPlayer == null)
                {
                    Debug.LogError(
                        "PlaybackManager: " +
                        "MusicPlayer no está asignado."
                    );

                    return;
                }

                musicPlayer.instrument =
                    MusicPlayer.InstrumentType.Guitar;

                musicPlayer.enabled = true;

                Debug.Log(
                    "PlaybackManager → " +
                    "Reproduciendo GUITARRA"
                );

                break;


            case PlaybackType.Bass:

                if (musicPlayer == null)
                {
                    Debug.LogError(
                        "PlaybackManager: " +
                        "MusicPlayer no está asignado."
                    );

                    return;
                }

                musicPlayer.instrument =
                    MusicPlayer.InstrumentType.Bass;

                musicPlayer.enabled = true;

                Debug.Log(
                    "PlaybackManager → " +
                    "Reproduciendo BAJO"
                );

                break;


            case PlaybackType.Drums:

                if (drumPlayer == null)
                {
                    Debug.LogError(
                        "PlaybackManager: " +
                        "DrumPlayer no está asignado."
                    );

                    return;
                }

                drumPlayer.enabled = true;

                Debug.Log(
                    "PlaybackManager → " +
                    "Reproduciendo BATERÍA"
                );

                break;
        }
    }

    public void Pausar()
    {
        switch (playbackType)
        {
            case PlaybackType.Guitar:
            case PlaybackType.Bass:

                if (musicPlayer != null &&
                    musicPlayer.enabled)
                {
                    musicPlayer.PausePlayback();

                    Debug.Log(
                        "PlaybackManager → PAUSA"
                    );
                }

                break;


            case PlaybackType.Drums:

                if (drumPlayer != null &&
                    drumPlayer.enabled)
                {
                    drumPlayer.PausePlayback();

                    Debug.Log(
                        "PlaybackManager → PAUSA"
                    );
                }

                break;
        }
    }

    public void Stop()
    {
        StopAllPlayback();

        Debug.Log(
            "PlaybackManager → DETENIDO"
        );
    }


    private void StopAllPlayback()
    {
        if (musicPlayer != null)
        {
            musicPlayer.StopPlayback();
            musicPlayer.enabled = false;
        }

        if (drumPlayer != null)
        {
            drumPlayer.StopPlayback();
            drumPlayer.enabled = false;
        }

        if (playbackAudioSource != null)
        {
            playbackAudioSource.Stop();
        }
    }

    public void SetVolume(
        float newVolume
    )
    {
        volume =
            Mathf.Clamp01(newVolume);

        if (playbackAudioSource != null)
        {
            playbackAudioSource.volume =
                volume;
        }

        if (musicPlayer != null)
        {
            AudioSource[] sources =
                musicPlayer.GetComponentsInChildren<AudioSource>(
                    true
                );

            foreach (AudioSource source in sources)
            {
                source.volume =
                    volume;
            }
        }

        if (drumPlayer != null)
        {
            AudioSource[] sources =
                drumPlayer.GetComponentsInChildren<AudioSource>(
                    true
                );

            foreach (AudioSource source in sources)
            {
                source.volume =
                    volume;
            }
        }
    }

    public void SetMute(
        bool shouldMute
    )
    {
        mute =
            shouldMute;

        if (playbackAudioSource != null)
        {
            playbackAudioSource.mute =
                mute;
        }

        if (musicPlayer != null)
        {
            AudioSource[] sources =
                musicPlayer.GetComponentsInChildren<AudioSource>(
                    true
                );

            foreach (AudioSource source in sources)
            {
                source.mute =
                    mute;
            }
        }

        if (drumPlayer != null)
        {
            AudioSource[] sources =
                drumPlayer.GetComponentsInChildren<AudioSource>(
                    true
                );

            foreach (AudioSource source in sources)
            {
                source.mute =
                    mute;
            }
        }
    }

    public AudioSource GetPlaybackAudioSource()
    {
        return playbackAudioSource;
    }


    public void RouteAudioSource(
        AudioSource source
    )
    {
        if (source == null)
        {
            return;
        }

        AssignMixerGroup(source);

        source.volume =
            volume;

        source.mute =
            mute;
    }

    private void OnValidate()
    {
        volume =
            Mathf.Clamp01(volume);

        if (Application.isPlaying)
        {
            SetVolume(volume);
            SetMute(mute);
        }
    }
}
