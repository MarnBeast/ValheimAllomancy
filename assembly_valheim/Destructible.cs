using System;
using UnityEngine;

public class Destructible : MonoBehaviour, IDestructible
{
	public Action m_onDestroyed;

	public Action m_onDamaged;

	[Header("Destruction")]
	public DestructibleType m_destructibleType = DestructibleType.Default;

	public float m_health = 1f;

	public HitData.DamageModifiers m_damages;

	public float m_minDamageTreshold;

	public int m_minToolTier;

	public float m_hitNoise;

	public float m_destroyNoise;

	public float m_ttl;

	public GameObject m_spawnWhenDestroyed;

	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public bool m_autoCreateFragments;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private bool m_firstFrame = true;

	private bool m_destroyed;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		if ((bool)m_nview && m_nview.GetZDO() != null)
		{
			m_nview.Register<HitData>("Damage", RPC_Damage);
			if (m_autoCreateFragments)
			{
				m_nview.Register("CreateFragments", RPC_CreateFragments);
			}
			if (m_ttl > 0f)
			{
				InvokeRepeating("DestroyNow", m_ttl, 1f);
			}
		}
	}

	private void Start()
	{
		m_firstFrame = false;
	}

	public GameObject GetParentObject()
	{
		return null;
	}

	public DestructibleType GetDestructibleType()
	{
		return m_destructibleType;
	}

	public void Damage(HitData hit)
	{
		if (!m_firstFrame && m_nview.IsValid())
		{
			m_nview.InvokeRPC("Damage", hit);
		}
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner() || m_destroyed)
		{
			return;
		}
		float @float = m_nview.GetZDO().GetFloat("health", m_health);
		hit.ApplyResistance(m_damages, out var significantModifier);
		float totalDamage = hit.GetTotalDamage();
		if ((bool)(UnityEngine.Object)(object)m_body)
		{
			m_body.AddForceAtPosition(hit.m_dir * hit.m_pushForce, hit.m_point, (ForceMode)1);
		}
		if (hit.m_toolTier < m_minToolTier)
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f);
			return;
		}
		DamageText.instance.ShowText(significantModifier, hit.m_point, totalDamage);
		if (totalDamage <= 0f)
		{
			return;
		}
		@float -= totalDamage;
		m_nview.GetZDO().Set("health", @float);
		m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform);
		if (m_onDamaged != null)
		{
			m_onDamaged();
		}
		if (m_hitNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if ((bool)closestPlayer)
			{
				closestPlayer.AddNoise(m_hitNoise);
			}
		}
		if (@float <= 0f)
		{
			Destroy();
		}
	}

	private void DestroyNow()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			Destroy();
		}
	}

	public void Destroy()
	{
		CreateDestructionEffects();
		if (m_destroyNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if ((bool)closestPlayer)
			{
				closestPlayer.AddNoise(m_destroyNoise);
			}
		}
		if ((bool)m_spawnWhenDestroyed)
		{
			ZNetView component = UnityEngine.Object.Instantiate(m_spawnWhenDestroyed, base.transform.position, base.transform.rotation).GetComponent<ZNetView>();
			component.SetLocalScale(base.transform.localScale);
			component.GetZDO().SetPGWVersion(m_nview.GetZDO().GetPGWVersion());
		}
		if (m_onDestroyed != null)
		{
			m_onDestroyed();
		}
		ZNetScene.instance.Destroy(base.gameObject);
		m_destroyed = true;
	}

	private void CreateDestructionEffects()
	{
		m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform);
		if (m_autoCreateFragments)
		{
			m_nview.InvokeRPC(ZNetView.Everybody, "CreateFragments");
		}
	}

	private void RPC_CreateFragments(long peer)
	{
		CreateFragments(base.gameObject);
	}

	public static void CreateFragments(GameObject rootObject, bool visibleOnly = true)
	{
		MeshRenderer[] componentsInChildren = rootObject.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		int layer = LayerMask.NameToLayer("effect");
		MeshRenderer[] array = componentsInChildren;
		foreach (MeshRenderer meshRenderer in array)
		{
			if (!meshRenderer.gameObject.activeInHierarchy || (visibleOnly && !meshRenderer.isVisible))
			{
				continue;
			}
			MeshFilter component = meshRenderer.gameObject.GetComponent<MeshFilter>();
			if (!(component == null))
			{
				if (component.sharedMesh == null)
				{
					ZLog.Log((object)("Meshfilter missing mesh " + component.gameObject.name));
					continue;
				}
				GameObject gameObject = new GameObject();
				gameObject.layer = layer;
				gameObject.transform.position = component.gameObject.transform.position;
				gameObject.transform.rotation = component.gameObject.transform.rotation;
				gameObject.transform.localScale = component.gameObject.transform.lossyScale * 0.9f;
				gameObject.AddComponent<MeshFilter>().sharedMesh = component.sharedMesh;
				MeshRenderer meshRenderer2 = gameObject.AddComponent<MeshRenderer>();
				meshRenderer2.sharedMaterials = meshRenderer.sharedMaterials;
				meshRenderer2.material.SetFloat("_RippleDistance", 0f);
				meshRenderer2.material.SetFloat("_ValueNoise", 0f);
				Rigidbody val = gameObject.AddComponent<Rigidbody>();
				gameObject.AddComponent<BoxCollider>();
				val.AddForce(UnityEngine.Random.onUnitSphere * 2f, (ForceMode)2);
				gameObject.AddComponent<TimedDestruction>().Trigger(UnityEngine.Random.Range(2, 4));
			}
		}
	}
}
