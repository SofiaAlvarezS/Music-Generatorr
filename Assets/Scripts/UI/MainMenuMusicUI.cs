using UnityEngine;
using TMPro;

public class MainMenuMusicUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown instrumentDropdown;

    [Header("Sistema de música")]
    public PlaybackManager playbackManager;


    // =========================================================
    // GENERAR / REPRODUCIR
    // =========================================================

    public void Generar()
    {
        if (instrumentDropdown == null)
        {
            Debug.LogError("No se asignó el Dropdown de instrumentos.");
            return;
        }

        if (playbackManager == null)
        {
            Debug.LogError("No se asignó el PlaybackManager.");
            return;
        }


        // Seleccionar instrumento
        switch (instrumentDropdown.value)
        {
            case 1:
                // Guitarra
                playbackManager.SetPlaybackType(
                    PlaybackManager.PlaybackType.Guitar
                );

                Debug.Log("Main Menu → Guitarra seleccionada.");
                break;


            case 2:
                // Bajo
                playbackManager.SetPlaybackType(
                    PlaybackManager.PlaybackType.Bass
                );

                Debug.Log("Main Menu → Bajo seleccionado.");
                break;


            case 3:
                // Batería
                playbackManager.SetPlaybackType(
                    PlaybackManager.PlaybackType.Drums
                );

                Debug.Log("Main Menu → Batería seleccionada.");
                break;


            default:
                Debug.LogWarning(
                    "Selecciona un instrumento antes de reproducir."
                );
                return;
        }


        // Reproducir
        playbackManager.Play();
    }


    // =========================================================
    // DETENER
    // =========================================================

    public void Detener()
    {
        if (playbackManager == null)
        {
            Debug.LogError("No se asignó el PlaybackManager.");
            return;
        }

        playbackManager.Stop();
    }
}
