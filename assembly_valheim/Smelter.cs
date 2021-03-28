using System;
using System.Collections.Generic;
using UnityEngine;

public class Smelter : MonoBehaviour
{
	[Serializable]
	public class ItemConversion
	{
		public ItemDrop m_from;

		public ItemDrop m_to;
	}

	public string m_name = "Smelter";

	public string m_addOreTooltip = "$piece_smelter_additem";

	public string m_emptyOreTooltip = "$piece_smelter_empty";

	public Switch m_addWoodSwitch;

	public Switch m_addOreSwitch;

	public Switch m_emptyOreSwitch;

	public Transform m_outputPoint;

	public Transform m_roofCheckPoint;

	public GameObject m_enabledObject;

	public Animator[] m_animators;

	public ItemDrop m_fuelItem;

	public int m_maxOre = 10;

	public int m_maxFuel = 10;

	public int m_fuelPerProduct = 4;

	public float m_secPerProduct = 10f;

	public bool m_spawnStack;

	public bool m_requiresRoof;

	public Windmill m_windmill;

	public List<ItemConversion> m_conversion = new List<ItemConversion>();

	public EffectList m_oreAddedEffects = new EffectList();

	public EffectList m_fuelAddedEffects = new EffectList();

	public EffectList m_produceEffects = new EffectList();

	private ZNetView m_nview;

