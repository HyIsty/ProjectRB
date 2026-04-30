using UnityEngine;

/// <summary>
/// 플레이어가 장착 중인 무기 슬롯을 관리하는 컨트롤러.
/// 현재 최소 구현 기준:
/// - 최대 무기 슬롯 2개
/// - 전투 중 무기 스왑은 AP 소모 없음
/// - 현재 활성 무기를 다른 시스템이 참조할 수 있게 제공
/// 
/// 이 스크립트는 "쏘는" 역할을 하지 않는다.
/// 단지 어떤 무기가 현재 선택되어 있는지와,
/// 각 슬롯에 어떤 WeaponRuntime이 들어있는지를 관리한다.
/// </summary>
public class PlayerWeaponController : MonoBehaviour
{
    [Header("Equipped Weapons")]
    [Tooltip("무기 슬롯 0번 (주 무기)")]
    [SerializeField] private WeaponRuntime primaryWeapon = new WeaponRuntime();

    [Tooltip("무기 슬롯 1번 (보조 무기)")]
    [SerializeField] private WeaponRuntime secondaryWeapon = new WeaponRuntime();

    [Header("Current State")]
    [Tooltip("현재 활성 무기 슬롯 인덱스. 0 또는 1")]
    [SerializeField] private int currentWeaponIndex = 0;

    [Header("Visual")]
    [SerializeField] private WeaponVisualController weaponVisualController;

    [Header("Reload")]
    [SerializeField] private int reloadApCost = 1;

    /// <summary>
    /// 현재 활성 슬롯 번호를 반환한다. (0 또는 1)
    /// </summary>
    public int CurrentWeaponIndex => currentWeaponIndex;

    /// <summary>
    /// 슬롯 0의 WeaponRuntime
    /// </summary>
    public WeaponRuntime PrimaryWeapon => primaryWeapon;

    /// <summary>
    /// 슬롯 1의 WeaponRuntime
    /// </summary>
    public WeaponRuntime SecondaryWeapon => secondaryWeapon;

    /// <summary>
    /// 현재 활성 무기의 WeaponRuntime을 반환한다.
    /// 슬롯이 비어 있거나 baseData가 없을 수도 있으므로
    /// 사용할 때 HasCurrentWeapon도 함께 체크하는 것이 안전하다.
    /// </summary>
    public WeaponRuntime CurrentWeaponRuntime
    {
        get
        {
            return GetWeaponRuntimeByIndex(currentWeaponIndex);
        }
    }

    public bool HasAnyWeapon()
    {
        return HasWeaponInSlot(0) || HasWeaponInSlot(1);
    }

    /// <summary>
    /// 반대편 슬롯 무기를 반환한다.
    /// 예: 현재 0번이면 1번 반환
    /// </summary>
    public WeaponRuntime OtherWeaponRuntime
    {
        get
        {
            int otherIndex = currentWeaponIndex == 0 ? 1 : 0;
            return GetWeaponRuntimeByIndex(otherIndex);
        }
    }

    /// <summary>
    /// 반대편 슬롯에도 유효한 무기가 있는지 여부
    /// </summary>
    public bool HasOtherWeapon
    {
        get
        {
            WeaponRuntime other = OtherWeaponRuntime;
            return other != null && other.HasBaseData;
        }
    }

    private void Awake()
    {
        EnsureRuntimeObjectsExist();
        ClampCurrentWeaponIndex();
    }

    private void OnValidate()
    {
        EnsureRuntimeObjectsExist();
        ClampCurrentWeaponIndex();
    }

    /// <summary>
    /// 슬롯 인덱스(0 또는 1)에 맞는 WeaponRuntime을 반환한다.
    /// 잘못된 인덱스가 들어오면 null을 반환한다.
    /// </summary>
    public WeaponRuntime GetWeaponRuntimeByIndex(int index)
    {
        switch (index)
        {
            case 0:
                return primaryWeapon;

            case 1:
                return secondaryWeapon;

            default:
                return null;
        }
    }

