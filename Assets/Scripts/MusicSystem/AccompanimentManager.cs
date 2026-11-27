using UnityEngine;

public class AccompanimentManager : MonoBehaviour
{
    [Header("Referencias principales")]
    public ProceduralPreviewController ui;

    public PlaybackManager playback;


    [Header("Generadores procedurales")]
    public GuitarGenerator guitarGenerator;

    public BassGenerator bassGenerator;

    public DrumGenerator drumGenerator;


    [Header("Sincronizacion")]
    public MusicalClock musicalClock;


    [Header("Coordinacion musical")]
    public MusicalCoordinator coordinator;


    [Header("Depuracion")]
    public bool printDebug = true;


    // ==========================================
    // GENERAR ACOMPANAMIENTOS
    // ==========================================

    public void Generate()
    {
        StopAll();


        if (ui == null)
        {
            Debug.LogError(
                "Falta asignar la UI."
            );

            return;
        }


        var selected = ui.Selected();


        if (selected.Count == 0)
        {
            ui.FailGeneration();

            return;
        }


        bool useGuitar =
            selected.Contains("Guitarra");

        bool useBass =
            selected.Contains("Bajo");

        bool useDrums =
            selected.Contains("Batería");


        // ======================================
        // VALIDAR REFERENCIAS
        // ======================================

        if (useGuitar &&
            guitarGenerator == null)
        {
            ReportError(
                "Falta asignar GuitarGenerator."
            );

            return;
        }


        if (useBass &&
            bassGenerator == null)
        {
            ReportError(
                "Falta asignar BassGenerator."
            );

            return;
        }


        if (useDrums &&
            drumGenerator == null)
        {
            ReportError(
                "Falta asignar DrumGenerator."
            );

            return;
        }


        // ======================================
        // PREPARAR COORDINACION MUSICAL
        // ======================================

        if (coordinator != null)
        {
            coordinator.ResetCoordination();
        }
        else
        {
            Debug.LogWarning(
                "MusicalCoordinator no asignado. " +
                "Se generaran los instrumentos " +
                "sin coordinacion adicional."
            );
        }


        if (drumGenerator != null)
        {
            drumGenerator.coordinator =
                coordinator;
        }


        if (bassGenerator != null)
        {
            bassGenerator.coordinator =
                coordinator;
        }


        if (guitarGenerator != null)
        {
            guitarGenerator.coordinator =
                coordinator;
        }


        // ======================================
        // 1. GENERAR BATERIA
        // ======================================

        if (useDrums)
        {
            drumGenerator.GenerateDrums();


            if (!drumGenerator.HasValidGeneration())
            {
                ReportError(
                    "Error generando bateria procedural."
                );

                return;
            }
        }


        // ======================================
        // 2. GENERAR BAJO
        // ======================================

        if (useBass)
        {
            bassGenerator.GenerateBass();


            if (!bassGenerator.HasValidGeneration())
            {
                ReportError(
                    "Error generando bajo procedural."
                );

                return;
            }
        }


        // ======================================
        // 3. GENERAR GUITARRA
        // ======================================

        if (useGuitar)
        {
            guitarGenerator.GenerateGuitar();


            if (!guitarGenerator.HasValidGeneration())
            {
                ReportError(
                    "Error generando guitarra procedural."
                );

                return;
            }
        }


        // ======================================
        // FINALIZAR GENERACION
        // ======================================

        ui.CompleteGeneration();


        ui.playAll.interactable =
            CanPlayOriginal();

        ui.exportButton.interactable = true;


        ui.status.text =
            "Generado: " +
            string.Join(" + ", selected);


        if (coordinator != null)
        {
            coordinator.PrintCoordination();
        }


        if (printDebug)
        {
            Debug.Log(
                "Acompanamientos generados: " +
                string.Join(" + ", selected)
            );
        }
    }


    // ==========================================
    // VALIDAR ACOMPANAMIENTOS GENERADOS
    // ==========================================

