using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Resource,
    EquipableWeapon,
    EquipableArmor,
    EquipableBoots,
    EquipableHelm,
    Consumable,

}

public enum StatsType
{
    HP,
    MP,
    AD,
    DF,
    CR,
    CRD,
    PD,
    XP
}


[CreateAssetMenu(menuName = "New Item")]
public class ItemData : ScriptableObject
{
    public string id;
    [Header("Info")]
    public string displayName;
    public string description;
    public ItemType type;
    public Sprite icon;
    public GameObject dropPrefab;

    [Header("Stacking")]
    public bool canStack;
    public int maxStackAmount;

    [Header("ItemStats info")]
    public ItemDataStats[] statsOfItem;

    [Header("Equip")]
    public GameObject itemPrefab;

    public int buyPrice;

}

[System.Serializable]
public class ItemDataStats
{
    public StatsType type;
    public float value;
}
