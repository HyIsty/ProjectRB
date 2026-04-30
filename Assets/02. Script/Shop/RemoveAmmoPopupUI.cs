using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RemoveAmmoPopupUI : MonoBehaviour
{
    private class AmmoGroup
    {
        public AmmoModuleData representative;
        public int count;
    }

    [Header("Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Outside Cancel")]
    [SerializeField] private Button outsideCancelButton;

    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private RemoveAmmoRowItemUI rowPrefab;

    private readonly List<RemoveAmmoRowItemUI> spawnedRows = new List<RemoveAmmoRowItemUI>();

    private Action<AmmoModuleData> onConfirmRemove;
    private Action onCancelRemove;

    private void Awake()
    {
        if (outsideCancelButton != null)
        {
            outsideCancelButton.onClick.RemoveAllListeners();
            outsideCancelButton.onClick.AddListener(Cancel);
        }

        Hide();
    }

    public void Show(
        List<AmmoModuleData> ammoDeck,
        Action<AmmoModuleData> confirmCallback,
        Action cancelCallback)
    {
        onConfirmRemove = confirmCallback;
        onCancelRemove = cancelCallback;

        if (popupRoot != null)
            popupRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        RebuildList(ammoDeck);
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        onConfirmRemove = null;
        onCancelRemove = null;

        ClearRows();
    }

    public void SelectAmmo(AmmoModuleData ammo)
    {
        if (ammo == null)
            return;

        if (onConfirmRemove == null)
        {
            Debug.LogWarning("[RemoveAmmoPopupUI] Confirm callback is missing.");
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayShopBuy();
        onConfirmRemove.Invoke(ammo);
    }

    private void Cancel()
    {
        if (onCancelRemove != null)
            onCancelRemove.Invoke();
        else
            Hide();
    }

    private void RebuildList(List<AmmoModuleData> ammoDeck)
    {
        ClearRows();

        if (contentRoot == null || rowPrefab == null)
        {
            Debug.LogWarning("[RemoveAmmoPopupUI] ContentRoot or RowPrefab is missing.");
            return;
        }

        if (ammoDeck == null || ammoDeck.Count == 0)
            return;

        Dictionary<string, AmmoGroup> groups = BuildGroups(ammoDeck);

        foreach (AmmoGroup group in groups.Values)
        {
            RemoveAmmoRowItemUI row = Instantiate(rowPrefab, contentRoot);
            row.Bind(group.representative, group.count, this);
            spawnedRows.Add(row);
        }
    }

    private Dictionary<string, AmmoGroup> BuildGroups(List<AmmoModuleData> ammoDeck)
    {
        Dictionary<string, AmmoGroup> groups = new Dictionary<string, AmmoGroup>();

        for (int i = 0; i < ammoDeck.Count; i++)
        {
            AmmoModuleData ammo = ammoDeck[i];

            if (ammo == null)
                continue;

            string key = GetAmmoKey(ammo);

            if (!groups.ContainsKey(key))
            {
                groups[key] = new AmmoGroup
                {
                    representative = ammo,
                    count = 0
                };
            }

            groups[key].count++;
        }

        return groups;
    }

    private string GetAmmoKey(AmmoModuleData ammo)
    {
        if (ammo == null)
            return "NULL";

        if (!string.IsNullOrEmpty(ammo.id))
            return ammo.id;

        return ammo.displayName;
    }

    private void ClearRows()
    {
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] != null)
                Destroy(spawnedRows[i].gameObject);
        }

        spawnedRows.Clear();
    }
}