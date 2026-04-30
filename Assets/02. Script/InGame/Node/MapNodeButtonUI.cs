using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 런 맵 노드 버튼 UI.
/// 
/// 최종 표시 방식:
/// - 공통 네모 프레임 1개를 모든 노드에 사용
/// - 노드 타입은 아이콘으로 표시
/// - 못 가는 노드는 frame/icon 색을 어둡게 처리
/// - 갈 수 있는 노드는 frame/icon 색을 Color.white로 처리
/// - 클리어된 노드는 체크 이미지만 위에 표시
/// - Current 전용 프레임/마커는 사용하지 않음
/// </summary>
public class MapNodeButtonUI : MonoBehaviour
{
    public enum NodeVisualState
    {
        Locked,     // 아직 못 가는 노드
        Reachable,  // 현재 갈 수 있는 노드
        Cleared,    // 이미 클리어한 노드
        Current     // 현재 위치 노드
    }

    [Header("Refs")]
    [SerializeField] private Button button;

    // 기존 backgroundImage에 연결해둔 Image가 있으면 유지되도록 FormerlySerializedAs 사용
    [FormerlySerializedAs("backgroundImage")]
    [SerializeField] private Image frameImage;

    [SerializeField] private Image iconImage;
    [SerializeField] private Image clearedCheckImage;

    [Header("Optional Debug Text")]
    [SerializeField] private TMP_Text roomTypeText;
    [SerializeField] private bool showRoomTypeText = false;

    [Header("Frame Sprite")]
    [SerializeField] private Sprite baseFrameSprite;

    [Header("Room Type Icon Sprites")]
    [SerializeField] private Sprite startIconSprite;
    [SerializeField] private Sprite combatIconSprite;
    [SerializeField] private Sprite restIconSprite;
    [SerializeField] private Sprite shopIconSprite;
    [SerializeField] private Sprite randomIconSprite;
    [SerializeField] private Sprite rewardIconSprite;
    [SerializeField] private Sprite bossIconSprite;

    [Header("Overlay Sprites")]
    [SerializeField] private Sprite clearedCheckSprite;

    [Header("State Colors")]
    [SerializeField] private Color normalColor = Color.white;

