using System.Reflection;
using UnityEngine;

/// <summary>
/// 실제 UI 인벤토리 drag/drop 전에,
/// 현재 선택 무기에 부착물을 임시로 장착해서
/// 효과 적용 여부를 빠르게 확인하는 디버그 스크립트.
/// </summary>
public class WeaponAttachmentDebugTester : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private PlayerWeaponController weaponControllerBehaviour;

    [Header("Debug Keys")]
    [SerializeField] private KeyCode equipMuzzleKey = KeyCode.F5;
    [SerializeField] private KeyCode equipMagazineKey = KeyCode.F6;
    [SerializeField] private KeyCode equipScopeKey = KeyCode.F7;
    [SerializeField] private KeyCode clearAttachmentsKey = KeyCode.F8;
    [SerializeField] private KeyCode printWeaponStateKey = KeyCode.F9;

    [Header("Debug Attachment Data")]
    [SerializeField] private WeaponAttachmentData debugMuzzleAttachment = new WeaponAttachmentData();
    [SerializeField] private WeaponAttachmentData debugMagazineAttachment = new WeaponAttachmentData();
    [SerializeField] private WeaponAttachmentData debugScopeAttachment = new WeaponAttachmentData();

    private void Update()
    {
        if (Input.GetKeyDown(equipMuzzleKey))
        {
            TryEquipToCurrentWeapon(debugMuzzleAttachment);
        }

        if (Input.GetKeyDown(equipMagazineKey))
        {
            TryEquipToCurrentWeapon(debugMagazineAttachment);
        }

        if (Input.GetKeyDown(equipScopeKey))
        {
            TryEquipToCurrentWeapon(debugScopeAttachment);
        }

        if (Input.GetKeyDown(clearAttachmentsKey))
        {
            WeaponRuntime weapon = GetCurrentWeaponRuntime();

            if (weapon == null)
                return;

            weapon.ClearAttachments();
            Debug.Log($"[Attachment Debug] Cleared all attachments from [{weapon.WeaponName}].");
            Debug.Log(weapon.GetDebugSummary());
        }

        if (Input.GetKeyDown(printWeaponStateKey))
        {
            WeaponRuntime weapon = GetCurrentWeaponRuntime();

            if (weapon == null)
                return;

            Debug.Log(weapon.GetDebugSummary());
        }
    }

    private void TryEquipToCurrentWeapon(WeaponAttachmentData attachment)
    {
        WeaponRuntime weapon = GetCurrentWeaponRuntime();

        if (weapon == null)
            return;

        if (attachment == null)
        {
            Debug.LogWarning("[Attachment Debug] Attachment data is null.");
            return;
        }

        bool success = weapon.TryEquipAttachment(attachment);

        if (success)
        {
            Debug.Log(weapon.GetDebugSummary());
        }
    }

    /// <summary>
    /// 네 PlayerWeaponController 실제 API 이름이 다를 수 있어서
    /// reflection으로 흔한 이름들을 찾아온다.
    /// 
    /// 우선순위:
    /// 1) CurrentWeaponRuntime 프로퍼티
    /// 2) GetCurrentWeaponRuntime() 메서드
    /// 3) currentWeaponRuntime 필드
    /// 
    /// 네 코드에서 이름이 다르면 여기에 이름 하나만 추가하면 된다.
    /// </summary>
    private WeaponRuntime GetCurrentWeaponRuntime()
    {
        if (weaponControllerBehaviour == null)
        {
            Debug.LogWarning("[Attachment Debug] weaponControllerBehaviour is not assigned.");
            return null;
        }

        System.Type type = weaponControllerBehaviour.GetType();

        PropertyInfo property = type.GetProperty(
            "CurrentWeaponRuntime",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property != null)
        {
            object value = property.GetValue(weaponControllerBehaviour);
            return value as WeaponRuntime;
        }

        MethodInfo method = type.GetMethod(
            "GetCurrentWeaponRuntime",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (method != null)
        {
            object value = method.Invoke(weaponControllerBehaviour, null);
            return value as WeaponRuntime;
        }

        FieldInfo field = type.GetField(
            "currentWeaponRuntime",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field != null)
        {
            object value = field.GetValue(weaponControllerBehaviour);
            return value as WeaponRuntime;
        }

        Debug.LogWarning("[Attachment Debug] Could not find current weapon runtime accessor on weapon controller.");
        return null;
    }
}