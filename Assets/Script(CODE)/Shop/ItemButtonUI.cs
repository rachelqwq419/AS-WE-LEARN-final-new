using UnityEngine;
using UnityEngine.UI;

public class ItemButtonUI : MonoBehaviour
{
    public ItemData itemData;
    public Button button;
    public Image iconImage;
    private void Start()
    {
        if (iconImage != null)
            iconImage.sprite = itemData.icon;
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(OnClick);

    }

    private void OnClick()
    {
        ItemInfoUIManager.instance.DisplayItemInfo(itemData);
    }

}
