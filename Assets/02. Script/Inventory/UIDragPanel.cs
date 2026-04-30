using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 패널을 마우스로 드래그해서 옮기는 스크립트.
/// 
/// 추천 사용 방식:
/// - 패널 전체가 아니라 "헤더 바" 오브젝트에 붙인다.
/// - dragTarget에는 실제로 움직일 패널 루트 RectTransform을 넣는다.
/// 
/// 왜 헤더에 붙이냐:
/// - 패널 전체에 붙이면 내부 버튼 클릭과 드래그가 서로 싸우기 쉽다.
/// </summary>
public class UIDragPanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Drag Target")]
    [SerializeField] private RectTransform dragTarget;

    private RectTransform targetRect;
    private RectTransform parentRect;


    // 드래그 시작 시 마우스와 패널 중심 사이의 차이
    private Vector2 dragOffset;

    private void Awake()
    {
        if (dragTarget != null)
            targetRect = dragTarget;
        else
            targetRect = transform as RectTransform;

        if (targetRect != null)
            parentRect = targetRect.parent as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (targetRect == null)
            return;

        if (parentRect == null)
            parentRect = targetRect.parent as RectTransform;

        if (parentRect == null)
            return;

        // 부모 기준 local point 계산
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            dragOffset = targetRect.anchoredPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetRect == null || parentRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            targetRect.anchoredPosition = localPoint + dragOffset;
        }
    }
}