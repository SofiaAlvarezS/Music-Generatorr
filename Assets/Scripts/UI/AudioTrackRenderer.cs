using System;
using System.Collections.Generic;
using UnityEngine;

public static class AudioTrackRenderer
{
    public const int DefaultSampleRate = 44100;

    public const int OutputChannels = 2;

    public const float MaximumDuration = 600f;


    // ==========================================
    // RENDERIZAR PISTA DE AUDIO
    // ==========================================

    public static float[] Render(
        List<AudioRenderEvent> events,
        double minimumDuration = 0.0,
        int sampleRate = DefaultSampleRate,
        bool preventClipping = true)
    {
        if (events == null)
        {
            throw new ArgumentNullException(
                nameof(events)
            );
        }


        if (sampleRate < 8000 ||
            sampleRate > 48000)
        {
            throw new ArgumentException(
                "Frecuencia de muestreo invalida."
            );
        }


        if (double.IsNaN(minimumDuration) ||
            double.IsInfinity(minimumDuration) ||
            minimumDuration < 0.0)
        {
            throw new ArgumentException(
                "Duracion minima invalida."
            );
        }


        // ======================================
        // CALCULAR DURACION TOTAL
        // ======================================

        double totalDuration =
            GetTotalDuration(
                events,
                minimumDuration
            );


        if (totalDuration > MaximumDuration)
        {
            throw new Exception(
                "La pista supera la duracion " +
                "maxima permitida."
            );
        }


        int totalFrames = Math.Max(
            1,
            (int)Math.Ceiling(
                totalDuration * sampleRate
            )
        );


        // Dos muestras por frame:
        // canal izquierdo y derecho.

        float[] output =
            new float[
                totalFrames * OutputChannels
            ];


        // ======================================
        // CACHE DE MUESTRAS
        // ======================================

        Dictionary<AudioClip, float[]> clipCache =
            new Dictionary<AudioClip, float[]>();


        // ======================================
        // RENDERIZAR EVENTOS
        // ======================================

        foreach (AudioRenderEvent audioEvent
                 in events)
        {
            if (!audioEvent.IsValid)
            {
                throw new Exception(
                    "Se encontro un evento " +
                    "de audio invalido."
                );
            }


            if (audioEvent.volume == 0f)
            {
                continue;
            }


            RenderEvent(
                audioEvent,
                output,
                sampleRate,
                clipCache
            );
        }


        // ======================================
        // PROTECCION CONTRA SATURACION
        // ======================================

        if (preventClipping)
        {
            PreventClipping(output);
        }


        return output;
    }


    // ==========================================
    // CALCULAR DURACION TOTAL
    // ==========================================

    public static double GetTotalDuration(
        List<AudioRenderEvent> events,
        double minimumDuration = 0.0)
    {
        double duration = minimumDuration;


        foreach (AudioRenderEvent audioEvent
                 in events)
        {
            if (!audioEvent.IsValid)
            {
                throw new Exception(
                    "Evento invalido al calcular " +
                    "la duracion."
                );
            }


            duration = Math.Max(
                duration,
                audioEvent.EndTime
            );
        }


        return duration;
    }


    // ==========================================
    // RENDERIZAR EVENTO INDIVIDUAL
    // ==========================================

