using System;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

[Serializable]
public class DeckBuildCardDataExample
{
    [SerializeField] private string cardId = "card_000";
    [SerializeField] private string cardName = "Strike";
    [SerializeField] private DeckBuildCardTypeExample cardType = DeckBuildCardTypeExample.Attack;
    [SerializeField] private int cost = 1;
    [SerializeField] private int value = 5;
    [SerializeField] private bool exhaustOnPlay = false;

    public string CardId => cardId;
    public string CardName => cardName;
    public DeckBuildCardTypeExample CardType => cardType;
    public int Cost => cost;
    public int Value => value;
    public bool ExhaustOnPlay => exhaustOnPlay;

    public DeckBuildCardDataExample Clone()
    {
        return new DeckBuildCardDataExample
        {
            cardId = this.cardId,
            cardName = this.cardName,
            cardType = this.cardType,
            cost = this.cost,
            value = this.value,
            exhaustOnPlay = this.exhaustOnPlay
        };
    }
}