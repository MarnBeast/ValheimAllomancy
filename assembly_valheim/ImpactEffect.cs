using UnityEngine;

public class ImpactEffect : MonoBehaviour
{
	public EffectList m_hitEffect = new EffectList();

	public EffectList m_destroyEffect = new EffectList();

	public float m_hitDestroyChance;

	public float m_minVelocity;

	public float m_maxVelocity;

	public bool m_damageToSelf;

	public bool m_damagePlayers = true;

	public bool m_damageFish;

	public int m_toolTier;

	public HitData.DamageTypes m_damages;

	public LayerMask m_triggerMask;

	public float m_interval = 0.5f;

	private bool m_firstHit = true;

	private bool m_hitEffectEnabled = true;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		if (m_maxVelocity < m_minVelocity)
		{
			m_maxVelocity = m_minVelocity;
		}
	}

	public void OnCollisionEnter(Collision info)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (!m_nview.IsValid() || ((bool)m_nview && !m_nview.IsOwner()) || info.get_contacts().Length == 0 || !m_hitEffectEnabled || (m_triggerMask.value & (1 << ((Component)(object)info.get_collider()).gameObject.layer)) == 0)
		{
			return;
		}
		float magnitude = info.get_relativeVelocity().magnitude;
		if (magnitude < m_minVelocity)
		{
			return;
		}
		ContactPoint val = info.get_contacts()[0];
		Vector3 point = ((ContactPoint)(ref val)).get_point();
		Vector3 pointVelocity = m_body.GetPointVelocity(point);
		m_hitEffectEnabled = false;
		Invoke("ResetHitTimer", m_interval);
		if (m_damages.HaveDamage())
		{
			GameObject gameObject = Projectile.FindHitObject(((ContactPoint)(ref val)).get_otherCollider());
			float num = (num = Utils.LerpStep(m_minVelocity, m_maxVelocity, magnitude));
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null)
			{
				Character character = component as Character;
				if ((bool)character)
				{
					if (!m_damagePlayers && character.IsPlayer())
					{
						return;
					}
					float num2 = Vector3.Dot(-info.get_relativeVelocity().normalized, pointVelocity);
					if (num2 < m_minVelocity)
					{
						return;
					}
					ZLog.Log((object)("Rel vel " + num2));
					num = Utils.LerpStep(m_minVelocity, m_maxVelocity, num2);
					if (character.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.DoubleImpactDamage))
					{
						num *= 2f;
					}
				}
				if (!m_damageFish && (bool)gameObject.GetComponent<Fish>())
				{
					return;
				}
				HitData hitData = new HitData();
				hitData.m_point = point;
				hitData.m_dir = pointVelocity.normalized;
				hitData.m_hitCollider = info.get_collider();
				hitData.m_toolTier = m_toolTier;
				hitData.m_damage = m_damages.Clone();
				hitData.m_damage.Modify(num);
				component.Damage(hitData);
			}
			if (m_damageToSelf)
			{
				IDestructible component2 = GetComponent<IDestructible>();
				if (component2 != null)
				{
					HitData hitData2 = new HitData();
					hitData2.m_point = point;
					hitData2.m_dir = -pointVelocity.normalized;
					hitData2.m_toolTier = m_toolTier;
					hitData2.m_damage = m_damages.Clone();
					hitData2.m_damage.Modify(num);
					component2.Damage(hitData2);
				}
			}
		}
		Vector3 rhs = Vector3.Cross(-Vector3.Normalize(info.get_relativeVelocity()), ((ContactPoint)(ref val)).get_normal());
		Vector3 vector = Vector3.Cross(((ContactPoint)(ref val)).get_normal(), rhs);
		Quaternion rot = Quaternion.identity;
		if (vector != Vector3.zero && ((ContactPoint)(ref val)).get_normal() != Vector3.zero)
		{
			rot = Quaternion.LookRotation(vector, ((ContactPoint)(ref val)).get_normal());
		}
		m_hitEffect.Create(point, rot);
		if (m_firstHit && m_hitDestroyChance > 0f && Random.value <= m_hitDestroyChance)
		{
			m_destroyEffect.Create(point, rot);
			GameObject gameObject2 = base.gameObject;
			if ((bool)base.transform.parent)
			{
				Animator componentInParent = base.transform.GetComponentInParent<Animator>();
				if ((bool)(Object)(object)componentInParent)
				{
					gameObject2 = ((Component)(object)componentInParent).gameObject;
				}
			}
			Object.Destroy(gameObject2);
		}
		m_firstHit = false;
	}

	private Vector3 GetAVGPos(ContactPoint[] points)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)("Pooints " + points.Length));
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < points.Length; i++)
		{
			ContactPoint val = points[i];
			ZLog.Log((object)("P " + ((Component)(object)((ContactPoint)(ref val)).get_otherCollider()).gameObject.name));
			zero += ((ContactPoint)(ref val)).get_point();
		}
		return zero;
	}

	private void ResetHitTimer()
	{
		m_hitEffectEnabled = true;
	}
}
