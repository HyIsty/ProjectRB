using System.Collections.Generic;
using UnityEngine;

public class ItemPoolDatabase : MonoBehaviour
{
    [Header("Weapon Pool")]
    [SerializeField] private List<WeaponData> weaponPool = new List<WeaponData>();

    [Header("Ammo Pool")]
    [SerializeField] private List<AmmoModuleData> ammoPool = new List<AmmoModuleData>();

    [Header("Attachment Pool")]
    [SerializeField] private List<WeaponAttachmentData> attachmentPool = new List<WeaponAttachmentData>();

    public List<WeaponData> WeaponPool => weaponPool;
    public List<AmmoModuleData> AmmoPool => ammoPool;
    public List<WeaponAttachmentData> AttachmentPool => attachmentPool;

    public bool HasWeapons()
    {
        return weaponPool != null && weaponPool.Count > 0;
    }

    public bool HasAmmo()
    {
        return ammoPool != null && ammoPool.Count > 0;
    }

    public bool HasAttachments()
    {
        return attachmentPool != null && attachmentPool.Count > 0;
    }

    public WeaponData GetRandomWeapon()
    {
        if (!HasWeapons())
            return null;

        return weaponPool[Random.Range(0, weaponPool.Count)];
    }

    public AmmoModuleData GetRandomAmmo()
    {
        if (!HasAmmo())
            return null;

        return ammoPool[Random.Range(0, ammoPool.Count)];
    }

    public WeaponAttachmentData GetRandomAttachment()
    {
        if (!HasAttachments())
            return null;

        return attachmentPool[Random.Range(0, attachmentPool.Count)];
    }
}