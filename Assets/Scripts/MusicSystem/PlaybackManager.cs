using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
public class PlaybackManager : MonoBehaviour
{
    public enum PlaybackType
    {
        Guitar,
        Bass,
        Drums
    }


    [Header("Referencias")]
    public MusicPlayer musicPlayer;

    public DrumPlayer drumPlayer;


    [Header("Configuracion")]
    public PlaybackType playbackType;

    public bool playOnStart = false;


    [Header("Audio Mixer")]
    public AudioMixerGroup playbackMixerGroup;

    public AudioSource playbackAudioSource;


    [Header("Control de audio")]
    [Range(0f, 1f)]
    public float volume = 1f;

    public bool mute = false;


    [Header("Depuracion")]
    public bool printDebug = true;


    private bool isPrepared = false;


    // ==========================================
    // ESTADO DE REPRODUCCION
    // ==========================================

    public bool IsPlaying
    {
        get
        {
            switch (playbackType)
            {
                case PlaybackType.Guitar:
                case PlaybackType.Bass:

                    return musicPlayer != null &&
                           musicPlayer.IsPlaying;

                case PlaybackType.Drums:

                    return drumPlayer != null &&
                           drumPlayer.IsPlaying;

                default:

                    return false;
            }
        }
    }


    public bool IsPaused
    {
        get
        {
            switch (playbackType)
            {
                case PlaybackType.Guitar:
                case PlaybackType.Bass:

                    return musicPlayer != null &&
                           musicPlayer.IsPaused;

                case PlaybackType.Drums:

                    return drumPlayer != null &&
                           drumPlayer.IsPaused;

                default:

                    return false;
            }
        }
    }


    // ==========================================
    // INICIALIZACION
    // ==========================================

