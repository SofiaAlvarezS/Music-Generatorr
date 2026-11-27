using System.Collections;
using System.Collections.Generic;
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
    public AudioMixerGroup playbackMixerGroup;

    private AudioSource audioSource;

    private bool isPaused = false;
    private bool isPlaying = false;

    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;

    // ==========================================
    // SINCRONIZACION DSP
    // ==========================================

    private bool synchronizedPlayback = false;

    private double pauseStartedAt;
    private double accumulatedPause;

    private float scheduledDuration;

    private struct ScheduledNote
    {
        public float position;
        public AudioClip clip;
    }

    private readonly List<ScheduledNote> scheduledNotes =
        new List<ScheduledNote>();

    private readonly Dictionary<Vector2Int, AudioClip> scheduledClips =
        new Dictionary<Vector2Int, AudioClip>();

    // ==========================================
    // INICIALIZACION
    // ==========================================

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
                "MusicPlayer no tiene asignado un MusicXMLReader."
            );
        }
    }

    // ==========================================
    // CONFIGURACION DE AUDIO
    // ==========================================

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
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = volume;

        if (playbackMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup =
                playbackMixerGroup;
        }
    }

    // ==========================================
    // REPRODUCCION ORIGINAL INDEPENDIENTE
    // ==========================================

    public void PlayPlayback()
    {
        StopPlayback();

        if (musicReader == null ||
            !musicReader.isReady ||
            musicReader.musicData == null)
        {
            Debug.LogWarning(
                "MusicPlayer: carga una partitura primero."
            );

            return;
        }

        EnsureAudioSource();
        ConfigureAudioSource();

        StartCoroutine(PlayMusic());
    }

    private IEnumerator WaitForMusicData()
    {
        while (!musicReader.isReady)
        {
            yield return null;
        }

        StartCoroutine(PlayMusic());
    }

    private IEnumerator PlayMusic()
    {
        isPlaying = true;
        isPaused = false;
        synchronizedPlayback = false;

        foreach (
            MeasureData measure
            in musicReader.musicData.measures
        )
        {
            foreach (NoteData note in measure.notes)
            {
                float duration =
                    GetDurationInSeconds(note);

                if (duration <= 0f)
                    continue;

                if (note.isRest)
                {
                    yield return WaitWithPause(duration);
                    continue;
                }

                PlayNote(note, duration);

                yield return WaitWithPause(duration);
            }
        }

        isPlaying = false;
    }

    private IEnumerator WaitWithPause(float duration)
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

    // ==========================================
    // SINTESIS DE NOTAS ORIGINALES
    // ==========================================

    private void PlayNote(
        NoteData note,
        float duration
    )
    {
        float frequency = GetFrequency(
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

                clip = guitarSynthesizer.GenerateNote(
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

                clip = bassSynthesizer.GenerateNote(
                    frequency,
                    duration
                );

                break;
        }

        if (clip == null)
            return;

        if (audioSource == null)
        {
            EnsureAudioSource();
            ConfigureAudioSource();
        }

        audioSource.clip = clip;
        audioSource.volume = volume;

        audioSource.Play();
    }

    // ==========================================
    // PREPARAR REPRODUCCION SINCRONIZADA
    // ==========================================

    public bool PrepareSynchronizedPlayback()
    {
        StopPlayback();

        if (musicReader == null ||
            !musicReader.isReady ||
            musicReader.musicData == null)
        {
            Debug.LogWarning(
                "No hay una partitura cargada."
            );

            return false;
        }

        if (instrument == InstrumentType.Guitar &&
            guitarSynthesizer == null)
        {
            Debug.LogError(
                "GuitarSynthesizer no asignado."
            );

            return false;
        }

        if (instrument == InstrumentType.Bass &&
            bassSynthesizer == null)
        {
            Debug.LogError(
                "BassSynthesizer no asignado."
            );

            return false;
        }

        EnsureAudioSource();
        ConfigureAudioSource();

        try
        {
            var analysis = MusicalAnalyzer.Analyze(
                musicReader.musicData
            );

            scheduledDuration =
                analysis.totalDuration;

            foreach (var measure in analysis.measures)
            {
                foreach (var note in measure.notes)
                {
                    if (note.isRest ||
                        note.duration <= 0f)
                    {
                        continue;
                    }

                    int durationMs = Mathf.Max(
                        1,
                        Mathf.RoundToInt(
                            note.duration * 1000f
                        )
                    );

                    Vector2Int key = new Vector2Int(
                        note.midi,
                        durationMs
                    );

                    if (!scheduledClips.TryGetValue(
                        key,
                        out AudioClip clip
                    ))
                    {
                        float frequency =
                            MusicalAnalyzer.MidiToFrequency(
                                note.midi
                            );

                        float duration =
                            durationMs / 1000f;

                        switch (instrument)
                        {
                            case InstrumentType.Guitar:

                                clip =
                                    guitarSynthesizer.GenerateNote(
                                        frequency,
                                        duration
                                    );

                                break;

                            case InstrumentType.Bass:

                                clip =
                                    bassSynthesizer.GenerateNote(
                                        frequency,
                                        duration
                                    );

                                break;
                        }

                        if (clip == null)
                        {
                            throw new System.Exception(
                                "No se pudo sintetizar una nota."
                            );
                        }

                        scheduledClips.Add(key, clip);
                    }

                    scheduledNotes.Add(
                        new ScheduledNote
                        {
                            position =
                                measure.startTime +
                                note.start,

                            clip = clip
                        }
                    );
                }
            }

            scheduledNotes.Sort(
                (a, b) =>
                    a.position.CompareTo(b.position)
            );

            Debug.Log(
                "MusicPlayer preparado | Instrumento: " +
                instrument +
                " | Notas: " +
                scheduledNotes.Count +
                " | Clips únicos: " +
                scheduledClips.Count +
                " | Duración: " +
                scheduledDuration.ToString("F2") +
                " segundos"
            );

            return true;
        }
        catch (System.Exception e)
        {
            ClearScheduledNotes();

            Debug.LogError(
                "Error preparando reproducción: " +
                e.Message
            );

            return false;
        }
    }

    // ==========================================
    // INICIAR REPRODUCCION SINCRONIZADA
    // ==========================================

    public void PlaySynchronized(double startTime)
    {
        EnsureAudioSource();
        ConfigureAudioSource();

        StopAllCoroutines();

        audioSource.Stop();
        audioSource.clip = null;

        synchronizedPlayback = true;

        isPlaying = true;
        isPaused = false;

        accumulatedPause = 0;
        pauseStartedAt = 0;

        StartCoroutine(
            PlaySynchronizedMusic(startTime)
        );
    }

    private IEnumerator PlaySynchronizedMusic(
        double startTime
    )
    {
        double endTime =
            startTime + scheduledDuration;

        foreach (ScheduledNote note in scheduledNotes)
        {
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

            audioSource.PlayOneShot(note.clip);

            endTime = System.Math.Max(
                endTime,
                startTime +
                note.position +
                note.clip.length
            );
        }

        // Esperar hasta finalizar la partitura
        // y las colas de los sonidos.

        while (
            isPaused ||
            AudioSettings.dspTime <
            endTime + accumulatedPause
        )
        {
            yield return null;
        }

        isPlaying = false;
        synchronizedPlayback = false;

        Debug.Log(
            "MusicPlayer: reproducción sincronizada finalizada."
        );
    }

    // ==========================================
    // PAUSAR REPRODUCCION
    // ==========================================

    public void PausePlayback()
    {
        if (!isPlaying || isPaused)
            return;

        isPaused = true;

        if (synchronizedPlayback)
        {
            pauseStartedAt =
                AudioSettings.dspTime;
        }

        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }

    // ==========================================
    // REANUDAR REPRODUCCION
    // ==========================================

    public void ResumePlayback()
    {
        if (!isPlaying || !isPaused)
            return;

        if (synchronizedPlayback)
        {
            accumulatedPause +=
                AudioSettings.dspTime -
                pauseStartedAt;
        }

        isPaused = false;

        if (audioSource != null)
        {
            audioSource.UnPause();
        }
    }

    // ==========================================
    // DETENER REPRODUCCION
    // ==========================================

    public void StopPlayback()
    {
        isPaused = false;
        isPlaying = false;

        synchronizedPlayback = false;

        accumulatedPause = 0;
        pauseStartedAt = 0;

        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        ClearScheduledNotes();
    }

    // ==========================================
    // LIMPIEZA DE CLIPS SINCRONIZADOS
    // ==========================================

    private void ClearScheduledNotes()
    {
        scheduledNotes.Clear();

        foreach (AudioClip clip in scheduledClips.Values)
        {
            if (clip != null)
            {
                Destroy(clip);
            }
        }

        scheduledClips.Clear();

        scheduledDuration = 0f;
    }

    // ==========================================
    // CONVERSION DE DURACION
    // ==========================================

    private float GetDurationInSeconds(NoteData note)
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

        return
            (float)note.duration /
            divisions *
            60f /
            tempo;
    }

    // ==========================================
    // CONVERSION DE NOTA A FRECUENCIA
    // ==========================================

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

        return 440f * Mathf.Pow(
            2f,
            (
                semitone +
                (octave - 4) * 12 +
                alter
            ) / 12f
        );
    }

    // ==========================================
    // LIMPIEZA FINAL
    // ==========================================

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
        return scheduledDuration;
    }
}