using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 RoomTemplate 중 하나를 랜덤 선택해서 BoardManager에 전달한다.
/// 나중에 "전투방 / 이벤트방 / 보스방" 같은 규칙이 생기면
/// 여기서 선택 로직만 바꾸면 된다.
/// </summary>
public class RoomLoader : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private List<GameObject> roomTemplatePrefabs = new List<GameObject>();
    [SerializeField] private bool loadOnStart = true;

    private void Start()
    {
        if (loadOnStart)
            LoadRandomRoom();
    }

    [ContextMenu("Load Random Room")]
    public void LoadRandomRoom()
    {
        if (boardManager == null)
        {
            Debug.LogWarning("BoardManager is not assigned.");
            return;
        }

        if (roomTemplatePrefabs == null || roomTemplatePrefabs.Count == 0)
        {
            Debug.LogWarning("No room templates assigned.");
            return;
        }

        int randomIndex = Random.Range(0, roomTemplatePrefabs.Count);
        GameObject selectedPrefab = roomTemplatePrefabs[randomIndex];

        if (selectedPrefab == null)
        {
            Debug.LogWarning("Selected room template prefab is null.");
            return;
        }

        RoomTemplateAuthoring authoring = selectedPrefab.GetComponent<RoomTemplateAuthoring>();

        if (authoring == null)
        {
            Debug.LogWarning($"Prefab [{selectedPrefab.name}] has no RoomTemplateAuthoring component.");
            return;
        }

        RoomTemplateData runtimeData = authoring.CreateRuntimeData();
        boardManager.BuildRoom(runtimeData);
    }
}