using UnityEngine;

public class ShipControlls : MonoBehaviour, Interactable, Hoverable
{
	public string m_hoverText = "";

	public Ship m_ship;

	public float m_maxUseRange = 10f;

	public Transform m_attachPoint;

	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	public string m_attachAnimation = "attach_chair";

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = m_ship.GetComponent<ZNetView>();
		m_nview.Register<ZDOID>("RequestControl", RPC_RequestControl);
		m_nview.Register<ZDOID>("ReleaseControl", RPC_ReleaseControl);
		m_nview.Register<bool>("RequestRespons", RPC_RequestRespons);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool Interact(Humanoid character, bool repeat)
	{
		if (repeat)
		{
			return false;
		}
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (!InUseDistance(character))
		{
			return false;
		}
		Player player = character as Player;
		if (player == null)
		{
			return false;
		}
		if (player.IsEncumbered())
		{
			return false;
		}
		if (player.GetStandingOnShip() != m_ship)
		{
			return false;
		}
		m_nview.InvokeRPC("RequestControl", player.GetZDOID());
		return false;
	}

	public Ship GetShip()
	{
		return m_ship;
	}

	public string GetHoverText()
	{
		if (!InUseDistance(Player.m_localPlayer))
		{
			return Localization.get_instance().Localize("<color=grey>$piece_toofar</color>");
		}
		return Localization.get_instance().Localize("[<color=yellow><b>$KEY_Use</b></color>] " + m_hoverText);
	}

	public string GetHoverName()
	{
		return Localization.get_instance().Localize(m_hoverText);
	}

	private void RPC_RequestControl(long sender, ZDOID playerID)
	{
		if (m_nview.IsOwner() && m_ship.IsPlayerInBoat(playerID))
		{
			if (GetUser() == playerID || !HaveValidUser())
			{
				m_nview.GetZDO().Set("user", playerID);
				m_nview.InvokeRPC(sender, "RequestRespons", true);
			}
			else
			{
				m_nview.InvokeRPC(sender, "RequestRespons", false);
			}
		}
	}

	private void RPC_ReleaseControl(long sender, ZDOID playerID)
	{
		if (m_nview.IsOwner() && GetUser() == playerID)
		{
			m_nview.GetZDO().Set("user", ZDOID.None);
		}
	}

	private void RPC_RequestRespons(long sender, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			Player.m_localPlayer.StartShipControl(this);
			if (m_attachPoint != null)
			{
				Player.m_localPlayer.AttachStart(m_attachPoint, hideWeapons: false, isBed: false, m_attachAnimation, m_detachOffset);
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
		}
	}

	public void OnUseStop(Player player)
	{
		if (m_nview.IsValid())
		{
			m_nview.InvokeRPC("ReleaseControl", player.GetZDOID());
			if (m_attachPoint != null)
			{
				player.AttachStop();
			}
		}
	}

	public bool HaveValidUser()
	{
		ZDOID user = GetUser();
		if (user.IsNone())
		{
			return false;
		}
		return m_ship.IsPlayerInBoat(user);
	}

	public bool IsLocalUser()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		ZDOID user = GetUser();
		if (user.IsNone())
		{
			return false;
		}
		return user == Player.m_localPlayer.GetZDOID();
	}

	private ZDOID GetUser()
	{
		if (!m_nview.IsValid())
		{
			return ZDOID.None;
		}
		return m_nview.GetZDO().GetZDOID("user");
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, m_attachPoint.position) < m_maxUseRange;
	}
}