    // 못 가는 노드는 기존 이미지보다 어둡게만 보이도록 색상 곱하기 처리
    [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    private string nodeId;
    private RunMapOverlayUI owner;

    private void Reset()
    {
        // 컴포넌트를 처음 붙였을 때 기본 참조를 최대한 자동으로 잡는다.
        if (button == null)
            button = GetComponent<Button>();

        if (frameImage == null)
            frameImage = GetComponent<Image>();

        if (roomTypeText == null)
            roomTypeText = GetComponentInChildren<TMP_Text>(true);
    }

    /// <summary>
    /// 노드 데이터를 받아 UI를 갱신한다.
    /// </summary>
    public void Bind(
        RunMapOverlayUI owner,
        MapNodeData nodeData,
        bool interactable,
        NodeVisualState visualState)
    {
        this.owner = owner;

        if (nodeData == null)
        {
            nodeId = string.Empty;
            ApplyEmptyState();
            return;
        }

        nodeId = nodeData.nodeId;

        ApplyVisualState(nodeData, interactable, visualState);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickNode);

            // Button의 Color Tint가 우리가 지정한 색을 덮어씌우지 않게 막는다.
            // 상태 표현은 이 스크립트에서 직접 처리한다.
            button.transition = Selectable.Transition.None;
        }
    }

    private void OnClickNode()
    {
        if (owner == null)
            return;

        if (string.IsNullOrEmpty(nodeId))
            return;

        owner.TryNodeClick(nodeId);
    }

    /// <summary>
    /// 노드 상태에 맞춰 프레임, 아이콘, 체크 표시를 갱신한다.
    /// </summary>
    private void ApplyVisualState(MapNodeData nodeData, bool interactable, NodeVisualState visualState)
    {
        if (button != null)
            button.interactable = interactable;

        RoomType effectiveRoomType = GetEffectiveRoomType(nodeData);

        // 1. 모든 노드는 같은 공통 프레임을 사용한다.
        SetImage(frameImage, baseFrameSprite);

        // 2. 노드 종류는 아이콘으로 표시한다.
        SetImage(iconImage, GetIconSprite(effectiveRoomType));

        // 3. Locked만 어둡게, 나머지는 원본 색 그대로.
        bool isLocked = visualState == NodeVisualState.Locked;
        Color stateColor = isLocked ? lockedColor : normalColor;

        SetImageColor(frameImage, stateColor);
        SetImageColor(iconImage, stateColor);

        // 4. Cleared는 체크 표시만 올린다.
        bool isCleared = visualState == NodeVisualState.Cleared;
        SetImage(clearedCheckImage, isCleared ? clearedCheckSprite : null);
        SetImageColor(clearedCheckImage, normalColor);

        // 5. 텍스트는 디버그용. 아이콘 UI로 갈 거면 false 추천.
        if (roomTypeText != null)
        {
            roomTypeText.gameObject.SetActive(showRoomTypeText);
            roomTypeText.text = GetDisplayText(nodeData);
            roomTypeText.color = stateColor;
        }
    }

    /// <summary>
    /// 비어 있거나 잘못된 노드 데이터가 들어왔을 때 안전하게 표시한다.
    /// </summary>
    private void ApplyEmptyState()
    {
        if (button != null)
            button.interactable = false;

        SetImage(frameImage, baseFrameSprite);
        SetImage(iconImage, null);
        SetImage(clearedCheckImage, null);

        SetImageColor(frameImage, lockedColor);

        if (roomTypeText != null)
        {
            roomTypeText.gameObject.SetActive(showRoomTypeText);
            roomTypeText.text = string.Empty;
            roomTypeText.color = lockedColor;
        }
    }

    /// <summary>
    /// Image에 Sprite를 넣는다.
    /// Sprite가 null이면 Image를 꺼서 안 보이게 한다.
    /// </summary>
    private void SetImage(Image targetImage, Sprite sprite)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = sprite;
        targetImage.enabled = sprite != null;
    }

    /// <summary>
    /// Image 색상을 바꾼다.
    /// Sprite가 들어간 뒤에 호출해야 색이 제대로 유지된다.
    /// </summary>
    private void SetImageColor(Image targetImage, Color color)
    {
        if (targetImage == null)
            return;

        targetImage.color = color;
    }

    /// <summary>
    /// Random 노드가 이미 실제 타입으로 확정됐으면 그 확정 타입 아이콘을 보여준다.
    /// 예: Random -> Shop으로 확정됨 = Shop 아이콘 표시
    /// </summary>
    private RoomType GetEffectiveRoomType(MapNodeData nodeData)
    {
        if (nodeData == null)
            return RoomType.Random;

        if (nodeData.roomType == RoomType.Random && nodeData.hasResolvedRandomType)
            return nodeData.resolvedRandomType;

        return nodeData.roomType;
    }

    private Sprite GetIconSprite(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Start:
                return startIconSprite;

            case RoomType.Combat:
                return combatIconSprite;

            case RoomType.Rest:
                return restIconSprite;

            case RoomType.Shop:
                return shopIconSprite;

            case RoomType.Random:
                return randomIconSprite;

            case RoomType.Reward:
                return rewardIconSprite;

            case RoomType.Boss:
                return bossIconSprite;

            default:
                return randomIconSprite;
        }
    }

    /// <summary>
    /// 디버그 텍스트용.
    /// showRoomTypeText가 false면 실제 화면에는 안 나온다.
    /// </summary>
    private string GetDisplayText(MapNodeData nodeData)
    {
        if (nodeData == null)
            return string.Empty;

        RoomType effectiveRoomType = GetEffectiveRoomType(nodeData);

        if (effectiveRoomType == RoomType.Boss)
            return "BOSS";

        return effectiveRoomType.ToString();
    }
}