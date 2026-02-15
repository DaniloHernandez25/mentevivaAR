using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using Firebase.Auth;

public class Login : MonoBehaviour
{
    [SerializeField] private TMP_InputField pinInput;      
    [SerializeField] private GameObject mensajeGO;
    private TMP_Text mensajeTexto;

    [SerializeField] private UIManager uiManager;

    private string firebaseURL = "https://fcar-9d923-default-rtdb.firebaseio.com";
    private string authToken = ""; // <--- AGREGADO: Para guardar el token de seguridad

    private void Start()
    {
        if (mensajeGO != null)
        {
            mensajeTexto = mensajeGO.GetComponent<TMP_Text>();
            mensajeGO.SetActive(false);
        }

        // --- BLOQUE AGREGADO PARA SEGURIDAD ---
        StartCoroutine(ObtenerToken()); 
        // ---------------------------------------

        if (PlayerPrefs.HasKey("idUsuario"))
        {
            Debug.Log("Usuario ya logueado: " + PlayerPrefs.GetString("nombreUsuario"));
            uiManager?.ShowMenu();
        }
    }

    // --- CORRUTINA PARA AUTENTICACIÓN (Igual que en Registro) ---
    IEnumerator ObtenerToken()
    {
        var auth = FirebaseAuth.DefaultInstance;
        var task = auth.SignInAnonymouslyAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("[Login] Error al autenticar: " + task.Exception);
            MostrarMensaje("Error de seguridad al conectar");
        }
        else
        {
            var tokenTask = task.Result.User.TokenAsync(false); 
            yield return new WaitUntil(() => tokenTask.IsCompleted);

            if (tokenTask.IsFaulted)
            {
                Debug.LogError("[Login] Error al obtener token: " + tokenTask.Exception);
            }
            else
            {
                authToken = tokenTask.Result;
                Debug.Log("[Login] Autenticación exitosa. Token obtenido.");
            }
        }
    }

    public void OnClickLogin()
    {
        if (mensajeGO != null)
            mensajeGO.SetActive(false);

        StartCoroutine(LoginUsuario());
    }

    IEnumerator LoginUsuario()
    {
        string pin = pinInput.text.Trim();

        if (string.IsNullOrEmpty(pin))
        {
            MostrarMensaje("Por favor ingresa tu PIN");
            yield break;
        }

        // Verificamos si ya tenemos el token antes de continuar
        if (string.IsNullOrEmpty(authToken))
        {
            MostrarMensaje("Conectando con el servidor...");
            yield return StartCoroutine(ObtenerToken());
            if (string.IsNullOrEmpty(authToken))
            {
                MostrarMensaje("Error de conexión segura");
                yield break;
            }
        }

        Debug.Log($"[Login] Buscando usuario con PIN: '{pin}'");

        // 🔥 URL ACTUALIZADA: Se agrega ?auth={authToken}
        string urlGet = $"{firebaseURL}/usuarios.json?auth={authToken}";

        using (UnityWebRequest www = UnityWebRequest.Get(urlGet))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Login] Error de conexión: {www.error}");
                MostrarMensaje("Error de conexión con Firebase");
                yield break;
            }

            string respuesta = www.downloadHandler.text;

            if (string.IsNullOrEmpty(respuesta) || respuesta == "null")
            {
                MostrarMensaje("PIN incorrecto o no registrado");
                yield break;
            }

            // --- Lógica de búsqueda de PIN (Tu lógica original mejorada) ---
            bool loginExitoso = false;
            string idUsuarioString = "";
            string nombreUsuario = "";
            int edadUsuario = 0;

            try
            {
                respuesta = respuesta.Trim();
                if (respuesta.StartsWith("{") && respuesta.EndsWith("}"))
                {
                    respuesta = respuesta.Substring(1, respuesta.Length - 2);
                }

                string[] usuarios = respuesta.Split(new string[] { "}," }, System.StringSplitOptions.RemoveEmptyEntries);

                foreach (string usuario in usuarios)
                {
                    // Extraer PIN para comparar
                    int startPin = usuario.IndexOf("\"pin\":\"") + 7;
                    if (startPin < 7) continue; 
                    int endPin = usuario.IndexOf("\"", startPin);
                    string pinDB = usuario.Substring(startPin, endPin - startPin);

                    if (pinDB == pin)
                    {
                        // Extraer ID (el PIN se usa como ID según tu script de registro)
                        int startId = usuario.IndexOf("\"") + 1;
                        int endId = usuario.IndexOf("\"", startId);
                        idUsuarioString = usuario.Substring(startId, endId - startId);

                        // Extraer nombre
                        int startNombre = usuario.IndexOf("\"nombre\":\"") + 10;
                        if (startNombre >= 10)
                        {
                            int endNombre = usuario.IndexOf("\"", startNombre);
                            nombreUsuario = usuario.Substring(startNombre, endNombre - startNombre);
                        }

                        // Extraer edad
                        int startEdad = usuario.IndexOf("\"edad\":") + 7;
                        if (startEdad >= 7)
                        {
                            int endEdad = usuario.IndexOf(",", startEdad);
                            if (endEdad == -1) endEdad = usuario.IndexOf("}", startEdad);
                            string edadStr = usuario.Substring(startEdad, endEdad - startEdad).Trim();
                            int.TryParse(edadStr, out edadUsuario);
                        }

                        loginExitoso = true;
                        break;
                    }
                }

                if (loginExitoso)
                {
                    // Guardar sesión
                    PlayerPrefs.SetString("idUsuario", idUsuarioString); // Guardamos como String por si el PIN tiene ceros a la izquierda
                    PlayerPrefs.SetString("nombreUsuario", nombreUsuario);
                    PlayerPrefs.SetInt("edadUsuario", edadUsuario);
                    PlayerPrefs.SetString("pinUsuario", pin);
                    PlayerPrefs.Save();

                    Debug.Log($"[Login] ¡Éxito! Bienvenido {nombreUsuario}");
                    pinInput.text = "";
                    uiManager?.ShowMenu();
                }
                else
                {
                    MostrarMensaje("PIN incorrecto");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Login] Error al procesar datos: {e.Message}");
                MostrarMensaje("Error al procesar datos");
            }
        }
    }

    private void MostrarMensaje(string texto)
    {
        if (mensajeGO != null && mensajeTexto != null)
        {
            mensajeTexto.text = texto;
            mensajeGO.SetActive(true);
        }
    }

    public void CerrarSesion()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        uiManager?.ShowMainMenu();
    }
}