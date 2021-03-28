using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
	public EffectList m_effects = new EffectList();

	public float m_cooldownDelay = 2f;

	private float m_cooldownTimer;

	private void Update()
	{
		m_cooldownTimer += Time.deltaTime;
		if (m_cooldownTimer > m_cooldownDelay)
		{
			float waterLevel = WaterVolume.GetWaterLevel(base.transform.position);
			if (base.transform.position.y < waterLevel)
			{
				m_effects.Create(base.transform.position, base.transform.rotation, base.transform);
				m_cooldownTimer = 0f;
			}
		}
	}
}
