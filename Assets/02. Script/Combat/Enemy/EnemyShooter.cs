using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Enemy 전용 Shooter.
/// - 입력 처리 없음
/// - EnemyAIController가 직접 호출
/// - Enemy는 AmmoDeckRuntime을 쓰지 않음
/// - enemyAmmoData 1종을 재장전 시 슬롯 수만큼 채움
/// - 발사 시 loaded ammo 1개 소비
/// - discard pile 없음
/// </summary>
[RequireComponent(typeof(UnitStatusController))]
public class EnemyShooter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private UnitStatusController statusController;

    [Header("Enemy Loadout")]
    [SerializeField] private WeaponData enemyWeaponData;
    [SerializeField] private AmmoModuleData enemyAmmoData;

    [Header("Raycast")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask targetMask;

    [Header("Optional Visuals")]
    [SerializeField] private ShotTracerFactory shotTracerFactory;

    [Header("Spread / Fire Pattern")]
    [SerializeField] private float shotgunExtraSpreadMultiplier = 1.7f;
    [SerializeField] private float rifleExtraSpreadMultiplier = 0.75f;

    [Header("Debug")]
    [SerializeField] private bool debugLog;
    [SerializeField] private WeaponRuntime runtimeWeapon;

    private bool firedEffectThisAttack;

    public bool HasUsableWeapon => runtimeWeapon != null && GetWeaponHasBaseData(runtimeWeapon);
    public bool HasLoadedAmmo => GetLoadedAmmoCount(runtimeWeapon) > 0;
    public bool NeedsReload => HasUsableWeapon && GetLoadedAmmoCount(runtimeWeapon) < GetCurrentSlotCapacity(runtimeWeapon);
    public int CurrentShootApCost => HasUsableWeapon ? GetCurrentApCost(runtimeWeapon) : int.MaxValue;

    private Vector3 ShootOrigin => shootOrigin != null ? shootOrigin.position : transform.position;

    private void Awake()
    {
        if (statusController == null)
            statusController = GetComponent<UnitStatusController>();
        if(shotTracerFactory == null)
            shotTracerFactory = FindFirstObjectByType<ShotTracerFactory>();

        if (enemyWeaponData != null)
        {
            runtimeWeapon = new WeaponRuntime(enemyWeaponData);
        }
    }

    /// <summary>
    /// "위치상" target을 위협할 수 있는지.
    /// AP / loaded ammo와 별개로 LOS + range만 본다.
    /// </summary>
    public bool CanThreatenTargetPosition(Transform target)
    {
        if (!HasUsableWeapon || target == null)
            return false;

        Vector3 from = ShootOrigin;
        Vector3 to = target.position;

        if (!HasLineOfSight(from, to))
            return false;

        return IsTargetInShootRangeFromWorld(from, to);
    }

    /// <summary>
    /// 특정 월드 위치에서 targetWorld가 사거리 안인지 검사
    /// 이동 점수 계산용
    /// </summary>
    public bool IsTargetInShootRangeFromWorld(Vector3 fromWorld, Vector3 targetWorld)
    {
        if (!HasUsableWeapon)
            return false;

        float distance = Vector2.Distance(fromWorld, targetWorld);
        ShotRangeBand band = EvaluateRangeBand(distance, runtimeWeapon);

        return band != ShotRangeBand.OutOfRange;
    }

    public bool CanShootTarget(Transform target)
    {
        if (!HasUsableWeapon || target == null)
            return false;

        if (statusController != null && !statusController.CanShoot)
            return false;

        if (!HasLoadedAmmo)
            return false;

        return CanThreatenTargetPosition(target);
    }

    public bool TryShootTarget(Transform target)
    {
        if (!CanShootTarget(target))
            return false;

        if (!TryConsumeNextAmmo(runtimeWeapon, out AmmoModuleData usedAmmo) || usedAmmo == null)
            return false;

        firedEffectThisAttack = false;

        WeaponType weaponType = GetWeaponType(runtimeWeapon);
        int projectiles = Mathf.Max(1, GetCurrentProjectilesPerAttack(runtimeWeapon));

        switch (weaponType)
        {
            case WeaponType.Shotgun:
                {
                    for (int i = 0; i < projectiles; i++)
                    {
                        FireSingleProjectile(target, usedAmmo, shotgunExtraSpreadMultiplier);
                    }
                    break;
                }

            case WeaponType.Rifle:
                {
                    // 현재 minimum-safe prototype:
                    // 한 액션 안에서 빠르게 연속 판정
                    for (int i = 0; i < projectiles; i++)
                    {
                        FireSingleProjectile(target, usedAmmo, rifleExtraSpreadMultiplier);
                    }
                    break;
                }

            case WeaponType.Sniper:
            case WeaponType.Pistol:
            default:
                {
                    FireSingleProjectile(target, usedAmmo, 1f);
                    break;
                }
        }

        // Enemy는 discard pile 없음.
        // consumed ammo는 그냥 끝.
        if (debugLog)
        {
            Debug.Log($"[{name}] Enemy shot with {GetAmmoDisplayName(usedAmmo)}");
        }
        PlayEnemyGunSfx(runtimeWeapon);
        return true;
    }

    public bool TryReload()
    {
        if (!HasUsableWeapon)
            return false;

        if (enemyAmmoData == null)
            return false;

        int capacity = GetCurrentSlotCapacity(runtimeWeapon);
        int loadedCount = GetLoadedAmmoCount(runtimeWeapon);

        bool loadedAny = false;

        while (loadedCount < capacity)
        {
            bool loaded = TryLoadAmmo(runtimeWeapon, enemyAmmoData);
            if (!loaded)
                break;

            loadedCount++;
            loadedAny = true;
        }

        if (debugLog && loadedAny)
        {
            Debug.Log($"[{name}] Enemy reloaded. Loaded count = {GetLoadedAmmoCount(runtimeWeapon)}");
        }

        return loadedAny;
    }

    private void FireSingleProjectile(Transform explicitTarget, AmmoModuleData usedAmmo, float extraSpreadMultiplier)
    {
        Vector3 origin = ShootOrigin;
        Vector3 targetPoint = explicitTarget != null ? explicitTarget.position : origin + transform.right * 2f;
        Vector2 baseDirection = (targetPoint - origin).normalized;

        float spreadDegrees = GetCurrentAimSpread(runtimeWeapon) * extraSpreadMultiplier;
        Vector2 shotDirection = ApplySpread(baseDirection, spreadDegrees);

        float maxDistance = GetCurrentMaxRange(runtimeWeapon);

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, shotDirection, maxDistance, obstacleMask | targetMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector3 tracerEnd = origin + (Vector3)(shotDirection * maxDistance);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null)
                continue;

            tracerEnd = hit.point;

            // 장애물이 먼저 맞으면 막힘
            if (IsInLayerMask(hit.collider.gameObject.layer, obstacleMask))
            {
                if (debugLog)
                    Debug.Log($"[{name}] Shot blocked by obstacle.");
                break;
            }

            UnitHealthController targetHealth = hit.collider.GetComponentInParent<UnitHealthController>();
            if (targetHealth == null)
                continue;

            float distance = hit.distance;
            ShotRangeBand rangeBand = EvaluateRangeBand(distance, runtimeWeapon);

            if (rangeBand == ShotRangeBand.OutOfRange)
                break;

            ShotCalculationContext shotContext = BuildShotCalculationContext(usedAmmo, runtimeWeapon, distance, rangeBand);
            int finalDamage = shotContext.FinalDamage;

            int hpBefore = targetHealth.CurrentHP;

            ApplyDamage(targetHealth, finalDamage, rangeBand);

            int hpAfter = targetHealth.CurrentHP;
            bool didHit = true;
            bool didKill = hpAfter <= 0 && hpBefore > 0;

            // 멀티 projectile 공격에서도 ammo effect는 첫 성공 히트 1회만
            if (!firedEffectThisAttack)
            {
                DispatchAmmoEffect(
                    attacker: gameObject,
                    target: targetHealth.gameObject,
                    usedRound: usedAmmo,
                    finalDamage: finalDamage,
                    didHit: didHit,
                    didKill: didKill,
                    rangeBand: rangeBand
                );

                firedEffectThisAttack = true;
            }
            Debug.Log(
    $"[{name}] Enemy Damage Debug -> " +
    $"AmmoBaseDamage={GetAmmoBaseDamage(usedAmmo)}, " +
    $"WeaponMultiplier={GetCurrentWeaponDamageMultiplier(runtimeWeapon)}, " +
    $"RangeBand={rangeBand}, " +
    $"RangeMultiplier={GetRangeMultiplier(runtimeWeapon, rangeBand)}, " +
    $"FinalDamage={finalDamage}"
);

            if (debugLog)
            {
                Debug.Log($"[{name}] Hit {targetHealth.name} | Damage = {finalDamage} | Band = {rangeBand}");
            }

            break;
        }

        SpawnTracer(origin, tracerEnd);
    }

    // ---------------------------------------------------------------------
    // Shot Calculation
    // ---------------------------------------------------------------------

    private ShotCalculationContext BuildShotCalculationContext(
        AmmoModuleData usedAmmo,
        WeaponRuntime weapon,
        float distance,
        ShotRangeBand rangeBand)
    {
        ShotCalculationContext ctx = new ShotCalculationContext();

        ctx.UsedAmmo = usedAmmo;
        ctx.Weapon = weapon;
        ctx.Distance = distance;
        ctx.RangeBand = rangeBand;

        // 1) ammo base damage
        ctx.AmmoBaseDamage = GetAmmoBaseDamage(usedAmmo);

        // 2) always-on weapon stats
        ctx.WeaponMultiplier = GetCurrentWeaponDamageMultiplier(weapon);
        ctx.RangeMultiplier = GetRangeMultiplier(weapon, rangeBand);

        ctx.FinalDamageMultiplier = 1f;
        ctx.FinalFlatDamageAdd = 0;

        // 3) 현재 Enemy는 고정 탄종 + 단순 운용이므로
        // conditional / final modifier는 확장 포인트만 둔다
        ApplyAmmoConditionalModifiers(ref ctx);

        float raw = (ctx.AmmoBaseDamage + ctx.ConditionalAmmoDamageAdd)
                    * (ctx.WeaponMultiplier + ctx.ConditionalWeaponMultiplierAdd)
                    * ctx.RangeMultiplier;

        ctx.RawDamage = raw;

        ApplyFinalDamageModifiers(ref ctx);

        float finalFloat = (ctx.RawDamage + ctx.FinalFlatDamageAdd) * ctx.FinalDamageMultiplier;
        ctx.FinalDamage = Mathf.Max(0, Mathf.RoundToInt(finalFloat));
        Debug.Log(
    $"[{name}] Final Mod Debug -> " +
    $"Raw={ctx.RawDamage}, " +
    $"FinalFlat={ctx.FinalFlatDamageAdd}, " +
    $"FinalMul={ctx.FinalDamageMultiplier}"
);
        return ctx;
    }

    private void ApplyAmmoConditionalModifiers(ref ShotCalculationContext ctx)
    {
        // 현재 no-op.
        // 나중에 적 전용 특수 modifier 붙이고 싶으면 여기 확장
    }

    private void ApplyFinalDamageModifiers(ref ShotCalculationContext ctx)
    {
        // 현재 no-op.
    }

    // ---------------------------------------------------------------------
    // Effect Dispatch
    // ---------------------------------------------------------------------

    private void DispatchAmmoEffect(
        GameObject attacker,
        GameObject target,
        AmmoModuleData usedRound,
        int finalDamage,
        bool didHit,
        bool didKill,
        ShotRangeBand rangeBand)
    {
        if (usedRound == null)
            return;

        string effectId = GetAmmoEffectId(usedRound);
        if (string.IsNullOrWhiteSpace(effectId))
            return;

        if (CardActionManager.Instance == null)
            return;

        CardActionContext context = new CardActionContext(attacker, target,  usedRound, finalDamage, didHit, didKill, rangeBand);

        CardActionManager.Instance.ExecuteAmmoEffect(context);
    }

    // ---------------------------------------------------------------------
    // Damage / Tracer / LOS
    // ---------------------------------------------------------------------

    private void ApplyDamage(UnitHealthController targetHealth, int damage, ShotRangeBand rangeBand)
    {
        targetHealth.TakeDamage(damage, rangeBand);
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (shotTracerFactory == null)
            return;

        shotTracerFactory.SpawnTracer(start, end);
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        RaycastHit2D hit = Physics2D.Linecast(from, to, obstacleMask);
        return hit.collider == null;
    }

    private Vector2 ApplySpread(Vector2 baseDirection, float spreadDegrees)
    {
        if (spreadDegrees <= 0.001f)
            return baseDirection.normalized;

        float randomAngle = UnityEngine.Random.Range(-spreadDegrees, spreadDegrees);
        Quaternion rot = Quaternion.Euler(0f, 0f, randomAngle);
        Vector2 result = rot * baseDirection;

        return result.normalized;
    }

    private ShotRangeBand EvaluateRangeBand(float distance, WeaponRuntime weapon)
    {
        if (distance <= GetCurrentOptimalRangeMax(weapon))
            return ShotRangeBand.Optimal;

        if (distance <= GetCurrentMaxRange(weapon))
            return ShotRangeBand.Far;

        return ShotRangeBand.OutOfRange;
    }

    private float GetRangeMultiplier(WeaponRuntime weapon, ShotRangeBand band)
    {
        switch (band)
        {
            case ShotRangeBand.Optimal:
                return GetCurrentOptimalDamageMultiplier(weapon);

            case ShotRangeBand.Far:
                return GetCurrentFarDamageMultiplier(weapon);

            default:
                return 0f;
        }
    }

    // ---------------------------------------------------------------------
    // Runtime Helpers
    // ---------------------------------------------------------------------

    private WeaponRuntime CreateWeaponRuntime(WeaponData weaponData)
    {
        try
        {
            return new WeaponRuntime(weaponData);
        }
        catch
        {
            WeaponRuntime runtime = new WeaponRuntime();

            FieldInfo baseDataField = typeof(WeaponRuntime).GetField("baseData", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (baseDataField != null)
                baseDataField.SetValue(runtime, weaponData);

            return runtime;
        }
    }

    private bool TryConsumeNextAmmo(WeaponRuntime weapon, out AmmoModuleData usedAmmo)
    {
        usedAmmo = null;
        if (weapon == null)
            return false;

        MethodInfo method = typeof(WeaponRuntime).GetMethod("TryConsumeNextAmmo", BindingFlags.Instance | BindingFlags.Public);
        if (method != null)
        {
            object[] args = { null };
            bool ok = (bool)method.Invoke(weapon, args);
            usedAmmo = args[0] as AmmoModuleData;
            return ok;
        }

        return false;
    }

    private bool TryLoadAmmo(WeaponRuntime weapon, AmmoModuleData ammo)
    {
        if (weapon == null || ammo == null)
            return false;

        MethodInfo tryLoad = typeof(WeaponRuntime).GetMethod("TryLoadAmmo", BindingFlags.Instance | BindingFlags.Public);
        if (tryLoad != null)
        {
            object result = tryLoad.Invoke(weapon, new object[] { ammo });
            if (result is bool boolResult)
                return boolResult;
        }

        MethodInfo load = typeof(WeaponRuntime).GetMethod("LoadAmmo", BindingFlags.Instance | BindingFlags.Public);
        if (load != null)
        {
            load.Invoke(weapon, new object[] { ammo });
            return true;
        }

        FieldInfo loadedAmmoField = typeof(WeaponRuntime).GetField("loadedAmmo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (loadedAmmoField != null)
        {
            List<AmmoModuleData> list = loadedAmmoField.GetValue(weapon) as List<AmmoModuleData>;
            if (list != null && list.Count < GetCurrentSlotCapacity(weapon))
            {
                // Enemy는 고정 탄종 1개를 반복 사용
                list.Add(ammo);
                return true;
            }
        }

        return false;
    }

    // ---------------------------------------------------------------------
    // Data Readers
    // ---------------------------------------------------------------------

    private bool GetWeaponHasBaseData(WeaponRuntime weapon)
    {
        if (weapon == null)
            return false;

        PropertyInfo prop = typeof(WeaponRuntime).GetProperty("HasBaseData", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.PropertyType == typeof(bool))
            return (bool)prop.GetValue(weapon);

        FieldInfo baseDataField = typeof(WeaponRuntime).GetField("baseData", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return baseDataField != null && baseDataField.GetValue(weapon) != null;
    }

    private int GetLoadedAmmoCount(WeaponRuntime weapon)
    {
        if (weapon == null)
            return 0;

        FieldInfo loadedAmmoField = typeof(WeaponRuntime).GetField("loadedAmmo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (loadedAmmoField != null)
        {
            if (loadedAmmoField.GetValue(weapon) is IList list)
                return list.Count;
        }

        PropertyInfo countProp = typeof(WeaponRuntime).GetProperty("LoadedAmmoCount", BindingFlags.Instance | BindingFlags.Public);
        if (countProp != null)
            return Convert.ToInt32(countProp.GetValue(weapon));

        return 0;
    }

    private WeaponType GetWeaponType(WeaponRuntime weapon) => GetRuntimeValue<WeaponType>(weapon, "WeaponType");
    private int GetCurrentApCost(WeaponRuntime weapon) => GetRuntimeValue<int>(weapon, "CurrentApCost");
    private int GetCurrentSlotCapacity(WeaponRuntime weapon) => GetRuntimeValue<int>(weapon, "CurrentSlotCapacity");
    private float GetCurrentWeaponDamageMultiplier(WeaponRuntime weapon) => GetRuntimeValue<float>(weapon, "CurrentWeaponDamageMultiplier");
    private float GetCurrentAimSpread(WeaponRuntime weapon) => GetRuntimeValue<float>(weapon, "CurrentAimSpread");
    private int GetCurrentProjectilesPerAttack(WeaponRuntime weapon) => GetRuntimeValue<int>(weapon, "CurrentProjectilesPerAttack");
    private float GetCurrentOptimalRangeMax(WeaponRuntime weapon) => GetRuntimeValue<float>(weapon, "CurrentOptimalRangeMax");
    private float GetCurrentMaxRange(WeaponRuntime weapon) => GetRuntimeValue<float>(weapon, "CurrentMaxRange");
    private float GetCurrentOptimalDamageMultiplier(WeaponRuntime weapon) => GetRuntimeValue<float>(weapon, "CurrentOptimalDamageMultiplier");
    private float GetCurrentFarDamageMultiplier(WeaponRuntime weapon) => GetRuntimeValue<float>(weapon, "CurrentFarDamageMultiplier");

    private T GetRuntimeValue<T>(WeaponRuntime weapon, string propertyName)
    {
        if (weapon == null)
            return default;

        PropertyInfo prop = typeof(WeaponRuntime).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (prop != null)
        {
            object value = prop.GetValue(weapon);
            if (value is T typed)
                return typed;
        }

        return default;
    }

    private int GetAmmoBaseDamage(AmmoModuleData ammo)
    {
        if (ammo == null)
            return 0;

        FieldInfo field = typeof(AmmoModuleData).GetField("damage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            return Convert.ToInt32(field.GetValue(ammo));

        PropertyInfo prop = typeof(AmmoModuleData).GetProperty("damage", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null)
            return Convert.ToInt32(prop.GetValue(ammo));

        return 0;
    }

    private string GetAmmoEffectId(AmmoModuleData ammo)
    {
        if (ammo == null)
            return string.Empty;

        FieldInfo field = typeof(AmmoModuleData).GetField("effectId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            return field.GetValue(ammo) as string ?? string.Empty;

        PropertyInfo prop = typeof(AmmoModuleData).GetProperty("effectId", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null)
            return prop.GetValue(ammo) as string ?? string.Empty;

        return string.Empty;
    }

    private string GetAmmoDisplayName(AmmoModuleData ammo)
    {
        if (ammo == null)
            return "(null ammo)";

        FieldInfo nameField = typeof(AmmoModuleData).GetField("ammoName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (nameField != null)
            return nameField.GetValue(ammo) as string ?? ammo.displayName;

        return ammo.displayName;
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask.value) != 0;
    }

    private void PlayEnemyGunSfx(WeaponRuntime weapon)
    {
        if (weapon == null || !weapon.HasBaseData)
            return;

        if (SoundManager.Instance == null)
            return;

        switch (weapon.WeaponType)
        {
            case WeaponType.Pistol:
                SoundManager.Instance.PlayPistolShot();
                break;

            case WeaponType.Shotgun:
                SoundManager.Instance.PlayShotgunShot();
                break;

            case WeaponType.Rifle:
                SoundManager.Instance.PlayRifleShot();
                break;

            case WeaponType.Sniper:
                SoundManager.Instance.PlaySniperShot();
                break;
        }
    }



    // ---------------------------------------------------------------------
    // Inner Shot Context
    // ---------------------------------------------------------------------

    private struct ShotCalculationContext
    {
        public AmmoModuleData UsedAmmo;
        public WeaponRuntime Weapon;
        public float Distance;
        public ShotRangeBand RangeBand;

        public int AmmoBaseDamage;
        public float WeaponMultiplier;
        public float RangeMultiplier;

        public int ConditionalAmmoDamageAdd;
        public float ConditionalWeaponMultiplierAdd;

        public float RawDamage;

        public int FinalFlatDamageAdd;
        public float FinalDamageMultiplier;

        public int FinalDamage { get; set; }
    }
}