using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class minijuegosOrientacion : MonoBehaviour
{
    [Header("Imagen UI a mover (Esfera)")]
    public RectTransform imageToMove;

    [Header("Configuración de Movimiento")]
    public float moveDistance = 1200f;
    public float initialMoveSpeed = 600f;
    public float speedIncrease = 150f;
    public float maxMoveSpeed = 2400f;

    [Header("Botones de Dirección - Usar íconos únicos")]
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;

    [Header("Textos")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI progressText; // Nuevo: muestra progreso

    [Header("Servidor")]
    public SendResultToServer resultSender;

    [Header("Sonidos")]
    public AudioClip correctSound;  // Sonido para respuesta correcta
    public AudioClip wrongSound;    // Sonido para respuesta incorrecta
    private AudioSource audioSource; // Fuente de audio

    // --- Estado interno esfera ---
    private Vector2 initialPosition = new Vector2(0, 150);
    private Vector2 targetPosition;
    private int currentDirection; // 0=Arriba, 1=Abajo, 2=Izquierda, 3=Derecha
    private bool isMoving = false;
    private bool gameStarted = false;
    private bool gameInitialized = false;
    private int lastDirection = -1;
    private bool waitingForResponse = false; // Nueva variable para saber si estamos esperando respuesta

    // --- Puntuación / métricas ---
    private int correctCount = 0;
    private int errorCount = 0; // Solo errores (respuestas incorrectas + timeouts)
    private float timeAtMoveStart = 0f;
    private float totalResponseTime = 0f;
    private float gameStartTime = 0f;
    private float currentMoveSpeed;

    // --- Variables de límite de tiempo ---
    private float responseTimeLimit = 10f;
    private const float minResponseTime = 5f;
    private const float timeReductionPerSuccess = 0.5f;

    // --- Objetivo del juego ---
    private const int targetCorrectAnswers = 15;

    // --- Delegado y evento ---
    public delegate void GameWonHandler();
    public static event GameWonHandler OnGameWon;

    void Start()
    {
        // Asignar listeners a los botones
        upButton.onClick.AddListener(() => CheckDirection(0));
        downButton.onClick.AddListener(() => CheckDirection(1));
        leftButton.onClick.AddListener(() => CheckDirection(2));
        rightButton.onClick.AddListener(() => CheckDirection(3));

        // Inicializar UI
        if (imageToMove != null) imageToMove.anchoredPosition = initialPosition;
        if (infoText != null) infoText.text = "";
        if (timerText != null) timerText.text = "";
        if (progressText != null) progressText.text = "";

        audioSource = gameObject.AddComponent<AudioSource>();
        currentMoveSpeed = initialMoveSpeed;
    }

    public void StartGame()
    {
        if (gameInitialized) return;
        gameInitialized = true;
        StartCoroutine(InitializeGame());
    }

    IEnumerator InitializeGame()
    {
        // Resetear variables
        isMoving = false;
        gameStarted = false;
        waitingForResponse = false;
        correctCount = 0;
        errorCount = 0;
        totalResponseTime = 0f;
        currentMoveSpeed = initialMoveSpeed;
        responseTimeLimit = 10f;
        lastDirection = -1;
        
        gameStartTime = Time.time;

        if (infoText != null)
            infoText.text = "¡Observa hacia dónde se mueve la esfera.";
        
        if (progressText != null)
            progressText.text = $"Progreso: 0/{targetCorrectAnswers}";

        yield return new WaitForSeconds(2f);

        gameStarted = true;
        yield return new WaitForSeconds(0.3f);
        PickRandomDirectionAndMove();
    }

    void Update()
    {
        if (!gameStarted || imageToMove == null) return;

        // Movimiento de la esfera
        if (isMoving)
        {
            imageToMove.anchoredPosition = Vector2.MoveTowards(
                imageToMove.anchoredPosition,
                targetPosition,
                currentMoveSpeed * Time.deltaTime
            );

            if (Vector2.Distance(imageToMove.anchoredPosition, targetPosition) < 0.5f)
            {
                imageToMove.anchoredPosition = targetPosition;
                isMoving = false;
                
                // Solo iniciar el temporizador si ya terminó todo el movimiento (incluyendo doble)
                if (waitingForResponse)
                {
                    timeAtMoveStart = Time.time;
                    
                    if (infoText != null)
                        infoText.text = "¿Hacia dónde se movió la esfera?";
                }
            }
        }

        // Mostrar tiempo restante solo cuando estamos esperando respuesta
        if (!isMoving && waitingForResponse && gameStarted)
        {
            float timeLeft = Mathf.Max(0, responseTimeLimit - (Time.time - timeAtMoveStart));
            
            if (timerText != null)
                timerText.text = $"Tiempo: {timeLeft:F1}s";

            // Si se agota el tiempo
            if (timeLeft <= 0f)
            {
                errorCount++;
                totalResponseTime += responseTimeLimit;
                
                if (infoText != null)
                    infoText.text = $"¡Tiempo agotado! Errores: {errorCount}";

                waitingForResponse = false;
                StartCoroutine(ResetAfterDelay(1.5f));
            }
        }
    }

    void PickRandomDirectionAndMove()
    {
        if (!gameStarted || isMoving) return;

        waitingForResponse = false; // Resetear el estado de espera
        
        // Elegir dirección aleatoria diferente a la anterior
        int rand;
        do { 
            rand = Random.Range(0, 4); 
        } while (rand == lastDirection);
        
        lastDirection = rand;
        currentDirection = rand;

        Vector2 move = Vector2.zero;
        switch (rand)
        {
            case 0: move = Vector2.up * moveDistance; break;
            case 1: move = Vector2.down * moveDistance; break;
            case 2: move = Vector2.left * moveDistance; break;
            case 3: move = Vector2.right * moveDistance; break;
        }

        // Si la velocidad es muy alta, hacer movimiento engañoso
        if (currentMoveSpeed >= 1800f)
        {
            StartCoroutine(DoubleMove(move));
        }
        else
        {
            targetPosition = initialPosition + move;
            isMoving = true;
            waitingForResponse = true; // Movimiento simple, esperar respuesta después
        }

        if (infoText != null)
            infoText.text = "¡Observa con atención!";
        
        if (timerText != null)
            timerText.text = "";
    }

    IEnumerator DoubleMove(Vector2 finalMove)
    {
        // Primer movimiento falso
        Vector2[] directions = { 
            Vector2.up * moveDistance, 
            Vector2.down * moveDistance, 
            Vector2.left * moveDistance, 
            Vector2.right * moveDistance 
        };
        
        Vector2 firstMove = directions[Random.Range(0, directions.Length)];
        while (firstMove == finalMove)
            firstMove = directions[Random.Range(0, directions.Length)];

        targetPosition = initialPosition + firstMove;
        isMoving = true;
        yield return new WaitUntil(() => !isMoving);

        // Volver al centro
        targetPosition = initialPosition;
        isMoving = true;
        yield return new WaitUntil(() => !isMoving);

        // Movimiento real
        targetPosition = initialPosition + finalMove;
        isMoving = true;
        waitingForResponse = true; // Ahora sí, esperar respuesta después del movimiento final
    }

    void CheckDirection(int chosenDirection)
    {
        // Solo se puede responder si el juego está activo, no hay movimiento en curso y estamos esperando respuesta
        if (!gameStarted || isMoving || !waitingForResponse) return;

        // Calcular el tiempo usado en esta respuesta
        float responseTime = Time.time - timeAtMoveStart;
        totalResponseTime += responseTime;

        // Ya no estamos esperando respuesta
        waitingForResponse = false;

        if (chosenDirection == currentDirection)
        {
            // Respuesta correcta
            correctCount++;

            if (infoText != null)
                infoText.text = "¡Correcto!";
            if (audioSource != null && correctSound != null)
                audioSource.PlayOneShot(correctSound);

            if (progressText != null)
                progressText.text = $"Progreso: {correctCount}/{targetCorrectAnswers}";

            // Aumentar velocidad de la esfera
            currentMoveSpeed = Mathf.Min(currentMoveSpeed + speedIncrease, maxMoveSpeed);

            // Reducir tiempo de respuesta pero nunca menor a minResponseTime
            responseTimeLimit = Mathf.Max(responseTimeLimit - timeReductionPerSuccess, minResponseTime);

            // Verificar si se completó el juego
            if (correctCount >= targetCorrectAnswers)
            {
                EndGame();
                return;
            }

            // Reiniciar para el siguiente movimiento después de un breve delay
            StartCoroutine(ResetAfterDelay(1f));
        }
        else
        {
            // Respuesta incorrecta
            errorCount++;

            if (infoText != null)
                infoText.text = $"¡Incorrecto! Errores: {errorCount}";
            if (audioSource != null && wrongSound != null)
                audioSource.PlayOneShot(wrongSound);

            // Reiniciar para el siguiente movimiento después de un poco más de delay
            StartCoroutine(ResetAfterDelay(1.5f));
        }
    }


    IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (imageToMove != null)
            imageToMove.anchoredPosition = initialPosition;
        
        if (timerText != null)
            timerText.text = "";
        
        PickRandomDirectionAndMove();
    }

    void EndGame()
    {
        gameStarted = false;
        waitingForResponse = false;
        
        float tiempoTotal = Time.time - gameStartTime;
        int totalMovimientos = correctCount + errorCount;
        float tiempoPromedio = totalMovimientos > 0 ? totalResponseTime / totalMovimientos : 0f;

        if (infoText != null)
            infoText.text = "¡Felicidades! Has completado el juego.";

        if (timerText != null)
            timerText.text = "";

        // Enviar datos al servidor
        if (resultSender != null)
        {
            resultSender.SetFase(SendResultToServer.FaseJuego.Orientación);
            resultSender.errores = errorCount;
            resultSender.tiempoPromedio = tiempoPromedio;
            resultSender.tiempoUsado = tiempoTotal;
            
            StartCoroutine(resultSender.SendResult());
        }
        else
        {
            Debug.LogWarning("[minijuegosOrientacion] No se asignó SendResultToServer en el Inspector");
        }

        OnGameWon?.Invoke();
    }
}