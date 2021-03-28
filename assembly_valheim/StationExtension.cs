using System.Collections.Generic;
using UnityEngine;

public class StationExtension : MonoBehaviour, Hoverable
{
	public CraftingStation m_craftingStation;

	public float m_maxStationDistance = 5f;

	public GameObject m_connectionPrefab;

	private GameObject m_connection;

	private Piece m_piece;

	private Collider[] m_colliders;

	private static List<StationExtension> m_allExtensions = new List<StationExtension>();

	private void Awake()
	{
		if (GetComponent<ZNetView>().GetZDO() != null)
		{
			m_piece = GetComponent<Piece>();
			m_allExtensions.Add(this);
		}
	}

	private void OnDestroy()
	{
		if ((bool)m_connection)
		{
			Object.Destroy(m_connection);
			m_connection = null;
		}
		m_allExtensions.Remove(this);
	}

	public string GetHoverText()
	{
		PokeEffect();
		return Localization.get_instance().Localize(m_piece.m_name);
	}

	public string GetHoverName()
	{
		return Localization.get_instance().Localize(m_piece.m_name);
	}

	public string GetExtensionName()
	{
		return m_piece.m_name;
	}

	public static void FindExtensions(CraftingStation station, Vector3 pos, List<StationExtension> extensions)
	{
		foreach (StationExtension allExtension in m_allExtensions)
		{
			if (Vector3.Distance(allExtension.transform.position, pos) < allExtension.m_maxStationDistance && allExtension.m_craftingStation.m_name == station.m_name && !ExtensionInList(extensions, allExtension))
			{
				extensions.Add(allExtension);
			}
		}
	}

	private static bool ExtensionInList(List<StationExtension> extensions, StationExtension extension)
	{
		foreach (StationExtension extension2 in extensions)
		{
			if (extension2.GetExtensionName() == extension.GetExtensionName())
			{
				return true;
			}
		}
		return false;
	}

	public bool OtherExtensionInRange(float radius)
	{
		foreach (StationExtension allExtension in m_allExtensions)
		{
			if (!(allExtension == this) && Vector3.Distance(allExtension.transform.position, base.transform.position) < radius)
			{
				return true;
			}
		}
		return false;
	}

	public List<CraftingStation> FindStationsInRange(Vector3 center)
	{
		List<CraftingStation> list = new List<CraftingStation>();
		CraftingStation.FindStationsInRange(m_craftingStation.m_name, center, m_maxStationDistance, list);
		return list;
	}

	public CraftingStation FindClosestStationInRange(Vector3 center)
	{
		return CraftingStation.FindClosestStationInRange(m_craftingStation.m_name, center, m_maxStationDistance);
	}

	private void PokeEffect()
	{
		CraftingStation craftingStation = FindClosestStationInRange(base.transform.position);
		if ((bool)craftingStation)
		{
			StartConnectionEffect(craftingStation);
		}
	}

	public void StartConnectionEffect(CraftingStation station)
	{
		StartConnectionEffect(station.GetConnectionEffectPoint());
	}

	public void StartConnectionEffect(Vector3 targetPos)
	{
		Vector3 center = GetCenter();
		if (m_connection == null)
		{
			m_connection = Object.Instantiate(m_connectionPrefab, center, Quaternion.identity);
		}
		Vector3 vector = targetPos - center;
		Quaternion rotation = Quaternion.LookRotation(vector.normalized);
		m_connection.transform.position = center;
		m_connection.transform.rotation = rotation;
		m_connection.transform.localScale = new Vector3(1f, 1f, vector.magnitude);
		CancelInvoke("StopConnectionEffect");
		Invoke("StopConnectionEffect", 1f);
	}

	public void StopConnectionEffect()
	{
		if ((bool)m_connection)
		{
			Object.Destroy(m_connection);
			m_connection = null;
		}
	}

	private Vector3 GetCenter()
	{
		if (m_colliders == null)
		{
			m_colliders = GetComponentsInChildren<Collider>();
		}
		Vector3 position = base.transform.position;
		Collider[] colliders = m_colliders;
		foreach (Collider val in colliders)
		{
			if (val.get_bounds().max.y > position.y)
			{
				position.y = val.get_bounds().max.y;
			}
		}
		return position;
	}
}
