using System;
using System.Collections.Generic;
using UnityEngine;

public static class MusicalAnalyzer
{
    private static readonly string[] NoteNames =
    {
        "C", "C#", "D", "D#", "E", "F",
        "F#", "G", "G#", "A", "A#", "B"
    };

    // ==========================================
    // DATOS DEL ANALISIS
    // ==========================================

    public class AnalysisResult
    {
        public float tempo;
        public int beats;
        public int beatType;
        public int divisions;

        public int tonic;
        public bool isMinor;

        public bool hasPitchInformation;

        public float averageDensity;
        public float averageMidi;

        public float totalDuration;

        public List<MeasureAnalysis> measures =
            new List<MeasureAnalysis>();

        public string KeyName
        {
            get
            {
                if (!hasPitchInformation)
                    return "Indeterminada";

                return NoteNames[tonic] +
                    (isMinor ? " menor" : " mayor");
            }
        }
    }

    public class MeasureAnalysis
    {
        public int number;

        public float startTime;
        public float duration;

        public int noteCount;

        public float density;
        public float activity;
        public float silenceRatio;

        public float averageMidi;

        public bool isSilent;

        public int chordRoot;
        public bool chordMinor;

        public float harmonicConfidence;

        public float syncopation;

        public float[] pitchWeights =
            new float[12];

        public List<NoteEvent> notes =
            new List<NoteEvent>();

        public string ChordName
        {
            get
            {
                if (isSilent)
                    return "Silencio";

                return NoteNames[chordRoot] +
                    (chordMinor ? "m" : "");
            }
        }
    }

    public class NoteEvent
    {
        public int midi;

        // Posición relativa al compás, en segundos.
        public float start;

        public float duration;
        public bool isRest;
    }

    // ==========================================
    // ANALISIS PRINCIPAL
    // ==========================================

    public static AnalysisResult Analyze(MusicData data)
    {
        if (data == null ||
            data.measures == null ||
            data.measures.Count == 0)
        {
            throw new ArgumentException(
                "No hay datos musicales para analizar."
            );
        }

        AnalysisResult result = new AnalysisResult();

        result.tempo =
            data.tempo > 0 ? data.tempo : 120f;

        result.beats =
            data.beats > 0 ? data.beats : 4;

        result.beatType =
            data.beatType > 0 ? data.beatType : 4;

        result.divisions =
            data.divisions > 0 ? data.divisions : 1;

        float quarterDuration =
            60f / result.tempo;

        float beatDuration =
            quarterDuration * 4f / result.beatType;

        float measureDuration =
            beatDuration * result.beats;

        float secondsPerDivision =
            quarterDuration / result.divisions;

        float[] globalWeights = new float[12];

        float totalMidi = 0f;
        float totalWeight = 0f;

        float currentTime = 0f;

        // Analizar cada compás.
        foreach (MeasureData measure in data.measures)
        {
            MeasureAnalysis analysis = AnalyzeMeasure(
                measure,
                currentTime,
                measureDuration,
                beatDuration,
                secondsPerDivision
            );

            result.measures.Add(analysis);

            currentTime += analysis.duration;

            for (int i = 0; i < 12; i++)
            {
                globalWeights[i] +=
                    analysis.pitchWeights[i];
            }

            foreach (NoteEvent note in analysis.notes)
            {
                if (note.isRest)
                    continue;

                totalMidi +=
                    note.midi * note.duration;

                totalWeight += note.duration;
            }
        }

        result.totalDuration = currentTime;

        result.hasPitchInformation =
            totalWeight > 0f;

        result.averageMidi =
            totalWeight > 0f
            ? totalMidi / totalWeight
            : -1f;

        // Detectar tonalidad global.
        if (result.hasPitchInformation)
        {
            DetectKey(
                globalWeights,
                out result.tonic,
                out result.isMinor
            );
        }

        // Inferir acordes utilizando el contexto tonal.
        int previousRoot = result.tonic;
        bool previousMinor = result.isMinor;

        int activeMeasures = 0;
        float totalDensity = 0f;

        foreach (MeasureAnalysis measure in result.measures)
        {
            if (measure.isSilent)
            {
                measure.chordRoot = previousRoot;
                measure.chordMinor = previousMinor;
                measure.harmonicConfidence = 0f;

                continue;
            }

            InferChord(
                measure,
                result.tonic,
                result.isMinor,
                previousRoot,
                previousMinor
            );

            previousRoot = measure.chordRoot;
            previousMinor = measure.chordMinor;

            totalDensity += measure.density;
            activeMeasures++;
        }

        result.averageDensity =
            activeMeasures > 0
            ? totalDensity / activeMeasures
            : 0f;

        return result;
    }

