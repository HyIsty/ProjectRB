using UnityEngine;

/// <summary>
/// 적 유닛의 고정 무기 스프라이트를 시작 시 적용한다.
/// 적은 현재 플레이어처럼 무기 슬롯을 바꾸지 않으므로 단순하게 한 번만 적용한다.
/// </summary>
public class EnemyWeaponVisualBinder : MonoBehaviour
{
    [Header("Enemy Weapon")]
    [SerializeField] private WeaponData enemyWeaponData;

    [Header("Visual")]
    [SerializeField] private WeaponVisualController weaponVisualController;

    private void Awake()
    {
        if (weaponVisualController == null)
            weaponVisualController = GetComponentInChildren<WeaponVisualController>();
    }

    private void Start()
    {
        if (weaponVisualController == null)
        {
            Debug.LogWarning("[EnemyWeaponVisualBinder] WeaponVisualController is missing.");
            return;
        }

        WeaponRuntime enemyWeaponRuntime = new WeaponRuntime(enemyWeaponData);

        WeaponVisualController visual = GetComponentInChildren<WeaponVisualController>();

        if (visual != null)
            visual.ApplyWeaponRuntime(enemyWeaponRuntime);

        weaponVisualController.ApplyWeaponRuntime(enemyWeaponRuntime);
    }
}