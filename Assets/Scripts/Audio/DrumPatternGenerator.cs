using System.Collections.Generic;
using UnityEngine;

public class DrumPatternGenerator : MonoBehaviour
{
    [Header("Drum Map")]
    public int kickNote = 36;
    public int snareNote = 38;
    public int closedHatNote = 42;
    public int openHatNote = 46;
    public int crashNote = 49;


    [Header("Velocities")]
    [Range(0f, 1f)]
    public float kickVelocity = 0.9f;

    [Range(0f, 1f)]
    public float snareVelocity = 0.85f;

    [Range(0f, 1f)]
    public float hatVelocity = 0.55f;


    [Header("Depuracion")]
    public bool printDebug = true;


    // ==========================================
    // PATRON ORIGINAL
    // ==========================================

    public DrumPattern GeneratePattern(
        int numberOfMeasures)
    {
        return GeneratePattern(
            numberOfMeasures,
            4,
            4
        );
    }


    // ==========================================
    // PATRON BASICO
    // ==========================================

    public DrumPattern GenerateBasicPattern()
    {
        return GeneratePattern(4);
    }


    // ==========================================
    // NUEVO: PATRON SEGUN MUSICXML
    // ==========================================

    public DrumPattern GeneratePatternFromMusic(
        MusicData musicData)
    {
        if (musicData == null ||
            musicData.measures == null ||
            musicData.measures.Count == 0)
        {
            Debug.LogWarning(
                "DrumPatternGenerator: " +
                "No existe una partitura valida."
            );

            return GenerateBasicPattern();
        }


        int numberOfMeasures =
            musicData.measures.Count;


        int beatsPerMeasure =
            musicData.beats > 0
            ? musicData.beats
            : 4;


        int beatType =
            musicData.beatType > 0
            ? musicData.beatType
            : 4;


        return GeneratePattern(
            numberOfMeasures,
            beatsPerMeasure,
            beatType
        );
    }


    // ==========================================
    // GENERACION SEGUN METRICA Y DURACION
    // ==========================================

    public DrumPattern GeneratePattern(
        int numberOfMeasures,
        int beatsPerMeasure,
        int beatType)
    {
        DrumPattern pattern =
            new DrumPattern();


        numberOfMeasures =
            Mathf.Max(1, numberOfMeasures);

        beatsPerMeasure =
            Mathf.Max(1, beatsPerMeasure);

        beatType =
            Mathf.Max(1, beatType);


        if ((long)numberOfMeasures *
            beatsPerMeasure > 10000)
        {
            throw new System.Exception(
                "El patron supera el limite " +
                "de pulsos permitido."
            );
        }


        // Duracion del pulso expresada
        // en unidades de negra.

        float pulseLength =
            4f / beatType;


        float measureLength =
            beatsPerMeasure * pulseLength;


        int subdivisionsPerMeasure =
            beatsPerMeasure * 2;


        pattern.beats =
            numberOfMeasures * beatsPerMeasure;


        pattern.subdivisions =
            numberOfMeasures *
            subdivisionsPerMeasure;


        // ======================================
        // GENERAR TODOS LOS COMPASES
        // ======================================

        for (int measure = 0;
             measure < numberOfMeasures;
             measure++)
        {
            float measureStart =
                measure * measureLength;


            GenerateHiHat(
                pattern,
                measureStart,
                beatsPerMeasure,
                pulseLength
            );


            GenerateKick(
                pattern,
                measureStart,
                beatsPerMeasure,
                beatType,
                pulseLength
            );


            GenerateSnare(
                pattern,
                measureStart,
                beatsPerMeasure,
                beatType,
                pulseLength
            );
        }


        // Ordenar cronologicamente.

        pattern.notes.Sort(
            (a, b) =>
                a.position.CompareTo(
                    b.position
                )
        );


        if (printDebug)
        {
            Debug.Log(
                "================================"
            );

            Debug.Log(
                "PATRON DE BATERIA GENERADO"
            );

            Debug.Log(
                "Compases: " +
                numberOfMeasures
            );

            Debug.Log(
                "Metrica: " +
                beatsPerMeasure +
                "/" +
                beatType
            );

            Debug.Log(
                "Eventos: " +
                pattern.notes.Count
            );

            Debug.Log(
                "Duracion en negras: " +
                numberOfMeasures *
                measureLength
            );

            Debug.Log(
                "================================"
            );
        }


        return pattern;
    }


