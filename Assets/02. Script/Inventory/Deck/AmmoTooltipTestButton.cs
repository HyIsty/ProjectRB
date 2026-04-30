using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 임시 테스트용.
/// 지정한 ammoData를 hover 하면 AmmoTooltipUI를 띄운다.
/// </summary>
public class AmmoTooltipTestButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private AmmoTooltipUI ammoTooltipUI;
    [SerializeField] private AmmoModuleData ammoData;
    [SerializeField] private int previewDamageDelta = 0;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ammoTooltipUI == null || ammoData == null)
            return;

        ammoTooltipUI.ShowForAmmo(ammoData, previewDamageDelta);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ammoTooltipUI == null)
            return;

        ammoTooltipUI.Hide();
    }
}