using UnityEngine;

/// <summary>
/// 유닛이 들고 있는 무기 스프라이트를 표시하는 공용 비주얼 컨트롤러.
/// 
/// 핵심:
/// - WeaponData를 직접 받는 것이 아니라 WeaponRuntime을 받는다.
/// - WeaponRuntime.HasBaseData가 true일 때만 BaseData에서 스프라이트를 꺼낸다.
/// - 빈 슬롯이면 총기 스프라이트를 숨긴다.
/// </summary>
public class WeaponVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer gunSpriteRenderer;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private bool hideWhenNoWeapon = true;

    private WeaponRuntime currentWeaponRuntime;

    public WeaponRuntime CurrentWeaponRuntime => currentWeaponRuntime;

    private void Awake()
    {
        if (gunSpriteRenderer == null)
            gunSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// 현재 무기 런타임을 받아서 총기 스프라이트를 갱신한다.
    /// </summary>
    public void ApplyWeaponRuntime(WeaponRuntime weaponRuntime)
    {
        currentWeaponRuntime = weaponRuntime;

        if (gunSpriteRenderer == null)
        {
            Debug.LogWarning("[WeaponVisualController] Gun SpriteRenderer is missing.");
            return;
        }

        if (weaponRuntime == null || !weaponRuntime.HasBaseData)
        {
            ClearWeaponVisual();
            return;
        }

        WeaponData weaponData = weaponRuntime.BaseData;

        if (weaponData == null)
        {
            ClearWeaponVisual();
            return;
        }

        // 중요:
        // displaySprite가 네 WeaponData의 실제 스프라이트 필드명이 아니면,
        // 여기만 네 필드명으로 바꿔라.
        Sprite weaponSprite = weaponData.weaponSprite;

        if (weaponSprite != null)
        {
            gunSpriteRenderer.enabled = true;
            gunSpriteRenderer.sprite = weaponSprite;
        }
        else
        {
            ApplyFallback();
        }
    }

    /// <summary>
    /// 무기가 없을 때 총기 비주얼을 비운다.
    /// </summary>
    public void ClearWeaponVisual()
    {
        currentWeaponRuntime = null;

        if (gunSpriteRenderer == null)
            return;

        if (hideWhenNoWeapon)
        {
            gunSpriteRenderer.sprite = null;
            gunSpriteRenderer.enabled = false;
        }
        else
        {
            ApplyFallback();
        }
    }

    private void ApplyFallback()
    {
        if (gunSpriteRenderer == null)
            return;

        if (fallbackSprite != null)
        {
            gunSpriteRenderer.enabled = true;
            gunSpriteRenderer.sprite = fallbackSprite;
        }
        else
        {
            gunSpriteRenderer.sprite = null;
            gunSpriteRenderer.enabled = false;
        }
    }
}