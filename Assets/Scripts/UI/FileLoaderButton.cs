using UnityEngine;
using System.IO;

public class FileLoaderButton : MonoBehaviour
{
    [Header("Referencia al Lector")]
    public MusicXMLReader musicXMLReader;

    // Método que debes conectar al evento On Click() de tu Botón en Unity
    public void OnClickOpenFileDialog()
    {
        if (musicXMLReader == null)
        {
            Debug.LogError("FileLoaderButton: No se ha asignado el MusicXMLReader.");
            return;
        }

        string path = EditorOpenFilePanel();

        if (!string.IsNullOrEmpty(path))
        {
            musicXMLReader.isReady = false;
            musicXMLReader.LoadMusicXMLFromFile(path);
        }
    }

    private string EditorOpenFilePanel()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.OpenFilePanel("Selecciona un archivo MusicXML", "", "musicxml,xml");
#else
        Debug.LogWarning("El selector de archivos del editor solo funciona en modo desarrollo.");
        return "";
#endif
    }
}