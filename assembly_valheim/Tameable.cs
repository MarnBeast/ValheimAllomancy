using System;
using UnityEngine;

public class Tameable : MonoBehaviour, Interactable
{
	private const float m_playerMaxDistance = 15f;

	private const float m_tameDeltaTime = 3f;

	public float m_fedDuration = 30f;

	public float m_tamingTime = 1800f;

	public EffectList m_tamedEffect = new EffectList();

	public EffectList m_sootheEffect = new EffectList();

	public EffectList m_petEffect = new EffectList();

	public bool m_commandable;

	private Character m_character;

	private MonsterAI m_monsterAI;

	private ZNetView m_nview;

	private float m_lastPetTime;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_character = GetComponent<Character>();
		m_monsterAI = GetComponent<MonsterAI>();
		MonsterAI monsterAI = m_monsterAI;
		monsterAI.m_onConsumedItem = (Action<ItemDrop>)Delegate.Combine(monsterAI.m_onConsumedItem, new Action<ItemDrop>(OnConsumedItem));
		if (m_nview.IsValid())
		{
			m_nview.Register<ZDOID>("Command", RPC_Command);
			InvokeRepeating("TamingUpdate", 3f, 3f);
		}
	}

	public string GetHoverText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		string str = Localization.get_instance().Localize(m_character.m_name);
		if (m_character.IsTamed())
		{
			str += Localization.get_instance().Localize(" ( $hud_tame, " + GetStatusString() + " )");
			return str + Localization.get_instance().Localize("\n[<color=yellow><b>$KEY_Use</b></color>] $hud_pet");
		}
		int tameness = GetTameness();
		if (tameness <= 0)
		{
			return str + Localization.get_instance().Localize(" ( $hud_wild, " + GetStatusString() + " )");
		}
		return str + Localization.get_instance().Localize(" ( $hud_tameness  " + tameness + "%, " + GetStatusString() + " )");
	}

	private string GetStatusString()
	{
		if (m_monsterAI.IsAlerted())
		{
			return "$hud_tamefrightened";
		}
		if (IsHungry())
		{
			return "$hud_tamehungry";
		}
		if (m_character.IsTamed())
		{
			return "$hud_tamehappy";
		}
		return "$hud_tameinprogress";
	}

	public bool Interact(Humanoid user, bool hold)
	{
		if (hold)
		{
			return false;
		}
		if (!m_nview.IsValid())
		{
			return false;
		}
		string hoverName = m_character.GetHoverName();
		if (m_character.IsTamed())
		{
			if (Time.time - m_lastPetTime > 1f)
			{
				m_lastPetTime = Time.time;
				m_petEffect.Create(m_character.GetCenterPoint(), Quaternion.identity);
				if (m_commandable)
				{
					Command(user);
				}
				else
				{
					user.Message(MessageHud.MessageType.Center, hoverName + " $hud_tamelove");
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void TamingUpdate()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && !m_character.IsTamed() && !IsHungry() && !m_monsterAI.IsAlerted())
		{
			DecreaseRemainingTime(3f);
			if (GetRemainingTime() <= 0f)
			{
				Tame();
			}
			else
			{
				m_sootheEffect.Create(m_character.GetCenterPoint(), Quaternion.identity);
			}
		}
	}

	public void Tame()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && !m_character.IsTamed())
		{
			m_monsterAI.MakeTame();
			m_tamedEffect.Create(m_character.GetCenterPoint(), Quaternion.identity);
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 30f);
			if ((bool)closestPlayer)
			{
				closestPlayer.Message(MessageHud.MessageType.Center, m_character.m_name + " $hud_tamedone");
			}
		}
	}

	public static void TameAllInArea(Vector3 point, float radius)
	{
		foreach (Character allCharacter in Character.GetAllCharacters())
		{
			if (!allCharacter.IsPlayer())
			{
				Tameable component = allCharacter.GetComponent<Tameable>();
				if ((bool)component)
				{
					component.Tame();
				}
			}
		}
	}

	private void Command(Humanoid user)
	{
		m_nview.InvokeRPC("Command", user.GetZDOID());
	}

	private Player GetPlayer(ZDOID characterID)
	{
		GameObject gameObject = ZNetScene.instance.FindInstance(characterID);
		if ((bool)gameObject)
		{
			return gameObject.GetComponent<Player>();
		}
		return null;
	}

	private void RPC_Command(long sender, ZDOID characterID)
	{
		Player player = GetPlayer(characterID);
		if (!(player == null))
		{
			if ((bool)m_monsterAI.GetFollowTarget())
			{
				m_monsterAI.SetFollowTarget(null);
				m_monsterAI.SetPatrolPoint();
				player.Message(MessageHud.MessageType.Center, m_character.GetHoverName() + " $hud_tamestay");
			}
			else
			{
				m_monsterAI.ResetPatrolPoint();
				m_monsterAI.SetFollowTarget(player.gameObject);
				player.Message(MessageHud.MessageType.Center, m_character.GetHoverName() + " $hud_tamefollow");
			}
		}
	}

	public bool IsHungry()
	{
		DateTime d = new DateTime(m_nview.GetZDO().GetLong("TameLastFeeding", 0L));
		return (ZNet.instance.GetTime() - d).TotalSeconds > (double)m_fedDuration;
	}

	private void ResetFeedingTimer()
	{
		m_nview.GetZDO().Set("TameLastFeeding", ZNet.instance.GetTime().Ticks);
	}

	private int GetTameness()
	{
		float remainingTime = GetRemainingTime();
		return (int)((1f - Mathf.Clamp01(remainingTime / m_tamingTime)) * 100f);
	}

	private void OnConsumedItem(ItemDrop item)
	{
		if (IsHungry())
		{
			m_sootheEffect.Create(m_character.GetCenterPoint(), Quaternion.identity);
		}
		ResetFeedingTimer();
	}

	private void DecreaseRemainingTime(float time)
	{
		float remainingTime = GetRemainingTime();
		remainingTime -= time;
		if (remainingTime < 0f)
		{
			remainingTime = 0f;
		}
		m_nview.GetZDO().Set("TameTimeLeft", remainingTime);
	}

	private float GetRemainingTime()
	{
		return m_nview.GetZDO().GetFloat("TameTimeLeft", m_tamingTime);
	}
}
