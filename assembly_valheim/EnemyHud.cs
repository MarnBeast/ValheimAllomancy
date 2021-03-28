using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHud : MonoBehaviour
{
	private class HudData
	{
		public Character m_character;

		public BaseAI m_ai;

		public GameObject m_gui;

		public GameObject m_healthRoot;

		public RectTransform m_level2;

		public RectTransform m_level3;

		public RectTransform m_alerted;

		public RectTransform m_aware;

		public GuiBar m_healthFast;

		public GuiBar m_healthSlow;

		public Text m_name;

		public float m_hoverTimer = 99999f;
	}

	private static EnemyHud m_instance;

	public GameObject m_hudRoot;

	public GameObject m_baseHud;

	public GameObject m_baseHudBoss;

	public GameObject m_baseHudPlayer;

	public float m_maxShowDistance = 10f;

	public float m_maxShowDistanceBoss = 100f;

	public float m_hoverShowDuration = 60f;

	private Vector3 m_refPoint = Vector3.zero;

	private Dictionary<Character, HudData> m_huds = new Dictionary<Character, HudData>();

	public static EnemyHud instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_baseHud.SetActive(value: false);
		m_baseHudBoss.SetActive(value: false);
		m_baseHudPlayer.SetActive(value: false);
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void LateUpdate()
	{
		m_hudRoot.SetActive(!Hud.IsUserHidden());
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null)
		{
			m_refPoint = localPlayer.transform.position;
		}
		foreach (Character allCharacter in Character.GetAllCharacters())
		{
			if (!(allCharacter == localPlayer) && TestShow(allCharacter))
			{
				ShowHud(allCharacter);
			}
		}
		UpdateHuds(localPlayer, Time.deltaTime);
	}

	private bool TestShow(Character c)
	{
		float num = Vector3.SqrMagnitude(c.transform.position - m_refPoint);
		if (c.IsBoss() && num < m_maxShowDistanceBoss * m_maxShowDistanceBoss)
		{
			if (num < m_maxShowDistanceBoss * m_maxShowDistanceBoss && c.GetComponent<BaseAI>().IsAlerted())
			{
				return true;
			}
		}
		else if (num < m_maxShowDistance * m_maxShowDistance)
		{
			if (c.IsPlayer() && c.IsCrouching())
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private void ShowHud(Character c)
	{
		if (!m_huds.TryGetValue(c, out var value))
		{
			GameObject original = (c.IsPlayer() ? m_baseHudPlayer : ((!c.IsBoss()) ? m_baseHud : m_baseHudBoss));
			value = new HudData();
			value.m_character = c;
			value.m_ai = c.GetComponent<BaseAI>();
			value.m_gui = Object.Instantiate(original, m_hudRoot.transform);
			value.m_gui.SetActive(value: true);
			value.m_healthRoot = value.m_gui.transform.Find("Health").gameObject;
			value.m_healthFast = value.m_healthRoot.transform.Find("health_fast").GetComponent<GuiBar>();
			value.m_healthSlow = value.m_healthRoot.transform.Find("health_slow").GetComponent<GuiBar>();
			value.m_level2 = value.m_gui.transform.Find("level_2") as RectTransform;
			value.m_level3 = value.m_gui.transform.Find("level_3") as RectTransform;
			value.m_alerted = value.m_gui.transform.Find("Alerted") as RectTransform;
			value.m_aware = value.m_gui.transform.Find("Aware") as RectTransform;
			value.m_name = value.m_gui.transform.Find("Name").GetComponent<Text>();
			value.m_name.set_text(Localization.get_instance().Localize(c.GetHoverName()));
			m_huds.Add(c, value);
		}
	}

	private void UpdateHuds(Player player, float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!mainCamera)
		{
			return;
		}
		Character y = (player ? player.GetHoverCreature() : null);
		if ((bool)player)
		{
			player.IsCrouching();
		}
		Character character = null;
		foreach (KeyValuePair<Character, HudData> hud in m_huds)
		{
			HudData value = hud.Value;
			if (!value.m_character || !TestShow(value.m_character))
			{
				if (character == null)
				{
					character = value.m_character;
					Object.Destroy(value.m_gui);
				}
				continue;
			}
			if (value.m_character == y)
			{
				value.m_hoverTimer = 0f;
			}
			value.m_hoverTimer += dt;
			float healthPercentage = value.m_character.GetHealthPercentage();
			if (value.m_character.IsPlayer() || value.m_character.IsBoss() || value.m_hoverTimer < m_hoverShowDuration)
			{
				value.m_gui.SetActive(value: true);
				int level = value.m_character.GetLevel();
				if ((bool)value.m_level2)
				{
					value.m_level2.gameObject.SetActive(level == 2);
				}
				if ((bool)value.m_level3)
				{
					value.m_level3.gameObject.SetActive(level == 3);
				}
				if (!value.m_character.IsBoss() && !value.m_character.IsPlayer())
				{
					bool flag = value.m_character.GetBaseAI().HaveTarget();
					bool flag2 = value.m_character.GetBaseAI().IsAlerted();
					value.m_alerted.gameObject.SetActive(flag2);
					value.m_aware.gameObject.SetActive(!flag2 && flag);
				}
			}
			else
			{
				value.m_gui.SetActive(value: false);
			}
			value.m_healthSlow.SetValue(healthPercentage);
			value.m_healthFast.SetValue(healthPercentage);
			if (!value.m_character.IsBoss() && value.m_gui.activeSelf)
			{
				Vector3 zero = Vector3.zero;
				zero = ((!value.m_character.IsPlayer()) ? value.m_character.GetTopPoint() : (value.m_character.GetHeadPoint() + Vector3.up * 0.3f));
				Vector3 position = mainCamera.WorldToScreenPoint(zero);
				if (position.x < 0f || position.x > (float)Screen.width || position.y < 0f || position.y > (float)Screen.height || position.z > 0f)
				{
					value.m_gui.transform.position = position;
					value.m_gui.SetActive(value: true);
				}
				else
				{
					value.m_gui.SetActive(value: false);
				}
			}
		}
		if (character != null)
		{
			m_huds.Remove(character);
		}
	}

	public bool ShowingBossHud()
	{
		foreach (KeyValuePair<Character, HudData> hud in m_huds)
		{
			if ((bool)hud.Value.m_character && hud.Value.m_character.IsBoss())
			{
				return true;
			}
		}
		return false;
	}

	public Character GetActiveBoss()
	{
		foreach (KeyValuePair<Character, HudData> hud in m_huds)
		{
			if ((bool)hud.Value.m_character && hud.Value.m_character.IsBoss())
			{
				return hud.Value.m_character;
			}
		}
		return null;
	}
}
