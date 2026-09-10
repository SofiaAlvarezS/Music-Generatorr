using UnityEngine;

public class DrumGenerator : MonoBehaviour
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


    // =========================================================
    // GENERAR PATRÓN BÁSICO
    // =========================================================

    public DrumPattern GenerateBasicPattern()
    {
        DrumPattern pattern =
            new DrumPattern();


        // 4/4
        pattern.beats = 4;

        // 8 subdivisiones
        pattern.subdivisions = 8;


        // =====================================================
        // HI-HAT
        // =====================================================

        for (
            int i = 0;
            i < 8;
            i++
        )
        {
            DrumNoteData hat =
                new DrumNoteData();


            hat.midiNote =
                closedHatNote;


            hat.position =
                i * 0.5f;


            hat.duration =
                0.1f;


            hat.velocity =
                hatVelocity;


            pattern.notes.Add(
                hat
            );
        }


        // =====================================================
        // KICK
        // =====================================================

        int[] kickPositions =
        {
            0,
            4
        };


        foreach (
            int position
            in kickPositions
        )
        {
            DrumNoteData kick =
                new DrumNoteData();


            kick.midiNote =
                kickNote;


            kick.position =
                position * 0.5f;


            kick.duration =
                0.1f;


            kick.velocity =
                kickVelocity;


            pattern.notes.Add(
                kick
            );
        }


        // =====================================================
        // SNARE
        // =====================================================

        int[] snarePositions =
        {
            2,
            6
        };


        foreach (
            int position
            in snarePositions
        )
        {
            DrumNoteData snare =
                new DrumNoteData();


            snare.midiNote =
                snareNote;


            snare.position =
                position * 0.5f;


            snare.duration =
                0.1f;


            snare.velocity =
                snareVelocity;


            pattern.notes.Add(
                snare
            );
        }


        return pattern;
    }
}