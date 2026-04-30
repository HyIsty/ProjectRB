using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AdaptivePerformance;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class DeckBuildControllerExample : MonoBehaviour
{
    [Header("Start Deck")]
    [SerializeField] private List<DeckBuildCardDataExample> startingDeck = new List<DeckBuildCardDataExample>();

    [Header("Turn Rule")]
    [SerializeField] private int drawAmountPerTurn = 5;
    [SerializeField] private int maxEnergyPerTurn = 3;
    [SerializeField] private bool autoInitializeOnStart = true;

    [Header("Runtime")]
    [SerializeField] private List<DeckBuildCardDataExample> drawPile = new List<DeckBuildCardDataExample>();
    [SerializeField] private List<DeckBuildCardDataExample> discardPile = new List<DeckBuildCardDataExample>();
    [SerializeField] private List<DeckBuildCardDataExample> exhaustPile = new List<DeckBuildCardDataExample>();
    [SerializeField] private List<DeckBuildCardDataExample> hand = new List<DeckBuildCardDataExample>();

    [Header("State")]
    [SerializeField] private int currentEnergy = 0;
    [SerializeField] private int turnCount = 0;
    [SerializeField] private int selectedHandIndex = 0;
    [SerializeField] private bool isInitialized = false;

    [Header("Gizmo Layout")]
    [SerializeField] private Vector3 drawPileOffset = new Vector3(-6f, 2f, 0f);
    [SerializeField] private Vector3 discardPileOffset = new Vector3(-3f, 2f, 0f);
    [SerializeField] private Vector3 exhaustPileOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private Vector3 handStartOffset = new Vector3(-5f, -2.5f, 0f);
    [SerializeField] private Vector3 energyOffset = new Vector3(4.5f, 2.4f, 0f);

    [Header("Gizmo Size")]
    [SerializeField] private float handCardSpacing = 1.8f;
    [SerializeField] private Vector2 cardSize = new Vector2(1.2f, 1.8f);
    [SerializeField] private Vector2 pileZoneSize = new Vector2(1.4f, 1.9f);
    [SerializeField] private float stackOffset = 0.08f;
    [SerializeField] private int visibleStackCount = 5;

    private void Start()
    {
        if (autoInitializeOnStart && Application.isPlaying)
        {
            InitializeDeck();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        HandleInput();
    }

    [ContextMenu("Initialize Deck")]
    public void InitializeDeck()
    {
        drawPile.Clear();
        discardPile.Clear();
        exhaustPile.Clear();
        hand.Clear();

        for (int i = 0; i < startingDeck.Count; i++)
        {
            if (startingDeck[i] == null)
                continue;

            drawPile.Add(startingDeck[i].Clone());
        }

        Shuffle(drawPile);

        currentEnergy = 0;
        turnCount = 0;
        selectedHandIndex = 0;
        isInitialized = true;

        StartNewTurn();
        Debug.Log("덱 초기화 완료");
    }

    [ContextMenu("Start New Turn")]
    public void StartNewTurn()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("먼저 InitializeDeck을 호출하세요.");
            return;
        }

        turnCount++;
        currentEnergy = maxEnergyPerTurn;

        DrawCards(drawAmountPerTurn - hand.Count);
        EnsureValidSelectedIndex();

        Debug.Log($"턴 시작 / Turn {turnCount} / Energy {currentEnergy}");
    }

    [ContextMenu("End Turn")]
    public void EndTurn()
    {
        if (!isInitialized)
            return;

        DiscardEntireHand();
        currentEnergy = 0;
        selectedHandIndex = 0;

        StartNewTurn();
    }

    [ContextMenu("Draw 1")]
    public void DrawOneDebug()
    {
        if (!isInitialized)
            return;

        DrawCards(1);
        EnsureValidSelectedIndex();
    }

    [ContextMenu("Play Selected Card")]
    public void PlaySelectedCard()
    {
        if (!isInitialized)
            return;

        if (!HasValidHandSelection())
        {
            Debug.Log("플레이할 카드가 없습니다.");
            return;
        }

        DeckBuildCardDataExample card = hand[selectedHandIndex];

        if (card == null)
            return;

        if (currentEnergy < card.Cost)
        {
            Debug.Log($"에너지 부족 / 필요 {card.Cost} / 현재 {currentEnergy}");
            return;
        }

        currentEnergy -= card.Cost;
        hand.RemoveAt(selectedHandIndex);

        ResolveCard(card);

        if (card.ExhaustOnPlay)
            exhaustPile.Add(card);
        else
            discardPile.Add(card);

        EnsureValidSelectedIndex();

        Debug.Log($"카드 사용: {card.CardName} / 남은 Energy {currentEnergy}");
    }

    [ContextMenu("Discard Selected Card")]
    public void DiscardSelectedCardDebug()
    {
        if (!isInitialized)
            return;

        if (!HasValidHandSelection())
            return;

        DeckBuildCardDataExample card = hand[selectedHandIndex];
        hand.RemoveAt(selectedHandIndex);
        discardPile.Add(card);

        EnsureValidSelectedIndex();

        Debug.Log($"손패 버리기: {card.CardName}");
    }

    private void HandleInput()
    {
        if (!isInitialized)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                InitializeDeck();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
            SelectPreviousCard();

        if (Input.GetKeyDown(KeyCode.E))
            SelectNextCard();

        if (Input.GetKeyDown(KeyCode.Space))
            PlaySelectedCard();

        if (Input.GetKeyDown(KeyCode.Backspace))
            DiscardSelectedCardDebug();

        if (Input.GetKeyDown(KeyCode.Return))
            EndTurn();

        if (Input.GetKeyDown(KeyCode.T))
            DrawOneDebug();

        if (Input.GetKeyDown(KeyCode.R))
            InitializeDeck();
    }

    private void ResolveCard(DeckBuildCardDataExample card)
    {
        switch (card.CardType)
        {
            case DeckBuildCardTypeExample.Attack:
                Debug.Log($"[Attack] {card.CardName} / Damage {card.Value}");
                break;

            case DeckBuildCardTypeExample.Block:
                Debug.Log($"[Block] {card.CardName} / Block {card.Value}");
                break;

            case DeckBuildCardTypeExample.Draw:
                Debug.Log($"[Draw] {card.CardName} / Draw {card.Value}");
                DrawCards(card.Value);
                break;

            case DeckBuildCardTypeExample.Energy:
                currentEnergy += card.Value;
                Debug.Log($"[Energy] {card.CardName} / Gain Energy {card.Value}");
                break;

            case DeckBuildCardTypeExample.Utility:
                Debug.Log($"[Utility] {card.CardName} / Value {card.Value}");
                break;
        }
    }

    private void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (!TryDrawOne())
                break;
        }
    }

    private bool TryDrawOne()
    {
        if (drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDrawPile();
        }

        if (drawPile.Count == 0)
            return false;

        int topIndex = drawPile.Count - 1;
        DeckBuildCardDataExample drawnCard = drawPile[topIndex];
        drawPile.RemoveAt(topIndex);
        hand.Add(drawnCard);

        return true;
    }

    private void ReshuffleDiscardIntoDrawPile()
    {
        if (discardPile.Count == 0)
            return;

        for (int i = 0; i < discardPile.Count; i++)
        {
            drawPile.Add(discardPile[i]);
        }

        discardPile.Clear();
        Shuffle(drawPile);

        Debug.Log("버린 더미를 섞어서 드로우 더미로 이동");
    }

    private void DiscardEntireHand()
    {
        while (hand.Count > 0)
        {
            DeckBuildCardDataExample card = hand[0];
            hand.RemoveAt(0);
            discardPile.Add(card);
        }
    }

    private void SelectPreviousCard()
    {
        if (hand.Count == 0)
            return;

        selectedHandIndex--;

        if (selectedHandIndex < 0)
            selectedHandIndex = hand.Count - 1;
    }

    private void SelectNextCard()
    {
        if (hand.Count == 0)
            return;

        selectedHandIndex++;

        if (selectedHandIndex >= hand.Count)
            selectedHandIndex = 0;
    }

    private bool HasValidHandSelection()
    {
        return hand.Count > 0 && selectedHandIndex >= 0 && selectedHandIndex < hand.Count;
    }

    private void EnsureValidSelectedIndex()
    {
        if (hand.Count == 0)
        {
            selectedHandIndex = 0;
            return;
        }

        if (selectedHandIndex >= hand.Count)
            selectedHandIndex = hand.Count - 1;

        if (selectedHandIndex < 0)
            selectedHandIndex = 0;
    }

    private void Shuffle(List<DeckBuildCardDataExample> targetList)
    {
        for (int i = targetList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            DeckBuildCardDataExample temp = targetList[i];
            targetList[i] = targetList[randomIndex];
            targetList[randomIndex] = temp;
        }
    }

    private void OnDrawGizmos()
    {
        DrawPileZone(transform.position + drawPileOffset, drawPile, "Draw");
        DrawPileZone(transform.position + discardPileOffset, discardPile, "Discard");
        DrawPileZone(transform.position + exhaustPileOffset, exhaustPile, "Exhaust");
        DrawHandZone();
        DrawEnergyZone();
        DrawStateLabel();
    }

    private void DrawPileZone(Vector3 center, List<DeckBuildCardDataExample> pile, string label)
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, new Vector3(pileZoneSize.x, pileZoneSize.y, 0.1f));

        int countToDraw = Mathf.Min(pile.Count, visibleStackCount);

        for (int i = 0; i < countToDraw; i++)
        {
            int cardIndex = pile.Count - 1 - i;
            DeckBuildCardDataExample card = pile[cardIndex];

            Vector3 offset = new Vector3(i * stackOffset, i * stackOffset, 0f);
            Vector3 pos = center + offset;

            Gizmos.color = GetCardColor(card);
            Gizmos.DrawCube(pos, new Vector3(cardSize.x * 0.85f, cardSize.y * 0.85f, 0.05f));

            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(pos, new Vector3(cardSize.x * 0.85f, cardSize.y * 0.85f, 0.05f));
        }

