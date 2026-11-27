using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using System.IO;

public class ProceduralPreviewController : MonoBehaviour
{
    public Dropdown original;
    public Toggle[] instruments;

    public Button generate, playOriginal, playGenerated,
                  playAll, stop, importFile;

    public Text status, fileName;

    public MusicXMLReader reader;
    public PlaybackManager playback;

    public UnityEvent onGenerate = new UnityEvent();
    public UnityEvent onPlayGenerated = new UnityEvent();
    public UnityEvent onPlayAll = new UnityEvent();
    public UnityEvent onStop = new UnityEvent();

    public Button exportButton, confirmExport, cancelExport;

    public GameObject exportPanel;
    public Dropdown exportMode;
    public InputField exportName;
    public Text exportStatus;

    public UnityEvent onExport = new UnityEvent();

    public enum ExportMode
    {
        MixedAccompaniment = 0,
        FullMix = 1
    }

    public ExportMode RequestedMode { get; private set; }

    public string RequestedName { get; private set; }

    public int OriginalIndex => original.value - 1;

    public bool HasGenerated => generated;

    private bool generated, loaded;

    private readonly string[] names =
    {
        "Guitarra",
        "Bajo",
        "Batería"
    };


    void Start()
    {
        exportPanel.SetActive(false);
        ConfigureExportOptions();
        exportButton.onClick.AddListener(() =>
        {
            if (generated)
            {
                exportStatus.text = "Formato: WAV";
                exportPanel.SetActive(true);
            }
        });

        cancelExport.onClick.AddListener(() =>
        {
            exportPanel.SetActive(false);
        });

        confirmExport.onClick.AddListener(RequestExport);

        original.onValueChanged.AddListener(_ =>
        {
            Invalidate();
        });

        foreach (Toggle t in instruments)
        {
            t.group = null;

            t.onValueChanged.AddListener(_ =>
            {
                Invalidate();
            });
        }

        importFile.onClick.AddListener(OpenFile);

        generate.onClick.AddListener(() =>
        {
            generated = false;

            Refresh();

            status.text = "Generando...";

            onGenerate.Invoke();
        });


        // NUEVO: reproducción, pausa y reanudación
        // desde el mismo botón.
        playOriginal.onClick.AddListener(
            ToggleOriginalPlayback
        );


        playGenerated.onClick.AddListener(() =>
        {
            onPlayGenerated.Invoke();
        });

        playAll.onClick.AddListener(() =>
        {
            onPlayAll.Invoke();
        });

        stop.onClick.AddListener(() =>
        {
            if (playback != null)
            {
                playback.Stop();
            }

            onStop.Invoke();
        });

        Refresh();
    }

    private void ConfigureExportOptions()
    {
        exportMode.ClearOptions();

        List<string> options = new List<string>()
    {
        "Solo acompanamientos",
        "Original + acompanamientos"
    };

        exportMode.AddOptions(options);

        exportMode.value = 0;

        exportMode.RefreshShownValue();
    }
  
    public void ToggleOriginalPlayback()
    {
        if (!loaded ||
            original.value < 1 ||
            playback == null)
        {
            return;
        }

        var selectedType =
            (PlaybackManager.PlaybackType)
            (original.value - 1);

        MusicPlayer player = playback.musicPlayer;

        // Comprueba si el instrumento original
        // ya se está reproduciendo.
        if (
            selectedType !=
                PlaybackManager.PlaybackType.Drums &&

            playback.playbackType == selectedType &&

            player != null &&

            player.IsPlaying
        )
        {
            if (player.IsPaused)
            {
                // Continúa desde la posición actual.
                playback.Reanudar();
            }
            else
            {
                // Pausa sin reiniciar la partitura.
                playback.Pausar();
            }

            return;
        }

        // Si no hay reproducción activa,
        // comienza desde el principio.
        onStop.Invoke();

        playback.SetPlaybackType(selectedType);

        playback.Play();
    }


