using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BassGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public MusicXMLReader reader;
    public AudioSource audioSource;
    public BassSynthesizer synthesizer;
    public ProceduralPreviewController ui;

    [Header("Coordinacion musical")]
    public MusicalCoordinator coordinator;

    [Header("Reproduccion")]
    [Range(0f, 1f)]
    public float volume = 0.45f;

    [Range(0.1f, 1f)]
    public float noteLength = 0.85f;

    [Header("Registro musical")]
    [Range(28, 48)]
    public int minimumMidi = 28;

    [Range(40, 64)]
    public int maximumMidi = 52;

    [Header("Expresion musical")]
    [Range(0f, 1f)]
    public float expression = 0.25f;

    [Range(0f, 1f)]
    public float harmonicThreshold = 0.12f;

    public bool usePassingNotes = true;

    [Header("Depuracion")]
    public bool printAnalysis = true;

    // ==========================================
    // ESTILOS DE GENERACION
    // ==========================================

    private enum BassStyle
    {
        Silence,
        Melodic,
        Sustained,
        Groove,
        Response
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

    public void GenerateBass()
    {
        ClearGenerated();

        try
        {
            if (ui == null)
            {
                throw new System.Exception(
                    "Asigna la UI en BassGenerator"
                );
            }

            if (!ui.Selected().Contains("Bajo"))
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
                    "Asigna BassSynthesizer"
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

            for (int i = 0;
                 i < analysis.measures.Count;
                 i++)
            {
                var measure = analysis.measures[i];

                var next =
                    i + 1 < analysis.measures.Count
                    ? analysis.measures[i + 1]
                    : null;

                BassStyle style =
                    SelectStyle(measure);

                GenerateMeasure(
                    measure,
                    next,
                    style,
                    pulse
                );

                if (printAnalysis)
                {
                    Debug.Log(
                        "Bajo | Compas " +
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
                    "No se generaron notas de bajo"
                );
            }

            // Confirmar generacion.

            generatedFrom = reader.musicData;

            generatedDuration =
                analysis.totalDuration;

            ui.CompleteGeneration();

            ui.status.text =
                "Bajo adaptativo: " +
                events.Count +
                " notas | " +
                analysis.KeyName;

        

            Debug.Log(
                "Bajo procedural generado: " +
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

    private BassStyle SelectStyle(
        MusicalAnalyzer.MeasureAnalysis measure)
    {
        // Respetar compases silenciosos.

        if (measure.isSilent)
        {
            return BassStyle.Silence;
        }

        // Aprovechar espacios de la base.

        if (measure.silenceRatio >= 0.30f)
        {
            return BassStyle.Response;
        }

        // Mayor libertad melodica cuando
        // la base tiene pocas notas.

        if (measure.density <= 0.75f)
        {
            return BassStyle.Melodic;
        }

        // Mantener estabilidad armonica
        // cuando la base es muy activa.

        if (measure.density > 2f)
        {
            return BassStyle.Sustained;
        }

        return BassStyle.Groove;
    }

    // ==========================================
    // GENERACION POR COMPAS
    // ==========================================

    private void GenerateMeasure(
        MusicalAnalyzer.MeasureAnalysis measure,
        MusicalAnalyzer.MeasureAnalysis next,
        BassStyle style,
        float pulse)
    {
        switch (style)
        {
            case BassStyle.Silence:

                return;

            case BassStyle.Melodic:

                GenerateMelodic(
                    measure,
                    next,
                    pulse
                );

                break;

            case BassStyle.Sustained:

                GenerateSustained(
                    measure,
                    pulse
                );

                break;

            case BassStyle.Groove:

                GenerateGroove(
                    measure,
                    next,
                    pulse
                );

                break;

            case BassStyle.Response:

                GenerateResponse(
                    measure,
                    pulse
                );

                break;
        }
    }

    // ==========================================
    // ESTILO MELODICO
    // ==========================================

    private void GenerateMelodic(
        MusicalAnalyzer.MeasureAnalysis measure,
        MusicalAnalyzer.MeasureAnalysis next,
        float pulse)
    {
        // Grados:
        // 0 = fundamental
        // 1 = tercera
        // 2 = quinta

        int[] patternA = { 0, 2, 1, 2 };
        int[] patternB = { 0, 1, 2, 0 };

        int[] pattern =
            measure.number % 2 == 0
            ? patternB
            : patternA;

        for (int beat = 0;
             beat < analysis.beats;
             beat++)
        {
            float position =
                beat * pulse;

            int degree =
                pattern[beat % pattern.Length];

            int pitchClass =
                GetChordPitchClass(
                    measure,
                    degree
                );

            bool lastBeat =
                beat == analysis.beats - 1;

            // Preparar el siguiente acorde
            // mediante una nota de transicion.

            if (lastBeat &&
                CanUsePassingNote(measure, next))
            {
                pitchClass =
                    GetPassingPitchClass(next);
            }

            float velocity =
                beat == 0 ? 0.9f : 0.7f;

            AddNote(
                measure,
                position,
                pulse * noteLength,
                pitchClass,
                velocity
            );
        }
    }

    // ==========================================
    // ESTILO SOSTENIDO
    // ==========================================

    private void GenerateSustained(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        float step = pulse * 2f;

        int index = 0;

        for (float position = 0f;
             position < measure.duration - 0.0001f;
             position += step)
        {
            int pitchClass =
                GetChordPitchClass(
                    measure,
                    0
                );

            int fifth =
                NormalizePitchClass(
                    measure.chordRoot + 7
                );

            // Utilizar la quinta solamente
            // cuando existe suficiente evidencia
            // armonica en la base.

            if (index % 2 == 1 &&
                measure.harmonicConfidence >=
                harmonicThreshold &&
                measure.pitchWeights[fifth] > 0f)
            {
                pitchClass = fifth;
            }

            float velocity =
                index == 0 ? 0.85f : 0.7f;

            AddNote(
                measure,
                position,
                step * noteLength,
                pitchClass,
                velocity
            );

            index++;
        }
    }

    // ==========================================
    // ESTILO GROOVE
    // ==========================================

    private void GenerateGroove(
        MusicalAnalyzer.MeasureAnalysis measure,
        MusicalAnalyzer.MeasureAnalysis next,
        float pulse)
    {
        for (int beat = 0;
             beat < analysis.beats;
             beat++)
        {
            float position =
                beat * pulse;

            int degree =
                beat % 2 == 0 ? 0 : 2;

            int pitchClass =
                GetChordPitchClass(
                    measure,
                    degree
                );

            // Adaptar el bajo a una base sincopada.

            if (measure.syncopation >= 0.25f &&
                beat % 2 == 1)
            {
                float offbeat =
                    position + pulse * 0.5f;

                if (offbeat < measure.duration &&
                    HasNearbyAttack(
                        measure,
                        offbeat,
                        pulse * 0.12f))
                {
                    position = offbeat;
                }
            }

            // Preparar el siguiente compas.

            if (beat == analysis.beats - 1 &&
                CanUsePassingNote(measure, next))
            {
                pitchClass =
                    GetPassingPitchClass(next);
            }

            float velocity =
                beat % 2 == 0 ? 0.85f : 0.65f;

            AddNote(
                measure,
                position,
                pulse * noteLength,
                pitchClass,
                velocity
            );
        }
    }

    // ==========================================
    // ESTILO RESPUESTA
    // ==========================================

    private void GenerateResponse(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        int root =
            GetChordPitchClass(
                measure,
                0
            );

        // Reforzar el primer ataque de la base.

        foreach (var note in measure.notes)
        {
            if (note.isRest)
            {
                continue;
            }

            AddNote(
                measure,
                note.start,
                Mathf.Min(
                    pulse * noteLength,
                    note.duration
                ),
                root,
                0.85f
            );

            break;
        }

        float step = pulse * 0.5f;

        int index = 0;

        // Buscar espacios libres para responder
        // a la melodia original.

        for (float position = 0f;
             position < measure.duration - 0.0001f;
             position += step)
        {
            float availableSpace =
                GetAvailableSpace(
                    measure,
                    position
                );

            if (availableSpace < pulse * 0.2f)
            {
                continue;
            }

            int degree =
                index % 2 == 0 ? 0 : 2;

            int pitchClass =
                GetChordPitchClass(
                    measure,
                    degree
                );

            float duration =
                Mathf.Min(
                    step * noteLength,
                    availableSpace
                );

            AddNote(
                measure,
                position,
                duration,
                pitchClass,
                0.65f
            );

            index++;
        }
    }

    // ==========================================
    // NORMALIZAR CLASE DE ALTURA
    // ==========================================

    private int NormalizePitchClass(int pitch)
    {
        return ((pitch % 12) + 12) % 12;
    }

    // ==========================================
    // SELECCION DE NOTAS DEL ACORDE
    // ==========================================

    private int GetChordPitchClass(
        MusicalAnalyzer.MeasureAnalysis measure,
        int degree)
    {
        int root =
            NormalizePitchClass(
                measure.chordRoot
            );

        int third =
            measure.chordMinor ? 3 : 4;

        // Evitar terceras cuando la armonia
        // tiene poca confianza.

        if (degree == 1 &&
            measure.harmonicConfidence <
            harmonicThreshold)
        {
            degree = 2;
        }

        switch (degree)
        {
            case 1:

                return NormalizePitchClass(
                    root + third
                );

            case 2:

                return NormalizePitchClass(
                    root + 7
                );

            default:

                return root;
        }
    }

    // ==========================================
    // VALIDACION DE NOTAS DE TRANSICION
    // ==========================================

    private bool CanUsePassingNote(
        MusicalAnalyzer.MeasureAnalysis measure,
        MusicalAnalyzer.MeasureAnalysis next)
    {
        if (!usePassingNotes ||
            next == null ||
            next.isSilent ||
            !analysis.hasPitchInformation)
        {
            return false;
        }

        if (measure.chordRoot == next.chordRoot)
        {
            return false;
        }

        if (measure.harmonicConfidence <
            harmonicThreshold ||
            next.harmonicConfidence <
            harmonicThreshold)
        {
            return false;
        }

        return true;
    }

    // ==========================================
    // SELECCION DE NOTAS DE TRANSICION
    // ==========================================

    private int GetPassingPitchClass(
        MusicalAnalyzer.MeasureAnalysis next)
    {
        int nextRoot =
            NormalizePitchClass(
                next.chordRoot
            );

        // Aproximacion por semitono inferior.

        int candidateA =
            NormalizePitchClass(
                nextRoot - 1
            );

        // Aproximacion por tono inferior.

        int candidateB =
            NormalizePitchClass(
                nextRoot - 2
            );

        // Priorizar notas de la escala global.

        if (MusicalAnalyzer.IsInScale(
            candidateA,
            analysis.tonic,
            analysis.isMinor))
        {
            return candidateA;
        }

        if (MusicalAnalyzer.IsInScale(
            candidateB,
            analysis.tonic,
            analysis.isMinor))
        {
            return candidateB;
        }

        // Alternativa: quinta del siguiente acorde.

        return NormalizePitchClass(
            nextRoot + 7
        );
    }

    // ==========================================
    // SELECCION DEL REGISTRO MIDI
    // ==========================================

    private int SelectPitch(
        MusicalAnalyzer.MeasureAnalysis measure,
        int pitchClass)
    {
        pitchClass =
            NormalizePitchClass(pitchClass);

        int targetMidi =
            previousMidi >= 0
            ? previousMidi
            : 36;

        int bestMidi = -1;

        float bestScore =
            float.PositiveInfinity;

        for (int midi = minimumMidi;
             midi <= maximumMidi;
             midi++)
        {
            if (midi % 12 != pitchClass)
            {
                continue;
            }

            // Favorecer movimientos cortos
            // entre notas consecutivas.

            float score =
                Mathf.Abs(midi - targetMidi);

            // Favorecer el registro grave.

            score +=
                Mathf.Abs(midi - 36) * 0.15f;

            // Mantener cierta separacion
            // respecto al instrumento original.

            if (measure.averageMidi >= 0f)
            {
                float distance =
                    Mathf.Abs(
                        midi - measure.averageMidi
                    );

                if (distance < 12f)
                {
                    score += 2f;
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

    private float GetAvailableSpace(
        MusicalAnalyzer.MeasureAnalysis measure,
        float position)
    {
        if (position < 0f ||
            position >= measure.duration)
        {
            return 0f;
        }

        float available =
            measure.duration - position;

        foreach (var note in measure.notes)
        {
            if (note.isRest)
            {
                continue;
            }

            float noteEnd =
                note.start + note.duration;

            // La base esta sonando.

            if (position >= note.start &&
                position < noteEnd)
            {
                return 0f;
            }

            // Calcular distancia hasta
            // el siguiente ataque.

            if (note.start > position)
            {
                available = Mathf.Min(
                    available,
                    note.start - position
                );
            }
        }

        return Mathf.Max(0f, available);
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
        int pitchClass,
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
        // NUEVO: COORDINACION CON EL BOMBO
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
                coordinator.AdjustBassPosition(
                    absolutePosition,
                    measure.startTime,
                    measure.startTime +
                    measure.duration,
                    pulse
                );

            float localPosition =
                adjustedPosition - measure.startTime;

            // Comprobar si la nota original
            // aprovechaba un silencio de la base.

            bool originallyInGap =
                GetAvailableSpace(
                    measure,
                    position
                ) >= duration - 0.0001f;

            bool validAdjustment =
                localPosition >= 0f &&
                localPosition < measure.duration;

            // Proteger los espacios musicales.

            if (validAdjustment &&
                originallyInGap)
            {
                validAdjustment =
                    GetAvailableSpace(
                        measure,
                        localPosition
                    ) >= duration - 0.0001f;
            }

            // Evitar ataques duplicados.

            if (validAdjustment)
            {
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
        // SELECCION DE NOTA MIDI
        // ======================================

        int midi =
            SelectPitch(
                measure,
                pitchClass
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
        // NUEVO: REFORZAR ACENTOS DEL BOMBO
        // ======================================

        if (coordinator != null)
        {
            velocity =
                coordinator.AdjustBassVelocity(
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
            coordinator.RegisterBassNote(
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
               ui.Selected().Contains("Bajo") &&
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

    public void PlayBass()
    {
        if (!CanPlay())
        {
            Debug.LogWarning(
                "Genera primero el bajo procedural"
            );

            return;
        }

        if (ui.playback != null)
        {
            ui.playback.Stop();
        }

        ui.onStop.Invoke();

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

        ui.onStop.Invoke();

        StopBass();

        ui.playback.SetPlaybackType(
            (PlaybackManager.PlaybackType)
            ui.OriginalIndex
        );

        // Compatibilidad con bateria original.

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

        StopBass();

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

            // Reproducir nota sintetizada.

            audioSource.PlayOneShot(
                note.clip,
                volume * note.velocity
            );

            // Considerar la duracion completa
            // del clip y sus colas.

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

    public void StopBass()
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
        StopBass();

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
        StopBass();
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