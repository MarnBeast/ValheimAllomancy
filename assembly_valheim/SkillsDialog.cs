using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillsDialog : MonoBehaviour
{
	public RectTransform m_listRoot;

	public GameObject m_elementPrefab;

	public Text m_totalSkillText;

	public float m_spacing = 80f;

	private float m_baseListSize;

	private List<GameObject> m_elements = new List<GameObject>();

	private void Awake()
	{
		m_baseListSize = m_listRoot.rect.height;
	}

	public void Setup(Player player)
	{
		base.gameObject.SetActive(value: true);
		foreach (GameObject element in m_elements)
		{
			Object.Destroy(element);
		}
		m_elements.Clear();
		List<Skills.Skill> skillList = player.GetSkills().GetSkillList();
		for (int i = 0; i < skillList.Count; i++)
		{
			Skills.Skill skill = skillList[i];
			GameObject gameObject = Object.Instantiate(m_elementPrefab, Vector3.zero, Quaternion.identity, m_listRoot);
			gameObject.SetActive(value: true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)(-i) * m_spacing);
			gameObject.GetComponentInChildren<UITooltip>().m_text = skill.m_info.m_description;
			Utils.FindChild(gameObject.transform, "icon").GetComponent<Image>().set_sprite(skill.m_info.m_icon);
			Utils.FindChild(gameObject.transform, "name").GetComponent<Text>().set_text(Localization.get_instance().Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower()));
			Utils.FindChild(gameObject.transform, "leveltext").GetComponent<Text>().set_text(((int)skill.m_level).ToString());
			Utils.FindChild(gameObject.transform, "levelbar").GetComponent<GuiBar>().SetValue(skill.m_level / 100f);
			Utils.FindChild(gameObject.transform, "currentlevel").GetComponent<GuiBar>().SetValue(skill.GetLevelPercentage());
			m_elements.Add(gameObject);
		}
		float size = Mathf.Max(m_baseListSize, (float)skillList.Count * m_spacing);
		m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		m_totalSkillText.set_text("<color=orange>" + player.GetSkills().GetTotalSkill().ToString("0") + "</color><color=white> / </color><color=orange>" + player.GetSkills().GetTotalSkillCap().ToString("0") + "</color>");
	}

	public void OnClose()
	{
		base.gameObject.SetActive(value: false);
	}
}
