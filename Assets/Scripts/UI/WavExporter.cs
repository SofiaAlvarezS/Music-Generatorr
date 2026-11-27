using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class WavExporter
{
    public const int DefaultSampleRate = 44100;

    public const int DefaultChannels = 2;

    public const int BitsPerSample = 16;


    // ==========================================
    // EXPORTAR ARCHIVO WAV
    // ==========================================

    public static string Export(
        string filePath,
        float[] samples,
        int sampleRate = DefaultSampleRate,
        int channels = DefaultChannels)
    {
        // Validar ruta.

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "La ruta del archivo esta vacia."
            );
        }


        // Validar muestras.

        if (samples == null ||
            samples.Length == 0)
        {
            throw new ArgumentException(
                "No existen muestras para exportar."
            );
        }


        // Validar frecuencia de muestreo.

        if (sampleRate < 8000 ||
            sampleRate > 48000)
        {
            throw new ArgumentException(
                "Frecuencia de muestreo invalida."
            );
        }


        // Validar canales.

        if (channels != 1 &&
            channels != 2)
        {
            throw new ArgumentException(
                "Solo se admite audio mono o estereo."
            );
        }


        if (samples.Length % channels != 0)
        {
            throw new ArgumentException(
                "La cantidad de muestras no coincide " +
                "con el numero de canales."
            );
        }


        // ======================================
        // PREPARAR RUTA
        // ======================================

        if (!string.Equals(
            Path.GetExtension(filePath),
            ".wav",
            StringComparison.OrdinalIgnoreCase))
        {
            filePath += ".wav";
        }


        filePath =
            Path.GetFullPath(filePath);


        string directory =
            Path.GetDirectoryName(filePath);


        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(
                directory
            );
        }


        // ======================================
        // CALCULAR PARAMETROS WAV
        // ======================================

        int bytesPerSample =
            BitsPerSample / 8;


        int blockAlign =
            channels * bytesPerSample;


        int byteRate =
            sampleRate * blockAlign;


        long dataSizeLong =
            (long)samples.Length *
            bytesPerSample;


        if (dataSizeLong >
            uint.MaxValue - 36L)
        {
            throw new Exception(
                "El archivo supera el limite " +
                "del formato WAV RIFF."
            );
        }


        uint dataSize =
            (uint)dataSizeLong;


        uint riffSize =
            36u + dataSize;


        // ======================================
        // VALIDAR TODAS LAS MUESTRAS
        // ======================================

        for (int i = 0;
             i < samples.Length;
             i++)
        {
            if (float.IsNaN(samples[i]) ||
                float.IsInfinity(samples[i]))
            {
                throw new Exception(
                    "La pista contiene muestras invalidas."
                );
            }
        }


        // Evitar sobrescribir archivos existentes.

        filePath =
            GetUniqueFilePath(filePath);


        // ======================================
        // CREAR ARCHIVO WAV
        // ======================================

        bool fileCreated = false;


        try
        {
            using (FileStream stream =
                new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write))
            {
                fileCreated = true;


                using (BinaryWriter writer =
                    new BinaryWriter(stream))
                {
                    WriteHeader(
                        writer,
                        riffSize,
                        dataSize,
                        sampleRate,
                        channels,
                        byteRate,
                        blockAlign
                    );


                    WriteSamples(
                        writer,
                        samples
                    );
                }
            }
        }
        catch
        {
            if (fileCreated &&
                File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            throw;
        }


        // ======================================
        // INFORMACION DE EXPORTACION
        // ======================================

        double duration =
            (double)samples.Length /
            channels /
            sampleRate;


        Debug.Log(
            "================================"
        );

        Debug.Log(
            "ARCHIVO WAV EXPORTADO"
        );

        Debug.Log(
            "Archivo: " +
            Path.GetFileName(filePath)
        );

        Debug.Log(
            "Frecuencia: " +
            sampleRate +
            " Hz"
        );

        Debug.Log(
            "Canales: " +
            channels
        );

        Debug.Log(
            "Profundidad: " +
            BitsPerSample +
            " bits"
        );

        Debug.Log(
            "Duracion: " +
            duration.ToString("F2") +
            " segundos"
        );

        Debug.Log(
            "Ruta: " +
            filePath
        );

        Debug.Log(
            "================================"
        );


        return filePath;
    }


    // ==========================================
    // ESCRIBIR CABECERA WAV
    // ==========================================

    private static void WriteHeader(
        BinaryWriter writer,
        uint riffSize,
        uint dataSize,
        int sampleRate,
        int channels,
        int byteRate,
        int blockAlign)
    {
        // RIFF Chunk

        WriteText(
            writer,
            "RIFF"
        );

        writer.Write(riffSize);

        WriteText(
            writer,
            "WAVE"
        );


        // Format Chunk

        WriteText(
            writer,
            "fmt "
        );


        // Tamano del bloque PCM.

        writer.Write(16u);


        // Formato 1 = PCM sin compresion.

        writer.Write((ushort)1);


        // Numero de canales.

        writer.Write(
            (ushort)channels
        );


        // Frecuencia de muestreo.

        writer.Write(
            (uint)sampleRate
        );


        // Bytes por segundo.

        writer.Write(
            (uint)byteRate
        );


        // Alineacion de bloques.

        writer.Write(
            (ushort)blockAlign
        );


        // Bits por muestra.

        writer.Write(
            (ushort)BitsPerSample
        );


        // Data Chunk

        WriteText(
            writer,
            "data"
        );


        writer.Write(dataSize);
    }


    // ==========================================
    // ESCRIBIR MUESTRAS PCM
    // ==========================================

    private static void WriteSamples(
        BinaryWriter writer,
        float[] samples)
    {
        for (int i = 0;
             i < samples.Length;
             i++)
        {
            float sample =
                Mathf.Clamp(
                    samples[i],
                    -1f,
                    1f
                );


            short pcmSample;


            if (sample >= 0f)
            {
                pcmSample =
                    (short)Mathf.RoundToInt(
                        sample * 32767f
                    );
            }
            else
            {
                pcmSample =
                    (short)Mathf.RoundToInt(
                        sample * 32768f
                    );
            }


            writer.Write(
                pcmSample
            );
        }
    }


    // ==========================================
    // ESCRIBIR IDENTIFICADORES ASCII
    // ==========================================

    private static void WriteText(
        BinaryWriter writer,
        string text)
    {
        byte[] bytes =
            Encoding.ASCII.GetBytes(text);


        writer.Write(bytes);
    }


    // ==========================================
    // EVITAR SOBRESCRIBIR ARCHIVOS
    // ==========================================

    private static string GetUniqueFilePath(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }


        string directory =
            Path.GetDirectoryName(filePath);


        string fileName =
            Path.GetFileNameWithoutExtension(
                filePath
            );


        string extension =
            Path.GetExtension(filePath);


        int counter = 1;


        string newPath;


        do
        {
            newPath =
                Path.Combine(
                    directory,
                    fileName +
                    "_" +
                    counter +
                    extension
                );


            counter++;

        }
        while (File.Exists(newPath));


        return newPath;
    }
}