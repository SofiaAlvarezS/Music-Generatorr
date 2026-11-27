using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SintetizadorWavetable : MonoBehaviour
{
    [Header("Configuración del Clic Grave")]
    public int tamanoTabla = 2048;
    public float frecuenciaBase = 220f;    // Tono grave (Frecuencia en Hz)
    public float duracionSegundos = 0.08f; // Duración corta para el impacto

    [Header("Control de Volumen")]
    [Range(0f, 1f)]
    public float volumen = 0.2f;           // Deslizador de volumen (0.2 = 20%)

    private float[] tablaOndas;
    private double fase = 0;
    private double incrementoFase;
    private int sampleRate;
    private bool reproduciendo = false;
    private float tiempoActual = 0;
    private AudioSource audioSource;

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        if (sampleRate <= 0) sampleRate = 44100;

        GenerarTablaDeOndas();

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Audio 2D

        // Genera un clip mudo para mantener activo el motor de audio de Unity
        if (audioSource.clip == null)
        {
            audioSource.clip = AudioClip.Create("DummySilent", 44100, 1, sampleRate, false);
        }

        audioSource.loop = true;
        audioSource.playOnAwake = true;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void GenerarTablaDeOndas()
    {
        tablaOndas = new float[tamanoTabla];
        for (int i = 0; i < tamanoTabla; i++)
        {
            float t = (float)i / tamanoTabla;
            tablaOndas[i] = Mathf.Sin(2 * Mathf.PI * t) * 0.7f +
                            Mathf.Sin(6 * Mathf.PI * t) * 0.3f;
        }
    }

    public void ReproducirClicUI()
    {
        fase = 0;
        tiempoActual = 0;
        incrementoFase = frecuenciaBase * tamanoTabla / sampleRate;
        reproduciendo = true;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!reproduciendo)
        {
            System.Array.Clear(data, 0, data.Length);
            return;
        }

        for (int i = 0; i < data.Length; i += channels)
        {
            float envolvente = Mathf.Max(0, 1.0f - (tiempoActual / duracionSegundos));

            if (envolvente <= 0)
            {
                reproduciendo = false;
            }

            int indiceActual = (int)fase % tamanoTabla;

            // Se aplica el multiplicador de volumen directamente en la señal final
            float muestra = tablaOndas[indiceActual] * envolvente * volumen;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = muestra;
            }

            fase += incrementoFase;
            tiempoActual += 1.0f / sampleRate;
        }
    }
}