    // ==========================================
    // ANALISIS POR COMPAS
    // ==========================================

    private static MeasureAnalysis AnalyzeMeasure(
        MeasureData measure,
        float startTime,
        float measureDuration,
        float beatDuration,
        float secondsPerDivision
    )
    {
        MeasureAnalysis result = new MeasureAnalysis();

        result.number = measure.number;
        result.startTime = startTime;
        result.duration = measureDuration;

        float midiSum = 0f;
        float midiWeight = 0f;

        int offBeatNotes = 0;

        List<NoteEvent> soundingNotes =
            new List<NoteEvent>();

        if (measure.notes != null)
        {
            foreach (NoteData note in measure.notes)
            {
                float start =
                    note.position * secondsPerDivision;

                float duration =
                    note.duration * secondsPerDivision;

                // Ignorar eventos sin duración válida.
                if (duration <= 0f || start < 0f)
                    continue;

                // Mantener los eventos dentro del compás.
                if (start >= measureDuration)
                    continue;

                duration = Mathf.Min(
                    duration,
                    measureDuration - start
                );

                NoteEvent musicalNote = new NoteEvent
                {
                    start = start,
                    duration = duration,
                    isRest = note.isRest,
                    midi = -1
                };

                if (note.isRest)
                {
                    result.notes.Add(musicalNote);
                    continue;
                }

                int midi = GetMidi(note);

                if (midi < 0 || midi > 127)
                    continue;

                musicalNote.midi = midi;

                result.notes.Add(musicalNote);
                soundingNotes.Add(musicalNote);

                result.noteCount++;

                int pitchClass = midi % 12;

                float beatPosition =
                    start / beatDuration;

                bool onBeat =
                    Mathf.Abs(
                        beatPosition -
                        Mathf.Round(beatPosition)
                    ) < 0.05f;

                float accentWeight =
                    onBeat ? 1.35f : 1f;

                // Las notas del inicio del compás
                // tienen mayor peso armónico.
                if (start < beatDuration * 0.05f)
                    accentWeight *= 1.4f;

                result.pitchWeights[pitchClass] +=
                    duration * accentWeight;

                midiSum += midi * duration;
                midiWeight += duration;

                if (!onBeat)
                    offBeatNotes++;
            }
        }

        result.isSilent =
            result.noteCount == 0;

        // Cantidad de ataques por pulso.
        result.density =
            result.noteCount /
            (measureDuration / beatDuration);

        // Proporción del compás que contiene sonido.
        float occupiedTime =
            CalculateOccupiedTime(soundingNotes);

        result.activity = Mathf.Clamp01(
            occupiedTime / measureDuration
        );

        result.silenceRatio =
            1f - result.activity;

        result.averageMidi =
            midiWeight > 0f
            ? midiSum / midiWeight
            : -1f;

        // Indicador aproximado de actividad
        // fuera de los pulsos principales.
        result.syncopation =
            result.noteCount > 0
            ? (float)offBeatNotes / result.noteCount
            : 0f;

        result.notes.Sort(
            (a, b) => a.start.CompareTo(b.start)
        );

        return result;
    }

    // ==========================================
    // OCUPACION TEMPORAL
    // ==========================================

