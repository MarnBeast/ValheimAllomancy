using UnityEngine;

public class Floating : MonoBehaviour, IWaterInteractable
{
	public float m_waterLevelOffset;

	public float m_forceDistance = 1f;

	public float m_force = 0.5f;

	public float m_balanceForceFraction = 0.02f;

	public float m_damping = 0.05f;

	private static float m_minImpactEffectVelocity = 0.5f;

	public EffectList m_impactEffects = new EffectList();

	public GameObject m_surfaceEffects;

	private float m_inWater = -10000f;

	private bool m_beenInWater;

	private bool m_wasInWater = true;

	private Rigidbody m_body;

	private Collider m_collider;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_collider = GetComponentInChildren<Collider>();
		SetSurfaceEffect(enabled: false);
		InvokeRepeating("TerrainCheck", Random.Range(10f, 30f), 30f);
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public bool IsOwner()
	{
		if (m_nview.IsValid())
		{
			return m_nview.IsOwner();
		}
		return false;
	}

	private void TerrainCheck()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			Rigidbody component = GetComponent<Rigidbody>();
			if ((bool)(Object)(object)component)
			{
				component.set_velocity(Vector3.zero);
			}
			ZLog.Log((object)("Moved up item " + base.gameObject.name));
		}
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		if (!IsInWater())
		{
			SetSurfaceEffect(enabled: false);
			return;
		}
		UpdateImpactEffect();
		float floatDepth = GetFloatDepth();
		if (floatDepth > 0f)
		{
			SetSurfaceEffect(enabled: false);
			return;
		}
		SetSurfaceEffect(enabled: true);
		Vector3 vector = m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		Vector3 worldCenterOfMass = m_body.get_worldCenterOfMass();
		float d = Mathf.Clamp01(Mathf.Abs(floatDepth) / m_forceDistance);
		Vector3 vector2 = Vector3.up * m_force * d * (Time.fixedDeltaTime * 50f);
		m_body.WakeUp();
		m_body.AddForceAtPosition(vector2 * m_balanceForceFraction, vector, (ForceMode)2);
		m_body.AddForceAtPosition(vector2, worldCenterOfMass, (ForceMode)2);
		m_body.set_velocity(m_body.get_velocity() - m_body.get_velocity() * m_damping * d);
		m_body.set_angularVelocity(m_body.get_angularVelocity() - m_body.get_angularVelocity() * m_damping * d);
	}

	public bool IsInWater()
	{
		return m_inWater > -10000f;
	}

	private void SetSurfaceEffect(bool enabled)
	{
		if (m_surfaceEffects != null)
		{
			m_surfaceEffects.SetActive(enabled);
		}
	}

	private void UpdateImpactEffect()
	{
		if (m_body.IsSleeping() || !m_impactEffects.HasEffects())
		{
			return;
		}
		Vector3 vector = m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		if (vector.y < m_inWater)
		{
			if (!m_wasInWater)
			{
				m_wasInWater = true;
				Vector3 pos = vector;
				pos.y = m_inWater;
				if (m_body.GetPointVelocity(vector).magnitude > m_minImpactEffectVelocity)
				{
					m_impactEffects.Create(pos, Quaternion.identity);
				}
			}
		}
		else
		{
			m_wasInWater = false;
		}
	}

	private float GetFloatDepth()
	{
		return m_body.get_worldCenterOfMass().y - m_inWater - m_waterLevelOffset;
	}

	public void SetInWater(float waterLevel)
	{
		m_inWater = waterLevel;
		if (!m_beenInWater && waterLevel > -10000f && GetFloatDepth() < 0f)
		{
			m_beenInWater = true;
		}
	}

	public bool BeenInWater()
	{
		return m_beenInWater;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.down * m_waterLevelOffset, new Vector3(1f, 0.05f, 1f));
	}
}