    // ==========================================
    // GENERACION DEL HI-HAT
    // ==========================================

    private void GenerateHiHat(
        DrumPattern pattern,
        float measureStart,
        int beatsPerMeasure,
        float pulseLength)
    {
        int subdivisions =
            beatsPerMeasure * 2;


        float subdivisionLength =
            pulseLength * 0.5f;


        for (int i = 0;
             i < subdivisions;
             i++)
        {
            float position =
                measureStart +
                i * subdivisionLength;


            AddDrumNote(
                pattern,
                closedHatNote,
                position,
                0.1f,
                hatVelocity
            );
        }
    }


    // ==========================================
    // GENERACION DEL BOMBO
    // ==========================================

    private void GenerateKick(
        DrumPattern pattern,
        float measureStart,
        int beatsPerMeasure,
        int beatType,
        float pulseLength)
    {
        for (int beat = 0;
             beat < beatsPerMeasure;
             beat++)
        {
            if (!IsKickBeat(
                beat,
                beatsPerMeasure,
                beatType))
            {
                continue;
            }


            float position =
                measureStart +
                beat * pulseLength;


            AddDrumNote(
                pattern,
                kickNote,
                position,
                0.1f,
                kickVelocity
            );
        }
    }


    // ==========================================
    // GENERACION DE LA CAJA
    // ==========================================

    private void GenerateSnare(
        DrumPattern pattern,
        float measureStart,
        int beatsPerMeasure,
        int beatType,
        float pulseLength)
    {
        for (int beat = 0;
             beat < beatsPerMeasure;
             beat++)
        {
            if (!IsSnareBeat(
                beat,
                beatsPerMeasure,
                beatType))
            {
                continue;
            }


            float position =
                measureStart +
                beat * pulseLength;


            AddDrumNote(
                pattern,
                snareNote,
                position,
                0.1f,
                snareVelocity
            );
        }
    }


    // ==========================================
    // ACENTOS DEL BOMBO SEGUN METRICA
    // ==========================================

    private bool IsKickBeat(
        int beat,
        int beatsPerMeasure,
        int beatType)
    {
        // Compases compuestos:
        // 6/8, 9/8, 12/8.

        if (beatType == 8 &&
            beatsPerMeasure >= 6 &&
            beatsPerMeasure % 3 == 0)
        {
            return beat % 6 == 0;
        }


        // Compases de tres pulsos.

        if (beatsPerMeasure == 3)
        {
            return beat == 0;
        }


        // Compases simples.

        return beat % 2 == 0;
    }


    // ==========================================
    // ACENTOS DE CAJA SEGUN METRICA
    // ==========================================

    private bool IsSnareBeat(
        int beat,
        int beatsPerMeasure,
        int beatType)
    {
        // Compases compuestos.

        if (beatType == 8 &&
            beatsPerMeasure >= 6 &&
            beatsPerMeasure % 3 == 0)
        {
            return beat % 6 == 3;
        }


        // Compases de tres pulsos.

        if (beatsPerMeasure == 3)
        {
            return beat == 2;
        }


        // Compases simples.

        return beat % 2 == 1;
    }


    // ==========================================
    // CREACION DE EVENTOS DE PERCUSION
    // ==========================================

    private void AddDrumNote(
        DrumPattern pattern,
        int midiNote,
        float position,
        float duration,
        float velocity)
    {
        DrumNoteData note =
            new DrumNoteData();


        note.midiNote =
            midiNote;


        note.position =
            position;


        note.duration =
            duration;


        note.velocity =
            Mathf.Clamp01(velocity);


        pattern.notes.Add(note);
    }
}