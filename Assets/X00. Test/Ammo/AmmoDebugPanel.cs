using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 탄환/무기 시스템 디버그 UI.
/// 
/// 현재 표시 대상:
/// - AmmoDeckRuntime의 Draw / Discard 개수
/// - PlayerWeaponController의 현재 무기 / 다른 무기 상태
/// - 각 무기의 장전 탄 목록
/// - 현재 무기의 다음 발 탄환
/// 
/// 목적은 "예쁜 UI"가 아니라 "검증 가능한 UI"다.
/// </summary>
public class AmmoDebugPanel : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private AmmoDeckRuntime ammoDeck;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private TMP_Text debugText;

    [Header("Refresh Option")]
    [SerializeField] private bool refreshEveryFrame = true;

    private readonly StringBuilder sb = new StringBuilder(2048);

    private void Start()
    {
        // 먼저 참조를 찾아놓고,
        // 그 다음에 텍스트를 갱신하는 게 맞다.
        if (ammoDeck == null)
            ammoDeck = FindFirstObjectByType<AmmoDeckRuntime>();

        if (weaponController == null)
            weaponController = FindFirstObjectByType<PlayerWeaponController>();

        if (ammoDeck == null || weaponController == null)
        {
            Debug.LogWarning("[AmmoDebugPanel] AmmoDeckRuntime 혹은 PlayerWeaponController의 참조가 불가능합니다.");
        }

        RefreshText();
    }

    private void Update()
    {
        if (refreshEveryFrame)
        {
            RefreshText();
        }
    }

    /// <summary>
    /// 현재 상태를 TMP_Text에 반영한다.
    /// </summary>
    [ContextMenu("Refresh Debug Text")]
    public void RefreshText()
    {
        if (debugText == null)
            return;

        if (ammoDeck == null || weaponController == null)
        {
            debugText.text = "Ammo Debug Panel\nReference Missing";
            return;
        }

        sb.Clear();

        sb.AppendLine("=== Ammo / Weapon Debug ===");
        sb.AppendLine();

        AppendDeckInfo();
        sb.AppendLine();

        AppendWeaponInfo(
            title: "Current Weapon",
            weapon: weaponController.CurrentWeaponRuntime,
            slotIndex: weaponController.CurrentWeaponIndex
        );

        sb.AppendLine();

        int otherSlotIndex = weaponController.CurrentWeaponIndex == 0 ? 1 : 0;
        AppendWeaponInfo(
            title: "Other Weapon",
            weapon: weaponController.OtherWeaponRuntime,
            slotIndex: otherSlotIndex
        );

        debugText.text = sb.ToString();
    }

    private void AppendDeckInfo()
    {
        sb.AppendLine("[Deck]");
        sb.AppendLine($"Draw: {ammoDeck.DrawCount}");
        sb.AppendLine($"Discard: {ammoDeck.DiscardCount}");
    }

    private void AppendWeaponInfo(string title, WeaponRuntime weapon, int slotIndex)
    {
        sb.AppendLine($"[{title}]");
        sb.AppendLine($"Slot Index: {slotIndex}");

        if (weapon == null || !weapon.HasBaseData)
        {
            sb.AppendLine("No Weapon");
            return;
        }

        sb.AppendLine($"Name: {weapon.WeaponName}");
        sb.AppendLine($"Type: {weapon.WeaponType}");
        sb.AppendLine($"AP Cost: {weapon.CurrentApCost}");
        sb.AppendLine($"Loaded: {weapon.LoadedAmmoCount}/{weapon.CurrentSlotCapacity}");
        sb.AppendLine($"Projectiles/Attack: {weapon.CurrentProjectilesPerAttack}");
        sb.AppendLine($"Damage Multiplier: {weapon.CurrentWeaponDamageMultiplier:0.##}");
        sb.AppendLine($"Aim Spread: {weapon.CurrentAimSpread:0.##}");
        sb.AppendLine($"Optimal Range Max: {weapon.CurrentOptimalRangeMax}");
        sb.AppendLine($"Max Range: {weapon.CurrentMaxRange}");
        sb.AppendLine($"Optimal Damage x: {weapon.CurrentOptimalDamageMultiplier:0.##}");
        sb.AppendLine($"Far Damage x: {weapon.CurrentFarDamageMultiplier:0.##}");

        AmmoModuleData nextAmmo = weapon.PeekNextAmmo();
        string nextShot = "Empty";

        if (nextAmmo != null)
        {
            nextShot = $"{nextAmmo.displayName} (+{nextAmmo.damage})";
        }

        sb.AppendLine($"Next Shot: {nextShot}");

        AppendLoadedAmmoList(weapon);
    }

    private void AppendLoadedAmmoList(WeaponRuntime weapon)
    {
        sb.AppendLine("Magazine:");

        if (weapon.LoadedAmmo == null || weapon.LoadedAmmo.Count == 0)
        {
            sb.AppendLine(" - (empty)");
            return;
        }

        for (int i = 0; i < weapon.LoadedAmmo.Count; i++)
        {
            AmmoModuleData ammo = weapon.LoadedAmmo[i];

            if (ammo == null)
            {
                sb.AppendLine($" - [{i}] NULL");
                continue;
            }

            sb.AppendLine($" - [{i}] {ammo.displayName} (+{ammo.damage})");
        }
    }
}