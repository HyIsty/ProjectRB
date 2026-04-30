using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탄창 슬롯 관리 스크립트.
/// Queue를 사용해서 "먼저 들어간 탄이 먼저 나가는" 구조를 만든다.
/// 권총 기준 최소 구현으로 4칸을 기본값으로 둔다.
/// </summary>
public class MagazineSlotQueue : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private AmmoDeckRuntime ammoDeck;

    [Header("Magazine Setting")]
    [SerializeField] private int slotCapacity = 4;
    [SerializeField] private bool autoLoadOnStart = true;

    // Queue = 선입선출
    private Queue<AmmoModuleData> loadedRounds = new Queue<AmmoModuleData>();

    // 마지막으로 소비된 탄환을 기록해 두면 디버그 패널에 표시하기 좋다.
    private AmmoModuleData lastConsumedRound;

    public int SlotCapacity => slotCapacity;
    public int LoadedCount => loadedRounds.Count;
    public int EmptySlotCount => Mathf.Max(0, slotCapacity - loadedRounds.Count);

    public string LastConsumedRoundName
    {
        get
        {
            return lastConsumedRound != null ? lastConsumedRound.displayName : "None";
        }
    }

    private void Start()
    {
        if (autoLoadOnStart)
        {
            ReloadToFull();
        }
    }

    /// <summary>
    /// 탄창에 탄이 있는지 확인한다.
    /// </summary>
    public bool HasAmmo()
    {
        return loadedRounds.Count > 0;
    }

    /// <summary>
    /// 다음에 발사될 탄환을 미리 확인한다.
    /// Queue 특성상 맨 앞 탄환이 다음 발사 탄환이다.
    /// </summary>
    public AmmoModuleData PeekNextRound()
    {
        if (loadedRounds.Count == 0)
            return null;

        return loadedRounds.Peek();
    }

    /// <summary>
    /// 빈 슬롯이 하나 이상 있으면 탄 1발만 장전한다.
    /// 실제 장전 상세 UI는 나중에 만들고,
    /// 지금은 최소 구현용 자동 장전만 제공한다.
    /// </summary>
    public bool TryReloadOne()
    {
        if (ammoDeck == null)
        {
            Debug.LogError("[MagazineSlotQueue] AmmoDeckRuntime reference is missing.");
            return false;
        }

        if (loadedRounds.Count >= slotCapacity)
        {
            Debug.Log("[MagazineSlotQueue] Magazine is already full.");
            return false;
        }

        AmmoModuleData drawnRound = ammoDeck.DrawOne();

        if (drawnRound == null)
        {
            Debug.LogWarning("[MagazineSlotQueue] Reload failed. No round available in deck.");
            return false;
        }

        loadedRounds.Enqueue(drawnRound);

        Debug.Log($"[MagazineSlotQueue] Reloaded -> {drawnRound.displayName} | Loaded={loadedRounds.Count}/{slotCapacity}");
        return true;
    }

    /// <summary>
    /// 탄창이 찰 때까지 자동 장전한다.
    /// 가능한 만큼만 채운다.
    /// </summary>
    public int ReloadToFull()
    {
        int reloadCount = 0;

        while (loadedRounds.Count < slotCapacity)
        {
            bool success = TryReloadOne();

            if (!success)
                break;

            reloadCount++;
        }

        Debug.Log($"[MagazineSlotQueue] ReloadToFull complete. Added={reloadCount}, Loaded={loadedRounds.Count}/{slotCapacity}");
        return reloadCount;
    }

    /// <summary>
    /// 다음 탄환 1발을 소비한다.
    /// 소비된 탄환은 자동으로 Discard Pile로 보낸다.
    /// 현재 최소 구현에서는 "발사 시도 = 탄 소비"로 보는 것이 디버깅에 가장 단순하다.
    /// </summary>
    public AmmoModuleData ConsumeNextRound()
    {
        if (ammoDeck == null)
        {
            Debug.LogError("[MagazineSlotQueue] AmmoDeckRuntime reference is missing.");
            return null;
        }

        if (loadedRounds.Count == 0)
        {
            Debug.LogWarning("[MagazineSlotQueue] Cannot consume round. Magazine is empty.");
            return null;
        }

        AmmoModuleData usedRound = loadedRounds.Dequeue();
        lastConsumedRound = usedRound;

        // 소비된 탄환은 버림 더미로 이동한다.
        ammoDeck.Discard(usedRound);

        Debug.Log($"[MagazineSlotQueue] Consumed -> {usedRound.displayName} | Loaded={loadedRounds.Count}/{slotCapacity}");
        return usedRound;
    }

    /// <summary>
    /// 탄창을 비운다.
    /// reset용 디버그 함수로 유용하다.
    /// discardLoadedRounds가 true면 비워진 탄들도 discard로 보낸다.
    /// </summary>
    public void ClearMagazine(bool discardLoadedRounds)
    {
        if (discardLoadedRounds && ammoDeck == null)
        {
            Debug.LogError("[MagazineSlotQueue] AmmoDeckRuntime reference is missing.");
            return;
        }

        while (loadedRounds.Count > 0)
        {
            AmmoModuleData round = loadedRounds.Dequeue();

            if (discardLoadedRounds)
            {
                ammoDeck.Discard(round);
            }
        }

        Debug.Log("[MagazineSlotQueue] Magazine cleared.");
    }

    /// <summary>
    /// 디버그 UI용으로 탄창 상태를 문자열 배열로 반환한다.
    /// 예: [Basic, Heavy, Empty, Empty]
    /// </summary>
    public string[] GetSlotDisplayArray()
    {
        string[] result = new string[slotCapacity];

        // Queue를 바로 배열처럼 인덱싱할 수 없으므로,
        // foreach로 순서대로 복사해서 넣는다.
        int index = 0;
        foreach (AmmoModuleData round in loadedRounds)
        {
            result[index] = round.displayName;
            index++;
        }

        // 남는 칸은 Empty로 채운다.
        for (int i = index; i < slotCapacity; i++)
        {
            result[i] = "Empty";
        }

        return result;
    }
}