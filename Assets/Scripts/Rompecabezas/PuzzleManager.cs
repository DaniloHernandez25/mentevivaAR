using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class PuzzleSet
{
    public Sprite[] piezas; // Siempre 4 imágenes por set
}

public class PuzzleManager : MonoBehaviour
{
    [Header("Conjuntos de piezas")]
    public List<PuzzleSet> setsDisponibles;
    public GameObject piezaPrefab;
    public Transform contenedorPiezas;
    public Transform[] dropZones;
    public Canvas canvas;

    [Header("UI")]
    public TMP_Text feedbackText;
    public SendResultToServer resultSender;

    [Header("Configuración")]
    public int maxRondas = 5;

    private int rondasCompletadas = 0;
    private int piezasCorrectas = 0;
    private int fallos = 0;
    private float rondaStartTime;
    private bool gameOver = false;
    private float tiempoTotalUsado = 0f;

    private List<int> indicesUsados = new List<int>();

    // Evento Notificación
    public delegate void GameWonHandler();
    public static event GameWonHandler OnGameWon;


    // 🔥 MUY IMPORTANTE: cuando se oculta el Canvas, se detienen TODAS las coroutines
    void OnDisable()
    {
        StopAllCoroutines();
    }


    void OnEnable()
    {
        if (contenedorPiezas != null)
            contenedorPiezas.SetAsLastSibling();

        IniciarRonda();
    }


    void IniciarRonda()
    {
        if (gameOver) return;

        if (rondasCompletadas >= maxRondas)
        {
            FinalizarJuego();
            return;
        }

        feedbackText.text = "Observa el orden";
        StartCoroutine(GenerarYMostrarSecuencia());
        rondaStartTime = Time.time;
    }


    void LimpiarPiezas()
    {
        foreach (Transform child in contenedorPiezas)
            Destroy(child.gameObject);

        foreach (Transform drop in dropZones)
        {
            foreach (Transform child in drop)
                Destroy(child.gameObject);
        }
    }


    public void VerificarDrop(PiezaDrag pieza, Transform dropZone)
    {
        if (gameOver || pieza == null) return;

        int dropIndex = System.Array.IndexOf(dropZones, dropZone);

        if (dropIndex == pieza.index)
        {
            pieza.transform.SetParent(dropZone);
            pieza.transform.localPosition = Vector3.zero;

            CanvasGroup cg = pieza.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            piezasCorrectas++;

            if (piezasCorrectas == dropZones.Length)
            {
                float tiempoRonda = Time.time - rondaStartTime;
                tiempoTotalUsado += tiempoRonda;

                rondasCompletadas++;
                MostrarMensajeTemporal("Ronda Completada", 3f);

                if (rondasCompletadas >= maxRondas)
                    Invoke(nameof(FinalizarJuego), 1f);
                else
                    Invoke(nameof(IniciarRonda), 1f);
            }
        }
        else
        {
            pieza.transform.SetParent(contenedorPiezas);
            fallos++;
            MostrarMensajeTemporal("Pieza incorrecta, intenta otra posición", 3f);
        }
    }


    public void MostrarMensajeTemporal(string mensaje, float duracion)
    {
        feedbackText.text = mensaje;

        StopAllCoroutines(); // detener mensajes anteriores
        StartCoroutine(BorrarMensajeDespues(duracion));
    }

    private System.Collections.IEnumerator BorrarMensajeDespues(float tiempo)
    {
        yield return new WaitForSeconds(tiempo);
        if (this != null && this.isActiveAndEnabled)
            feedbackText.text = "";
    }


    // ======================================================
    //              GENERAR Y MOSTRAR SECUENCIA
    // ======================================================
    private System.Collections.IEnumerator GenerarYMostrarSecuencia()
    {
        LimpiarPiezas();

        // Elegir set de piezas no repetido
        int randSet;
        do
        {
            randSet = Random.Range(0, setsDisponibles.Count);
        } 
        while (indicesUsados.Contains(randSet) && indicesUsados.Count < setsDisponibles.Count);

        indicesUsados.Add(randSet);
        Sprite[] seleccionadas = setsDisponibles[randSet].piezas;

        // Crear preview ordenada
        List<GameObject> piezasTemporales = new List<GameObject>();

        for (int i = 0; i < dropZones.Length; i++)
        {
            GameObject pieza = Instantiate(piezaPrefab, dropZones[i]);
            pieza.GetComponent<Image>().sprite = seleccionadas[i];

            CanvasGroup cg = pieza.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            pieza.GetComponent<RectTransform>().localPosition = Vector3.zero;
            piezasTemporales.Add(pieza);
        }

        // Esperar 5 segundos
        yield return new WaitForSeconds(5f);

        // Si el puzzle se ocultó → terminar
        if (!this.isActiveAndEnabled)
            yield break;

        // Mover piezas al contenedor y mezclarlas
        RectTransform panelRect = contenedorPiezas.GetComponent<RectTransform>();
        Vector2 panelSize = panelRect.rect.size;

        for (int i = 0; i < piezasTemporales.Count; i++)
        {
            GameObject pieza = piezasTemporales[i];
            if (pieza == null) continue; // 🔥 evita MissingReferenceException

            pieza.transform.SetParent(contenedorPiezas);

            float randomX = Random.Range(-panelSize.x / 2 + 50f, panelSize.x / 2 - 50f);
            float randomY = Random.Range(-panelSize.y / 2 + 50f, panelSize.y / 2 - 50f);
            pieza.GetComponent<RectTransform>().localPosition = new Vector2(randomX, randomY);

            CanvasGroup cg = pieza.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            PiezaDrag dragScript = pieza.GetComponent<PiezaDrag>();
            dragScript.puzzleManager = this;
            dragScript.canvas = canvas;
            dragScript.index = i;
        }

        feedbackText.text = "Ahora completa el rompecabezas";
        piezasCorrectas = 0;
    }



    void FinalizarJuego()
    {
        gameOver = true;
        feedbackText.text = "¡Ganaste el puzzle!";

        float totalIntentos = piezasCorrectas + fallos;
        float errorPercent = (totalIntentos > 0) ? (fallos * 100f / totalIntentos) : 0f;

        if (resultSender != null)
        {
            resultSender.SetFase(SendResultToServer.FaseJuego.Rompecabezas);
            resultSender.porcentajeError = errorPercent;
            resultSender.tiempoUsado = tiempoTotalUsado;

            StartCoroutine(resultSender.SendResult());
        }

        OnGameWon?.Invoke();
    }
}
