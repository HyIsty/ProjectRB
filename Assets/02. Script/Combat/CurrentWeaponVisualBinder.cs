using UnityEngine;

/// <summary>
/// 플레이어의 현재 선택 무기 런타임을 WeaponVisualController에 반영하는 스크립트.
/// 
/// 장점:
/// - PlayerWeaponController 내부 코드를 많이 안 건드려도 된다.
/// - 현재 무기 슬롯이 바뀌면 다음 Update에서 자동으로 총 스프라이트가 바뀐다.
/// </summary>
public class CurrentWeaponVisualBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private WeaponVisualController weaponVisualController;

    private WeaponRuntime lastWeaponRuntime;

    private void Awake()
    {
        if (weaponController == null)
            weaponController = GetComponent<PlayerWeaponController>();

        if (weaponVisualController == null)
            weaponVisualController = GetComponentInChildren<WeaponVisualController>();
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void Update()
    {
        RefreshIfChanged();
    }

    /// <summary>
    /// 현재 무기 런타임이 바뀌었을 때만 비주얼을 갱신한다.
    /// </summary>
    private void RefreshIfChanged()
    {
        if (weaponController == null || weaponVisualController == null)
            return;

        WeaponRuntime currentRuntime = weaponController.GetCurrentWeaponRuntime();

        if (ReferenceEquals(currentRuntime, lastWeaponRuntime))
            return;

        lastWeaponRuntime = currentRuntime;
        weaponVisualController.ApplyWeaponRuntime(currentRuntime);
    }

    /// <summary>
    /// 강제로 현재 무기 비주얼을 다시 반영한다.
    /// 전투 시작 직후나 런타임 무기 세팅 직후 호출하면 좋다.
    /// </summary>
    public void ForceRefresh()
    {
        if (weaponController == null || weaponVisualController == null)
            return;

        WeaponRuntime currentRuntime = weaponController.GetCurrentWeaponRuntime();

        lastWeaponRuntime = currentRuntime;
        weaponVisualController.ApplyWeaponRuntime(currentRuntime);
    }
}