    public void RequestExport()
    {
        if (!generated)
        {
            exportStatus.text =
                "Primero genera los acompanamientos.";

            return;
        }


        string name = exportName.text.Trim();


        if (string.IsNullOrEmpty(name) ||
            name.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0 ||
            name == "." ||
            name == "..")
        {
            exportStatus.text =
                "Nombre de archivo invalido.";

            return;
        }


        RequestedName = name;


        switch (exportMode.value)
        {
            case 0:

                RequestedMode =
                    ExportMode.MixedAccompaniment;

                break;


            case 1:

                RequestedMode =
                    ExportMode.FullMix;

                break;


            default:

                exportStatus.text =
                    "Selecciona una modalidad valida.";

                return;
        }


        exportStatus.text =
            "Preparando exportacion WAV...";


        onExport.Invoke();
    }


    public void ReportExportResult(string message)
    {
        exportStatus.text = message;
    }


    public void OpenFile()
    {
#if UNITY_EDITOR

        string path =
            UnityEditor.EditorUtility.OpenFilePanel(
                "Selecciona MusicXML",
                "",
                "musicxml,xml"
            );

        if (!string.IsNullOrEmpty(path))
        {
            LoadFile(path);
        }

#else

        status.text =
            "Falta conectar selector de archivos para el ejecutable";

#endif
    }


    public void LoadFile(string path)
    {
        Invalidate();

        loaded = false;

        if (reader != null)
        {
            reader.isReady = false;
        }

        fileName.text = "Sin archivo";

        try
        {
            if (reader == null)
            {
                throw new System.Exception(
                    "Asigna MusicXMLReader"
                );
            }

            reader.LoadMusicXMLFromFile(path);

            loaded =
                reader.isReady &&
                reader.musicData != null &&
                reader.musicData.measures != null &&
                reader.musicData.measures.Count > 0;

            if (loaded)
            {
                fileName.text =
                    Path.GetFileName(path);
            }

            Refresh();

            if (!loaded)
            {
                FailGeneration();
            }
        }
        catch (System.Exception e)
        {
            Refresh();

            status.text = e.Message;

            status.color = Color.red;
        }
    }


    public List<string> Selected()
    {
        var result = new List<string>();

        for (int i = 0; i < instruments.Length; i++)
        {
            if (
                instruments[i].isOn &&
                original.value != i + 1
            )
            {
                result.Add(names[i]);
            }
        }

        return result;
    }


    public void CompleteGeneration()
    {
        generated =
            loaded &&
            original.value > 0 &&
            Selected().Count > 0;

        Refresh();
    }


    public void FailGeneration()
    {
        generated = false;

        Refresh();

        status.text =
            "Error de carga o generación";

        status.color = Color.red;
    }


    void Invalidate()
    {
        generated = false;

        if (playback != null)
        {
            playback.Stop();
        }

        onStop.Invoke();

        Refresh();
    }


    void Refresh()
    {
        exportButton.interactable = generated;

        if (!generated)
        {
            exportPanel.SetActive(false);
        }

        for (int i = 0; i < instruments.Length; i++)
        {
            bool allowed =
                loaded &&
                original.value > 0 &&
                original.value != i + 1;

            if (!allowed)
            {
                instruments[i].SetIsOnWithoutNotify(
                    false
                );
            }

            instruments[i].interactable = allowed;
        }

        generate.interactable =
            loaded &&
            original.value > 0 &&
            Selected().Count > 0;

        playOriginal.interactable =
            loaded &&
            original.value > 0 &&
            playback != null;

        playGenerated.interactable =
            playAll.interactable = generated;

        status.color =
            generated
            ? new Color(.35f, .85f, .55f)
            : Color.white;

        status.text =
            !loaded
            ? "Importa una partitura"

            : original.value == 0
            ? "Selecciona instrumento original"

            : generated
            ? "Generación completada"

            : generate.interactable
            ? "Listo para generar"

            : "Selecciona acompañamientos";
    }
}