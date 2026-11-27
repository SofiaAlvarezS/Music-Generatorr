using UnityEngine;

public class GuitarSynthesizer : MonoBehaviour
{
    [Header("Cuerda")]
    [Range(0.90f, 0.9999f)]
    public float damping = 0.9975f;

    [Range(0f, 1f)]
    public float noiseAmount = 1f;

    [Header("Ataque de púa")]
    [Range(0f, 1f)]
    public float pickAmount = 0.20f;

    [Range(500f, 12000f)]
    public float pickBrightness = 5500f;

    [Header("ADSR")]
    [Tooltip("Ataque inicial")]
    public float attack = 0.002f;

    [Tooltip("Caída inicial")]
    public float decay = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("Nivel sostenido de la cuerda")]
    public float sustain = 0.30f;

    [Tooltip("Cola después de terminar la nota")]
    public float release = 1.0f;

    [Header("Filtro")]
    [Range(100f, 12000f)]
    public float cutoff = 4200f;

    [Header("Cuerpo de guitarra")]
    [Range(0f, 1f)]
    public float bodyResonance = 0.18f;

    [Range(0f, 1f)]
    public float bodyAmount = 0.20f;

    [Header("Audio")]
    public int sampleRate = 44100;

    public AudioClip GenerateNote(
        float frequency,
        float duration
    )
    {
        if (frequency <= 0f)
        {
            Debug.LogError(
                "La frecuencia debe ser mayor que 0."
            );

            return null;
        }

        if (duration <= 0f)
        {
            Debug.LogError(
                "La duración debe ser mayor que 0."
            );

            return null;
        }

        float totalDuration =
            duration + release;


        int sampleCount =
            Mathf.CeilToInt(
                totalDuration * sampleRate
            );


        AudioClip clip =
            AudioClip.Create(
                "AcousticGuitarNote",
                sampleCount,
                1,
                sampleRate,
                false
            );


        float[] samples =
            new float[sampleCount];


        int delaySamples =
            Mathf.Max(
                2,
                Mathf.RoundToInt(
                    sampleRate / frequency
                )
            );


        float[] stringBuffer =
            new float[delaySamples];


        for (int i = 0; i < delaySamples; i++)
        {
            float noise =
                Random.Range(-1f, 1f);

            stringBuffer[i] =
                noise * noiseAmount;
        }


        float filterAlpha =
            1f -
            Mathf.Exp(
                -2f *
                Mathf.PI *
                cutoff /
                sampleRate
            );


        float previousOutput = 0f;


        for (int i = 0; i < sampleCount; i++)
        {
            float time =
                (float)i / sampleRate;


            int index =
                i % delaySamples;


            int nextIndex =
                (index + 1) % delaySamples;


            float current =
                stringBuffer[index];


            float next =
                stringBuffer[nextIndex];


            float averaged =
                0.5f *
                (current + next);


            averaged *= damping;


            stringBuffer[index] =
                averaged;


            float sample =
                current;


            previousOutput =
                previousOutput +
                filterAlpha *
                (sample - previousOutput);


            sample =
                previousOutput;


            if (time < 0.02f)
            {
                float pickEnvelope =
                    1f - (time / 0.02f);

                float pickNoise =
                    Random.Range(
                        -1f,
                        1f
                    );

                sample +=
                    pickNoise *
                    pickEnvelope *
                    pickAmount *
                    0.15f;
            }


            float envelope =
                GetEnvelope(
                    time,
                    duration
                );


            sample *= envelope;


            samples[i] =
                sample;
        }


        if (bodyAmount > 0f)
        {
            for (
                int i = 1;
                i < sampleCount;
                i++
            )
            {
                samples[i] +=
                    samples[i - 1] *
                    bodyResonance *
                    bodyAmount;
            }
        }


        NormalizeSamples(
            samples
        );


        clip.SetData(
            samples,
            0
        );


        return clip;
    }


    private float GetEnvelope(
        float time,
        float noteDuration
    )
    {
    
        if (time < attack)
        {
            if (attack <= 0f)
            {
                return 1f;
            }

            return time / attack;
        }

        if (time < attack + decay)
        {
            if (decay <= 0f)
            {
                return sustain;
            }

            float t =
                (time - attack) /
                decay;


            return Mathf.Lerp(
                1f,
                sustain,
                t
            );
        }

        if (time < noteDuration)
        {
            return sustain;
        }

        float releaseTime =
            time - noteDuration;


        if (releaseTime < release)
        {
            if (release <= 0f)
            {
                return 0f;
            }

            float t =
                releaseTime /
                release;


            return Mathf.Lerp(
                sustain,
                0f,
                t
            );
        }


        return 0f;
    }


    private void NormalizeSamples(
        float[] samples
    )
    {
        float maximum = 0f;


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