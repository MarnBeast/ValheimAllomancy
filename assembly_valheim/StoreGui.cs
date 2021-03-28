using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StoreGui : MonoBehaviour
{
	private static StoreGui m_instance;

	public GameObject m_rootPanel;

	public Button m_buyButton;

	public Button m_sellButton;

	public RectTransform m_listRoot;

	public GameObject m_listElement;

	public Scrollbar m_listScroll;

	public ScrollRectEnsureVisible m_itemEnsureVisible;

	public Text m_coinText;

	public EffectList m_buyEffects = new EffectList();

	public EffectList m_sellEffects = new EffectList();

	public float m_hideDistance = 5f;

	public float m_itemSpacing = 64f;

	public ItemDrop m_coinPrefab;

	private List<GameObject> m_itemList = new List<GameObject>();

	private Trader.TradeItem m_selectedItem;

	private Trader m_trader;

	private float m_itemlistBaseSize;

	private int m_hiddenFrames;

	private List<ItemDrop.ItemData> m_tempItems = new List<ItemDrop.ItemData>();

	public static StoreGui instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_rootPanel.SetActive(value: false);
		m_itemlistBaseSize = m_listRoot.rect.height;
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	private void Update()
	{
		if (!m_rootPanel.activeSelf)
		{
			m_hiddenFrames++;
			return;
		}
		m_hiddenFrames = 0;
		if (!m_trader)
		{
			Hide();
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene())
		{
			Hide();
			return;
		}
		if (Vector3.Distance(m_trader.transform.position, Player.m_localPlayer.transform.position) > m_hideDistance)
		{
			Hide();
			return;
		}
		if (InventoryGui.IsVisible() || Minimap.IsOpen())
		{
			Hide();
			return;
		}
		if ((Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible() && !Menu.IsVisible() && (bool)TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("Use")))
		{
			ZInput.ResetButtonStatus("JoyButtonB");
			Hide();
		}
		UpdateBuyButton();
		UpdateSellButton();
		UpdateRecipeGamepadInput();
		m_coinText.set_text(GetPlayerCoins().ToString());
	}

	public void Show(Trader trader)
	{
		if (!(m_trader == trader) || !IsVisible())
		{
			m_trader = trader;
			m_rootPanel.SetActive(value: true);
			FillList();
		}
	}

	public void Hide()
	{
		m_trader = null;
		m_rootPanel.SetActive(value: false);
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_hiddenFrames <= 1;
		}
		return false;
	}

	public void OnBuyItem()
	{
		BuySelectedItem();
	}

	private void BuySelectedItem()
	{
		if (m_selectedItem != null && CanAfford(m_selectedItem))
		{
			int stack = Mathf.Min(m_selectedItem.m_stack, m_selectedItem.m_prefab.m_itemData.m_shared.m_maxStackSize);
			int quality = m_selectedItem.m_prefab.m_itemData.m_quality;
			int variant = m_selectedItem.m_prefab.m_itemData.m_variant;
			if (Player.m_localPlayer.GetInventory().AddItem(m_selectedItem.m_prefab.name, stack, quality, variant, 0L, "") != null)
			{
				Player.m_localPlayer.GetInventory().RemoveItem(m_coinPrefab.m_itemData.m_shared.m_name, m_selectedItem.m_price);
				m_trader.OnBought(m_selectedItem);
				m_buyEffects.Create(base.transform.position, Quaternion.identity);
				Player.m_localPlayer.ShowPickupMessage(m_selectedItem.m_prefab.m_itemData, m_selectedItem.m_prefab.m_itemData.m_stack);
				FillList();
				Gogan.LogEvent("Game", "BoughtItem", m_selectedItem.m_prefab.name, 0L);
			}
		}
	}

	public void OnSellItem()
	{
		SellItem();
	}

	private void SellItem()
	{
		ItemDrop.ItemData sellableItem = GetSellableItem();
		if (sellableItem != null)
		{
			int stack = sellableItem.m_shared.m_value * sellableItem.m_stack;
			Player.m_localPlayer.GetInventory().RemoveItem(sellableItem);
			Player.m_localPlayer.GetInventory().AddItem(m_coinPrefab.gameObject.name, stack, m_coinPrefab.m_itemData.m_quality, m_coinPrefab.m_itemData.m_variant, 0L, "");
			string text = "";
			text = ((sellableItem.m_stack <= 1) ? sellableItem.m_shared.m_name : (sellableItem.m_stack + "x" + sellableItem.m_shared.m_name));
			m_sellEffects.Create(base.transform.position, Quaternion.identity);
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.get_instance().Localize("$msg_sold", new string[2]
			{
				text,
				stack.ToString()
			}), 0, sellableItem.m_shared.m_icons[0]);
			m_trader.OnSold();
			FillList();
			Gogan.LogEvent("Game", "SoldItem", text, 0L);
		}
	}

	private int GetPlayerCoins()
	{
		return Player.m_localPlayer.GetInventory().CountItems(m_coinPrefab.m_itemData.m_shared.m_name);
	}

	private bool CanAfford(Trader.TradeItem item)
	{
		int playerCoins = GetPlayerCoins();
		return item.m_price <= playerCoins;
	}

	private void FillList()
	{
		int playerCoins = GetPlayerCoins();
		int num = GetSelectedItemIndex();
		List<Trader.TradeItem> items = m_trader.m_items;
		foreach (GameObject item in m_itemList)
		{
			Object.Destroy(item);
		}
		m_itemList.Clear();
		float b = (float)items.Count * m_itemSpacing;
		b = Mathf.Max(m_itemlistBaseSize, b);
		m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b);
		for (int i = 0; i < items.Count; i++)
		{
			Trader.TradeItem tradeItem = items[i];
			GameObject element = Object.Instantiate(m_listElement, m_listRoot);
			element.SetActive(value: true);
			(element.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * (0f - m_itemSpacing));
			bool flag = tradeItem.m_price <= playerCoins;
			Image component = element.transform.Find("icon").GetComponent<Image>();
			component.set_sprite(tradeItem.m_prefab.m_itemData.m_shared.m_icons[0]);
			((Graphic)component).set_color(flag ? Color.white : new Color(1f, 0f, 1f, 0f));
			string text = Localization.get_instance().Localize(tradeItem.m_prefab.m_itemData.m_shared.m_name);
			if (tradeItem.m_stack > 1)
			{
				text = text + " x" + tradeItem.m_stack;
			}
			Text component2 = element.transform.Find("name").GetComponent<Text>();
			component2.set_text(text);
			((Graphic)component2).set_color(flag ? Color.white : Color.grey);
			UITooltip component3 = element.GetComponent<UITooltip>();
			component3.m_topic = tradeItem.m_prefab.m_itemData.m_shared.m_name;
			component3.m_text = tradeItem.m_prefab.m_itemData.GetTooltip();
			Text component4 = Utils.FindChild(element.transform, "price").GetComponent<Text>();
			component4.set_text(tradeItem.m_price.ToString());
			if (!flag)
			{
				((Graphic)component4).set_color(Color.grey);
			}
			((UnityEvent)(object)element.GetComponent<Button>().get_onClick()).AddListener((UnityAction)delegate
			{
				OnSelectedItem(element);
			});
			m_itemList.Add(element);
		}
		if (num < 0)
		{
			num = 0;
		}
		SelectItem(num, center: false);
	}

	private void OnSelectedItem(GameObject button)
	{
		int index = FindSelectedRecipe(button);
		SelectItem(index, center: false);
	}

	private int FindSelectedRecipe(GameObject button)
	{
		for (int i = 0; i < m_itemList.Count; i++)
		{
			if (m_itemList[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	private void SelectItem(int index, bool center)
	{
		ZLog.Log((object)("Setting selected recipe " + index));
		for (int i = 0; i < m_itemList.Count; i++)
		{
			bool active = i == index;
			m_itemList[i].transform.Find("selected").gameObject.SetActive(active);
		}
		if (center && index >= 0)
		{
			m_itemEnsureVisible.CenterOnItem(m_itemList[index].transform as RectTransform);
		}
		if (index < 0)
		{
			m_selectedItem = null;
		}
		else
		{
			m_selectedItem = m_trader.m_items[index];
		}
	}

	private void UpdateSellButton()
	{
		((Selectable)m_sellButton).set_interactable(GetSellableItem() != null);
	}

	private ItemDrop.ItemData GetSellableItem()
	{
		m_tempItems.Clear();
		Player.m_localPlayer.GetInventory().GetValuableItems(m_tempItems);
		foreach (ItemDrop.ItemData tempItem in m_tempItems)
		{
			if (tempItem.m_shared.m_name != m_coinPrefab.m_itemData.m_shared.m_name)
			{
				return tempItem;
			}
		}
		return null;
	}

	private int GetSelectedItemIndex()
	{
		int result = 0;
		for (int i = 0; i < m_trader.m_items.Count; i++)
		{
			if (m_trader.m_items[i] == m_selectedItem)
			{
				result = i;
			}
		}
		return result;
	}

	private void UpdateBuyButton()
	{
		UITooltip component = ((Component)(object)m_buyButton).GetComponent<UITooltip>();
		if (m_selectedItem != null)
		{
			bool flag = CanAfford(m_selectedItem);
			bool flag2 = Player.m_localPlayer.GetInventory().HaveEmptySlot();
			((Selectable)m_buyButton).set_interactable(flag && flag2);
			if (!flag)
			{
				component.m_text = Localization.get_instance().Localize("$msg_missingrequirement");
			}
			else if (!flag2)
			{
				component.m_text = Localization.get_instance().Localize("$inventory_full");
			}
			else
			{
				component.m_text = "";
			}
		}
		else
		{
			((Selectable)m_buyButton).set_interactable(false);
			component.m_text = "";
		}
	}

	private void UpdateRecipeGamepadInput()
	{
		if (m_itemList.Count > 0)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				SelectItem(Mathf.Min(m_itemList.Count - 1, GetSelectedItemIndex() + 1), center: true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				SelectItem(Mathf.Max(0, GetSelectedItemIndex() - 1), center: true);
			}
		}
	}
}