#if UNITY_EDITOR
        Handles.color = Color.white;
        Handles.Label(center + new Vector3(-0.65f, 1.25f, 0f), $"{label} : {pile.Count}");
#endif
    }

    private void DrawHandZone()
    {
        for (int i = 0; i < hand.Count; i++)
        {
            Vector3 pos = transform.position + handStartOffset + new Vector3(i * handCardSpacing, 0f, 0f);
            DeckBuildCardDataExample card = hand[i];

            bool isSelected = i == selectedHandIndex;
            bool canAfford = currentEnergy >= card.Cost;

            Color fillColor = GetCardColor(card);

            if (!canAfford)
                fillColor *= 0.5f;

            Gizmos.color = fillColor;
            Gizmos.DrawCube(pos, new Vector3(cardSize.x, cardSize.y, 0.05f));

            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(pos, new Vector3(cardSize.x, cardSize.y, 0.05f));

            if (isSelected)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(pos, new Vector3(cardSize.x * 1.15f, cardSize.y * 1.15f, 0.05f));
            }

#if UNITY_EDITOR
            string info = $"{card.CardName}\nC:{card.Cost} V:{card.Value}";
            Handles.color = Color.white;
            Handles.Label(pos + new Vector3(-0.45f, 1.15f, 0f), info);
#endif
        }

