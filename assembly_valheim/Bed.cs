using UnityEngine;

public class Bed : MonoBehaviour, Hoverable, Interactable
{
	public Transform m_spawnPoint;

	public float m_monsterCheckRadius = 20f;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			m_nview.Register<long, string>("SetOwner", RPC_SetOwner);
		}
	}

	public string GetHoverText()
	{
		string ownerName = GetOwnerName();
		if (ownerName == "")
		{
			return Localization.get_instance().Localize("$piece_bed_unclaimed\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_claim");
		}
		string text = ownerName + "'s $piece_bed";
		if (IsMine())
		{
			if (IsCurrent())
			{
				return Localization.get_instance().Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_sleep");
			}
			return Localization.get_instance().Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_setspawn");
		}
		return Localization.get_instance().Localize(text);
	}

	public string GetHoverName()
	{
		return Localization.get_instance().Localize("$piece_bed");
	}

	public bool Interact(Humanoid human, bool repeat)
	{
		if (repeat)
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		long owner = GetOwner();
		Player human2 = human as Player;
		if (owner == 0L)
		{
			ZLog.Log((object)"Has no creator");
			if (!CheckExposure(human2))
			{
				return false;
			}
			SetOwner(playerID, Game.instance.GetPlayerProfile().GetName());
			Game.instance.GetPlayerProfile().SetCustomSpawnPoint(GetSpawnPoint());
			human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset");
		}
		else if (IsMine())
		{
			ZLog.Log((object)"Is mine");
			if (IsCurrent())
			{
				ZLog.Log((object)"is current spawnpoint");
				if (!EnvMan.instance.IsAfternoon() && !EnvMan.instance.IsNight())
				{
					human.Message(MessageHud.MessageType.Center, "$msg_cantsleep");
					return false;
				}
				if (!CheckEnemies(human2))
				{
					return false;
				}
				if (!CheckExposure(human2))
				{
					return false;
				}
				if (!CheckFire(human2))
				{
					return false;
				}
				if (!CheckWet(human2))
				{
					return false;
				}
				human.AttachStart(m_spawnPoint, hideWeapons: true, isBed: true, "attach_bed", new Vector3(0f, 0.5f, 0f));
				return false;
			}
			ZLog.Log((object)"Not current spawn point");
			if (!CheckExposure(human2))
			{
				return false;
			}
			Game.instance.GetPlayerProfile().SetCustomSpawnPoint(GetSpawnPoint());
			human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset");
		}
		return false;
	}

	private bool CheckWet(Player human)
	{
		if (human.GetSEMan().HaveStatusEffect("Wet"))
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedwet");
			return false;
		}
		return true;
	}

	private bool CheckEnemies(Player human)
	{
		if (human.IsSensed())
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedenemiesnearby");
			return false;
		}
		return true;
	}

	private bool CheckExposure(Player human)
	{
		float num = default(float);
		bool flag = default(bool);
		Cover.GetCoverForPoint(GetSpawnPoint(), ref num, ref flag);
		if (!flag)
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedneedroof");
			return false;
		}
		if (num < 0.8f)
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedtooexposed");
			return false;
		}
		ZLog.Log((object)("exporeusre check " + num + "  " + flag.ToString()));
		return true;
	}

	private bool CheckFire(Player human)
	{
		if (!EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Heat))
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bednofire");
			return false;
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool IsCurrent()
	{
		if (!IsMine())
		{
			return false;
		}
		return Vector3.Distance(GetSpawnPoint(), Game.instance.GetPlayerProfile().GetCustomSpawnPoint()) < 1f;
	}

	public Vector3 GetSpawnPoint()
	{
		return m_spawnPoint.position;
	}

	private bool IsMine()
	{
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		long owner = GetOwner();
		return playerID == owner;
	}

	private void SetOwner(long uid, string name)
	{
		m_nview.InvokeRPC("SetOwner", uid, name);
	}

	private void RPC_SetOwner(long sender, long uid, string name)
	{
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set("owner", uid);
			m_nview.GetZDO().Set("ownerName", name);
		}
	}

	private long GetOwner()
	{
		return m_nview.GetZDO().GetLong("owner", 0L);
	}

	private string GetOwnerName()
	{
		return m_nview.GetZDO().GetString("ownerName");
	}
}
