using UnityEngine;

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

    private void Awake()
    {
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
    }
}