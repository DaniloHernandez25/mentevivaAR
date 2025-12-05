using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PiezaDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PuzzleManager puzzleManager;
    public Canvas canvas;
    public int index; // índice de la pieza

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform parentBeforeDrag;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        parentBeforeDrag = transform.parent;
        transform.SetParent(canvas.transform); // traer al frente
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // Buscar dropZone debajo del mouse
        Transform drop = null;
        foreach (var zone in puzzleManager.dropZones)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                zone.GetComponent<RectTransform>(),
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null))
            {
                drop = zone;
                break;
            }
        }

        if (drop != null)
            puzzleManager.VerificarDrop(this, drop);
        else
            transform.SetParent(parentBeforeDrag); // volver al panel
    }
}