    private static float CalculateOccupiedTime(
        List<NoteEvent> notes
    )
    {
        if (notes.Count == 0)
            return 0f;

        notes.Sort(
            (a, b) => a.start.CompareTo(b.start)
        );

        float occupied = 0f;

        float intervalStart = notes[0].start;

        float intervalEnd =
            notes[0].start + notes[0].duration;

        for (int i = 1; i < notes.Count; i++)
        {
            float start = notes[i].start;

            float end =
                start + notes[i].duration;

            if (start <= intervalEnd)
            {
                intervalEnd =
                    Mathf.Max(intervalEnd, end);
            }
            else
            {
                occupied +=
                    intervalEnd - intervalStart;

                intervalStart = start;
                intervalEnd = end;
            }
        }

        occupied += intervalEnd - intervalStart;

        return occupied;
    }

    // ==========================================
    // DETECCION DE TONALIDAD
    // ==========================================

    private static void DetectKey(
        float[] weights,
        out int tonic,
        out bool isMinor
    )
    {
        // Perfiles tonales de Krumhansl-Kessler.
        float[] majorProfile =
        {
            6.35f, 2.23f, 3.48f, 2.33f,
            4.38f, 4.09f, 2.52f, 5.19f,
            2.39f, 3.66f, 2.29f, 2.88f
        };

        float[] minorProfile =
        {
            6.33f, 2.68f, 3.52f, 5.38f,
            2.60f, 3.53f, 2.54f, 4.75f,
            3.98f, 2.69f, 3.34f, 3.17f
        };

        tonic = 0;
        isMinor = false;

        float bestScore = float.NegativeInfinity;

        for (int root = 0; root < 12; root++)
        {
            float majorScore =
                Correlation(weights, majorProfile, root);

            float minorScore =
                Correlation(weights, minorProfile, root);

            if (majorScore > bestScore)
            {
                bestScore = majorScore;
                tonic = root;
                isMinor = false;
            }

            if (minorScore > bestScore)
            {
                bestScore = minorScore;
                tonic = root;
                isMinor = true;
            }
        }
    }

    private static float Correlation(
        float[] weights,
        float[] profile,
        int root
    )
    {
        float meanA = 0f;
        float meanB = 0f;

        for (int i = 0; i < 12; i++)
        {
            meanA += weights[i];
            meanB += profile[i];
        }

        meanA /= 12f;
        meanB /= 12f;

        float numerator = 0f;
        float denominatorA = 0f;
        float denominatorB = 0f;

        for (int i = 0; i < 12; i++)
        {
            float a =
                weights[(root + i) % 12] - meanA;

            float b =
                profile[i] - meanB;

            numerator += a * b;
            denominatorA += a * a;
            denominatorB += b * b;
        }

        float denominator = Mathf.Sqrt(
            denominatorA * denominatorB
        );

        return denominator > 0f
            ? numerator / denominator
            : 0f;
    }

    // ==========================================
    // INFERENCIA DE ACORDES
    // ==========================================

