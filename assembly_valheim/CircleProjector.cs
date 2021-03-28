using System;
using System.Collections.Generic;
using UnityEngine;

public class CircleProjector : MonoBehaviour
{
	public float m_radius = 5f;

	public int m_nrOfSegments = 20;

	public GameObject m_prefab;

	public LayerMask m_mask;

	private List<GameObject> m_segments = new List<GameObject>();

	private void Start()
	{
		CreateSegments();
	}

	private void Update()
	{
		CreateSegments();
		float num = (float)Math.PI * 2f / (float)m_segments.Count;
		RaycastHit val = default(RaycastHit);
		for (int i = 0; i < m_segments.Count; i++)
		{
			float f = (float)i * num + Time.time * 0.1f;
			Vector3 vector = base.transform.position + new Vector3(Mathf.Sin(f) * m_radius, 0f, Mathf.Cos(f) * m_radius);
			GameObject gameObject = m_segments[i];
			if (Physics.Raycast(vector + Vector3.up * 500f, Vector3.down, ref val, 1000f, m_mask.value))
			{
				vector.y = ((RaycastHit)(ref val)).get_point().y;
			}
			gameObject.transform.position = vector;
		}
		for (int j = 0; j < m_segments.Count; j++)
		{
			GameObject gameObject2 = m_segments[j];
			GameObject gameObject3 = ((j == 0) ? m_segments[m_segments.Count - 1] : m_segments[j - 1]);
			Vector3 normalized = (((j == m_segments.Count - 1) ? m_segments[0] : m_segments[j + 1]).transform.position - gameObject3.transform.position).normalized;
			gameObject2.transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
		}
	}

	private void CreateSegments()
	{
		if (m_segments.Count == m_nrOfSegments)
		{
			return;
		}
		foreach (GameObject segment in m_segments)
		{
			UnityEngine.Object.Destroy(segment);
		}
		m_segments.Clear();
		for (int i = 0; i < m_nrOfSegments; i++)
		{
			GameObject item = UnityEngine.Object.Instantiate(m_prefab, base.transform.position, Quaternion.identity, base.transform);
			m_segments.Add(item);
		}
	}
}
