using System.Collections;
using UnityEngine;

public class DrumPlayer : MonoBehaviour
{
    [Header("Referencias")]
    public MusicXMLReader musicReader;

    public DrumPatternGenerator patternGenerator;

    public DrumSynthesizer drumSynthesizer;


    [Header("Tempo")]
    public bool useMusicXMLTempo = true;

    public float bpm = 120f;


    [Header("Reproducción")]
    public bool playOnStart = true;

    [Range(0f, 1f)]
    public float volume = 0.8f;


    private DrumPattern pattern;


    private void Start()
    {
        if (patternGenerator == null)
        {
            Debug.LogError(
                "DrumPlayer: falta asignar " +
                "DrumPatternGenerator."
            );

            return;
        }


        if (drumSynthesizer == null)
        {
            Debug.LogError(
                "DrumPlayer: falta asignar " +
                "DrumSynthesizer."
            );

            return;
        }


        // Generar patrón
        pattern =
            patternGenerator.GenerateBasicPattern();


        Debug.Log(
            "Patrón de batería generado. " +
            "Eventos: " +
            pattern.notes.Count
        );


        if (playOnStart)
        {
            if (musicReader != null)
            {
                StartCoroutine(
                    WaitForMusicData()
                );
            }
            else
            {
                StartCoroutine(
                    PlayPattern()
                );
            }
        }
    }

    private IEnumerator WaitForMusicData()
    {
        Debug.Log(
            "DrumPlayer esperando MusicData..."
        );


        while (!musicReader.isReady)
        {
            yield return null;
        }


        Debug.Log(
            "MusicData lista."
        );


        // Obtener tempo
        if (useMusicXMLTempo)
        {
            bpm =
                musicReader.musicData.tempo;
        }


        Debug.Log(
            "Tempo utilizado por batería: " +
            bpm +
            " BPM"
        );


        StartCoroutine(
            PlayPattern()
        );
    }


    private IEnumerator PlayPattern()
    {
        if (bpm <= 0f)
        {
            Debug.LogWarning(
                "BPM inválido. " +
                "Se utilizarán 120 BPM."
            );

            bpm = 120f;
        }


        float secondsPerBeat =
            60f / bpm;


        float currentTime =
            0f;


        pattern.notes.Sort(
            (a, b) =>
                a.position.CompareTo(
                    b.position
                )
        );


        Debug.Log(
            "================================"
        );

        Debug.Log(
            "INICIANDO BATERÍA"
        );

        Debug.Log(
            "BPM: " +
            bpm
        );

        Debug.Log(
            "================================"
        );


        foreach (
            DrumNoteData note
            in pattern.notes
        )
        {
            float waitTime =
                note.position -
                currentTime;


            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(
                    waitTime *
                    secondsPerBeat
                );
            }


            PlayDrumNote(
                note
            );


            currentTime =
                note.position;
        }


        Debug.Log(
            "Batería terminada."
        );
    }

    private void PlayDrumNote(
        DrumNoteData note
    )
    {
        AudioClip clip =
            drumSynthesizer.GenerateDrum(
                note.midiNote,
                note.duration,
                note.velocity
            );


        if (clip == null)
        {
            return;
        }


        GameObject drumObject =
            new GameObject(
                "DrumHit_" +
                note.midiNote
            );


        drumObject.transform.parent =
            transform;


        AudioSource source =
            drumObject.AddComponent<AudioSource>();


        source.clip =
            clip;


        source.volume =
            volume;


        source.Play();


        Destroy(
            drumObject,
            clip.length + 0.05f
        );
    }

    public void Play()
    {
        if (pattern == null)
        {
            pattern =
                patternGenerator.GenerateBasicPattern();
        }


        StopAllCoroutines();


        if (
            useMusicXMLTempo &&
            musicReader != null &&
            musicReader.isReady
        )
        {
            bpm =
                musicReader.musicData.tempo;
        }


        StartCoroutine(
            PlayPattern()
        );
    }
}

