using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 인벤토리의 런타임 데이터.
/// 지금 단계에서는 "잉여 부착물 목록"만 관리한다.
/// </summary>
public class InventoryRuntime : MonoBehaviour
{
    [Header("Unequipped Attachments")]
    [SerializeField] private List<WeaponAttachmentData> unequippedAttachments = new List<WeaponAttachmentData>();

    /// <summary>
    /// 외부 읽기 전용 접근용.
    /// </summary>
    public IReadOnlyList<WeaponAttachmentData> UnequippedAttachments => unequippedAttachments;

    /// <summary>
    /// 해당 부착물이 잉여 목록에 있는지 검사.
    /// </summary>
    public bool ContainsAttachment(WeaponAttachmentData attachment)
    {
        if (attachment == null)
            return false;

        return unequippedAttachments.Contains(attachment);
    }

    /// <summary>
    /// 잉여 목록에 부착물 추가.
    /// </summary>
    public bool TryAddAttachment(WeaponAttachmentData attachment)
    {
        if (attachment == null)
        {
            Debug.LogWarning("[InventoryRuntime] TryAddAttachment failed: attachment is null.", this);
            return false;
        }

        if (unequippedAttachments.Contains(attachment))
        {
            Debug.LogWarning($"[InventoryRuntime] TryAddAttachment skipped: already contains [{attachment.attachmentName}].", this);
            return false;
        }

        unequippedAttachments.Add(attachment);
        return true;
    }

    /// <summary>
    /// 잉여 목록에서 부착물 제거.
    /// </summary>
    public bool TryRemoveAttachment(WeaponAttachmentData attachment)
    {
        if (attachment == null)
        {
            Debug.LogWarning("[InventoryRuntime] TryRemoveAttachment failed: attachment is null.", this);
            return false;
        }

        return unequippedAttachments.Remove(attachment);
    }

    public void SetInventoryRuntime(List<WeaponAttachmentData> newInventoryRuntime)
    {
        unequippedAttachments.Clear();

        if (newInventoryRuntime == null)
            return;

        for(int i = 0; i < newInventoryRuntime.Count; i++)
        {
            if(newInventoryRuntime[i] != null)
                unequippedAttachments.Add(newInventoryRuntime [i]);
        }
    }

}