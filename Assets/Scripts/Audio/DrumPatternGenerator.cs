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

    public DrumPattern GeneratePattern(
        int numberOfMeasures
    )
    {
        DrumPattern pattern =
            new DrumPattern();

        int beatsPerMeasure = 4;

        int subdivisionsPerMeasure = 8;


        if (numberOfMeasures <= 0)
        {
            numberOfMeasures = 1;
        }


        pattern.beats =
            beatsPerMeasure *
            numberOfMeasures;


        pattern.subdivisions =
            subdivisionsPerMeasure *
            numberOfMeasures;

        for (
            int measure = 0;
            measure < numberOfMeasures;
            measure++
        )
        {
            float measureStart =
                measure *
                beatsPerMeasure;


            for (
                int i = 0;
                i < subdivisionsPerMeasure;
                i++
            )
            {
                DrumNoteData hat =
                    new DrumNoteData();


                hat.midiNote =
                    closedHatNote;


                hat.position =
                    measureStart +
                    i * 0.5f;


                hat.duration =
                    0.1f;


                hat.velocity =
                    hatVelocity;


                pattern.notes.Add(hat);
            }

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
                    measureStart +
                    position * 0.5f;


                kick.duration =
                    0.1f;


                kick.velocity =
                    kickVelocity;


                pattern.notes.Add(kick);
            }

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
                    measureStart +
                    position * 0.5f;


                snare.duration =
                    0.1f;


                snare.velocity =
                    snareVelocity;


                pattern.notes.Add(snare);
            }
        }


        Debug.Log(
            "DrumPattern generado: " +
            numberOfMeasures +
            " compases | " +
            pattern.notes.Count +
            " eventos"
        );


        return pattern;
    }

    public DrumPattern GenerateBasicPattern()
    {
        return GeneratePattern(4);
    }
}