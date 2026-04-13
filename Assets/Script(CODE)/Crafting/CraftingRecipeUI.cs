using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingRecipeUI : MonoBehaviour
{
    public CraftingRecipe recipe;
    public TextMeshProUGUI buttonText;
    //public GameObject buttonObject;
    public Image icon;
    public TextMeshProUGUI itemName;
    public Image[] resourceCosts;
    public Color canCraftColor, cannotCraftColor;
    public bool canCraft;

    private void OnEnable()
    {
        UpdateCanCraft();
    }

    public void UpdateCanCraft()
    {
        canCraft = true;

        for (int i = 0; i < recipe.costs.Length; i++)
        {
            //if we dont have enough item then dont craft
            if (!InventoryManager.instance.HasItem(recipe.costs[i].item, recipe.costs[i].quantity))
            {
                canCraft = false;
                break;
            }
        }

        //set the background image if we can craft to cancraftColor if not set it to cannotCraftColor
        buttonText.color = canCraft ? canCraftColor : cannotCraftColor;
        //buttonObject.SetActive(canCraft);
    }

    // Start is called before the first frame update
    void Start()
    {
        icon.sprite = recipe.itemToCraft.icon;
        itemName.text = recipe.itemToCraft.displayName;

        for (int i = 0; i < resourceCosts.Length; i++)
        {
            if (i < recipe.costs.Length)
            {
                resourceCosts[i].gameObject.SetActive(true);
                resourceCosts[i].sprite = recipe.costs[i].item.icon;
                resourceCosts[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = recipe.costs[i].quantity.ToString();
            }
            else
            {
                resourceCosts[i].gameObject.SetActive(false);
            }

        }
    }

    public void OnClickCraftingButton()
    {
        if (canCraft)
        {
            CraftingWindow.instance.Craft(recipe);
        }
    }

}
