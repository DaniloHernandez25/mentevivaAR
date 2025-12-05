using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class SendResultToServer : MonoBehaviour
{
    [Header("Configuración Firebase")]
    private string firebaseURL = "https://fcar-9d923-default-rtdb.firebaseio.com";

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

    public IEnumerator SendResult()
    {
        int idUsuario = PlayerPrefs.GetInt("idUsuario", -1);
        if (idUsuario == -1)
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

        Debug.Log($"[SendResultToServer] Enviando a Firebase: {rutaFirebase}");
        Debug.Log($"[SendResultToServer] JSON: {json}");

        string urlCompleta = $"{firebaseURL}/{rutaFirebase}.json";

        // ✅ Crear request correctamente para Firebase
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
                string respuesta = www.downloadHandler.text.Trim();
                Debug.Log($"[SendResultToServer] ✅ Resultado guardado en Firebase: {respuesta}");
            }
        }
    }

    public void SetFase(FaseJuego nuevaFase) => fase = nuevaFase;

    [Serializable]
    public class OrientacionData
    {
        public string errores;
        public int tiempoPromedioRespuesta;
        public int tiempoUsado;
        public string fecha;
    }

    [Serializable]
    public class MemoriaData
    {
        public int errores;
        public int tiempoUsado;
        public string fecha;
    }

    [Serializable]
    public class CalculoData
    {
        public int errores;
        public int tiempoPromedioRespuesta;
        public int tiempoUsado;
        public string fecha;
    }

    [Serializable]
    public class LenguajeData
    {
        public string palabrasPedidas;
        public string oracionesDadas;
        public int tiempoUsado;
        public string fecha;
    }

    [Serializable]
    public class RompecabezasData
    {
        public int porcentajeError;
        public int tiempoUsado;
        public string fecha;
    }

    [Serializable]
    public class EspacialData
    {
        public int tiempoPromedioRespuesta;
        public int tiempoUsado;
        public string fecha;
    }
}