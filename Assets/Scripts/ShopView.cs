using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện (View) của Shop. Thuần túy là chứa biến tham chiếu tới UI và Update UI.
/// Không chứa bất kỳ logic trừ tiền nào.
/// </summary>
public class ShopView : MonoBehaviour
{
    public Text coinUI;
    public ShopTemplate[] shopPanels;
    public Button[] purchaseBtns;
    public Text statHP;
    public Text statSpeed;
    public Text statDamage;

    public void SetCoin(int coin)
    {
        if (coinUI != null) coinUI.text = coin.ToString();
    }

    public void SetItemInfo(int index, string title, string description, int cost)
    {
        if (index < 0 || index >= shopPanels.Length) return;
        if (shopPanels[index].txtTitle != null) shopPanels[index].txtTitle.text = title;
        if (shopPanels[index].txtDescription != null) shopPanels[index].txtDescription.text = description;
        if (shopPanels[index].txtCost != null) shopPanels[index].txtCost.text = cost.ToString();
    }

    public void SetPurchaseInteractable(int index, bool interactable)
    {
        if (index >= 0 && index < purchaseBtns.Length)
        {
            if (purchaseBtns[index] != null)
                purchaseBtns[index].interactable = interactable;
        }
    }

    public void UpdateStats(string hpTxt, string speedTxt, string damageTxt)
    {
        if (statHP != null) statHP.text = hpTxt;
        if (statSpeed != null) statSpeed.text = speedTxt;
        if (statDamage != null) statDamage.text = damageTxt;
    }
}
