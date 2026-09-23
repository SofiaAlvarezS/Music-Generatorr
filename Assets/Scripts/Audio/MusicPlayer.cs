
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class MusicPlayer : MonoBehaviour
{
    public MusicXMLReader musicReader;

    public enum InstrumentType
    {
        Guitar,
        Bass
    }

    [Header("Instrumento")]
    public InstrumentType instrument;

    [Header("Sintetizadores")]
    public GuitarSynthesizer guitarSynthesizer;
    public BassSynthesizer bassSynthesizer;

    [Header("Audio")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Header("Audio Mixer")]
    [Tooltip("Grupo del Mixer al que se enviará el audio.")]
    public AudioMixerGroup playbackMixerGroup;

    private AudioSource audioSource;

    private void Awake()
    {
        EnsureAudioSource();
        ConfigureAudioSource();
    }

    private void Start()
    {
        EnsureAudioSource();
        ConfigureAudioSource();

        if (musicReader == null)
        {
            Debug.LogError(
                "MusicPlayer no tiene asignado " +
                "un MusicXMLReader."
            );

            return;
        }

        StartCoroutine(WaitForMusicData());
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();

            Debug.Log(
                "MusicPlayer: AudioSource creado automáticamente."
            );
        }
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = volume;

        if (playbackMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup =
                playbackMixerGroup;

            Debug.Log(
                "MusicPlayer: AudioSource conectado al grupo " +
                playbackMixerGroup.name
            );
        }
        else
        {
            Debug.LogWarning(
                "MusicPlayer: no hay un AudioMixerGroup asignado."
            );
        }
    }

    private IEnumerator WaitForMusicData()
    {
        Debug.Log(
            "MusicPlayer esperando MusicData..."
        );

        while (!musicReader.isReady)
        {
            yield return null;
        }

        Debug.Log(
            "MusicData lista."
        );

        StartCoroutine(PlayMusic());
    }

    private IEnumerator PlayMusic()
    {
        Debug.Log(
            "===================================="
        );

        Debug.Log(
            "INICIANDO REPRODUCCIÓN"
        );

        Debug.Log(
            "Instrumento: " +
            instrument
        );

        Debug.Log(
            "===================================="
        );

        foreach (
            MeasureData measure
            in musicReader.musicData.measures
        )
        {
            Debug.Log(
                "Compás " +
                measure.number
            );

            foreach (
                NoteData note
                in measure.notes
            )
            {
                float duration =
                    GetDurationInSeconds(note);

                if (note.isRest)
                {
                    Debug.Log(
                        "Silencio | " +
                        duration +
                        " segundos"
                    );

                    yield return new WaitForSeconds(duration);

                    continue;
                }

                Debug.Log(
                    "Nota: " +
                    note.step +
                    note.octave +
                    " | Duración: " +
                    duration
                );

                PlayNote(note, duration);

                yield return new WaitForSeconds(duration);
            }
        }

        Debug.Log(
            "===================================="
        );

        Debug.Log(
            "REPRODUCCIÓN TERMINADA"
        );

        Debug.Log(
            "===================================="
        );
    }

    private void PlayNote(
        NoteData note,
        float duration
    )
    {
        float frequency =
            GetFrequency(
                note.step,
                note.octave,
                note.alter
            );

        AudioClip clip = null;

        switch (instrument)
        {
            case InstrumentType.Guitar:

                if (guitarSynthesizer == null)
                {
                    Debug.LogError(
                        "No se ha asignado GuitarSynthesizer."
                    );

                    return;
                }

                clip =
                    guitarSynthesizer.GenerateNote(
                        frequency,
                        duration
                    );

                break;

            case InstrumentType.Bass:

                if (bassSynthesizer == null)
                {
                    Debug.LogError(
                        "No se ha asignado BassSynthesizer."
                    );

                    return;
                }

                clip =
                    bassSynthesizer.GenerateNote(
                        frequency,
                        duration
                    );

                break;
        }

        if (clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            EnsureAudioSource();
            ConfigureAudioSource();
        }

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    private float GetDurationInSeconds(
        NoteData note
    )
    {
        float tempo =
            musicReader.musicData.tempo;

        if (tempo <= 0f)
        {
            tempo = 120f;
        }

        int divisions =
            musicReader.musicData.divisions;

        if (divisions <= 0)
        {
            divisions = 1;
        }

        float quarterNoteDuration =
            60f / tempo;

        float quarterNotes =
            (float)note.duration / divisions;

        return quarterNotes * quarterNoteDuration;
    }

    private float GetFrequency(
        string noteName,
        int octave,
        int alter
    )
    {
        int semitone;

        switch (noteName)
        {
            case "C":
                semitone = -9;
                break;

            case "D":
                semitone = -7;
                break;

            case "E":
                semitone = -5;
                break;

            case "F":
                semitone = -4;
                break;

            case "G":
                semitone = -2;
                break;

            case "A":
                semitone = 0;
                break;

            case "B":
                semitone = 2;
                break;

            default:
                semitone = 0;
                break;
        }

        int octaveDifference =
            octave - 4;

        int totalSemitones =
            semitone +
            octaveDifference * 12 +
            alter;

        return 440f *
               Mathf.Pow(
                   2f,
                   totalSemitones / 12f
               );
    }

    public void StopPlayback()
    {
        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}