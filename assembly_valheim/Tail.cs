using System;
using System.Collections.Generic;
using UnityEngine;

public class Tail : MonoBehaviour
{
	private class TailSegment
	{
		public Transform transform;

		public Vector3 pos;

		public Quaternion rot;

		public float distance;
	}

	public List<Transform> m_tailJoints = new List<Transform>();

	public float m_yMovementDistance = 0.5f;

	public float m_yMovementFreq = 0.5f;

	public float m_yMovementOffset = 0.2f;

	public float m_maxAngle = 80f;

	public float m_gravity = 2f;

	public float m_gravityInWater = 0.1f;

	public bool m_waterSurfaceCheck;

	public bool m_groundCheck;

	public float m_smoothness = 0.1f;

	public float m_tailRadius;

	public Character m_character;

	public Rigidbody m_characterBody;

	public Rigidbody m_tailBody;

	private List<TailSegment> m_positions = new List<TailSegment>();

	private void Awake()
	{
		foreach (Transform tailJoint in m_tailJoints)
		{
			float distance = Vector3.Distance(tailJoint.parent.position, tailJoint.position);
			Vector3 position = tailJoint.position;
			TailSegment tailSegment = new TailSegment();
			tailSegment.transform = tailJoint;
			tailSegment.pos = position;
			tailSegment.rot = tailJoint.rotation;
			tailSegment.distance = distance;
			m_positions.Add(tailSegment);
		}
	}

	private void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		if ((bool)m_character)
		{
			m_character.IsSwiming();
		}
		for (int i = 0; i < m_positions.Count; i++)
		{
			TailSegment tailSegment = m_positions[i];
			if (m_waterSurfaceCheck)
			{
				float waterLevel = WaterVolume.GetWaterLevel(tailSegment.pos);
				if (tailSegment.pos.y + m_tailRadius > waterLevel)
				{
					tailSegment.pos.y -= m_gravity * deltaTime;
				}
				else
				{
					tailSegment.pos.y -= m_gravityInWater * deltaTime;
				}
			}
			else
			{
				tailSegment.pos.y -= m_gravity * deltaTime;
			}
			Vector3 a = tailSegment.transform.parent.position + tailSegment.transform.parent.up * tailSegment.distance * 0.5f;
			Vector3 a2 = Vector3.RotateTowards(target: Vector3.Normalize(a - tailSegment.pos), current: -tailSegment.transform.parent.up, maxRadiansDelta: (float)Math.PI / 180f * m_maxAngle, maxMagnitudeDelta: 1f);
			Vector3 vector = a - a2 * tailSegment.distance * 0.5f;
			if (m_groundCheck)
			{
				float groundHeight = ZoneSystem.instance.GetGroundHeight(vector);
				if (vector.y - m_tailRadius < groundHeight)
				{
					vector.y = groundHeight + m_tailRadius;
				}
			}
			vector = Vector3.Lerp(tailSegment.pos, vector, m_smoothness);
			Vector3 normalized = (a - vector).normalized;
			Vector3 rhs = Vector3.Cross(Vector3.up, -normalized);
			Quaternion b = Quaternion.LookRotation(Vector3.Cross(-normalized, rhs), -normalized);
			b = Quaternion.Slerp(tailSegment.rot, b, m_smoothness);
			tailSegment.transform.position = vector;
			tailSegment.transform.rotation = b;
			tailSegment.pos = vector;
			tailSegment.rot = b;
		}
		if ((bool)(UnityEngine.Object)(object)m_tailBody)
		{
			m_tailBody.set_velocity(Vector3.zero);
			m_tailBody.set_angularVelocity(Vector3.zero);
		}
	}
}
