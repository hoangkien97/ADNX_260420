using UnityEngine;
public enum ShopItemType
{
    UpgradeSpeed,
    UpgradeDamage,
    UpgradeMaxHP,
}

[CreateAssetMenu(fileName = "shopMenu", menuName = "ScriptableObjects/New Shop Item", order = 1)]
public class ShopItemSO : ScriptableObject
{
    public string title;
    public string description;
    public int baseCost;
    public ShopItemType itemType;
    public float effectValue;
}
