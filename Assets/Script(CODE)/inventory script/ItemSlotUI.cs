using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    public Image icon;
    public Button button;
    public TextMeshProUGUI quantityText;
    private ItemSlot currentSlot;
    public int index;


    public void Set(ItemSlot slot)
    {
        currentSlot = slot;
        icon.gameObject.SetActive(true);
        icon.sprite = slot.item.icon;
        quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : string.Empty;
    }

    public void Clear()
    {
        currentSlot = null;
        icon.gameObject.SetActive(false);
        quantityText.text = string.Empty;
    }

    public void OnclickButton()
    {
        InventoryManager.instance.SelectItem(index);
    }
}