    private void Awake()
    {
        EnsureAudioSource();

        ConfigureAudioSource();


        if (musicPlayer != null)
        {
            if (playbackMixerGroup != null)
            {
                musicPlayer.playbackMixerGroup =
                    playbackMixerGroup;
            }

            musicPlayer.enabled = false;
        }


        if (drumPlayer != null)
        {
            if (playbackMixerGroup != null)
            {
                drumPlayer.playbackMixerGroup =
                    playbackMixerGroup;
            }

            drumPlayer.playOnStart = false;

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


    // ==========================================
    // CONFIGURACION DE AUDIO
    // ==========================================

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


    private void ConfigureAudioSource()
    {
        if (playbackAudioSource == null)
        {
            return;
        }


        playbackAudioSource.playOnAwake = false;

        playbackAudioSource.loop = false;

        playbackAudioSource.volume = volume;

        playbackAudioSource.mute = mute;


        if (playbackMixerGroup != null)
        {
            playbackAudioSource.outputAudioMixerGroup =
                playbackMixerGroup;
        }
    }


    // ==========================================
    // SELECCION DE INSTRUMENTO
    // ==========================================

    public void SetPlaybackType(
        PlaybackType type)
    {
        StopAllPlayback();

        playbackType = type;
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


    // ==========================================
    // REPRODUCCION INDIVIDUAL
    // ==========================================

    public void Play()
    {
        StopAllPlayback();


        switch (playbackType)
        {
            case PlaybackType.Guitar:

                if (musicPlayer == null)
                {
                    Debug.LogError(
                        "MusicPlayer no asignado."
                    );

                    return;
                }


                musicPlayer.instrument =
                    MusicPlayer.InstrumentType.Guitar;

                musicPlayer.enabled = true;

                musicPlayer.PlayPlayback();

                break;


            case PlaybackType.Bass:

                if (musicPlayer == null)
                {
                    Debug.LogError(
                        "MusicPlayer no asignado."
                    );

                    return;
                }


                musicPlayer.instrument =
                    MusicPlayer.InstrumentType.Bass;

                musicPlayer.enabled = true;

                musicPlayer.PlayPlayback();

                break;


            case PlaybackType.Drums:

                if (drumPlayer == null)
                {
                    Debug.LogError(
                        "DrumPlayer no asignado."
                    );

                    return;
                }


                drumPlayer.playOnStart = false;

                drumPlayer.enabled = true;

                drumPlayer.Play();

                break;
        }
    }


    // ==========================================
    // PREPARAR REPRODUCCION SINCRONIZADA
    // ==========================================

    public bool PrepareSynchronizedPlayback()
    {
        StopAllPlayback();


        bool prepared = false;


        switch (playbackType)
        {
            case PlaybackType.Guitar:

                if (musicPlayer == null)
                {
                    Debug.LogError(
                        "MusicPlayer no asignado."
                    );

                    return false;
                }


                musicPlayer.instrument =
                    MusicPlayer.InstrumentType.Guitar;

                musicPlayer.enabled = true;


                prepared =
                    musicPlayer.PrepareSynchronizedPlayback();

                break;


            case PlaybackType.Bass:

                if (musicPlayer == null)
                {
                    Debug.LogError(
                        "MusicPlayer no asignado."
                    );

                    return false;
                }


                musicPlayer.instrument =
                    MusicPlayer.InstrumentType.Bass;

                musicPlayer.enabled = true;


                prepared =
                    musicPlayer.PrepareSynchronizedPlayback();

                break;


            // NUEVO: BATERIA SINCRONIZADA

            case PlaybackType.Drums:

                if (drumPlayer == null)
                {
                    Debug.LogError(
                        "DrumPlayer no asignado."
                    );

                    return false;
                }


                drumPlayer.playOnStart = false;

                drumPlayer.enabled = true;


                prepared =
                    drumPlayer.PrepareSynchronizedPlayback();

                break;
        }


        if (!prepared)
        {
            StopAllPlayback();

            Debug.LogError(
                "No se pudo preparar: " +
                playbackType
            );

            return false;
        }


        isPrepared = true;


        if (printDebug)
        {
            Debug.Log(
                "PlaybackManager preparado: " +
                playbackType
            );
        }


        return true;
    }


    // ==========================================
    // REPRODUCCION SINCRONIZADA
    // ==========================================

    public void PlaySynchronized(
        double startTime)
    {
        if (!isPrepared)
        {
            Debug.LogError(
                "PlaybackManager: " +
                "el instrumento no está preparado."
            );

            return;
        }


        if (double.IsNaN(startTime) ||
            double.IsInfinity(startTime))
        {
            Debug.LogError(
                "Tiempo DSP invalido."
            );

            return;
        }


        switch (playbackType)
        {
            case PlaybackType.Guitar:
            case PlaybackType.Bass:

                if (musicPlayer == null ||
                    !musicPlayer.enabled)
                {
                    Debug.LogError(
                        "MusicPlayer no preparado."
                    );

                    return;
                }


                musicPlayer.PlaySynchronized(
                    startTime
                );

                break;


            // NUEVO: BATERIA SINCRONIZADA

            case PlaybackType.Drums:

                if (drumPlayer == null ||
                    !drumPlayer.enabled)
                {
                    Debug.LogError(
                        "DrumPlayer no preparado."
                    );

                    return;
                }


                drumPlayer.PlaySynchronized(
                    startTime
                );

                break;
        }


        if (printDebug)
        {
            Debug.Log(
                "PlaybackManager | " +
                playbackType +
                " | Inicio DSP: " +
                startTime.ToString("F4")
            );
        }
    }


    // ==========================================
    // PAUSAR
    // ==========================================

    public void Pausar()
    {
        switch (playbackType)
        {
            case PlaybackType.Guitar:
            case PlaybackType.Bass:

                if (musicPlayer != null)
                {
                    musicPlayer.PausePlayback();
                }

                break;


            case PlaybackType.Drums:

                if (drumPlayer != null)
                {
                    drumPlayer.PausePlayback();
                }

                break;
        }
    }


    // ==========================================
    // REANUDAR
    // ==========================================

    public void Reanudar()
    {
        switch (playbackType)
        {
            case PlaybackType.Guitar:
            case PlaybackType.Bass:

                if (musicPlayer != null)
                {
                    musicPlayer.ResumePlayback();
                }

                break;


            case PlaybackType.Drums:

                if (drumPlayer != null)
                {
                    drumPlayer.ResumePlayback();
                }

                break;
        }
    }


    // ==========================================
    // DETENER
    // ==========================================

    public void Stop()
    {
        StopAllPlayback();
    }


    private void StopAllPlayback()
    {
        isPrepared = false;


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


    // ==========================================
    // CONTROL DE VOLUMEN
    // ==========================================

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);


        if (playbackAudioSource != null)
        {
            playbackAudioSource.volume =
                volume;
        }
    }


    public void SetMute(bool newMute)
    {
        mute = newMute;


        if (playbackAudioSource != null)
        {
            playbackAudioSource.mute =
                mute;
        }
    }


    // ==========================================
    // CICLO DE VIDA
    // ==========================================

    private void OnDisable()
    {
        StopAllPlayback();
    }
    // ==========================================
    // OBTENER EVENTOS DEL INSTRUMENTO ORIGINAL
    // ==========================================

    public List<AudioRenderEvent> GetRenderEvents()
    {
        if (!isPrepared)
        {
            return new List<AudioRenderEvent>();
        }


        switch (playbackType)
        {
            case PlaybackType.Guitar:
            case PlaybackType.Bass:

                if (musicPlayer != null)
                {
                    return musicPlayer.GetRenderEvents();
                }

                break;


            case PlaybackType.Drums:

                if (drumPlayer != null)
                {
                    return drumPlayer.GetRenderEvents();
                }

                break;
        }


        return new List<AudioRenderEvent>();
    }


    // ==========================================
    // OBTENER DURACION DEL ORIGINAL
    // ==========================================

    public float GetRenderDuration()
    {
        if (!isPrepared)
        {
            return 0f;
        }


        switch (playbackType)
        {
            case PlaybackType.Guitar:
            case PlaybackType.Bass:

                if (musicPlayer != null)
                {
                    return musicPlayer.GetRenderDuration();
                }

                break;


            case PlaybackType.Drums:

                if (drumPlayer != null)
                {
                    return drumPlayer.GetRenderDuration();
                }

                break;
        }


        return 0f;
    }
}