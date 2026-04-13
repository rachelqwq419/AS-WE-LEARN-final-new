using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipes", menuName = "New Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public ItemData itemToCraft;
    public ResourceCost[] costs;
}

[System.Serializable]
public class ResourceCost
{
    public ItemData item;
    public int quantity;
}
