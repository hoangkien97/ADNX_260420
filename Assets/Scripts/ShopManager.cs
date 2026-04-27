using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public int coin;
    public Text coinUI;
    public ShopItemSO[] shopItemsSO;
    public ShopTemplate[] shopPanels;
    public GameObject[] shopPanelsGO;
    public Button[] myPurchaseBtn;
    [SerializeField] private Text statHP;
    [SerializeField] private Text statSpeed;
    [SerializeField] private Text statDamage;
    private Player player;
    private Gun gun;


    void OnEnable()
    {
        for (int i = 0; i < shopItemsSO.Length; i++)
        {
            shopPanelsGO[i].SetActive(true);
        }

        coin = GameManager.CountCoin;
        coinUI.text = coin.ToString();

        LoadPanel();
        checkPurchaseable();
        RefreshPlayerStatTexts();
    }

    void Update()
    {

    }

    public void LoadPanel()
    {
        for (int i = 0; i < shopItemsSO.Length; i++)
        {
            shopPanels[i].txtTitle.text = shopItemsSO[i].title;
            shopPanels[i].txtDescription.text = shopItemsSO[i].description;
            shopPanels[i].txtCost.text = shopItemsSO[i].baseCost.ToString();
        }
    }

    public void PurchaseItem(int btnNo)
    {
        if (coin >= shopItemsSO[btnNo].baseCost)
        {
            coin -= shopItemsSO[btnNo].baseCost;
            GameManager.CountCoin = coin;
            coinUI.text = coin.ToString(); 

            ApplyEffect(shopItemsSO[btnNo]);
            checkPurchaseable();
            RefreshPlayerStatTexts();
        }
    }

    public void checkPurchaseable()
    {
        for (int i = 0; i < shopItemsSO.Length; i++)
        {
            if (coin >= shopItemsSO[i].baseCost)
            {
                myPurchaseBtn[i].interactable = true;
            }
            else
            {
                myPurchaseBtn[i].interactable = false;
            }
        }
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    private void ApplyEffect(ShopItemSO item)
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm == null) return;

        switch (item.itemType)
        {
            case ShopItemType.UpgradeSpeed:
                gm.UpgradeSpeed(item.effectValue);
                break;
            case ShopItemType.UpgradeDamage:
                gm.UpgradeDamage(item.effectValue);
                break;
            case ShopItemType.UpgradeMaxHP:
                gm.UpgradeMaxHP(item.effectValue);
                break;
        }
    }

    private void RefreshPlayerStatTexts()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }

        if (gun == null)
        {
            gun = FindAnyObjectByType<Gun>();
        }

        if (statHP != null)
        {
            statHP.text = player != null ? player.MaxHp.ToString("MaxHP : 0.##") : "N/A";
        }

        if (statSpeed != null)
        {
            statSpeed.text = player != null ? player.MoveSpeed.ToString("Speed : 0.##") : "N/A";
        }

        if (statDamage != null)
        {
            statDamage.text = gun != null ? gun.CurrentDamage.ToString("Damage : 0.##") : "N/A";
        }
    }

}
