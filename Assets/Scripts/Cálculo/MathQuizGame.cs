using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MathQuizGame : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI feedbackText;
    public Button[] optionButtons;

    [Header("Configuración")]
    public int maxRondas = 10;

    [Header("Servidor")]
    public SendResultToServer resultSender;

    private int correctAnswer;
    private int rondasCompletadas = 0;
    private int aciertos = 0;
    private int fallos = 0;
    private bool gameOver = false;
    private int nivelDificultad = 1;

    private float tiempoInicioRonda;
    private float tiempoTotalRespuesta = 0f;

    public delegate void GameWonHandler();
    public static event GameWonHandler OnGameWon;

    void Start()
    {
        feedbackText.text = "Resuelve la operación";
        GenerarPregunta();
    }

    void GenerarPregunta()
    {
        if (gameOver) return;

        feedbackText.text = "";
        DeterminarDificultad();
        correctAnswer = GenerarResultadoSegunDificultad();
        questionText.text = "¿Qué operación da " + correctAnswer + "?";

        List<string> operaciones = new List<string> { GenerarOperacionCorrecta(correctAnswer) };
        while (operaciones.Count < 3)
        {
            int resultadoFalso = GenerarResultadoSegunDificultad();
            if (resultadoFalso == correctAnswer) continue;

            string opFalsa = GenerarOperacionCorrecta(resultadoFalso);
            if (!operaciones.Contains(opFalsa)) operaciones.Add(opFalsa);
        }

        // Mezclar operaciones
        for (int i = 0; i < operaciones.Count; i++)
        {
            int randomIndex = Random.Range(0, operaciones.Count);
            string temp = operaciones[i];
            operaciones[i] = operaciones[randomIndex];
            operaciones[randomIndex] = temp;
        }

        // Asignar botones y listener
        for (int i = 0; i < optionButtons.Length; i++)
        {
            string opTexto = operaciones[i];
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = opTexto;
            bool esCorrecta = EvaluarOperacion(opTexto) == correctAnswer;

            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => ValidarRespuesta(esCorrecta));
        }

        tiempoInicioRonda = Time.time; // Inicia contador de tiempo de respuesta
    }
    

    // 🆕 Determinar el nivel de dificultad basado en aciertos
    void DeterminarDificultad()
    {
        if (aciertos <= 2)
            nivelDificultad = 1; // Solo sumas simples (2 dígitos)
        else if (aciertos <= 4)
            nivelDificultad = 2; // Sumas con números mayores (2 dígitos)
        else if (aciertos <= 6)
            nivelDificultad = 3; // Restas (2 dígitos)
        else if (aciertos <= 8)
            nivelDificultad = 4; // Restas con números mayores (3 dígitos)
        else if (aciertos <= 10)
            nivelDificultad = 5; // Multiplicaciones simples (1 dígito × 1 dígito)
        else if (aciertos <= 12)
            nivelDificultad = 6; // Multiplicaciones medias (2 dígitos × 1 dígito)
        else
            nivelDificultad = 7; // Multiplicaciones complejas (2 dígitos × 2 dígitos, resultado hasta 999)
    }

    // 🆕 Generar resultado según nivel de dificultad
    int GenerarResultadoSegunDificultad()
    {
        switch (nivelDificultad)
        {
            case 1: return Random.Range(5, 20);      // Sumas básicas
            case 2: return Random.Range(20, 50);     // Sumas mayores
            case 3: return Random.Range(5, 30);      // Restas básicas
            case 4: return Random.Range(50, 200);    // Restas mayores
            case 5: return Random.Range(4, 50);      // Multiplicaciones básicas (ej: 2×3=6, 7×7=49)
            case 6: return Random.Range(20, 200);    // Multiplicaciones medias
            case 7: return Random.Range(100, 999);   // Multiplicaciones complejas
            default: return Random.Range(10, 50);
        }
    }

    // 🆕 Generar operación correcta según dificultad
    string GenerarOperacionCorrecta(int resultado)
    {
        switch (nivelDificultad)
        {
            case 1: // Solo sumas simples
            case 2: // Sumas con números mayores
                {
                    int a = Random.Range(1, resultado);
                    int b = resultado - a;
                    return a + " + " + b;
                }

            case 3: // Restas básicas
            case 4: // Restas mayores
                {
                    int maxA = (nivelDificultad == 3) ? 50 : 300;
                    int a = Random.Range(resultado + 1, Mathf.Min(resultado + 30, maxA));
                    int b = a - resultado;
                    return a + " - " + b;
                }

            case 5: // Multiplicaciones simples (1 dígito × 1 dígito)
                {
                    List<int> divisores = ObtenerDivisores(resultado);
                    if (divisores.Count >= 2)
                    {
                        // Buscar divisores de 1 dígito
                        List<int> divisoresSimples = new List<int>();
                        foreach (int d in divisores)
                        {
                            if (d >= 2 && d <= 9 && resultado / d >= 2 && resultado / d <= 9)
                                divisoresSimples.Add(d);
                        }

                        if (divisoresSimples.Count > 0)
                        {
                            int a = divisoresSimples[Random.Range(0, divisoresSimples.Count)];
                            int b = resultado / a;
                            return a + " × " + b;
                        }
                    }
                    // Si no hay divisores válidos, generar suma
                    int x = Random.Range(1, resultado);
                    int y = resultado - x;
                    return x + " + " + y;
                }

            case 6: // Multiplicaciones medias (2 dígitos × 1 dígito)
            case 7: // Multiplicaciones complejas (2 dígitos × 2 dígitos)
                {
                    List<int> divisores = ObtenerDivisores(resultado);
                    if (divisores.Count >= 2)
                    {
                        int minDiv = (nivelDificultad == 6) ? 2 : 10;
                        int maxDiv = (nivelDificultad == 6) ? 9 : 99;

                        List<int> divisoresValidos = new List<int>();
                        foreach (int d in divisores)
                        {
                            int otro = resultado / d;
                            if (d >= minDiv && d <= maxDiv && otro >= 2 && otro <= 99)
                                divisoresValidos.Add(d);
                        }

                        if (divisoresValidos.Count > 0)
                        {
                            int a = divisoresValidos[Random.Range(0, divisoresValidos.Count)];
                            int b = resultado / a;
                            return a + " × " + b;
                        }
                    }
                    // Si no hay divisores válidos, generar resta
                    int maxNum = Mathf.Min(resultado + 200, 999);
                    int num1 = Random.Range(resultado + 10, maxNum);
                    int num2 = num1 - resultado;
                    return num1 + " - " + num2;
                }

            default:
                {
                    int a = Random.Range(1, resultado);
                    int b = resultado - a;
                    return a + " + " + b;
                }
        }
    }

    // 🆕 Obtener divisores de un número
    List<int> ObtenerDivisores(int numero)
    {
        List<int> divisores = new List<int>();
        for (int i = 2; i <= Mathf.Sqrt(numero); i++)
        {
            if (numero % i == 0)
            {
                divisores.Add(i);
                if (i != numero / i)
                    divisores.Add(numero / i);
            }
        }
        return divisores;
    }

    // 🆕 Evaluar operación (ahora incluye multiplicación)
    int EvaluarOperacion(string operacion)
    {
        string[] partes = operacion.Split(' ');
        int a = int.Parse(partes[0]);
        string simbolo = partes[1];
        int b = int.Parse(partes[2]);

        if (simbolo == "+") return a + b;
        if (simbolo == "-") return a - b;
        if (simbolo == "×" || simbolo == "*") return a * b;

        return 0;
    }

    void ValidarRespuesta(bool esCorrecta)
    {
        if (gameOver) return;

        float tiempoRonda = Time.time - tiempoInicioRonda;
        tiempoTotalRespuesta += tiempoRonda;

        if (esCorrecta)
        {
            aciertos++;
            feedbackText.text = $"¡Correcto!";
        }
        else
        {
            fallos++;
            feedbackText.text = "Incorrecto. Intenta otra vez.";
        }

        rondasCompletadas++;

        if (rondasCompletadas >= maxRondas)
        {
            gameOver = true;

            int total = aciertos + fallos;
            float porcentajeError = (total > 0) ? (fallos * 100f / total) : 0f;
            float tiempoPromedio = (total > 0) ? (tiempoTotalRespuesta / total) : 0f;

            feedbackText.text = (aciertos >= fallos) ? $"¡Ganaste! Nivel máximo: {nivelDificultad}" : "Inténtalo de nuevo.";

            // Enviar datos al servidor
            if (resultSender != null)
            {
                resultSender.SetFase(SendResultToServer.FaseJuego.Cálculo);
                resultSender.errores = fallos;
                resultSender.tiempoUsado = tiempoTotalRespuesta;
                resultSender.tiempoPromedio = tiempoPromedio;

                StartCoroutine(resultSender.SendResult());
            }

            OnGameWon?.Invoke();
        }
        else
        {
            Invoke(nameof(GenerarPregunta), 2f);
        }
    }
}