    /// <summary>
    /// 지정 슬롯에 유효한 무기(baseData가 있는 무기)가 있는지 확인한다.
    /// </summary>
    public bool HasWeaponInSlot(int slotIndex)
    {
        WeaponRuntime runtime = GetWeaponRuntimeBySlot(slotIndex);
        return runtime != null && runtime.HasBaseData;
    }

    public bool HasCurrentWeapon()
    {
        return CurrentWeaponRuntime != null && CurrentWeaponRuntime.HasBaseData;
    }

    public WeaponRuntime GetWeaponRuntimeBySlot(int slotIndex)
    {
        return slotIndex switch
        {
            0 => primaryWeapon,
            1 => secondaryWeapon,
            _ => null
        };
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        return !HasWeaponInSlot(slotIndex);
    }

    public bool TrySwitchWeapon(int targetSlotIndex)
    {
        if (!IsValidWeaponSlotIndex(targetSlotIndex))
            return false;

        if (!HasWeaponInSlot(targetSlotIndex))
            return false;

        currentWeaponIndex = targetSlotIndex;
        return true;
    }

    public bool TrySwitchToOtherWeapon()
    {
        int otherIndex = currentWeaponIndex == 0 ? 1 : 0;
        return TrySwitchWeapon(otherIndex);
    }

    /// <summary>
    /// 지정 슬롯에 새 WeaponData를 세팅한다.
    /// keepLoadedAmmo가 false면 기존 장전 탄은 비워진다.
    /// 
    /// 예:
    /// - 새 무기 장착: keepLoadedAmmo = false 추천
    /// - 같은 무기 데이터만 갱신: 상황에 따라 true 가능
    /// </summary>
    public bool SetWeaponBaseData(int slotIndex, WeaponData newWeaponData)
    {
        WeaponRuntime runtime = GetWeaponRuntimeByIndex(slotIndex);

        if (runtime == null)
            return false;

        runtime.SetBaseData(newWeaponData);
        return true;
    }

    /// <summary>
    /// 지정 슬롯 무기를 비운다.
    /// 현재는 WeaponRuntime 자체를 새로 갈아끼우는 대신,
    /// baseData를 null로 만들고 장전 탄을 비우는 방식으로 처리한다.
    /// </summary>
    public bool ClearWeaponSlot(int slotIndex)
    {
        WeaponRuntime runtime = GetWeaponRuntimeByIndex(slotIndex);

        if (runtime == null)
            return false;

        runtime.SetBaseData(null);
        runtime.ClearAttachments();

        // 현재 슬롯 무기를 지웠는데,
        // 남아 있는 다른 슬롯에 무기가 있다면 그쪽으로 자동 전환할 수 있다.
        // minimum 구현에서는 너무 많은 자동 규칙을 넣지 않고,
        // 현재 인덱스가 빈 슬롯을 가리킬 수도 있게 둔다.
        return true;
    }

    public void FallbackToValidWeaponSlot()
    {
        if (HasWeaponInSlot(0))
        {
            currentWeaponIndex = 0;
            return;
        }

        if (HasWeaponInSlot(1))
        {
            currentWeaponIndex = 1;
            return;
        }

        currentWeaponIndex = 0;
    }

    /// <summary>
    /// 현재 활성 무기의 런타임을 바로 반환한다.
    /// 외부에서 좀 더 읽기 편하게 만든 편의 함수.
    /// </summary>
    public WeaponRuntime GetCurrentWeaponRuntime()
    {
        return CurrentWeaponRuntime;
    }

    /// <summary>
    /// 현재 활성 무기의 WeaponData를 반환한다.
    /// 현재 무기가 없으면 null 반환.
    /// </summary>
    public WeaponData GetCurrentWeaponData()
    {
        WeaponRuntime current = CurrentWeaponRuntime;

        if (current == null || !current.HasBaseData)
            return null;

        return current.BaseData;
    }

