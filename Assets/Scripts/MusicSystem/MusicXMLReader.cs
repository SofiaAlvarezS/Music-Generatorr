using System.IO;
using System.Xml;
using UnityEngine;

public class MusicXMLReader : MonoBehaviour
{

    public MusicData musicData;

    // Indica si el XML ya fue procesado
    public bool isReady = false;

    private void Start()
    {
        string filePath = Path.Combine(
            Application.dataPath,
            "Music",
            "prueba.musicxml"
        );

        if (!File.Exists(filePath))
        {
            Debug.LogError(
                "No se encontró el archivo MusicXML en: " +
                filePath
            );

            return;
        }

        string musicXML =
            File.ReadAllText(filePath);

        Debug.Log(
            "MusicXML encontrado correctamente."
        );

        ReadXML(musicXML);
    }


    private void ReadXML(string musicXML)
    {
        XmlDocument document =
            new XmlDocument();

        try
        {
            document.LoadXml(musicXML);

            Debug.Log(
                "XML cargado correctamente."
            );

            // Crear modelo musical
            musicData = new MusicData();

            // Leer información general
            ReadTempo(document);
            ReadTimeSignature(document);
            ReadDivisions(document);

            // Leer compases y notas
            ReadMeasures(document);

            // Mostrar información
            PrintMusicData();

            // Avisar que ya está listo
            isReady = true;

            Debug.Log(
                "MusicData está lista para ser utilizada."
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Error al interpretar el XML: " +
                e.Message
            );
        }
    }



    private void ReadTempo(XmlDocument document)
    {
        // Buscar <sound tempo="..."/>
        XmlNode soundNode =
            document.SelectSingleNode(
                "//sound[@tempo]"
            );

        if (soundNode != null)
        {
            string tempoValue =
                soundNode.Attributes["tempo"].Value;

            if (float.TryParse(
                tempoValue,
                out float tempo
            ))
            {
                musicData.tempo = tempo;
            }

            Debug.Log(
                "TEMPO: " +
                tempoValue +
                " BPM"
            );

            return;
        }


        // Buscar <per-minute>
        XmlNode perMinuteNode =
            document.SelectSingleNode(
                "//per-minute"
            );

        if (perMinuteNode != null)
        {
            string tempoValue =
                perMinuteNode.InnerText;

            if (float.TryParse(
                tempoValue,
                out float tempo
            ))
            {
                musicData.tempo = tempo;
            }

            Debug.Log(
                "TEMPO: " +
                tempoValue +
                " BPM"
            );

            return;
        }


        // No se encontró tempo
        musicData.tempo = 0;

        Debug.LogWarning(
            "No se encontró información de tempo."
        );
    }


    private void ReadTimeSignature(
        XmlDocument document
    )
    {
        XmlNode beatsNode =
            document.SelectSingleNode(
                "//time/beats"
            );

        XmlNode beatTypeNode =
            document.SelectSingleNode(
                "//time/beat-type"
            );

        if (
            beatsNode != null &&
            beatTypeNode != null
        )
        {
            string beatsValue =
                beatsNode.InnerText;

            string beatTypeValue =
                beatTypeNode.InnerText;

            int.TryParse(
                beatsValue,
                out musicData.beats
            );

            int.TryParse(
                beatTypeValue,
                out musicData.beatType
            );

            Debug.Log(
                "COMPÁS: " +
                beatsValue +
                "/" +
                beatTypeValue
            );
        }
        else
        {
            Debug.LogWarning(
                "No se encontró información del compás."
            );
        }
    }

    private void ReadDivisions(
        XmlDocument document
    )
    {
        XmlNode divisionsNode =
            document.SelectSingleNode(
                "//divisions"
            );

        if (divisionsNode != null)
        {
            string divisionsValue =
                divisionsNode.InnerText;

            int.TryParse(
                divisionsValue,
                out musicData.divisions
            );

            Debug.Log(
                "DIVISIONS: " +
                divisionsValue
            );
        }
        else
        {
            Debug.LogWarning(
                "No se encontró información de divisions."
            );
        }
    }