    private static void InferChord(
        MeasureAnalysis measure,
        int tonic,
        bool keyMinor,
        int previousRoot,
        bool previousMinor
    )
    {
        float totalWeight = 0f;

        foreach (float weight in measure.pitchWeights)
        {
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return;

        float bestScore = float.NegativeInfinity;
        float secondScore = float.NegativeInfinity;

        for (int root = 0; root < 12; root++)
        {
            for (int mode = 0; mode < 2; mode++)
            {
                bool minor = mode == 1;

                int third =
                    (root + (minor ? 3 : 4)) % 12;

                int fifth = (root + 7) % 12;

                float rootWeight =
                    measure.pitchWeights[root];

                float thirdWeight =
                    measure.pitchWeights[third];

                float fifthWeight =
                    measure.pitchWeights[fifth];

                float outsideWeight =
                    totalWeight -
                    rootWeight -
                    thirdWeight -
                    fifthWeight;

                float score =
                    rootWeight * 1.4f +
                    thirdWeight * 1.2f +
                    fifthWeight -
                    outsideWeight * 0.65f;

                score /= totalWeight;

                // Favorecer acordes compatibles
                // con la tonalidad detectada.
                if (IsInScale(root, tonic, keyMinor))
                    score += 0.08f;

                if (IsInScale(third, tonic, keyMinor))
                    score += 0.08f;

                if (IsInScale(fifth, tonic, keyMinor))
                    score += 0.08f;

                // Favorecer continuidad armónica.
                if (root == previousRoot &&
                    minor == previousMinor)
                {
                    score += 0.06f;
                }

                // Considerar la primera nota del compás.
                foreach (NoteEvent note in measure.notes)
                {
                    if (note.isRest)
                        continue;

                    if (note.midi % 12 == root)
                        score += 0.15f;

                    break;
                }

                if (score > bestScore)
                {
                    secondScore = bestScore;
                    bestScore = score;

                    measure.chordRoot = root;
                    measure.chordMinor = minor;
                }
                else if (score > secondScore)
                {
                    secondScore = score;
                }
            }
        }

        // Margen entre los dos mejores candidatos.
        // No representa una probabilidad estadística.
        measure.harmonicConfidence =
            Mathf.Clamp01(bestScore - secondScore);
    }

    // ==========================================
    // UTILIDADES MUSICALES
    // ==========================================

    public static bool IsInScale(
        int pitch,
        int tonic,
        bool minor
    )
    {
        int[] scale = minor
            ? new int[] { 0, 2, 3, 5, 7, 8, 10 }
            : new int[] { 0, 2, 4, 5, 7, 9, 11 };

        int interval =
            ((pitch - tonic) % 12 + 12) % 12;

        foreach (int degree in scale)
        {
            if (interval == degree)
                return true;
        }

        return false;
    }

    public static int GetMidi(NoteData note)
    {
        if (note.isRest)
            return -1;

        int semitone;

        switch (note.step)
        {
            case "C": semitone = 0; break;
            case "D": semitone = 2; break;
            case "E": semitone = 4; break;
            case "F": semitone = 5; break;
            case "G": semitone = 7; break;
            case "A": semitone = 9; break;
            case "B": semitone = 11; break;

            default:
                return -1;
        }

        return
            (note.octave + 1) * 12 +
            semitone +
            note.alter;
    }

    public static float MidiToFrequency(int midi)
    {
        return 440f *
            Mathf.Pow(2f, (midi - 69) / 12f);
    }

    // ==========================================
    // DEPURACION
    // ==========================================

    public static void PrintAnalysis(
        AnalysisResult result
    )
    {
        Debug.Log(
            "========== ANALISIS MUSICAL =========="
        );

        Debug.Log("Tempo: " + result.tempo);

        Debug.Log(
            "Compás: " +
            result.beats + "/" + result.beatType
        );

        Debug.Log(
            "Tonalidad probable: " + result.KeyName
        );

        Debug.Log(
            "Duración: " +
            result.totalDuration.ToString("F2") +
            " segundos"
        );

        Debug.Log(
            "Densidad promedio: " +
            result.averageDensity.ToString("F2")
        );

        foreach (MeasureAnalysis measure in result.measures)
        {
            Debug.Log(
                "Compás " + measure.number +
                " | Acorde: " + measure.ChordName +
                " | Notas: " + measure.noteCount +
                " | Densidad: " +
                measure.density.ToString("F2") +
                " | Actividad: " +
                measure.activity.ToString("P0") +
                " | Silencio: " +
                measure.silenceRatio.ToString("P0") +
                " | Fuera de pulso: " +
                measure.syncopation.ToString("P0") +
                " | Margen armónico: " +
                measure.harmonicConfidence.ToString("F2")
            );
        }

        Debug.Log(
            "========== FIN DEL ANALISIS =========="
        );
    }
}