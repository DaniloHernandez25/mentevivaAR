using UnityEngine;

public class CanvasActivator : MonoBehaviour
{
    [Header("Canvas a controlar")]
    public GameObject canvas;

    public minijuegosOrientacion orientacion; 
    public memoriaMinijuego memoria; 
    public PuzzleManager puzzle; // 🔹 Nueva referencia al puzzle

    public void Show()
    {
        if (canvas != null)
            canvas.SetActive(true);

        if (orientacion != null)
            orientacion.StartGame();

        if (memoria != null)
            memoria.StartGame();

        if (puzzle != null)
            puzzle.enabled = true; // 🔹 Activa el componente PuzzleManager
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.SetActive(false);

        if (puzzle != null)
            puzzle.enabled = false; // 🔹 Detiene el puzzle si quieres ocultarlo
    }
}
