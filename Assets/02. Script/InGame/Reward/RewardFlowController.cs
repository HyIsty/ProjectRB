using System.Collections.Generic;
using UnityEngine;
using System;

public class RewardFlowController : MonoBehaviour
{
    [Header("Reward References")]
    [SerializeField] private RewardFactory rewardFactory;
    [SerializeField] private RewardPanelUI rewardPanelUI;
    [SerializeField] private WeaponReplacePopupUI weaponReplacePopupUI;

    private RewardCandidate pendingWeaponReward;

    public event Action OnRewardCompleted;

    public void OpenReward()
    {
        if (rewardFactory == null)
        {
            Debug.LogWarning("[RewardFlowController] RewardFactory is missing.");
            return;
        }

        if (rewardPanelUI == null)
        {
            Debug.LogWarning("[RewardFlowController] RewardPanelUI is missing.");
            return;
        }

        List<RewardCandidate> candidates = rewardFactory.GenerateRewards();

        rewardPanelUI.Show(candidates, this);

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();
    }

    public void SelectReward(RewardCandidate candidate)
    {
        if (candidate == null)
            return;

        if (RunGameManager.Instance == null || !RunGameManager.Instance.HasActiveRun)
        {
            Debug.LogWarning("[RewardFlowController] No active run.");
            return;
        }

        RunData runData = RunGameManager.Instance.CurrentRunData;

        if (runData == null)
        {
            Debug.LogWarning("[RewardFlowController] RunData is null.");
            return;
        }

        switch (candidate.rewardType)
        {
            case RewardType.Weapon:
                ApplyWeaponReward(candidate, runData);
                break;

            case RewardType.Ammo:
                ApplyAmmoReward(candidate, runData);
                ApplyGoldBonus(candidate, runData);
                CompleteRewardFlow();
                break;

            case RewardType.Attachment:
                ApplyAttachmentReward(candidate, runData);
                ApplyGoldBonus(candidate, runData);
                CompleteRewardFlow();
                break;
        }
    }

    public void SkipReward()
    {
        pendingWeaponReward = null;

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();

        CompleteRewardFlow();
    }

    private void ApplyAmmoReward(RewardCandidate candidate, RunData runData)
    {
        if (candidate == null || candidate.ammoData == null || runData == null)
            return;

        if (runData.ammoDeck == null)
            runData.ammoDeck = new List<AmmoModuleData>();

        // 현재 프로젝트가 데이터 복사를 따로 쓰고 있으면 그 복사 함수를 써도 된다.
        runData.ammoDeck.Add(candidate.ammoData);

        Debug.Log($"[Reward] Ammo gained: {candidate.ammoData.displayName}");
    }

    private void ApplyAttachmentReward(RewardCandidate candidate, RunData runData)
    {
        if (candidate == null || candidate.attachmentData == null || runData == null)
            return;

        if (runData.inventory == null)
            runData.inventory = new InventoryData();

        if (runData.inventory.spareAttachments == null)
            runData.inventory.spareAttachments = new List<WeaponAttachmentData>();

        runData.inventory.spareAttachments.Add(candidate.attachmentData);

        Debug.Log($"[Reward] Attachment gained: {candidate.attachmentData.attachmentName}");
    }

    private void ApplyWeaponReward(RewardCandidate candidate, RunData runData)
    {
        if (candidate == null || candidate.weaponData == null || runData == null)
            return;

        int emptySlotIndex = FindEmptyWeaponSlot(runData);

        if (emptySlotIndex >= 0)
        {
            SetWeaponToSlot(runData, emptySlotIndex, candidate.weaponData);

            // 빈 슬롯에 바로 들어간 경우는 여기서 골드 지급
            ApplyGoldBonus(candidate, runData);

            CompleteRewardFlow();
            return;
        }

        // 두 슬롯이 모두 찼으면 교체 팝업으로 넘긴다.
        pendingWeaponReward = candidate;

        if (weaponReplacePopupUI == null)
        {
            Debug.LogWarning("[RewardFlowController] WeaponReplacePopupUI is missing. Cannot replace weapon.");
            pendingWeaponReward = null;
            return;
        }

        weaponReplacePopupUI.Show(
            runData,
            OnWeaponReplaceConfirmed,
            OnWeaponReplaceCanceled
        );
    }

    private int FindEmptyWeaponSlot(RunData runData)
    {
        if (runData == null || runData.equippedWeapons == null)
            return -1;

        for (int i = 0; i < runData.equippedWeapons.Length; i++)
        {
            WeaponLoadoutData slot = runData.equippedWeapons[i];

            if (slot == null)
                return i;

            if (!slot.hasWeapon)
                return i;

            if (slot.weaponData == null)
                return i;
        }

        return -1;
    }

    private void SetWeaponToSlot(RunData runData, int slotIndex, WeaponData weaponData)
    {
        if (runData == null || weaponData == null)
            return;

        if (runData.equippedWeapons == null || runData.equippedWeapons.Length < 2)
            runData.equippedWeapons = new WeaponLoadoutData[2];

        if (slotIndex < 0 || slotIndex >= runData.equippedWeapons.Length)
            return;

        if (runData.equippedWeapons[slotIndex] == null)
            runData.equippedWeapons[slotIndex] = new WeaponLoadoutData();

        WeaponLoadoutData slot = runData.equippedWeapons[slotIndex];

        slot.hasWeapon = true;
        slot.weaponData = weaponData;

        // 새 무기를 넣으면 기존 부착물은 비운다.
        if (slot.equippedAttachments == null)
            slot.equippedAttachments = new List<WeaponAttachmentData>();
        else
            slot.equippedAttachments.Clear();

        runData.currentWeaponSlotIndex = slotIndex;

        Debug.Log($"[Reward] Weapon set to slot {slotIndex}: {weaponData.weaponName}");
    }

    private void OnWeaponReplaceConfirmed(int slotIndex)
    {
        if (pendingWeaponReward == null)
            return;

        if (RunGameManager.Instance == null || !RunGameManager.Instance.HasActiveRun)
            return;

        RunData runData = RunGameManager.Instance.CurrentRunData;

        if (runData == null)
            return;

        SetWeaponToSlot(runData, slotIndex, pendingWeaponReward.weaponData);

        // 무기 교체가 확정된 순간에만 골드 지급
        ApplyGoldBonus(pendingWeaponReward, runData);

        pendingWeaponReward = null;

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();

        CompleteRewardFlow();
    }

    private void OnWeaponReplaceCanceled()
    {
        // 취소하면 골드 지급 없음.
        pendingWeaponReward = null;

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();

        // RewardPanel은 유지한다.
    }

    private void ApplyGoldBonus(RewardCandidate candidate, RunData runData)
    {
        if (candidate == null || runData == null)
            return;

        int amount = Mathf.Max(0, candidate.goldAmount);

        runData.gold += amount;

        Debug.Log($"[Reward] Gold Bonus +{amount}. Current Gold = {runData.gold}");
    }

    private void CompleteRewardFlow()
    {
        pendingWeaponReward = null;

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();

        if (rewardPanelUI != null)
            rewardPanelUI.Hide();

        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();

        OnRewardCompleted?.Invoke();
    }
}