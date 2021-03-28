using System.Collections.Generic;
using UnityEngine;

public class StaticTarget : MonoBehaviour
{
	public bool m_primaryTarget;

	public bool m_randomTarget = true;

	private List<Collider> m_colliders;

	private Vector3 m_center;

	private bool m_haveCenter;

	public virtual bool IsValidMonsterTarget()
	{
		return true;
	}

	public Vector3 GetCenter()
	{
		if (!m_haveCenter)
		{
			List<Collider> allColliders = GetAllColliders();
			m_center = Vector3.zero;
			foreach (Collider item in allColliders)
			{
				if ((bool)(Object)(object)item)
				{
					m_center += item.get_bounds().center;
				}
			}
			m_center /= (float)m_colliders.Count;
		}
		return m_center;
	}

	public List<Collider> GetAllColliders()
	{
		if (m_colliders == null)
		{
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
			m_colliders = new List<Collider>();
			m_colliders.Capacity = componentsInChildren.Length;
			Collider[] array = componentsInChildren;
			foreach (Collider val in array)
			{
				if (val.get_enabled() && ((Component)(object)val).gameObject.activeInHierarchy && !val.get_isTrigger())
				{
					m_colliders.Add(val);
				}
			}
		}
		return m_colliders;
	}

	public Vector3 FindClosestPoint(Vector3 point)
	{
		List<Collider> allColliders = GetAllColliders();
		if (allColliders.Count == 0)
		{
			return base.transform.position;
		}
		float num = 9999999f;
		Vector3 result = Vector3.zero;
		foreach (Collider item in allColliders)
		{
			MeshCollider val = item as MeshCollider;
			Vector3 vector = (((bool)(Object)(object)val && !val.get_convex()) ? item.ClosestPointOnBounds(point) : item.ClosestPoint(point));
			float num2 = Vector3.Distance(point, vector);
			if (num2 < num)
			{
				result = vector;
				num = num2;
			}
		}
		return result;
	}
}
