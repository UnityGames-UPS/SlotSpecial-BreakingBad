using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HoldButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float holdTimeThreshold = 1.0f;
    public UnityEvent onClick = new UnityEvent();
    public UnityEvent onLongPress = new UnityEvent();

    private bool isPointerDown = false;
    private bool longPressTriggered = false;
    private float pointerDownTime = 0f;

    private void Update()
    {
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
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isPointerDown = true;
        longPressTriggered = false;
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
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
