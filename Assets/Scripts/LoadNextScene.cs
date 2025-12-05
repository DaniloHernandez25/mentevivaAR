using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadNextScene : MonoBehaviour
{
    [Header("Configura aquí el índice de la escena a cargar")]
    [SerializeField] private int sceneIndex; // Índice de la escena en Build Settings

    // Método que se puede llamar desde un botón UI
    public void LoadScene()
    {
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogWarning("El índice " + sceneIndex + " no existe en los Build Settings.");
        }
    }
}
