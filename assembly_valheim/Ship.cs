using System;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
	public enum Speed
	{
		Stop,
		Back,
		Slow,
		Half,
		Full
	}

	private bool m_forwardPressed;

	private bool m_backwardPressed;

	private float m_sendRudderTime;

	private Vector3 windChangeVelocity = Vector3.zero;

	private bool sailWasInPosition;

	[Header("Objects")]
	public GameObject m_sailObject;

	public GameObject m_mastObject;

	public GameObject m_rudderObject;

	public ShipControlls m_shipControlls;

	public Transform m_controlGuiPos;

	[Header("Misc")]
	public BoxCollider m_floatCollider;

	public float m_waterLevelOffset;

	public float m_forceDistance = 1f;

	public float m_force = 0.5f;

	public float m_damping = 0.05f;

	public float m_dampingSideway = 0.05f;

	public float m_dampingForward = 0.01f;

	public float m_angularDamping = 0.01f;

	public float m_disableLevel = -0.5f;

	public float m_sailForceOffset;

	public float m_sailForceFactor = 0.1f;

	public float m_rudderSpeed = 0.5f;

	public float m_stearForceOffset = -10f;

	public float m_stearForce = 0.5f;

	public float m_stearVelForceFactor = 0.1f;

	public float m_backwardForce = 50f;

	public float m_rudderRotationMax = 30f;

	public float m_rudderRotationSpeed = 30f;

	public float m_minWaterImpactForce = 2.5f;

	public float m_minWaterImpactInterval = 2f;

	public float m_waterImpactDamage = 10f;

	public float m_upsideDownDmgInterval = 1f;

	public float m_upsideDownDmg = 20f;

	public EffectList m_waterImpactEffect = new EffectList();

	private Speed m_speed;

	private float m_rudder;

	private float m_rudderValue;

	private Vector3 m_sailForce = Vector3.zero;

	private List<Player> m_players = new List<Player>();

	private static List<Ship> m_currentShips = new List<Ship>();

	private Rigidbody m_body;

	private ZNetView m_nview;

	private Cloth m_sailCloth;

	private float m_lastDepth = -9999f;

	private float m_lastWaterImpactTime;

	private float m_upsideDownDmgTimer;

	private float m_rudderPaddleTimer;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		WearNTear component = GetComponent<WearNTear>();
		if ((bool)component)
		{
			component.m_onDestroyed = (Action)Delegate.Combine(component.m_onDestroyed, new Action(OnDestroyed));
		}
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
		}
		m_body.set_maxDepenetrationVelocity(2f);
		Heightmap.ForceGenerateAll();
		m_sailCloth = m_sailObject.GetComponentInChildren<Cloth>();
	}

	public bool CanBeRemoved()
	{
		return m_players.Count == 0;
	}

	private void Start()
	{
		m_nview.Register("Stop", RPC_Stop);
		m_nview.Register("Forward", RPC_Forward);
		m_nview.Register("Backward", RPC_Backward);
		m_nview.Register<float>("Rudder", RPC_Rudder);
		InvokeRepeating("UpdateOwner", 2f, 2f);
	}

	private void PrintStats()
	{
		if (m_players.Count != 0)
		{
			ZLog.Log((object)("Vel:" + m_body.get_velocity().magnitude.ToString("0.0")));
		}
	}

	public void ApplyMovementControlls(Vector3 dir)
	{
		bool flag = (double)dir.z > 0.5;
		bool flag2 = (double)dir.z < -0.5;
		if (flag && !m_forwardPressed)
		{
			Forward();
		}
		if (flag2 && !m_backwardPressed)
		{
			Backward();
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num = Mathf.Lerp(0.5f, 1f, Mathf.Abs(m_rudderValue));
		m_rudder = dir.x * num;
		m_rudderValue += m_rudder * m_rudderSpeed * fixedDeltaTime;
		m_rudderValue = Mathf.Clamp(m_rudderValue, -1f, 1f);
		if (Time.time - m_sendRudderTime > 0.2f)
		{
			m_sendRudderTime = Time.time;
			m_nview.InvokeRPC("Rudder", m_rudderValue);
		}
		m_forwardPressed = flag;
		m_backwardPressed = flag2;
	}

	public void Forward()
	{
		m_nview.InvokeRPC("Forward");
	}

	public void Backward()
	{
		m_nview.InvokeRPC("Backward");
	}

	public void Rudder(float rudder)
	{
		m_nview.Invoke("Rudder", rudder);
	}

	private void RPC_Rudder(long sender, float value)
	{
		m_rudderValue = value;
	}

	public void Stop()
	{
		m_nview.InvokeRPC("Stop");
	}

	private void RPC_Stop(long sender)
	{
		m_speed = Speed.Stop;
	}

	private void RPC_Forward(long sender)
	{
		switch (m_speed)
		{
		case Speed.Stop:
			m_speed = Speed.Slow;
			break;
		case Speed.Slow:
			m_speed = Speed.Half;
			break;
		case Speed.Half:
			m_speed = Speed.Full;
			break;
		case Speed.Back:
			m_speed = Speed.Stop;
			break;
		case Speed.Full:
			break;
		}
	}

	private void RPC_Backward(long sender)
	{
		switch (m_speed)
		{
		case Speed.Stop:
			m_speed = Speed.Back;
			break;
		case Speed.Slow:
			m_speed = Speed.Stop;
			break;
		case Speed.Half:
			m_speed = Speed.Slow;
			break;
		case Speed.Full:
			m_speed = Speed.Half;
			break;
		case Speed.Back:
			break;
		}
	}

	private void FixedUpdate()
	{
		bool flag = HaveControllingPlayer();
		UpdateControlls(Time.fixedDeltaTime);
		UpdateSail(Time.fixedDeltaTime);
		UpdateRudder(Time.fixedDeltaTime, flag);
		if ((bool)m_nview && !m_nview.IsOwner())
		{
			return;
		}
		UpdateUpsideDmg(Time.fixedDeltaTime);
		if (m_players.Count == 0)
		{
			m_speed = Speed.Stop;
			m_rudderValue = 0f;
		}
		if (!flag && (m_speed == Speed.Slow || m_speed == Speed.Back))
		{
			m_speed = Speed.Stop;
		}
		float waveFactor = 1f;
		Vector3 worldCenterOfMass = m_body.get_worldCenterOfMass();
		Vector3 vector = ((Component)(object)m_floatCollider).transform.position + ((Component)(object)m_floatCollider).transform.forward * m_floatCollider.get_size().z / 2f;
		Vector3 vector2 = ((Component)(object)m_floatCollider).transform.position - ((Component)(object)m_floatCollider).transform.forward * m_floatCollider.get_size().z / 2f;
		Vector3 vector3 = ((Component)(object)m_floatCollider).transform.position - ((Component)(object)m_floatCollider).transform.right * m_floatCollider.get_size().x / 2f;
		Vector3 vector4 = ((Component)(object)m_floatCollider).transform.position + ((Component)(object)m_floatCollider).transform.right * m_floatCollider.get_size().x / 2f;
		float waterLevel = WaterVolume.GetWaterLevel(worldCenterOfMass, waveFactor);
		float waterLevel2 = WaterVolume.GetWaterLevel(vector3, waveFactor);
		float waterLevel3 = WaterVolume.GetWaterLevel(vector4, waveFactor);
		float waterLevel4 = WaterVolume.GetWaterLevel(vector, waveFactor);
		float waterLevel5 = WaterVolume.GetWaterLevel(vector2, waveFactor);
		float num = (waterLevel + waterLevel2 + waterLevel3 + waterLevel4 + waterLevel5) / 5f;
		float num2 = worldCenterOfMass.y - num - m_waterLevelOffset;
		if (!(num2 > m_disableLevel))
		{
			m_body.WakeUp();
			UpdateWaterForce(num2, Time.fixedDeltaTime);
			Vector3 vector5 = new Vector3(vector3.x, waterLevel2, vector3.z);
			Vector3 vector6 = new Vector3(vector4.x, waterLevel3, vector4.z);
			Vector3 vector7 = new Vector3(vector.x, waterLevel4, vector.z);
			Vector3 vector8 = new Vector3(vector2.x, waterLevel5, vector2.z);
			float fixedDeltaTime = Time.fixedDeltaTime;
			float d = fixedDeltaTime * 50f;
			float num3 = Mathf.Clamp01(Mathf.Abs(num2) / m_forceDistance);
			Vector3 a = Vector3.up * m_force * num3;
			m_body.AddForceAtPosition(a * d, worldCenterOfMass, (ForceMode)2);
			float num4 = Vector3.Dot(m_body.get_velocity(), base.transform.forward);
			float num5 = Vector3.Dot(m_body.get_velocity(), base.transform.right);
			Vector3 velocity = m_body.get_velocity();
			velocity.y -= velocity.y * velocity.y * Mathf.Sign(velocity.y) * m_damping * num3;
			velocity -= base.transform.forward * (num4 * num4 * Mathf.Sign(num4)) * m_dampingForward * num3;
			velocity -= base.transform.right * (num5 * num5 * Mathf.Sign(num5)) * m_dampingSideway * num3;
			if (velocity.magnitude > m_body.get_velocity().magnitude)
			{
				velocity = velocity.normalized * m_body.get_velocity().magnitude;
			}
			if (m_players.Count == 0)
			{
				velocity.x *= 0.1f;
				velocity.z *= 0.1f;
			}
			m_body.set_velocity(velocity);
			m_body.set_angularVelocity(m_body.get_angularVelocity() - m_body.get_angularVelocity() * m_angularDamping * num3);
			float num6 = 0.15f;
			float num7 = 0.5f;
			float f = Mathf.Clamp((vector7.y - vector.y) * num6, 0f - num7, num7);
			float f2 = Mathf.Clamp((vector8.y - vector2.y) * num6, 0f - num7, num7);
			float f3 = Mathf.Clamp((vector5.y - vector3.y) * num6, 0f - num7, num7);
			float f4 = Mathf.Clamp((vector6.y - vector4.y) * num6, 0f - num7, num7);
			f = Mathf.Sign(f) * Mathf.Abs(Mathf.Pow(f, 2f));
			f2 = Mathf.Sign(f2) * Mathf.Abs(Mathf.Pow(f2, 2f));
			f3 = Mathf.Sign(f3) * Mathf.Abs(Mathf.Pow(f3, 2f));
			f4 = Mathf.Sign(f4) * Mathf.Abs(Mathf.Pow(f4, 2f));
			m_body.AddForceAtPosition(Vector3.up * f * d, vector, (ForceMode)2);
			m_body.AddForceAtPosition(Vector3.up * f2 * d, vector2, (ForceMode)2);
			m_body.AddForceAtPosition(Vector3.up * f3 * d, vector3, (ForceMode)2);
			m_body.AddForceAtPosition(Vector3.up * f4 * d, vector4, (ForceMode)2);
			float sailSize = 0f;
			if (m_speed == Speed.Full)
			{
				sailSize = 1f;
			}
			else if (m_speed == Speed.Half)
			{
				sailSize = 0.5f;
			}
			Vector3 sailForce = GetSailForce(sailSize, fixedDeltaTime);
			Vector3 vector9 = worldCenterOfMass + base.transform.up * m_sailForceOffset;
			m_body.AddForceAtPosition(sailForce, vector9, (ForceMode)2);
			Vector3 vector10 = base.transform.position + base.transform.forward * m_stearForceOffset;
			float d2 = num4 * m_stearVelForceFactor;
			m_body.AddForceAtPosition(base.transform.right * d2 * (0f - m_rudderValue) * fixedDeltaTime, vector10, (ForceMode)2);
			Vector3 zero = Vector3.zero;
			switch (m_speed)
			{
			case Speed.Slow:
				zero += base.transform.forward * m_backwardForce * (1f - Mathf.Abs(m_rudderValue));
				break;
			case Speed.Back:
				zero += -base.transform.forward * m_backwardForce * (1f - Mathf.Abs(m_rudderValue));
				break;
			}
			if (m_speed == Speed.Back || m_speed == Speed.Slow)
			{
				float d3 = ((m_speed != Speed.Back) ? 1 : (-1));
				zero += base.transform.right * m_stearForce * (0f - m_rudderValue) * d3;
			}
			m_body.AddForceAtPosition(zero * fixedDeltaTime, vector10, (ForceMode)2);
			ApplyEdgeForce(Time.fixedDeltaTime);
		}
	}

	private void UpdateUpsideDmg(float dt)
	{
		if (!(base.transform.up.y < 0f))
		{
			return;
		}
		m_upsideDownDmgTimer += dt;
		if (m_upsideDownDmgTimer > m_upsideDownDmgInterval)
		{
			m_upsideDownDmgTimer = 0f;
			IDestructible component = GetComponent<IDestructible>();
			if (component != null)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_blunt = m_upsideDownDmg;
				hitData.m_point = base.transform.position;
				hitData.m_dir = Vector3.up;
				component.Damage(hitData);
			}
		}
	}

	private Vector3 GetSailForce(float sailSize, float dt)
	{
		Vector3 windDir = EnvMan.instance.GetWindDir();
		float windIntensity = EnvMan.instance.GetWindIntensity();
		float num = Mathf.Lerp(0.25f, 1f, windIntensity);
		float windAngleFactor = GetWindAngleFactor();
		windAngleFactor *= num;
		Vector3 target = Vector3.Normalize(windDir + base.transform.forward) * windAngleFactor * m_sailForceFactor * sailSize;
		m_sailForce = Vector3.SmoothDamp(m_sailForce, target, ref windChangeVelocity, 1f, 99f);
		return m_sailForce;
	}

	public float GetWindAngleFactor()
	{
		float num = Vector3.Dot(EnvMan.instance.GetWindDir(), -base.transform.forward);
		float num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
		float num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
		return num2 * num3;
	}

	private void UpdateWaterForce(float depth, float dt)
	{
		if (m_lastDepth == -9999f)
		{
			m_lastDepth = depth;
			return;
		}
		float num = depth - m_lastDepth;
		m_lastDepth = depth;
		float num2 = num / dt;
		if (num2 > 0f || !(Mathf.Abs(num2) > m_minWaterImpactForce) || !(Time.time - m_lastWaterImpactTime > m_minWaterImpactInterval))
		{
			return;
		}
		m_lastWaterImpactTime = Time.time;
		m_waterImpactEffect.Create(base.transform.position, base.transform.rotation);
		if (m_players.Count > 0)
		{
			IDestructible component = GetComponent<IDestructible>();
			if (component != null)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_blunt = m_waterImpactDamage;
				hitData.m_point = base.transform.position;
				hitData.m_dir = Vector3.up;
				component.Damage(hitData);
			}
		}
	}

	private void ApplyEdgeForce(float dt)
	{
		float magnitude = base.transform.position.magnitude;
		float num = 10420f;
		if (magnitude > num)
		{
			Vector3 a = Vector3.Normalize(base.transform.position);
			float d = Utils.LerpStep(num, 10500f, magnitude) * 8f;
			Vector3 a2 = a * d;
			m_body.AddForce(a2 * dt, (ForceMode)2);
		}
	}

	private void FixTilt()
	{
		float num = Mathf.Asin(base.transform.right.y);
		float num2 = Mathf.Asin(base.transform.forward.y);
		if (Mathf.Abs(num) > (float)Math.PI / 6f)
		{
			if (num > 0f)
			{
				base.transform.RotateAround(base.transform.position, base.transform.forward, (0f - Time.fixedDeltaTime) * 20f);
			}
			else
			{
				base.transform.RotateAround(base.transform.position, base.transform.forward, Time.fixedDeltaTime * 20f);
			}
		}
		if (Mathf.Abs(num2) > (float)Math.PI / 6f)
		{
			if (num2 > 0f)
			{
				base.transform.RotateAround(base.transform.position, base.transform.right, (0f - Time.fixedDeltaTime) * 20f);
			}
			else
			{
				base.transform.RotateAround(base.transform.position, base.transform.right, Time.fixedDeltaTime * 20f);
			}
		}
	}

	private void UpdateControlls(float dt)
	{
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set("forward", (int)m_speed);
			m_nview.GetZDO().Set("rudder", m_rudderValue);
			return;
		}
		m_speed = (Speed)m_nview.GetZDO().GetInt("forward");
		if (Time.time - m_sendRudderTime > 1f)
		{
			m_rudderValue = m_nview.GetZDO().GetFloat("rudder");
		}
	}

	public bool IsSailUp()
	{
		if (m_speed != Speed.Half)
		{
			return m_speed == Speed.Full;
		}
		return true;
	}

	private void UpdateSail(float dt)
	{
		UpdateSailSize(dt);
		Vector3 windDir = EnvMan.instance.GetWindDir();
		windDir = Vector3.Cross(Vector3.Cross(windDir, base.transform.up), base.transform.up);
		if (m_speed == Speed.Full || m_speed == Speed.Half)
		{
			float t = 0.5f + Vector3.Dot(base.transform.forward, windDir) * 0.5f;
			Quaternion to = Quaternion.LookRotation(-Vector3.Lerp(windDir, Vector3.Normalize(windDir - base.transform.forward), t), base.transform.up);
			m_mastObject.transform.rotation = Quaternion.RotateTowards(m_mastObject.transform.rotation, to, 30f * dt);
		}
		else if (m_speed == Speed.Back)
		{
			Quaternion from = Quaternion.LookRotation(-base.transform.forward, base.transform.up);
			Quaternion to2 = Quaternion.LookRotation(-windDir, base.transform.up);
			to2 = Quaternion.RotateTowards(from, to2, 80f);
			m_mastObject.transform.rotation = Quaternion.RotateTowards(m_mastObject.transform.rotation, to2, 30f * dt);
		}
	}

	private void UpdateRudder(float dt, bool haveControllingPlayer)
	{
		if (!m_rudderObject)
		{
			return;
		}
		Quaternion b = Quaternion.Euler(0f, m_rudderRotationMax * (0f - m_rudderValue), 0f);
		if (haveControllingPlayer)
		{
			if (m_speed == Speed.Slow)
			{
				m_rudderPaddleTimer += dt;
				b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * 6f) * 20f, 0f);
			}
			else if (m_speed == Speed.Back)
			{
				m_rudderPaddleTimer += dt;
				b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * -3f) * 40f, 0f);
			}
		}
		m_rudderObject.transform.localRotation = Quaternion.Slerp(m_rudderObject.transform.localRotation, b, 0.5f);
	}

	private void UpdateSailSize(float dt)
	{
		float num = 0f;
		switch (m_speed)
		{
		case Speed.Back:
			num = 0.1f;
			break;
		case Speed.Half:
			num = 0.5f;
			break;
		case Speed.Full:
			num = 1f;
			break;
		case Speed.Slow:
			num = 0.1f;
			break;
		case Speed.Stop:
			num = 0.1f;
			break;
		}
		Vector3 localScale = m_sailObject.transform.localScale;
		bool flag = Mathf.Abs(localScale.y - num) < 0.01f;
		if (!flag)
		{
			localScale.y = Mathf.MoveTowards(localScale.y, num, dt);
			m_sailObject.transform.localScale = localScale;
		}
		if ((bool)(UnityEngine.Object)(object)m_sailCloth)
		{
			if (m_speed == Speed.Stop || m_speed == Speed.Slow || m_speed == Speed.Back)
			{
				if (flag && m_sailCloth.get_enabled())
				{
					m_sailCloth.set_enabled(false);
				}
			}
			else if (flag)
			{
				if (!sailWasInPosition)
				{
					m_sailCloth.set_enabled(false);
					m_sailCloth.set_enabled(true);
				}
			}
			else
			{
				m_sailCloth.set_enabled(true);
			}
		}
		sailWasInPosition = flag;
	}

	private void UpdateOwner()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && !(Player.m_localPlayer == null) && m_players.Count > 0 && !IsPlayerInBoat(Player.m_localPlayer))
		{
			long owner = m_players[0].GetOwner();
			m_nview.GetZDO().SetOwner(owner);
			ZLog.Log((object)("Changing ship owner to " + owner));
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		Player component = ((Component)(object)collider).GetComponent<Player>();
		if ((bool)component)
		{
			m_players.Add(component);
			ZLog.Log((object)("Player onboard, total onboard " + m_players.Count));
			if (component == Player.m_localPlayer)
			{
				m_currentShips.Add(this);
			}
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		Player component = ((Component)(object)collider).GetComponent<Player>();
		if ((bool)component)
		{
			m_players.Remove(component);
			ZLog.Log((object)("Player over board, players left " + m_players.Count));
			if (component == Player.m_localPlayer)
			{
				m_currentShips.Remove(this);
			}
		}
	}

	public bool IsPlayerInBoat(ZDOID zdoid)
	{
		foreach (Player player in m_players)
		{
			if (player.GetZDOID() == zdoid)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsPlayerInBoat(Player player)
	{
		return m_players.Contains(player);
	}

	public bool HasPlayerOnboard()
	{
		return m_players.Count > 0;
	}

	private void OnDestroyed()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			Gogan.LogEvent("Game", "ShipDestroyed", base.gameObject.name, 0L);
		}
		m_currentShips.Remove(this);
	}

	public bool IsWindControllActive()
	{
		foreach (Player player in m_players)
		{
			if (player.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.SailingPower))
			{
				return true;
			}
		}
		return false;
	}

	public static Ship GetLocalShip()
	{
		if (m_currentShips.Count == 0)
		{
			return null;
		}
		return m_currentShips[m_currentShips.Count - 1];
	}

	public bool HaveControllingPlayer()
	{
		if (m_players.Count == 0)
		{
			return false;
		}
		return m_shipControlls.HaveValidUser();
	}

	public bool IsOwner()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.IsOwner();
	}

	public float GetSpeed()
	{
		return Vector3.Dot(m_body.get_velocity(), base.transform.forward);
	}

	public Speed GetSpeedSetting()
	{
		return m_speed;
	}

	public float GetRudder()
	{
		return m_rudder;
	}

	public float GetRudderValue()
	{
		return m_rudderValue;
	}

	public float GetShipYawAngle()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return 0f;
		}
		return 0f - Utils.YawFromDirection(mainCamera.transform.InverseTransformDirection(base.transform.forward));
	}

	public float GetWindAngle()
	{
		Vector3 windDir = EnvMan.instance.GetWindDir();
		return 0f - Utils.YawFromDirection(base.transform.InverseTransformDirection(windDir));
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position + base.transform.forward * m_stearForceOffset, 0.25f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position + base.transform.up * m_sailForceOffset, 0.25f);
	}
}
