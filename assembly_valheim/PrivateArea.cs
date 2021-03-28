using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PrivateArea : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "Guard stone";

	public float m_radius = 10f;

	public float m_updateConnectionsInterval = 5f;

	public GameObject m_enabledEffect;

	public CircleProjector m_areaMarker;

	public EffectList m_flashEffect = new EffectList();

	public EffectList m_activateEffect = new EffectList();

	public EffectList m_deactivateEffect = new EffectList();

	public EffectList m_addPermittedEffect = new EffectList();

	public EffectList m_removedPermittedEffect = new EffectList();

	public GameObject m_connectEffect;

	public GameObject m_inRangeEffect;

	public MeshRenderer m_model;

	private ZNetView m_nview;

	private Piece m_piece;

	private bool m_flashAvailable = true;

	private bool m_tempChecked;

	private List<GameObject> m_connectionInstances = new List<GameObject>();

	private float m_connectionUpdateTime = -1000f;

	private List<PrivateArea> m_connectedAreas = new List<PrivateArea>();

	private static List<PrivateArea> m_allAreas = new List<PrivateArea>();

	private void Awake()
	{
		if ((bool)m_areaMarker)
		{
			m_areaMarker.m_radius = m_radius;
		}
		m_nview = GetComponent<ZNetView>();
		if (m_nview.IsValid())
		{
			WearNTear component = GetComponent<WearNTear>();
			component.m_onDamaged = (Action)Delegate.Combine(component.m_onDamaged, new Action(OnDamaged));
			m_piece = GetComponent<Piece>();
			if ((bool)m_areaMarker)
			{
				m_areaMarker.gameObject.SetActive(value: false);
			}
			if ((bool)m_inRangeEffect)
			{
				m_inRangeEffect.SetActive(value: false);
			}
			m_allAreas.Add(this);
			InvokeRepeating("UpdateStatus", 0f, 1f);
			m_nview.Register<long>("ToggleEnabled", RPC_ToggleEnabled);
			m_nview.Register<long, string>("TogglePermitted", RPC_TogglePermitted);
			m_nview.Register("FlashShield", RPC_FlashShield);
		}
	}

	private void OnDestroy()
	{
		m_allAreas.Remove(this);
	}

	private void UpdateStatus()
	{
		bool flag = IsEnabled();
		m_enabledEffect.SetActive(flag);
		m_flashAvailable = true;
		Material[] materials = m_model.materials;
		foreach (Material material in materials)
		{
			if (flag)
			{
				material.EnableKeyword("_EMISSION");
			}
			else
			{
				material.DisableKeyword("_EMISSION");
			}
		}
	}

	public string GetHoverText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		if (Player.m_localPlayer == null)
		{
			return "";
		}
		ShowAreaMarker();
		StringBuilder stringBuilder = new StringBuilder(256);
		if (m_piece.IsCreator())
		{
			if (IsEnabled())
			{
				stringBuilder.Append(m_name + " ( $piece_guardstone_active )");
				stringBuilder.Append("\n$piece_guardstone_owner:" + GetCreatorName());
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_deactivate");
			}
			else
			{
				stringBuilder.Append(m_name + " ($piece_guardstone_inactive )");
				stringBuilder.Append("\n$piece_guardstone_owner:" + GetCreatorName());
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_activate");
			}
		}
		else if (IsEnabled())
		{
			stringBuilder.Append(m_name + " ( $piece_guardstone_active )");
			stringBuilder.Append("\n$piece_guardstone_owner:" + GetCreatorName());
		}
		else
		{
			stringBuilder.Append(m_name + " ( $piece_guardstone_inactive )");
			stringBuilder.Append("\n$piece_guardstone_owner:" + GetCreatorName());
			if (IsPermitted(Player.m_localPlayer.GetPlayerID()))
			{
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_remove");
			}
			else
			{
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_add");
			}
		}
		AddUserList(stringBuilder);
		return Localization.get_instance().Localize(stringBuilder.ToString());
	}

	private void AddUserList(StringBuilder text)
	{
		List<KeyValuePair<long, string>> permittedPlayers = GetPermittedPlayers();
		text.Append("\n$piece_guardstone_additional: ");
		for (int i = 0; i < permittedPlayers.Count; i++)
		{
			text.Append(permittedPlayers[i].Value);
			if (i != permittedPlayers.Count - 1)
			{
				text.Append(", ");
			}
		}
	}

	private void RemovePermitted(long playerID)
	{
		List<KeyValuePair<long, string>> permittedPlayers = GetPermittedPlayers();
		if (permittedPlayers.RemoveAll((KeyValuePair<long, string> x) => x.Key == playerID) > 0)
		{
			SetPermittedPlayers(permittedPlayers);
			m_removedPermittedEffect.Create(base.transform.position, base.transform.rotation);
		}
	}

	private bool IsPermitted(long playerID)
	{
		foreach (KeyValuePair<long, string> permittedPlayer in GetPermittedPlayers())
		{
			if (permittedPlayer.Key == playerID)
			{
				return true;
			}
		}
		return false;
	}

	private void AddPermitted(long playerID, string playerName)
	{
		List<KeyValuePair<long, string>> permittedPlayers = GetPermittedPlayers();
		foreach (KeyValuePair<long, string> item in permittedPlayers)
		{
			if (item.Key == playerID)
			{
				return;
			}
		}
		permittedPlayers.Add(new KeyValuePair<long, string>(playerID, playerName));
		SetPermittedPlayers(permittedPlayers);
		m_addPermittedEffect.Create(base.transform.position, base.transform.rotation);
	}

	private void SetPermittedPlayers(List<KeyValuePair<long, string>> users)
	{
		m_nview.GetZDO().Set("permitted", users.Count);
		for (int i = 0; i < users.Count; i++)
		{
			KeyValuePair<long, string> keyValuePair = users[i];
			m_nview.GetZDO().Set("pu_id" + i, keyValuePair.Key);
			m_nview.GetZDO().Set("pu_name" + i, keyValuePair.Value);
		}
	}

	private List<KeyValuePair<long, string>> GetPermittedPlayers()
	{
		List<KeyValuePair<long, string>> list = new List<KeyValuePair<long, string>>();
		int @int = m_nview.GetZDO().GetInt("permitted");
		for (int i = 0; i < @int; i++)
		{
			long @long = m_nview.GetZDO().GetLong("pu_id" + i, 0L);
			string @string = m_nview.GetZDO().GetString("pu_name" + i);
			if (@long != 0L)
			{
				list.Add(new KeyValuePair<long, string>(@long, @string));
			}
		}
		return list;
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid human, bool hold)
	{
		if (hold)
		{
			return false;
		}
		Player player = human as Player;
		if (m_piece.IsCreator())
		{
			m_nview.InvokeRPC("ToggleEnabled", player.GetPlayerID());
			return true;
		}
		if (IsEnabled())
		{
			return false;
		}
		m_nview.InvokeRPC("TogglePermitted", player.GetPlayerID(), player.GetPlayerName());
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void RPC_TogglePermitted(long uid, long playerID, string name)
	{
		if (m_nview.IsOwner() && !IsEnabled())
		{
			if (IsPermitted(playerID))
			{
				RemovePermitted(playerID);
			}
			else
			{
				AddPermitted(playerID, name);
			}
		}
	}

	private void RPC_ToggleEnabled(long uid, long playerID)
	{
		ZLog.Log((object)("Toggle enabled from " + playerID + "  creator is " + m_piece.GetCreator()));
		if (m_nview.IsOwner() && m_piece.GetCreator() == playerID)
		{
			SetEnabled(!IsEnabled());
		}
	}

	public bool IsEnabled()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetBool("enabled");
	}

	private void SetEnabled(bool enabled)
	{
		m_nview.GetZDO().Set("enabled", enabled);
		UpdateStatus();
		if (enabled)
		{
			m_activateEffect.Create(base.transform.position, base.transform.rotation);
		}
		else
		{
			m_deactivateEffect.Create(base.transform.position, base.transform.rotation);
		}
	}

	public void Setup(string name)
	{
		m_nview.GetZDO().Set("creatorName", name);
	}

	public void PokeAllAreasInRange()
	{
		foreach (PrivateArea allArea in m_allAreas)
		{
			if (!(allArea == this) && IsInside(allArea.transform.position, 0f))
			{
				allArea.StartInRangeEffect();
			}
		}
	}

	private void StartInRangeEffect()
	{
		m_inRangeEffect.SetActive(value: true);
		CancelInvoke("StopInRangeEffect");
		Invoke("StopInRangeEffect", 0.2f);
	}

	private void StopInRangeEffect()
	{
		m_inRangeEffect.SetActive(value: false);
	}

	public void PokeConnectionEffects()
	{
		List<PrivateArea> connectedAreas = GetConnectedAreas();
		StartConnectionEffects();
		foreach (PrivateArea item in connectedAreas)
		{
			item.StartConnectionEffects();
		}
	}

	private void StartConnectionEffects()
	{
		List<PrivateArea> list = new List<PrivateArea>();
		foreach (PrivateArea allArea in m_allAreas)
		{
			if (!(allArea == this) && IsInside(allArea.transform.position, 0f))
			{
				list.Add(allArea);
			}
		}
		Vector3 vector = base.transform.position + Vector3.up * 1.4f;
		if (m_connectionInstances.Count != list.Count)
		{
			StopConnectionEffects();
			for (int i = 0; i < list.Count; i++)
			{
				GameObject item = UnityEngine.Object.Instantiate(m_connectEffect, vector, Quaternion.identity, base.transform);
				m_connectionInstances.Add(item);
			}
		}
		if (m_connectionInstances.Count != 0)
		{
			for (int j = 0; j < list.Count; j++)
			{
				Vector3 vector2 = list[j].transform.position + Vector3.up * 1.4f - vector;
				Quaternion rotation = Quaternion.LookRotation(vector2.normalized);
				GameObject gameObject = m_connectionInstances[j];
				gameObject.transform.position = vector;
				gameObject.transform.rotation = rotation;
				gameObject.transform.localScale = new Vector3(1f, 1f, vector2.magnitude);
			}
			CancelInvoke("StopConnectionEffects");
			Invoke("StopConnectionEffects", 0.3f);
		}
	}

	private void StopConnectionEffects()
	{
		foreach (GameObject connectionInstance in m_connectionInstances)
		{
			UnityEngine.Object.Destroy(connectionInstance);
		}
		m_connectionInstances.Clear();
	}

	private string GetCreatorName()
	{
		return m_nview.GetZDO().GetString("creatorName");
	}

	public static bool CheckInPrivateArea(Vector3 point, bool flash = false)
	{
		foreach (PrivateArea allArea in m_allAreas)
		{
			if (allArea.IsEnabled() && allArea.IsInside(point, 0f))
			{
				if (flash)
				{
					allArea.FlashShield(flashConnected: false);
				}
				return true;
			}
		}
		return false;
	}

	public static bool CheckAccess(Vector3 point, float radius = 0f, bool flash = true)
	{
		bool flag = false;
		List<PrivateArea> list = new List<PrivateArea>();
		foreach (PrivateArea allArea in m_allAreas)
		{
			if (allArea.IsEnabled() && allArea.IsInside(point, radius))
			{
				if (allArea.HaveLocalAccess())
				{
					flag = true;
				}
				else
				{
					list.Add(allArea);
				}
			}
		}
		if (!flag && list.Count > 0)
		{
			if (flash)
			{
				foreach (PrivateArea item in list)
				{
					item.FlashShield(flashConnected: false);
				}
			}
			return false;
		}
		return true;
	}

	private bool HaveLocalAccess()
	{
		if (m_piece.IsCreator())
		{
			return true;
		}
		if (IsPermitted(Player.m_localPlayer.GetPlayerID()))
		{
			return true;
		}
		return false;
	}

	private List<PrivateArea> GetConnectedAreas(bool forceUpdate = false)
	{
		if (Time.time - m_connectionUpdateTime > m_updateConnectionsInterval || forceUpdate)
		{
			GetAllConnectedAreas(m_connectedAreas);
			m_connectionUpdateTime = Time.time;
		}
		return m_connectedAreas;
	}

	private void GetAllConnectedAreas(List<PrivateArea> areas)
	{
		Queue<PrivateArea> queue = new Queue<PrivateArea>();
		queue.Enqueue(this);
		foreach (PrivateArea allArea in m_allAreas)
		{
			allArea.m_tempChecked = false;
		}
		m_tempChecked = true;
		while (queue.Count > 0)
		{
			PrivateArea privateArea = queue.Dequeue();
			foreach (PrivateArea allArea2 in m_allAreas)
			{
				if (!allArea2.m_tempChecked && allArea2.IsEnabled() && allArea2.IsInside(privateArea.transform.position, 0f))
				{
					allArea2.m_tempChecked = true;
					queue.Enqueue(allArea2);
					areas.Add(allArea2);
				}
			}
		}
	}

	private void FlashShield(bool flashConnected)
	{
		if (!m_flashAvailable)
		{
			return;
		}
		m_flashAvailable = false;
		m_nview.InvokeRPC(ZNetView.Everybody, "FlashShield");
		if (!flashConnected)
		{
			return;
		}
		foreach (PrivateArea connectedArea in GetConnectedAreas())
		{
			if (connectedArea.m_nview.IsValid())
			{
				connectedArea.m_nview.InvokeRPC(ZNetView.Everybody, "FlashShield");
			}
		}
	}

	private void RPC_FlashShield(long uid)
	{
		m_flashEffect.Create(base.transform.position, Quaternion.identity);
	}

	private bool IsInside(Vector3 point, float radius)
	{
		return Utils.DistanceXZ(base.transform.position, point) < m_radius + radius;
	}

	public void ShowAreaMarker()
	{
		if ((bool)m_areaMarker)
		{
			m_areaMarker.gameObject.SetActive(value: true);
			CancelInvoke("HideMarker");
			Invoke("HideMarker", 0.5f);
		}
	}

	private void HideMarker()
	{
		m_areaMarker.gameObject.SetActive(value: false);
	}

	private void OnDamaged()
	{
		if (IsEnabled())
		{
			FlashShield(flashConnected: false);
		}
	}

	private void OnDrawGizmosSelected()
	{
	}
}
