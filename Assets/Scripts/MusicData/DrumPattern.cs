using System.Collections.Generic;

[System.Serializable]
public class DrumPattern
{
    public int beats = 4;

    public int subdivisions = 8;

    public List<DrumNoteData> notes =
        new List<DrumNoteData>();
}