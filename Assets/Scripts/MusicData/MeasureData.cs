using System.Collections.Generic;

[System.Serializable]
public class MeasureData
{
    public int number;

    public List<NoteData> notes =
        new List<NoteData>();
}