using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DrumGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public MusicXMLReader reader;
    public AudioSource audioSource;
    public DrumSynthesizer synthesizer;
    public ProceduralPreviewController ui;

    [Header("Coordinacion musical")]
    public MusicalCoordinator coordinator;

    [Header("Volumen general")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Header("Volumen por instrumento")]
    [Range(0f, 1f)]
    public float kickVolume = 1f;

    [Range(0f, 1f)]
    public float snareVolume = 0.8f;

    [Range(0f, 1f)]
    public float hiHatVolume = 0.45f;

    [Header("Generacion procedural")]
    [Range(0f, 1f)]
    public float complexity = 0.5f;

    [Range(0f, 1f)]
    public float expression = 0.25f;

    public bool useFills = true;
    public bool followBaseAttacks = true;
    public bool printAnalysis = true;

    // ==========================================
    // NOTAS MIDI DE PERCUSION
    // ==========================================

    private const int Kick = 36;
    private const int Snare = 38;
    private const int HiHat = 42;

    // ==========================================
    // ESTILOS DE GENERACION
    // ==========================================

    private enum DrumStyle
    {
        Silence,
        Soft,
        Groove,
        Driving,
        Syncopated,
        Response
    }

    // ==========================================
    // ESTRUCTURA DE EVENTOS
    // ==========================================

    private struct DrumEvent
    {
        public int midi;
        public float start;
        public float velocity;
        public AudioClip clip;
    }

    private readonly List<DrumEvent> events =
        new List<DrumEvent>();

    private readonly Dictionary<int, AudioClip> clips =
        new Dictionary<int, AudioClip>();

    private MusicalAnalyzer.AnalysisResult analysis;

    private MusicData generatedFrom;

    private Coroutine playbackCoroutine;

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

    public void GenerateDrums()
    {
        ClearGenerated();

        try
        {
            if (ui == null)
            {
                throw new System.Exception(
                    "Asigna la UI en DrumGenerator"
                );
            }

            if (!ui.Selected().Contains("Batería"))
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
                    "Asigna DrumSynthesizer"
                );
            }

            // Analizar la partitura original.

            analysis = MusicalAnalyzer.Analyze(
                reader.musicData
            );

            // Calcular la duracion de cada pulso.

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

            // Generar cada compas individualmente.

            for (int i = 0;
                 i < analysis.measures.Count;
                 i++)
            {
                var measure = analysis.measures[i];

                var next =
                    i + 1 < analysis.measures.Count
                    ? analysis.measures[i + 1]
                    : null;

                DrumStyle style = SelectStyle(measure);

                GenerateMeasure(
                    measure,
                    style,
                    pulse
                );

                // Generar transiciones cuando corresponda.

                if (ShouldGenerateFill(
                    measure,
                    next,
                    i))
                {
                    GenerateFill(measure, pulse);
                }

                if (printAnalysis)
                {
                    Debug.Log(
                        "Bateria | Compas " +
                        measure.number +
                        " | Estilo: " + style +
                        " | Densidad: " +
                        measure.density.ToString("F2")
                    );
                }
            }

            // Ordenar cronologicamente los eventos.

            events.Sort(
                (a, b) => a.start.CompareTo(b.start)
            );

            if (events.Count == 0)
            {
                throw new System.Exception(
                    "No se generaron golpes de bateria"
                );
            }

            // ======================================
            // NUEVO: COORDINACION MUSICAL
            // ======================================

            RegisterCoordinationEvents();

            // ======================================
            // CONFIRMAR GENERACION
            // ======================================

            generatedFrom = reader.musicData;

            generatedDuration =
                analysis.totalDuration;

            ui.CompleteGeneration();

            ui.status.text =
                "Bateria adaptativa: " +
                events.Count + " golpes";


            Debug.Log(
                "Bateria procedural generada: " +
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
    // NUEVO: REGISTRAR EVENTOS DE COORDINACION
    // ==========================================

    private void RegisterCoordinationEvents()
    {
        if (coordinator == null)
        {
            return;
        }

        int kickCount = 0;
        int snareCount = 0;

        foreach (DrumEvent hit in events)
        {
            coordinator.RegisterDrumHit(
                hit.midi,
                hit.start
            );

            if (hit.midi == Kick)
            {
                kickCount++;
            }
            else if (hit.midi == Snare)
            {
                snareCount++;
            }
        }

        if (printAnalysis)
        {
            Debug.Log(
                "Bateria registrada en MusicalCoordinator" +
                " | Bombos: " + kickCount +
                " | Cajas: " + snareCount
            );
        }
    }

    // ==========================================
    // SELECCION DEL ESTILO
    // ==========================================

    private DrumStyle SelectStyle(
        MusicalAnalyzer.MeasureAnalysis measure)
    {
        if (measure.isSilent)
        {
            return DrumStyle.Silence;
        }

        if (measure.silenceRatio >= 0.35f)
        {
            return DrumStyle.Response;
        }

        if (measure.density <= 0.75f)
        {
            return DrumStyle.Soft;
        }

        if (measure.syncopation >= 0.35f)
        {
            return DrumStyle.Syncopated;
        }

        if (measure.density > 2f)
        {
            return DrumStyle.Driving;
        }

        return DrumStyle.Groove;
    }

    // ==========================================
    // GENERACION POR COMPAS
    // ==========================================

    private void GenerateMeasure(
        MusicalAnalyzer.MeasureAnalysis measure,
        DrumStyle style,
        float pulse)
    {
        switch (style)
        {
            case DrumStyle.Silence:

                return;

            case DrumStyle.Soft:

                GenerateSoft(measure, pulse);
                break;

            case DrumStyle.Groove:

                GenerateGroove(measure, pulse);
                break;

            case DrumStyle.Driving:

                GenerateDriving(measure, pulse);
                break;

            case DrumStyle.Syncopated:

                GenerateSyncopated(measure, pulse);
                break;

            case DrumStyle.Response:

                GenerateResponse(measure, pulse);
                break;
        }
    }

    // ==========================================
    // ACENTOS DE BOMBO SEGUN EL COMPAS
    // ==========================================

    private bool IsKickBeat(int beat)
    {
        // Compases compuestos: 6/8, 9/8, 12/8.

        if (analysis.beatType == 8 &&
            analysis.beats >= 6 &&
            analysis.beats % 3 == 0)
        {
            return beat % 6 == 0;
        }

        // Compases de tres pulsos.

        if (analysis.beats == 3)
        {
            return beat == 0;
        }

        // Compases simples.

        return beat % 2 == 0;
    }

    // ==========================================
    // ACENTOS DE CAJA SEGUN EL COMPAS
    // ==========================================

    private bool IsSnareBeat(int beat)
    {
        if (analysis.beatType == 8 &&
            analysis.beats >= 6 &&
            analysis.beats % 3 == 0)
        {
            return beat % 6 == 3;
        }

        if (analysis.beats == 3)
        {
            return beat == 2;
        }

        return beat % 2 == 1;
    }

    // ==========================================
    // ESTILO SUAVE
    // ==========================================

    private void GenerateSoft(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        for (int beat = 0;
             beat < analysis.beats;
             beat++)
        {
            float position = beat * pulse;

            AddHit(
                measure,
                HiHat,
                position,
                0.4f
            );

            if (IsKickBeat(beat))
            {
                AddHit(
                    measure,
                    Kick,
                    position,
                    0.65f
                );
            }

            if (IsSnareBeat(beat))
            {
                AddHit(
                    measure,
                    Snare,
                    position,
                    0.5f
                );
            }
        }
    }

    // ==========================================
    // ESTILO GROOVE
    // ==========================================

    private void GenerateGroove(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        for (int beat = 0;
             beat < analysis.beats;
             beat++)
        {
            float position = beat * pulse;

            // Hi-hat en el pulso.

            AddHit(
                measure,
                HiHat,
                position,
                0.7f
            );

            // Hi-hat en el contratiempo.

            AddHit(
                measure,
                HiHat,
                position + pulse * 0.5f,
                0.45f
            );

            // Bombo.

            if (IsKickBeat(beat))
            {
                AddHit(
                    measure,
                    Kick,
                    position,
                    0.85f
                );
            }

            // Caja.

            if (IsSnareBeat(beat))
            {
                AddHit(
                    measure,
                    Snare,
                    position,
                    0.8f
                );
            }

            // Detectar ataques de la base
            // en los contratiempos.

            float offbeat =
                position + pulse * 0.5f;

            if (followBaseAttacks &&
                complexity >= 0.35f &&
                HasNearbyAttack(
                    measure,
                    offbeat,
                    pulse * 0.12f))
            {
                AddHit(
                    measure,
                    Kick,
                    offbeat,
                    0.6f
                );
            }
        }
    }

    // ==========================================
    // ESTILO ENERGICO
    // ==========================================

    private void GenerateDriving(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        int subdivisions =
            complexity > 0.7f ? 4 : 2;

        for (int beat = 0;
             beat < analysis.beats;
             beat++)
        {
            float position = beat * pulse;

            // Hi-hat con mayor subdivision.

            for (int i = 0;
                 i < subdivisions;
                 i++)
            {
                float offset =
                    pulse * i / subdivisions;

                AddHit(
                    measure,
                    HiHat,
                    position + offset,
                    i == 0 ? 0.8f : 0.5f
                );
            }

            // Bombo con mayor intensidad.

            if (IsKickBeat(beat))
            {
                AddHit(
                    measure,
                    Kick,
                    position,
                    0.95f
                );
            }

            // Caja con mayor intensidad.

            if (IsSnareBeat(beat))
            {
                AddHit(
                    measure,
                    Snare,
                    position,
                    0.9f
                );
            }

            // Reforzar ataques de la base.

            float offbeat =
                position + pulse * 0.5f;

            if (followBaseAttacks &&
                complexity >= 0.5f &&
                HasNearbyAttack(
                    measure,
                    offbeat,
                    pulse * 0.12f))
            {
                AddHit(
                    measure,
                    Kick,
                    offbeat,
                    0.75f
                );
            }
        }
    }

    // ==========================================
    // ESTILO SINCOPADO
    // ==========================================

    private void GenerateSyncopated(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        // Crear primero el groove principal.

        GenerateGroove(measure, pulse);

        if (!followBaseAttacks)
        {
            return;
        }

        float lastExtraKick = -pulse;

        foreach (var note in measure.notes)
        {
            if (note.isRest)
            {
                continue;
            }

            float beatPosition =
                note.start / pulse;

            float distance =
                Mathf.Abs(
                    beatPosition -
                    Mathf.Round(beatPosition)
                );

            bool isOffbeat =
                distance > 0.12f;

            if (!isOffbeat)
            {
                continue;
            }

            // Evitar demasiados bombos consecutivos.

            if (note.start - lastExtraKick <
                pulse * (1f - complexity * 0.5f))
            {
                continue;
            }

            AddHit(
                measure,
                Kick,
                note.start,
                0.75f
            );

            lastExtraKick = note.start;
        }
    }

    // ==========================================
    // RESPUESTA A SILENCIOS
    // ==========================================

    private void GenerateResponse(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        for (int beat = 0;
             beat < analysis.beats;
             beat++)
        {
            float position = beat * pulse;

            // Evitar golpes en pulsos
            // donde la base permanece en silencio.

            if (!IsBaseSounding(
                measure,
                position))
            {
                continue;
            }

            AddHit(
                measure,
                HiHat,
                position,
                0.5f
            );

            if (IsKickBeat(beat))
            {
                AddHit(
                    measure,
                    Kick,
                    position,
                    0.7f
                );
            }

            if (IsSnareBeat(beat))
            {
                AddHit(
                    measure,
                    Snare,
                    position,
                    0.65f
                );
            }
        }

        if (!followBaseAttacks)
        {
            return;
        }

        float lastAttack = -pulse;

        foreach (var note in measure.notes)
        {
            if (note.isRest)
            {
                continue;
            }

            if (note.start - lastAttack <
                pulse * 0.5f)
            {
                continue;
            }

            AddHit(
                measure,
                Kick,
                note.start,
                0.65f
            );

            lastAttack = note.start;
        }
    }

    // ==========================================
    // DETECCION DE ATAQUES CERCANOS
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
    // DETECCION DE ACTIVIDAD DE LA BASE
    // ==========================================

    private bool IsBaseSounding(
        MusicalAnalyzer.MeasureAnalysis measure,
        float position)
    {
        foreach (var note in measure.notes)
        {
            if (note.isRest)
            {
                continue;
            }

            if (position >= note.start &&
                position < note.start + note.duration)
            {
                return true;
            }
        }

        return false;
    }

    // ==========================================
    // DETECTAR TRANSICIONES MUSICALES
    // ==========================================

    private bool ShouldGenerateFill(
        MusicalAnalyzer.MeasureAnalysis measure,
        MusicalAnalyzer.MeasureAnalysis next,
        int index)
    {
        if (!useFills ||
            complexity < 0.25f ||
            measure.isSilent ||
            next == null ||
            next.isSilent)
        {
            return false;
        }

        // Final de una frase de cuatro compases.

        bool endOfPhrase =
            (index + 1) % 4 == 0;

        // Cambio de comportamiento musical.

        bool styleChange =
            SelectStyle(measure) != SelectStyle(next);

        return endOfPhrase ||
            (complexity >= 0.6f && styleChange);
    }

    // ==========================================
    // GENERACION DE REDOBLES
    // ==========================================

    private void GenerateFill(
        MusicalAnalyzer.MeasureAnalysis measure,
        float pulse)
    {
        float position = Mathf.Max(
            0f,
            measure.duration - pulse
        );

        float absoluteStart =
            measure.startTime + position;

        float absoluteEnd =
            measure.startTime + measure.duration;

        // Eliminar caja y hi-hat del ultimo pulso
        // para sustituirlos por el redoble.

        events.RemoveAll(hit =>
            hit.start >= absoluteStart &&
            hit.start < absoluteEnd &&
            (hit.midi == Snare ||
             hit.midi == HiHat)
        );

        int subdivisions =
            complexity >= 0.65f ? 4 : 2;

        for (int i = 0;
             i < subdivisions;
             i++)
        {
            float offset =
                pulse * i / subdivisions;

            float velocity = Mathf.Lerp(
                0.55f,
                0.95f,
                (float)i / (subdivisions - 1)
            );

            AddHit(
                measure,
                Snare,
                position + offset,
                velocity
            );
        }
    }

    // ==========================================
    // CREACION DE EVENTOS DE PERCUSION
    // ==========================================

    private void AddHit(
        MusicalAnalyzer.MeasureAnalysis measure,
        int midi,
        float position,
        float velocity)
    {
        // Validar posicion dentro del compas.

        if (position < 0f ||
            position >= measure.duration)
        {
            return;
        }

        float absolutePosition =
            measure.startTime + position;

        // Evitar golpes duplicados.

        foreach (DrumEvent hit in events)
        {
            if (hit.midi == midi &&
                Mathf.Abs(
                    hit.start - absolutePosition
                ) < 0.025f)
            {
                return;
            }
        }

        // Variacion expresiva de intensidad.

        velocity += Mathf.Sin(
            measure.number * 1.73f +
            position * 4.21f +
            midi
        ) * expression * 0.2f;

        velocity = Mathf.Clamp01(velocity);

        // Buscar un clip previamente sintetizado.

        if (!clips.TryGetValue(
            midi,
            out AudioClip clip))
        {
            float duration;

            switch (midi)
            {
                case Kick:

                    duration = 0.25f;
                    break;

                case Snare:

                    duration = 0.18f;
                    break;

                default:

                    duration = 0.08f;
                    break;
            }

            // Sintetizar el golpe.

            clip = synthesizer.GenerateDrum(
                midi,
                duration,
                1f
            );

            if (clip == null)
            {
                throw new System.Exception(
                    "No se pudo sintetizar el golpe " +
                    midi
                );
            }

            clips.Add(midi, clip);
        }

        // Registrar evento de percusion.

        events.Add(new DrumEvent
        {
            midi = midi,
            start = absolutePosition,
            velocity = velocity,
            clip = clip
        });
    }

    // ==========================================
    // VALIDACION DE GENERACION
    // ==========================================

    public bool HasValidGeneration()
    {
        return ui != null &&
               ui.HasGenerated &&
               ui.Selected().Contains("Batería") &&
               reader != null &&
               reader.isReady &&
               reader.musicData == generatedFrom &&
               events.Count > 0;
    }

    // ==========================================
    // REPRODUCCION SINCRONIZADA
    // ==========================================

    public void PlayScheduled(double startTime)
    {
        if (!HasValidGeneration())
        {
            return;
        }

        StopDrums();

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

        foreach (DrumEvent hit in events)
        {
            // Esperar la posicion absoluta
            // utilizando el reloj DSP.

            while (
                AudioSettings.dspTime <
                startTime + hit.start)
            {
                yield return null;
            }

            float instrumentVolume =
                GetInstrumentVolume(hit.midi);

            audioSource.PlayOneShot(
                hit.clip,
                volume *
                instrumentVolume *
                hit.velocity
            );

            // Considerar la duracion de las
            // colas de los sonidos.

            endTime = System.Math.Max(
                endTime,
                startTime +
                hit.start +
                hit.clip.length
            );
        }

        // Esperar el final de la composicion.

        while (AudioSettings.dspTime < endTime)
        {
            yield return null;
        }

        playbackCoroutine = null;
    }

    // ==========================================
    // VOLUMEN INDIVIDUAL DE PERCUSION
    // ==========================================

    private float GetInstrumentVolume(int midi)
    {
        switch (midi)
        {
            case Kick:

                return kickVolume;

            case Snare:

                return snareVolume;

            case HiHat:

                return hiHatVolume;

            default:

                return 1f;
        }
    }

    // ==========================================
    // DETENER REPRODUCCION
    // ==========================================

    public void StopDrums()
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
        StopDrums();

        events.Clear();

        generatedFrom = null;
        analysis = null;
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
        StopDrums();
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

        foreach (var hit in events)
        {
            if (hit.clip == null)
            {
                continue;
            }

            float finalVolume =
                volume *
                GetInstrumentVolume(hit.midi) *
                hit.velocity;

            result.Add(
                new AudioRenderEvent(
                    hit.clip,
                    hit.start,
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