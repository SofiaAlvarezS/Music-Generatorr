using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GuitarGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public MusicXMLReader reader;
    public AudioSource audioSource;
    public GuitarSynthesizer synthesizer;
    public ProceduralPreviewController ui;

    [Header("Coordinacion musical")]
    public MusicalCoordinator coordinator;

    [Header("Reproduccion")]
    [Range(0f, 1f)]
    public float volume = 0.35f;

    [Range(0.1f, 1f)]
    public float noteLength = 0.75f;

    [Header("Registro musical")]
    public int minimumMidi = 52;
    public int maximumMidi = 76;

    [Header("Expresion musical")]
    [Range(0f, 1f)]
    public float expression = 0.25f;

    [Range(0f, 1f)]
    public float harmonicThreshold = 0.12f;

    [Header("Depuracion")]
    public bool printAnalysis = true;

    // ==========================================
    // ESTILOS DE GENERACION
    // ==========================================

    private enum GuitarStyle
    {
        Silence,
        Arpeggio,
        Response,
        Rhythmic,
        Minimal
    }

    // ==========================================
    // ESTRUCTURA DE EVENTOS MUSICALES
    // ==========================================

    private struct Event
    {
        public int midi;
        public float start;
        public float duration;
        public float velocity;
        public AudioClip clip;
    }

    private readonly List<Event> events =
        new List<Event>();

    private readonly Dictionary<Vector2Int, AudioClip> clips =
        new Dictionary<Vector2Int, AudioClip>();

    private MusicalAnalyzer.AnalysisResult analysis;

    private MusicData generatedFrom;

    private Coroutine playbackCoroutine;

    private int previousMidi = -1;

    private float generatedDuration;

    // ==========================================
    // INICIALIZACION
    // ==========================================

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    // ==========================================
    // GENERACION PRINCIPAL
    // ==========================================

    public void GenerateGuitar()
    {
        ClearGenerated();

        try
        {
            if (ui == null)
            {
                throw new System.Exception(
                    "Asigna la UI en GuitarGenerator"
                );
            }

            if (!ui.Selected().Contains("Guitarra"))
            {
                return;
            }

            if (reader == null ||
                reader != ui.reader ||
                !reader.isReady ||
                reader.musicData == null)
            {
                throw new System.Exception(
                    "Carga una partitura valida"
                );
            }

            if (synthesizer == null)
            {
                throw new System.Exception(
                    "Asigna GuitarSynthesizer"
                );
            }

            if (minimumMidi < 0 ||
                maximumMidi > 127 ||
                minimumMidi > maximumMidi)
            {
                throw new System.Exception(
                    "Registro MIDI invalido"
                );
            }

            // Analizar la partitura original.

            analysis = MusicalAnalyzer.Analyze(
                reader.musicData
            );

            if (!analysis.hasPitchInformation)
            {
                throw new System.Exception(
                    "La partitura no contiene informacion tonal"
                );
            }

            // Calcular duracion del pulso.

            float pulse =
                60f / analysis.tempo *
                4f / analysis.beatType;

            if (float.IsNaN(pulse) ||
                float.IsInfinity(pulse) ||
                pulse <= 0f ||
                pulse > 10f ||
                (long)analysis.beats *
                analysis.measures.Count > 10000)
            {
                throw new System.Exception(
                    "Tempo o tamaño fuera del rango permitido"
                );
            }

            if (printAnalysis)
            {
                MusicalAnalyzer.PrintAnalysis(analysis);
            }

            previousMidi = -1;

            // Generar acompañamiento por compases.

            foreach (var measure in analysis.measures)
            {
                GuitarStyle style =
                    SelectStyle(measure);

                GenerateMeasure(
                    measure,
                    style,
                    pulse
                );

                if (printAnalysis)
                {
                    Debug.Log(
                        "Guitarra | Compas " +
                        measure.number +
                        " | Acorde: " +
                        measure.ChordName +
                        " | Estilo: " +
                        style +
                        " | Densidad: " +
                        measure.density.ToString("F2")
                    );
                }
            }

            // Ordenar eventos cronologicamente.

            events.Sort(
                (a, b) => a.start.CompareTo(b.start)
            );

            if (events.Count == 0)
            {
                throw new System.Exception(
                    "No se generaron notas de guitarra"
                );
            }

            // Confirmar generacion.

            generatedFrom = reader.musicData;

            generatedDuration =
                analysis.totalDuration;

            ui.CompleteGeneration();

            ui.status.text =
                "Guitarra adaptativa: " +
                events.Count + " notas";

            Debug.Log(
                "Guitarra procedural generada: " +
                events.Count + " eventos"
            );
        }
        catch (System.Exception e)
        {
            ClearGenerated();

            if (ui != null)
            {
                ui.FailGeneration();
                ui.status.text = e.Message;
            }

            Debug.LogError(e.Message, this);
        }
    }

    // ==========================================
    // SELECCION DEL ESTILO MUSICAL
    // ==========================================

    private GuitarStyle SelectStyle(
        MusicalAnalyzer.MeasureAnalysis measure)
    {
        // Respetar compases completamente silenciosos.

        if (measure.isSilent)
        {
            return GuitarStyle.Silence;
        }

        // Aprovechar espacios de la melodia original.

        if (measure.silenceRatio >= 0.25f)
        {
            return GuitarStyle.Response;
        }

        // Arpegios cuando la base tiene poca actividad.

        if (measure.density <= 0.75f)
        {
            return GuitarStyle.Arpeggio;
        }

        // Acompañamiento ritmico para densidad media.

        if (measure.density <= 2f)
        {
            return GuitarStyle.Rhythmic;
        }

        // Evitar saturar una base muy activa.

        return GuitarStyle.Minimal;
    }

    // ==========================================
    // GENERACION POR COMPAS
    // ==========================================

    private void GenerateMeasure(
        MusicalAnalyzer.MeasureAnalysis measure,
        GuitarStyle style,
        float pulse)
    {
        switch (style)
        {
            case GuitarStyle.Silence:

                return;

            case GuitarStyle.Arpeggio:

                GenerateArpeggio(measure, pulse);
                break;

            case GuitarStyle.Response:

                GenerateResponse(measure, pulse);
                break;

            case GuitarStyle.Rhythmic:

                GenerateRhythmic(measure, pulse);
                break;

            case GuitarStyle.Minimal:

                GenerateMinimal(measure, pulse);
                break;
        }
    }

    // ==========================================
    // ESTILO ARPEGIO
    // ==========================================

    private void GenerateArpeggio(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        // Grados:
        // 0 = fundamental
        // 1 = tercera
        // 2 = quinta

        int[] patternA = { 0, 1, 2, 1 };
        int[] patternB = { 0, 2, 1, 2 };

        int[] pattern =
            measure.number % 2 == 0
            ? patternB
            : patternA;

        float step = pulse * 0.5f;

        int index = 0;

        for (float position = 0f;
             position < measure.duration;
             position += step)
        {
            int degree =
                pattern[index % pattern.Length];

            float velocity =
                index % 4 == 0 ? 0.85f : 0.65f;

            AddNote(
                measure,
                position,
                step * noteLength,
                degree,
                velocity
            );

            index++;
        }
    }

    // ==========================================
    // ESTILO RESPUESTA
    // ==========================================

    private void GenerateResponse(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        float step = pulse * 0.5f;

        int[] pattern = { 0, 2, 1, 2 };

        int index = 0;

        int initialCount = events.Count;

        for (float position = 0f;
             position < measure.duration;
             position += step)
        {
            float duration =
                step * noteLength;

            // Buscar espacios libres en la base.

            if (!IsSpaceAvailable(
                measure,
                position,
                duration))
            {
                continue;
            }

            int degree =
                pattern[index % pattern.Length];

            AddNote(
                measure,
                position,
                duration,
                degree,
                0.75f
            );

            index++;
        }

        // Si no existen espacios suficientes,
        // utilizar un acompañamiento ritmico.

        if (events.Count == initialCount)
        {
            GenerateRhythmic(measure, pulse);
        }
    }

    // ==========================================
    // ESTILO RITMICO
    // ==========================================

    private void GenerateRhythmic(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        int beatStep =
            measure.density > 1.5f ? 2 : 1;

        int[] pattern = { 0, 2, 1, 2 };

        int index = 0;

        for (int beat = 0;
             beat < analysis.beats;
             beat += beatStep)
        {
            float position =
                beat * pulse;

            // Evitar algunos ataques simultaneos
            // con la melodia original.

            if (HasNearbyAttack(
                measure,
                position,
                pulse * 0.12f))
            {
                float alternative =
                    position + pulse * 0.5f;

                if (alternative < measure.duration &&
                    !HasNearbyAttack(
                        measure,
                        alternative,
                        pulse * 0.12f))
                {
                    position = alternative;
                }
            }

            int degree =
                pattern[index % pattern.Length];

            float velocity =
                beat == 0 ? 0.85f : 0.65f;

            AddNote(
                measure,
                position,
                pulse * noteLength,
                degree,
                velocity
            );

            index++;
        }
    }

    // ==========================================
    // ESTILO MINIMALISTA
    // ==========================================

    private void GenerateMinimal(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        // Primera nota del compas.

        float firstPosition = 0f;

        if (HasNearbyAttack(
            measure,
            firstPosition,
            pulse * 0.12f))
        {
            firstPosition = pulse * 0.5f;
        }

        AddNote(
            measure,
            firstPosition,
            pulse * noteLength,
            0,
            0.6f
        );

        // Segunda nota en la mitad del compas.

        float secondPosition =
            measure.duration * 0.5f;

        if (secondPosition < measure.duration &&
            secondPosition > firstPosition + 0.025f)
        {
            AddNote(
                measure,
                secondPosition,
                pulse * noteLength,
                2,
                0.55f
            );
        }
    }

    // ==========================================
    // SELECCION ARMONICA
    // ==========================================

    private int SelectPitch(
        MusicalAnalyzer.MeasureAnalysis measure,
        int degree,
        float position)
    {
        int root =
            ((measure.chordRoot % 12) + 12) % 12;

        int third =
            measure.chordMinor ? 3 : 4;

        // Evitar terceras cuando la armonia
        // detectada tiene poca confianza.

        if (degree == 1 &&
            measure.harmonicConfidence <
            harmonicThreshold)
        {
            degree = 2;
        }

        int interval;

        switch (degree)
        {
            case 1:

                interval = third;
                break;

            case 2:

                interval = 7;
                break;

            default:

                interval = 0;
                break;
        }

        int pitchClass =
            (root + interval) % 12;

        int targetMidi =
            previousMidi >= 0
            ? previousMidi
            : 60;

        int bestMidi = -1;

        float bestScore =
            float.PositiveInfinity;

        // Buscar la mejor octava dentro
        // del registro de la guitarra.

        for (int midi = minimumMidi;
             midi <= maximumMidi;
             midi++)
        {
            if (midi % 12 != pitchClass)
            {
                continue;
            }

            // Favorecer movimientos melodicos cortos.

            float score =
                Mathf.Abs(midi - targetMidi);

            // Evitar repetir excesivamente
            // la misma nota.

            if (midi == previousMidi)
            {
                score += 2f;
            }

            // Mantener cierta separacion
            // respecto al registro de la base.

            if (Mathf.Abs(
                midi - measure.averageMidi
            ) < 5f)
            {
                score += 3f;
            }

            // Evaluar notas simultaneas
            // de la melodia original.

            foreach (var note in measure.notes)
            {
                if (note.isRest)
                {
                    continue;
                }

                bool sounding =
                    position >= note.start &&
                    position < note.start + note.duration;

                if (!sounding)
                {
                    continue;
                }

                int distance =
                    Mathf.Abs(midi - note.midi);

                int intervalClass =
                    distance % 12;

                // Penalizar disonancias cercanas.

                if (intervalClass == 1 ||
                    intervalClass == 11)
                {
                    score += 2.5f;
                }

                if (distance <= 2)
                {
                    score += 3f;
                }
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestMidi = midi;
            }
        }

        return bestMidi;
    }

    // ==========================================
    // DETECCION DE ATAQUES DE LA BASE
    // ==========================================

    private bool HasNearbyAttack(
        MusicalAnalyzer.MeasureAnalysis measure,
        float position,
        float tolerance)
    {
        foreach (var note in measure.notes)
        {
            if (note.isRest)
            {
                continue;
            }

            if (Mathf.Abs(
                note.start - position
            ) <= tolerance)
            {
                return true;
            }
        }

        return false;
    }

    // ==========================================
    // DETECCION DE ESPACIOS MUSICALES
    // ==========================================

    private bool IsSpaceAvailable(
        MusicalAnalyzer.MeasureAnalysis measure,
        float position,
        float duration)
    {
        if (position < 0f ||
            position + duration > measure.duration)
        {
            return false;
        }

        float end =
            position + duration;

        foreach (var note in measure.notes)
        {
            if (note.isRest)
            {
                continue;
            }

            float noteStart =
                note.start;

            float noteEnd =
                note.start + note.duration;

            bool overlaps =
                position < noteEnd &&
                end > noteStart;

            if (overlaps)
            {
                return false;
            }
        }

        return true;
    }

    // ==========================================
    // DETECTAR ATAQUES PROPIOS
    // ==========================================

    private bool HasOwnNearbyAttack(
        float absolutePosition,
        float tolerance)
    {
        foreach (Event note in events)
        {
            if (Mathf.Abs(
                note.start - absolutePosition
            ) < tolerance)
            {
                return true;
            }
        }

        return false;
    }

    // ==========================================
    // CREACION DE EVENTOS MUSICALES
    // ==========================================

    private void AddNote(
        MusicalAnalyzer.MeasureAnalysis measure,
        float position,
        float duration,
        int degree,
        float velocity)
    {
        // Validar posicion inicial.

        if (position < 0f ||
            position >= measure.duration)
        {
            return;
        }

        duration = Mathf.Min(
            duration,
            measure.duration - position
        );

        if (duration < 0.03f)
        {
            return;
        }

        // ======================================
        // NUEVO: COORDINACION RITMICA
        // ======================================

        if (coordinator != null &&
            coordinator.enableCoordination)
        {
            float pulse =
                60f / analysis.tempo *
                4f / analysis.beatType;

            float absolutePosition =
                measure.startTime + position;

            float adjustedPosition =
                coordinator.AdjustGuitarPosition(
                    absolutePosition,
                    measure.startTime,
                    measure.startTime +
                    measure.duration,
                    pulse
                );

            float localPosition =
                adjustedPosition - measure.startTime;

            // Comprobar si la nota original
            // estaba aprovechando un silencio.

            bool originallyInGap =
                IsSpaceAvailable(
                    measure,
                    position,
                    duration
                );

            bool validAdjustment =
                localPosition >= 0f &&
                localPosition < measure.duration;

            if (validAdjustment &&
                originallyInGap)
            {
                // Conservar los espacios musicales
                // encontrados por el generador.

                validAdjustment =
                    IsSpaceAvailable(
                        measure,
                        localPosition,
                        duration
                    );
            }

            if (validAdjustment)
            {
                // Evitar duplicar ataques propios.

                validAdjustment =
                    !HasOwnNearbyAttack(
                        adjustedPosition,
                        0.025f
                    );
            }

            if (validAdjustment)
            {
                position = localPosition;
            }
        }

        // ======================================
        // VALIDACION DEFINITIVA
        // ======================================

        if (position < 0f ||
            position >= measure.duration)
        {
            return;
        }

        duration = Mathf.Min(
            duration,
            measure.duration - position
        );

        if (duration < 0.03f)
        {
            return;
        }

        float finalPosition =
            measure.startTime + position;

        if (HasOwnNearbyAttack(
            finalPosition,
            0.025f))
        {
            return;
        }

        // ======================================
        // SELECCION DE NOTA ARMONICA
        // ======================================

        int midi = SelectPitch(
            measure,
            degree,
            position
        );

        if (midi < 0)
        {
            return;
        }

        // ======================================
        // EXPRESION MUSICAL
        // ======================================

        velocity += Mathf.Sin(
            measure.number * 1.73f +
            position * 4.21f
        ) * expression * 0.2f;

        velocity =
            Mathf.Clamp01(velocity);

        // ======================================
        // NUEVO: EQUILIBRIO DINAMICO
        // ======================================

        if (coordinator != null)
        {
            velocity =
                coordinator.AdjustGuitarVelocity(
                    finalPosition,
                    velocity
                );
        }

        // ======================================
        // REUTILIZACION DE CLIPS
        // ======================================

        int durationMs = Mathf.Max(
            1,
            Mathf.RoundToInt(
                duration * 1000f
            )
        );

        Vector2Int key =
            new Vector2Int(
                midi,
                durationMs
            );

        if (!clips.TryGetValue(
            key,
            out AudioClip clip))
        {
            float frequency =
                MusicalAnalyzer.MidiToFrequency(
                    midi
                );

            clip = synthesizer.GenerateNote(
                frequency,
                durationMs / 1000f
            );

            if (clip == null)
            {
                throw new System.Exception(
                    "No se pudo sintetizar la nota " +
                    midi
                );
            }

            clips.Add(key, clip);
        }

        // ======================================
        // REGISTRAR EVENTO MUSICAL
        // ======================================

        events.Add(new Event
        {
            midi = midi,
            start = finalPosition,
            duration = duration,
            velocity = velocity,
            clip = clip
        });

        previousMidi = midi;

        // ======================================
        // NUEVO: REGISTRAR EN COORDINADOR
        // ======================================

        if (coordinator != null)
        {
            coordinator.RegisterGuitarNote(
                finalPosition
            );
        }
    }

    // ==========================================
    // VALIDACION DE GENERACION
    // ==========================================

    private bool CanPlay()
    {
        return ui != null &&
               ui.HasGenerated &&
               ui.Selected().Contains("Guitarra") &&
               reader != null &&
               reader.isReady &&
               reader.musicData == generatedFrom &&
               events.Count > 0;
    }

    public bool HasValidGeneration()
    {
        return CanPlay();
    }

    // ==========================================
    // REPRODUCCION INDIVIDUAL
    // ==========================================

    public void PlayGuitar()
    {
        if (!CanPlay())
        {
            Debug.LogWarning(
                "Genera primero la guitarra procedural"
            );

            return;
        }

        PlayScheduled(
            AudioSettings.dspTime + 0.15
        );
    }

    // ==========================================
    // REPRODUCCION CON INSTRUMENTO ORIGINAL
    // ==========================================

    public void PlayTogether()
    {
        if (!CanPlay() ||
            ui.playback == null)
        {
            return;
        }

        StopGuitar();

        ui.playback.SetPlaybackType(
            (PlaybackManager.PlaybackType)
            ui.OriginalIndex
        );

        if (ui.OriginalIndex == 2)
        {
            ui.playback.Play();

            PlayScheduled(
                AudioSettings.dspTime
            );

            return;
        }

        if (!ui.playback.PrepareSynchronizedPlayback())
        {
            Debug.LogError(
                "No se pudo preparar la base musical"
            );

            return;
        }

        double startTime =
            AudioSettings.dspTime + 0.15;

        ui.playback.PlaySynchronized(startTime);

        PlayScheduled(startTime);
    }

    // ==========================================
    // REPRODUCCION SINCRONIZADA
    // ==========================================

    public void PlayScheduled(double startTime)
    {
        if (!CanPlay())
        {
            return;
        }

        StopGuitar();

        playbackCoroutine = StartCoroutine(
            PlayAccompaniment(startTime)
        );
    }

    // ==========================================
    // REPRODUCCION DE EVENTOS
    // ==========================================

    private IEnumerator PlayAccompaniment(
        double startTime)
    {
        double endTime =
            startTime + generatedDuration;

        foreach (Event note in events)
        {
            // Esperar la posicion absoluta
            // utilizando el reloj DSP.

            while (
                AudioSettings.dspTime <
                startTime + note.start)
            {
                yield return null;
            }

            // PlayOneShot permite conservar
            // las colas de las notas anteriores.

            audioSource.PlayOneShot(
                note.clip,
                volume * note.velocity
            );

            // Considerar la duracion completa
            // del clip sintetizado.

            endTime = System.Math.Max(
                endTime,
                startTime +
                note.start +
                note.clip.length
            );
        }

        // Esperar el final del acompañamiento.

        while (AudioSettings.dspTime < endTime)
        {
            yield return null;
        }

        playbackCoroutine = null;
    }

    // ==========================================
    // DETENER REPRODUCCION
    // ==========================================

    public void StopGuitar()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    // ==========================================
    // LIMPIAR GENERACION
    // ==========================================

    private void ClearGenerated()
    {
        StopGuitar();

        events.Clear();

        generatedFrom = null;
        analysis = null;

        previousMidi = -1;
        generatedDuration = 0f;

        foreach (AudioClip clip in clips.Values)
        {
            if (clip != null)
            {
                Destroy(clip);
            }
        }

        clips.Clear();
    }

    // ==========================================
    // CICLO DE VIDA
    // ==========================================

    private void OnDisable()
    {
        StopGuitar();
    }

    private void OnDestroy()
    {
        ClearGenerated();
    }
    public List<AudioRenderEvent> GetRenderEvents()
    {
        List<AudioRenderEvent> result =
            new List<AudioRenderEvent>();

        if (!HasValidGeneration())
        {
            return result;
        }

        foreach (var note in events)
        {
            if (note.clip == null)
            {
                continue;
            }

            float finalVolume =
                volume * note.velocity;

            result.Add(
                new AudioRenderEvent(
                    note.clip,
                    note.start,
                    finalVolume
                )
            );
        }

        return result;
    }


    public float GetRenderDuration()
    {
        return generatedDuration;
    }
}