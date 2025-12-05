using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class Login : MonoBehaviour
{
    [SerializeField] private TMP_InputField pinInput;      
    [SerializeField] private GameObject mensajeGO;
    private TMP_Text mensajeTexto;

    [SerializeField] private UIManager uiManager;

    // 🔥 URL de tu Firebase Realtime Database
    private string firebaseURL = "https://fcar-9d923-default-rtdb.firebaseio.com";

    private void Start()
    {
        if (mensajeGO != null)
        {
            mensajeTexto = mensajeGO.GetComponent<TMP_Text>();
            mensajeGO.SetActive(false);
        }

        // Si ya existe sesión guardada, entrar directo
        if (PlayerPrefs.HasKey("idUsuario"))
        {
            Debug.Log("Usuario ya logueado: " + PlayerPrefs.GetString("nombreUsuario"));
            uiManager?.ShowMenu();
        }
    }

    public void OnClickLogin()
    {
        // Limpiar mensaje previo
        if (mensajeGO != null)
            mensajeGO.SetActive(false);

        StartCoroutine(LoginUsuario());
    }

    IEnumerator LoginUsuario()
    {
        string pin = pinInput.text.Trim();

        // Validaciones
        if (string.IsNullOrEmpty(pin))
        {
            MostrarMensaje("Por favor ingresa tu PIN");
            yield break;
        }

        if (pin.Length != 4)
        {
            MostrarMensaje("El PIN debe tener 4 dígitos");
            yield break;
        }

        if (!int.TryParse(pin, out _))
        {
            MostrarMensaje("El PIN debe contener solo números");
            yield break;
        }

        Debug.Log($"[Login] Buscando usuario con PIN: '{pin}'");

        // 🔥 Obtener todos los usuarios de Firebase
        string urlGet = $"{firebaseURL}/usuarios.json";

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
            Debug.Log($"[Login] Respuesta recibida: {respuesta.Substring(0, Mathf.Min(200, respuesta.Length))}...");

            // Si no hay usuarios o la respuesta es null
            if (string.IsNullOrEmpty(respuesta) || respuesta == "null")
            {
                Debug.LogWarning("[Login] No hay usuarios registrados");
                MostrarMensaje("PIN incorrecto");
                yield break;
            }

            // Buscar el PIN en la respuesta
            bool loginExitoso = false;
            int idUsuario = -1;
            string nombreUsuario = "";
            int edadUsuario = 0;
            string discapacidadUsuario = "";

            try
            {
                // Quitar llaves exteriores
                respuesta = respuesta.Trim();
                if (respuesta.StartsWith("{") && respuesta.EndsWith("}"))
                {
                    respuesta = respuesta.Substring(1, respuesta.Length - 2);
                }

                // Dividir por usuarios (por cada "ID":{...})
                string[] usuarios = respuesta.Split(new string[] { "}," }, System.StringSplitOptions.RemoveEmptyEntries);

                foreach (string usuario in usuarios)
                {
                    // Extraer ID del usuario
                    int startId = usuario.IndexOf("\"") + 1;
                    int endId = usuario.IndexOf("\"", startId);
                    string id = usuario.Substring(startId, endId - startId);

                    // Extraer PIN
                    int startPin = usuario.IndexOf("\"pin\":\"") + 7;
                    if (startPin < 7) continue; // No tiene campo pin
                    int endPin = usuario.IndexOf("\"", startPin);
                    string pinDB = usuario.Substring(startPin, endPin - startPin);

                    Debug.Log($"[Login] Verificando usuario ID {id}: pin='{pinDB}'");

                    // Verificar PIN
                    if (pinDB == pin)
                    {
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

                        // Extraer discapacidad
                        int startDisc = usuario.IndexOf("\"discapacidad\":\"") + 16;
                        if (startDisc >= 16)
                        {
                            int endDisc = usuario.IndexOf("\"", startDisc);
                            discapacidadUsuario = usuario.Substring(startDisc, endDisc - startDisc);
                        }

                        loginExitoso = true;
                        idUsuario = int.Parse(id);
                        break;
                    }
                }

                if (loginExitoso)
                {
                    // ✅ Guardar sesión en PlayerPrefs
                    PlayerPrefs.SetInt("idUsuario", idUsuario);
                    PlayerPrefs.SetString("nombreUsuario", nombreUsuario);
                    PlayerPrefs.SetInt("edadUsuario", edadUsuario);
                    PlayerPrefs.SetString("discapacidadUsuario", discapacidadUsuario);
                    PlayerPrefs.SetString("pinUsuario", pin);
                    PlayerPrefs.Save();

                    Debug.Log($"[Login] Login exitoso! Usuario ID: {idUsuario}, Nombre: {nombreUsuario}");
                    mensajeGO?.SetActive(false);
                    
                    // Limpiar campo PIN
                    pinInput.text = "";
                    
                    uiManager?.ShowMenu();
                }
                else
                {
                    Debug.LogWarning("[Login] PIN incorrecto");
                    MostrarMensaje("PIN incorrecto");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Login] Error parseando respuesta: {e.Message}");
                MostrarMensaje("Error procesando respuesta de Firebase");
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

        Debug.Log("[Login] Mensaje: " + texto);
    }

    public void CerrarSesion()
    {
        PlayerPrefs.DeleteKey("idUsuario");
        PlayerPrefs.DeleteKey("nombreUsuario");
        PlayerPrefs.DeleteKey("edadUsuario");
        PlayerPrefs.DeleteKey("discapacidadUsuario");
        PlayerPrefs.DeleteKey("pinUsuario");
        PlayerPrefs.Save();

        Debug.Log("Sesión cerrada");
        uiManager?.ShowMainMenu();
    }

    // 📦 Clases auxiliares
    [System.Serializable]
    public class UsuarioData
    {
        public string nombre;
        public int edad;
        public string discapacidad;
        public string pin;
    }
}