#if UNITY_EDITOR
        Handles.color = Color.white;
        Handles.Label(transform.position + handStartOffset + new Vector3(0f, -1.5f, 0f), $"Hand : {hand.Count}");
#endif
    }

    private void DrawEnergyZone()
    {
        for (int i = 0; i < maxEnergyPerTurn; i++)
        {
            Vector3 pos = transform.position + energyOffset + new Vector3(i * 0.7f, 0f, 0f);

            Gizmos.color = i < currentEnergy ? Color.cyan : new Color(0.25f, 0.25f, 0.25f, 1f);
            Gizmos.DrawSphere(pos, 0.22f);

            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(pos, 0.22f);
        }

#if UNITY_EDITOR
        Handles.color = Color.white;
        Handles.Label(transform.position + energyOffset + new Vector3(0f, 0.45f, 0f), $"Energy : {currentEnergy}/{maxEnergyPerTurn}");
#endif
    }

    private void DrawStateLabel()
    {
#if UNITY_EDITOR
        string selectedCardText = "None";

        if (HasValidHandSelection())
        {
            DeckBuildCardDataExample card = hand[selectedHandIndex];
            selectedCardText = $"{card.CardName} / Cost {card.Cost} / Type {card.CardType}";
        }

        Handles.color = Color.white;
        Handles.Label(
            transform.position + new Vector3(-6f, 4.2f, 0f),
            $"Turn : {turnCount}\nSelected : {selectedCardText}\nControls : Q/E Select, Space Play, Enter EndTurn, T Draw1, Backspace Discard, R Reset"
        );
#endif
    }

    private Color GetCardColor(DeckBuildCardDataExample card)
    {
        if (card == null)
            return Color.white;

        switch (card.CardType)
        {
            case DeckBuildCardTypeExample.Attack:
                return new Color(0.95f, 0.35f, 0.35f, 1f);

            case DeckBuildCardTypeExample.Block:
                return new Color(0.35f, 0.7f, 1f, 1f);

            case DeckBuildCardTypeExample.Draw:
                return new Color(0.8f, 0.4f, 1f, 1f);

            case DeckBuildCardTypeExample.Energy:
                return new Color(1f, 0.85f, 0.25f, 1f);

            case DeckBuildCardTypeExample.Utility:
                return new Color(0.55f, 0.9f, 0.55f, 1f);

            default:
                return Color.white;
        }
    }
}