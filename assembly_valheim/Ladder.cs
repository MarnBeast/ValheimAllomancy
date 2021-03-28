using UnityEngine;

public class Ladder : MonoBehaviour, Interactable, Hoverable
{
	public Transform m_targetPos;

	public string m_name = "Ladder";

	public float m_useDistance = 2f;

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!InUseDistance(character))
		{
			return false;
		}
		character.transform.position = m_targetPos.position;
		character.transform.rotation = m_targetPos.rotation;
		character.SetLookDir(m_targetPos.forward);
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public string GetHoverText()
	{
		if (!InUseDistance(Player.m_localPlayer))
		{
			return Localization.get_instance().Localize("<color=grey>$piece_toofar</color>");
		}
		return Localization.get_instance().Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, base.transform.position) < m_useDistance;
	}
}
