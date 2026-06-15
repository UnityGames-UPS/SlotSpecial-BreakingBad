using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlotSwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private SlotManager slotManager;
    private Vector2 startPosition;
    private bool swipeDetected = false;

    private void Start()
    {
        if (slotManager == null)
        {
            slotManager = FindObjectOfType<SlotManager>();
        }
    }

    public void Setup(SlotManager manager)
    {
        slotManager = manager;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotManager == null || slotManager.IsSpinning || slotManager.IsFeatureTransitioning) return;

        // Verify we didn't click on an interactive button/toggle
        if (EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = eventData.position;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var res in results)
            {
                if (res.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                    res.gameObject.GetComponentInParent<UnityEngine.UI.Toggle>() != null ||
                    res.gameObject.name.ToLower().Contains("button"))
                {
                    return; // Ignore drag if started on a button/toggle
                }
            }
        }

        startPosition = eventData.position;
        swipeDetected = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (swipeDetected || slotManager == null || slotManager.IsSpinning || slotManager.IsFeatureTransitioning) return;

        Vector2 currentPosition = eventData.position;
        float diffY = currentPosition.y - startPosition.y;
        float diffX = currentPosition.x - startPosition.x;

        float swipeThreshold = slotManager.SwipeThresholdValue;

        if (Mathf.Abs(diffY) > swipeThreshold && Mathf.Abs(diffY) > Mathf.Abs(diffX))
        {
            swipeDetected = true;
            if (diffY < 0)
            {
                // Top to bottom swipe -> Spin normal
                slotManager.StartSlots(false, false);
            }
            else
            {
                // Bottom to top swipe -> Spin reverse
                slotManager.StartSlots(false, true);
            }
        }
    }
}
