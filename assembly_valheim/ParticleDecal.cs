using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParticleDecal : MonoBehaviour
{
	public ParticleSystem m_decalSystem;

	[Range(0f, 100f)]
	public float m_chance = 100f;

	private ParticleSystem part;

	private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

	private void Awake()
	{
		part = GetComponent<ParticleSystem>();
		collisionEvents = new List<ParticleCollisionEvent>();
	}

	private void OnParticleCollision(GameObject other)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		if (!(m_chance < 100f) || !(Random.Range(0f, 100f) > m_chance))
		{
			int num = ParticlePhysicsExtensions.GetCollisionEvents(part, other, collisionEvents);
			for (int i = 0; i < num; i++)
			{
				ParticleCollisionEvent val = collisionEvents[i];
				Vector3 eulerAngles = Quaternion.LookRotation(((ParticleCollisionEvent)(ref val)).get_normal()).eulerAngles;
				eulerAngles.x = 0f - eulerAngles.x + 180f;
				eulerAngles.y = 0f - eulerAngles.y;
				eulerAngles.z = Random.Range(0, 360);
				EmitParams val2 = default(EmitParams);
				((EmitParams)(ref val2)).set_position(((ParticleCollisionEvent)(ref val)).get_intersection());
				((EmitParams)(ref val2)).set_rotation3D(eulerAngles);
				((EmitParams)(ref val2)).set_velocity(-((ParticleCollisionEvent)(ref val)).get_normal() * 0.001f);
				m_decalSystem.Emit(val2, 1);
			}
		}
	}
}
