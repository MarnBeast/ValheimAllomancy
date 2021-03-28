using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CookingStation : MonoBehaviour, Interactable, Hoverable
{
	[Serializable]
	public class ItemConversion
	{
		public ItemDrop m_from;

		public ItemDrop m_to;

		public float m_cookTime = 10f;
	}

	private const float cookDelta = 1f;

	public EffectList m_addEffect = new EffectList();

	public EffectList m_doneEffect = new EffectList();

	public EffectList m_overcookedEffect = new EffectList();

	public EffectList m_pickEffector = new EffectList();

	public float m_spawnOffset = 0.5f;

	public float m_spawnForce = 5f;

	public ItemDrop m_overCookedItem;

	public List<ItemConversion> m_conversion = new List<ItemConversion>();

	public Transform[] m_slots;

	public string m_name = "";

	private ZNetView m_nview;

	private ParticleSystem[] m_ps;

	private AudioSource[] m_as;

	private void Awake()
	{
		m_nview = base.gameObject.GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			m_ps = (ParticleSystem[])(object)new ParticleSystem[m_slots.Length];
			m_as = (AudioSource[])(object)new AudioSource[m_slots.Length];
			for (int i = 0; i < m_slots.Length; i++)
			{
				m_ps[i] = m_slots[i].GetComponentInChildren<ParticleSystem>();
				m_as[i] = m_slots[i].GetComponentInChildren<AudioSource>();
			}
			m_nview.Register("RemoveDoneItem", RPC_RemoveDoneItem);
			m_nview.Register<string>("AddItem", RPC_AddItem);
			m_nview.Register<int, string>("SetSlotVisual", RPC_SetSlotVisual);
			InvokeRepeating("UpdateCooking", 0f, 1f);
		}
	}

	private void UpdateCooking()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		if (m_nview.IsOwner() && IsFireLit())
		{
			for (int i = 0; i < m_slots.Length; i++)
			{
				GetSlot(i, out var itemName, out var cookedTime);
				if (!(itemName != "") || !(itemName != m_overCookedItem.name))
				{
					continue;
				}
				ItemConversion itemConversion = GetItemConversion(itemName);
				if (itemName == null)
				{
					SetSlot(i, "", 0f);
					continue;
				}
				cookedTime += 1f;
				if (cookedTime > itemConversion.m_cookTime * 2f)
				{
					m_overcookedEffect.Create(m_slots[i].position, Quaternion.identity);
					SetSlot(i, m_overCookedItem.name, cookedTime);
				}
				else if (cookedTime > itemConversion.m_cookTime && itemName == itemConversion.m_from.name)
				{
					m_doneEffect.Create(m_slots[i].position, Quaternion.identity);
					SetSlot(i, itemConversion.m_to.name, cookedTime);
				}
				else
				{
					SetSlot(i, itemName, cookedTime);
				}
			}
		}
		UpdateVisual();
	}

	private void UpdateVisual()
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			GetSlot(i, out var itemName, out var _);
			SetSlotVisual(i, itemName);
		}
	}

	private void RPC_SetSlotVisual(long sender, int slot, string item)
	{
		SetSlotVisual(slot, item);
	}

	private void SetSlotVisual(int i, string item)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (item == "")
		{
			EmissionModule emission = m_ps[i].get_emission();
			((EmissionModule)(ref emission)).set_enabled(false);
			m_as[i].set_mute(true);
			if (m_slots[i].childCount > 0)
			{
				UnityEngine.Object.Destroy(m_slots[i].GetChild(0).gameObject);
			}
			return;
		}
		EmissionModule emission2 = m_ps[i].get_emission();
		((EmissionModule)(ref emission2)).set_enabled(true);
		m_as[i].set_mute(false);
		if (m_slots[i].childCount == 0 || m_slots[i].GetChild(0).name != item)
		{
			if (m_slots[i].childCount > 0)
			{
				UnityEngine.Object.Destroy(m_slots[i].GetChild(0).gameObject);
			}
			Transform transform = ObjectDB.instance.GetItemPrefab(item).transform.Find("attach");
			Transform transform2 = m_slots[i];
			GameObject gameObject = UnityEngine.Object.Instantiate(transform.gameObject, transform2.position, transform2.rotation, transform2);
			gameObject.name = item;
			Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].shadowCastingMode = ShadowCastingMode.Off;
			}
		}
	}

	private void RPC_RemoveDoneItem(long sender)
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			GetSlot(i, out var itemName, out var _);
			if (itemName != "" && IsItemDone(itemName))
			{
				SpawnItem(itemName);
				SetSlot(i, "", 0f);
				m_nview.InvokeRPC(ZNetView.Everybody, "SetSlotVisual", i, "");
				break;
			}
		}
	}

	private bool HaveDoneItem()
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			GetSlot(i, out var itemName, out var _);
			if (itemName != "" && IsItemDone(itemName))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsItemDone(string itemName)
	{
		if (itemName == m_overCookedItem.name)
		{
			return true;
		}
		ItemConversion itemConversion = GetItemConversion(itemName);
		if (itemConversion == null)
		{
			return false;
		}
		if (itemName == itemConversion.m_to.name)
		{
			return true;
		}
		return false;
	}

	private void SpawnItem(string name)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		Vector3 vector = base.transform.position + Vector3.up * m_spawnOffset;
		Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
		UnityEngine.Object.Instantiate(itemPrefab, vector, rotation).GetComponent<Rigidbody>().set_velocity(Vector3.up * m_spawnForce);
		m_pickEffector.Create(vector, Quaternion.identity);
	}

	public string GetHoverText()
	{
		return Localization.get_instance().Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_cstand_cook\n[<color=yellow><b>1-8</b></color>] $piece_cstand_cook");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid user, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (HaveDoneItem())
		{
			m_nview.InvokeRPC("RemoveDoneItem");
			return true;
		}
		ItemDrop.ItemData itemData = FindCookableItem(user.GetInventory());
		if (itemData == null)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_nocookitems");
			return false;
		}
		UseItem(user, itemData);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!IsFireLit())
		{
			user.Message(MessageHud.MessageType.Center, "$msg_needfire");
			return false;
		}
		if (GetFreeSlot() == -1)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_nocookroom");
			return false;
		}
		return CookItem(user.GetInventory(), item);
	}

	private bool IsFireLit()
	{
		if ((bool)EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Burning, 0.25f))
		{
			return true;
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

	private bool CookItem(Inventory inventory, ItemDrop.ItemData item)
	{
		string name = item.m_dropPrefab.name;
		if (!m_nview.HasOwner())
		{
			m_nview.ClaimOwnership();
		}
		if (!IsItemAllowed(item))
		{
			return false;
		}
		if (GetFreeSlot() == -1)
		{
			return false;
		}
		inventory.RemoveOneItem(item);
		m_nview.InvokeRPC("AddItem", name);
		return true;
	}

	private void RPC_AddItem(long sender, string itemName)
	{
		if (IsItemAllowed(itemName))
		{
			int freeSlot = GetFreeSlot();
			if (freeSlot != -1)
			{
				SetSlot(freeSlot, itemName, 0f);
				m_nview.InvokeRPC(ZNetView.Everybody, "SetSlotVisual", freeSlot, itemName);
				m_addEffect.Create(m_slots[freeSlot].position, Quaternion.identity);
			}
		}
	}

	private void SetSlot(int slot, string itemName, float cookedTime)
	{
		m_nview.GetZDO().Set("slot" + slot, itemName);
		m_nview.GetZDO().Set("slot" + slot, cookedTime);
	}

	private void GetSlot(int slot, out string itemName, out float cookedTime)
	{
		itemName = m_nview.GetZDO().GetString("slot" + slot);
		cookedTime = m_nview.GetZDO().GetFloat("slot" + slot);
	}

	private int GetFreeSlot()
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			if (m_nview.GetZDO().GetString("slot" + i) == "")
			{
				return i;
			}
		}
		return -1;
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

	private ItemConversion GetItemConversion(string itemName)
	{
		foreach (ItemConversion item in m_conversion)
		{
			if (item.m_from.gameObject.name == itemName || item.m_to.gameObject.name == itemName)
			{
				return item;
			}
		}
		return null;
	}
}
