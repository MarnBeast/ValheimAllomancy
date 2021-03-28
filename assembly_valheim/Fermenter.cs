using System;
using System.Collections.Generic;
using UnityEngine;

public class Fermenter : MonoBehaviour, Hoverable, Interactable
{
	[Serializable]
	public class ItemConversion
	{
		public ItemDrop m_from;

		public ItemDrop m_to;

		public int m_producedItems = 4;
	}

	private enum Status
	{
		Empty,
		Fermenting,
		Exposed,
		Ready
	}

	private const float updateDT = 2f;

	public string m_name = "Fermentation barrel";

	public float m_fermentationDuration = 2400f;

	public GameObject m_fermentingObject;

	public GameObject m_readyObject;

	public GameObject m_topObject;

	public EffectList m_addedEffects = new EffectList();

	public EffectList m_tapEffects = new EffectList();

	public EffectList m_spawnEffects = new EffectList();

	public Switch m_addSwitch;

	public Switch m_tapSwitch;

	public float m_tapDelay = 1.5f;

	public Transform m_outputPoint;

	public Transform m_roofCheckPoint;

	public List<ItemConversion> m_conversion = new List<ItemConversion>();

	private ZNetView m_nview;

	private float m_updateCoverTimer;

	private bool m_exposed;

	private string m_delayedTapItem = "";

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_fermentingObject.SetActive(value: false);
		m_readyObject.SetActive(value: false);
		m_topObject.SetActive(value: true);
		if (!(m_nview == null) && m_nview.GetZDO() != null)
		{
			m_nview.Register<string>("AddItem", RPC_AddItem);
			m_nview.Register("Tap", RPC_Tap);
			InvokeRepeating("UpdateVis", 2f, 2f);
		}
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.get_instance().Localize(m_name + "\n$piece_noaccess");
		}
		switch (GetStatus())
		{
		case Status.Ready:
		{
			string contentName2 = GetContentName();
			return Localization.get_instance().Localize(m_name + " ( " + contentName2 + ", $piece_fermenter_ready )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_fermenter_tap");
		}
		case Status.Fermenting:
		{
			string contentName = GetContentName();
			if (m_exposed)
			{
				return Localization.get_instance().Localize(m_name + " ( " + contentName + ", $piece_fermenter_exposed )");
			}
			return Localization.get_instance().Localize(m_name + " ( " + contentName + ", $piece_fermenter_fermenting )");
		}
		case Status.Empty:
			return Localization.get_instance().Localize(m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_fermenter_add");
		default:
			return m_name;
		}
	}

	public bool Interact(Humanoid user, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		switch (GetStatus())
		{
		case Status.Empty:
		{
			ItemDrop.ItemData itemData = FindCookableItem(user.GetInventory());
			if (itemData == null)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems");
				return true;
			}
			AddItem(user, itemData);
			break;
		}
		case Status.Ready:
			m_nview.InvokeRPC("Tap");
			break;
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return false;
		}
		return AddItem(user, item);
	}

	private void UpdateVis()
	{
		UpdateCover(2f);
		switch (GetStatus())
		{
		case Status.Empty:
			m_fermentingObject.SetActive(value: false);
			m_readyObject.SetActive(value: false);
			m_topObject.SetActive(value: false);
			break;
		case Status.Fermenting:
			m_readyObject.SetActive(value: false);
			m_topObject.SetActive(value: true);
			m_fermentingObject.SetActive(!m_exposed);
			break;
		case Status.Ready:
			m_fermentingObject.SetActive(value: false);
			m_readyObject.SetActive(value: true);
			m_topObject.SetActive(value: true);
			break;
		case Status.Exposed:
			break;
		}
	}

	private Status GetStatus()
	{
		if (string.IsNullOrEmpty(GetContent()))
		{
			return Status.Empty;
		}
		if (GetFermentationTime() > (double)m_fermentationDuration)
		{
			return Status.Ready;
		}
		return Status.Fermenting;
	}

	private bool AddItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (GetStatus() != 0)
		{
			return false;
		}
		if (!IsItemAllowed(item))
		{
			return false;
		}
		if (!user.GetInventory().RemoveOneItem(item))
		{
			return false;
		}
		m_nview.InvokeRPC("AddItem", item.m_dropPrefab.name);
		return true;
	}

	private void RPC_AddItem(long sender, string name)
	{
		if (m_nview.IsOwner() && GetStatus() == Status.Empty)
		{
			if (!IsItemAllowed(name))
			{
				ZLog.DevLog((object)"Item not allowed");
				return;
			}
			m_addedEffects.Create(base.transform.position, base.transform.rotation);
			m_nview.GetZDO().Set("Content", name);
			m_nview.GetZDO().Set("StartTime", ZNet.instance.GetTime().Ticks);
		}
	}

	private void RPC_Tap(long sender)
	{
		if (m_nview.IsOwner() && GetStatus() == Status.Ready)
		{
			m_delayedTapItem = GetContent();
			Invoke("DelayedTap", m_tapDelay);
			m_tapEffects.Create(base.transform.position, base.transform.rotation);
			m_nview.GetZDO().Set("Content", "");
			m_nview.GetZDO().Set("StartTime", 0);
		}
	}

	private void DelayedTap()
	{
		m_spawnEffects.Create(m_outputPoint.transform.position, Quaternion.identity);
		ItemConversion itemConversion = GetItemConversion(m_delayedTapItem);
		if (itemConversion != null)
		{
			float d = 0.3f;
			for (int i = 0; i < itemConversion.m_producedItems; i++)
			{
				Vector3 position = m_outputPoint.position + Vector3.up * d;
				UnityEngine.Object.Instantiate(itemConversion.m_to, position, Quaternion.identity);
			}
		}
	}

	private void ResetFermentationTimer()
	{
		if (GetStatus() == Status.Fermenting)
		{
			m_nview.GetZDO().Set("StartTime", ZNet.instance.GetTime().Ticks);
		}
	}

	private double GetFermentationTime()
	{
		DateTime d = new DateTime(m_nview.GetZDO().GetLong("StartTime", 0L));
		if (d.Ticks == 0L)
		{
			return -1.0;
		}
		return (ZNet.instance.GetTime() - d).TotalSeconds;
	}

	private string GetContentName()
	{
		string content = GetContent();
		if (string.IsNullOrEmpty(content))
		{
			return "";
		}
		ItemConversion itemConversion = GetItemConversion(content);
		if (itemConversion == null)
		{
			return "Invalid";
		}
		return itemConversion.m_from.m_itemData.m_shared.m_name;
	}

	private string GetContent()
	{
		return m_nview.GetZDO().GetString("Content");
	}

	private void UpdateCover(float dt)
	{
		m_updateCoverTimer += dt;
		if (m_updateCoverTimer > 10f)
		{
			m_updateCoverTimer = 0f;
			float num = default(float);
			bool flag = default(bool);
			Cover.GetCoverForPoint(m_roofCheckPoint.position, ref num, ref flag);
			m_exposed = !flag || num < 0.7f;
			if (m_exposed && m_nview.IsOwner())
			{
				ResetFermentationTimer();
			}
		}
	}

	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return IsItemAllowed(item.m_dropPrefab.name);
	}

	private bool IsItemAllowed(string itemName)
	{
		foreach (ItemConversion item in m_conversion)
		{
			if (item.m_from.gameObject.name == itemName)
			{
				return true;
			}
		}
		return false;
	}

	private ItemDrop.ItemData FindCookableItem(Inventory inventory)
	{
		foreach (ItemConversion item2 in m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(item2.m_from.m_itemData.m_shared.m_name);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	private ItemConversion GetItemConversion(string itemName)
	{
		foreach (ItemConversion item in m_conversion)
		{
			if (item.m_from.gameObject.name == itemName)
			{
				return item;
			}
		}
		return null;
	}
}
