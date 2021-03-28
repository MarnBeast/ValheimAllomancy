using System.Collections;
using UnityEngine;

public class LightLod : MonoBehaviour
{
	public bool m_lightLod = true;

	public float m_lightDistance = 40f;

	public bool m_shadowLod = true;

	public float m_shadowDistance = 20f;

	private Light m_light;

	private float m_baseRange;

	private float m_baseShadowStrength;

	private void Awake()
	{
		m_light = GetComponent<Light>();
		m_baseRange = m_light.range;
		m_baseShadowStrength = m_light.shadowStrength;
		if (m_shadowLod && m_light.shadows == LightShadows.None)
		{
			m_shadowLod = false;
		}
		if (m_lightLod)
		{
			m_light.range = 0f;
			m_light.enabled = false;
		}
		if (m_shadowLod)
		{
			m_light.shadowStrength = 0f;
			m_light.shadows = LightShadows.None;
		}
	}

	private void OnEnable()
	{
		StartCoroutine("UpdateLoop");
	}

	private IEnumerator UpdateLoop()
	{
		while (true)
		{
			Camera mainCamera = Utils.GetMainCamera();
			if ((bool)mainCamera && (bool)m_light)
			{
				float distance = Vector3.Distance(mainCamera.transform.position, base.transform.position);
				if (m_lightLod)
				{
					if (distance < m_lightDistance)
					{
						while ((bool)m_light && (m_light.range < m_baseRange || !m_light.enabled))
						{
							m_light.enabled = true;
							m_light.range = Mathf.Min(m_baseRange, m_light.range + Time.deltaTime * m_baseRange);
							yield return null;
						}
					}
					else
					{
						while ((bool)m_light && (m_light.range > 0f || m_light.enabled))
						{
							m_light.range = Mathf.Max(0f, m_light.range - Time.deltaTime * m_baseRange);
							if (m_light.range <= 0f)
							{
								m_light.enabled = false;
							}
							yield return null;
						}
					}
				}
				if (m_shadowLod)
				{
					if (distance < m_shadowDistance)
					{
						while ((bool)m_light && (m_light.shadowStrength < m_baseShadowStrength || m_light.shadows == LightShadows.None))
						{
							m_light.shadows = LightShadows.Soft;
							m_light.shadowStrength = Mathf.Min(m_baseShadowStrength, m_light.shadowStrength + Time.deltaTime * m_baseShadowStrength);
							yield return null;
						}
					}
					else
					{
						while ((bool)m_light && (m_light.shadowStrength > 0f || m_light.shadows != 0))
						{
							m_light.shadowStrength = Mathf.Max(0f, m_light.shadowStrength - Time.deltaTime * m_baseShadowStrength);
							if (m_light.shadowStrength <= 0f)
							{
								m_light.shadows = LightShadows.None;
							}
							yield return null;
						}
					}
				}
			}
			yield return new WaitForSeconds(1f);
		}
	}
}
