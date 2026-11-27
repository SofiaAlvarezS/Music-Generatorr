using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class AudioExportManager : MonoBehaviour
{
    [Header("Referencias principales")]
    public ProceduralPreviewController ui;

    public PlaybackManager playback;

    public AccompanimentManager accompanimentManager;


    [Header("Generadores procedurales")]
    public GuitarGenerator guitarGenerator;

    public BassGenerator bassGenerator;

    public DrumGenerator drumGenerator;


    [Header("Configuracion de exportacion")]
    public string exportFolder = "Exportaciones";

    public bool preventClipping = true;

    public bool printDebug = true;


    private bool isExporting = false;


    // ==========================================
    // SOLICITUD DE EXPORTACION
    // ==========================================

    public void ExportRequested()
    {
        if (isExporting)
        {
            return;
        }


        if (ui == null)
        {
            Debug.LogError(
                "Falta asignar la interfaz."
            );

            return;
        }


        if (!ui.HasGenerated)
        {
            ReportError(
                "Primero genera los acompanamientos."
            );

            return;
        }


        switch (ui.RequestedMode)
        {
            case ProceduralPreviewController
                .ExportMode.MixedAccompaniment:

                ExportAccompaniment();

                break;


            case ProceduralPreviewController
                .ExportMode.FullMix:

                ExportFullMix();

                break;


            default:

                ReportError(
                    "Modalidad de exportacion no disponible."
                );

                break;
        }
    }


    // ==========================================
    // EXPORTAR SOLO ACOMPANAMIENTOS
    // ==========================================

    public void ExportAccompaniment()
    {
        ExportAudio(false);
    }


    // ==========================================
    // EXPORTAR ORIGINAL + ACOMPANAMIENTOS
    // ==========================================

    public void ExportFullMix()
    {
        ExportAudio(true);
    }


    // ==========================================
    // PROCESO PRINCIPAL DE EXPORTACION
    // ==========================================

    private void ExportAudio(
        bool includeOriginal)
    {
        if (isExporting)
        {
            return;
        }


        isExporting = true;

        bool originalPrepared = false;


        try
        {
            ValidateReferences();


            if (ui.confirmExport != null)
            {
                ui.confirmExport.interactable = false;
            }


            // ==================================
            // VALIDAR NOMBRE
            // ==================================

            string fileName =
                ui.RequestedName;


            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new Exception(
                    "Escribe un nombre para el archivo."
                );
            }


            fileName = fileName.Trim();


            if (fileName.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0 ||
                fileName == "." ||
                fileName == "..")
            {
                throw new Exception(
                    "Nombre de archivo invalido."
                );
            }


            if (fileName.EndsWith(
                ".wav",
                StringComparison.OrdinalIgnoreCase))
            {
                fileName =
                    Path.GetFileNameWithoutExtension(
                        fileName
                    );
            }


            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new Exception(
                    "Nombre de archivo invalido."
                );
            }


            // ==================================
            // SELECCIONAR CARPETA
            // ==================================

            string folder =
                SelectExportFolder();


            if (string.IsNullOrEmpty(folder))
            {
                ui.ReportExportResult(
                    "Exportacion cancelada."
                );

                return;
            }


            // ==================================
            // DETENER REPRODUCCION
            // ==================================

            accompanimentManager.StopAll();


            ui.ReportExportResult(
                "Preparando audio..."
            );


            // ==================================
            // CREAR LISTA DE EVENTOS
            // ==================================

            List<AudioRenderEvent> renderEvents =
                new List<AudioRenderEvent>();


            List<string> exportedInstruments =
                new List<string>();


            double totalDuration = 0.0;


            // ==================================
            // OBTENER ACOMPANAMIENTOS
            // ==================================

            CollectAccompaniments(
                renderEvents,
                exportedInstruments,
                ref totalDuration
            );


            if (renderEvents.Count == 0)
            {
                throw new Exception(
                    "No existen eventos de audio " +
                    "para exportar."
                );
            }


            // ==================================
            // INCLUIR INSTRUMENTO ORIGINAL
            // ==================================

            if (includeOriginal)
            {
                PrepareOriginal();


                originalPrepared = true;


                List<AudioRenderEvent> originalEvents =
                    playback.GetRenderEvents();


                if (originalEvents.Count == 0)
                {
                    throw new Exception(
                        "El instrumento original " +
                        "no contiene eventos de audio."
                    );
                }


                renderEvents.AddRange(
                    originalEvents
                );


                totalDuration = Math.Max(
                    totalDuration,
                    playback.GetRenderDuration()
                );


                exportedInstruments.Insert(
                    0,
                    GetOriginalName()
                );
            }


            // ==================================
            // RENDERIZAR AUDIO
            // ==================================

            float[] samples =
                AudioTrackRenderer.Render(
                    renderEvents,
                    totalDuration,
                    WavExporter.DefaultSampleRate,
                    preventClipping
                );


            // ==================================
            // DEFINIR NOMBRE FINAL
            // ==================================

            string suffix =
                includeOriginal
                ? "_Completa"
                : "_Acompanamiento";


            string finalName =
                fileName +
                suffix +
                ".wav";


            string filePath =
                Path.Combine(
                    folder,
                    finalName
                );


            // ==================================
            // EXPORTAR WAV
            // ==================================

            string exportedPath =
                WavExporter.Export(
                    filePath,
                    samples,
                    WavExporter.DefaultSampleRate,
                    WavExporter.DefaultChannels
                );


            // ==================================
            // RESULTADO
            // ==================================

            ui.ReportExportResult(
                "Exportacion completada:\n" +
                Path.GetFileName(exportedPath) +
                "\nInstrumentos: " +
                string.Join(
                    " + ",
                    exportedInstruments
                ) +
                "\nRuta: " +
                exportedPath
            );


            if (printDebug)
            {
                Debug.Log(
                    "================================"
                );

                Debug.Log(
                    "EXPORTACION COMPLETADA"
                );

                Debug.Log(
                    "Modalidad: " +
                    (
                        includeOriginal
                        ? "Mezcla completa"
                        : "Solo acompanamientos"
                    )
                );

                Debug.Log(
                    "Instrumentos: " +
                    string.Join(
                        " + ",
                        exportedInstruments
                    )
                );

                Debug.Log(
                    "Eventos renderizados: " +
                    renderEvents.Count
                );

                Debug.Log(
                    "Archivo: " +
                    exportedPath
                );

                Debug.Log(
                    "================================"
                );
            }
        }
        catch (Exception exception)
        {
            ReportError(
                exception.Message
            );
        }
        finally
        {
            // Liberar los clips preparados
            // del instrumento original.

            if (originalPrepared &&
                playback != null)
            {
                playback.Stop();
            }


            isExporting = false;


            if (ui != null &&
                ui.confirmExport != null)
            {
                ui.confirmExport.interactable = true;
            }
        }
    }


    // ==========================================
    // OBTENER ACOMPANAMIENTOS SELECCIONADOS
    // ==========================================

    private void CollectAccompaniments(
        List<AudioRenderEvent> renderEvents,
        List<string> exportedInstruments,
        ref double totalDuration)
    {
        List<string> selected =
            ui.Selected();


        if (selected.Count == 0)
        {
            throw new Exception(
                "No hay acompanamientos seleccionados."
            );
        }


        // ======================================
        // GUITARRA PROCEDURAL
        // ======================================

        if (selected.Contains("Guitarra"))
        {
            if (guitarGenerator == null ||
                !guitarGenerator.HasValidGeneration())
            {
                throw new Exception(
                    "La guitarra procedural " +
                    "no tiene una generacion valida."
                );
            }


            AddTrack(
                guitarGenerator.GetRenderEvents(),
                guitarGenerator.GetRenderDuration(),
                "Guitarra",
                renderEvents,
                exportedInstruments,
                ref totalDuration
            );
        }


        // ======================================
        // BAJO PROCEDURAL
        // ======================================

        if (selected.Contains("Bajo"))
        {
            if (bassGenerator == null ||
                !bassGenerator.HasValidGeneration())
            {
                throw new Exception(
                    "El bajo procedural " +
                    "no tiene una generacion valida."
                );
            }


            AddTrack(
                bassGenerator.GetRenderEvents(),
                bassGenerator.GetRenderDuration(),
                "Bajo",
                renderEvents,
                exportedInstruments,
                ref totalDuration
            );
        }


        // ======================================
        // BATERIA PROCEDURAL
        // ======================================

        if (selected.Contains("Batería"))
        {
            if (drumGenerator == null ||
                !drumGenerator.HasValidGeneration())
            {
                throw new Exception(
                    "La bateria procedural " +
                    "no tiene una generacion valida."
                );
            }


            AddTrack(
                drumGenerator.GetRenderEvents(),
                drumGenerator.GetRenderDuration(),
                "Bateria",
                renderEvents,
                exportedInstruments,
                ref totalDuration
            );
        }
    }


    // ==========================================
    // AGREGAR PISTA A LA MEZCLA
    // ==========================================

    private void AddTrack(
        List<AudioRenderEvent> trackEvents,
        double trackDuration,
        string instrumentName,
        List<AudioRenderEvent> renderEvents,
        List<string> exportedInstruments,
        ref double totalDuration)
    {
        if (trackEvents == null ||
            trackEvents.Count == 0)
        {
            throw new Exception(
                "No existen eventos de: " +
                instrumentName
            );
        }


        renderEvents.AddRange(
            trackEvents
        );


        exportedInstruments.Add(
            instrumentName
        );


        totalDuration = Math.Max(
            totalDuration,
            trackDuration
        );


        if (printDebug)
        {
            Debug.Log(
                "Pista agregada: " +
                instrumentName +
                " | Eventos: " +
                trackEvents.Count
            );
        }
    }


    // ==========================================
    // PREPARAR INSTRUMENTO ORIGINAL
    // ==========================================

    private void PrepareOriginal()
    {
        if (playback == null)
        {
            throw new Exception(
                "Falta asignar PlaybackManager."
            );
        }


        if (ui.OriginalIndex < 0 ||
            ui.OriginalIndex > 2)
        {
            throw new Exception(
                "Selecciona un instrumento original."
            );
        }


        PlaybackManager.PlaybackType originalType =
            (PlaybackManager.PlaybackType)
            ui.OriginalIndex;


        // Validar que el original utilice
        // la misma partitura.

        if (originalType ==
            PlaybackManager.PlaybackType.Drums)
        {
            if (playback.drumPlayer == null ||
                playback.drumPlayer.musicReader != ui.reader)
            {
                throw new Exception(
                    "DrumPlayer no esta configurado " +
                    "con la partitura actual."
                );
            }


            playback.drumPlayer.useMusicXMLTempo =
                true;
        }
        else
        {
            if (playback.musicPlayer == null ||
                playback.musicPlayer.musicReader != ui.reader)
            {
                throw new Exception(
                    "MusicPlayer no esta configurado " +
                    "con la partitura actual."
                );
            }
        }


        playback.SetPlaybackType(
            originalType
        );


        // Preparar los clips sin reproducirlos.

        if (!playback.PrepareSynchronizedPlayback())
        {
            throw new Exception(
                "No se pudo preparar " +
                "el instrumento original."
            );
        }
    }


    // ==========================================
    // NOMBRE DEL INSTRUMENTO ORIGINAL
    // ==========================================

    private string GetOriginalName()
    {
        switch (ui.OriginalIndex)
        {
            case 0:

                return "Guitarra";


            case 1:

                return "Bajo";


            case 2:

                return "Bateria";


            default:

                return "Original";
        }
    }


    // ==========================================
    // SELECCIONAR CARPETA DE EXPORTACION
    // ==========================================

    private string SelectExportFolder()
    {
#if UNITY_EDITOR

        return UnityEditor.EditorUtility.OpenFolderPanel(
            "Selecciona carpeta de exportacion",
            Application.persistentDataPath,
            ""
        );

#else

        string folder =
            Path.Combine(
                Application.persistentDataPath,
                exportFolder
            );


        Directory.CreateDirectory(folder);


        return folder;

#endif
    }


    // ==========================================
    // VALIDAR REFERENCIAS
    // ==========================================

    private void ValidateReferences()
    {
        if (ui == null)
        {
            throw new Exception(
                "Falta asignar la interfaz."
            );
        }


        if (accompanimentManager == null)
        {
            throw new Exception(
                "Falta asignar AccompanimentManager."
            );
        }


        if (!ui.HasGenerated)
        {
            throw new Exception(
                "Primero genera los acompanamientos."
            );
        }


        if (ui.reader == null ||
            !ui.reader.isReady ||
            ui.reader.musicData == null)
        {
            throw new Exception(
                "No existe una partitura valida."
            );
        }
    }


    // ==========================================
    // REPORTAR ERRORES
    // ==========================================

    private void ReportError(
        string message)
    {
        if (ui != null)
        {
            ui.ReportExportResult(
                "Error: " + message
            );
        }


        Debug.LogError(
            "AudioExportManager: " +
            message,
            this
        );
    }
}