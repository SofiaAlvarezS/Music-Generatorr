using System.Collections;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    // =========================================================
    // REFERENCIA AL LECTOR
    // =========================================================

    public MusicXMLReader musicReader;


    // =========================================================
    // INSTRUMENTO
    // =========================================================

    public enum InstrumentType
    {
        Guitar,
        Bass
    }

    [Header("Instrumento")]
    public InstrumentType instrument;


    // =========================================================
    // SINTETIZADORES
    // =========================================================

    [Header("Sintetizadores")]
    public GuitarSynthesizer guitarSynthesizer;
    public BassSynthesizer bassSynthesizer;


    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio")]
    [Range(0f, 1f)]
    public float volume = 0.5f;


    private AudioSource audioSource;


    // =========================================================
    // INICIO
    // =========================================================

    private void Start()
    {
        audioSource =
            gameObject.AddComponent<AudioSource>();


        // Comprobar MusicXMLReader
        if (musicReader == null)
        {
            Debug.LogError(
                "MusicPlayer no tiene asignado " +
                "un MusicXMLReader."
            );

            return;
        }


        // Esperar a que MusicData esté lista
        StartCoroutine(
            WaitForMusicData()
        );
    }


    // =========================================================
    // ESPERAR MUSIC DATA
    // =========================================================

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


        // Comenzar reproducción
        StartCoroutine(
            PlayMusic()
        );
    }


    // =========================================================
    // REPRODUCCIÓN
    // =========================================================

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


        // Recorrer todos los compases
        foreach (
            MeasureData measure
            in musicReader.musicData.measures
        )
        {
            Debug.Log(
                "Compás " +
                measure.number
            );


            // Recorrer eventos del compás
            foreach (
                NoteData note
                in measure.notes
            )
            {
                float duration =
                    GetDurationInSeconds(
                        note
                    );


                // =================================================
                // SILENCIO
                // =================================================

                if (note.isRest)
                {
                    Debug.Log(
                        "Silencio | " +
                        duration +
                        " segundos"
                    );


                    yield return new WaitForSeconds(
                        duration
                    );


                    continue;
                }


                // =================================================
                // NOTA
                // =================================================

                Debug.Log(
                    "Nota: " +
                    note.step +
                    note.octave +
                    " | Duración: " +
                    duration
                );


                // Reproducir usando el sintetizador
                // seleccionado
                PlayNote(
                    note,
                    duration
                );


                yield return new WaitForSeconds(
                    duration
                );
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


    // =========================================================
    // REPRODUCIR NOTA
    // =========================================================

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


        // =====================================================
        // ELEGIR INSTRUMENTO
        // =====================================================

        switch (instrument)
        {
            case InstrumentType.Guitar:

                if (guitarSynthesizer == null)
                {
                    Debug.LogError(
                        "No se ha asignado " +
                        "GuitarSynthesizer."
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
                        "No se ha asignado " +
                        "BassSynthesizer."
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


        // =====================================================
        // REPRODUCCIÓN
        // =====================================================

        audioSource.clip =
            clip;

        audioSource.volume =
            volume;

        audioSource.Play();
    }


    // =========================================================
    // DURACIÓN MUSICXML → SEGUNDOS
    // =========================================================

    private float GetDurationInSeconds(
        NoteData note
    )
    {
        float tempo =
            musicReader.musicData.tempo;


        // Si el XML no tiene tempo
        // utilizamos 120 BPM temporalmente
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


        // Duración de una negra
        float quarterNoteDuration =
            60f / tempo;


        // Duración en negras
        float quarterNotes =
            (float)note.duration /
            divisions;


        // Duración en segundos
        return quarterNotes *
               quarterNoteDuration;
    }


    // =========================================================
    // NOTA → FRECUENCIA
    // =========================================================

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
}