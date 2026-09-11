using System.Collections.Generic;

[System.Serializable]
public class MusicData
{
    public float tempo;

    public int beats;
    public int beatType;

    public int divisions;

    public List<MeasureData> measures =
        new List<MeasureData>();
}