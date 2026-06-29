using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlotSwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (slotManager == null || !slotManager.IsSpinning) return;

        
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
                    return; 
                }
            }
        }

        slotManager.PerformStop();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotManager == null || slotManager.IsSpinning || slotManager.IsFeatureTransitioning) return;

        
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
                    return; 
                }
            }
        }

        startPosition = eventData.position;
        swipeDetected = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (swipeDetected || slotManager == null || slotManager.IsSpinning || slotManager.IsFeatureTransitioning) return;

        Vector2 screenDelta = eventData.position - startPosition;
        
        
        Vector3 localDelta = Quaternion.Inverse(transform.rotation) * (Vector3)screenDelta;

        float diffY = localDelta.y;
        float diffX = localDelta.x;

        float swipeThreshold = slotManager.SwipeThresholdValue;

        if (Mathf.Abs(diffY) > swipeThreshold && Mathf.Abs(diffY) > Mathf.Abs(diffX))
        {
            swipeDetected = true;
            if (diffY < 0)
            {
                
                slotManager.StartSlots(false, false);
            }
            else
            {
                
                slotManager.StartSlots(false, true);
            }
        }
    }
}
