using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탄 모듈 덱의 런타임 관리 스크립트.
/// - 스타팅 덱 생성
/// - Draw Pile 관리
/// - Discard Pile 관리
/// - Draw / Discard / Shuffle
/// 를 담당한다.
/// </summary>
public class AmmoDeckRuntime : MonoBehaviour
{
    [Header("Starter Deck (Inspector Input)")]
    [SerializeField] private List<AmmoModuleEntry> starterDeckEntries = new List<AmmoModuleEntry>();

    [Header("Build Option")]
    [SerializeField] private bool buildDeckOnAwake = true;
    [SerializeField] private bool shuffleAfterBuild = true;

    // 실제 런타임에서 도는 더미들
    private List<AmmoModuleData> drawPile = new List<AmmoModuleData>();
    private List<AmmoModuleData> discardPile = new List<AmmoModuleData>();

    public int DrawCount => drawPile.Count;
    public int DiscardCount => discardPile.Count;

    private void Awake()
    {
        if (buildDeckOnAwake)
        {
            BuildStarterDeck();
        }
    }

    /// <summary>
    /// 인스펙터에 적어 둔 스타팅 덱 정보를 바탕으로
    /// 실제 런타임 Draw Pile을 새로 만든다.
    /// </summary>
    [ContextMenu("Build Starter Deck")]
    public void BuildStarterDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        // AmmoModuleEntry는 "설계도" 느낌이고,
        // AmmoModuleData는 "실제 카드 1장" 느낌이다.
        for (int i = 0; i < starterDeckEntries.Count; i++)
        {
            AmmoModuleEntry entry = starterDeckEntries[i];

            if (entry == null)
                continue;

            drawPile.Add(new AmmoModuleData(entry));
        }

        if (shuffleAfterBuild)
        {
            ShuffleDrawPile();
        }

        Debug.Log($"[AmmoDeckRuntime] Starter deck built. Draw={drawPile.Count}, Discard={discardPile.Count}");
    }


    /// <summary>
    /// Draw Pile을 셔플한다.
    /// </summary>
    [ContextMenu("Shuffle Draw Pile")]
    public void ShuffleDrawPile()
    {
        ShuffleList(drawPile);
        Debug.Log("[AmmoDeckRuntime] Draw pile shuffled.");
    }

    /// <summary>
    /// 카드 1장을 뽑는다.
    /// Draw Pile이 비어 있으면 Discard를 섞어서 Draw로 옮긴 뒤 다시 뽑는다.
    /// </summary>
    public AmmoModuleData DrawOne()
    {
        // 먼저 드로우 더미가 비었는지 확인한다.
        if (drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDraw();
        }

        // 그래도 비어 있으면 정말 뽑을 카드가 없는 것이다.
        if (drawPile.Count == 0)
        {
            Debug.LogWarning("[AmmoDeckRuntime] No card to draw. Draw and Discard are both empty.");
            return null;
        }

        // 리스트의 마지막 장을 뽑는 방식으로 처리한다.
        int lastIndex = drawPile.Count - 1;
        AmmoModuleData drawnCard = drawPile[lastIndex];
        drawPile.RemoveAt(lastIndex);

        Debug.Log($"[AmmoDeckRuntime] Draw -> {drawnCard.displayName} | Draw={drawPile.Count}, Discard={discardPile.Count}");
        return drawnCard;
    }

    /// <summary>
    /// 카드 여러 장을 뽑는다.
    /// count만큼 뽑되, 못 뽑는 경우 가능한 만큼만 뽑는다.
    /// </summary>
    public List<AmmoModuleData> DrawMultiple(int count)
    {
        List<AmmoModuleData> result = new List<AmmoModuleData>();

        for (int i = 0; i < count; i++)
        {
            AmmoModuleData drawn = DrawOne();

            if (drawn == null)
                break;

            result.Add(drawn);
        }

        return result;
    }

    /// <summary>
    /// 사용한 탄환 카드를 버림 더미로 보낸다.
    /// </summary>
    public void Discard(AmmoModuleData usedCard)
    {
        if (usedCard == null)
            return;

        discardPile.Add(usedCard);
        Debug.Log($"[AmmoDeckRuntime] Discard -> {usedCard.displayName} | Draw={drawPile.Count}, Discard={discardPile.Count}");
    }

    /// <summary>
    /// Discard Pile 전체를 Draw Pile로 옮기고 섞는다.
    /// </summary>
    public void ReshuffleDiscardIntoDraw()
    {
        if (discardPile.Count == 0)
        {
            Debug.LogWarning("[AmmoDeckRuntime] Cannot reshuffle. Discard pile is empty.");
            return;
        }

        drawPile.AddRange(discardPile);
        discardPile.Clear();

        ShuffleDrawPile();

        Debug.Log($"[AmmoDeckRuntime] Reshuffled discard into draw. Draw={drawPile.Count}, Discard={discardPile.Count}");
    }
    public IReadOnlyList<AmmoModuleData> GetDrawPileDataSnapshot()
    {
        return new List<AmmoModuleData>(drawPile);
    }

    public IReadOnlyList<AmmoModuleData> GetDiscardPileDataSnapshot()
    {
        return new List<AmmoModuleData>(discardPile);
    }

    /// <summary>
    /// Fisher-Yates 방식으로 리스트를 섞는다.
    /// </summary>
    private void ShuffleList(List<AmmoModuleData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            AmmoModuleData temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void SetDeckFromRun(List<AmmoModuleData> runDeck, bool shuffle = true)
    {
        drawPile.Clear();
        discardPile.Clear();

        if (runDeck == null)
        {
            Debug.LogWarning("[AmmoDeckRuntime] SetDeckFromRun called with null runDeck.");
            return;
        }

        for (int i = 0; i < runDeck.Count; i++)
        {
            AmmoModuleData ammo = runDeck[i];
            if (ammo == null)
                continue;

            // AmmoModuleData가 참조형(class)이고
            // 런타임에서 개별 카드 인스턴스를 분리하고 싶으면
            // 여기서 복제 생성자가 있으면 그걸 쓰는 게 더 안전하다.
            // 예: drawPile.Add(new AmmoModuleData(ammo));
            drawPile.Add(ammo);
        }

        if (shuffle)
            ShuffleDrawPile();

        Debug.Log($"[AmmoDeckRuntime] Run deck applied. Draw={drawPile.Count}, Discard={discardPile.Count}");
    }
}