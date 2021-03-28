using System;
using UnityEngine;

public class ShipConstructor : MonoBehaviour
{
	public GameObject m_shipPrefab;

	public GameObject m_hideWhenConstructed;

	public Transform m_spawnPoint;

	public long m_constructionTimeMinutes = 1L;

	private ZNetView m_nview;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		if (!(m_nview == null) && m_nview.GetZDO() != null)
		{
			if (m_nview.IsOwner() && m_nview.GetZDO().GetLong("spawntime", 0L) == 0L)
			{
				m_nview.GetZDO().Set("spawntime", ZNet.instance.GetTime().Ticks);
			}
			InvokeRepeating("UpdateConstruction", 5f, 1f);
			if (IsBuilt())
			{
				m_hideWhenConstructed.SetActive(value: false);
			}
		}
	}

	private bool IsBuilt()
	{
		return m_nview.GetZDO().GetBool("done");
	}

	private void UpdateConstruction()
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (IsBuilt())
		{
			m_hideWhenConstructed.SetActive(value: false);
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(m_nview.GetZDO().GetLong("spawntime", 0L));
		if ((time - d).TotalMinutes > (double)m_constructionTimeMinutes)
		{
			m_hideWhenConstructed.SetActive(value: false);
			UnityEngine.Object.Instantiate(m_shipPrefab, m_spawnPoint.position, m_spawnPoint.rotation);
			m_nview.GetZDO().Set("done", value: true);
		}
	}
}
