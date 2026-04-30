using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunData
{
    [Header("Player HP")]
    public int currentHp;
    public int maxHp;

    [Header("Equipped Weapons")]
    // 플레이어가 장착한 2개의 무기 상태
    public WeaponLoadoutData[] equippedWeapons = new WeaponLoadoutData[2];

    [Header("Current Weapon Slot")]
    // 현재 손에 들고 있는 무기 슬롯 인덱스 (0 또는 1)
    public int currentWeaponSlotIndex;

    [Header("Ammo Deck")]
    // 전투 시작 시 이 전체 덱을 기반으로 CombatRuntime이 draw pile을 만든다
    public List<AmmoModuleData> ammoDeck = new List<AmmoModuleData>();

    [Header("Inventory")]
    public InventoryData inventory = new InventoryData();

    [Header("RoomNode")]
    public RunMapData mapData = new RunMapData();

    [Header("Currency")]
    public int gold;
    [Header("Shop Service")]
    public int removeAmmoPrice;

}

[Serializable]
public class WeaponLoadoutData
{
    public bool hasWeapon;
    // 현재 장착된 무기 기본 정보
    public WeaponData weaponData;

    // 해당 무기에 장착된 부착물 목록
    public List<WeaponAttachmentData> equippedAttachments = new List<WeaponAttachmentData>();
}

[Serializable]
public class InventoryData
{
    // 인벤토리 안의 잉여 부착물
    public List<WeaponAttachmentData> spareAttachments = new List<WeaponAttachmentData>();

    // 나중에 필요하면 여기다 확장
    // public List<WeaponData> spareWeapons = new List<WeaponData>();
    // public List<ThrowableData> throwables = new List<ThrowableData>();
}