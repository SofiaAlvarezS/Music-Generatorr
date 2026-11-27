using UnityEngine;

public class BassSynthesizer : MonoBehaviour
{
    [Header("Osciladores")]

    [Range(0f, 1f)]
    public float sineAmount = 0.7f;

    [Range(0f, 1f)]
    public float sawAmount = 0.3f;


    [Header("Envelope ADSR")]

    public float attack = 0.02f;

    public float decay = 0.15f;

    [Range(0f, 1f)]
    public float sustain = 0.7f;

    public float release = 0.15f;


    [Header("Filtro")]

    [Range(50f, 5000f)]
    public float cutoff = 800f;


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
                totalDuration *
                sampleRate
            );

        AudioClip clip =
            AudioClip.Create(
                "BassNote",
                sampleCount,
                1,
                sampleRate,
                false
            );


        float[] samples =
            new float[sampleCount];


        float previousSample = 0f;


        float filterAlpha =
            1f -
            Mathf.Exp(
                -2f *
                Mathf.PI *
                cutoff /
                sampleRate
            );


        for (
            int i = 0;
            i < sampleCount;
            i++
        )
        {
            float time =
                (float)i /
                sampleRate;

            float sine =
                Mathf.Sin(
                    2f *
                    Mathf.PI *
                    frequency *
                    time
                );


            float phase =
                (frequency * time) % 1f;


            float saw =
                2f * phase - 1f;

            float sample =
                sine * sineAmount +
                saw * sawAmount;
            float envelope =
                GetEnvelope(
                    time,
                    duration
                );


            sample *= envelope;

            previousSample =
                previousSample +
                filterAlpha *
                (sample - previousSample);


            samples[i] =
                previousSample;
        }


        NormalizeSamples(samples);

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
            float decayTime =
                time - attack;


            float t =
                decayTime / decay;


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
                releaseTime / release;


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
        float maximum =
            0f;


        for (
            int i = 0;
            i < samples.Length;
            i++
        )
        {
            float absoluteValue =
                Mathf.Abs(
                    samples[i]
                );


            if (
                absoluteValue >
                maximum
            )
            {
                maximum =
                    absoluteValue;
            }
        }


        // Si no hay señal, no hacemos nada
        if (maximum <= 0f)
        {
            return;
        }



        float targetPeak =
            0.8f;


        float normalizationFactor =
            targetPeak /
            maximum;


        for (
            int i = 0;
            i < samples.Length;
            i++
        )
        {
            samples[i] *=
                normalizationFactor;
        }
    }
}
