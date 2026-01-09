using UnityEngine;

public class CanvasActivator : MonoBehaviour
{
    [Header("Canvas a controlar")]
    public GameObject canvas;

    public minijuegosOrientacion orientacion; 
    public memoriaMinijuego memoria; 
    public PuzzleManager puzzle;

    public void Show()
    {
        if (canvas != null)
            canvas.SetActive(true);

        if (orientacion != null)
            orientacion.StartGame();

        if (memoria != null)
            memoria.StartGame();

        if (puzzle != null)
            puzzle.enabled = true;
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.SetActive(false);
    }
}
