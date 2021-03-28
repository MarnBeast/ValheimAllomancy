using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ZNetScene : MonoBehaviour
{
	private static ZNetScene m_instance;

	private const int m_maxCreatedPerFrame = 10;

	private const int m_maxDestroyedPerFrame = 20;

	private const float m_createDestroyFps = 30f;

	public List<GameObject> m_prefabs = new List<GameObject>();

	public List<GameObject> m_nonNetViewPrefabs = new List<GameObject>();

	private Dictionary<int, GameObject> m_namedPrefabs = new Dictionary<int, GameObject>();

	private Dictionary<ZDO, ZNetView> m_instances = new Dictionary<ZDO, ZNetView>(new ZDOComparer());

	private GameObject m_netSceneRoot;

	private List<ZDO> m_tempCurrentObjects = new List<ZDO>();

	private List<ZDO> m_tempCurrentObjects2 = new List<ZDO>();

	private List<ZDO> m_tempCurrentDistantObjects = new List<ZDO>();

	private List<ZNetView> m_tempRemoved = new List<ZNetView>();

	private HashSet<ZDO> m_tempActiveZDOs = new HashSet<ZDO>(new ZDOComparer());

	private float m_createDestroyTimer;

	public static ZNetScene instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		foreach (GameObject prefab in m_prefabs)
		{
			m_namedPrefabs.Add(StringExtensionMethods.GetStableHashCode(prefab.name), prefab);
		}
		foreach (GameObject nonNetViewPrefab in m_nonNetViewPrefabs)
		{
			m_namedPrefabs.Add(StringExtensionMethods.GetStableHashCode(nonNetViewPrefab.name), nonNetViewPrefab);
		}
		ZDOMan instance = ZDOMan.instance;
		instance.m_onZDODestroyed = (Action<ZDO>)Delegate.Combine(instance.m_onZDODestroyed, new Action<ZDO>(OnZDODestroyed));
		m_netSceneRoot = new GameObject("_NetSceneRoot");
		ZRoutedRpc.instance.Register<Vector3, Quaternion, int>("SpawnObject", RPC_SpawnObject);
	}

	private void OnDestroy()
	{
		ZLog.Log((object)"Net scene destroyed");
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public void Shutdown()
	{
		foreach (KeyValuePair<ZDO, ZNetView> instance in m_instances)
		{
			if ((bool)instance.Value)
			{
				instance.Value.ResetZDO();
				UnityEngine.Object.Destroy(instance.Value.gameObject);
			}
		}
		m_instances.Clear();
		base.enabled = false;
	}

	public void AddInstance(ZDO zdo, ZNetView nview)
	{
		m_instances[zdo] = nview;
		if (nview.transform.parent == null)
		{
			nview.transform.SetParent(m_netSceneRoot.transform);
		}
	}

	private bool IsPrefabZDOValid(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return false;
		}
		if (GetPrefab(prefab) == null)
		{
			return false;
		}
		return true;
	}

	private GameObject CreateObject(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return null;
		}
		GameObject prefab2 = GetPrefab(prefab);
		if (prefab2 == null)
		{
			return null;
		}
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		ZNetView.m_useInitZDO = true;
		ZNetView.m_initZDO = zdo;
		GameObject result = UnityEngine.Object.Instantiate(prefab2, position, rotation);
		if (ZNetView.m_initZDO != null)
		{
			ZLog.LogWarning((object)string.Concat("ZDO ", zdo.m_uid, " not used when creating object ", prefab2.name));
			ZNetView.m_initZDO = null;
		}
		ZNetView.m_useInitZDO = false;
		return result;
	}

	public void Destroy(GameObject go)
	{
		ZNetView component = go.GetComponent<ZNetView>();
		if ((bool)component && component.GetZDO() != null)
		{
			ZDO zDO = component.GetZDO();
			component.ResetZDO();
			m_instances.Remove(zDO);
			if (zDO.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zDO);
			}
		}
		UnityEngine.Object.Destroy(go);
	}

	public GameObject GetPrefab(int hash)
	{
		if (m_namedPrefabs.TryGetValue(hash, out var value))
		{
			return value;
		}
		return null;
	}

	public GameObject GetPrefab(string name)
	{
		int stableHashCode = StringExtensionMethods.GetStableHashCode(name);
		return GetPrefab(stableHashCode);
	}

	public int GetPrefabHash(GameObject go)
	{
		return StringExtensionMethods.GetStableHashCode(go.name);
	}

	public bool IsAreaReady(Vector3 point)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		Vector2i zone = ZoneSystem.instance.GetZone(point);
		if (!ZoneSystem.instance.IsZoneLoaded(zone))
		{
			return false;
		}
		m_tempCurrentObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, 1, 0, m_tempCurrentObjects);
		foreach (ZDO tempCurrentObject in m_tempCurrentObjects)
		{
			if (IsPrefabZDOValid(tempCurrentObject) && !FindInstance(tempCurrentObject))
			{
				return false;
			}
		}
		return true;
	}

	private bool InLoadingScreen()
	{
		if (Player.m_localPlayer == null || Player.m_localPlayer.IsTeleporting())
		{
			return true;
		}
		return false;
	}

	private void CreateObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int maxCreatedPerFrame = 10;
		if (InLoadingScreen())
		{
			maxCreatedPerFrame = 100;
		}
		int frameCount = Time.frameCount;
		foreach (ZDO key in m_instances.Keys)
		{
			key.m_tempCreateEarmark = frameCount;
		}
		int created = 0;
		CreateObjectsSorted(currentNearObjects, maxCreatedPerFrame, ref created);
		CreateDistantObjects(currentDistantObjects, maxCreatedPerFrame, ref created);
	}

	private void CreateObjectsSorted(List<ZDO> currentNearObjects, int maxCreatedPerFrame, ref int created)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		m_tempCurrentObjects2.Clear();
		int frameCount = Time.frameCount;
		foreach (ZDO currentNearObject in currentNearObjects)
		{
			if (currentNearObject.m_tempCreateEarmark != frameCount && (currentNearObject.m_distant || ZoneSystem.instance.IsZoneLoaded(currentNearObject.GetSector())))
			{
				m_tempCurrentObjects2.Add(currentNearObject);
			}
		}
		foreach (ZDO item in m_tempCurrentObjects2.OrderByDescending((ZDO item) => item.m_type))
		{
			if (CreateObject(item) != null)
			{
				created++;
				if (created > maxCreatedPerFrame)
				{
					break;
				}
			}
			else if (ZNet.instance.IsServer())
			{
				item.SetOwner(ZDOMan.instance.GetMyID());
				ZLog.Log((object)("Destroyed invalid predab ZDO:" + item.m_uid));
				ZDOMan.instance.DestroyZDO(item);
			}
		}
	}

	private void CreateDistantObjects(List<ZDO> objects, int maxCreatedPerFrame, ref int created)
	{
		if (created > maxCreatedPerFrame)
		{
			return;
		}
		int frameCount = Time.frameCount;
		foreach (ZDO @object in objects)
		{
			if (@object.m_tempCreateEarmark == frameCount)
			{
				continue;
			}
			if (CreateObject(@object) != null)
			{
				created++;
				if (created > maxCreatedPerFrame)
				{
					break;
				}
			}
			else if (ZNet.instance.IsServer())
			{
				@object.SetOwner(ZDOMan.instance.GetMyID());
				ZLog.Log((object)string.Concat("Destroyed invalid predab ZDO:", @object.m_uid, "  prefab hash:", @object.GetPrefab()));
				ZDOMan.instance.DestroyZDO(@object);
			}
		}
	}

	private void OnZDODestroyed(ZDO zdo)
	{
		if (m_instances.TryGetValue(zdo, out var value))
		{
			value.ResetZDO();
			UnityEngine.Object.Destroy(value.gameObject);
			m_instances.Remove(zdo);
		}
	}

	private void RemoveObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int frameCount = Time.frameCount;
		foreach (ZDO currentNearObject in currentNearObjects)
		{
			currentNearObject.m_tempRemoveEarmark = frameCount;
		}
		foreach (ZDO currentDistantObject in currentDistantObjects)
		{
			currentDistantObject.m_tempRemoveEarmark = frameCount;
		}
		m_tempRemoved.Clear();
		foreach (ZNetView value in m_instances.Values)
		{
			if (value.GetZDO().m_tempRemoveEarmark != frameCount)
			{
				m_tempRemoved.Add(value);
			}
		}
		for (int i = 0; i < m_tempRemoved.Count; i++)
		{
			ZNetView zNetView = m_tempRemoved[i];
			ZDO zDO = zNetView.GetZDO();
			zNetView.ResetZDO();
			UnityEngine.Object.Destroy(zNetView.gameObject);
			if (!zDO.m_persistent && zDO.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zDO);
			}
			m_instances.Remove(zDO);
		}
	}

	public ZNetView FindInstance(ZDO zdo)
	{
		if (m_instances.TryGetValue(zdo, out var value))
		{
			return value;
		}
		return null;
	}

	public bool HaveInstance(ZDO zdo)
	{
		return m_instances.ContainsKey(zdo);
	}

	public GameObject FindInstance(ZDOID id)
	{
		ZDO zDO = ZDOMan.instance.GetZDO(id);
		if (zDO != null)
		{
			ZNetView zNetView = FindInstance(zDO);
			if ((bool)zNetView)
			{
				return zNetView.gameObject;
			}
		}
		return null;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		m_createDestroyTimer += deltaTime;
		if (m_createDestroyTimer >= 71f / (678f * (float)Math.PI))
		{
			m_createDestroyTimer = 0f;
			CreateDestroyObjects();
		}
	}

	private void CreateDestroyObjects()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		Vector2i zone = ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition());
		m_tempCurrentObjects.Clear();
		m_tempCurrentDistantObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, m_tempCurrentObjects, m_tempCurrentDistantObjects);
		CreateObjects(m_tempCurrentObjects, m_tempCurrentDistantObjects);
		RemoveObjects(m_tempCurrentObjects, m_tempCurrentDistantObjects);
	}

	public bool InActiveArea(Vector2i zone, Vector3 refPoint)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		Vector2i zone2 = ZoneSystem.instance.GetZone(refPoint);
		return InActiveArea(zone, zone2);
	}

	public bool InActiveArea(Vector2i zone, Vector2i refCenterZone)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		int num = ZoneSystem.instance.m_activeArea - 1;
		if (zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num)
		{
			return zone.y >= refCenterZone.y - num;
		}
		return false;
	}

	public bool OutsideActiveArea(Vector3 point)
	{
		return OutsideActiveArea(point, ZNet.instance.GetReferencePosition());
	}

	public bool OutsideActiveArea(Vector3 point, Vector3 refPoint)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		Vector2i zone = ZoneSystem.instance.GetZone(refPoint);
		Vector2i zone2 = ZoneSystem.instance.GetZone(point);
		if (zone2.x > zone.x - ZoneSystem.instance.m_activeArea && zone2.x < zone.x + ZoneSystem.instance.m_activeArea && zone2.y < zone.y + ZoneSystem.instance.m_activeArea)
		{
			return zone2.y <= zone.y - ZoneSystem.instance.m_activeArea;
		}
		return true;
	}

	public bool HaveInstanceInSector(Vector2i sector)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<ZDO, ZNetView> instance in m_instances)
		{
			if ((bool)instance.Value && !instance.Value.m_distant && ZoneSystem.instance.GetZone(instance.Value.transform.position) == sector)
			{
				return true;
			}
		}
		return false;
	}

	public int NrOfInstances()
	{
		return m_instances.Count;
	}

	public void SpawnObject(Vector3 pos, Quaternion rot, GameObject prefab)
	{
		int prefabHash = GetPrefabHash(prefab);
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SpawnObject", pos, rot, prefabHash);
	}

	private void RPC_SpawnObject(long spawner, Vector3 pos, Quaternion rot, int prefabHash)
	{
		GameObject prefab = GetPrefab(prefabHash);
		if (prefab == null)
		{
			ZLog.Log((object)("Missing prefab " + prefabHash));
		}
		else
		{
			UnityEngine.Object.Instantiate(prefab, pos, rot);
		}
	}
}
