using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Registro : MonoBehaviour
{
    [Header("Campos de Entrada")]
    [SerializeField] private TMP_InputField nombreInput;
    [SerializeField] private TMP_InputField edadInput;
    
    [Header("Mensajes")]
    [SerializeField] private TextMeshProUGUI mensajeText;
    
    [Header("Panel de PIN")]
    [SerializeField] private GameObject panelPIN;
    [SerializeField] private TextMeshProUGUI pinText;

    [Header("Botón Registro")]
    [SerializeField] private UnityEngine.UI.Button botonRegistrar;

    private string firebaseURL = "https://fcar-9d923-default-rtdb.firebaseio.com";

    void Start()
    {
        if (panelPIN != null)
            panelPIN.SetActive(false);
    }

    public void OnClickRegistrar()
    {
        string nombre = nombreInput.text.Trim();
        string edadTexto = edadInput.text.Trim();

        if (string.IsNullOrEmpty(nombre))
        {
            MostrarMensaje("El nombre no puede estar vacío");
            return;
        }

        if (string.IsNullOrEmpty(edadTexto))
        {
            MostrarMensaje("La edad no puede estar vacía");
            return;
        }

        if (!int.TryParse(edadTexto, out int edad) || edad <= 0 || edad > 120)
        {
            MostrarMensaje("Ingresa una edad válida (1-120)");
            return;
        }

        MostrarMensaje("Registrando...");

        if (botonRegistrar != null)
            botonRegistrar.interactable = false;

        StartCoroutine(RegistrarUsuario(nombre, edad));
    }

    IEnumerator RegistrarUsuario(string nombre, int edad)
    {
        string pin = GenerarPIN();
        Debug.Log($"[Registro] PIN generado: {pin}");

        yield return StartCoroutine(VerificarYRegistrar(nombre, edad, pin));
    }

    IEnumerator VerificarYRegistrar(string nombre, int edad, string pin)
    {
        string urlGet = $"{firebaseURL}/usuarios.json";

        using (UnityWebRequest www = UnityWebRequest.Get(urlGet))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Registro] Error al obtener usuarios: {www.error}");
                MostrarMensaje($"Error de conexión: {www.error}");

                if (botonRegistrar != null)
                    botonRegistrar.interactable = true;

                yield break;
            }

            string respuesta = www.downloadHandler.text;
            bool pinExiste = false;

            if (!string.IsNullOrEmpty(respuesta) && respuesta != "null")
            {
                if (respuesta.Contains($"\"pin\":\"{pin}\""))
                {
                    pinExiste = true;
                    Debug.Log($"[Registro] PIN {pin} ya existe, generando uno nuevo...");
                }
            }

            if (pinExiste)
            {
                string nuevoPIN = GenerarPIN();
                yield return StartCoroutine(VerificarYRegistrar(nombre, edad, nuevoPIN));
            }
            else
            {
                yield return StartCoroutine(CrearNuevoUsuario(nombre, edad, pin));
            }
        }
    }

    IEnumerator CrearNuevoUsuario(string nombre, int edad, string pin)
    {
        UsuarioData nuevoUsuario = new UsuarioData
        {
            nombre = nombre,
            edad = edad,
            pin = pin
        };

        string json = JsonUtility.ToJson(nuevoUsuario);
        string urlPut = $"{firebaseURL}/usuarios/{pin}.json";

        using (UnityWebRequest www = UnityWebRequest.Put(urlPut, json))
        {
            www.method = "PUT";
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Registro] Error al crear usuario: {www.error}");
                MostrarMensaje($"Error al registrar: {www.error}");

                if (botonRegistrar != null)
                    botonRegistrar.interactable = true;
            }
            else
            {
                Debug.Log($"[Registro] Usuario registrado con PIN {pin}");
                MostrarPIN(pin);
                MostrarMensaje("¡Usuario registrado exitosamente!");

                nombreInput.text = "";
                edadInput.text = "";
            }
        }
    }

    string GenerarPIN()
    {
        int numeroAleatorio = Random.Range(1000, 10000);
        return numeroAleatorio.ToString();
    }

    void MostrarPIN(string pin)
    {
        if (panelPIN != null && pinText != null)
        {
            panelPIN.SetActive(true);
            pinText.text = $"Tu PIN es:\n{pin}\n\n¡Guárdalo para iniciar sesión!";
            Debug.Log($"[Registro] Mostrando PIN: {pin}");
        }
        else
        {
            Debug.LogWarning("[Registro] Panel PIN o texto PIN no asignado en el Inspector");
        }
    }

    void MostrarMensaje(string mensaje)
    {
        if (mensajeText != null)
        {
            mensajeText.text = mensaje;
            Debug.Log($"[Registro] Mensaje: {mensaje}");
            StartCoroutine(BorrarMensajeDespuesDeDelay(3f));
        }
    }

    IEnumerator BorrarMensajeDespuesDeDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (mensajeText != null)
            mensajeText.text = "";
    }

    public void CerrarPanelPIN()
    {
        if (panelPIN != null)
            panelPIN.SetActive(false);

        if (botonRegistrar != null)
            botonRegistrar.interactable = true;

        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowMainMenu();
        }
    }

    [System.Serializable]
    public class UsuarioData
    {
        public string nombre;
        public int edad;
        public string pin;
    }
}
