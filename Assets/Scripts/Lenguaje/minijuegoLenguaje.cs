using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class minijuegoLenguaje : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI instruccionText;
    public TMP_InputField inputField;
    public Button validarButton;
    public TextMeshProUGUI feedbackText;

    [Header("Banco de Palabras")]
    [Tooltip("Lista de todas las palabras disponibles - Edítala en el Inspector")]
    public List<string> sujetos;
    public List<string> verbos;
    public List<string> complementos;

    [Header("Configuración")]
    public int maxRondas = 3;

    private List<string> historialPalabras = new List<string>();
    private List<string> historialOraciones = new List<string>();

    // Estado del juego
    private int rondasCompletadas = 0;
    private int aciertosTotal = 0;
    private int fallosTotal = 0;
    private bool gameOver = false;
    private int dificultadActual = 3;

    private List<string> palabrasActuales;

    // Evento para el UIManager
    public delegate void GameWonHandler();
    public static event GameWonHandler OnGameWon;

    void Start()
    {
        validarButton.onClick.AddListener(ValidarOracion);
        IniciarNuevaRonda();
    }

    void IniciarNuevaRonda()
    {
        if (gameOver) return;

        if (rondasCompletadas >= maxRondas)
        {
            FinalizarJuego();
            return;
        }

        if (inputField != null)
            inputField.text = "";

        DeterminarDificultad();
        SeleccionarEstructuraSegunDificultad();

        string palabrasTexto = string.Join(", ", palabrasActuales);
        instruccionText.text = $"Forme una oración con las palabras:\n{palabrasTexto}";
    }

    void DeterminarDificultad()
    {
        // Incrementa progresivamente: 3, 4, 5 palabras máximo
        dificultadActual = Mathf.Min(3 + rondasCompletadas, 5);
    }

    string NombreDificultad()
    {
        return dificultadActual switch
        {
            3 => "Fácil",
            4 => "Medio",
            5 => "Difícil",
            _ => "Normal"
        };
    }

    void SeleccionarEstructuraSegunDificultad()
    {
        palabrasActuales = new List<string>();

        if (dificultadActual == 3) // Fácil: S + V + C
        {
            string s = sujetos[Random.Range(0, sujetos.Count)];
            string v = verbos[Random.Range(0, verbos.Count)];
            string c = complementos[Random.Range(0, complementos.Count)];

            palabrasActuales.Add(s);
            palabrasActuales.Add(v);
            palabrasActuales.Add(c);
        }
        else if (dificultadActual == 4) // Medio: (2S + V + C) O (S + 2V + C)
        {
            bool dosSujetos = Random.value < 0.5f;

            if (dosSujetos)
            {
                string s1 = sujetos[Random.Range(0, sujetos.Count)];
                string s2 = sujetos[Random.Range(0, sujetos.Count)];
                string v = verbos[Random.Range(0, verbos.Count)];
                string c = complementos[Random.Range(0, complementos.Count)];

                palabrasActuales.Add(s1);
                palabrasActuales.Add(s2);
                palabrasActuales.Add(v);
                palabrasActuales.Add(c);
            }
            else
            {
                string s = sujetos[Random.Range(0, sujetos.Count)];
                string v1 = verbos[Random.Range(0, verbos.Count)];
                string v2 = verbos[Random.Range(0, verbos.Count)];
                string c = complementos[Random.Range(0, complementos.Count)];

                palabrasActuales.Add(s);
                palabrasActuales.Add(v1);
                palabrasActuales.Add(v2);
                palabrasActuales.Add(c);
            }
        }
        else if (dificultadActual == 5) // Difícil: 2S + 2V + C
        {
            string s1 = sujetos[Random.Range(0, sujetos.Count)];
            string s2 = sujetos[Random.Range(0, sujetos.Count)];
            string v1 = verbos[Random.Range(0, verbos.Count)];
            string v2 = verbos[Random.Range(0, verbos.Count)];
            string c = complementos[Random.Range(0, complementos.Count)];

            palabrasActuales.Add(s1);
            palabrasActuales.Add(s2);
            palabrasActuales.Add(v1);
            palabrasActuales.Add(v2);
            palabrasActuales.Add(c);
        }
    }

    void ValidarOracion()
    {
        if (gameOver) return;

        string oracionJugador = inputField.text.Trim();

        if (string.IsNullOrEmpty(oracionJugador))
        {
            feedbackText.text = "Escribe algo primero";
            return;
        }

        // ✅ Validar que se haya escrito más de una palabra para formar una oración
        string[] palabrasEnEntrada = oracionJugador.Split(' ');
        if (palabrasEnEntrada.Length < 2 || !oracionJugador.Contains(" "))
        {
            feedbackText.text = "Escribe una oración completa";
            return;
        }

        // Guardar palabras pedidas y oración escrita
        string palabrasTexto = string.Join(" ", palabrasActuales);
        historialPalabras.Add(palabrasTexto);
        historialOraciones.Add(oracionJugador);

        // Verificar palabras faltantes
        List<string> palabrasFaltantes = new List<string>();
        foreach (string palabra in palabrasActuales)
        {
            if (!oracionJugador.ToLower().Contains(palabra.ToLower()))
                palabrasFaltantes.Add(palabra);
        }

        if (palabrasFaltantes.Count == 0)
        {
            aciertosTotal++;
            rondasCompletadas++;
            feedbackText.text = $"¡Correcto!";

            if (rondasCompletadas >= maxRondas)
                Invoke(nameof(FinalizarJuego), 2f);
            else
                Invoke(nameof(IniciarNuevaRonda), 2f);
        }
        else
        {
            fallosTotal++;
            string faltantesTexto = string.Join(", ", palabrasFaltantes);
            feedbackText.text = $"Faltan palabras: {faltantesTexto}";
        }
    }

    void FinalizarJuego()
    {
        gameOver = true;

        instruccionText.text = "¡Juego completado!";

        if (inputField != null) inputField.gameObject.SetActive(false);
        validarButton.gameObject.SetActive(false);

        // Unimos resultados por |
        string palabrasFinal = string.Join("|", historialPalabras);
        string oracionesFinal = string.Join("|", historialOraciones);

        // Enviar resultados al servidor
        SendResultToServer sender = FindFirstObjectByType<SendResultToServer>();
        if (sender != null)
        {
            sender.SetFase(SendResultToServer.FaseJuego.Lenguaje);
            sender.palabrasPedidas = palabrasFinal;
            sender.oracionesDadas = oracionesFinal;
            sender.tiempoUsado = Time.timeSinceLevelLoad;
            StartCoroutine(sender.SendResult());
        }

        OnGameWon?.Invoke();
    }
}