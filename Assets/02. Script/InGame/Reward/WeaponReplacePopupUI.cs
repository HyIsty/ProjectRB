using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponReplacePopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Cancel Area")]
    [SerializeField] private Button outsideCancelButton;

    [Header("Slot A UI")]
    [SerializeField] private Button slotAButton;
    [SerializeField] private Image slotAWeaponIcon;
    [SerializeField] private TMP_Text slotAWeaponNameText;

    [Header("Slot B UI")]
    [SerializeField] private Button slotBButton;
    [SerializeField] private Image slotBWeaponIcon;
    [SerializeField] private TMP_Text slotBWeaponNameText;

    // 교체 확정 시 호출된다.
    // int 값은 교체할 무기 슬롯 인덱스다.
    // 0 = Slot A, 1 = Slot B
    private Action<int> onConfirmReplace;

    // 바깥 클릭으로 교체 선택을 취소했을 때 호출된다.
    private Action onCancelReplace;

    private void Awake()
    {
        // 바깥 영역 클릭 시 교체 취소
        if (outsideCancelButton != null)
        {
            outsideCancelButton.onClick.RemoveAllListeners();
            outsideCancelButton.onClick.AddListener(CancelReplace);
        }

        // Slot A 선택 시 0번 슬롯 교체 확정
        if (slotAButton != null)
        {
            slotAButton.onClick.RemoveAllListeners();
            slotAButton.onClick.AddListener(() => ConfirmReplace(0));
        }

        // Slot B 선택 시 1번 슬롯 교체 확정
        if (slotBButton != null)
        {
            slotBButton.onClick.RemoveAllListeners();
            slotBButton.onClick.AddListener(() => ConfirmReplace(1));
        }

        Hide();
    }

    public void Show(
        RunData runData,
        Action<int> confirmCallback,
        Action cancelCallback)
    {
        // RewardFlowController가 넘겨준 콜백을 저장한다.
        onConfirmReplace = confirmCallback;
        onCancelReplace = cancelCallback;

        if (popupRoot != null)
            popupRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        // 현재 장착 중인 두 무기를 버튼에 표시한다.
        RefreshSlotButtonUI(runData, 0, slotAWeaponIcon, slotAWeaponNameText);
        RefreshSlotButtonUI(runData, 1, slotBWeaponIcon, slotBWeaponNameText);
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        // 팝업이 닫히면 이전 콜백 참조를 비운다.
        onConfirmReplace = null;
        onCancelReplace = null;
    }

    private void ConfirmReplace(int slotIndex)
    {
        // 교체 확정은 RewardFlowController가 처리한다.
        // 이 UI는 "몇 번 슬롯을 골랐는지"만 넘긴다.
        if (onConfirmReplace == null)
        {
            Debug.LogWarning("[WeaponReplacePopupUI] Confirm callback is missing.");
            return;
        }

        onConfirmReplace.Invoke(slotIndex);
    }

    private void CancelReplace()
    {
        // 교체 선택만 취소한다.
        // RewardPanel은 RewardFlowController 쪽에서 유지한다.
        if (onCancelReplace != null)
            onCancelReplace.Invoke();
        else
            Hide();
    }

    private void RefreshSlotButtonUI(
        RunData runData,
        int slotIndex,
        Image weaponIconImage,
        TMP_Text weaponNameText)
    {
        WeaponData weaponData = GetWeaponData(runData, slotIndex);

        if (weaponNameText != null)
        {
            weaponNameText.text = weaponData != null
                ? weaponData.weaponName
                : "Empty";
        }

        if (weaponIconImage != null)
        {
            if (weaponData != null && weaponData.weaponSprite != null)
            {
                weaponIconImage.sprite = weaponData.weaponSprite;
                weaponIconImage.enabled = true;
            }
            else
            {
                weaponIconImage.sprite = null;
                weaponIconImage.enabled = false;
            }
        }
    }

    private WeaponData GetWeaponData(RunData runData, int slotIndex)
    {
        if (runData == null || runData.equippedWeapons == null)
            return null;

        if (slotIndex < 0 || slotIndex >= runData.equippedWeapons.Length)
            return null;

        WeaponLoadoutData loadout = runData.equippedWeapons[slotIndex];

        if (loadout == null)
            return null;

        if (!loadout.hasWeapon)
            return null;

        if (loadout.weaponData == null)
            return null;

        return loadout.weaponData;
    }
}