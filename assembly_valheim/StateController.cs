using UnityEngine;

public class StateController : StateMachineBehaviour
{
	public string m_effectJoint = "";

	public EffectList m_enterEffect = new EffectList();

	public bool m_enterDisableChildren;

	public bool m_enterEnableChildren;

	public GameObject[] m_enterDisable = new GameObject[0];

	public GameObject[] m_enterEnable = new GameObject[0];

	private Transform m_effectJoinT;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (m_enterEffect.HasEffects())
		{
			m_enterEffect.Create(GetEffectPos(animator), ((Component)(object)animator).transform.rotation);
		}
		if (m_enterDisableChildren)
		{
			for (int i = 0; i < ((Component)(object)animator).transform.childCount; i++)
			{
				((Component)(object)animator).transform.GetChild(i).gameObject.SetActive(value: false);
			}
		}
		if (m_enterEnableChildren)
		{
			for (int j = 0; j < ((Component)(object)animator).transform.childCount; j++)
			{
				((Component)(object)animator).transform.GetChild(j).gameObject.SetActive(value: true);
			}
		}
	}

	private Vector3 GetEffectPos(Animator animator)
	{
		if (m_effectJoint.Length == 0)
		{
			return ((Component)(object)animator).transform.position;
		}
		if (m_effectJoinT == null)
		{
			m_effectJoinT = Utils.FindChild(((Component)(object)animator).transform, m_effectJoint);
		}
		return m_effectJoinT.position;
	}

	public StateController()
		: this()
	{
	}
}
