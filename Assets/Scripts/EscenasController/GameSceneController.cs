using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneController : MonoBehaviour
{
    public static GameSceneController Instance { get; private set; }

    private bool gameWasCompleted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Presionar ESC para volver a la escena principal
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
        }
    }

    private void OnEnable()
    {
        // Suscribirse a eventos de juegos
        minijuegosOrientacion.OnGameWon += HandleGameWon;
        memoriaMinijuego.OnGameWon += HandleGameWon;
        MathQuizGame.OnGameWon += HandleGameWon;
        minijuegoLenguaje.OnGameWon += HandleGameWon;
        PuzzleManager.OnGameWon += HandleGameWon;
        Espacial.OnGameWon += HandleGameWon;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        minijuegosOrientacion.OnGameWon -= HandleGameWon;
        memoriaMinijuego.OnGameWon -= HandleGameWon;
        minijuegoLenguaje.OnGameWon -= HandleGameWon;
        MathQuizGame.OnGameWon -= HandleGameWon;
        PuzzleManager.OnGameWon -= HandleGameWon;
        Espacial.OnGameWon -= HandleGameWon;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Maneja juego completado
    private void HandleGameWon()
    {
        gameWasCompleted = true;
        Debug.Log("Juego completado!");
        Invoke(nameof(ReturnToMainScene), 3f);
    }

    private void ReturnToMainScene()
    {
        SceneManager.LoadScene(0);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Solo actuar cuando se carga la escena principal (índice 0)
        if (scene.buildIndex == 0)
        {
            UIManager uiManager = Object.FindFirstObjectByType<UIManager>();

            if (uiManager != null)
            {
                // Si el juego fue completado, mostrar el menú
                if (gameWasCompleted)
                {
                    uiManager.ShowMenu();
                    Debug.Log("Menú principal activado tras completar juego.");
                    gameWasCompleted = false;
                }
                else
                {
                    // Si se llegó presionando ESC, también mostrar el menú
                    uiManager.ShowMenu();
                }
            }
            else
            {
                Debug.LogWarning("No se encontró UIManager en la escena principal");
            }
        }
    }
}