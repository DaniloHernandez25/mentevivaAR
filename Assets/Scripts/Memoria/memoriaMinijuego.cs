using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class memoriaMinijuego : MonoBehaviour
{
    [Header("Botones de colores (asignar en el inspector)")]
    public Button azul;
    public Button amarillo;
    public Button verde;
    public Button rojo;
    public Button naranja;
    public Button morado;

    private List<Button> botones;
    private List<int> patron = new List<int>();
    private List<int> inputJugador = new List<int>();

    [Header("Configuración del juego")]
    public float tiempoIluminado = 0.5f;
    public float tiempoEntreLuces = 0.25f;
    public float tiempoEsperaEntreRondas = 1.0f;
    public int maxPatron = 8;

    [Header("UI Mensajes")]
    public TMP_Text mensajeUI;

    [Header("Audio")]
    public AudioClip[] sonidosBotones; // 🆕 Array de sonidos (uno por cada botón)
    public AudioSource audioSource; // 🆕 AudioSource para reproducir sonidos

    [Header("Servidor")]
    public SendResultToServer resultSender;

    private bool puedeClic = false;

    // Acumulado durante toda la partida
    private int correctCount = 0;
    private int failCount = 0;
    private float gameStartTime = 0f;

    public delegate void GameWonHandler();
    public static event GameWonHandler OnGameWon;

    void Awake()
    {
        botones = new List<Button> { azul, amarillo, verde, rojo, naranja, morado };

        for (int i = 0; i < botones.Count; i++)
        {
            int index = i;
            botones[i].onClick.AddListener(() => OnPlayerClick(index));
        }

        // 🆕 Crear AudioSource si no existe
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void StartGame()
    {
        // Guardar el tiempo de inicio
        gameStartTime = Time.time;

        // Solo reinicia los contadores al iniciar la partida
        correctCount = 0;
        failCount = 0;
        patron.Clear(); 
        inputJugador.Clear();

        mensajeUI.text = "Sigue el patrón";
        StopAllCoroutines();
        StartCoroutine(NuevoPatron());
    }

    IEnumerator NuevoPatron()
    {
        puedeClic = false;
        inputJugador.Clear();

        if (patron.Count < maxPatron)
        {
            int nuevoIndex = Random.Range(0, botones.Count);
            patron.Add(nuevoIndex);
        }

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < patron.Count; i++)
        {
            yield return StartCoroutine(IluminarBoton(patron[i]));
            yield return new WaitForSeconds(tiempoEntreLuces);
        }

        mensajeUI.text = "";
        puedeClic = true;
    }

    IEnumerator IluminarBoton(int index)
    {
        Image img = botones[index].GetComponent<Image>();
        Color original = img.color;
        Color iluminado = original * 2.0f;

        img.color = iluminado;

        // 🆕 Reproducir sonido del botón
        if (audioSource != null && sonidosBotones != null && index < sonidosBotones.Length)
        {
            if (sonidosBotones[index] != null)
            {
                audioSource.PlayOneShot(sonidosBotones[index]);
            }
        }

        yield return new WaitForSeconds(tiempoIluminado);
        img.color = original;
    }

    void OnPlayerClick(int index)
    {
        if (!puedeClic) return;

        StartCoroutine(IluminarBoton(index));
        inputJugador.Add(index);

        int pos = inputJugador.Count - 1;

        // ✅ Validar que el patrón tenga esa posición
        if (pos >= patron.Count)
        {
            // El jugador clickeó de más por algún motivo
            inputJugador.Clear();
            failCount++;
            StartCoroutine(MostrarMensajeTemporal("Demasiados clics, intenta de nuevo"));
            return;
        }

        // ✅ Comparar correctamente
        if (inputJugador[pos] != patron[pos])
        {
            failCount++;
            StartCoroutine(RegistrarErrorYPausar());
            StartCoroutine(MostrarMensajeTemporal("Incorrecto, intenta de nuevo"));
            return;
        }
        else
        {
            correctCount++;
        }

        if (inputJugador.Count == patron.Count)
        {
            if (patron.Count == maxPatron)
            {
                StartCoroutine(FinalizarJuego());
                return;
            }

            StartCoroutine(EsperarYNuevoPatron());
        }
    }



    IEnumerator RegistrarErrorYPausar()
    {
        puedeClic = false;

        float errorPercent = ((float)failCount / (correctCount + failCount)) * 100f;

        yield return new WaitForSeconds(1.5f);

        // 🆕 Repetir el patrón actual en lugar de reiniciar
        yield return StartCoroutine(RepetirPatronActual());
    }

    // 🆕 Nueva función para repetir el patrón actual
    IEnumerator RepetirPatronActual()
    {
        puedeClic = false;
        inputJugador.Clear();

        yield return new WaitForSeconds(1f);

        // Mostrar el patrón actual nuevamente
        for (int i = 0; i < patron.Count; i++)
        {
            yield return StartCoroutine(IluminarBoton(patron[i]));
            yield return new WaitForSeconds(tiempoEntreLuces);
        }

        mensajeUI.text = "";
        puedeClic = true;
    }

    IEnumerator EsperarYNuevoPatron()
    {
        puedeClic = false;
        mensajeUI.text = "Correcto!";
        yield return new WaitForSeconds(1.2f);   // ⏳ Tiempo que se muestra el mensaje
        mensajeUI.text = "";
        yield return new WaitForSeconds(tiempoEsperaEntreRondas);
        StartCoroutine(NuevoPatron());
    }

    IEnumerator FinalizarJuego()
    {
        puedeClic = false;

        // Calcular porcentaje de error si quieres enviarlo también
        float errorPercent = ((float)failCount / (correctCount + failCount)) * 100f;

        mensajeUI.text = $"¡Juego completado!";
        Debug.Log($"[RESULTADO] Correctos: {correctCount}, Fails: {failCount}, Error%: {errorPercent:F1}");

        // 🆕 Enviar resultado final al servidor
        if (resultSender != null)
        {
            resultSender.SetFase(SendResultToServer.FaseJuego.Memoria);
            resultSender.errores = failCount;
            resultSender.tiempoUsado = Time.time - gameStartTime; 
            StartCoroutine(resultSender.SendResult());
        }

        OnGameWon?.Invoke();

        yield return new WaitForSeconds(2f);
        StartGame(); // reinicia todo
    }

    IEnumerator MostrarMensajeTemporal(string texto)
    {
        mensajeUI.text = texto;
        yield return new WaitForSeconds(1.2f);
        mensajeUI.text = "";
    }
}