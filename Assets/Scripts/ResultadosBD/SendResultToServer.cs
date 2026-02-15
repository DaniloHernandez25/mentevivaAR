using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Firebase.Auth; // <--- AGREGADO

public class SendResultToServer : MonoBehaviour
{
    [Header("Configuración Firebase")]
    private string firebaseURL = "https://fcar-9d923-default-rtdb.firebaseio.com";
    private string authToken = ""; 

    [Header("Parámetros")]
    public FaseJuego fase;

    // Parámetros generales
    public int errores;
    public float tiempoPromedio;
    public float tiempoUsado;
    public float porcentajeError;
    public string palabrasPedidas;
    public string oracionesDadas;

    public enum FaseJuego
    {
        Orientación,
        Memoria,
        Cálculo,
        Lenguaje,
        Rompecabezas,
        Espacial
    }

    // --- BLOQUE AGREGADO PARA SEGURIDAD ---
    void Start()
    {
        StartCoroutine(ObtenerToken());
    }

    IEnumerator ObtenerToken()
    {
        var auth = FirebaseAuth.DefaultInstance;
        var task = auth.SignInAnonymouslyAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("[SendResult] Error al autenticar: " + task.Exception);
        }
        else
        {
            var tokenTask = task.Result.User.TokenAsync(false); 
            yield return new WaitUntil(() => tokenTask.IsCompleted);

            if (!tokenTask.IsFaulted)
            {
                authToken = tokenTask.Result;
                Debug.Log("[SendResult] Token obtenido correctamente.");
            }
        }
    }
    // ---------------------------------------

    public IEnumerator SendResult()
    {
        // 1. Asegurarse de tener token antes de enviar
        if (string.IsNullOrEmpty(authToken))
        {
            yield return StartCoroutine(ObtenerToken());
        }

        // 2. IMPORTANTE: Cambiado a GetString para coincidir con tu Login.cs
        string idUsuario = PlayerPrefs.GetString("idUsuario", ""); 
        
        if (string.IsNullOrEmpty(idUsuario))
        {
            Debug.LogError("[SendResultToServer] No se encontró 'idUsuario' en PlayerPrefs");
            yield break;
        }

        string json = "";
        string rutaFirebase = "";

        switch (fase)
        {
            case FaseJuego.Orientación:
                rutaFirebase = $"orientacion/{idUsuario}";
                var dataOrientacion = new OrientacionData
                {
                    errores = errores.ToString(),
                    tiempoPromedioRespuesta = (int)tiempoPromedio,
                    tiempoUsado = (int)tiempoUsado,
                    fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                json = JsonUtility.ToJson(dataOrientacion);
                break;

            case FaseJuego.Memoria:
                rutaFirebase = $"memoria/{idUsuario}";
                var dataMemoria = new MemoriaData
                {
                    errores = errores,
                    tiempoUsado = (int)tiempoUsado,
                    fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                json = JsonUtility.ToJson(dataMemoria);
                break;

            case FaseJuego.Cálculo:
                rutaFirebase = $"calculo/{idUsuario}";
                var dataCalculo = new CalculoData
                {
                    errores = errores,
                    tiempoPromedioRespuesta = (int)tiempoPromedio,
                    tiempoUsado = (int)tiempoUsado,
                    fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                json = JsonUtility.ToJson(dataCalculo);
                break;

            case FaseJuego.Lenguaje:
                rutaFirebase = $"lenguaje/{idUsuario}";
                var dataLenguaje = new LenguajeData
                {
                    palabrasPedidas = palabrasPedidas,
                    oracionesDadas = oracionesDadas,
                    tiempoUsado = (int)tiempoUsado,
                    fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                json = JsonUtility.ToJson(dataLenguaje);
                break;

            case FaseJuego.Rompecabezas:
                rutaFirebase = $"rompecabezas/{idUsuario}";
                var dataRompecabezas = new RompecabezasData
                {
                    porcentajeError = (int)porcentajeError,
                    tiempoUsado = (int)tiempoUsado,
                    fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                json = JsonUtility.ToJson(dataRompecabezas);
                break;

            case FaseJuego.Espacial:
                rutaFirebase = $"espacial/{idUsuario}";
                var dataEspacial = new EspacialData
                {
                    tiempoPromedioRespuesta = (int)tiempoPromedio,
                    tiempoUsado = (int)tiempoUsado,
                    fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                json = JsonUtility.ToJson(dataEspacial);
                break;
        }

        // ✅ URL ACTUALIZADA: Se agrega ?auth={authToken}
        string urlCompleta = $"{firebaseURL}/{rutaFirebase}.json?auth={authToken}";

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        
        using (UnityWebRequest www = new UnityWebRequest(urlCompleta, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SendResultToServer] Error al enviar resultado: {www.error}");
            }
            else
            {
                Debug.Log($"[SendResultToServer] ✅ Resultado guardado en Firebase: {www.downloadHandler.text}");
            }
        }
    }

    public void SetFase(FaseJuego nuevaFase) => fase = nuevaFase;

    [Serializable] public class OrientacionData { public string errores; public int tiempoPromedioRespuesta; public int tiempoUsado; public string fecha; }
    [Serializable] public class MemoriaData { public int errores; public int tiempoUsado; public string fecha; }
    [Serializable] public class CalculoData { public int errores; public int tiempoPromedioRespuesta; public int tiempoUsado; public string fecha; }
    [Serializable] public class LenguajeData { public string palabrasPedidas; public string oracionesDadas; public int tiempoUsado; public string fecha; }
    [Serializable] public class RompecabezasData { public int porcentajeError; public int tiempoUsado; public string fecha; }
    [Serializable] public class EspacialData { public int tiempoPromedioRespuesta; public int tiempoUsado; public string fecha; }
}