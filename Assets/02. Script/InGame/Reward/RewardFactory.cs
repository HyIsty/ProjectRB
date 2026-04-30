using System.Collections.Generic;
using UnityEngine;

public class RewardFactory : MonoBehaviour
{
    [SerializeField] private ItemPoolDatabase itemPoolDatabase;
    [Header("Reward Count")]
    [SerializeField] private int rewardCount = 3;

    [Header("Duplicate Rule")]
    [SerializeField] private bool allowDuplicateRewards = false;

    [Header("Guaranteed Gold Bonus")]
    [SerializeField] private int minGoldBonus = 50;
    [SerializeField] private int maxGoldBonus = 150;
    [SerializeField] private bool forceDifferentGoldAmount = true;

    public List<RewardCandidate> GenerateRewards()
    {
        return GenerateRewards(rewardCount);
    }

    public List<RewardCandidate> GenerateRewards(int count)
    {
        List<RewardCandidate> rewards = new List<RewardCandidate>();

        int safeCount = Mathf.Max(1, count);

        for (int i = 0; i < safeCount; i++)
        {
            RewardCandidate candidate = CreateRewardCandidate(rewards);

            if (candidate == null)
                continue;

            // 모든 보상 카드에 확정 골드 보너스를 붙인다.
            candidate.goldAmount = RollGoldAmount(rewards);

            rewards.Add(candidate);
        }

        return rewards;
    }

    private RewardCandidate CreateRewardCandidate(List<RewardCandidate> existingRewards)
    {
        List<RewardType> possibleTypes = GetAvailableRewardTypes();

        if (possibleTypes.Count == 0)
        {
            Debug.LogWarning("[RewardFactory] No reward pools available.");
            return null;
        }

        // 몇 번 시도해서 중복이 아닌 보상을 만든다.
        for (int i = 0; i < 20; i++)
        {
            RewardType selectedType = possibleTypes[Random.Range(0, possibleTypes.Count)];
            RewardCandidate candidate = CreateCandidateByType(selectedType);

            if (candidate == null)
                continue;

            if (allowDuplicateRewards || !IsDuplicateReward(candidate, existingRewards))
                return candidate;
        }

        // 중복 회피에 실패하면 마지막으로 그냥 하나 만든다.
        RewardType fallbackType = possibleTypes[Random.Range(0, possibleTypes.Count)];
        return CreateCandidateByType(fallbackType);
    }

    private List<RewardType> GetAvailableRewardTypes()
    {
        List<RewardType> possibleTypes = new List<RewardType>();
        if (itemPoolDatabase == null)
            return possibleTypes;

        if (itemPoolDatabase.HasWeapons())
            possibleTypes.Add(RewardType.Weapon);

        if (itemPoolDatabase.HasAmmo())
            possibleTypes.Add(RewardType.Ammo);

        if (itemPoolDatabase.HasAttachments())
            possibleTypes.Add(RewardType.Attachment);

        return possibleTypes;
    }

    private RewardCandidate CreateCandidateByType(RewardType rewardType)
    {
        switch (rewardType)
        {
            case RewardType.Weapon:
                return CreateWeaponReward();

            case RewardType.Ammo:
                return CreateAmmoReward();

            case RewardType.Attachment:
                return CreateAttachmentReward();

            default:
                return null;
        }
    }

    private RewardCandidate CreateWeaponReward()
    {
        if (itemPoolDatabase == null || !itemPoolDatabase.HasWeapons())
            return null;

        RewardCandidate candidate = new RewardCandidate();
        candidate.rewardType = RewardType.Weapon;
        candidate.weaponData = itemPoolDatabase.GetRandomWeapon();

        return candidate;
    }


    private RewardCandidate CreateAmmoReward()
    {
        if (itemPoolDatabase == null || !itemPoolDatabase.HasAmmo())
            return null;

        RewardCandidate candidate = new RewardCandidate();
        candidate.rewardType = RewardType.Ammo;
        candidate.ammoData = itemPoolDatabase.GetRandomAmmo();

        return candidate;
    }
    private RewardCandidate CreateAttachmentReward()
    {
        if (itemPoolDatabase == null || !itemPoolDatabase.HasAttachments())
            return null;

        RewardCandidate candidate = new RewardCandidate();
        candidate.rewardType = RewardType.Attachment;
        candidate.attachmentData = itemPoolDatabase.GetRandomAttachment();

        return candidate;
    }

    private int RollGoldAmount(List<RewardCandidate> existingRewards)
    {
        int min = Mathf.Min(minGoldBonus, maxGoldBonus);
        int max = Mathf.Max(minGoldBonus, maxGoldBonus);

        if (!forceDifferentGoldAmount)
            return Random.Range(min, max + 1);

        // 50~150은 폭이 넓으니 20번 정도만 굴려도 충분하다.
        for (int i = 0; i < 20; i++)
        {
            int rolled = Random.Range(min, max + 1);

            bool alreadyUsed = false;

            for (int j = 0; j < existingRewards.Count; j++)
            {
                if (existingRewards[j] != null && existingRewards[j].goldAmount == rolled)
                {
                    alreadyUsed = true;
                    break;
                }
            }

            if (!alreadyUsed)
                return rolled;
        }

        // 혹시 다 실패하면 그냥 랜덤 허용
        return Random.Range(min, max + 1);
    }

    private bool IsDuplicateReward(RewardCandidate candidate, List<RewardCandidate> existingRewards)
    {
        if (candidate == null || existingRewards == null)
            return false;

        for (int i = 0; i < existingRewards.Count; i++)
        {
            RewardCandidate existing = existingRewards[i];

            if (existing == null)
                continue;

            if (existing.rewardType != candidate.rewardType)
                continue;

            switch (candidate.rewardType)
            {
                case RewardType.Weapon:
                    if (existing.weaponData == candidate.weaponData)
                        return true;
                    break;

                case RewardType.Ammo:
                    if (existing.ammoData == candidate.ammoData)
                        return true;
                    break;

                case RewardType.Attachment:
                    if (existing.attachmentData == candidate.attachmentData)
                        return true;
                    break;
            }
        }

        return false;
    }
}