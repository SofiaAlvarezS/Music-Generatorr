using UnityEngine;

public class DrumSynthesizer : MonoBehaviour
{
    [Header("Audio")]
    public int sampleRate = 44100;


    [Header("Niveles")]
    [Range(0f, 1f)]
    public float kickLevel = 0.9f;

    [Range(0f, 1f)]
    public float snareLevel = 0.7f;

    [Range(0f, 1f)]
    public float hiHatLevel = 0.45f;


    // =========================================================
    // GENERAR SONIDO SEGÚN MIDI
    // =========================================================

    public AudioClip GenerateDrum(
        int midiNote,
        float duration,
        float velocity
    )
    {
        switch (midiNote)
        {
            case 36:
                return GenerateKick(
                    duration,
                    velocity
                );

            case 38:
                return GenerateSnare(
                    duration,
                    velocity
                );

            case 42:
                return GenerateClosedHiHat(
                    duration,
                    velocity
                );

            default:
                Debug.LogWarning(
                    "No existe un sonido de batería " +
                    "para la nota MIDI " +
                    midiNote
                );

                return null;
        }
    }


    // =========================================================
    // KICK
    // =========================================================

    private AudioClip GenerateKick(
        float duration,
        float velocity
    )
    {
        int sampleCount =
            Mathf.CeilToInt(
                duration * sampleRate
            );

        AudioClip clip =
            AudioClip.Create(
                "Kick",
                sampleCount,
                1,
                sampleRate,
                false
            );

        float[] samples =
            new float[sampleCount];


        float startFrequency =
            150f;

        float endFrequency =
            50f;


        for (int i = 0; i < sampleCount; i++)
        {
            float time =
                (float)i / sampleRate;

            float normalizedTime =
                time / duration;


            // Caída de frecuencia
            float frequency =
                Mathf.Lerp(
                    startFrequency,
                    endFrequency,
                    normalizedTime
                );


            // Envolvente de amplitud
            float amplitude =
                Mathf.Exp(
                    -12f * time
                );


            // Onda sinusoidal
            float sample =
                Mathf.Sin(
                    2f *
                    Mathf.PI *
                    frequency *
                    time
                );


            samples[i] =
                sample *
                amplitude *
                velocity *
                kickLevel;
        }


        Normalize(samples);

        clip.SetData(
            samples,
            0
        );

        return clip;
    }


    // =========================================================
    // SNARE
    // =========================================================

    private AudioClip GenerateSnare(
        float duration,
        float velocity
    )
    {
        int sampleCount =
            Mathf.CeilToInt(
                duration * sampleRate
            );

        AudioClip clip =
            AudioClip.Create(
                "Snare",
                sampleCount,
                1,
                sampleRate,
                false
            );

        float[] samples =
            new float[sampleCount];


        for (int i = 0; i < sampleCount; i++)
        {
            float time =
                (float)i / sampleRate;


            // Envolvente rápida
            float amplitude =
                Mathf.Exp(
                    -18f * time
                );


            // Ruido
            float noise =
                Random.Range(
                    -1f,
                    1f
                );


            // Componente tonal
            float tone =
                Mathf.Sin(
                    2f *
                    Mathf.PI *
                    180f *
                    time
                );


            float sample =
                noise * 0.75f +
                tone * 0.25f;


            samples[i] =
                sample *
                amplitude *
                velocity *
                snareLevel;
        }


        Normalize(samples);

        clip.SetData(
            samples,
            0
        );

        return clip;
    }


    // =========================================================
    // CLOSED HI-HAT
    // =========================================================

    private AudioClip GenerateClosedHiHat(
        float duration,
        float velocity
    )
    {
        int sampleCount =
            Mathf.CeilToInt(
                duration * sampleRate
            );

        AudioClip clip =
            AudioClip.Create(
                "ClosedHiHat",
                sampleCount,
                1,
                sampleRate,
                false
            );

        float[] samples =
            new float[sampleCount];


        // Estado del filtro
        float previousSample = 0f;


        float cutoff =
            7000f;


        float alpha =
            1f -
            Mathf.Exp(
                -2f *
                Mathf.PI *
                cutoff /
                sampleRate
            );


        for (int i = 0; i < sampleCount; i++)
        {
            float time =
                (float)i / sampleRate;


            // Ruido
            float noise =
                Random.Range(
                    -1f,
                    1f
                );


            // High frequency content
            previousSample =
                previousSample +
                alpha *
                (noise - previousSample);


            // Envolvente muy rápida
            float amplitude =
                Mathf.Exp(
                    -40f * time
                );


            samples[i] =
                previousSample *
                amplitude *
                velocity *
                hiHatLevel;
        }


        Normalize(samples);

        clip.SetData(
            samples,
            0
        );

        return clip;
    }


    // =========================================================
    // NORMALIZACIÓN
    // =========================================================

    private void Normalize(
        float[] samples
    )
    {
        float maximum =
            0f;


        for (
            int i = 0;
            i < samples.Length;
            i++
        )
        {
            float value =
                Mathf.Abs(
                    samples[i]
                );


            if (value > maximum)
            {
                maximum = value;
            }
        }


        if (maximum <= 0f)
        {
            return;
        }


        float targetPeak =
            0.8f;


        float factor =
            targetPeak /
            maximum;


        for (
            int i = 0;
            i < samples.Length;
            i++
        )
        {
            samples[i] *=
                factor;
        }
    }
}