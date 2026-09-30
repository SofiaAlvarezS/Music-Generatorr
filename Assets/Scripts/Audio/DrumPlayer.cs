using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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


    [Header("Audio Mixer")]
    [Tooltip("Grupo del Mixer al que se enviarán los golpes.")]
    public AudioMixerGroup playbackMixerGroup;


    private DrumPattern pattern;

    private bool isPaused = false;
    private bool isPlaying = false;


    // Guarda los AudioSources de los golpes que están sonando
    private List<AudioSource> activeSources =
        new List<AudioSource>();

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


        if (useMusicXMLTempo)
        {
            bpm =
                musicReader.musicData.tempo;
        }


        if (bpm <= 0f)
        {
            bpm = 120f;
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
            bpm = 120f;
        }


        isPlaying = true;
        isPaused = false;


        float secondsPerBeat =
            60f / bpm;


        float currentTime = 0f;


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
                yield return WaitWithPause(
                    waitTime * secondsPerBeat
                );
            }


            PlayDrumNote(note);


            currentTime =
                note.position;
        }


        isPlaying = false;


        Debug.Log(
            "Batería terminada."
        );
    }

    private IEnumerator WaitWithPause(
        float duration
    )
    {
        float remaining = duration;


        while (remaining > 0f)
        {
            if (!isPaused)
            {
                remaining -= Time.deltaTime;
            }


            yield return null;
        }
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


        source.clip = clip;
        source.volume = volume;
        source.playOnAwake = false;


        if (playbackMixerGroup != null)
        {
            source.outputAudioMixerGroup =
                playbackMixerGroup;
        }


        activeSources.Add(source);


        source.Play();


        StartCoroutine(
            DestroyDrumSourceWhenFinished(
                drumObject,
                source,
                clip.length
            )
        );
    }

    private IEnumerator DestroyDrumSourceWhenFinished(
        GameObject drumObject,
        AudioSource source,
        float duration
    )
    {
        float remaining = duration;


        while (remaining > 0f)
        {
            if (!isPaused)
            {
                remaining -= Time.deltaTime;
            }


            yield return null;
        }


        activeSources.Remove(source);


        if (drumObject != null)
        {
            Destroy(drumObject);
        }
    }

    public void Play()
    {
        if (pattern == null)
        {
            pattern =
                patternGenerator.GenerateBasicPattern();
        }


        StopAllCoroutines();


        isPaused = false;
        isPlaying = false;


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

    public void PausePlayback()
    {
        if (!isPlaying)
            return;


        isPaused = true;


        foreach (
            AudioSource source
            in activeSources
        )
        {
            if (source != null)
            {
                source.Pause();
            }
        }


        Debug.Log(
            "DrumPlayer → PAUSADO"
        );
    }
    public void ResumePlayback()
    {
        if (!isPlaying)
            return;


        isPaused = false;


        foreach (
            AudioSource source
            in activeSources
        )
        {
            if (source != null)
            {
                source.UnPause();
            }
        }


        Debug.Log(
            "DrumPlayer → REANUDADO"
        );
    }

    public void StopPlayback()
    {
        isPaused = false;
        isPlaying = false;


        StopAllCoroutines();


        AudioSource[] sources =
            GetComponentsInChildren<AudioSource>(
                true
            );


        foreach (
            AudioSource source
            in sources
        )
        {
            if (source != null)
            {
                source.Stop();
            }
        }


        activeSources.Clear();


        Debug.Log(
            "DrumPlayer → DETENIDO"
        );
    }
}