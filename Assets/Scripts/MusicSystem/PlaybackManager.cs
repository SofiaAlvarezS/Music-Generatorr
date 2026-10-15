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
    public PlaybackType playbackType = PlaybackType.Guitar;

    [Header("Reproducción")]
    public bool playOnStart = false;

    [Header("Audio Mixer")]
    public AudioMixerGroup playbackMixerGroup;

    [Header("AudioSource del Manager (Control Principal)")]
    public AudioSource playbackAudioSource;

    [Range(0f, 1f)]
    public float volume = 1f;

    public bool mute = false;

    private void Awake()
    {
        EnsureAudioSource();
        ConfigureManagerAudioSource();
        ConfigurePlayerAudioSources();

        if (musicPlayer != null) musicPlayer.enabled = false;
        if (drumPlayer != null) drumPlayer.enabled = false;
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
        }
    }

    private void ConfigureManagerAudioSource()
    {
        if (playbackAudioSource == null) return;

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
            AudioSource[] sources = musicPlayer.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in sources)
            {
                AssignMixerGroup(source);
            }
        }

        if (drumPlayer != null)
        {
            AudioSource[] sources = drumPlayer.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in sources)
            {
                AssignMixerGroup(source);
            }
        }
    }

    private void AssignMixerGroup(AudioSource source)
    {
        if (source == null) return;

        if (playbackMixerGroup != null)
        {
            source.outputAudioMixerGroup = playbackMixerGroup;
        }
    }

    public void SetPlaybackType(PlaybackType type)
    {
        playbackType = type;
        Debug.Log("PlaybackManager → Instrumento seleccionado: " + playbackType);
    }

    public void SetGuitar() { SetPlaybackType(PlaybackType.Guitar); }
    public void SetBass() { SetPlaybackType(PlaybackType.Bass); }
    public void SetDrums() { SetPlaybackType(PlaybackType.Drums); }

    public void Play()
    {
        StopAllPlayback();

        switch (playbackType)
        {
            case PlaybackType.Guitar:
                if (musicPlayer == null) { Debug.LogError("MusicPlayer no asignado."); return; }
                musicPlayer.instrument = MusicPlayer.InstrumentType.Guitar;
                musicPlayer.enabled = true;
                break;

            case PlaybackType.Bass:
                if (musicPlayer == null) { Debug.LogError("MusicPlayer no asignado."); return; }
                musicPlayer.instrument = MusicPlayer.InstrumentType.Bass;
                musicPlayer.enabled = true;
                break;

            case PlaybackType.Drums:
                if (drumPlayer == null) { Debug.LogError("DrumPlayer no asignado."); return; }
                drumPlayer.enabled = true;
                break;
        }
    }

    public void Pausar()
    {
        if ((playbackType == PlaybackType.Guitar || playbackType == PlaybackType.Bass) && musicPlayer != null)
        {
            musicPlayer.PausePlayback();
        }
        else if (playbackType == PlaybackType.Drums && drumPlayer != null)
        {
            drumPlayer.PausePlayback();
        }
    }

    public void Stop()
    {
        StopAllPlayback();
    }

    private void StopAllPlayback()
    {
        if (musicPlayer != null) { musicPlayer.StopPlayback(); musicPlayer.enabled = false; }
        if (drumPlayer != null) { drumPlayer.StopPlayback(); drumPlayer.enabled = false; }
        if (playbackAudioSource != null) { playbackAudioSource.Stop(); }
    }

    // Método conectado al Slider (en modo Dynamic float)
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        // Curva exponencial suave para que el volumen baje de forma natural y progresiva
        float smoothVolume = volume * volume;

        if (playbackAudioSource != null)
        {
            playbackAudioSource.volume = smoothVolume;
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

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        if (Application.isPlaying)
        {
            SetVolume(volume);
            SetMute(mute);
        }
    }
}