    private static void RenderEvent(
        AudioRenderEvent audioEvent,
        float[] output,
        int outputSampleRate,
        Dictionary<AudioClip, float[]> clipCache)
    {
        AudioClip clip =
            audioEvent.clip;


        int sourceChannels =
            clip.channels;

        int sourceFrames =
            clip.samples;

        int sourceSampleRate =
            clip.frequency;


        if (sourceFrames <= 0 ||
            sourceSampleRate <= 0)
        {
            throw new Exception(
                "AudioClip sin muestras validas."
            );
        }


        if (sourceChannels != 1 &&
            sourceChannels != 2)
        {
            throw new Exception(
                "Solo se admiten clips " +
                "mono y estereo."
            );
        }


        // ======================================
        // OBTENER MUESTRAS DEL AUDIOCLIP
        // ======================================

        float[] sourceData;


        if (!clipCache.TryGetValue(
            clip,
            out sourceData))
        {
            sourceData =
                new float[
                    sourceFrames * sourceChannels
                ];


            bool success =
                clip.GetData(
                    sourceData,
                    0
                );


            if (!success)
            {
                throw new Exception(
                    "No se pudieron leer " +
                    "las muestras de: " +
                    clip.name
                );
            }


            clipCache.Add(
                clip,
                sourceData
            );
        }


        // ======================================
        // POSICION DEL EVENTO EN LA PISTA
        // ======================================

        int startFrame =
            (int)Math.Round(
                audioEvent.startTime *
                outputSampleRate
            );


        int outputFrames =
            output.Length / OutputChannels;


        // ======================================
        // CONVERSION DE FRECUENCIA
        // ======================================

        double sampleRatio =
            (double)sourceSampleRate /
            outputSampleRate;


        int renderedFrames =
            (int)Math.Ceiling(
                sourceFrames / sampleRatio
            );


        // ======================================
        // RENDERIZAR CADA MUESTRA
        // ======================================

        for (int frame = 0;
             frame < renderedFrames;
             frame++)
        {
            int destinationFrame =
                startFrame + frame;


            if (destinationFrame >= outputFrames)
            {
                break;
            }


            double sourcePosition =
                frame * sampleRatio;


            int sourceIndex =
                (int)sourcePosition;


            if (sourceIndex >= sourceFrames)
            {
                break;
            }


            int nextSourceIndex =
                Mathf.Min(
                    sourceIndex + 1,
                    sourceFrames - 1
                );


            float interpolation =
                (float)(
                    sourcePosition -
                    sourceIndex
                );


            float left;
            float right;


            // ==================================
            // AUDIO MONO
            // ==================================

            if (sourceChannels == 1)
            {
                float sample =
                    Mathf.Lerp(
                        sourceData[sourceIndex],
                        sourceData[nextSourceIndex],
                        interpolation
                    );


                left = sample;

                right = sample;
            }


            // ==================================
            // AUDIO ESTEREO
            // ==================================

            else
            {
                int current =
                    sourceIndex * 2;

                int next =
                    nextSourceIndex * 2;


                left =
                    Mathf.Lerp(
                        sourceData[current],
                        sourceData[next],
                        interpolation
                    );


                right =
                    Mathf.Lerp(
                        sourceData[current + 1],
                        sourceData[next + 1],
                        interpolation
                    );
            }


            // ==================================
            // APLICAR VOLUMEN
            // ==================================

            left *= audioEvent.volume;

            right *= audioEvent.volume;


            // ==================================
            // SUMAR AL BUFFER FINAL
            // ==================================

            int destinationIndex =
                destinationFrame * 2;


            output[destinationIndex] +=
                left;


            output[destinationIndex + 1] +=
                right;
        }
    }


    // ==========================================
    // OBTENER PICO MAXIMO
    // ==========================================

    public static float GetPeak(
        float[] samples)
    {
        if (samples == null)
        {
            throw new ArgumentNullException(
                nameof(samples)
            );
        }


        float peak = 0f;


        for (int i = 0;
             i < samples.Length;
             i++)
        {
            if (float.IsNaN(samples[i]) ||
                float.IsInfinity(samples[i]))
            {
                throw new Exception(
                    "La pista contiene " +
                    "muestras invalidas."
                );
            }


            float absolute =
                Mathf.Abs(samples[i]);


            if (absolute > peak)
            {
                peak = absolute;
            }
        }


        return peak;
    }


    // ==========================================
    // PROTECCION CONTRA CLIPPING
    // ==========================================

    public static void PreventClipping(
        float[] samples,
        float maximumPeak = 0.98f)
    {
        maximumPeak =
            Mathf.Clamp(
                maximumPeak,
                0.01f,
                1f
            );


        float peak =
            GetPeak(samples);


        if (peak <= maximumPeak)
        {
            return;
        }


        float gain =
            maximumPeak / peak;


        ApplyGain(
            samples,
            gain
        );


        Debug.Log(
            "AudioTrackRenderer: " +
            "proteccion contra clipping aplicada. " +
            "Pico original: " +
            peak.ToString("F3") +
            " | Ganancia: " +
            gain.ToString("F3")
        );
    }


    // ==========================================
    // APLICAR GANANCIA
    // ==========================================

    public static void ApplyGain(
        float[] samples,
        float gain)
    {
        if (samples == null)
        {
            throw new ArgumentNullException(
                nameof(samples)
            );
        }


        if (float.IsNaN(gain) ||
            float.IsInfinity(gain) ||
            gain < 0f)
        {
            throw new ArgumentException(
                "Ganancia invalida."
            );
        }


        for (int i = 0;
             i < samples.Length;
             i++)
        {
            samples[i] *= gain;
        }
    }
}