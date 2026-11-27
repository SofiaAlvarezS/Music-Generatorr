using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingMenu : MonoBehaviour
{
    [SerializeField] private string nextScene = "MainMenu";

    public void Continuar()
    {
        SceneManager.LoadScene(nextScene);
    }
}

