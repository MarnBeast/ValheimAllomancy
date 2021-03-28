using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pathfinding : MonoBehaviour
{
	private class NavMeshTile
	{
		public Vector3Int m_tile;

		public Vector3 m_center;

		public float m_pokeTime = -1000f;

		public float m_buildTime = -1000f;

		public NavMeshData m_data;

		public NavMeshDataInstance m_instance;

		public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links1 = new List<KeyValuePair<Vector3, NavMeshLinkInstance>>();

		public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links2 = new List<KeyValuePair<Vector3, NavMeshLinkInstance>>();
	}

	public enum AgentType
	{
		Humanoid = 1,
		TrollSize,
		HugeSize,
		HorseSize,
		HumanoidNoSwim,
		HumanoidAvoidWater,
		Fish,
		Wolf,
		BigFish,
		GoblinBruteSize,
		HumanoidBigNoSwim
	}

	public enum AreaType
	{
		Default,
		NotWalkable,
		Jump,
		Water
	}

	private class AgentSettings
	{
		public AgentType m_agentType;

		public NavMeshBuildSettings m_build;

		public bool m_canWalk = true;

		public bool m_avoidWater;

		public bool m_canSwim = true;

		public float m_swimDepth;

		public int m_areaMask = -1;

		public AgentSettings(AgentType type)
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			m_agentType = type;
			m_build = NavMesh.CreateSettings();
		}
	}

	private List<Vector3> tempPath = new List<Vector3>();

	private List<Vector3> optPath = new List<Vector3>();

	private List<Vector3> tempStitchPoints = new List<Vector3>();

	private RaycastHit[] tempHitArray = (RaycastHit[])(object)new RaycastHit[255];

	private static Pathfinding m_instance;

	public LayerMask m_layers;

	public LayerMask m_waterLayers;

	private Dictionary<Vector3Int, NavMeshTile> m_tiles = new Dictionary<Vector3Int, NavMeshTile>();

	public float m_tileSize = 32f;

	public float m_defaultCost = 1f;

	public float m_waterCost = 4f;

	public float m_linkCost = 10f;

	public float m_linkWidth = 1f;

	public float m_updateInterval = 5f;

	public float m_tileTimeout = 30f;

	private const float m_tileHeight = 6000f;

	private const float m_tileY = 2500f;

	private float m_updatePathfindingTimer;

	private Queue<Vector3Int> m_queuedAreas = new Queue<Vector3Int>();

	private Queue<NavMeshLinkInstance> m_linkRemoveQueue = new Queue<NavMeshLinkInstance>();

	private Queue<NavMeshDataInstance> m_tileRemoveQueue = new Queue<NavMeshDataInstance>();

	private Vector3Int m_cachedTileID = new Vector3Int(-9999999, -9999999, -9999999);

	private NavMeshTile m_cachedTile;

	private List<AgentSettings> m_agentSettings = new List<AgentSettings>();

	private AsyncOperation m_buildOperation;

	private NavMeshTile m_buildTile;

	private List<KeyValuePair<NavMeshTile, NavMeshTile>> m_edgeBuildQueue = new List<KeyValuePair<NavMeshTile, NavMeshTile>>();

	private NavMeshPath m_path;

	public static Pathfinding instance => m_instance;

	private void Awake()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		m_instance = this;
		SetupAgents();
		m_path = new NavMeshPath();
	}

	private void ClearAgentSettings()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		List<NavMeshBuildSettings> list = new List<NavMeshBuildSettings>();
		for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
		{
			list.Add(NavMesh.GetSettingsByIndex(i));
		}
		foreach (NavMeshBuildSettings item in list)
		{
			NavMeshBuildSettings current = item;
			if (((NavMeshBuildSettings)(ref current)).get_agentTypeID() != 0)
			{
				NavMesh.RemoveSettings(((NavMeshBuildSettings)(ref current)).get_agentTypeID());
			}
		}
	}

	private void OnDestroy()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		foreach (NavMeshTile value in m_tiles.Values)
		{
			ClearLinks(value);
			if ((bool)(UnityEngine.Object)(object)value.m_data)
			{
				NavMesh.RemoveNavMeshData(value.m_instance);
			}
		}
		m_tiles.Clear();
		DestroyAllLinks();
	}

	private AgentSettings AddAgent(AgentType type, AgentSettings copy = null)
	{
		while ((int)(type + 1) > m_agentSettings.Count)
		{
			m_agentSettings.Add(null);
		}
		AgentSettings agentSettings = new AgentSettings(type);
		if (copy != null)
		{
			((NavMeshBuildSettings)(ref agentSettings.m_build)).set_agentHeight(((NavMeshBuildSettings)(ref copy.m_build)).get_agentHeight());
			((NavMeshBuildSettings)(ref agentSettings.m_build)).set_agentClimb(((NavMeshBuildSettings)(ref copy.m_build)).get_agentClimb());
			((NavMeshBuildSettings)(ref agentSettings.m_build)).set_agentRadius(((NavMeshBuildSettings)(ref copy.m_build)).get_agentRadius());
			((NavMeshBuildSettings)(ref agentSettings.m_build)).set_agentSlope(((NavMeshBuildSettings)(ref copy.m_build)).get_agentSlope());
		}
		m_agentSettings[(int)type] = agentSettings;
		return agentSettings;
	}

	private void SetupAgents()
	{
		ClearAgentSettings();
		AgentSettings agentSettings = AddAgent(AgentType.Humanoid);
		((NavMeshBuildSettings)(ref agentSettings.m_build)).set_agentHeight(1.8f);
		((NavMeshBuildSettings)(ref agentSettings.m_build)).set_agentClimb(0.3f);
		((NavMeshBuildSettings)(ref agentSettings.m_build)).set_agentRadius(0.4f);
		((NavMeshBuildSettings)(ref agentSettings.m_build)).set_agentSlope(85f);
		((NavMeshBuildSettings)(ref AddAgent(AgentType.Wolf, agentSettings).m_build)).set_agentSlope(85f);
		AddAgent(AgentType.HumanoidNoSwim, agentSettings).m_canSwim = false;
		AgentSettings agentSettings2 = AddAgent(AgentType.HumanoidBigNoSwim);
		((NavMeshBuildSettings)(ref agentSettings2.m_build)).set_agentHeight(2.5f);
		((NavMeshBuildSettings)(ref agentSettings2.m_build)).set_agentClimb(0.3f);
		((NavMeshBuildSettings)(ref agentSettings2.m_build)).set_agentRadius(0.5f);
		((NavMeshBuildSettings)(ref agentSettings2.m_build)).set_agentSlope(85f);
		agentSettings2.m_canSwim = false;
		AddAgent(AgentType.HumanoidAvoidWater, agentSettings).m_avoidWater = true;
		AgentSettings agentSettings3 = AddAgent(AgentType.TrollSize);
		((NavMeshBuildSettings)(ref agentSettings3.m_build)).set_agentHeight(7f);
		((NavMeshBuildSettings)(ref agentSettings3.m_build)).set_agentClimb(0.6f);
		((NavMeshBuildSettings)(ref agentSettings3.m_build)).set_agentRadius(1f);
		((NavMeshBuildSettings)(ref agentSettings3.m_build)).set_agentSlope(85f);
		AgentSettings agentSettings4 = AddAgent(AgentType.GoblinBruteSize);
		((NavMeshBuildSettings)(ref agentSettings4.m_build)).set_agentHeight(3.5f);
		((NavMeshBuildSettings)(ref agentSettings4.m_build)).set_agentClimb(0.3f);
		((NavMeshBuildSettings)(ref agentSettings4.m_build)).set_agentRadius(0.8f);
		((NavMeshBuildSettings)(ref agentSettings4.m_build)).set_agentSlope(85f);
		AgentSettings agentSettings5 = AddAgent(AgentType.HugeSize);
		((NavMeshBuildSettings)(ref agentSettings5.m_build)).set_agentHeight(10f);
		((NavMeshBuildSettings)(ref agentSettings5.m_build)).set_agentClimb(0.6f);
		((NavMeshBuildSettings)(ref agentSettings5.m_build)).set_agentRadius(2f);
		((NavMeshBuildSettings)(ref agentSettings5.m_build)).set_agentSlope(85f);
		AgentSettings agentSettings6 = AddAgent(AgentType.HorseSize);
		((NavMeshBuildSettings)(ref agentSettings6.m_build)).set_agentHeight(2.5f);
		((NavMeshBuildSettings)(ref agentSettings6.m_build)).set_agentClimb(0.3f);
		((NavMeshBuildSettings)(ref agentSettings6.m_build)).set_agentRadius(0.8f);
		((NavMeshBuildSettings)(ref agentSettings6.m_build)).set_agentSlope(85f);
		AgentSettings agentSettings7 = AddAgent(AgentType.Fish);
		((NavMeshBuildSettings)(ref agentSettings7.m_build)).set_agentHeight(0.5f);
		((NavMeshBuildSettings)(ref agentSettings7.m_build)).set_agentClimb(1f);
		((NavMeshBuildSettings)(ref agentSettings7.m_build)).set_agentRadius(0.5f);
		((NavMeshBuildSettings)(ref agentSettings7.m_build)).set_agentSlope(90f);
		agentSettings7.m_canSwim = true;
		agentSettings7.m_canWalk = false;
		agentSettings7.m_swimDepth = 0.4f;
		agentSettings7.m_areaMask = 12;
		AgentSettings agentSettings8 = AddAgent(AgentType.BigFish);
		((NavMeshBuildSettings)(ref agentSettings8.m_build)).set_agentHeight(1.5f);
		((NavMeshBuildSettings)(ref agentSettings8.m_build)).set_agentClimb(1f);
		((NavMeshBuildSettings)(ref agentSettings8.m_build)).set_agentRadius(1f);
		((NavMeshBuildSettings)(ref agentSettings8.m_build)).set_agentSlope(90f);
		agentSettings8.m_canSwim = true;
		agentSettings8.m_canWalk = false;
		agentSettings8.m_swimDepth = 1.5f;
		agentSettings8.m_areaMask = 12;
		NavMesh.SetAreaCost(0, m_defaultCost);
		NavMesh.SetAreaCost(3, m_waterCost);
	}

	private AgentSettings GetSettings(AgentType agentType)
	{
		return m_agentSettings[(int)agentType];
	}

	private int GetAgentID(AgentType agentType)
	{
		return ((NavMeshBuildSettings)(ref GetSettings(agentType).m_build)).get_agentTypeID();
	}

	private void Update()
	{
		if (!IsBuilding())
		{
			m_updatePathfindingTimer += Time.deltaTime;
			if (m_updatePathfindingTimer > 0.1f)
			{
				m_updatePathfindingTimer = 0f;
				UpdatePathfinding();
			}
			if (!IsBuilding())
			{
				DestroyQueuedNavmeshData();
			}
		}
	}

	private void DestroyAllLinks()
	{
		while (m_linkRemoveQueue.Count > 0 || m_tileRemoveQueue.Count > 0)
		{
			DestroyQueuedNavmeshData();
		}
	}

	private void DestroyQueuedNavmeshData()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (m_linkRemoveQueue.Count > 0)
		{
			int num = Mathf.Min(m_linkRemoveQueue.Count, Mathf.Max(25, m_linkRemoveQueue.Count / 40));
			for (int i = 0; i < num; i++)
			{
				NavMesh.RemoveLink(m_linkRemoveQueue.Dequeue());
			}
		}
		else if (m_tileRemoveQueue.Count > 0)
		{
			NavMesh.RemoveNavMeshData(m_tileRemoveQueue.Dequeue());
		}
	}

	private void UpdatePathfinding()
	{
		Buildtiles();
		TimeoutTiles();
	}

	public bool HavePath(Vector3 from, Vector3 to, AgentType agentType)
	{
		return GetPath(from, to, null, agentType, requireFullPath: true, cleanup: false);
	}

	public bool FindValidPoint(out Vector3 point, Vector3 center, float range, AgentType agentType)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		PokePoint(center, agentType);
		AgentSettings settings = GetSettings(agentType);
		NavMeshQueryFilter val = default(NavMeshQueryFilter);
		((NavMeshQueryFilter)(ref val)).set_agentTypeID((int)settings.m_agentType);
		((NavMeshQueryFilter)(ref val)).set_areaMask(settings.m_areaMask);
		NavMeshHit val2 = default(NavMeshHit);
		if (NavMesh.SamplePosition(center, ref val2, range, val))
		{
			point = ((NavMeshHit)(ref val2)).get_position();
			return true;
		}
		point = center;
		return false;
	}

	public bool GetPath(Vector3 from, Vector3 to, List<Vector3> path, AgentType agentType, bool requireFullPath = false, bool cleanup = true)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Invalid comparison between Unknown and I4
		path?.Clear();
		PokeArea(from, agentType);
		PokeArea(to, agentType);
		AgentSettings settings = GetSettings(agentType);
		if (!SnapToNavMesh(ref from, settings))
		{
			return false;
		}
		if (!SnapToNavMesh(ref to, settings))
		{
			return false;
		}
		NavMeshQueryFilter val = default(NavMeshQueryFilter);
		((NavMeshQueryFilter)(ref val)).set_agentTypeID(((NavMeshBuildSettings)(ref settings.m_build)).get_agentTypeID());
		((NavMeshQueryFilter)(ref val)).set_areaMask(settings.m_areaMask);
		if (NavMesh.CalculatePath(from, to, val, m_path))
		{
			if ((int)m_path.get_status() == 1 && requireFullPath)
			{
				return false;
			}
			if (path != null)
			{
				path.AddRange(m_path.get_corners());
				if (cleanup)
				{
					CleanPath(path, settings);
				}
			}
			return true;
		}
		return false;
	}

	private void CleanPath(List<Vector3> basePath, AgentSettings settings)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		if (basePath.Count <= 2)
		{
			return;
		}
		NavMeshQueryFilter val = default(NavMeshQueryFilter);
		((NavMeshQueryFilter)(ref val)).set_agentTypeID(((NavMeshBuildSettings)(ref settings.m_build)).get_agentTypeID());
		((NavMeshQueryFilter)(ref val)).set_areaMask(settings.m_areaMask);
		int num = 0;
		optPath.Clear();
		optPath.Add(basePath[num]);
		do
		{
			num = FindNextNode(basePath, val, num);
			optPath.Add(basePath[num]);
		}
		while (num < basePath.Count - 1);
		tempPath.Clear();
		tempPath.Add(optPath[0]);
		NavMeshHit val2 = default(NavMeshHit);
		for (int i = 1; i < optPath.Count - 1; i++)
		{
			Vector3 vector = optPath[i - 1];
			Vector3 vector2 = optPath[i];
			Vector3 vector3 = optPath[i + 1];
			Vector3 normalized = (vector3 - vector2).normalized;
			Vector3 normalized2 = (vector2 - vector).normalized;
			Vector3 vector4 = vector2 - (normalized + normalized2).normalized * Vector3.Distance(vector2, vector) * 0.33f;
			vector4.y = (vector2.y + vector.y) * 0.5f;
			Vector3 normalized3 = (vector4 - vector2).normalized;
			if (!NavMesh.Raycast(vector2 + normalized3 * 0.1f, vector4, ref val2, val) && !NavMesh.Raycast(vector4, vector, ref val2, val))
			{
				tempPath.Add(vector4);
			}
			tempPath.Add(vector2);
			Vector3 vector5 = vector2 + (normalized + normalized2).normalized * Vector3.Distance(vector2, vector3) * 0.33f;
			vector5.y = (vector2.y + vector3.y) * 0.5f;
			Vector3 normalized4 = (vector5 - vector2).normalized;
			if (!NavMesh.Raycast(vector2 + normalized4 * 0.1f, vector5, ref val2, val) && !NavMesh.Raycast(vector5, vector3, ref val2, val))
			{
				tempPath.Add(vector5);
			}
		}
		tempPath.Add(optPath[optPath.Count - 1]);
		basePath.Clear();
		basePath.AddRange(tempPath);
	}

	private int FindNextNode(List<Vector3> path, NavMeshQueryFilter filter, int start)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		NavMeshHit val = default(NavMeshHit);
		for (int i = start + 2; i < path.Count; i++)
		{
			if (NavMesh.Raycast(path[start], path[i], ref val, filter))
			{
				return i - 1;
			}
		}
		return path.Count - 1;
	}

	private bool SnapToNavMesh(ref Vector3 point, AgentSettings settings)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		if ((bool)ZoneSystem.instance)
		{
			if (ZoneSystem.instance.GetGroundHeight(point, out var height) && point.y < height)
			{
				point.y = height;
			}
			if (settings.m_canSwim)
			{
				point.y = Mathf.Max(ZoneSystem.instance.m_waterLevel - settings.m_swimDepth, point.y);
			}
		}
		NavMeshQueryFilter val = default(NavMeshQueryFilter);
		((NavMeshQueryFilter)(ref val)).set_agentTypeID(((NavMeshBuildSettings)(ref settings.m_build)).get_agentTypeID());
		((NavMeshQueryFilter)(ref val)).set_areaMask(settings.m_areaMask);
		NavMeshHit val2 = default(NavMeshHit);
		if (NavMesh.SamplePosition(point, ref val2, 1.5f, val))
		{
			point = ((NavMeshHit)(ref val2)).get_position();
			return true;
		}
		if (NavMesh.SamplePosition(point, ref val2, 10f, val))
		{
			point = ((NavMeshHit)(ref val2)).get_position();
			return true;
		}
		if (NavMesh.SamplePosition(point, ref val2, 20f, val))
		{
			point = ((NavMeshHit)(ref val2)).get_position();
			return true;
		}
		return false;
	}

	private void TimeoutTiles()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		foreach (KeyValuePair<Vector3Int, NavMeshTile> tile in m_tiles)
		{
			if (realtimeSinceStartup - tile.Value.m_pokeTime > m_tileTimeout)
			{
				ClearLinks(tile.Value);
				if (((NavMeshDataInstance)(ref tile.Value.m_instance)).get_valid())
				{
					m_tileRemoveQueue.Enqueue(tile.Value.m_instance);
				}
				m_tiles.Remove(tile.Key);
				break;
			}
		}
	}

	private void PokeArea(Vector3 point, AgentType agentType)
	{
		Vector3Int tile = GetTile(point, agentType);
		PokeTile(tile);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j != 0 || i != 0)
				{
					Vector3Int tileID = new Vector3Int(tile.x + j, tile.y + i, tile.z);
					PokeTile(tileID);
				}
			}
		}
	}

	private void PokePoint(Vector3 point, AgentType agentType)
	{
		Vector3Int tile = GetTile(point, agentType);
		PokeTile(tile);
	}

	private void PokeTile(Vector3Int tileID)
	{
		GetNavTile(tileID).m_pokeTime = Time.realtimeSinceStartup;
	}

	private void Buildtiles()
	{
		if (UpdateAsyncBuild())
		{
			return;
		}
		NavMeshTile navMeshTile = null;
		float num = 0f;
		foreach (NavMeshTile value in m_tiles.Values)
		{
			float num2 = value.m_pokeTime - value.m_buildTime;
			if (num2 > m_updateInterval && (navMeshTile == null || num2 > num))
			{
				navMeshTile = value;
				num = num2;
			}
		}
		if (navMeshTile != null)
		{
			BuildTile(navMeshTile);
			navMeshTile.m_buildTime = Time.realtimeSinceStartup;
		}
	}

	private void BuildTile(NavMeshTile tile)
	{
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected O, but got Unknown
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		_ = DateTime.Now;
		List<NavMeshBuildSource> list = new List<NavMeshBuildSource>();
		List<NavMeshBuildMarkup> list2 = new List<NavMeshBuildMarkup>();
		AgentType z = (AgentType)tile.m_tile.z;
		AgentSettings settings = GetSettings(z);
		Bounds bounds = new Bounds(tile.m_center, new Vector3(m_tileSize, 6000f, m_tileSize));
		Bounds bounds2 = new Bounds(Vector3.zero, new Vector3(m_tileSize, 6000f, m_tileSize));
		int num = ((!settings.m_canWalk) ? 1 : 0);
		NavMeshBuilder.CollectSources(bounds, m_layers.value, (NavMeshCollectGeometry)1, num, list2, list);
		if (settings.m_avoidWater)
		{
			List<NavMeshBuildSource> list3 = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(bounds, m_waterLayers.value, (NavMeshCollectGeometry)1, 1, list2, list3);
			foreach (NavMeshBuildSource item in list3)
			{
				NavMeshBuildSource current = item;
				((NavMeshBuildSource)(ref current)).set_transform(((NavMeshBuildSource)(ref current)).get_transform() * Matrix4x4.Translate(Vector3.down * 0.2f));
				list.Add(current);
			}
		}
		else if (settings.m_canSwim)
		{
			List<NavMeshBuildSource> list4 = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(bounds, m_waterLayers.value, (NavMeshCollectGeometry)1, 3, list2, list4);
			if (settings.m_swimDepth != 0f)
			{
				foreach (NavMeshBuildSource item2 in list4)
				{
					NavMeshBuildSource current2 = item2;
					((NavMeshBuildSource)(ref current2)).set_transform(((NavMeshBuildSource)(ref current2)).get_transform() * Matrix4x4.Translate(Vector3.down * settings.m_swimDepth));
					list.Add(current2);
				}
			}
			else
			{
				list.AddRange(list4);
			}
		}
		if ((UnityEngine.Object)(object)tile.m_data == null)
		{
			tile.m_data = new NavMeshData();
			tile.m_data.set_position(tile.m_center);
		}
		m_buildOperation = NavMeshBuilder.UpdateNavMeshDataAsync(tile.m_data, settings.m_build, list, bounds2);
		m_buildTile = tile;
	}

	private bool IsBuilding()
	{
		if (m_buildOperation != null)
		{
			return !m_buildOperation.isDone;
		}
		return false;
	}

	private bool UpdateAsyncBuild()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (m_buildOperation == null)
		{
			return false;
		}
		if (!m_buildOperation.isDone)
		{
			return true;
		}
		if (!((NavMeshDataInstance)(ref m_buildTile.m_instance)).get_valid())
		{
			m_buildTile.m_instance = NavMesh.AddNavMeshData(m_buildTile.m_data);
		}
		RebuildLinks(m_buildTile);
		m_buildOperation = null;
		m_buildTile = null;
		return true;
	}

	private void ClearLinks(NavMeshTile tile)
	{
		ClearLinks(tile.m_links1);
		ClearLinks(tile.m_links2);
	}

	private void ClearLinks(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<Vector3, NavMeshLinkInstance> link in links)
		{
			m_linkRemoveQueue.Enqueue(link.Value);
		}
		links.Clear();
	}

	private void RebuildLinks(NavMeshTile tile)
	{
		AgentType z = (AgentType)tile.m_tile.z;
		AgentSettings settings = GetSettings(z);
		float num = m_tileSize / 2f;
		ConnectAlongEdge(tile.m_links1, tile.m_center + new Vector3(num, 0f, num), tile.m_center + new Vector3(num, 0f, 0f - num), m_linkWidth, settings);
		ConnectAlongEdge(tile.m_links2, tile.m_center + new Vector3(0f - num, 0f, num), tile.m_center + new Vector3(num, 0f, num), m_linkWidth, settings);
	}

	private void ConnectAlongEdge(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links, Vector3 p0, Vector3 p1, float step, AgentSettings settings)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 normalized = (p1 - p0).normalized;
		Vector3 a = Vector3.Cross(Vector3.up, normalized);
		float num = Vector3.Distance(p0, p1);
		bool canSwim = settings.m_canSwim;
		tempStitchPoints.Clear();
		for (float num2 = step / 2f; num2 <= num; num2 += step)
		{
			Vector3 p2 = p0 + normalized * num2;
			FindGround(p2, canSwim, tempStitchPoints, settings);
		}
		if (CompareLinks(tempStitchPoints, links))
		{
			return;
		}
		ClearLinks(links);
		foreach (Vector3 tempStitchPoint in tempStitchPoints)
		{
			NavMeshLinkData val = default(NavMeshLinkData);
			((NavMeshLinkData)(ref val)).set_startPosition(tempStitchPoint - a * 0.1f);
			((NavMeshLinkData)(ref val)).set_endPosition(tempStitchPoint + a * 0.1f);
			((NavMeshLinkData)(ref val)).set_width(step);
			((NavMeshLinkData)(ref val)).set_costModifier(m_linkCost);
			((NavMeshLinkData)(ref val)).set_bidirectional(true);
			((NavMeshLinkData)(ref val)).set_agentTypeID(((NavMeshBuildSettings)(ref settings.m_build)).get_agentTypeID());
			((NavMeshLinkData)(ref val)).set_area(2);
			NavMeshLinkInstance value = NavMesh.AddLink(val);
			if (((NavMeshLinkInstance)(ref value)).get_valid())
			{
				links.Add(new KeyValuePair<Vector3, NavMeshLinkInstance>(tempStitchPoint, value));
			}
		}
	}

	private bool CompareLinks(List<Vector3> tempStitchPoints, List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
	{
		if (tempStitchPoints.Count != links.Count)
		{
			return false;
		}
		for (int i = 0; i < tempStitchPoints.Count; i++)
		{
			if (tempStitchPoints[i] != links[i].Key)
			{
				return false;
			}
		}
		return true;
	}

	private bool SnapToNearestGround(Vector3 p, out Vector3 pos, float range)
	{
		RaycastHit val = default(RaycastHit);
		if (Physics.Raycast(p + Vector3.up, Vector3.down, ref val, range + 1f, m_layers.value | m_waterLayers.value))
		{
			pos = ((RaycastHit)(ref val)).get_point();
			return true;
		}
		if (Physics.Raycast(p + Vector3.up * range, Vector3.down, ref val, range, m_layers.value | m_waterLayers.value))
		{
			pos = ((RaycastHit)(ref val)).get_point();
			return true;
		}
		pos = p;
		return false;
	}

	private void FindGround(Vector3 p, bool testWater, List<Vector3> hits, AgentSettings settings)
	{
		p.y = 6000f;
		int num = (testWater ? (m_layers.value | m_waterLayers.value) : m_layers.value);
		float agentHeight = ((NavMeshBuildSettings)(ref settings.m_build)).get_agentHeight();
		float y = p.y;
		int num2 = Physics.RaycastNonAlloc(p, Vector3.down, tempHitArray, 10000f, num);
		for (int i = 0; i < num2; i++)
		{
			Vector3 point = ((RaycastHit)(ref tempHitArray[i])).get_point();
			if (!(Mathf.Abs(point.y - y) < agentHeight))
			{
				y = point.y;
				if (((1 << ((Component)(object)((RaycastHit)(ref tempHitArray[i])).get_collider()).gameObject.layer) & (int)m_waterLayers) != 0)
				{
					point.y -= settings.m_swimDepth;
				}
				hits.Add(point);
			}
		}
	}

	private NavMeshTile GetNavTile(Vector3 point, AgentType agent)
	{
		Vector3Int tile = GetTile(point, agent);
		return GetNavTile(tile);
	}

	private NavMeshTile GetNavTile(Vector3Int tile)
	{
		if (tile == m_cachedTileID)
		{
			return m_cachedTile;
		}
		if (m_tiles.TryGetValue(tile, out var value))
		{
			m_cachedTileID = tile;
			m_cachedTile = value;
			return value;
		}
		value = new NavMeshTile();
		value.m_tile = tile;
		value.m_center = GetTilePos(tile);
		m_tiles.Add(tile, value);
		m_cachedTileID = tile;
		m_cachedTile = value;
		return value;
	}

	private Vector3Int GetTile(Vector3 point, AgentType agent)
	{
		int x = Mathf.FloorToInt((point.x + m_tileSize / 2f) / m_tileSize);
		int y = Mathf.FloorToInt((point.z + m_tileSize / 2f) / m_tileSize);
		return new Vector3Int(x, y, (int)agent);
	}

	public Vector3 GetTilePos(Vector3Int id)
	{
		return new Vector3((float)id.x * m_tileSize, 2500f, (float)id.y * m_tileSize);
	}
}
