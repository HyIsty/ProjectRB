using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어 사격 "실행"만 담당하는 스크립트.
///
/// 중요:
/// - 이 스크립트는 더 이상 직접 입력을 읽지 않는다.
/// - 입력 감지는 앞으로 PlayerInputManager가 담당한다.
/// - 이 스크립트는 외부에서 사격 요청을 받으면,
///   현재 상태와 무기/탄약을 검사하고 실제 사격만 수행한다.
///
/// 현재 구현 포함:
/// - 권총 / 저격총 단발 사격
/// - 샷건 동시 산탄
/// - 소총 점사 코루틴
/// - 장애물 / 적 충돌 판정
/// - 사거리 밴드 기반 데미지 계산
/// - 탄 소모 및 discard 처리
/// - 첫 적중 1회 ammo effect 발동
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private AmmoDeckRuntime ammoDeck;
    [SerializeField] private UnitStatusController statusController;
    [SerializeField] private CombatManager combatManager;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask targetMask;

    [Header("Rifle Burst Timing")]
    [Tooltip("소총 점사에서 각 발 사이 간격(second)")]
    [SerializeField] private float rifleBurstInterval = 0.06f;

    [Header("Weapon Spread Extras")]
    [Tooltip("소총 점사 시 추가 오차(degrees)")]
    [SerializeField] private float rifleBurstExtraSpread = 1.5f;

    [Tooltip("샷건 산탄 시 추가 오차(degrees)")]
    [SerializeField] private float shotgunSpreadAngle = 12f;

    [Header("Ray Tracer")]
    [SerializeField] private ShotTracerFactory shotTracerFactory;

    /// <summary>
    /// 현재 소총 점사 코루틴이 진행 중인지 여부.
    /// 점사 중에는 새 사격 요청을 받지 않도록 막는다.
    /// </summary>
    private bool isBurstFiring = false;

    /// <summary>
    /// 외부에서 읽을 수 있는 점사 상태.
    /// 필요하면 InputManager나 UI에서 참고 가능.
    /// </summary>
    public bool IsBurstFiring => isBurstFiring;

    private void Awake()
    {
        if (aimController == null)
            aimController = GetComponent<PlayerAimController>();

        if (weaponController == null)
            weaponController = GetComponent<PlayerWeaponController>();

        if (statusController == null)
            statusController = GetComponent<UnitStatusController>();

        if (ammoDeck == null)
            ammoDeck = FindFirstObjectByType<AmmoDeckRuntime>();

        if (shotTracerFactory == null)
            shotTracerFactory = FindFirstObjectByType<ShotTracerFactory>();
        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();

    }

    /// <summary>
    /// InputManager가 호출할 메인 진입점.
    ///
    /// 반환값:
    /// - true  : 사격 요청이 정상적으로 받아들여짐
    /// - false : 사격 불가 상태라서 요청 거절
    /// </summary>
    public bool TryShootRequested()
    {
        // 점사 중에는 새 입력 막기
        if (isBurstFiring)
            return false;

        if (aimController == null)
        {
            Debug.LogError("PlayerShooter: PlayerAimController reference is missing.");
            return false;
        }

        if (weaponController == null)
        {
            Debug.LogError("PlayerShooter: PlayerWeaponController reference is missing.");
            return false;
        }

        if (ammoDeck == null)
        {
            Debug.LogError("PlayerShooter: AmmoDeckRuntime reference is missing.");
            return false;
        }

        // 상태이상 등으로 사격 불가면 중단
        if (statusController != null && !statusController.CanShoot)
        {
            Debug.Log("Player cannot shoot right now.");
            return false;
        }

        // 현재 장착 무기 검사
        if (!weaponController.HasCurrentWeapon())
        {
            Debug.Log("PlayerShooter: No current weapon is equipped.");
            return false;
        }
        WeaponRuntime currentWeapon = weaponController.CurrentWeaponRuntime;

        if (currentWeapon == null || !currentWeapon.HasBaseData)
        {
            Debug.Log("PlayerShooter: Current weapon runtime is invalid.");
            return false;
        }
        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("PlayerShooter: TurnManager is not assigned.");
            return false;
        }

        if (!TurnManager.Instance.IsPlayerTurn)
            return false;

        int apCost = Mathf.Max(0, currentWeapon.CurrentApCost);




        // 현재 조준 정보
        Vector2 origin = aimController.ShootOrigin;
        Vector2 baseDirection = aimController.AimDirection;

        if (baseDirection.sqrMagnitude < 0.0001f)
            return false;

        baseDirection.Normalize();
        
        if (!TurnManager.Instance.HasEnoughPlayerAP(apCost))
            return false;

        // 한 슬롯 소비
        if (!currentWeapon.TryConsumeNextAmmo(out AmmoModuleData usedRound))
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayEmptyFire();
            Debug.Log("Current weapon has no loaded ammo. Cannot shoot.");
            return false;
        }


        // 실제 공격 시작 직전에 AP 차감
        if (!TurnManager.Instance.TrySpendPlayerAP(apCost))
            return false;

        // 여기로 이동
        if (combatManager != null)
        {
            combatManager.NotifyPlayerGunshot(transform.position);
        }

        // 무기 타입별 발사 처리
        switch (currentWeapon.WeaponType)
        {
            case WeaponType.Rifle:
                StartCoroutine(FireRifleBurstRoutine(origin, baseDirection, currentWeapon, usedRound));
                break;

            case WeaponType.Shotgun:
                SoundManager.Instance?.PlayShotgunShot();
                FireShotgunAttack(origin, baseDirection, currentWeapon, usedRound);

                if (usedRound != null)
                    ammoDeck.Discard(usedRound);
                break;

            case WeaponType.Pistol:
                SoundManager.Instance?.PlayPistolShot();
                FireSingleAttack(origin, baseDirection, currentWeapon, usedRound);

                if (usedRound != null)
                    ammoDeck.Discard(usedRound);
                break;
            case WeaponType.Sniper:
                SoundManager.Instance?.PlaySniperShot();
                FireSingleAttack(origin, baseDirection, currentWeapon, usedRound);

                if (usedRound != null)
                    ammoDeck.Discard(usedRound);
                break;
            default:
                FireSingleAttack(origin, baseDirection, currentWeapon, usedRound);

                if (usedRound != null)
                    ammoDeck.Discard(usedRound);
                break;
        }

        return true;
    }

    /// <summary>
    /// 이전 이름 호환용 래퍼.
    /// 기존 코드에서 TryShoot()를 부르고 있어도 동작하게 남겨둔다.
    /// 나중에 모두 TryShootRequested()로 통일되면 삭제해도 된다.
    /// </summary>
    public void TryShoot()
    {
        TryShootRequested();
    }

    /// <summary>
    /// 권총 / 저격총 구현:
    /// 1개의 ray를 발사한다.
    /// </summary>
    private void FireSingleAttack(Vector2 origin, Vector2 baseDirection, WeaponRuntime weapon, AmmoModuleData usedRound)
    {
        bool effectTriggered = false;

        Vector2 shotDirection = ApplyRandomSpread(baseDirection, weapon.CurrentAimSpread);

        AttackRayResult result = ResolveOneRay(
            origin,
            shotDirection,
            weapon,
            usedRound,
            ref effectTriggered
        );

        if (result == AttackRayResult.Miss)
        {
            Debug.Log($"Shot fired, but no enemy was hit. Used round: {usedRound.displayName}");
        }
    }

    /// <summary>
    /// 소총 구현:
    /// 한 슬롯을 소비해서, 아주 짧은 간격으로 여러 발 사격 -> 점사
    /// </summary>
    private IEnumerator FireRifleBurstRoutine(
        Vector2 origin,
        Vector2 baseDirection,
        WeaponRuntime weapon,
        AmmoModuleData usedRound)
    {
        isBurstFiring = true;

        int projectileCount = Mathf.Max(1, weapon.CurrentProjectilesPerAttack);

        bool effectTriggered = false;
        bool anyTargetHit = false;

        for (int i = 0; i < projectileCount; i++)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayRifleShot();
            // 점사 느낌을 위해 매 발마다 현재 조준 방향을 다시 읽는다.
            Vector2 currentDirection = aimController != null ? aimController.AimDirection : baseDirection;

            if (currentDirection.sqrMagnitude < 0.0001f)
                currentDirection = baseDirection;

            currentDirection.Normalize();

            float spread = weapon.CurrentAimSpread + rifleBurstExtraSpread;
            Vector2 shotDirection = ApplyRandomSpread(currentDirection, spread);

            AttackRayResult result = ResolveOneRay(
                origin,
                shotDirection,
                weapon,
                usedRound,
                ref effectTriggered
            );

            if (result == AttackRayResult.HitTarget)
                anyTargetHit = true;

            // 마지막 발 뒤에는 기다릴 필요 없음
            if (i < projectileCount - 1)
            {
                yield return new WaitForSeconds(rifleBurstInterval);
            }
        }

        if (!anyTargetHit)
        {
            Debug.Log($"Rifle burst fired, but no enemy was hit. Used round: {usedRound.displayName}");
        }

        if (usedRound != null)
        {
            ammoDeck.Discard(usedRound);
        }

        isBurstFiring = false;
    }

    /// <summary>
    /// 샷건 구현:
    /// WeaponData.projectilesPerAttack 수만큼 펠릿을 동시에 발사한다.
    /// </summary>
    private void FireShotgunAttack(Vector2 origin, Vector2 baseDirection, WeaponRuntime weapon, AmmoModuleData usedRound)
    {
        int projectileCount = Mathf.Max(1, weapon.CurrentProjectilesPerAttack);

        bool effectTriggered = false;
        bool anyTargetHit = false;

        for (int i = 0; i < projectileCount; i++)
        {
            float spread = weapon.CurrentAimSpread + shotgunSpreadAngle;
            Vector2 shotDirection = ApplyRandomSpread(baseDirection, spread);

            AttackRayResult result = ResolveOneRay(
                origin,
                shotDirection,
                weapon,
                usedRound,
                ref effectTriggered
            );

            if (result == AttackRayResult.HitTarget)
                anyTargetHit = true;
        }

        if (!anyTargetHit)
        {
            Debug.Log($"Shotgun blast fired, but no enemy was hit. Used round: {usedRound.displayName}");
        }
    }

    /// <summary>
    /// 하나의 ray를 발사해서
    /// - 장애물에 막히는지
    /// - 적을 맞히는지
    /// - 아무것도 못 맞히는지
    /// 판정한다.
    /// </summary>
    private AttackRayResult ResolveOneRay(
        Vector2 origin,
        Vector2 direction,
        WeaponRuntime weapon,
        AmmoModuleData usedRound,
        ref bool effectTriggered)
    {
        if (weapon == null || !weapon.HasBaseData)
            return AttackRayResult.Miss;

        float rayLength = weapon.CurrentMaxRange;

        int combinedMask = obstacleMask.value | targetMask.value;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, rayLength, combinedMask);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];

            if (hit.collider == null)
                continue;

            int hitLayerMask = 1 << hit.collider.gameObject.layer;

            // 장애물이 먼저 나오면 막힘
            if ((obstacleMask.value & hitLayerMask) != 0)
            {
                SpawnTracer(origin, hit.point);
                Debug.Log($"Shot blocked by obstacle. Used round: {usedRound.displayName}");
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayWallHit();
                return AttackRayResult.Blocked;
            }

            // 적이 먼저 나오면 피격 처리
            if ((targetMask.value & hitLayerMask) != 0)
            {
                ResolveTargetHit(
                    targetCollider: hit.collider,
                    usedRound: usedRound,
                    currentWeapon: weapon,
                    distanceToTarget: hit.distance,
                    effectTriggered: ref effectTriggered
                );

                SpawnTracer(origin, hit.point);


                return AttackRayResult.HitTarget;
            }
        }

        // 아무 것도 안 맞으면 최대 사거리 끝까지 tracer를 보여준다.
        Vector2 missEndPoint = origin + direction * rayLength;
        SpawnTracer(origin, missEndPoint);
        return AttackRayResult.Miss;
    }

    private void ResolveTargetHit(
        Collider2D targetCollider,
        AmmoModuleData usedRound,
        WeaponRuntime currentWeapon,
        float distanceToTarget,
        ref bool effectTriggered)
    {
        if (targetCollider == null)
            return;

        ShotRangeBand rangeBand = currentWeapon.GetRangeBand(distanceToTarget);

        if (rangeBand == ShotRangeBand.OutOfRange)
        {
            Debug.Log($"Target is outside max range. No hit. Used round: {usedRound.displayName}");
            return;
        }

        int finalDamage = CalculateDamage(distanceToTarget, currentWeapon, usedRound);

        bool didHit = false;
        bool didKill = false; // 차후 확장성을 위한 변수

        UnitHealthController targetHealth = targetCollider.GetComponentInParent<UnitHealthController>();

        if (targetHealth != null)
        {
            // 실제 적이 맞았을 때만 적대화 알림
            EnemyAIController enemyAI = targetCollider.GetComponentInParent<EnemyAIController>();
            if (combatManager != null && enemyAI != null)
            {
                combatManager.NotifyEnemyDamagedByPlayer(enemyAI);
            }

            targetHealth.TakeDamage(finalDamage, rangeBand);
            didHit = true;

            Debug.Log(
                $"Hit target: {targetCollider.name} / " +
                $"Weapon: {currentWeapon.WeaponName} / " +
                $"Ammo: {usedRound.displayName} / " +
                $"Damage: {finalDamage} / " +
                $"Range: {rangeBand} / " +
                $"Effect: {usedRound.GetSafeEffectId()}"
            );
        }
        else
        {
            Debug.Log(
                $"Hit target collider, but UnitHealthController was not found. " +
                $"Target: {targetCollider.name} / " +
                $"Weapon: {currentWeapon.WeaponName} / " +
                $"Ammo: {usedRound.displayName} / " +
                $"Damage: {finalDamage} / " +
                $"Range: {rangeBand}"
            );
        }

        // 한 번의 공격에서 ammo effect는 첫 적중 1회만 발동
        if (didHit && !effectTriggered && CardActionManager.Instance != null)
        {
            CardActionContext context = new CardActionContext(
                attacker: gameObject,
                target: targetCollider.gameObject,
                usedRound: usedRound,
                finalDamage: finalDamage,
                didHit: didHit,
                didKill: didKill,
                rangeBand: rangeBand
            );

            CardActionManager.Instance.ExecuteAmmoEffect(context);
            effectTriggered = true;
        }
    }

    /// <summary>
    /// 발당 최종 데미지 계산.
    /// </summary>
    private int CalculateDamage(float distanceToTarget, WeaponRuntime weapon, AmmoModuleData usedRound)
    {
        if (weapon == null || !weapon.HasBaseData)
            return 0;

        ShotCalculationContext context = BuildShotCalculationContext(weapon, usedRound);

        // 2층
        ApplyAmmoConditionalAttachments(context);

        // 사거리 판정 + 계수
        if (!TryEvaluateRange(context, distanceToTarget))
            return 0;

        // 기본 수식
        context.RawDamage =
            context.ProjectileBaseDamage *
            context.WeaponDamageMultiplier *
            context.RangeMultiplier;

        // 3층
        ApplyFinalDamageAttachments(context);

        return context.GetRoundedFinalDamage();
    }

    /// <summary>
    /// 최소 구현용 발당 기본 데미지.
    /// </summary>
    private int GetProjectileBaseDamage(AmmoModuleData usedRound)
    {
        int ammoBonusDamage = 0;

        if (usedRound != null)
            ammoBonusDamage = usedRound.damage;

        return Mathf.Max(0, ammoBonusDamage);
    }

    private Vector2 ApplyRandomSpread(Vector2 baseDirection, float spreadAngle)
    {
        if (spreadAngle <= 0f)
            return baseDirection.normalized;

        float randomAngle = Random.Range(-spreadAngle, spreadAngle);
        return RotateVector(baseDirection.normalized, randomAngle);
    }

    private Vector2 RotateVector(Vector2 direction, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;

        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        float x = direction.x * cos - direction.y * sin;
        float y = direction.x * sin + direction.y * cos;

        return new Vector2(x, y).normalized;
    }

    private void SpawnTracer(Vector2 start, Vector2 end)
    {
        if (shotTracerFactory != null)
        {
            shotTracerFactory.SpawnTracer(start, end);
        }
    }

    private enum AttackRayResult
    {
        Miss,
        Blocked,
        HitTarget
    }

    private bool TryEvaluateRange(ShotCalculationContext context, float distanceToTarget)
    {
        if (context == null)
            return false;

        if (distanceToTarget <= context.OptimalRangeMax)
        {
            context.RangeBand = ShotRangeBand.Optimal;
            context.RangeMultiplier = context.OptimalDamageMultiplier;
            return true;
        }

        if (distanceToTarget <= context.MaxRange)
        {
            context.RangeBand = ShotRangeBand.Far;
            context.RangeMultiplier = context.FarDamageMultiplier;
            return true;
        }

        context.RangeBand = ShotRangeBand.OutOfRange;
        context.RangeMultiplier = 0f;
        return false;
    }

    private ShotCalculationContext BuildShotCalculationContext(
        WeaponRuntime weapon,
        AmmoModuleData usedRound)
    {
        ShotCalculationContext context = new ShotCalculationContext();

        context.Weapon = weapon;
        context.UsedRound = usedRound;

        context.ProjectileBaseDamage = GetProjectileBaseDamage(usedRound);
        context.WeaponDamageMultiplier = weapon.CurrentWeaponDamageMultiplier;
        context.OptimalRangeMax = weapon.CurrentOptimalRangeMax;
        context.MaxRange = weapon.CurrentMaxRange;
        context.OptimalDamageMultiplier = weapon.CurrentOptimalDamageMultiplier;
        context.FarDamageMultiplier = weapon.CurrentFarDamageMultiplier;

        return context;
    }

    private void ApplyAmmoConditionalAttachments(ShotCalculationContext context)
    {
        if (context == null || context.Weapon == null)
            return;

        var attachments = context.Weapon.EquippedAttachments;

        if (attachments == null)
            return;

        for (int i = 0; i < attachments.Count; i++)
        {
            WeaponAttachmentData attachment = attachments[i];

            if (attachment == null)
                continue;

            if (!attachment.MatchesAmmo(context.UsedRound))
                continue;

            context.ProjectileBaseDamage += attachment.conditionalProjectileBaseDamageAdd;
            context.WeaponDamageMultiplier += attachment.conditionalWeaponDamageMultiplierAdd;
            context.OptimalRangeMax += attachment.conditionalOptimalRangeMaxAdd;
            context.MaxRange += attachment.conditionalMaxRangeAdd;
            context.OptimalDamageMultiplier += attachment.conditionalOptimalDamageMultiplierAdd;
            context.FarDamageMultiplier += attachment.conditionalFarDamageMultiplierAdd;
        }

        context.ProjectileBaseDamage = Mathf.Max(0, context.ProjectileBaseDamage);
        context.WeaponDamageMultiplier = Mathf.Max(0f, context.WeaponDamageMultiplier);
        context.OptimalRangeMax = Mathf.Max(0f, context.OptimalRangeMax);
        context.MaxRange = Mathf.Max(context.OptimalRangeMax, context.MaxRange);
        context.OptimalDamageMultiplier = Mathf.Max(0f, context.OptimalDamageMultiplier);
        context.FarDamageMultiplier = Mathf.Max(0f, context.FarDamageMultiplier);
    }

    private void ApplyFinalDamageAttachments(ShotCalculationContext context)
    {
        if (context == null || context.Weapon == null)
            return;

        var attachments = context.Weapon.EquippedAttachments;

        if (attachments == null)
            return;

        for (int i = 0; i < attachments.Count; i++)
        {
            WeaponAttachmentData attachment = attachments[i];

            if (attachment == null)
                continue;

            // 필요하면 여기서도 ammo 조건을 걸 수 있다.
            bool useThisFinalBonus =
                string.IsNullOrWhiteSpace(attachment.requiredAmmoId) ||
                attachment.MatchesAmmo(context.UsedRound);

            if (!useThisFinalBonus)
                continue;

            context.FinalDamageFlatAdd += attachment.finalDamageFlatAdd;
            context.FinalDamageMultiplier += attachment.finalDamageMultiplierAdd;
        }

        context.FinalDamageMultiplier = Mathf.Max(0f, context.FinalDamageMultiplier);
    }

    /// <summary>
    /// 현재 무기 타입에 맞는 사격음을 재생한다.
    /// 
    /// 주의:
    /// - 라이플은 burst 루프 안에서 한 발마다 따로 재생할 예정이라
    ///   여기에서는 일반 단발 무기용으로만 사용하거나,
    ///   라이플 케이스를 별도로 조심해서 호출해야 한다.
    /// </summary>
    private void PlayGunShotSfx(WeaponRuntime weapon)
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

            case WeaponType.Sniper:
                SoundManager.Instance.PlaySniperShot();
                break;

            case WeaponType.Rifle:
                // 라이플은 burst 루프 안에서 PlayRifleShot()을 여러 번 호출하는 쪽이 맞다.
                SoundManager.Instance.PlayRifleShot();
                break;
        }
    }
}