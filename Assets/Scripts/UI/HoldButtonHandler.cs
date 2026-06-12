using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float holdTimeThreshold = 1.0f;
    public UnityEvent onClick = new UnityEvent();
    public UnityEvent onLongPress = new UnityEvent();

    private bool isPointerDown = false;
    private bool longPressTriggered = false;
    private float pointerDownTime = 0f;
    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    private bool IsInteractable()
    {
        return selectable == null || selectable.IsInteractable();
    }

    private void Update()
    {
        if (!IsInteractable())
        {
            isPointerDown = false;
            return;
        }
        if (isPointerDown && !longPressTriggered)
        {
            if (Time.time - pointerDownTime >= holdTimeThreshold)
            {
                longPressTriggered = true;
                onLongPress?.Invoke();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPointerDown = true;
        longPressTriggered = false;
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (isPointerDown)
        {
            isPointerDown = false;
            if (!longPressTriggered)
            {
                onClick?.Invoke();
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerDown = false;
    }
}
