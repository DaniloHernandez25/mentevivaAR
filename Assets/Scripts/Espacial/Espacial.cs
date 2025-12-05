using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Vuforia;

public class Espacial : MonoBehaviour
{
    [Header("Lista de Image Targets")]
    public List<GameObject> imageTargets; 
    private List<GameObject> randomizedTargets;

    [Header("Sprites de referencia")]
    [Tooltip("Arrastra los sprites en el mismo orden que los Image Targets")]
    public List<Sprite> targetSprites;

    [Header("UI")]
    public TMP_Text instructionText;
    public UnityEngine.UI.Image referenceImage;

    private int currentIndex = 0;
    private bool targetDetected = false;

    // Para medir tiempos
    private float tiempoInicioTarget;
    private List<float> tiemposPorTarget = new List<float>();
    private float tiempoTotal;

    // Referencia al SendResultToServer
    public SendResultToServer sender;

    public static event System.Action OnGameWon;

    void Start()
    {
        if (imageTargets == null || imageTargets.Count == 0)
        {
            Debug.LogError("[Espacial] No hay Image Targets asignados.");
            return;
        }

        randomizedTargets = new List<GameObject>(imageTargets);
        ShuffleList(randomizedTargets);

        foreach (var target in imageTargets)
            if (target != null) target.SetActive(false);

        SetupVuforiaObservers();
        ShowCurrentTarget();
    }

    void SetupVuforiaObservers()
    {
        if (randomizedTargets == null) return;

        foreach (var target in randomizedTargets)
        {
            if (target == null) continue;

            var observer = target.GetComponent<DefaultObserverEventHandler>();
            if (observer == null)
                observer = target.AddComponent<DefaultObserverEventHandler>();

            var behaviour = target.GetComponent<ObserverBehaviour>();
            if (behaviour != null)
                behaviour.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        if (randomizedTargets == null || currentIndex >= randomizedTargets.Count)
            return;

        if (behaviour.gameObject != randomizedTargets[currentIndex])
            return;

        if (targetStatus.Status == Status.TRACKED || targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            if (!targetDetected)
            {
                targetDetected = true;
                float tiempoTarget = Time.time - tiempoInicioTarget;
                tiemposPorTarget.Add(tiempoTarget);
                tiempoTotal += tiempoTarget;

                Debug.Log($"[Espacial] ✅ Target detectado: {behaviour.gameObject.name}, tiempo: {tiempoTarget:F2}s");
                StartCoroutine(AvanzarAlSiguienteTarget());
            }
        }
    }

    IEnumerator AvanzarAlSiguienteTarget()
    {
        if (instructionText != null)
            instructionText.text = $"¡Imagen encontrada!";

        if (referenceImage != null)
            referenceImage.color = Color.green;

        yield return new WaitForSeconds(1.5f);

        targetDetected = false;
        currentIndex++;
        ShowCurrentTarget();
    }

    void ShowCurrentTarget()
    {
        if (randomizedTargets == null || currentIndex >= randomizedTargets.Count)
        {
            if (instructionText != null)
                instructionText.text = "¡Has completado todos los targets!";

            if (referenceImage != null)
                referenceImage.gameObject.SetActive(false);

            // Evento victoria
            OnGameWon?.Invoke();

            // Calcular tiempo promedio por target
            int tiempoPromedio = tiemposPorTarget.Count > 0 ? (int)(tiempoTotal / tiemposPorTarget.Count) : 0;

            // Enviar a Firebase
            if (sender != null)
            {
                sender.SetFase(SendResultToServer.FaseJuego.Espacial);
                sender.tiempoUsado = tiempoTotal;
                sender.tiempoPromedio = tiempoPromedio;
                StartCoroutine(sender.SendResult());
            }

            return;
        }

        // Activar solo el target actual
        for (int i = 0; i < randomizedTargets.Count; i++)
        {
            if (randomizedTargets[i] != null)
                randomizedTargets[i].SetActive(i == currentIndex);
        }

        UpdateReferenceImage();

        if (instructionText != null)
            instructionText.text = $"Busca esta imagen";

        // Iniciar temporizador
        tiempoInicioTarget = Time.time;

        if (randomizedTargets[currentIndex] != null)
            Debug.Log($"[Espacial] Target actual: {randomizedTargets[currentIndex].name}");
    }

    void UpdateReferenceImage()
    {
        if (referenceImage == null || randomizedTargets == null || currentIndex >= randomizedTargets.Count)
            return;

        GameObject currentTarget = randomizedTargets[currentIndex];
        if (currentTarget == null) return;

        int originalIndex = imageTargets.IndexOf(currentTarget);

        if (originalIndex >= 0 && originalIndex < targetSprites.Count)
        {
            referenceImage.sprite = targetSprites[originalIndex];
            referenceImage.color = Color.white;
            referenceImage.gameObject.SetActive(true);
            Debug.Log($"[Espacial] Mostrando sprite: {targetSprites[originalIndex].name}");
        }
        else
        {
            Debug.LogError($"[Espacial] No se encontró sprite para el target {currentTarget.name}");
        }
    }

    void ShuffleList<T>(List<T> list)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void OnDestroy()
    {
        if (randomizedTargets == null) return;

        foreach (var target in randomizedTargets)
        {
            if (target == null) continue;

            var behaviour = target.GetComponent<ObserverBehaviour>();
            if (behaviour != null)
                behaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }
}