    /// <summary>
    /// 현재 활성 무기의 타입을 반환한다.
    /// 현재 무기가 없으면 기본값 반환.
    /// </summary>
    public WeaponType GetCurrentWeaponType()
    {
        WeaponRuntime current = CurrentWeaponRuntime;

        if (current == null || !current.HasBaseData)
            return default;

        return current.WeaponType;
    }

    /// <summary>
    /// 디버그용으로 현재 활성 무기 이름을 반환한다.
    /// </summary>
    public string GetCurrentWeaponName()
    {
        WeaponRuntime current = CurrentWeaponRuntime;

        if (current == null || !current.HasBaseData)
            return "(No Weapon)";

        return current.WeaponName;
    }

    /// <summary>
    /// 현재 활성 무기에 탄이 장전되어 있는지 확인한다.
    /// </summary>
    public bool CurrentWeaponHasLoadedAmmo()
    {
        WeaponRuntime current = CurrentWeaponRuntime;

        if (current == null || !current.HasBaseData)
            return false;

        return current.HasLoadedAmmo;
    }

    /// <summary>
    /// 슬롯 인덱스가 유효한지 확인한다.
    /// 현재 최대 슬롯은 2개이므로 0 또는 1만 허용.
    /// </summary>
    private bool IsValidWeaponSlotIndex(int index)
    {
        return index == 0 || index == 1;
    }

    /// <summary>
    /// currentWeaponIndex가 0~1 범위를 벗어나지 않게 정리한다.
    /// </summary>
    private void ClampCurrentWeaponIndex()
    {
        if (currentWeaponIndex < 0)
            currentWeaponIndex = 0;

        if (currentWeaponIndex > 1)
            currentWeaponIndex = 1;
    }

    /// <summary>
    /// 직렬화된 WeaponRuntime 참조가 비어 있으면 새로 생성한다.
    /// Inspector에서 null이 들어가 있더라도 최소한의 형태를 유지하게 한다.
    /// </summary>
    private void EnsureRuntimeObjectsExist()
    {
        if (primaryWeapon == null)
            primaryWeapon = new WeaponRuntime();

        if (secondaryWeapon == null)
            secondaryWeapon = new WeaponRuntime();
    }

    public bool SetWeaponRuntime(int slotIndex, WeaponRuntime runtime)
    {
        if (!IsValidWeaponSlotIndex(slotIndex))
            return false;

        if (runtime == null)
            runtime = new WeaponRuntime();

        switch (slotIndex)
        {
            case 0:
                primaryWeapon = runtime;
                break;

            case 1:
                secondaryWeapon = runtime;
                break;
        }

        return true;
    }

    public bool SetCurrentWeaponIndex(int index)
    {
        if (!IsValidWeaponSlotIndex(index))
            return false;

        currentWeaponIndex = index;
        return true;
    }

    public void ClearLoadedAmmoAll()
    {
        if (primaryWeapon != null)
            primaryWeapon.ClearLoadedAmmo();

        if (secondaryWeapon != null)
            secondaryWeapon.ClearLoadedAmmo();
    }

    public bool TryReloadCurrentWeapon(AmmoDeckRuntime deckRuntime)
    {
        if (deckRuntime == null)
            return false;

        WeaponRuntime currentWeapon = CurrentWeaponRuntime;
        if (currentWeapon == null || !currentWeapon.HasBaseData)
            return false;

        if (!currentWeapon.CanReload())
            return false;

        if (TurnManager.Instance == null)
            return false;

        if (!TurnManager.Instance.IsPlayerTurn)
            return false;

        if (!TurnManager.Instance.TrySpendPlayerAP(reloadApCost))
            return false;

        bool loadedAny = false;

        while (currentWeapon.CanReload())
        {
            AmmoModuleData drawnAmmo = deckRuntime.DrawOne();
            if (drawnAmmo == null)
                break;

            if (currentWeapon.TryLoadAmmo(drawnAmmo))
            {
                loadedAny = true;
            }
            else
            {
                deckRuntime.Discard(drawnAmmo);
                break;
            }
        }
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayReloadSuccess();
        return loadedAny;
    }
}