using UnityEngine;

public class SE_Burning : StatusEffect
{
	[Header("SE_Burning")]
	public float m_damageInterval = 1f;

	private float m_timer;

	private float m_totalDamage;

	private HitData.DamageTypes m_damage;

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (m_character.GetSEMan().HaveStatusEffect("Wet"))
		{
			m_time += dt * 5f;
		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_point = m_character.GetCenterPoint();
			hitData.m_damage = m_damage.Clone();
			m_character.ApplyDamage(hitData, showDamageText: true, triggerEffects: false);
		}
	}

	public void AddFireDamage(float damage)
	{
		m_totalDamage = Mathf.Max(m_totalDamage, damage);
		int num = (int)(m_ttl / m_damageInterval);
		float fire = m_totalDamage / (float)num;
		m_damage.m_fire = fire;
		ResetTime();
	}

	public void AddSpiritDamage(float damage)
	{
		m_totalDamage = Mathf.Max(m_totalDamage, damage);
		int num = (int)(m_ttl / m_damageInterval);
		float spirit = m_totalDamage / (float)num;
		m_damage.m_spirit = spirit;
		ResetTime();
	}
}
