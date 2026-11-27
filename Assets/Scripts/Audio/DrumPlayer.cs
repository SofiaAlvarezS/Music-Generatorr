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


    [Header("Sincronización")]
    [Min(0.05f)]
    public float startDelay = 0.15f;

    public bool printDebug = true;


    private DrumPattern pattern;

    private bool isPaused = false;
    private bool isPlaying = false;

    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;


    private bool isPrepared = false;

    private double pauseStartedAt;
    private double accumulatedPause;

    private float preparedDuration;


    private List<AudioSource> activeSources =
        new List<AudioSource>();


    // ==========================================
    // EVENTOS PREPARADOS
    // ==========================================

    private struct ScheduledDrumNote
    {
        public int midiNote;

        public float position;

        public AudioClip clip;
    }


    private readonly List<ScheduledDrumNote> scheduledNotes =
        new List<ScheduledDrumNote>();


    private readonly List<AudioClip> generatedClips =
        new List<AudioClip>();


    // ==========================================
    // INICIALIZACION
    // ==========================================

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
                Play();
            }
        }
    }


    // ==========================================
    // ESPERAR CARGA DE LA PARTITURA
    // ==========================================

    private IEnumerator WaitForMusicData()
    {
        if (printDebug)
        {
            Debug.Log(
                "DrumPlayer esperando MusicData..."
            );
        }


        while (musicReader != null &&
               !musicReader.isReady)
        {
            yield return null;
        }


        if (musicReader == null)
        {
            yield break;
        }


        if (printDebug)
        {
            Debug.Log(
                "MusicData lista."
            );
        }


        Play();
    }


    // ==========================================
    // CONFIGURACION DEL TEMPO
    // ==========================================

    private void UpdateTempo()
    {
        if (useMusicXMLTempo &&
            musicReader != null &&
            musicReader.isReady &&
            musicReader.musicData != null)
        {
            bpm =
                musicReader.musicData.tempo;
        }


        if (float.IsNaN(bpm) ||
            float.IsInfinity(bpm) ||
            bpm <= 0f)
        {
            bpm = 120f;
        }


        if (printDebug)
        {
            Debug.Log(
                "Tempo utilizado por batería: " +
                bpm +
                " BPM"
            );
        }
    }


    // ==========================================
    // NUEVO: PREPARAR REPRODUCCION
    // ==========================================

    public bool PrepareSynchronizedPlayback()
    {
        StopPlayback();


        try
        {
            if (patternGenerator == null)
            {
                throw new System.Exception(
                    "Falta asignar DrumPatternGenerator."
                );
            }


            if (drumSynthesizer == null)
            {
                throw new System.Exception(
                    "Falta asignar DrumSynthesizer."
                );
            }


            UpdateTempo();


            // Generar el patrón de batería.

            if (musicReader != null &&
                musicReader.isReady &&
                musicReader.musicData != null)
            {
                pattern =
                    patternGenerator.GeneratePatternFromMusic(
                        musicReader.musicData
                    );
            }
            else
            {
                pattern =
                    patternGenerator.GenerateBasicPattern();
            }

            if (pattern == null ||
                pattern.notes == null ||
                pattern.notes.Count == 0)
            {
                throw new System.Exception(
                    "El patrón de batería está vacío."
                );
            }


            float secondsPerBeat =
                60f / bpm;


            preparedDuration = 0f;

            if (musicReader != null &&
                musicReader.isReady &&
                musicReader.musicData != null)
            {
                MusicData data =
                    musicReader.musicData;

                int beats =
                    data.beats > 0 ? data.beats : 4;

                int beatType =
                    data.beatType > 0 ? data.beatType : 4;

                preparedDuration =
                    data.measures.Count *
                    beats *
                    (4f / beatType) *
                    secondsPerBeat;
            }
            else
            {
                preparedDuration =
                    16f * secondsPerBeat;
            }


            // Ordenar cronológicamente.

            pattern.notes.Sort(
                (a, b) =>
                    a.position.CompareTo(
                        b.position
                    )
            );


            // Sintetizar todos los golpes antes
            // de comenzar la reproducción.

            foreach (
                DrumNoteData note
                in pattern.notes
            )
            {
                if (float.IsNaN(note.position) ||
                    float.IsInfinity(note.position) ||
                    note.position < 0f ||
                    note.duration <= 0f)
                {
                    continue;
                }


                AudioClip clip =
                    drumSynthesizer.GenerateDrum(
                        note.midiNote,
                        note.duration,
                        note.velocity
                    );


                if (clip == null)
                {
                    throw new System.Exception(
                        "No se pudo sintetizar el golpe: " +
                        note.midiNote
                    );
                }


                generatedClips.Add(clip);


                // Convertir posición musical
                // a segundos absolutos.

                float position =
                    note.position * secondsPerBeat;


                scheduledNotes.Add(
                    new ScheduledDrumNote
                    {
                        midiNote = note.midiNote,

                        position = position,

                        clip = clip
                    }
                );


                // Considerar la duración
                // completa del sonido.

                preparedDuration = Mathf.Max(
                    preparedDuration,
                    position + clip.length
                );
            }


            if (scheduledNotes.Count == 0)
            {
                throw new System.Exception(
                    "No existen golpes válidos."
                );
            }


            scheduledNotes.Sort(
                (a, b) =>
                    a.position.CompareTo(
                        b.position
                    )
            );


            isPrepared = true;


            if (printDebug)
            {
                Debug.Log(
                    "================================"
                );

                Debug.Log(
                    "BATERÍA PREPARADA"
                );

                Debug.Log(
                    "BPM: " + bpm
                );

                Debug.Log(
                    "Eventos: " +
                    scheduledNotes.Count
                );

                Debug.Log(
                    "Duración: " +
                    preparedDuration.ToString("F2") +
                    " segundos"
                );

                Debug.Log(
                    "================================"
                );
            }


            return true;
        }
        catch (System.Exception e)
        {
            StopPlayback();

            Debug.LogError(
                "DrumPlayer: " +
                e.Message,
                this
            );

            return false;
        }
    }


    // ==========================================
    // NUEVO: REPRODUCCION SINCRONIZADA
    // ==========================================

    public void PlaySynchronized(double startTime)
    {
        if (!isPrepared ||
            scheduledNotes.Count == 0)
        {
            Debug.LogError(
                "DrumPlayer: prepara primero " +
                "la reproducción."
            );

            return;
        }


        if (double.IsNaN(startTime) ||
            double.IsInfinity(startTime))
        {
            Debug.LogError(
                "DrumPlayer: tiempo DSP inválido."
            );

            return;
        }


        // Detener reproducción anterior sin
        // eliminar los clips preparados.

        StopAllCoroutines();

        ClearActiveSources();


        isPaused = false;

        isPlaying = true;

        pauseStartedAt = 0.0;

        accumulatedPause = 0.0;


        StartCoroutine(
            PlaySynchronizedPattern(startTime)
        );


        if (printDebug)
        {
            Debug.Log(
                "DrumPlayer | Inicio DSP: " +
                startTime.ToString("F4")
            );
        }
    }


    // ==========================================
    // REPRODUCCION MEDIANTE RELOJ DSP
    // ==========================================

    private IEnumerator PlaySynchronizedPattern(
        double startTime)
    {
        double endTime =
            startTime + preparedDuration;


        if (printDebug)
        {
            Debug.Log(
                "INICIANDO BATERÍA SINCRONIZADA"
            );
        }


        foreach (
            ScheduledDrumNote note
            in scheduledNotes
        )
        {
            // Esperar la posición absoluta
            // de cada evento musical.

            while (
                isPaused ||
                AudioSettings.dspTime <
                startTime +
                note.position +
                accumulatedPause
            )
            {
                yield return null;
            }


            PlayDrumNote(note);


            // Considerar posibles retrasos
            // de ejecución entre frames.

            double actualEnd =
                AudioSettings.dspTime -
                accumulatedPause +
                note.clip.length;


            endTime = System.Math.Max(
                endTime,
                actualEnd
            );
        }


        // Esperar que terminen todos
        // los golpes y sus colas.

        while (
            isPaused ||
            AudioSettings.dspTime <
            endTime + accumulatedPause
        )
        {
            yield return null;
        }


        isPlaying = false;

        isPaused = false;


        ClearActiveSources();


        if (printDebug)
        {
            Debug.Log(
                "Batería terminada."
            );
        }
    }


    // ==========================================
    // REPRODUCIR GOLPE DE BATERIA
    // ==========================================

    private void PlayDrumNote(
        ScheduledDrumNote note)
    {
        if (note.clip == null)
        {
            return;
        }


        GameObject drumObject =
            new GameObject(
                "DrumHit_" +
                note.midiNote
            );


        drumObject.transform.SetParent(
            transform,
            false
        );


        AudioSource source =
            drumObject.AddComponent<AudioSource>();


        source.clip = note.clip;

        source.volume = volume;

        source.playOnAwake = false;

        source.loop = false;

        source.spatialBlend = 0f;


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
                note.clip.length
            )
        );
    }


    // ==========================================
    // LIMPIEZA DE CADA GOLPE
    // ==========================================

    private IEnumerator DestroyDrumSourceWhenFinished(
        GameObject drumObject,
        AudioSource source,
        float duration)
    {
        double finishTime =
            AudioSettings.dspTime +
            duration -
            accumulatedPause;


        while (
            isPaused ||
            AudioSettings.dspTime <
            finishTime + accumulatedPause
        )
        {
            yield return null;
        }


        activeSources.Remove(source);


        if (source != null)
        {
            source.Stop();

            source.clip = null;
        }


        if (drumObject != null)
        {
            Destroy(drumObject);
        }
    }


    // ==========================================
    // REPRODUCCION INDIVIDUAL
    // ==========================================

    public void Play()
    {
        if (!PrepareSynchronizedPlayback())
        {
            return;
        }


        double startTime =
            AudioSettings.dspTime +
            startDelay;


        PlaySynchronized(startTime);
    }


    // ==========================================
    // PAUSAR REPRODUCCION
    // ==========================================

    public void PausePlayback()
    {
        if (!isPlaying ||
            isPaused)
        {
            return;
        }


        isPaused = true;


        pauseStartedAt =
            AudioSettings.dspTime;


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


        if (printDebug)
        {
            Debug.Log(
                "DrumPlayer → PAUSADO"
            );
        }
    }


    // ==========================================
    // REANUDAR REPRODUCCION
    // ==========================================

    public void ResumePlayback()
    {
        if (!isPlaying ||
            !isPaused)
        {
            return;
        }


        accumulatedPause +=
            AudioSettings.dspTime -
            pauseStartedAt;


        pauseStartedAt = 0.0;


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


        if (printDebug)
        {
            Debug.Log(
                "DrumPlayer → REANUDADO"
            );
        }
    }


    // ==========================================
    // DETENER REPRODUCCION
    // ==========================================

    public void StopPlayback()
    {
        isPaused = false;

        isPlaying = false;

        pauseStartedAt = 0.0;

        accumulatedPause = 0.0;


        StopAllCoroutines();


        ClearActiveSources();

        ClearPreparedNotes();


        if (printDebug)
        {
            Debug.Log(
                "DrumPlayer → DETENIDO"
            );
        }
    }


    // ==========================================
    // LIMPIAR FUENTES DE AUDIO
    // ==========================================

    private void ClearActiveSources()
    {
        foreach (
            AudioSource source
            in activeSources
        )
        {
            if (source == null)
            {
                continue;
            }


            source.Stop();

            source.clip = null;


            Destroy(
                source.gameObject
            );
        }


        activeSources.Clear();
    }


    // ==========================================
    // LIMPIAR EVENTOS Y CLIPS
    // ==========================================

    private void ClearPreparedNotes()
    {
        scheduledNotes.Clear();


        foreach (
            AudioClip clip
            in generatedClips
        )
        {
            if (clip != null)
            {
                Destroy(clip);
            }
        }


        generatedClips.Clear();


        preparedDuration = 0f;

        isPrepared = false;
    }


    // ==========================================
    // CICLO DE VIDA
    // ==========================================

    private void OnDisable()
    {
        StopPlayback();
    }


    private void OnDestroy()
    {
        StopPlayback();
    }
    // ==========================================
    // EVENTOS PARA EXPORTACION WAV
    // ==========================================

    public List<AudioRenderEvent> GetRenderEvents()
    {
        List<AudioRenderEvent> result =
            new List<AudioRenderEvent>();


        foreach (var note in scheduledNotes)
        {
            if (note.clip == null)
            {
                continue;
            }


            result.Add(
                new AudioRenderEvent(
                    note.clip,
                    note.position,
                    volume
                )
            );
        }


        return result;
    }


    // ==========================================
    // DURACION PARA EXPORTACION
    // ==========================================

    public float GetRenderDuration()
    {
        return preparedDuration;
    }
}