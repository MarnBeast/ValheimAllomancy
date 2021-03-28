using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Heightmap : MonoBehaviour
{
	public enum Biome
	{
		None = 0,
		Meadows = 1,
		Swamp = 2,
		Mountain = 4,
		BlackForest = 8,
		Plains = 0x10,
		AshLands = 0x20,
		DeepNorth = 0x40,
		Ocean = 0x100,
		Mistlands = 0x200,
		BiomesMax = 513
	}

	public enum BiomeArea
	{
		Edge = 1,
		Median,
		Everything
	}

	private static float[] tempBiomeWeights = new float[513];

	private static List<Heightmap> tempHmaps = new List<Heightmap>();

	public int m_width = 32;

	public float m_scale = 1f;

	public Material m_material;

	private const float m_levelMaxDelta = 8f;

	private const float m_smoothMaxDelta = 1f;

	public bool m_isDistantLod;

	public bool m_distantLodEditorHax;

	private List<float> m_heights = new List<float>();

	private HeightmapBuilder.HMBuildData m_buildData;

	private Texture2D m_clearedMask;

	private Material m_materialInstance;

	private MeshCollider m_collider;

	private float[] m_oceanDepth = new float[4];

	private Biome[] m_cornerBiomes = new Biome[4]
	{
		Biome.Meadows,
		Biome.Meadows,
		Biome.Meadows,
		Biome.Meadows
	};

	private Bounds m_bounds;

	private BoundingSphere m_boundingSphere;

	private Mesh m_collisionMesh;

	private Mesh m_renderMesh;

	private bool m_dirty;

	private static List<Heightmap> m_heightmaps = new List<Heightmap>();

	private static List<Vector3> m_tempVertises = new List<Vector3>();

	private static List<Vector2> m_tempUVs = new List<Vector2>();

	private static List<int> m_tempIndices = new List<int>();

	private static List<Color32> m_tempColors = new List<Color32>();

	private void Awake()
	{
		if (!m_isDistantLod)
		{
			m_heightmaps.Add(this);
		}
		m_collider = GetComponent<MeshCollider>();
	}

	private void OnDestroy()
	{
		if (!m_isDistantLod)
		{
			m_heightmaps.Remove(this);
		}
		if ((bool)m_materialInstance)
		{
			Object.DestroyImmediate(m_materialInstance);
		}
	}

	private void OnEnable()
	{
		if (!m_isDistantLod || !Application.isPlaying || m_distantLodEditorHax)
		{
			Regenerate();
		}
	}

	private void Update()
	{
		Render();
	}

	private void Render()
	{
		if (IsVisible())
		{
			if (m_dirty)
			{
				m_dirty = false;
				m_materialInstance.SetTexture("_ClearedMaskTex", m_clearedMask);
				RebuildRenderMesh();
			}
			if ((bool)m_renderMesh)
			{
				Matrix4x4 matrix = Matrix4x4.TRS(base.transform.position, Quaternion.identity, Vector3.one);
				Graphics.DrawMesh(m_renderMesh, matrix, m_materialInstance, base.gameObject.layer);
			}
		}
	}

	private bool IsVisible()
	{
		if (!Utils.InsideMainCamera(m_boundingSphere))
		{
			return false;
		}
		if (!Utils.InsideMainCamera(m_bounds))
		{
			return false;
		}
		return true;
	}

	public static void ForceGenerateAll()
	{
		foreach (Heightmap heightmap in m_heightmaps)
		{
			if (heightmap.HaveQueuedRebuild())
			{
				ZLog.Log((object)("Force generaeting hmap " + heightmap.transform.position));
				heightmap.Regenerate();
			}
		}
	}

	public void Poke(bool delayed)
	{
		if (delayed)
		{
			if (HaveQueuedRebuild())
			{
				CancelInvoke("Regenerate");
			}
			InvokeRepeating("Regenerate", 0.1f, 0f);
		}
		else
		{
			Regenerate();
		}
	}

	public bool HaveQueuedRebuild()
	{
		return IsInvoking("Regenerate");
	}

	public void Regenerate()
	{
		if (HaveQueuedRebuild())
		{
			CancelInvoke("Regenerate");
		}
		Generate();
		RebuildCollisionMesh();
		UpdateCornerDepths();
		m_dirty = true;
	}

	private void UpdateCornerDepths()
	{
		float num = (ZoneSystem.instance ? ZoneSystem.instance.m_waterLevel : 30f);
		m_oceanDepth[0] = GetHeight(0, m_width);
		m_oceanDepth[1] = GetHeight(m_width, m_width);
		m_oceanDepth[2] = GetHeight(m_width, 0);
		m_oceanDepth[3] = GetHeight(0, 0);
		m_oceanDepth[0] = Mathf.Max(0f, num - m_oceanDepth[0]);
		m_oceanDepth[1] = Mathf.Max(0f, num - m_oceanDepth[1]);
		m_oceanDepth[2] = Mathf.Max(0f, num - m_oceanDepth[2]);
		m_oceanDepth[3] = Mathf.Max(0f, num - m_oceanDepth[3]);
		m_materialInstance.SetFloatArray("_depth", m_oceanDepth);
	}

	public float[] GetOceanDepth()
	{
		return m_oceanDepth;
	}

	public static float GetOceanDepthAll(Vector3 worldPos)
	{
		Heightmap heightmap = FindHeightmap(worldPos);
		if ((bool)heightmap)
		{
			return heightmap.GetOceanDepth(worldPos);
		}
		return 0f;
	}

	public float GetOceanDepth(Vector3 worldPos)
	{
		WorldToVertex(worldPos, out var x, out var y);
		float t = (float)x / (float)m_width;
		float t2 = (float)y / (float)m_width;
		float a = Mathf.Lerp(m_oceanDepth[3], m_oceanDepth[2], t);
		float b = Mathf.Lerp(m_oceanDepth[0], m_oceanDepth[1], t);
		return Mathf.Lerp(a, b, t2);
	}

	private void Initialize()
	{
		int num = m_width + 1;
		int num2 = num * num;
		if (m_heights.Count != num2)
		{
			m_heights.Clear();
			for (int i = 0; i < num2; i++)
			{
				m_heights.Add(0f);
			}
			m_clearedMask = new Texture2D(m_width, m_width);
			m_clearedMask.wrapMode = TextureWrapMode.Clamp;
			m_materialInstance = new Material(m_material);
			m_materialInstance.SetTexture("_ClearedMaskTex", m_clearedMask);
		}
	}

	private void Generate()
	{
		Initialize();
		int num = m_width + 1;
		int num2 = num * num;
		Vector3 position = base.transform.position;
		if (m_buildData == null || m_buildData.m_baseHeights.Count != num2 || m_buildData.m_center != position || m_buildData.m_scale != m_scale || m_buildData.m_worldGen != WorldGenerator.instance)
		{
			m_buildData = HeightmapBuilder.instance.RequestTerrainSync(position, m_width, m_scale, m_isDistantLod, WorldGenerator.instance);
			m_cornerBiomes = m_buildData.m_cornerBiomes;
		}
		for (int i = 0; i < num2; i++)
		{
			m_heights[i] = m_buildData.m_baseHeights[i];
		}
		Color[] pixels = new Color[m_clearedMask.width * m_clearedMask.height];
		m_clearedMask.SetPixels(pixels);
		ApplyModifiers();
	}

	private float Distance(float x, float y, float rx, float ry)
	{
		float num = x - rx;
		float num2 = y - ry;
		float num3 = Mathf.Sqrt(num * num + num2 * num2);
		float num4 = 1.414f - num3;
		return num4 * num4 * num4;
	}

	public List<Biome> GetBiomes()
	{
		List<Biome> list = new List<Biome>();
		Biome[] cornerBiomes = m_cornerBiomes;
		foreach (Biome item in cornerBiomes)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public bool HaveBiome(Biome biome)
	{
		if ((m_cornerBiomes[0] & biome) == 0 && (m_cornerBiomes[1] & biome) == 0 && (m_cornerBiomes[2] & biome) == 0)
		{
			return (m_cornerBiomes[3] & biome) != 0;
		}
		return true;
	}

	public Biome GetBiome(Vector3 point)
	{
		if (m_isDistantLod)
		{
			return WorldGenerator.instance.GetBiome(point.x, point.z);
		}
		if (m_cornerBiomes[0] == m_cornerBiomes[1] && m_cornerBiomes[0] == m_cornerBiomes[2] && m_cornerBiomes[0] == m_cornerBiomes[3])
		{
			return m_cornerBiomes[0];
		}
		float x = point.x;
		float y = point.z;
		WorldToNormalizedHM(point, out x, out y);
		for (int i = 1; i < tempBiomeWeights.Length; i++)
		{
			tempBiomeWeights[i] = 0f;
		}
		tempBiomeWeights[(int)m_cornerBiomes[0]] += Distance(x, y, 0f, 0f);
		tempBiomeWeights[(int)m_cornerBiomes[1]] += Distance(x, y, 1f, 0f);
		tempBiomeWeights[(int)m_cornerBiomes[2]] += Distance(x, y, 0f, 1f);
		tempBiomeWeights[(int)m_cornerBiomes[3]] += Distance(x, y, 1f, 1f);
		int result = 0;
		float num = -99999f;
		for (int j = 1; j < tempBiomeWeights.Length; j++)
		{
			if (tempBiomeWeights[j] > num)
			{
				result = j;
				num = tempBiomeWeights[j];
			}
		}
		return (Biome)result;
	}

	public BiomeArea GetBiomeArea()
	{
		if (IsBiomeEdge())
		{
			return BiomeArea.Edge;
		}
		return BiomeArea.Median;
	}

	public bool IsBiomeEdge()
	{
		if (m_cornerBiomes[0] == m_cornerBiomes[1] && m_cornerBiomes[0] == m_cornerBiomes[2] && m_cornerBiomes[0] == m_cornerBiomes[3])
		{
			return false;
		}
		return true;
	}

	private void ApplyModifiers()
	{
		List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
		float[] array = null;
		float[] levelOnly = null;
		foreach (TerrainModifier item in allInstances)
		{
			if (item.enabled && TerrainVSModifier(item))
			{
				if (item.m_playerModifiction && array == null)
				{
					array = m_heights.ToArray();
					levelOnly = m_heights.ToArray();
				}
				ApplyModifier(item, array, levelOnly);
			}
		}
		m_clearedMask.Apply();
	}

	private void ApplyModifier(TerrainModifier modifier, float[] baseHeights, float[] levelOnly)
	{
		if (modifier.m_level)
		{
			LevelTerrain(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_levelRadius, modifier.m_square, baseHeights, levelOnly, modifier.m_playerModifiction);
		}
		if (modifier.m_smooth)
		{
			SmoothTerrain2(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_smoothRadius, modifier.m_square, levelOnly, modifier.m_smoothPower, modifier.m_playerModifiction);
		}
		if (modifier.m_paintCleared)
		{
			PaintCleared(modifier.transform.position, modifier.m_paintRadius, modifier.m_paintType, modifier.m_paintHeightCheck, apply: false);
		}
	}

	public bool TerrainVSModifier(TerrainModifier modifier)
	{
		Vector3 position = modifier.transform.position;
		float num = modifier.GetRadius() + 4f;
		Vector3 position2 = base.transform.position;
		float num2 = (float)m_width * m_scale * 0.5f;
		if (position.x + num < position2.x - num2)
		{
			return false;
		}
		if (position.x - num > position2.x + num2)
		{
			return false;
		}
		if (position.z + num < position2.z - num2)
		{
			return false;
		}
		if (position.z - num > position2.z + num2)
		{
			return false;
		}
		return true;
	}

	private Vector3 CalcNormal2(List<Vector3> vertises, int x, int y)
	{
		int num = m_width + 1;
		Vector3 vector = vertises[y * num + x];
		Vector3 rhs;
		if (x != m_width)
		{
			rhs = ((x != 0) ? (vertises[y * num + x + 1] - vertises[y * num + x - 1]) : (vertises[y * num + x + 1] - vector));
		}
		else
		{
			Vector3 b = vertises[y * num + x - 1];
			rhs = vector - b;
		}
		Vector3 lhs;
		if (y != m_width)
		{
			lhs = ((y != 0) ? (vertises[(y + 1) * num + x] - vertises[(y - 1) * num + x]) : (CalcVertex(x, y + 1) - vector));
		}
		else
		{
			Vector3 b2 = CalcVertex(x, y - 1);
			lhs = vector - b2;
		}
		Vector3 result = Vector3.Cross(lhs, rhs);
		result.Normalize();
		return result;
	}

	private Vector3 CalcNormal(int x, int y)
	{
		Vector3 vector = CalcVertex(x, y);
		Vector3 rhs;
		if (x == m_width)
		{
			Vector3 b = CalcVertex(x - 1, y);
			rhs = vector - b;
		}
		else
		{
			rhs = CalcVertex(x + 1, y) - vector;
		}
		Vector3 lhs;
		if (y == m_width)
		{
			Vector3 b2 = CalcVertex(x, y - 1);
			lhs = vector - b2;
		}
		else
		{
			lhs = CalcVertex(x, y + 1) - vector;
		}
		return Vector3.Cross(lhs, rhs).normalized;
	}

	private Vector3 CalcVertex(int x, int y)
	{
		int num = m_width + 1;
		return new Vector3((float)m_width * m_scale * -0.5f, 0f, (float)m_width * m_scale * -0.5f) + new Vector3(y: m_heights[y * num + x], x: (float)x * m_scale, z: (float)y * m_scale);
	}

	private Color GetBiomeColor(float ix, float iy)
	{
		if (m_cornerBiomes[0] == m_cornerBiomes[1] && m_cornerBiomes[0] == m_cornerBiomes[2] && m_cornerBiomes[0] == m_cornerBiomes[3])
		{
			return GetBiomeColor(m_cornerBiomes[0]);
		}
		Color32 biomeColor = GetBiomeColor(m_cornerBiomes[0]);
		Color32 biomeColor2 = GetBiomeColor(m_cornerBiomes[1]);
		Color32 biomeColor3 = GetBiomeColor(m_cornerBiomes[2]);
		Color32 biomeColor4 = GetBiomeColor(m_cornerBiomes[3]);
		Color32 a = Color32.Lerp(biomeColor, biomeColor2, ix);
		Color32 b = Color32.Lerp(biomeColor3, biomeColor4, ix);
		return Color32.Lerp(a, b, iy);
	}

	public static Color32 GetBiomeColor(Biome biome)
	{
		return biome switch
		{
			Biome.Swamp => new Color32(byte.MaxValue, 0, 0, 0), 
			Biome.Mountain => new Color32(0, byte.MaxValue, 0, 0), 
			Biome.BlackForest => new Color32(0, 0, byte.MaxValue, 0), 
			Biome.Plains => new Color32(0, 0, 0, byte.MaxValue), 
			Biome.AshLands => new Color32(byte.MaxValue, 0, 0, byte.MaxValue), 
			Biome.DeepNorth => new Color32(0, byte.MaxValue, 0, 0), 
			Biome.Mistlands => new Color32(0, 0, byte.MaxValue, byte.MaxValue), 
			_ => new Color32(0, 0, 0, 0), 
		};
	}

	private void RebuildCollisionMesh()
	{
		if (m_collisionMesh == null)
		{
			m_collisionMesh = new Mesh();
		}
		int num = m_width + 1;
		float num2 = -999999f;
		float num3 = 999999f;
		m_tempVertises.Clear();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 item = CalcVertex(j, i);
				m_tempVertises.Add(item);
				if (item.y > num2)
				{
					num2 = item.y;
				}
				if (item.y < num3)
				{
					num3 = item.y;
				}
			}
		}
		m_collisionMesh.SetVertices(m_tempVertises);
		int num4 = (num - 1) * (num - 1) * 6;
		if (m_collisionMesh.GetIndexCount(0) != num4)
		{
			m_tempIndices.Clear();
			for (int k = 0; k < num - 1; k++)
			{
				for (int l = 0; l < num - 1; l++)
				{
					int item2 = k * num + l;
					int item3 = k * num + l + 1;
					int item4 = (k + 1) * num + l + 1;
					int item5 = (k + 1) * num + l;
					m_tempIndices.Add(item2);
					m_tempIndices.Add(item5);
					m_tempIndices.Add(item3);
					m_tempIndices.Add(item3);
					m_tempIndices.Add(item5);
					m_tempIndices.Add(item4);
				}
			}
			m_collisionMesh.SetIndices(m_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		if ((bool)(Object)(object)m_collider)
		{
			m_collider.set_sharedMesh(m_collisionMesh);
		}
		float num5 = (float)m_width * m_scale * 0.5f;
		m_bounds.SetMinMax(base.transform.position + new Vector3(0f - num5, num3, 0f - num5), base.transform.position + new Vector3(num5, num2, num5));
		m_boundingSphere.position = m_bounds.center;
		m_boundingSphere.radius = Vector3.Distance(m_boundingSphere.position, m_bounds.max);
	}

	private void RebuildRenderMesh()
	{
		if (m_renderMesh == null)
		{
			m_renderMesh = new Mesh();
		}
		WorldGenerator instance = WorldGenerator.instance;
		int num = m_width + 1;
		Vector3 vector = base.transform.position + new Vector3((float)m_width * m_scale * -0.5f, 0f, (float)m_width * m_scale * -0.5f);
		m_tempVertises.Clear();
		m_tempUVs.Clear();
		m_tempIndices.Clear();
		m_tempColors.Clear();
		for (int i = 0; i < num; i++)
		{
			float iy = Mathf.SmoothStep(0f, 1f, (float)i / (float)m_width);
			for (int j = 0; j < num; j++)
			{
				float ix = Mathf.SmoothStep(0f, 1f, (float)j / (float)m_width);
				m_tempUVs.Add(new Vector2((float)j / (float)m_width, (float)i / (float)m_width));
				if (m_isDistantLod)
				{
					float wx = vector.x + (float)j * m_scale;
					float wy = vector.z + (float)i * m_scale;
					Biome biome = instance.GetBiome(wx, wy);
					m_tempColors.Add(GetBiomeColor(biome));
				}
				else
				{
					m_tempColors.Add(GetBiomeColor(ix, iy));
				}
			}
		}
		m_collisionMesh.GetVertices(m_tempVertises);
		m_collisionMesh.GetIndices(m_tempIndices, 0);
		m_renderMesh.Clear();
		m_renderMesh.SetVertices(m_tempVertises);
		m_renderMesh.SetColors(m_tempColors);
		m_renderMesh.SetUVs(0, m_tempUVs);
		m_renderMesh.SetIndices(m_tempIndices.ToArray(), MeshTopology.Triangles, 0, calculateBounds: true);
		m_renderMesh.RecalculateNormals();
		m_renderMesh.RecalculateTangents();
	}

	private void SmoothTerrain2(Vector3 worldPos, float radius, bool square, float[] levelOnlyHeights, float power, bool playerModifiction)
	{
		WorldToVertex(worldPos, out var x, out var y);
		float b = worldPos.y - base.transform.position.y;
		float num = radius / m_scale;
		int num2 = Mathf.CeilToInt(num);
		Vector2 a = new Vector2(x, y);
		int num3 = m_width + 1;
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				float num4 = Vector2.Distance(a, new Vector2(j, i));
				if (num4 > num)
				{
					continue;
				}
				float num5 = num4 / num;
				if (j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					num5 = ((power != 3f) ? Mathf.Pow(num5, power) : (num5 * num5 * num5));
					float height = GetHeight(j, i);
					float t = 1f - num5;
					float num6 = Mathf.Lerp(height, b, t);
					if (playerModifiction)
					{
						float num7 = levelOnlyHeights[i * num3 + j];
						num6 = Mathf.Clamp(num6, num7 - 1f, num7 + 1f);
					}
					SetHeight(j, i, num6);
				}
			}
		}
	}

	private bool AtMaxWorldLevelDepth(Vector3 worldPos)
	{
		GetWorldHeight(worldPos, out var height);
		GetWorldBaseHeight(worldPos, out var height2);
		return Mathf.Max(0f - (height - height2), 0f) >= 7.95f;
	}

	private bool GetWorldBaseHeight(Vector3 worldPos, out float height)
	{
		WorldToVertex(worldPos, out var x, out var y);
		int num = m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			height = 0f;
			return false;
		}
		height = m_buildData.m_baseHeights[y * num + x] + base.transform.position.y;
		return true;
	}

	private bool GetWorldHeight(Vector3 worldPos, out float height)
	{
		WorldToVertex(worldPos, out var x, out var y);
		int num = m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			height = 0f;
			return false;
		}
		height = m_heights[y * num + x] + base.transform.position.y;
		return true;
	}

	private bool GetAverageWorldHeight(Vector3 worldPos, float radius, out float height)
	{
		WorldToVertex(worldPos, out var x, out var y);
		float num = radius / m_scale;
		int num2 = Mathf.CeilToInt(num);
		Vector2 a = new Vector2(x, y);
		int num3 = m_width + 1;
		float num4 = 0f;
		int num5 = 0;
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				if (!(Vector2.Distance(a, new Vector2(j, i)) > num) && j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					num4 += GetHeight(j, i);
					num5++;
				}
			}
		}
		if (num5 == 0)
		{
			height = 0f;
			return false;
		}
		height = num4 / (float)num5 + base.transform.position.y;
		return true;
	}

	private bool GetMinWorldHeight(Vector3 worldPos, float radius, out float height)
	{
		WorldToVertex(worldPos, out var x, out var y);
		float num = radius / m_scale;
		int num2 = Mathf.CeilToInt(num);
		Vector2 a = new Vector2(x, y);
		int num3 = m_width + 1;
		height = 99999f;
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				if (!(Vector2.Distance(a, new Vector2(j, i)) > num) && j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					float height2 = GetHeight(j, i);
					if (height2 < height)
					{
						height = height2;
					}
				}
			}
		}
		return height != 99999f;
	}

	private bool GetMaxWorldHeight(Vector3 worldPos, float radius, out float height)
	{
		WorldToVertex(worldPos, out var x, out var y);
		float num = radius / m_scale;
		int num2 = Mathf.CeilToInt(num);
		Vector2 a = new Vector2(x, y);
		int num3 = m_width + 1;
		height = -99999f;
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				if (!(Vector2.Distance(a, new Vector2(j, i)) > num) && j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					float height2 = GetHeight(j, i);
					if (height2 > height)
					{
						height = height2;
					}
				}
			}
		}
		return height != -99999f;
	}

	public static bool AtMaxLevelDepth(Vector3 worldPos)
	{
		Heightmap heightmap = FindHeightmap(worldPos);
		if ((bool)heightmap)
		{
			return heightmap.AtMaxWorldLevelDepth(worldPos);
		}
		return false;
	}

	public static bool GetHeight(Vector3 worldPos, out float height)
	{
		Heightmap heightmap = FindHeightmap(worldPos);
		if ((bool)heightmap && heightmap.GetWorldHeight(worldPos, out height))
		{
			return true;
		}
		height = 0f;
		return false;
	}

	public static bool GetAverageHeight(Vector3 worldPos, float radius, out float height)
	{
		List<Heightmap> list = new List<Heightmap>();
		FindHeightmap(worldPos, radius, list);
		float num = 0f;
		int num2 = 0;
		foreach (Heightmap item in list)
		{
			if (item.GetAverageWorldHeight(worldPos, radius, out var height2))
			{
				num += height2;
				num2++;
			}
		}
		if (num2 > 0)
		{
			height = num / (float)num2;
			return true;
		}
		height = 0f;
		return false;
	}

	private void SmoothTerrain(Vector3 worldPos, float radius, bool square, float intensity)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		WorldToVertex(worldPos, out var x, out var y);
		float num = radius / m_scale;
		int num2 = Mathf.CeilToInt(num);
		Vector2 a = new Vector2(x, y);
		List<KeyValuePair<Vector2i, float>> list = new List<KeyValuePair<Vector2i, float>>();
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				if ((square || !(Vector2.Distance(a, new Vector2(j, i)) > num)) && j != 0 && i != 0 && j != m_width && i != m_width)
				{
					list.Add(new KeyValuePair<Vector2i, float>(new Vector2i(j, i), GetAvgHeight(j, i, 1)));
				}
			}
		}
		foreach (KeyValuePair<Vector2i, float> item in list)
		{
			float h = Mathf.Lerp(GetHeight(item.Key.x, item.Key.y), item.Value, intensity);
			SetHeight(item.Key.x, item.Key.y, h);
		}
	}

	private float GetAvgHeight(int cx, int cy, int w)
	{
		int num = m_width + 1;
		float num2 = 0f;
		int num3 = 0;
		for (int i = cy - w; i <= cy + w; i++)
		{
			for (int j = cx - w; j <= cx + w; j++)
			{
				if (j >= 0 && i >= 0 && j < num && i < num)
				{
					num2 += GetHeight(j, i);
					num3++;
				}
			}
		}
		if (num3 == 0)
		{
			return 0f;
		}
		return num2 / (float)num3;
	}

	private float GroundHeight(Vector3 point)
	{
		Ray ray = new Ray(point + Vector3.up * 100f, Vector3.down);
		RaycastHit val = default(RaycastHit);
		if (((Collider)m_collider).Raycast(ray, ref val, 300f))
		{
			return ((RaycastHit)(ref val)).get_point().y;
		}
		return -10000f;
	}

	private void FindObjectsToMove(Vector3 worldPos, float area, List<Rigidbody> objects)
	{
		if ((Object)(object)m_collider == null)
		{
			return;
		}
		Collider[] array = Physics.OverlapBox(worldPos, new Vector3(area / 2f, 500f, area / 2f));
		foreach (Collider val in array)
		{
			if (!((Object)(object)val == (Object)(object)m_collider) && (bool)(Object)(object)val.get_attachedRigidbody())
			{
				Rigidbody attachedRigidbody = val.get_attachedRigidbody();
				ZNetView component = ((Component)(object)attachedRigidbody).GetComponent<ZNetView>();
				if (!component || component.IsOwner())
				{
					objects.Add(attachedRigidbody);
				}
			}
		}
	}

	private void PaintCleared(Vector3 worldPos, float radius, TerrainModifier.PaintType paintType, bool heightCheck, bool apply)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		float num = worldPos.y - base.transform.position.y;
		WorldToVertex(worldPos, out var x, out var y);
		float num2 = radius / m_scale;
		int num3 = Mathf.CeilToInt(num2);
		Vector2 a = new Vector2(x, y);
		for (int i = y - num3; i <= y + num3; i++)
		{
			for (int j = x - num3; j <= x + num3; j++)
			{
				float num4 = Vector2.Distance(a, new Vector2(j, i));
				if (j >= 0 && i >= 0 && j < m_clearedMask.width && i < m_clearedMask.height && (!heightCheck || !(GetHeight(j, i) > num)))
				{
					float f = 1f - Mathf.Clamp01(num4 / num2);
					f = Mathf.Pow(f, 0.1f);
					Color color = m_clearedMask.GetPixel(j, i);
					switch (paintType)
					{
					case TerrainModifier.PaintType.Dirt:
						color = Color.Lerp(color, Color.red, f);
						break;
					case TerrainModifier.PaintType.Cultivate:
						color = Color.Lerp(color, Color.green, f);
						break;
					case TerrainModifier.PaintType.Paved:
						color = Color.Lerp(color, Color.blue, f);
						break;
					case TerrainModifier.PaintType.Reset:
						color = Color.Lerp(color, Color.black, f);
						break;
					}
					m_clearedMask.SetPixel(j, i, color);
				}
			}
		}
		if (apply)
		{
			m_clearedMask.Apply();
		}
	}

	public bool IsCleared(Vector3 worldPos)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		WorldToVertex(worldPos, out var x, out var y);
		Color pixel = m_clearedMask.GetPixel(x, y);
		if (!(pixel.r > 0.5f) && !(pixel.g > 0.5f))
		{
			return pixel.b > 0.5f;
		}
		return true;
	}

	public bool IsCultivated(Vector3 worldPos)
	{
		WorldToVertex(worldPos, out var x, out var y);
		return m_clearedMask.GetPixel(x, y).g > 0.5f;
	}

	private void WorldToVertex(Vector3 worldPos, out int x, out int y)
	{
		Vector3 vector = worldPos - base.transform.position;
		x = Mathf.FloorToInt(vector.x / m_scale + 0.5f) + m_width / 2;
		y = Mathf.FloorToInt(vector.z / m_scale + 0.5f) + m_width / 2;
	}

	private void WorldToNormalizedHM(Vector3 worldPos, out float x, out float y)
	{
		float num = (float)m_width * m_scale;
		Vector3 vector = worldPos - base.transform.position;
		x = vector.x / num + 0.5f;
		y = vector.z / num + 0.5f;
	}

	private void LevelTerrain(Vector3 worldPos, float radius, bool square, float[] baseHeights, float[] levelOnly, bool playerModifiction)
	{
		WorldToVertex(worldPos, out var x, out var y);
		Vector3 vector = worldPos - base.transform.position;
		float num = radius / m_scale;
		int num2 = Mathf.CeilToInt(num);
		int num3 = m_width + 1;
		Vector2 a = new Vector2(x, y);
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				if ((square || !(Vector2.Distance(a, new Vector2(j, i)) > num)) && j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					float num4 = vector.y;
					if (playerModifiction)
					{
						float num5 = baseHeights[i * num3 + j];
						num4 = (levelOnly[i * num3 + j] = Mathf.Clamp(num4, num5 - 8f, num5 + 8f));
					}
					SetHeight(j, i, num4);
				}
			}
		}
	}

	private float GetHeight(int x, int y)
	{
		int num = m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return 0f;
		}
		return m_heights[y * num + x];
	}

	private float GetBaseHeight(int x, int y)
	{
		int num = m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return 0f;
		}
		return m_buildData.m_baseHeights[y * num + x];
	}

	private void SetHeight(int x, int y, float h)
	{
		int num = m_width + 1;
		if (x >= 0 && y >= 0 && x < num && y < num)
		{
			m_heights[y * num + x] = h;
		}
	}

	public bool IsPointInside(Vector3 point, float radius = 0f)
	{
		float num = (float)m_width * m_scale * 0.5f;
		Vector3 position = base.transform.position;
		if (point.x + radius >= position.x - num && point.x - radius <= position.x + num && point.z + radius >= position.z - num && point.z - radius <= position.z + num)
		{
			return true;
		}
		return false;
	}

	public static List<Heightmap> GetAllHeightmaps()
	{
		return m_heightmaps;
	}

	public static Heightmap FindHeightmap(Vector3 point)
	{
		foreach (Heightmap heightmap in m_heightmaps)
		{
			if (heightmap.IsPointInside(point))
			{
				return heightmap;
			}
		}
		return null;
	}

	public static void FindHeightmap(Vector3 point, float radius, List<Heightmap> heightmaps)
	{
		foreach (Heightmap heightmap in m_heightmaps)
		{
			if (heightmap.IsPointInside(point, radius))
			{
				heightmaps.Add(heightmap);
			}
		}
	}

	public static Biome FindBiome(Vector3 point)
	{
		Heightmap heightmap = FindHeightmap(point);
		if ((bool)heightmap)
		{
			return heightmap.GetBiome(point);
		}
		return Biome.None;
	}

	public static bool HaveQueuedRebuild(Vector3 point, float radius)
	{
		tempHmaps.Clear();
		FindHeightmap(point, radius, tempHmaps);
		foreach (Heightmap tempHmap in tempHmaps)
		{
			if (tempHmap.HaveQueuedRebuild())
			{
				return true;
			}
		}
		return false;
	}

	public static Biome FindBiomeClutter(Vector3 point)
	{
		if ((bool)ZoneSystem.instance && !ZoneSystem.instance.IsZoneLoaded(point))
		{
			return Biome.None;
		}
		Heightmap heightmap = FindHeightmap(point);
		if ((bool)heightmap)
		{
			return heightmap.GetBiome(point);
		}
		return Biome.None;
	}

	public void Clear()
	{
		m_heights.Clear();
		m_clearedMask = null;
		m_materialInstance = null;
		m_buildData = null;
		if ((bool)m_collisionMesh)
		{
			m_collisionMesh.Clear();
		}
		if ((bool)m_renderMesh)
		{
			m_renderMesh.Clear();
		}
		if ((bool)(Object)(object)m_collider)
		{
			m_collider.set_sharedMesh((Mesh)null);
		}
	}
}
