using System;
using System.Collections.Generic;
using UnityEngine;

public class ZDO : IEquatable<ZDO>
{
	public enum ObjectType
	{
		Default,
		Prioritized,
		Solid
	}

	public ZDOID m_uid;

	public bool m_persistent;

	public bool m_distant;

	public long m_owner;

	public long m_timeCreated;

	public uint m_ownerRevision;

	public uint m_dataRevision;

	public int m_pgwVersion;

	public ObjectType m_type;

	public float m_tempSortValue;

	public bool m_tempHaveRevision;

	public int m_tempRemoveEarmark = -1;

	public int m_tempCreateEarmark = -1;

	private int m_prefab;

	private Vector2i m_sector = Vector2i.zero;

	private Vector3 m_position = Vector3.zero;

	private Quaternion m_rotation = Quaternion.identity;

	private Dictionary<int, float> m_floats;

	private Dictionary<int, Vector3> m_vec3;

	private Dictionary<int, Quaternion> m_quats;

	private Dictionary<int, int> m_ints;

	private Dictionary<int, long> m_longs;

	private Dictionary<int, string> m_strings;

	private ZDOMan m_zdoMan;

	public void Initialize(ZDOMan man, ZDOID id, Vector3 position)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		m_zdoMan = man;
		m_uid = id;
		m_position = position;
		m_sector = ZoneSystem.instance.GetZone(m_position);
		m_zdoMan.AddToSector(this, m_sector);
	}

	public void Initialize(ZDOMan man)
	{
		m_zdoMan = man;
	}

	public bool IsValid()
	{
		return m_zdoMan != null;
	}

	public void Reset()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		m_uid = ZDOID.None;
		m_persistent = false;
		m_owner = 0L;
		m_timeCreated = 0L;
		m_ownerRevision = 0u;
		m_dataRevision = 0u;
		m_pgwVersion = 0;
		m_distant = false;
		m_tempSortValue = 0f;
		m_tempHaveRevision = false;
		m_prefab = 0;
		m_sector = Vector2i.zero;
		m_position = Vector3.zero;
		m_rotation = Quaternion.identity;
		ReleaseFloats();
		ReleaseVec3();
		ReleaseQuats();
		ReleaseInts();
		ReleaseLongs();
		ReleaseStrings();
		m_zdoMan = null;
	}

	public ZDO Clone()
	{
		ZDO zDO = MemberwiseClone() as ZDO;
		zDO.m_floats = null;
		zDO.m_vec3 = null;
		zDO.m_quats = null;
		zDO.m_ints = null;
		zDO.m_longs = null;
		zDO.m_strings = null;
		if (m_floats != null && m_floats.Count > 0)
		{
			zDO.InitFloats();
			DictionaryExt.Copy<int, float>(zDO.m_floats, m_floats);
		}
		if (m_vec3 != null && m_vec3.Count > 0)
		{
			zDO.InitVec3();
			DictionaryExt.Copy<int, Vector3>(zDO.m_vec3, m_vec3);
		}
		if (m_quats != null && m_quats.Count > 0)
		{
			zDO.InitQuats();
			DictionaryExt.Copy<int, Quaternion>(zDO.m_quats, m_quats);
		}
		if (m_ints != null && m_ints.Count > 0)
		{
			zDO.InitInts();
			DictionaryExt.Copy<int, int>(zDO.m_ints, m_ints);
		}
		if (m_longs != null && m_longs.Count > 0)
		{
			zDO.InitLongs();
			DictionaryExt.Copy<int, long>(zDO.m_longs, m_longs);
		}
		if (m_strings != null && m_strings.Count > 0)
		{
			zDO.InitStrings();
			DictionaryExt.Copy<int, string>(zDO.m_strings, m_strings);
		}
		return zDO;
	}

	public bool Equals(ZDO other)
	{
		return this == other;
	}

	public void Set(KeyValuePair<int, int> hashPair, ZDOID id)
	{
		Set(hashPair.Key, id.userID);
		Set(hashPair.Value, id.id);
	}

	public static KeyValuePair<int, int> GetHashZDOID(string name)
	{
		return new KeyValuePair<int, int>(StringExtensionMethods.GetStableHashCode(name + "_u"), StringExtensionMethods.GetStableHashCode(name + "_i"));
	}

	public void Set(string name, ZDOID id)
	{
		Set(GetHashZDOID(name), id);
	}

	public ZDOID GetZDOID(KeyValuePair<int, int> hashPair)
	{
		long @long = GetLong(hashPair.Key, 0L);
		uint num = (uint)GetLong(hashPair.Value, 0L);
		if (@long == 0L || num == 0)
		{
			return ZDOID.None;
		}
		return new ZDOID(@long, num);
	}

	public ZDOID GetZDOID(string name)
	{
		return GetZDOID(GetHashZDOID(name));
	}

	public void Set(string name, float value)
	{
		int stableHashCode = StringExtensionMethods.GetStableHashCode(name);
		Set(stableHashCode, value);
	}

	public void Set(int hash, float value)
	{
		InitFloats();
		if (!m_floats.TryGetValue(hash, out var value2) || value2 != value)
		{
			m_floats[hash] = value;
			IncreseDataRevision();
		}
	}

	public void Set(string name, Vector3 value)
	{
		int stableHashCode = StringExtensionMethods.GetStableHashCode(name);
		Set(stableHashCode, value);
	}

	public void Set(int hash, Vector3 value)
	{
		InitVec3();
		if (!m_vec3.TryGetValue(hash, out var value2) || !(value2 == value))
		{
			m_vec3[hash] = value;
			IncreseDataRevision();
		}
	}

	public void Set(string name, Quaternion value)
	{
		int stableHashCode = StringExtensionMethods.GetStableHashCode(name);
		Set(stableHashCode, value);
	}

	public void Set(int hash, Quaternion value)
	{
		InitQuats();
		if (!m_quats.TryGetValue(hash, out var value2) || !(value2 == value))
		{
			m_quats[hash] = value;
			IncreseDataRevision();
		}
	}

	public void Set(string name, int value)
	{
		Set(StringExtensionMethods.GetStableHashCode(name), value);
	}

	public void Set(int hash, int value)
	{
		InitInts();
		if (!m_ints.TryGetValue(hash, out var value2) || value2 != value)
		{
			m_ints[hash] = value;
			IncreseDataRevision();
		}
	}

	public void Set(string name, bool value)
	{
		Set(name, value ? 1 : 0);
	}

	public void Set(int hash, bool value)
	{
		Set(hash, value ? 1 : 0);
	}

	public void Set(string name, long value)
	{
		Set(StringExtensionMethods.GetStableHashCode(name), value);
	}

	public void Set(int hash, long value)
	{
		InitLongs();
		if (!m_longs.TryGetValue(hash, out var value2) || value2 != value)
		{
			m_longs[hash] = value;
			IncreseDataRevision();
		}
	}

	public void Set(string name, byte[] bytes)
	{
		string value = Convert.ToBase64String(bytes);
		Set(name, value);
	}

	public byte[] GetByteArray(string name)
	{
		string @string = GetString(name);
		if (@string.Length > 0)
		{
			return Convert.FromBase64String(@string);
		}
		return null;
	}

	public void Set(string name, string value)
	{
		InitStrings();
		int stableHashCode = StringExtensionMethods.GetStableHashCode(name);
		if (!m_strings.TryGetValue(stableHashCode, out var value2) || !(value2 == value))
		{
			m_strings[stableHashCode] = value;
			IncreseDataRevision();
		}
	}

	public void SetPosition(Vector3 pos)
	{
		InternalSetPosition(pos);
	}

	public void InternalSetPosition(Vector3 pos)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (!(m_position == pos))
		{
			m_position = pos;
			SetSector(ZoneSystem.instance.GetZone(m_position));
			if (IsOwner())
			{
				IncreseDataRevision();
			}
		}
	}

	private void SetSector(Vector2i sector)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (!(m_sector == sector))
		{
			m_zdoMan.RemoveFromSector(this, m_sector);
			m_sector = sector;
			m_zdoMan.AddToSector(this, m_sector);
			m_zdoMan.ZDOSectorInvalidated(this);
		}
	}

	public Vector2i GetSector()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return m_sector;
	}

	public void SetRotation(Quaternion rot)
	{
		if (!(m_rotation == rot))
		{
			m_rotation = rot;
			IncreseDataRevision();
		}
	}

	public void SetType(ObjectType type)
	{
		if (m_type != type)
		{
			m_type = type;
			IncreseDataRevision();
		}
	}

	public void SetDistant(bool distant)
	{
		if (m_distant != distant)
		{
			m_distant = distant;
			IncreseDataRevision();
		}
	}

	public void SetPrefab(int prefab)
	{
		if (m_prefab != prefab)
		{
			m_prefab = prefab;
			IncreseDataRevision();
		}
	}

	public int GetPrefab()
	{
		return m_prefab;
	}

	public Vector3 GetPosition()
	{
		return m_position;
	}

	public Quaternion GetRotation()
	{
		return m_rotation;
	}

	private void IncreseDataRevision()
	{
		m_dataRevision++;
		if (!ZNet.instance.IsServer())
		{
			ZDOMan.instance.ClientChanged(m_uid);
		}
	}

	private void IncreseOwnerRevision()
	{
		m_ownerRevision++;
		if (!ZNet.instance.IsServer())
		{
			ZDOMan.instance.ClientChanged(m_uid);
		}
	}

	public float GetFloat(string name, float defaultValue = 0f)
	{
		return GetFloat(StringExtensionMethods.GetStableHashCode(name), defaultValue);
	}

	public float GetFloat(int hash, float defaultValue = 0f)
	{
		if (m_floats == null)
		{
			return defaultValue;
		}
		if (m_floats.TryGetValue(hash, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public Vector3 GetVec3(string name, Vector3 defaultValue)
	{
		return GetVec3(StringExtensionMethods.GetStableHashCode(name), defaultValue);
	}

	public Vector3 GetVec3(int hash, Vector3 defaultValue)
	{
		if (m_vec3 == null)
		{
			return defaultValue;
		}
		if (m_vec3.TryGetValue(hash, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public Quaternion GetQuaternion(string name, Quaternion defaultValue)
	{
		return GetQuaternion(StringExtensionMethods.GetStableHashCode(name), defaultValue);
	}

	public Quaternion GetQuaternion(int hash, Quaternion defaultValue)
	{
		if (m_quats == null)
		{
			return defaultValue;
		}
		if (m_quats.TryGetValue(hash, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public int GetInt(string name, int defaultValue = 0)
	{
		return GetInt(StringExtensionMethods.GetStableHashCode(name), defaultValue);
	}

	public int GetInt(int hash, int defaultValue = 0)
	{
		if (m_ints == null)
		{
			return defaultValue;
		}
		if (m_ints.TryGetValue(hash, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public bool GetBool(string name, bool defaultValue = false)
	{
		return GetBool(StringExtensionMethods.GetStableHashCode(name), defaultValue);
	}

	public bool GetBool(int hash, bool defaultValue = false)
	{
		if (m_ints == null)
		{
			return defaultValue;
		}
		if (m_ints.TryGetValue(hash, out var value))
		{
			return value != 0;
		}
		return defaultValue;
	}

	public long GetLong(string name, long defaultValue = 0L)
	{
		return GetLong(StringExtensionMethods.GetStableHashCode(name), defaultValue);
	}

	public long GetLong(int hash, long defaultValue = 0L)
	{
		if (m_longs == null)
		{
			return defaultValue;
		}
		if (m_longs.TryGetValue(hash, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public string GetString(string name, string defaultValue = "")
	{
		if (m_strings == null)
		{
			return defaultValue;
		}
		if (m_strings.TryGetValue(StringExtensionMethods.GetStableHashCode(name), out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public void Serialize(ZPackage pkg)
	{
		pkg.Write(m_persistent);
		pkg.Write(m_distant);
		pkg.Write(m_timeCreated);
		pkg.Write(m_pgwVersion);
		pkg.Write((sbyte)m_type);
		pkg.Write(m_prefab);
		pkg.Write(m_rotation);
		int num = 0;
		if (m_floats != null && m_floats.Count > 0)
		{
			num |= 1;
		}
		if (m_vec3 != null && m_vec3.Count > 0)
		{
			num |= 2;
		}
		if (m_quats != null && m_quats.Count > 0)
		{
			num |= 4;
		}
		if (m_ints != null && m_ints.Count > 0)
		{
			num |= 8;
		}
		if (m_strings != null && m_strings.Count > 0)
		{
			num |= 0x10;
		}
		if (m_longs != null && m_longs.Count > 0)
		{
			num |= 0x40;
		}
		pkg.Write(num);
		if (m_floats != null && m_floats.Count > 0)
		{
			pkg.Write((byte)m_floats.Count);
			foreach (KeyValuePair<int, float> @float in m_floats)
			{
				pkg.Write(@float.Key);
				pkg.Write(@float.Value);
			}
		}
		if (m_vec3 != null && m_vec3.Count > 0)
		{
			pkg.Write((byte)m_vec3.Count);
			foreach (KeyValuePair<int, Vector3> item in m_vec3)
			{
				pkg.Write(item.Key);
				pkg.Write(item.Value);
			}
		}
		if (m_quats != null && m_quats.Count > 0)
		{
			pkg.Write((byte)m_quats.Count);
			foreach (KeyValuePair<int, Quaternion> quat in m_quats)
			{
				pkg.Write(quat.Key);
				pkg.Write(quat.Value);
			}
		}
		if (m_ints != null && m_ints.Count > 0)
		{
			pkg.Write((byte)m_ints.Count);
			foreach (KeyValuePair<int, int> @int in m_ints)
			{
				pkg.Write(@int.Key);
				pkg.Write(@int.Value);
			}
		}
		if (m_longs != null && m_longs.Count > 0)
		{
			pkg.Write((byte)m_longs.Count);
			foreach (KeyValuePair<int, long> @long in m_longs)
			{
				pkg.Write(@long.Key);
				pkg.Write(@long.Value);
			}
		}
		if (m_strings == null || m_strings.Count <= 0)
		{
			return;
		}
		pkg.Write((byte)m_strings.Count);
		foreach (KeyValuePair<int, string> @string in m_strings)
		{
			pkg.Write(@string.Key);
			pkg.Write(@string.Value);
		}
	}

	public void Deserialize(ZPackage pkg)
	{
		m_persistent = pkg.ReadBool();
		m_distant = pkg.ReadBool();
		m_timeCreated = pkg.ReadLong();
		m_pgwVersion = pkg.ReadInt();
		m_type = (ObjectType)pkg.ReadSByte();
		m_prefab = pkg.ReadInt();
		m_rotation = pkg.ReadQuaternion();
		int num = pkg.ReadInt();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			InitFloats();
			int num2 = pkg.ReadByte();
			for (int i = 0; i < num2; i++)
			{
				int key = pkg.ReadInt();
				m_floats[key] = pkg.ReadSingle();
			}
		}
		else
		{
			ReleaseFloats();
		}
		if (((uint)num & 2u) != 0)
		{
			InitVec3();
			int num3 = pkg.ReadByte();
			for (int j = 0; j < num3; j++)
			{
				int key2 = pkg.ReadInt();
				m_vec3[key2] = pkg.ReadVector3();
			}
		}
		else
		{
			ReleaseVec3();
		}
		if (((uint)num & 4u) != 0)
		{
			InitQuats();
			int num4 = pkg.ReadByte();
			for (int k = 0; k < num4; k++)
			{
				int key3 = pkg.ReadInt();
				m_quats[key3] = pkg.ReadQuaternion();
			}
		}
		else
		{
			ReleaseQuats();
		}
		if (((uint)num & 8u) != 0)
		{
			InitInts();
			int num5 = pkg.ReadByte();
			for (int l = 0; l < num5; l++)
			{
				int key4 = pkg.ReadInt();
				m_ints[key4] = pkg.ReadInt();
			}
		}
		else
		{
			ReleaseInts();
		}
		if (((uint)num & 0x40u) != 0)
		{
			InitLongs();
			int num6 = pkg.ReadByte();
			for (int m = 0; m < num6; m++)
			{
				int key5 = pkg.ReadInt();
				m_longs[key5] = pkg.ReadLong();
			}
		}
		else
		{
			ReleaseLongs();
		}
		if (((uint)num & 0x10u) != 0)
		{
			InitStrings();
			int num7 = pkg.ReadByte();
			for (int n = 0; n < num7; n++)
			{
				int key6 = pkg.ReadInt();
				m_strings[key6] = pkg.ReadString();
			}
		}
		else
		{
			ReleaseStrings();
		}
	}

	public void Save(ZPackage pkg)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		pkg.Write(m_ownerRevision);
		pkg.Write(m_dataRevision);
		pkg.Write(m_persistent);
		pkg.Write(m_owner);
		pkg.Write(m_timeCreated);
		pkg.Write(m_pgwVersion);
		pkg.Write((sbyte)m_type);
		pkg.Write(m_distant);
		pkg.Write(m_prefab);
		pkg.Write(m_sector);
		pkg.Write(m_position);
		pkg.Write(m_rotation);
		if (m_floats != null)
		{
			pkg.Write((char)m_floats.Count);
			foreach (KeyValuePair<int, float> @float in m_floats)
			{
				pkg.Write(@float.Key);
				pkg.Write(@float.Value);
			}
		}
		else
		{
			pkg.Write('\0');
		}
		if (m_vec3 != null)
		{
			pkg.Write((char)m_vec3.Count);
			foreach (KeyValuePair<int, Vector3> item in m_vec3)
			{
				pkg.Write(item.Key);
				pkg.Write(item.Value);
			}
		}
		else
		{
			pkg.Write('\0');
		}
		if (m_quats != null)
		{
			pkg.Write((char)m_quats.Count);
			foreach (KeyValuePair<int, Quaternion> quat in m_quats)
			{
				pkg.Write(quat.Key);
				pkg.Write(quat.Value);
			}
		}
		else
		{
			pkg.Write('\0');
		}
		if (m_ints != null)
		{
			pkg.Write((char)m_ints.Count);
			foreach (KeyValuePair<int, int> @int in m_ints)
			{
				pkg.Write(@int.Key);
				pkg.Write(@int.Value);
			}
		}
		else
		{
			pkg.Write('\0');
		}
		if (m_longs != null)
		{
			pkg.Write((char)m_longs.Count);
			foreach (KeyValuePair<int, long> @long in m_longs)
			{
				pkg.Write(@long.Key);
				pkg.Write(@long.Value);
			}
		}
		else
		{
			pkg.Write('\0');
		}
		if (m_strings != null)
		{
			pkg.Write((char)m_strings.Count);
			foreach (KeyValuePair<int, string> @string in m_strings)
			{
				pkg.Write(@string.Key);
				pkg.Write(@string.Value);
			}
		}
		else
		{
			pkg.Write('\0');
		}
	}

	public void Load(ZPackage pkg, int version)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		m_ownerRevision = pkg.ReadUInt();
		m_dataRevision = pkg.ReadUInt();
		m_persistent = pkg.ReadBool();
		m_owner = pkg.ReadLong();
		m_timeCreated = pkg.ReadLong();
		m_pgwVersion = pkg.ReadInt();
		if (version >= 16 && version < 24)
		{
			pkg.ReadInt();
		}
		if (version >= 23)
		{
			m_type = (ObjectType)pkg.ReadSByte();
		}
		if (version >= 22)
		{
			m_distant = pkg.ReadBool();
		}
		if (version < 13)
		{
			pkg.ReadChar();
			pkg.ReadChar();
		}
		if (version >= 17)
		{
			m_prefab = pkg.ReadInt();
		}
		m_sector = pkg.ReadVector2i();
		m_position = pkg.ReadVector3();
		m_rotation = pkg.ReadQuaternion();
		int num = pkg.ReadChar();
		if (num > 0)
		{
			InitFloats();
			for (int i = 0; i < num; i++)
			{
				int key = pkg.ReadInt();
				m_floats[key] = pkg.ReadSingle();
			}
		}
		else
		{
			ReleaseFloats();
		}
		int num2 = pkg.ReadChar();
		if (num2 > 0)
		{
			InitVec3();
			for (int j = 0; j < num2; j++)
			{
				int key2 = pkg.ReadInt();
				m_vec3[key2] = pkg.ReadVector3();
			}
		}
		else
		{
			ReleaseVec3();
		}
		int num3 = pkg.ReadChar();
		if (num3 > 0)
		{
			InitQuats();
			for (int k = 0; k < num3; k++)
			{
				int key3 = pkg.ReadInt();
				m_quats[key3] = pkg.ReadQuaternion();
			}
		}
		else
		{
			ReleaseQuats();
		}
		int num4 = pkg.ReadChar();
		if (num4 > 0)
		{
			InitInts();
			for (int l = 0; l < num4; l++)
			{
				int key4 = pkg.ReadInt();
				m_ints[key4] = pkg.ReadInt();
			}
		}
		else
		{
			ReleaseInts();
		}
		int num5 = pkg.ReadChar();
		if (num5 > 0)
		{
			InitLongs();
			for (int m = 0; m < num5; m++)
			{
				int key5 = pkg.ReadInt();
				m_longs[key5] = pkg.ReadLong();
			}
		}
		else
		{
			ReleaseLongs();
		}
		int num6 = pkg.ReadChar();
		if (num6 > 0)
		{
			InitStrings();
			for (int n = 0; n < num6; n++)
			{
				int key6 = pkg.ReadInt();
				m_strings[key6] = pkg.ReadString();
			}
		}
		else
		{
			ReleaseStrings();
		}
		if (version < 17)
		{
			m_prefab = GetInt("prefab");
		}
	}

	public bool IsOwner()
	{
		return m_owner == m_zdoMan.GetMyID();
	}

	public bool HasOwner()
	{
		return m_owner != 0;
	}

	public void Print()
	{
		ZLog.Log((object)("UID:" + m_uid));
		ZLog.Log((object)("Persistent:" + m_persistent));
		ZLog.Log((object)("Owner:" + m_owner));
		ZLog.Log((object)("Revision:" + m_ownerRevision));
		foreach (KeyValuePair<int, float> @float in m_floats)
		{
			ZLog.Log((object)("F:" + @float.Key + " = " + @float.Value));
		}
	}

	public void SetOwner(long uid)
	{
		if (m_owner != uid)
		{
			m_owner = uid;
			IncreseOwnerRevision();
		}
	}

	public void SetPGWVersion(int version)
	{
		m_pgwVersion = version;
	}

	public int GetPGWVersion()
	{
		return m_pgwVersion;
	}

	private void InitFloats()
	{
		if (m_floats == null)
		{
			m_floats = Pool<Dictionary<int, float>>.Create();
			m_floats.Clear();
		}
	}

	private void InitVec3()
	{
		if (m_vec3 == null)
		{
			m_vec3 = Pool<Dictionary<int, Vector3>>.Create();
			m_vec3.Clear();
		}
	}

	private void InitQuats()
	{
		if (m_quats == null)
		{
			m_quats = Pool<Dictionary<int, Quaternion>>.Create();
			m_quats.Clear();
		}
	}

	private void InitInts()
	{
		if (m_ints == null)
		{
			m_ints = Pool<Dictionary<int, int>>.Create();
			m_ints.Clear();
		}
	}

	private void InitLongs()
	{
		if (m_longs == null)
		{
			m_longs = Pool<Dictionary<int, long>>.Create();
			m_longs.Clear();
		}
	}

	private void InitStrings()
	{
		if (m_strings == null)
		{
			m_strings = Pool<Dictionary<int, string>>.Create();
			m_strings.Clear();
		}
	}

	private void ReleaseFloats()
	{
		if (m_floats != null)
		{
			Pool<Dictionary<int, float>>.Release(m_floats);
			m_floats = null;
		}
	}

	private void ReleaseVec3()
	{
		if (m_vec3 != null)
		{
			Pool<Dictionary<int, Vector3>>.Release(m_vec3);
			m_vec3 = null;
		}
	}

	private void ReleaseQuats()
	{
		if (m_quats != null)
		{
			Pool<Dictionary<int, Quaternion>>.Release(m_quats);
			m_quats = null;
		}
	}

	private void ReleaseInts()
	{
		if (m_ints != null)
		{
			Pool<Dictionary<int, int>>.Release(m_ints);
			m_ints = null;
		}
	}

	private void ReleaseLongs()
	{
		if (m_longs != null)
		{
			Pool<Dictionary<int, long>>.Release(m_longs);
			m_longs = null;
		}
	}

	private void ReleaseStrings()
	{
		if (m_strings != null)
		{
			Pool<Dictionary<int, string>>.Release(m_strings);
			m_strings = null;
		}
	}
}