    private bool CanPlayGenerated()
    {
        if (ui == null ||
            !ui.HasGenerated)
        {
            return false;
        }


        var selected = ui.Selected();


        if (selected.Count == 0)
        {
            return false;
        }


        if (selected.Contains("Guitarra"))
        {
            if (guitarGenerator == null ||
                !guitarGenerator.HasValidGeneration())
            {
                return false;
            }
        }


        if (selected.Contains("Bajo"))
        {
            if (bassGenerator == null ||
                !bassGenerator.HasValidGeneration())
            {
                return false;
            }
        }


        if (selected.Contains("Batería"))
        {
            if (drumGenerator == null ||
                !drumGenerator.HasValidGeneration())
            {
                return false;
            }
        }


        return true;
    }


    // ==========================================
    // VALIDAR INSTRUMENTO ORIGINAL
    // ==========================================

    private bool CanPlayOriginal()
    {
        if (ui == null ||
            playback == null ||
            ui.reader == null ||
            !ui.reader.isReady ||
            ui.reader.musicData == null)
        {
            return false;
        }


        switch (ui.OriginalIndex)
        {
            // GUITARRA ORIGINAL

            case 0:

                return
                    playback.musicPlayer != null &&

                    playback.musicPlayer.musicReader ==
                    ui.reader &&

                    playback.musicPlayer.guitarSynthesizer
                    != null;


            // BAJO ORIGINAL

            case 1:

                return
                    playback.musicPlayer != null &&

                    playback.musicPlayer.musicReader ==
                    ui.reader &&

                    playback.musicPlayer.bassSynthesizer
                    != null;


            // BATERIA ORIGINAL

            case 2:

                return
                    playback.drumPlayer != null &&

                    playback.drumPlayer.musicReader ==
                    ui.reader &&

                    playback.drumPlayer.patternGenerator
                    != null &&

                    playback.drumPlayer.drumSynthesizer
                    != null;


            default:

                return false;
        }
    }


    // ==========================================
    // REPRODUCIR SOLO ACOMPANAMIENTOS
    // ==========================================

    public void PlayGenerated()
    {
        if (!CanPlayGenerated())
        {
            Debug.LogWarning(
                "Genera los acompanamientos primero."
            );

            return;
        }


        if (musicalClock == null)
        {
            Debug.LogError(
                "Falta asignar MusicalClock."
            );

            return;
        }


        StopAll();


        // Crear un inicio comun.

        double startTime =
            musicalClock.Begin();


        // Todos los acompanamientos reciben
        // exactamente el mismo tiempo DSP.

        StartAccompaniments(startTime);


        if (printDebug)
        {
            Debug.Log(
                "Reproduciendo acompanamientos" +
                " | Inicio DSP: " +
                startTime.ToString("F4")
            );
        }
    }


    // ==========================================
    // REPRODUCIR ORIGINAL + ACOMPANAMIENTOS
    // ==========================================

