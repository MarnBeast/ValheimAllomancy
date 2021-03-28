using UnityEngine;

public class SE_Harpooned : StatusEffect
{
	[Header("SE_Harpooned")]
	public float m_minForce = 2f;

	public float m_maxForce = 10f;

	public float m_minDistance = 6f;

	public float m_maxDistance = 30f;

	public float m_staminaDrain = 10f;

	public float m_staminaDrainInterval = 0.1f;

	public float m_maxMass = 50f;

	private bool m_broken;

	private Character m_attacker;

	private float m_drainStaminaTimer;

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void SetAttacker(Character attacker)
	{
		ZLog.Log((object)("Setting attacker " + attacker.m_name));
		m_attacker = attacker;
		m_time = 0f;
		if (Vector3.Distance(m_attacker.transform.position, m_character.transform.position) > m_maxDistance)
		{
			m_attacker.Message(MessageHud.MessageType.Center, "Target too far");
			m_broken = true;
			return;
		}
		m_attacker.Message(MessageHud.MessageType.Center, m_character.m_name + " harpooned");
		GameObject[] startEffectInstances = m_startEffectInstances;
		foreach (GameObject gameObject in startEffectInstances)
		{
			if ((bool)gameObject)
			{
				LineConnect component = gameObject.GetComponent<LineConnect>();
				if ((bool)component)
				{
					component.SetPeer(m_attacker.GetComponent<ZNetView>());
				}
			}
		}
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!m_attacker)
		{
			return;
		}
		Rigidbody component = m_character.GetComponent<Rigidbody>();
		if (!(Object)(object)component)
		{
			return;
		}
		Vector3 vector = m_attacker.transform.position - m_character.transform.position;
		Vector3 normalized = vector.normalized;
		float radius = m_character.GetRadius();
		float magnitude = vector.magnitude;
		float num = Mathf.Clamp01(Vector3.Dot(normalized, component.get_velocity()));
		float t = Utils.LerpStep(m_minDistance, m_maxDistance, magnitude);
		float num2 = Mathf.Lerp(m_minForce, m_maxForce, t);
		float num3 = Mathf.Clamp01(m_maxMass / component.get_mass());
		float num4 = num2 * num3;
		if (magnitude - radius > m_minDistance && num < num4)
		{
			normalized.y = 0f;
			normalized.Normalize();
			if (m_character.GetStandingOnShip() == null && !m_character.IsAttached())
			{
				component.AddForce(normalized * num4, (ForceMode)2);
			}
			m_drainStaminaTimer += dt;
			if (m_drainStaminaTimer > m_staminaDrainInterval)
			{
				m_drainStaminaTimer = 0f;
				float num5 = 1f - Mathf.Clamp01(num / num2);
				m_attacker.UseStamina(m_staminaDrain * num5);
			}
		}
		if (magnitude > m_maxDistance)
		{
			m_broken = true;
			m_attacker.Message(MessageHud.MessageType.Center, "Line broke");
		}
		if (!m_attacker.HaveStamina())
		{
			m_broken = true;
			m_attacker.Message(MessageHud.MessageType.Center, m_character.m_name + " escaped");
		}
	}

	public override bool IsDone()
	{
		if (base.IsDone())
		{
			return true;
		}
		if (m_broken)
		{
			return true;
		}
		if (!m_attacker)
		{
			return true;
		}
		if (m_time > 2f && (m_attacker.IsBlocking() || m_attacker.InAttack()))
		{
			m_attacker.Message(MessageHud.MessageType.Center, m_character.m_name + " released");
			return true;
		}
		return false;
	}
}