	private bool m_haveRoof;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (!(m_nview == null) && m_nview.GetZDO() != null)
		{
			Switch addOreSwitch = m_addOreSwitch;
			addOreSwitch.m_onUse = (Switch.Callback)Delegate.Combine(addOreSwitch.m_onUse, new Switch.Callback(OnAddOre));
			if ((bool)m_addWoodSwitch)
			{
				Switch addWoodSwitch = m_addWoodSwitch;
				addWoodSwitch.m_onUse = (Switch.Callback)Delegate.Combine(addWoodSwitch.m_onUse, new Switch.Callback(OnAddFuel));
			}
			if ((bool)m_emptyOreSwitch)
			{
				Switch emptyOreSwitch = m_emptyOreSwitch;
				emptyOreSwitch.m_onUse = (Switch.Callback)Delegate.Combine(emptyOreSwitch.m_onUse, new Switch.Callback(OnEmpty));
			}
			m_nview.Register<string>("AddOre", RPC_AddOre);
			m_nview.Register("AddFuel", RPC_AddFuel);
			m_nview.Register("EmptyProcessed", RPC_EmptyProcessed);
			WearNTear component = GetComponent<WearNTear>();
			if ((bool)component)
			{
				component.m_onDestroyed = (Action)Delegate.Combine(component.m_onDestroyed, new Action(OnDestroyed));
			}
			InvokeRepeating("UpdateSmelter", 1f, 1f);
		}
	}

	private void DropAllItems()
	{
		SpawnProcessed();
		if (m_fuelItem != null)
		{
			float @float = m_nview.GetZDO().GetFloat("fuel");
			for (int i = 0; i < (int)@float; i++)
			{
				Vector3 position = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate(m_fuelItem.gameObject, position, rotation);
			}
		}
		while (GetQueueSize() > 0)
		{
			string queuedOre = GetQueuedOre();
			RemoveOneOre();
			ItemConversion itemConversion = GetItemConversion(queuedOre);
			if (itemConversion != null)
			{
				Vector3 position2 = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion rotation2 = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate(itemConversion.m_from.gameObject, position2, rotation2);
			}
			else
			{
				ZLog.Log((object)("Invalid ore in smelter " + queuedOre));
			}
		}
	}

	private void OnDestroyed()
	{
		if (m_nview.IsOwner())
		{
			DropAllItems();
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

	private bool OnAddOre(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			item = FindCookableItem(user.GetInventory());
			if (item == null)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems");
				return false;
			}
		}
		if (!IsItemAllowed(item.m_dropPrefab.name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wontwork");
			return false;
		}
		ZLog.Log((object)("trying to add " + item.m_shared.m_name));
		if (GetQueueSize() >= m_maxOre)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name);
		user.GetInventory().RemoveItem(item, 1);
		m_nview.InvokeRPC("AddOre", item.m_dropPrefab.name);
		return true;
	}

	private float GetBakeTimer()
	{
		return m_nview.GetZDO().GetFloat("bakeTimer");
	}

	private void SetBakeTimer(float t)
	{
		m_nview.GetZDO().Set("bakeTimer", t);
	}

	private float GetFuel()
	{
		return m_nview.GetZDO().GetFloat("fuel");
	}

	private void SetFuel(float fuel)
	{
		m_nview.GetZDO().Set("fuel", fuel);
	}

	private int GetQueueSize()
	{
		return m_nview.GetZDO().GetInt("queued");
	}

	private void RPC_AddOre(long sender, string name)
	{
		if (m_nview.IsOwner())
		{
			if (!IsItemAllowed(name))
			{
				ZLog.Log((object)("Item not allowed " + name));
				return;
			}
			QueueOre(name);
			m_oreAddedEffects.Create(base.transform.position, base.transform.rotation);
			ZLog.Log((object)("Added ore " + name));
		}
	}

	private void QueueOre(string name)
	{
		int queueSize = GetQueueSize();
		m_nview.GetZDO().Set("item" + queueSize, name);
		m_nview.GetZDO().Set("queued", queueSize + 1);
	}

	private string GetQueuedOre()
	{
		if (GetQueueSize() == 0)
		{
			return "";
		}
		return m_nview.GetZDO().GetString("item0");
	}

	private void RemoveOneOre()
	{
		int queueSize = GetQueueSize();
		if (queueSize != 0)
		{
			for (int i = 0; i < queueSize; i++)
			{
				string @string = m_nview.GetZDO().GetString("item" + (i + 1));
				m_nview.GetZDO().Set("item" + i, @string);
			}
			m_nview.GetZDO().Set("queued", queueSize - 1);
		}
	}

	private bool OnEmpty(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (GetProcessedQueueSize() <= 0)
		{
			return false;
		}
		m_nview.InvokeRPC("EmptyProcessed");
		return true;
	}

	private void RPC_EmptyProcessed(long sender)
	{
		if (m_nview.IsOwner())
		{
			SpawnProcessed();
		}
	}

	private bool OnAddFuel(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (item != null && item.m_shared.m_name != m_fuelItem.m_itemData.m_shared.m_name)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wrongitem");
			return false;
		}
		if (GetFuel() > (float)(m_maxFuel - 1))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
			return false;
		}
		if (!user.GetInventory().HaveItem(m_fuelItem.m_itemData.m_shared.m_name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + m_fuelItem.m_itemData.m_shared.m_name);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + m_fuelItem.m_itemData.m_shared.m_name);
		user.GetInventory().RemoveItem(m_fuelItem.m_itemData.m_shared.m_name, 1);
		m_nview.InvokeRPC("AddFuel");
		return true;
	}

	private void RPC_AddFuel(long sender)
	{
		if (m_nview.IsOwner())
		{
			float fuel = GetFuel();
			SetFuel(fuel + 1f);
			m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform);
		}
	}

	private double GetDeltaTime()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(m_nview.GetZDO().GetLong("StartTime", time.Ticks));
		double totalSeconds = (time - d).TotalSeconds;
		m_nview.GetZDO().Set("StartTime", time.Ticks);
		return totalSeconds;
	}

	private float GetAccumulator()
	{
		return m_nview.GetZDO().GetFloat("accTime");
	}

	private void SetAccumulator(float t)
	{
		m_nview.GetZDO().Set("accTime", t);
	}

	private void UpdateRoof()
	{
		if (m_requiresRoof)
		{
			m_haveRoof = Cover.IsUnderRoof(m_roofCheckPoint.position);
		}
	}

	private void UpdateSmelter()
	{
		UpdateRoof();
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		double deltaTime = GetDeltaTime();
		float accumulator = GetAccumulator();
		accumulator += (float)deltaTime;
		float num = (m_windmill ? m_windmill.GetPowerOutput() : 1f);
		while (accumulator >= 1f)
		{
			accumulator -= 1f;
			float fuel = GetFuel();
			string queuedOre = GetQueuedOre();
			if ((m_maxFuel != 0 && !(fuel > 0f)) || !(queuedOre != "") || !(m_secPerProduct > 0f) || (m_requiresRoof && !m_haveRoof))
			{
				continue;
			}
			float num2 = 1f * num;
			if (m_maxFuel > 0)
			{
				float num3 = m_secPerProduct / (float)m_fuelPerProduct;
				fuel -= num2 / num3;
				if (fuel < 0f)
				{
					fuel = 0f;
				}
				SetFuel(fuel);
			}
			float bakeTimer = GetBakeTimer();
			bakeTimer += num2;
			SetBakeTimer(bakeTimer);
			if (bakeTimer > m_secPerProduct)
			{
				SetBakeTimer(0f);
				RemoveOneOre();
				QueueProcessed(queuedOre);
			}
		}
		if (GetQueuedOre() == "" || ((float)m_maxFuel > 0f && GetFuel() == 0f))
		{
			SpawnProcessed();
		}
		SetAccumulator(accumulator);
	}

	private void QueueProcessed(string ore)
	{
		if (!m_spawnStack)
		{
			Spawn(ore, 1);
			return;
		}
		string @string = m_nview.GetZDO().GetString("SpawnOre");
		int @int = m_nview.GetZDO().GetInt("SpawnAmount");
		if (@string.Length > 0)
		{
			if (@string != ore)
			{
				SpawnProcessed();
				m_nview.GetZDO().Set("SpawnOre", ore);
				m_nview.GetZDO().Set("SpawnAmount", 1);
				return;
			}
			@int++;
			ItemConversion itemConversion = GetItemConversion(ore);
			if (itemConversion == null || @int >= itemConversion.m_to.m_itemData.m_shared.m_maxStackSize)
			{
				Spawn(ore, @int);
				m_nview.GetZDO().Set("SpawnOre", "");
				m_nview.GetZDO().Set("SpawnAmount", 0);
			}
			else
			{
				m_nview.GetZDO().Set("SpawnAmount", @int);
			}
		}
		else
		{
			m_nview.GetZDO().Set("SpawnOre", ore);
			m_nview.GetZDO().Set("SpawnAmount", 1);
		}
	}

	private void SpawnProcessed()
	{
		int @int = m_nview.GetZDO().GetInt("SpawnAmount");
		if (@int > 0)
		{
			string @string = m_nview.GetZDO().GetString("SpawnOre");
			Spawn(@string, @int);
			m_nview.GetZDO().Set("SpawnOre", "");
			m_nview.GetZDO().Set("SpawnAmount", 0);
		}
	}

	private int GetProcessedQueueSize()
	{
		return m_nview.GetZDO().GetInt("SpawnAmount");
	}

	private void Spawn(string ore, int stack)
	{
		ItemConversion itemConversion = GetItemConversion(ore);
		if (itemConversion != null)
		{
			m_produceEffects.Create(base.transform.position, base.transform.rotation);
			UnityEngine.Object.Instantiate(itemConversion.m_to.gameObject, m_outputPoint.position, m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
		}
	}

	private void FixedUpdate()
	{
		if (m_nview.IsValid())
		{
			UpdateState();
			UpdateHoverTexts();
		}
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

	private void UpdateState()
	{
		bool flag = IsActive();
		m_enabledObject.SetActive(flag);
		Animator[] animators = m_animators;
		foreach (Animator val in animators)
		{
			if (((Component)(object)val).gameObject.activeInHierarchy)
			{
				val.SetBool("active", flag);
			}
		}
	}

	public bool IsActive()
	{
		if ((m_maxFuel == 0 || GetFuel() > 0f) && GetQueueSize() > 0)
		{
			if (m_requiresRoof)
			{
				return m_haveRoof;
			}
			return true;
		}
		return false;
	}

	private void UpdateHoverTexts()
	{
		if ((bool)m_addWoodSwitch)
		{
			float fuel = GetFuel();
			m_addWoodSwitch.m_hoverText = m_name + " (" + m_fuelItem.m_itemData.m_shared.m_name + " " + Mathf.Ceil(fuel) + "/" + m_maxFuel + ")\n[<color=yellow><b>$KEY_Use</b></color>] $piece_smelter_add " + m_fuelItem.m_itemData.m_shared.m_name;
		}
		if ((bool)m_emptyOreSwitch && m_spawnStack)
		{
			int processedQueueSize = GetProcessedQueueSize();
			m_emptyOreSwitch.m_hoverText = m_name + " (" + processedQueueSize + " $piece_smelter_ready \n[<color=yellow><b>$KEY_Use</b></color>] " + m_emptyOreTooltip;
		}
		int queueSize = GetQueueSize();
		m_addOreSwitch.m_hoverText = m_name + " (" + queueSize + "/" + m_maxOre + ") ";
		if (m_requiresRoof && !m_haveRoof && Mathf.Sin(Time.time * 10f) > 0f)
		{
			m_addOreSwitch.m_hoverText += " <color=yellow>$piece_smelter_reqroof</color>";
		}
		Switch addOreSwitch = m_addOreSwitch;
		addOreSwitch.m_hoverText = addOreSwitch.m_hoverText + "\n[<color=yellow><b>$KEY_Use</b></color>] " + m_addOreTooltip;
	}
}
