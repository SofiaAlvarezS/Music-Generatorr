using UnityEngine;

public class MusicalAnalyzerTest : MonoBehaviour
{
    public MusicXMLReader reader;

    [ContextMenu("Analizar partitura")]
    public void AnalyzeMusic()
    {
        if (reader == null ||
            !reader.isReady ||
            reader.musicData == null)
        {
            Debug.LogWarning(
                "Primero importa una partitura."
            );

            return;
        }

        MusicalAnalyzer.AnalysisResult result =
            MusicalAnalyzer.Analyze(reader.musicData);

        MusicalAnalyzer.PrintAnalysis(result);
    }
}