    public void PlayAll()
    {
        if (!CanPlayGenerated())
        {
            Debug.LogWarning(
                "No existen acompanamientos validos."
            );

            return;
        }


        if (!CanPlayOriginal())
        {
            Debug.LogWarning(
                "El instrumento original " +
                "no esta configurado correctamente."
            );

            return;
        }


        if (musicalClock == null)
        {
            Debug.LogError(
                "Falta asignar MusicalClock."
            );

            return;
        }


        StopAll();


        // ======================================
        // SELECCIONAR INSTRUMENTO ORIGINAL
        // ======================================

        PlaybackManager.PlaybackType originalType =
            (PlaybackManager.PlaybackType)
            ui.OriginalIndex;


        playback.SetPlaybackType(
            originalType
        );


        // La reproduccion conjunta debe utilizar
        // el mismo tempo de la partitura.

        if (originalType ==
            PlaybackManager.PlaybackType.Drums)
        {
            playback.drumPlayer.useMusicXMLTempo =
                true;
        }


        // ======================================
        // PREPARAR INSTRUMENTO ORIGINAL
        // ======================================

        // Ahora funciona para guitarra,
        // bajo y bateria.

        bool prepared =
            playback.PrepareSynchronizedPlayback();


        if (!prepared)
        {
            StopAll();


            ui.status.text =
                "Error preparando instrumento original.";

            ui.status.color = Color.red;


            Debug.LogError(
                ui.status.text
            );

            return;
        }


        // ======================================
        // CREAR RELOJ COMPARTIDO
        // ======================================

        double startTime =
            musicalClock.Begin();


        // ======================================
        // INICIAR INSTRUMENTO ORIGINAL
        // ======================================

        playback.PlaySynchronized(
            startTime
        );


        // ======================================
        // INICIAR ACOMPANAMIENTOS
        // ======================================

        StartAccompaniments(
            startTime
        );


        ui.status.text =
            "Reproduciendo original + " +
            string.Join(" + ", ui.Selected());

        ui.status.color = Color.white;


        if (printDebug)
        {
            Debug.Log(
                "================================"
            );

            Debug.Log(
                "REPRODUCCION CONJUNTA"
            );

            Debug.Log(
                "Instrumento original: " +
                originalType
            );

            Debug.Log(
                "Acompanamientos: " +
                string.Join(" + ", ui.Selected())
            );

            Debug.Log(
                "Inicio DSP compartido: " +
                startTime.ToString("F4")
            );

            Debug.Log(
                "================================"
            );
        }
    }


    // ==========================================
    // INICIAR GENERADORES SELECCIONADOS
    // ==========================================

    private void StartAccompaniments(
        double startTime)
    {
        var selected = ui.Selected();


        // BATERIA PROCEDURAL

        if (selected.Contains("Batería") &&
            drumGenerator != null)
        {
            drumGenerator.PlayScheduled(
                startTime
            );


            if (printDebug)
            {
                Debug.Log(
                    "Bateria procedural | DSP: " +
                    startTime.ToString("F4")
                );
            }
        }


        // BAJO PROCEDURAL

        if (selected.Contains("Bajo") &&
            bassGenerator != null)
        {
            bassGenerator.PlayScheduled(
                startTime
            );


            if (printDebug)
            {
                Debug.Log(
                    "Bajo procedural | DSP: " +
                    startTime.ToString("F4")
                );
            }
        }


        // GUITARRA PROCEDURAL

        if (selected.Contains("Guitarra") &&
            guitarGenerator != null)
        {
            guitarGenerator.PlayScheduled(
                startTime
            );


            if (printDebug)
            {
                Debug.Log(
                    "Guitarra procedural | DSP: " +
                    startTime.ToString("F4")
                );
            }
        }
    }


    // ==========================================
    // DETENER TODA LA REPRODUCCION
    // ==========================================

    public void StopAll()
    {
        if (playback != null)
        {
            playback.Stop();
        }


        if (drumGenerator != null)
        {
            drumGenerator.StopDrums();
        }


        if (bassGenerator != null)
        {
            bassGenerator.StopBass();
        }


        if (guitarGenerator != null)
        {
            guitarGenerator.StopGuitar();
        }


        if (musicalClock != null)
        {
            musicalClock.StopClock();
        }


        if (printDebug)
        {
            Debug.Log(
                "Reproduccion detenida."
            );
        }
    }


    // ==========================================
    // MANEJO DE ERRORES
    // ==========================================

    private void ReportError(
        string message)
    {
        StopAll();


        if (ui != null)
        {
            ui.FailGeneration();

            ui.status.text = message;

            ui.status.color = Color.red;
        }


        Debug.LogError(
            message,
            this
        );
    }


    // ==========================================
    // CICLO DE VIDA
    // ==========================================

    private void OnDisable()
    {
        StopAll();
    }
}