    private void ReadMeasures(
        XmlDocument document
    )
    {
        XmlNodeList measures =
            document.SelectNodes(
                "//measure"
            );

        Debug.Log(
            "NÚMERO DE COMPASES: " +
            measures.Count
        );

        int measureIndex = 1;

        foreach (XmlNode measure in measures)
        {
            MeasureData measureData =
                new MeasureData();

            measureData.number =
                measureIndex;

            // Posición temporal dentro del compás
            int currentPosition = 0;

            // Buscar notas
            XmlNodeList notes =
                measure.SelectNodes(
                    "note"
                );

            foreach (XmlNode note in notes)
            {
                NoteData noteData =
                    ReadNote(
                        note,
                        currentPosition
                    );

                if (noteData != null)
                {
                    measureData.notes.Add(
                        noteData
                    );
                }

                // Obtener duración
                XmlNode durationNode =
                    note.SelectSingleNode(
                        "duration"
                    );

                if (
                    durationNode != null &&
                    int.TryParse(
                        durationNode.InnerText,
                        out int duration
                    )
                )
                {
                    currentPosition +=
                        duration;
                }
            }

            // Guardar compás
            musicData.measures.Add(
                measureData
            );

            measureIndex++;
        }
    }

    private NoteData ReadNote(
        XmlNode note,
        int position
    )
    {
        NoteData noteData =
            new NoteData();

        noteData.position =
            position;


        XmlNode restNode =
            note.SelectSingleNode(
                "rest"
            );

        if (restNode != null)
        {
            noteData.isRest = true;

            XmlNode durationNode =
                note.SelectSingleNode(
                    "duration"
                );

            if (
                durationNode != null &&
                int.TryParse(
                    durationNode.InnerText,
                    out int duration
                )
            )
            {
                noteData.duration =
                    duration;
            }

            XmlNode typeNode =
                note.SelectSingleNode(
                    "type"
                );

            if (typeNode != null)
            {
                noteData.type =
                    typeNode.InnerText;
            }

            return noteData;
        }


        XmlNode pitchNode =
            note.SelectSingleNode(
                "pitch"
            );

        if (pitchNode == null)
        {
            return null;
        }

        noteData.isRest = false;


        XmlNode stepNode =
            pitchNode.SelectSingleNode(
                "step"
            );

        if (stepNode != null)
        {
            noteData.step =
                stepNode.InnerText;
        }


        XmlNode octaveNode =
            pitchNode.SelectSingleNode(
                "octave"
            );

        if (
            octaveNode != null &&
            int.TryParse(
                octaveNode.InnerText,
                out int octave
            )
        )
        {
            noteData.octave =
                octave;
        }

        XmlNode alterNode =
            pitchNode.SelectSingleNode(
                "alter"
            );

        if (
            alterNode != null &&
            int.TryParse(
                alterNode.InnerText,
                out int alter
            )
        )
        {
            noteData.alter =
                alter;
        }
        else
        {
            noteData.alter = 0;
        }


        XmlNode noteDurationNode =
            note.SelectSingleNode(
                "duration"
            );

        if (
            noteDurationNode != null &&
            int.TryParse(
                noteDurationNode.InnerText,
                out int noteDuration
            )
        )
        {
            noteData.duration =
                noteDuration;
        }

        XmlNode noteTypeNode =
            note.SelectSingleNode(
                "type"
            );

        if (noteTypeNode != null)
        {
            noteData.type =
                noteTypeNode.InnerText;
        }


        return noteData;
    }


    private void PrintMusicData()
    {
        Debug.Log(
            "===================================="
        );

        Debug.Log(
            "       MUSIC DATA CREADA"
        );

        Debug.Log(
            "===================================="
        );

        Debug.Log(
            "Tempo: " +
            musicData.tempo
        );

        Debug.Log(
            "Compás: " +
            musicData.beats +
            "/" +
            musicData.beatType
        );

        Debug.Log(
            "Divisions: " +
            musicData.divisions
        );

        Debug.Log(
            "Número de compases: " +
            musicData.measures.Count
        );


        // Mostrar compases y notas
        for (
            int i = 0;
            i < musicData.measures.Count;
            i++
        )
        {
            MeasureData measure =
                musicData.measures[i];

            Debug.Log(
                "Compás " +
                measure.number +
                " | Notas: " +
                measure.notes.Count
            );


            for (
                int j = 0;
                j < measure.notes.Count;
                j++
            )
            {
                NoteData note =
                    measure.notes[j];

                if (note.isRest)
                {
                    Debug.Log(
                        "   Silencio" +
                        " | Posición: " +
                        note.position +
                        " | Duración: " +
                        note.duration
                    );
                }
                else
                {
                    Debug.Log(
                        "   Nota: " +
                        note.step +
                        note.octave +
                        " | Posición: " +
                        note.position +
                        " | Duración: " +
                        note.duration
                    );
                }
            }
        }
    }
}