using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainModifier : MonoBehaviour
{
	public enum PaintType
	{
		Dirt,
		Cultivate,
		Paved,
		Reset
	}

	private static bool m_triggerOnPlaced = false;

	public int m_sortOrder;

	public bool m_playerModifiction;

	public float m_levelOffset;

	[Header("Level")]
	public bool m_level;

	public float m_levelRadius = 2f;

	public bool m_square = true;

	[Header("Smooth")]
	public bool m_smooth;

	public float m_smoothRadius = 2f;

	public float m_smoothPower = 3f;

	[Header("Paint")]
	public bool m_paintCleared = true;

	public bool m_paintHeightCheck;

	public PaintType m_paintType;

	public float m_paintRadius = 2f;

	[Header("Effects")]
	public EffectList m_onPlacedEffect = new EffectList();

	[Header("Spawn items")]
	public GameObject m_spawnOnPlaced;

	public float m_chanceToSpawn = 1f;

	public int m_maxSpawned = 1;

	public bool m_spawnAtMaxLevelDepth = true;

	private bool m_wasEnabled;

	private ZNetView m_nview;

	private static List<TerrainModifier> m_instances = new List<TerrainModifier>();

	private static bool m_needsSorting = false;

	private void Awake()
	{
		m_instances.Add(this);
		m_needsSorting = true;
		m_nview = GetComponent<ZNetView>();
		m_wasEnabled = base.enabled;
		if (base.enabled)
		{
			if (m_triggerOnPlaced)
			{
				OnPlaced();
			}
			PokeHeightmaps();
		}
	}

	private void OnDestroy()
	{
		m_instances.Remove(this);
		m_needsSorting = true;
		if (m_wasEnabled)
		{
			PokeHeightmaps();
		}
	}

	private void PokeHeightmaps()
	{
		bool delayed = !m_triggerOnPlaced;
		foreach (Heightmap allHeightmap in Heightmap.GetAllHeightmaps())
		{
			if (allHeightmap.TerrainVSModifier(this))
			{
				allHeightmap.Poke(delayed);
			}
		}
		if ((bool)ClutterSystem.instance)
		{
			ClutterSystem.instance.ResetGrass(base.transform.position, GetRadius());
		}
	}

	public float GetRadius()
	{
		float num = 0f;
		if (m_level && m_levelRadius > num)
		{
			num = m_levelRadius;
		}
		if (m_smooth && m_smoothRadius > num)
		{
			num = m_smoothRadius;
		}
		if (m_paintCleared && m_paintRadius > num)
		{
			num = m_paintRadius;
		}
		return num;
	}

	public static void SetTriggerOnPlaced(bool trigger)
	{
		m_triggerOnPlaced = trigger;
	}

	private void OnPlaced()
	{
		RemoveOthers(base.transform.position, GetRadius() / 4f);
		m_onPlacedEffect.Create(base.transform.position, Quaternion.identity);
		if ((bool)m_spawnOnPlaced && (m_spawnAtMaxLevelDepth || !Heightmap.AtMaxLevelDepth(base.transform.position + Vector3.up * m_levelOffset)) && Random.value <= m_chanceToSpawn)
		{
			Vector3 b = Random.insideUnitCircle * 0.2f;
			GameObject gameObject = Object.Instantiate(m_spawnOnPlaced, base.transform.position + Vector3.up * 0.5f + b, Quaternion.identity);
			gameObject.GetComponent<ItemDrop>().m_itemData.m_stack = Random.Range(1, m_maxSpawned + 1);
			gameObject.GetComponent<Rigidbody>().set_velocity(Vector3.up * 4f);
		}
	}

	private static void GetModifiers(Vector3 point, float range, List<TerrainModifier> modifiers, TerrainModifier ignore = null)
	{
		foreach (TerrainModifier instance in m_instances)
		{
			if (!(instance == ignore) && Utils.DistanceXZ(point, instance.transform.position) < range)
			{
				modifiers.Add(instance);
			}
		}
	}

	public static Piece FindClosestModifierPieceInRange(Vector3 point, float range)
	{
		float num = 999999f;
		TerrainModifier terrainModifier = null;
		foreach (TerrainModifier instance in m_instances)
		{
			if (!(instance.m_nview == null))
			{
				float num2 = Utils.DistanceXZ(point, instance.transform.position);
				if (!(num2 > range) && !(num2 > num))
				{
					num = num2;
					terrainModifier = instance;
				}
			}
		}
		if ((bool)terrainModifier)
		{
			return terrainModifier.GetComponent<Piece>();
		}
		return null;
	}

	private void RemoveOthers(Vector3 point, float range)
	{
		List<TerrainModifier> list = new List<TerrainModifier>();
		GetModifiers(point, range, list, this);
		int num = 0;
		foreach (TerrainModifier item in list)
		{
			if ((m_level || !item.m_level) && (!m_paintCleared || m_paintType != PaintType.Reset || (item.m_paintCleared && item.m_paintType == PaintType.Reset)) && (bool)item.m_nview && item.m_nview.IsValid())
			{
				num++;
				item.m_nview.ClaimOwnership();
				item.m_nview.Destroy();
			}
		}
	}

	private static int SortByModifiers(TerrainModifier a, TerrainModifier b)
	{
		if (a.m_playerModifiction == b.m_playerModifiction)
		{
			if (a.m_sortOrder == b.m_sortOrder)
			{
				return a.GetCreationTime().CompareTo(b.GetCreationTime());
			}
			return a.m_sortOrder.CompareTo(b.m_sortOrder);
		}
		return a.m_playerModifiction.CompareTo(b.m_playerModifiction);
	}

	public static List<TerrainModifier> GetAllInstances()
	{
		if (m_needsSorting)
		{
			m_instances.Sort(SortByModifiers);
			m_needsSorting = false;
		}
		return m_instances;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + Vector3.up * m_levelOffset, Quaternion.identity, new Vector3(1f, 0f, 1f));
		if (m_level)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(Vector3.zero, m_levelRadius);
		}
		if (m_smooth)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(Vector3.zero, m_smoothRadius);
		}
		if (m_paintCleared)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(Vector3.zero, m_paintRadius);
		}
		Gizmos.matrix = Matrix4x4.identity;
	}

	public long GetCreationTime()
	{
		if ((bool)m_nview && m_nview.GetZDO() != null)
		{
			return m_nview.GetZDO().m_timeCreated;
		}
		return 0L;
	}
}
