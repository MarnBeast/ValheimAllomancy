using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotkeyBar : MonoBehaviour
{
	private class ElementData
	{
		public bool m_used;

		public GameObject m_go;

		public Image m_icon;

		public GuiBar m_durability;

		public Text m_amount;

		public GameObject m_equiped;

		public GameObject m_queued;

		public GameObject m_selection;
	}

	public GameObject m_elementPrefab;

	public float m_elementSpace = 70f;

	private int m_selected;

	private List<ElementData> m_elements = new List<ElementData>();

	private List<ItemDrop.ItemData> m_items = new List<ItemDrop.ItemData>();

	private void Update()
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer && !InventoryGui.IsVisible() && !Menu.IsVisible() && !GameCamera.InFreeFly())
		{
			if (ZInput.GetButtonDown("JoyDPadLeft"))
			{
				m_selected = Mathf.Max(0, m_selected - 1);
			}
			if (ZInput.GetButtonDown("JoyDPadRight"))
			{
				m_selected = Mathf.Min(m_elements.Count - 1, m_selected + 1);
			}
			if (ZInput.GetButtonDown("JoyDPadUp"))
			{
				localPlayer.UseHotbarItem(m_selected + 1);
			}
		}
		if (m_selected > m_elements.Count - 1)
		{
			m_selected = Mathf.Max(0, m_elements.Count - 1);
		}
		UpdateIcons(localPlayer);
	}

	private void UpdateIcons(Player player)
	{
		if (!player || player.IsDead())
		{
			foreach (ElementData element in m_elements)
			{
				Object.Destroy(element.m_go);
			}
			m_elements.Clear();
			return;
		}
		player.GetInventory().GetBoundItems(m_items);
		m_items.Sort((ItemDrop.ItemData x, ItemDrop.ItemData y) => x.m_gridPos.x.CompareTo(y.m_gridPos.x));
		int num = 0;
		foreach (ItemDrop.ItemData item in m_items)
		{
			if (item.m_gridPos.x + 1 > num)
			{
				num = item.m_gridPos.x + 1;
			}
		}
		if (m_elements.Count != num)
		{
			foreach (ElementData element2 in m_elements)
			{
				Object.Destroy(element2.m_go);
			}
			m_elements.Clear();
			for (int i = 0; i < num; i++)
			{
				ElementData elementData = new ElementData();
				elementData.m_go = Object.Instantiate(m_elementPrefab, base.transform);
				elementData.m_go.transform.localPosition = new Vector3((float)i * m_elementSpace, 0f, 0f);
				elementData.m_go.transform.Find("binding").GetComponent<Text>().set_text((i + 1).ToString());
				elementData.m_icon = elementData.m_go.transform.transform.Find("icon").GetComponent<Image>();
				elementData.m_durability = elementData.m_go.transform.Find("durability").GetComponent<GuiBar>();
				elementData.m_amount = elementData.m_go.transform.Find("amount").GetComponent<Text>();
				elementData.m_equiped = elementData.m_go.transform.Find("equiped").gameObject;
				elementData.m_queued = elementData.m_go.transform.Find("queued").gameObject;
				elementData.m_selection = elementData.m_go.transform.Find("selected").gameObject;
				m_elements.Add(elementData);
			}
		}
		foreach (ElementData element3 in m_elements)
		{
			element3.m_used = false;
		}
		bool flag = ZInput.IsGamepadActive();
		for (int j = 0; j < m_items.Count; j++)
		{
			ItemDrop.ItemData itemData = m_items[j];
			ElementData elementData2 = m_elements[itemData.m_gridPos.x];
			elementData2.m_used = true;
			((Component)(object)elementData2.m_icon).gameObject.SetActive(value: true);
			elementData2.m_icon.set_sprite(itemData.GetIcon());
			((Component)(object)elementData2.m_durability).gameObject.SetActive(itemData.m_shared.m_useDurability);
			if (itemData.m_shared.m_useDurability)
			{
				if (itemData.m_durability <= 0f)
				{
					elementData2.m_durability.SetValue(1f);
					elementData2.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					elementData2.m_durability.SetValue(itemData.GetDurabilityPercentage());
					elementData2.m_durability.ResetColor();
				}
			}
			elementData2.m_equiped.SetActive(itemData.m_equiped);
			elementData2.m_queued.SetActive(player.IsItemQueued(itemData));
			if (itemData.m_shared.m_maxStackSize > 1)
			{
				((Component)(object)elementData2.m_amount).gameObject.SetActive(value: true);
				elementData2.m_amount.set_text(itemData.m_stack + "/" + itemData.m_shared.m_maxStackSize);
			}
			else
			{
				((Component)(object)elementData2.m_amount).gameObject.SetActive(value: false);
			}
		}
		for (int k = 0; k < m_elements.Count; k++)
		{
			ElementData elementData3 = m_elements[k];
			elementData3.m_selection.SetActive(flag && k == m_selected);
			if (!elementData3.m_used)
			{
				((Component)(object)elementData3.m_icon).gameObject.SetActive(value: false);
				((Component)(object)elementData3.m_durability).gameObject.SetActive(value: false);
				elementData3.m_equiped.SetActive(value: false);
				elementData3.m_queued.SetActive(value: false);
				((Component)(object)elementData3.m_amount).gameObject.SetActive(value: false);
			}
		}
	}
}
