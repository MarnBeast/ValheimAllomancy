using System;
using System.Collections.Generic;
using UnityEngine;

public class Ledge : MonoBehaviour
{
	public Collider m_collider;

	public TriggerTracker m_above;

	private void Awake()
	{
		if (GetComponent<ZNetView>().GetZDO() != null)
		{
			m_collider.set_enabled(true);
			TriggerTracker above = m_above;
			above.m_changed = (Action)Delegate.Combine(above.m_changed, new Action(Changed));
		}
	}

	private void Changed()
	{
		List<Collider> colliders = m_above.GetColliders();
		if (colliders.Count == 0)
		{
			m_collider.set_enabled(true);
			return;
		}
		bool enabled = false;
		foreach (Collider item in colliders)
		{
			if (((Component)(object)item).transform.position.y > base.transform.position.y)
			{
				enabled = true;
				break;
			}
		}
		m_collider.set_enabled(enabled);
	}
}
