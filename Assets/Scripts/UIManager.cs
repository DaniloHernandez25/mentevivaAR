using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject registerCanvas;
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject menuInformacionCanvas;


    private void Update()
    {
        // Detecta botón "Atrás" en Android
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }


    private void HandleBackButton()
    {
        // Si está en Login o Register, volver al MainMenu
        if (loginCanvas.activeSelf || registerCanvas.activeSelf || menuCanvas.activeSelf)
        {
            ShowMainMenu();
        }
        else if (mainMenuCanvas.activeSelf)
        {
            // Si ya está en el menú principal, salir de la app
            Application.Quit();
        }
    }

    // Métodos de cambio de pantallas
    public void ShowLogin()
    {
        mainMenuCanvas.SetActive(false);
        registerCanvas.SetActive(false);
        menuInformacionCanvas.SetActive(false);

        // ✅ Si ya hay sesión guardada, mostrar menú principal
        if (PlayerPrefs.HasKey("idUsuario"))
        {
            Debug.Log("Sesión detectada, abriendo menuCanvas");
            loginCanvas.SetActive(false);
            menuCanvas.SetActive(true);
        }
        else
        {
            loginCanvas.SetActive(true);
            menuCanvas.SetActive(false);
        }
    }


    public void ShowMainMenu()
    {
        loginCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);
        registerCanvas.SetActive(false);
        menuCanvas.SetActive(false);
    }

    public void ShowRegister()
    {
        loginCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        registerCanvas.SetActive(true);
        menuCanvas.SetActive(false);

    }
    public void ShowMenu()
    {
        loginCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        registerCanvas.SetActive(false);
        menuCanvas.SetActive(true);
    }
    public void ShowMenuInformacion()
    {
        loginCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        registerCanvas.SetActive(false);
        menuCanvas.SetActive(false);
        menuInformacionCanvas.SetActive(true);
    }
}
