using System;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour, IDestructible, Hoverable, IWaterInteractable
{
	public enum Faction
	{
		Players,
		AnimalsVeg,
		ForestMonsters,
		Undead,
		Demon,
		MountainMonsters,
		SeaMonsters,
		PlainsMonsters,
		Boss
	}

	public enum GroundTiltType
	{
		None,
		Pitch,
		Full,
		PitchRaycast,
		FullRaycast
	}

	private float m_underWorldCheckTimer;

	private Collider m_lowestContactCollider;

	private bool m_groundContact;

	private Vector3 m_groundContactPoint = Vector3.zero;

	private Vector3 m_groundContactNormal = Vector3.zero;

	public Action<float, Character> m_onDamaged;

	public Action m_onDeath;

	public Action<int> m_onLevelSet;

	public Action<Vector3> m_onLand;

	[Header("Character")]
	public string m_name = "";

	public Faction m_faction = Faction.AnimalsVeg;

	public bool m_boss;

	public string m_bossEvent = "";

	public string m_defeatSetGlobalKey = "";

	[Header("Movement & Physics")]
	public float m_crouchSpeed = 2f;

	public float m_walkSpeed = 5f;

	public float m_speed = 10f;

	public float m_turnSpeed = 300f;

	public float m_runSpeed = 20f;

	public float m_runTurnSpeed = 300f;

	public float m_flySlowSpeed = 5f;

	public float m_flyFastSpeed = 12f;

	public float m_flyTurnSpeed = 12f;

	public float m_acceleration = 1f;

	public float m_jumpForce = 10f;

	public float m_jumpForceForward;

	public float m_jumpForceTiredFactor = 0.7f;

	public float m_airControl = 0.1f;

	private const float m_slopeStaminaDrain = 10f;

	public const float m_minSlideDegreesPlayer = 38f;

	public const float m_minSlideDegreesMonster = 90f;

	private const float m_rootMotionMultiplier = 55f;

	private const float m_continousPushForce = 10f;

	private const float m_pushForcedissipation = 100f;

	private const float m_maxMoveForce = 20f;

	public bool m_canSwim = true;

	public float m_swimDepth = 2f;

	public float m_swimSpeed = 2f;

	public float m_swimTurnSpeed = 100f;

	public float m_swimAcceleration = 0.05f;

	public GroundTiltType m_groundTilt;

	public bool m_flying;

	public float m_jumpStaminaUsage = 10f;

	[Header("Bodyparts")]
	public Transform m_eye;

	protected Transform m_head;

	[Header("Effects")]
	public EffectList m_hitEffects = new EffectList();

	public EffectList m_critHitEffects = new EffectList();

	public EffectList m_backstabHitEffects = new EffectList();

	public EffectList m_deathEffects = new EffectList();

	public EffectList m_waterEffects = new EffectList();

	public EffectList m_slideEffects = new EffectList();

	public EffectList m_jumpEffects = new EffectList();

	[Header("Health & Damage")]
	public bool m_tolerateWater = true;

	public bool m_tolerateFire;

	public bool m_tolerateSmoke = true;

	public float m_health = 10f;

	public HitData.DamageModifiers m_damageModifiers;

	public bool m_staggerWhenBlocked = true;

	public float m_staggerDamageFactor;

	private const float m_staggerResetTime = 3f;

	private float m_staggerDamage;

	private float m_staggerTimer;

	private float m_backstabTime = -99999f;

	private const float m_backstabResetTime = 300f;

	private GameObject[] m_waterEffects_instances;

	private GameObject[] m_slideEffects_instances;

	protected Vector3 m_moveDir = Vector3.zero;

	protected Vector3 m_lookDir = Vector3.forward;

	protected Quaternion m_lookYaw = Quaternion.identity;

	protected bool m_run;

	protected bool m_walk;

	protected bool m_attack;

	protected bool m_attackDraw;

	protected bool m_secondaryAttack;

	protected bool m_blocking;

	protected GameObject m_visual;

	protected LODGroup m_lodGroup;

	protected Rigidbody m_body;

	protected CapsuleCollider m_collider;

	protected ZNetView m_nview;

	protected ZSyncAnimation m_zanim;

	protected Animator m_animator;

	protected CharacterAnimEvent m_animEvent;

	protected BaseAI m_baseAI;

	private const float m_maxFallHeight = 20f;

	private const float m_minFallHeight = 4f;

	private const float m_maxFallDamage = 100f;

	private const float m_staggerDamageBonus = 2f;

	private const float m_baseVisualRange = 30f;

	private const float m_autoJumpInterval = 0.5f;

	private float m_jumpTimer;

	private float m_lastAutoJumpTime;

	private float m_lastGroundTouch;

	private Vector3 m_lastGroundNormal = Vector3.up;

	private Vector3 m_lastGroundPoint = Vector3.up;

	private Collider m_lastGroundCollider;

	private Rigidbody m_lastGroundBody;

	private Vector3 m_lastAttachPos = Vector3.zero;

	private Rigidbody m_lastAttachBody;

	protected float m_maxAirAltitude = -10000f;

	protected float m_waterLevel = -10000f;

	private float m_swimTimer = 999f;

	protected SEMan m_seman;

	private float m_noiseRange;

	private float m_syncNoiseTimer;

	private bool m_tamed;

	private float m_lastTamedCheck;

	private int m_level = 1;

	private Vector3 m_currentVel = Vector3.zero;

	private float m_currentTurnVel;

	private float m_currentTurnVelChange;

	private Vector3 m_groundTiltNormal = Vector3.up;

	protected Vector3 m_pushForce = Vector3.zero;

	private Vector3 m_rootMotion = Vector3.zero;

	private static int forward_speed = 0;

	private static int sideway_speed = 0;

	private static int turn_speed = 0;

	private static int inWater = 0;

	private static int onGround = 0;

	private static int encumbered = 0;

	private static int flying = 0;

	private float m_slippage;

	protected bool m_wallRunning;

	protected bool m_sliding;

	protected bool m_running;

	private Vector3 m_originalLocalRef;

	private bool m_lodVisible = true;

	private static int m_smokeRayMask = 0;

	private float m_smokeCheckTimer;

	private static bool m_dpsDebugEnabled = false;

	private static List<KeyValuePair<float, float>> m_enemyDamage = new List<KeyValuePair<float, float>>();

	private static List<KeyValuePair<float, float>> m_playerDamage = new List<KeyValuePair<float, float>>();

	private static List<Character> m_characters = new List<Character>();

	protected static int m_characterLayer = 0;

	protected static int m_characterNetLayer = 0;

	protected static int m_characterGhostLayer = 0;

	protected static int m_animatorTagFreeze = Animator.StringToHash("freeze");

	protected static int m_animatorTagStagger = Animator.StringToHash("stagger");

	protected static int m_animatorTagSitting = Animator.StringToHash("sitting");

	protected virtual void Awake()
	{
		m_characters.Add(this);
		m_collider = GetComponent<CapsuleCollider>();
		m_body = GetComponent<Rigidbody>();
		m_zanim = GetComponent<ZSyncAnimation>();
		m_nview = GetComponent<ZNetView>();
		m_animator = GetComponentInChildren<Animator>();
		m_animEvent = ((Component)(object)m_animator).GetComponent<CharacterAnimEvent>();
		m_baseAI = GetComponent<BaseAI>();
		m_animator.set_logWarnings(false);
		m_visual = base.transform.Find("Visual").gameObject;
		m_lodGroup = m_visual.GetComponent<LODGroup>();
		m_head = m_animator.GetBoneTransform((HumanBodyBones)10);
		m_body.set_maxDepenetrationVelocity(2f);
		if (m_smokeRayMask == 0)
		{
			m_smokeRayMask = LayerMask.GetMask("smoke");
			m_characterLayer = LayerMask.NameToLayer("character");
			m_characterNetLayer = LayerMask.NameToLayer("character_net");
			m_characterGhostLayer = LayerMask.NameToLayer("character_ghost");
		}
		if (forward_speed == 0)
		{
			forward_speed = ZSyncAnimation.GetHash("forward_speed");
			sideway_speed = ZSyncAnimation.GetHash("sideway_speed");
			turn_speed = ZSyncAnimation.GetHash("turn_speed");
			inWater = ZSyncAnimation.GetHash("inWater");
			onGround = ZSyncAnimation.GetHash("onGround");
			encumbered = ZSyncAnimation.GetHash("encumbered");
			flying = ZSyncAnimation.GetHash("flying");
		}
		if ((bool)m_lodGroup)
		{
			m_originalLocalRef = m_lodGroup.localReferencePoint;
		}
		m_seman = new SEMan(this, m_nview);
		if (m_nview.GetZDO() == null)
		{
			return;
		}
		if (!IsPlayer())
		{
			if (m_nview.IsOwner() && GetHealth() == GetMaxHealth())
			{
				SetupMaxHealth();
			}
			m_tamed = m_nview.GetZDO().GetBool("tamed", m_tamed);
			m_level = m_nview.GetZDO().GetInt("level", 1);
		}
		m_nview.Register<HitData>("Damage", RPC_Damage);
		m_nview.Register<float, bool>("Heal", RPC_Heal);
		m_nview.Register<float>("AddNoise", RPC_AddNoise);
		m_nview.Register<Vector3>("Stagger", RPC_Stagger);
		m_nview.Register("ResetCloth", RPC_ResetCloth);
		m_nview.Register<bool>("SetTamed", RPC_SetTamed);
	}

	private void SetupMaxHealth()
	{
		int level = GetLevel();
		float difficultyHealthScale = Game.instance.GetDifficultyHealthScale(base.transform.position);
		SetMaxHealth(m_health * difficultyHealthScale * (float)level);
	}

	protected virtual void Start()
	{
		m_nview.GetZDO();
	}

	public virtual void OnDestroy()
	{
		m_seman.OnDestroy();
		m_characters.Remove(this);
	}

	public void SetLevel(int level)
	{
		if (level >= 1)
		{
			m_level = level;
			m_nview.GetZDO().Set("level", level);
			SetupMaxHealth();
			if (m_onLevelSet != null)
			{
				m_onLevelSet(m_level);
			}
		}
	}

	public int GetLevel()
	{
		return m_level;
	}

	public virtual bool IsPlayer()
	{
		return false;
	}

	public Faction GetFaction()
	{
		return m_faction;
	}

	protected virtual void FixedUpdate()
	{
		if (m_nview.IsValid())
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			UpdateLayer();
			UpdateContinousEffects();
			UpdateWater(fixedDeltaTime);
			UpdateGroundTilt(fixedDeltaTime);
			SetVisible(m_nview.HasOwner());
			if (m_nview.IsOwner())
			{
				UpdateGroundContact(fixedDeltaTime);
				UpdateNoise(fixedDeltaTime);
				m_seman.Update(fixedDeltaTime);
				UpdateStagger(fixedDeltaTime);
				UpdatePushback(fixedDeltaTime);
				UpdateMotion(fixedDeltaTime);
				UpdateSmoke(fixedDeltaTime);
				UnderWorldCheck(fixedDeltaTime);
				SyncVelocity();
				CheckDeath();
			}
		}
	}

	private void UpdateLayer()
	{
		if (((Component)(object)m_collider).gameObject.layer == m_characterLayer || ((Component)(object)m_collider).gameObject.layer == m_characterNetLayer)
		{
			if (m_nview.IsOwner())
			{
				((Component)(object)m_collider).gameObject.layer = (IsAttached() ? m_characterNetLayer : m_characterLayer);
			}
			else
			{
				((Component)(object)m_collider).gameObject.layer = m_characterNetLayer;
			}
		}
	}

	private void UnderWorldCheck(float dt)
	{
		if (IsDead())
		{
			return;
		}
		m_underWorldCheckTimer += dt;
		if (m_underWorldCheckTimer > 5f || IsPlayer())
		{
			m_underWorldCheckTimer = 0f;
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			if (base.transform.position.y < groundHeight - 1f)
			{
				Vector3 position = base.transform.position;
				position.y = groundHeight + 0.5f;
				base.transform.position = position;
				m_body.set_position(position);
				m_body.set_velocity(Vector3.zero);
			}
		}
	}

	private void UpdateSmoke(float dt)
	{
		if (m_tolerateSmoke)
		{
			return;
		}
		m_smokeCheckTimer += dt;
		if (m_smokeCheckTimer > 2f)
		{
			m_smokeCheckTimer = 0f;
			if (Physics.CheckSphere(GetTopPoint() + Vector3.up * 0.1f, 0.5f, m_smokeRayMask))
			{
				m_seman.AddStatusEffect("Smoked", resetTime: true);
			}
			else
			{
				m_seman.RemoveStatusEffect("Smoked", quiet: true);
			}
		}
	}

	private void UpdateContinousEffects()
	{
		SetupContinousEffect(base.transform.position, m_sliding, m_slideEffects, ref m_slideEffects_instances);
		Vector3 position = base.transform.position;
		position.y = m_waterLevel + 0.05f;
		SetupContinousEffect(position, InWater(), m_waterEffects, ref m_waterEffects_instances);
	}

	private void SetupContinousEffect(Vector3 point, bool enabled, EffectList effects, ref GameObject[] instances)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		if (!effects.HasEffects())
		{
			return;
		}
		if (enabled)
		{
			if (instances == null)
			{
				instances = effects.Create(point, Quaternion.identity, base.transform);
				return;
			}
			GameObject[] array = instances;
			foreach (GameObject gameObject in array)
			{
				if ((bool)gameObject)
				{
					gameObject.transform.position = point;
				}
			}
		}
		else
		{
			if (instances == null)
			{
				return;
			}
			GameObject[] array = instances;
			foreach (GameObject gameObject2 in array)
			{
				if ((bool)gameObject2)
				{
					ParticleSystem[] componentsInChildren = gameObject2.GetComponentsInChildren<ParticleSystem>();
					foreach (ParticleSystem obj in componentsInChildren)
					{
						EmissionModule emission = obj.get_emission();
						((EmissionModule)(ref emission)).set_enabled(false);
						obj.Stop();
					}
					CamShaker componentInChildren = gameObject2.GetComponentInChildren<CamShaker>();
					if ((bool)componentInChildren)
					{
						UnityEngine.Object.Destroy(componentInChildren);
					}
					ZSFX componentInChildren2 = gameObject2.GetComponentInChildren<ZSFX>();
					if ((bool)componentInChildren2)
					{
						componentInChildren2.FadeOut();
					}
					TimedDestruction component = gameObject2.GetComponent<TimedDestruction>();
					if ((bool)component)
					{
						component.Trigger();
					}
					else
					{
						UnityEngine.Object.Destroy(gameObject2);
					}
				}
			}
			instances = null;
		}
	}

	protected virtual void OnSwiming(Vector3 targetVel, float dt)
	{
	}

	protected virtual void OnSneaking(float dt)
	{
	}

	protected virtual void OnJump()
	{
	}

	protected virtual bool TakeInput()
	{
		return true;
	}

	private float GetSlideAngle()
	{
		if (!IsPlayer())
		{
			return 90f;
		}
		return 38f;
	}

	private void ApplySlide(float dt, ref Vector3 currentVel, Vector3 bodyVel, bool running)
	{
		bool flag = CanWallRun();
		float num = Mathf.Acos(Mathf.Clamp01(m_lastGroundNormal.y)) * 57.29578f;
		Vector3 lastGroundNormal = m_lastGroundNormal;
		lastGroundNormal.y = 0f;
		lastGroundNormal.Normalize();
		m_body.get_velocity();
		Vector3 a = Vector3.Cross(rhs: Vector3.Cross(m_lastGroundNormal, Vector3.up), lhs: m_lastGroundNormal);
		bool flag2 = currentVel.magnitude > 0.1f;
		if (num > GetSlideAngle())
		{
			if (running && flag && flag2)
			{
				UseStamina(10f * dt);
				m_slippage = 0f;
				m_wallRunning = true;
			}
			else
			{
				m_slippage = Mathf.MoveTowards(m_slippage, 1f, 1f * dt);
			}
			Vector3 b = a * 5f;
			currentVel = Vector3.Lerp(currentVel, b, m_slippage);
			m_sliding = m_slippage > 0.5f;
		}
		else
		{
			m_slippage = 0f;
		}
	}

	private void UpdateMotion(float dt)
	{
		UpdateBodyFriction();
		m_sliding = false;
		m_wallRunning = false;
		m_running = false;
		if (IsDead())
		{
			return;
		}
		if (IsDebugFlying())
		{
			UpdateDebugFly(dt);
			return;
		}
		if (InIntro())
		{
			m_maxAirAltitude = base.transform.position.y;
			m_body.set_velocity(Vector3.zero);
			m_body.set_angularVelocity(Vector3.zero);
		}
		if (!InWaterSwimDepth() && !IsOnGround())
		{
			float y = base.transform.position.y;
			m_maxAirAltitude = Mathf.Max(m_maxAirAltitude, y);
		}
		if (IsSwiming())
		{
			UpdateSwiming(dt);
		}
		else if (m_flying)
		{
			UpdateFlying(dt);
		}
		else
		{
			UpdateWalking(dt);
		}
		m_lastGroundTouch += Time.fixedDeltaTime;
		m_jumpTimer += Time.fixedDeltaTime;
	}

	private void UpdateDebugFly(float dt)
	{
		float num = (m_run ? 50 : 20);
		Vector3 b = m_moveDir * num;
		if (TakeInput())
		{
			if (ZInput.GetButton("Jump"))
			{
				b.y = num;
			}
			else if (Input.GetKey(KeyCode.LeftControl))
			{
				b.y = 0f - num;
			}
		}
		m_currentVel = Vector3.Lerp(m_currentVel, b, 0.5f);
		m_body.set_velocity(m_currentVel);
		m_body.set_useGravity(false);
		m_lastGroundTouch = 0f;
		m_maxAirAltitude = base.transform.position.y;
		m_body.set_rotation(Quaternion.RotateTowards(base.transform.rotation, m_lookYaw, m_turnSpeed * dt));
		m_body.set_angularVelocity(Vector3.zero);
		UpdateEyeRotation();
	}

	private void UpdateSwiming(float dt)
	{
		bool flag = IsOnGround();
		if (Mathf.Max(0f, m_maxAirAltitude - base.transform.position.y) > 0.5f && m_onLand != null)
		{
			m_onLand(new Vector3(base.transform.position.x, m_waterLevel, base.transform.position.z));
		}
		m_maxAirAltitude = base.transform.position.y;
		float speed = m_swimSpeed * GetAttackSpeedFactorMovement();
		if (InMinorAction())
		{
			speed = 0f;
		}
		m_seman.ApplyStatusEffectSpeedMods(ref speed);
		Vector3 vector = m_moveDir * speed;
		if (vector.magnitude > 0f && IsOnGround())
		{
			vector = Vector3.ProjectOnPlane(vector, m_lastGroundNormal).normalized * vector.magnitude;
		}
		if (IsPlayer())
		{
			m_currentVel = Vector3.Lerp(m_currentVel, vector, m_swimAcceleration);
		}
		else
		{
			float magnitude = vector.magnitude;
			float magnitude2 = m_currentVel.magnitude;
			if (magnitude > magnitude2)
			{
				magnitude = Mathf.MoveTowards(magnitude2, magnitude, m_swimAcceleration);
				vector = vector.normalized * magnitude;
			}
			m_currentVel = Vector3.Lerp(m_currentVel, vector, 0.5f);
		}
		if (vector.magnitude > 0.1f)
		{
			AddNoise(15f);
		}
		AddPushbackForce(ref m_currentVel);
		Vector3 vector2 = m_currentVel - m_body.get_velocity();
		vector2.y = 0f;
		if (vector2.magnitude > 20f)
		{
			vector2 = vector2.normalized * 20f;
		}
		m_body.AddForce(vector2, (ForceMode)2);
		float num = m_waterLevel - m_swimDepth;
		if (base.transform.position.y < num)
		{
			float t = Mathf.Clamp01((num - base.transform.position.y) / 2f);
			float target = Mathf.Lerp(0f, 10f, t);
			Vector3 velocity = m_body.get_velocity();
			velocity.y = Mathf.MoveTowards(velocity.y, target, 50f * dt);
			m_body.set_velocity(velocity);
		}
		else
		{
			float t2 = Mathf.Clamp01((0f - (num - base.transform.position.y)) / 1f);
			float num2 = Mathf.Lerp(0f, 10f, t2);
			Vector3 velocity2 = m_body.get_velocity();
			velocity2.y = Mathf.MoveTowards(velocity2.y, 0f - num2, 30f * dt);
			m_body.set_velocity(velocity2);
		}
		float target2 = 0f;
		if (m_moveDir.magnitude > 0.1f || AlwaysRotateCamera())
		{
			float speed2 = m_swimTurnSpeed;
			m_seman.ApplyStatusEffectSpeedMods(ref speed2);
			target2 = UpdateRotation(speed2, dt);
		}
		m_body.set_angularVelocity(Vector3.zero);
		UpdateEyeRotation();
		m_body.set_useGravity(true);
		float num3 = Vector3.Dot(m_currentVel, base.transform.forward);
		float value = Vector3.Dot(m_currentVel, base.transform.right);
		float num4 = Vector3.Dot(m_body.get_velocity(), base.transform.forward);
		m_currentTurnVel = Mathf.SmoothDamp(m_currentTurnVel, target2, ref m_currentTurnVelChange, 0.5f, 99f);
		m_zanim.SetFloat(forward_speed, IsPlayer() ? num3 : num4);
		m_zanim.SetFloat(sideway_speed, value);
		m_zanim.SetFloat(turn_speed, m_currentTurnVel);
		m_zanim.SetBool(inWater, !flag);
		m_zanim.SetBool(onGround, value: false);
		m_zanim.SetBool(encumbered, value: false);
		m_zanim.SetBool(flying, value: false);
		if (!flag)
		{
			OnSwiming(vector, dt);
		}
	}

	private void UpdateFlying(float dt)
	{
		float d = (m_run ? m_flyFastSpeed : m_flySlowSpeed) * GetAttackSpeedFactorMovement();
		Vector3 b = (CanMove() ? (m_moveDir * d) : Vector3.zero);
		m_currentVel = Vector3.Lerp(m_currentVel, b, m_acceleration);
		m_maxAirAltitude = base.transform.position.y;
		ApplyRootMotion(ref m_currentVel);
		AddPushbackForce(ref m_currentVel);
		Vector3 vector = m_currentVel - m_body.get_velocity();
		if (vector.magnitude > 20f)
		{
			vector = vector.normalized * 20f;
		}
		m_body.AddForce(vector, (ForceMode)2);
		float target = 0f;
		if ((m_moveDir.magnitude > 0.1f || AlwaysRotateCamera()) && !InDodge() && CanMove())
		{
			float speed = m_flyTurnSpeed;
			m_seman.ApplyStatusEffectSpeedMods(ref speed);
			target = UpdateRotation(speed, dt);
		}
		m_body.set_angularVelocity(Vector3.zero);
		UpdateEyeRotation();
		m_body.set_useGravity(false);
		float num = Vector3.Dot(m_currentVel, base.transform.forward);
		float value = Vector3.Dot(m_currentVel, base.transform.right);
		float num2 = Vector3.Dot(m_body.get_velocity(), base.transform.forward);
		m_currentTurnVel = Mathf.SmoothDamp(m_currentTurnVel, target, ref m_currentTurnVelChange, 0.5f, 99f);
		m_zanim.SetFloat(forward_speed, IsPlayer() ? num : num2);
		m_zanim.SetFloat(sideway_speed, value);
		m_zanim.SetFloat(turn_speed, m_currentTurnVel);
		m_zanim.SetBool(inWater, value: false);
		m_zanim.SetBool(onGround, value: false);
		m_zanim.SetBool(encumbered, value: false);
		m_zanim.SetBool(flying, value: true);
	}

	private void UpdateWalking(float dt)
	{
		Vector3 moveDir = m_moveDir;
		bool flag = IsCrouching();
		m_running = CheckRun(moveDir, dt);
		float num = m_speed * GetJogSpeedFactor();
		if ((m_walk || InMinorAction()) && !flag)
		{
			num = m_walkSpeed;
		}
		else if (m_running)
		{
			bool num2 = InWaterDepth() > 0.4f;
			float num3 = m_runSpeed * GetRunSpeedFactor();
			num = (num2 ? Mathf.Lerp(num, num3, 0.33f) : num3);
			if (IsPlayer() && moveDir.magnitude > 0f)
			{
				moveDir.Normalize();
			}
		}
		else if (flag || IsEncumbered())
		{
			num = m_crouchSpeed;
		}
		num *= GetAttackSpeedFactorMovement();
		m_seman.ApplyStatusEffectSpeedMods(ref num);
		Vector3 vector = (CanMove() ? (moveDir * num) : Vector3.zero);
		if (vector.magnitude > 0f && IsOnGround())
		{
			vector = Vector3.ProjectOnPlane(vector, m_lastGroundNormal).normalized * vector.magnitude;
		}
		float magnitude = vector.magnitude;
		float magnitude2 = m_currentVel.magnitude;
		if (magnitude > magnitude2)
		{
			magnitude = Mathf.MoveTowards(magnitude2, magnitude, m_acceleration);
			vector = vector.normalized * magnitude;
		}
		else if (IsPlayer())
		{
			magnitude = Mathf.MoveTowards(magnitude2, magnitude, m_acceleration * 2f);
			vector = ((vector.magnitude > 0f) ? (vector.normalized * magnitude) : (m_currentVel.normalized * magnitude));
		}
		m_currentVel = Vector3.Lerp(m_currentVel, vector, 0.5f);
		Vector3 velocity = m_body.get_velocity();
		Vector3 vel = m_currentVel;
		vel.y = velocity.y;
		if (IsOnGround() && (UnityEngine.Object)(object)m_lastAttachBody == null)
		{
			ApplySlide(dt, ref vel, velocity, m_running);
		}
		ApplyRootMotion(ref vel);
		AddPushbackForce(ref vel);
		ApplyGroundForce(ref vel, vector);
		Vector3 vector2 = vel - velocity;
		if (!IsOnGround())
		{
			if (vector.magnitude > 0.1f)
			{
				vector2 *= m_airControl;
			}
			else
			{
				vector2 = Vector3.zero;
			}
		}
		if (IsAttached())
		{
			vector2 = Vector3.zero;
		}
		if (IsSneaking())
		{
			OnSneaking(dt);
		}
		if (vector2.magnitude > 20f)
		{
			vector2 = vector2.normalized * 20f;
		}
		if (vector2.magnitude > 0.01f)
		{
			m_body.AddForce(vector2, (ForceMode)2);
		}
		if ((bool)(UnityEngine.Object)(object)m_lastGroundBody && ((Component)(object)m_lastGroundBody).gameObject.layer != base.gameObject.layer && m_lastGroundBody.get_mass() > m_body.get_mass())
		{
			float d = m_body.get_mass() / m_lastGroundBody.get_mass();
			m_lastGroundBody.AddForceAtPosition(-vector2 * d, base.transform.position, (ForceMode)2);
		}
		float target = 0f;
		if ((moveDir.magnitude > 0.1f || AlwaysRotateCamera()) && !InDodge() && CanMove())
		{
			float speed = (m_run ? m_runTurnSpeed : m_turnSpeed);
			m_seman.ApplyStatusEffectSpeedMods(ref speed);
			target = UpdateRotation(speed, dt);
		}
		UpdateEyeRotation();
		m_body.set_useGravity(true);
		float num4 = Vector3.Dot(m_currentVel, Vector3.ProjectOnPlane(base.transform.forward, m_lastGroundNormal).normalized);
		float value = Vector3.Dot(m_currentVel, Vector3.ProjectOnPlane(base.transform.right, m_lastGroundNormal).normalized);
		float num5 = Vector3.Dot(m_body.get_velocity(), base.transform.forward);
		m_currentTurnVel = Mathf.SmoothDamp(m_currentTurnVel, target, ref m_currentTurnVelChange, 0.5f, 99f);
		m_zanim.SetFloat(forward_speed, IsPlayer() ? num4 : num5);
		m_zanim.SetFloat(sideway_speed, value);
		m_zanim.SetFloat(turn_speed, m_currentTurnVel);
		m_zanim.SetBool(inWater, value: false);
		m_zanim.SetBool(onGround, IsOnGround());
		m_zanim.SetBool(encumbered, IsEncumbered());
		m_zanim.SetBool(flying, value: false);
		if (m_currentVel.magnitude > 0.1f)
		{
			if (m_running)
			{
				AddNoise(30f);
			}
			else if (!flag)
			{
				AddNoise(15f);
			}
		}
	}

	public bool IsSneaking()
	{
		if (IsCrouching() && m_currentVel.magnitude > 0.1f)
		{
			return IsOnGround();
		}
		return false;
	}

	private float GetSlopeAngle()
	{
		if (!IsOnGround())
		{
			return 0f;
		}
		float num = Vector3.SignedAngle(base.transform.forward, m_lastGroundNormal, base.transform.right);
		return 0f - (90f - (0f - num));
	}

	protected void AddPushbackForce(ref Vector3 velocity)
	{
		if (m_pushForce != Vector3.zero)
		{
			Vector3 normalized = m_pushForce.normalized;
			float num = Vector3.Dot(normalized, velocity);
			if (num < 10f)
			{
				velocity += normalized * (10f - num);
			}
			if (IsSwiming() || m_flying)
			{
				velocity *= 0.5f;
			}
		}
	}

	private void ApplyPushback(HitData hit)
	{
		if (hit.m_pushForce != 0f)
		{
			float d = Mathf.Min(40f, hit.m_pushForce / m_body.get_mass() * 5f);
			Vector3 pushForce = hit.m_dir * d;
			pushForce.y = 0f;
			if (m_pushForce.magnitude < pushForce.magnitude)
			{
				m_pushForce = pushForce;
			}
		}
	}

	private void UpdatePushback(float dt)
	{
		m_pushForce = Vector3.MoveTowards(m_pushForce, Vector3.zero, 100f * dt);
	}

	private void ApplyGroundForce(ref Vector3 vel, Vector3 targetVel)
	{
		Vector3 vector = Vector3.zero;
		if (IsOnGround() && (bool)(UnityEngine.Object)(object)m_lastGroundBody)
		{
			vector = m_lastGroundBody.GetPointVelocity(base.transform.position);
			vector.y = 0f;
		}
		Ship standingOnShip = GetStandingOnShip();
		if (standingOnShip != null)
		{
			if (targetVel.magnitude > 0.01f)
			{
				m_lastAttachBody = null;
			}
			else if ((UnityEngine.Object)(object)m_lastAttachBody != (UnityEngine.Object)(object)m_lastGroundBody)
			{
				m_lastAttachBody = m_lastGroundBody;
				m_lastAttachPos = ((Component)(object)m_lastAttachBody).transform.InverseTransformPoint(m_body.get_position());
			}
			if ((bool)(UnityEngine.Object)(object)m_lastAttachBody)
			{
				Vector3 vector2 = ((Component)(object)m_lastAttachBody).transform.TransformPoint(m_lastAttachPos);
				Vector3 a = vector2 - m_body.get_position();
				if (a.magnitude < 4f)
				{
					Vector3 position = vector2;
					position.y = m_body.get_position().y;
					if (standingOnShip.IsOwner())
					{
						a.y = 0f;
						vector += a * 10f;
					}
					else
					{
						m_body.set_position(position);
					}
				}
				else
				{
					m_lastAttachBody = null;
				}
			}
		}
		else
		{
			m_lastAttachBody = null;
		}
		vel += vector;
	}

	private float UpdateRotation(float turnSpeed, float dt)
	{
		Quaternion quaternion = (AlwaysRotateCamera() ? m_lookYaw : Quaternion.LookRotation(m_moveDir));
		float yawDeltaAngle = Utils.GetYawDeltaAngle(base.transform.rotation, quaternion);
		float num = 1f;
		if (!IsPlayer())
		{
			num = Mathf.Clamp01(Mathf.Abs(yawDeltaAngle) / 90f);
			num = Mathf.Pow(num, 0.5f);
		}
		float num2 = turnSpeed * GetAttackSpeedFactorRotation() * num;
		Quaternion rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, num2 * dt);
		if (Mathf.Abs(yawDeltaAngle) > 0.001f)
		{
			base.transform.rotation = rotation;
		}
		return num2 * Mathf.Sign(yawDeltaAngle) * ((float)Math.PI / 180f);
	}

	private void UpdateGroundTilt(float dt)
	{
		if (m_visual == null)
		{
			return;
		}
		if (m_nview.IsOwner())
		{
			if (m_groundTilt != 0)
			{
				if (!IsFlying() && IsOnGround())
				{
					Vector3 vector = m_lastGroundNormal;
					if (m_groundTilt == GroundTiltType.PitchRaycast || m_groundTilt == GroundTiltType.FullRaycast)
					{
						Vector3 p = base.transform.position + base.transform.forward * m_collider.get_radius();
						Vector3 p2 = base.transform.position - base.transform.forward * m_collider.get_radius();
						ZoneSystem.instance.GetSolidHeight(p, out var _, out var normal);
						ZoneSystem.instance.GetSolidHeight(p2, out var _, out var normal2);
						vector = (vector + normal + normal2).normalized;
					}
					Vector3 target = base.transform.InverseTransformVector(vector);
					target = Vector3.RotateTowards(Vector3.up, target, 0.87266463f, 1f);
					m_groundTiltNormal = Vector3.Lerp(m_groundTiltNormal, target, 0.05f);
					Vector3 vector2;
					if (m_groundTilt == GroundTiltType.Pitch || m_groundTilt == GroundTiltType.PitchRaycast)
					{
						Vector3 b = Vector3.Project(m_groundTiltNormal, Vector3.right);
						vector2 = m_groundTiltNormal - b;
					}
					else
					{
						vector2 = m_groundTiltNormal;
					}
					Vector3 forward = Vector3.Cross(vector2, Vector3.left);
					m_visual.transform.localRotation = Quaternion.LookRotation(forward, vector2);
				}
				else
				{
					m_visual.transform.localRotation = Quaternion.RotateTowards(m_visual.transform.localRotation, Quaternion.identity, dt * 200f);
				}
				m_nview.GetZDO().Set("tiltrot", m_visual.transform.localRotation);
			}
			else if (CanWallRun())
			{
				if (m_wallRunning)
				{
					Vector3 vector3 = Vector3.Lerp(Vector3.up, m_lastGroundNormal, 0.65f);
					Vector3 forward2 = Vector3.ProjectOnPlane(base.transform.forward, vector3);
					forward2.Normalize();
					Quaternion to = Quaternion.LookRotation(forward2, vector3);
					m_visual.transform.rotation = Quaternion.RotateTowards(m_visual.transform.rotation, to, 30f * dt);
				}
				else
				{
					m_visual.transform.localRotation = Quaternion.RotateTowards(m_visual.transform.localRotation, Quaternion.identity, 100f * dt);
				}
				m_nview.GetZDO().Set("tiltrot", m_visual.transform.localRotation);
			}
		}
		else if (m_groundTilt != 0 || CanWallRun())
		{
			Quaternion quaternion = m_nview.GetZDO().GetQuaternion("tiltrot", Quaternion.identity);
			m_visual.transform.localRotation = quaternion;
		}
	}

	public bool IsWallRunning()
	{
		return m_wallRunning;
	}

	private bool IsOnSnow()
	{
		return false;
	}

	public void Heal(float hp, bool showText = true)
	{
		if (!(hp <= 0f))
		{
			if (m_nview.IsOwner())
			{
				RPC_Heal(0L, hp, showText);
				return;
			}
			m_nview.InvokeRPC("Heal", hp, showText);
		}
	}

	private void RPC_Heal(long sender, float hp, bool showText)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		float health = GetHealth();
		if (health <= 0f || IsDead())
		{
			return;
		}
		float num = Mathf.Min(health + hp, GetMaxHealth());
		if (num > health)
		{
			SetHealth(num);
			if (showText)
			{
				Vector3 topPoint = GetTopPoint();
				DamageText.instance.ShowText(DamageText.TextType.Heal, topPoint, hp, IsPlayer());
			}
		}
	}

	public Vector3 GetTopPoint()
	{
		Vector3 center = ((Collider)m_collider).get_bounds().center;
		center.y = ((Collider)m_collider).get_bounds().max.y;
		return center;
	}

	public float GetRadius()
	{
		return m_collider.get_radius();
	}

	public Vector3 GetHeadPoint()
	{
		return m_head.position;
	}

	public Vector3 GetEyePoint()
	{
		return m_eye.position;
	}

	public Vector3 GetCenterPoint()
	{
		return ((Collider)m_collider).get_bounds().center;
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Character;
	}

	public void Damage(HitData hit)
	{
		if (m_nview.IsValid())
		{
			m_nview.InvokeRPC("Damage", hit);
		}
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (IsDebugFlying() || !m_nview.IsOwner() || GetHealth() <= 0f || IsDead() || IsTeleporting() || InCutscene() || (hit.m_dodgeable && IsDodgeInvincible()))
		{
			return;
		}
		Character attacker = hit.GetAttacker();
		if ((hit.HaveAttacker() && attacker == null) || (IsPlayer() && !IsPVPEnabled() && attacker != null && attacker.IsPlayer()))
		{
			return;
		}
		if (attacker != null && !attacker.IsPlayer())
		{
			float difficultyDamageScale = Game.instance.GetDifficultyDamageScale(base.transform.position);
			hit.ApplyModifier(difficultyDamageScale);
		}
		m_seman.OnDamaged(hit, attacker);
		if (m_baseAI != null && !m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f && Time.time - m_backstabTime > 300f)
		{
			m_backstabTime = Time.time;
			hit.ApplyModifier(hit.m_backstabBonus);
			m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, base.transform);
		}
		if (IsStaggering() && !IsPlayer())
		{
			hit.ApplyModifier(2f);
			m_critHitEffects.Create(hit.m_point, Quaternion.identity, base.transform);
		}
		if (hit.m_blockable && IsBlocking())
		{
			BlockAttack(hit, attacker);
		}
		ApplyPushback(hit);
		if (!string.IsNullOrEmpty(hit.m_statusEffect))
		{
			StatusEffect statusEffect = m_seman.GetStatusEffect(hit.m_statusEffect);
			if (statusEffect == null)
			{
				statusEffect = m_seman.AddStatusEffect(hit.m_statusEffect);
			}
			if (statusEffect != null && attacker != null)
			{
				statusEffect.SetAttacker(attacker);
			}
		}
		HitData.DamageModifiers damageModifiers = GetDamageModifiers();
		hit.ApplyResistance(damageModifiers, out var significantModifier);
		if (IsPlayer())
		{
			float bodyArmor = GetBodyArmor();
			hit.ApplyArmor(bodyArmor);
			DamageArmorDurability(hit);
		}
		float poison = hit.m_damage.m_poison;
		float fire = hit.m_damage.m_fire;
		float spirit = hit.m_damage.m_spirit;
		hit.m_damage.m_poison = 0f;
		hit.m_damage.m_fire = 0f;
		hit.m_damage.m_spirit = 0f;
		ApplyDamage(hit, showDamageText: true, triggerEffects: true, significantModifier);
		AddFireDamage(fire);
		AddSpiritDamage(spirit);
		AddPoisonDamage(poison);
		AddFrostDamage(hit.m_damage.m_frost);
		AddLightningDamage(hit.m_damage.m_lightning);
	}

	protected HitData.DamageModifier GetDamageModifier(HitData.DamageType damageType)
	{
		return GetDamageModifiers().GetModifier(damageType);
	}

	protected HitData.DamageModifiers GetDamageModifiers()
	{
		HitData.DamageModifiers mods = m_damageModifiers.Clone();
		ApplyArmorDamageMods(ref mods);
		m_seman.ApplyDamageMods(ref mods);
		return mods;
	}

	public void ApplyDamage(HitData hit, bool showDamageText, bool triggerEffects, HitData.DamageModifier mod = HitData.DamageModifier.Normal)
	{
		if (IsDebugFlying() || IsDead() || IsTeleporting() || InCutscene())
		{
			return;
		}
		float totalDamage = hit.GetTotalDamage();
		if (showDamageText && (totalDamage > 0f || !IsPlayer()))
		{
			DamageText.instance.ShowText(mod, hit.m_point, totalDamage, IsPlayer());
		}
		if (totalDamage <= 0f)
		{
			return;
		}
		if (!InGodMode() && !InGhostMode())
		{
			float health = GetHealth();
			health -= totalDamage;
			SetHealth(health);
		}
		float totalPhysicalDamage = hit.m_damage.GetTotalPhysicalDamage();
		AddStaggerDamage(totalPhysicalDamage * hit.m_staggerMultiplier, hit.m_dir);
		if (triggerEffects && totalDamage > 2f)
		{
			DoDamageCameraShake(hit);
			if (hit.m_damage.GetTotalPhysicalDamage() > 0f)
			{
				m_hitEffects.Create(hit.m_point, Quaternion.identity, base.transform);
			}
		}
		OnDamaged(hit);
		if (m_onDamaged != null)
		{
			m_onDamaged(totalDamage, hit.GetAttacker());
		}
		if (m_dpsDebugEnabled)
		{
			AddDPS(totalDamage, this);
		}
	}

	protected virtual void DoDamageCameraShake(HitData hit)
	{
	}

	protected virtual void DamageArmorDurability(HitData hit)
	{
	}

	private void AddFireDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			SE_Burning sE_Burning = m_seman.GetStatusEffect("Burning") as SE_Burning;
			if (sE_Burning == null)
			{
				sE_Burning = m_seman.AddStatusEffect("Burning") as SE_Burning;
			}
			sE_Burning.AddFireDamage(damage);
		}
	}

	private void AddSpiritDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			SE_Burning sE_Burning = m_seman.GetStatusEffect("Spirit") as SE_Burning;
			if (sE_Burning == null)
			{
				sE_Burning = m_seman.AddStatusEffect("Spirit") as SE_Burning;
			}
			sE_Burning.AddSpiritDamage(damage);
		}
	}

	private void AddPoisonDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			SE_Poison sE_Poison = m_seman.GetStatusEffect("Poison") as SE_Poison;
			if (sE_Poison == null)
			{
				sE_Poison = m_seman.AddStatusEffect("Poison") as SE_Poison;
			}
			sE_Poison.AddDamage(damage);
		}
	}

	private void AddFrostDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			SE_Frost sE_Frost = m_seman.GetStatusEffect("Frost") as SE_Frost;
			if (sE_Frost == null)
			{
				sE_Frost = m_seman.AddStatusEffect("Frost") as SE_Frost;
			}
			sE_Frost.AddDamage(damage);
		}
	}

	private void AddLightningDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			m_seman.AddStatusEffect("Lightning", resetTime: true);
		}
	}

	private void AddStaggerDamage(float damage, Vector3 forceDirection)
	{
		if (!(m_staggerDamageFactor <= 0f) || IsPlayer())
		{
			m_staggerDamage += damage;
			m_staggerTimer = 0f;
			float maxHealth = GetMaxHealth();
			float num = (IsPlayer() ? (maxHealth / 2f) : (maxHealth * m_staggerDamageFactor));
			if (m_staggerDamage >= num)
			{
				m_staggerDamage = 0f;
				Stagger(forceDirection);
			}
		}
	}

	private static void AddDPS(float damage, Character me)
	{
		if (me == Player.m_localPlayer)
		{
			CalculateDPS("To-you ", m_playerDamage, damage);
		}
		else
		{
			CalculateDPS("To-others ", m_enemyDamage, damage);
		}
	}

	private static void CalculateDPS(string name, List<KeyValuePair<float, float>> damages, float damage)
	{
		float time = Time.time;
		if (damages.Count > 0 && Time.time - damages[damages.Count - 1].Key > 5f)
		{
			damages.Clear();
		}
		damages.Add(new KeyValuePair<float, float>(time, damage));
		float num = Time.time - damages[0].Key;
		if (num < 0.01f)
		{
			return;
		}
		float num2 = 0f;
		foreach (KeyValuePair<float, float> damage2 in damages)
		{
			num2 += damage2.Value;
		}
		float num3 = num2 / num;
		string text = "DPS " + name + " (" + damages.Count + " attacks): " + num3.ToString("0.0");
		ZLog.Log((object)text);
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
	}

	private void UpdateStagger(float dt)
	{
		if (!(m_staggerDamageFactor <= 0f) || IsPlayer())
		{
			m_staggerTimer += dt;
			if (m_staggerTimer > 3f)
			{
				m_staggerDamage = 0f;
			}
		}
	}

	public void Stagger(Vector3 forceDirection)
	{
		if (m_nview.IsOwner())
		{
			RPC_Stagger(0L, forceDirection);
			return;
		}
		m_nview.InvokeRPC("Stagger", forceDirection);
	}

	private void RPC_Stagger(long sender, Vector3 forceDirection)
	{
		if (!IsStaggering())
		{
			if (forceDirection.magnitude > 0.01f)
			{
				forceDirection.y = 0f;
				base.transform.rotation = Quaternion.LookRotation(-forceDirection);
			}
			m_zanim.SetTrigger("stagger");
		}
	}

	protected virtual void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
	}

	public virtual float GetBodyArmor()
	{
		return 0f;
	}

	protected virtual bool BlockAttack(HitData hit, Character attacker)
	{
		return false;
	}

	protected virtual void OnDamaged(HitData hit)
	{
	}

	private void OnCollisionStay(Collision collision)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (!m_nview.IsValid() || !m_nview.IsOwner() || m_jumpTimer < 0.1f)
		{
			return;
		}
		ContactPoint[] contacts = collision.get_contacts();
		for (int i = 0; i < contacts.Length; i++)
		{
			ContactPoint val = contacts[i];
			float num = ((ContactPoint)(ref val)).get_point().y - base.transform.position.y;
			if (!(((ContactPoint)(ref val)).get_normal().y > 0.1f) || !(num < m_collider.get_radius()))
			{
				continue;
			}
			if (((ContactPoint)(ref val)).get_normal().y > m_groundContactNormal.y || !m_groundContact)
			{
				m_groundContact = true;
				m_groundContactNormal = ((ContactPoint)(ref val)).get_normal();
				m_groundContactPoint = ((ContactPoint)(ref val)).get_point();
				m_lowestContactCollider = collision.get_collider();
				continue;
			}
			Vector3 groundContactNormal = Vector3.Normalize(m_groundContactNormal + ((ContactPoint)(ref val)).get_normal());
			if (groundContactNormal.y > m_groundContactNormal.y)
			{
				m_groundContactNormal = groundContactNormal;
				m_groundContactPoint = (m_groundContactPoint + ((ContactPoint)(ref val)).get_point()) * 0.5f;
			}
		}
	}

	private void UpdateGroundContact(float dt)
	{
		if (!m_groundContact)
		{
			return;
		}
		m_lastGroundCollider = m_lowestContactCollider;
		m_lastGroundNormal = m_groundContactNormal;
		m_lastGroundPoint = m_groundContactPoint;
		m_lastGroundBody = (((UnityEngine.Object)(object)m_lastGroundCollider) ? m_lastGroundCollider.get_attachedRigidbody() : null);
		if (!IsPlayer() && (UnityEngine.Object)(object)m_lastGroundBody != null && ((Component)(object)m_lastGroundBody).gameObject.layer == base.gameObject.layer)
		{
			m_lastGroundCollider = null;
			m_lastGroundBody = null;
		}
		float num = Mathf.Max(0f, m_maxAirAltitude - base.transform.position.y);
		if (num > 0.8f)
		{
			if (m_onLand != null)
			{
				Vector3 lastGroundPoint = m_lastGroundPoint;
				if (InWater())
				{
					lastGroundPoint.y = m_waterLevel;
				}
				m_onLand(m_lastGroundPoint);
			}
			ResetCloth();
		}
		if (IsPlayer() && num > 4f)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = Mathf.Clamp01((num - 4f) / 16f) * 100f;
			hitData.m_point = m_lastGroundPoint;
			hitData.m_dir = m_lastGroundNormal;
			Damage(hitData);
		}
		ResetGroundContact();
		m_lastGroundTouch = 0f;
		m_maxAirAltitude = base.transform.position.y;
	}

	private void ResetGroundContact()
	{
		m_lowestContactCollider = null;
		m_groundContact = false;
		m_groundContactNormal = Vector3.zero;
		m_groundContactPoint = Vector3.zero;
	}

	public Ship GetStandingOnShip()
	{
		if (!IsOnGround())
		{
			return null;
		}
		if ((bool)(UnityEngine.Object)(object)m_lastGroundBody)
		{
			return ((Component)(object)m_lastGroundBody).GetComponent<Ship>();
		}
		return null;
	}

	public bool IsOnGround()
	{
		if (!(m_lastGroundTouch < 0.2f))
		{
			return m_body.IsSleeping();
		}
		return true;
	}

	private void CheckDeath()
	{
		if (!IsDead() && GetHealth() <= 0f)
		{
			OnDeath();
			if (m_onDeath != null)
			{
				m_onDeath();
			}
		}
	}

	protected virtual void OnRagdollCreated(Ragdoll ragdoll)
	{
	}

	protected virtual void OnDeath()
	{
		GameObject[] array = m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if ((bool)component)
			{
				CharacterDrop component2 = GetComponent<CharacterDrop>();
				LevelEffects componentInChildren = GetComponentInChildren<LevelEffects>();
				Vector3 velocity = m_body.get_velocity();
				if (m_pushForce.magnitude * 0.5f > velocity.magnitude)
				{
					velocity = m_pushForce * 0.5f;
				}
				float hue = 0f;
				float saturation = 0f;
				float value = 0f;
				if ((bool)componentInChildren)
				{
					componentInChildren.GetColorChanges(out hue, out saturation, out value);
				}
				component.Setup(velocity, hue, saturation, value, component2);
				OnRagdollCreated(component);
				if ((bool)component2)
				{
					component2.SetDropsEnabled(enabled: false);
				}
			}
		}
		if (!string.IsNullOrEmpty(m_defeatSetGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(m_defeatSetGlobalKey);
		}
		ZNetScene.instance.Destroy(base.gameObject);
		Gogan.LogEvent("Game", "Killed", m_name, 0L);
	}

	public float GetHealth()
	{
		return m_nview.GetZDO()?.GetFloat("health", GetMaxHealth()) ?? GetMaxHealth();
	}

	public void SetHealth(float health)
	{
		ZDO zDO = m_nview.GetZDO();
		if (zDO != null && m_nview.IsOwner())
		{
			if (health < 0f)
			{
				health = 0f;
			}
			zDO.Set("health", health);
		}
	}

	public float GetHealthPercentage()
	{
		return GetHealth() / GetMaxHealth();
	}

	public virtual bool IsDead()
	{
		return false;
	}

	public void SetMaxHealth(float health)
	{
		if (m_nview.GetZDO() != null)
		{
			m_nview.GetZDO().Set("max_health", health);
		}
		if (GetHealth() > health)
		{
			SetHealth(health);
		}
	}

	public float GetMaxHealth()
	{
		if (m_nview.GetZDO() != null)
		{
			return m_nview.GetZDO().GetFloat("max_health", m_health);
		}
		return m_health;
	}

	public virtual float GetMaxStamina()
	{
		return 0f;
	}

	public virtual float GetStaminaPercentage()
	{
		return 1f;
	}

	public bool IsBoss()
	{
		return m_boss;
	}

	public void SetLookDir(Vector3 dir)
	{
		if (dir.magnitude <= Mathf.Epsilon)
		{
			dir = base.transform.forward;
		}
		else
		{
			dir.Normalize();
		}
		m_lookDir = dir;
		dir.y = 0f;
		m_lookYaw = Quaternion.LookRotation(dir);
	}

	public Vector3 GetLookDir()
	{
		return m_eye.forward;
	}

	public virtual void OnAttackTrigger()
	{
	}

	public virtual void OnStopMoving()
	{
	}

	public virtual void OnWeaponTrailStart()
	{
	}

	public void SetMoveDir(Vector3 dir)
	{
		m_moveDir = dir;
	}

	public void SetRun(bool run)
	{
		m_run = run;
	}

	public void SetWalk(bool walk)
	{
		m_walk = walk;
	}

	public bool GetWalk()
	{
		return m_walk;
	}

	protected virtual void UpdateEyeRotation()
	{
		m_eye.rotation = Quaternion.LookRotation(m_lookDir);
	}

	public void OnAutoJump(Vector3 dir, float upVel, float forwardVel)
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && IsOnGround() && !IsDead() && !InAttack() && !InDodge() && !IsKnockedBack() && !(Time.time - m_lastAutoJumpTime < 0.5f))
		{
			m_lastAutoJumpTime = Time.time;
			if (!(Vector3.Dot(m_moveDir, dir) < 0.5f))
			{
				Vector3 zero = Vector3.zero;
				zero.y = upVel;
				zero += dir * forwardVel;
				m_body.set_velocity(zero);
				m_lastGroundTouch = 1f;
				m_jumpTimer = 0f;
				m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform);
				SetCrouch(crouch: false);
				UpdateBodyFriction();
			}
		}
	}

	public void Jump()
	{
		if (!IsOnGround() || IsDead() || InAttack() || IsEncumbered() || InDodge() || IsKnockedBack())
		{
			return;
		}
		bool flag = false;
		if (!HaveStamina(m_jumpStaminaUsage))
		{
			if (IsPlayer())
			{
				Hud.instance.StaminaBarNoStaminaFlash();
			}
			flag = true;
		}
		float num = 0f;
		Skills skills = GetSkills();
		if (skills != null)
		{
			num = skills.GetSkillFactor(Skills.SkillType.Jump);
			if (!flag)
			{
				RaiseSkill(Skills.SkillType.Jump);
			}
		}
		Vector3 velocity = m_body.get_velocity();
		Mathf.Acos(Mathf.Clamp01(m_lastGroundNormal.y));
		Vector3 normalized = (m_lastGroundNormal + Vector3.up).normalized;
		float num2 = 1f + num * 0.4f;
		float num3 = m_jumpForce * num2;
		float num4 = Vector3.Dot(normalized, velocity);
		if (num4 < num3)
		{
			velocity += normalized * (num3 - num4);
		}
		velocity += m_moveDir * m_jumpForceForward * num2;
		if (flag)
		{
			velocity *= m_jumpForceTiredFactor;
		}
		m_body.WakeUp();
		m_body.set_velocity(velocity);
		ResetGroundContact();
		m_lastGroundTouch = 1f;
		m_jumpTimer = 0f;
		m_zanim.SetTrigger("jump");
		AddNoise(30f);
		m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform);
		OnJump();
		SetCrouch(crouch: false);
		UpdateBodyFriction();
	}

	private void UpdateBodyFriction()
	{
		((Collider)m_collider).get_material().set_frictionCombine((PhysicMaterialCombine)1);
		if (IsDead())
		{
			((Collider)m_collider).get_material().set_staticFriction(1f);
			((Collider)m_collider).get_material().set_dynamicFriction(1f);
			((Collider)m_collider).get_material().set_frictionCombine((PhysicMaterialCombine)3);
		}
		else if (IsSwiming())
		{
			((Collider)m_collider).get_material().set_staticFriction(0.2f);
			((Collider)m_collider).get_material().set_dynamicFriction(0.2f);
		}
		else if (!IsOnGround())
		{
			((Collider)m_collider).get_material().set_staticFriction(0f);
			((Collider)m_collider).get_material().set_dynamicFriction(0f);
		}
		else if (IsFlying())
		{
			((Collider)m_collider).get_material().set_staticFriction(0f);
			((Collider)m_collider).get_material().set_dynamicFriction(0f);
		}
		else if (m_moveDir.magnitude < 0.1f)
		{
			((Collider)m_collider).get_material().set_staticFriction(0.8f * (1f - m_slippage));
			((Collider)m_collider).get_material().set_dynamicFriction(0.8f * (1f - m_slippage));
			((Collider)m_collider).get_material().set_frictionCombine((PhysicMaterialCombine)3);
		}
		else
		{
			((Collider)m_collider).get_material().set_staticFriction(0.4f * (1f - m_slippage));
			((Collider)m_collider).get_material().set_dynamicFriction(0.4f * (1f - m_slippage));
		}
	}

	public virtual bool StartAttack(Character target, bool charge)
	{
		return false;
	}

	public virtual void OnNearFire(Vector3 point)
	{
	}

	public ZDOID GetZDOID()
	{
		if (m_nview.IsValid())
		{
			return m_nview.GetZDO().m_uid;
		}
		return ZDOID.None;
	}

	public bool IsOwner()
	{
		if (m_nview.IsValid())
		{
			return m_nview.IsOwner();
		}
		return false;
	}

	public long GetOwner()
	{
		if (m_nview.IsValid())
		{
			return m_nview.GetZDO().m_owner;
		}
		return 0L;
	}

	public virtual bool UseMeleeCamera()
	{
		return false;
	}

	public virtual bool AlwaysRotateCamera()
	{
		return true;
	}

	public void SetInWater(float depth)
	{
		m_waterLevel = depth;
	}

	public virtual bool IsPVPEnabled()
	{
		return false;
	}

	public virtual bool InIntro()
	{
		return false;
	}

	public virtual bool InCutscene()
	{
		return false;
	}

	public virtual bool IsCrouching()
	{
		return false;
	}

	public virtual bool InBed()
	{
		return false;
	}

	public virtual bool IsAttached()
	{
		return false;
	}

	protected virtual void SetCrouch(bool crouch)
	{
	}

	public virtual void AttachStart(Transform attachPoint, bool hideWeapons, bool isBed, string attachAnimation, Vector3 detachOffset)
	{
	}

	public virtual void AttachStop()
	{
	}

	private void UpdateWater(float dt)
	{
		m_swimTimer += dt;
		if (InWaterSwimDepth())
		{
			if (m_nview.IsOwner())
			{
				m_seman.AddStatusEffect("Wet", resetTime: true);
			}
			if (m_canSwim)
			{
				m_swimTimer = 0f;
			}
		}
	}

	public bool IsSwiming()
	{
		return m_swimTimer < 0.5f;
	}

	public bool InWaterSwimDepth()
	{
		return InWaterDepth() > Mathf.Max(0f, m_swimDepth - 0.4f);
	}

	private float InWaterDepth()
	{
		if (GetStandingOnShip() != null)
		{
			return 0f;
		}
		return Mathf.Max(0f, m_waterLevel - base.transform.position.y);
	}

	public bool InWater()
	{
		return InWaterDepth() > 0f;
	}

	protected virtual bool CheckRun(Vector3 moveDir, float dt)
	{
		if (!m_run)
		{
			return false;
		}
		if (moveDir.magnitude < 0.1f)
		{
			return false;
		}
		if (IsCrouching() || IsEncumbered())
		{
			return false;
		}
		if (InDodge())
		{
			return false;
		}
		return true;
	}

	public bool IsRunning()
	{
		return m_running;
	}

	public virtual bool InPlaceMode()
	{
		return false;
	}

	public virtual bool HaveStamina(float amount = 0f)
	{
		return true;
	}

	public virtual void AddStamina(float v)
	{
	}

	public virtual void UseStamina(float stamina)
	{
	}

	public bool IsStaggering()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		AnimatorStateInfo currentAnimatorStateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
		return ((AnimatorStateInfo)(ref currentAnimatorStateInfo)).get_tagHash() == m_animatorTagStagger;
	}

	public virtual bool CanMove()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		AnimatorStateInfo val = (m_animator.IsInTransition(0) ? m_animator.GetNextAnimatorStateInfo(0) : m_animator.GetCurrentAnimatorStateInfo(0));
		if (((AnimatorStateInfo)(ref val)).get_tagHash() == m_animatorTagFreeze || ((AnimatorStateInfo)(ref val)).get_tagHash() == m_animatorTagStagger || ((AnimatorStateInfo)(ref val)).get_tagHash() == m_animatorTagSitting)
		{
			return false;
		}
		return true;
	}

	public virtual bool IsEncumbered()
	{
		return false;
	}

	public virtual bool IsTeleporting()
	{
		return false;
	}

	private bool CanWallRun()
	{
		return IsPlayer();
	}

	public void ShowPickupMessage(ItemDrop.ItemData item, int amount)
	{
		Message(MessageHud.MessageType.TopLeft, "$msg_added " + item.m_shared.m_name, amount, item.GetIcon());
	}

	public void ShowRemovedMessage(ItemDrop.ItemData item, int amount)
	{
		Message(MessageHud.MessageType.TopLeft, "$msg_removed " + item.m_shared.m_name, amount, item.GetIcon());
	}

	public virtual void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
	}

	public CapsuleCollider GetCollider()
	{
		return m_collider;
	}

	public virtual void OnStealthSuccess(Character character, float factor)
	{
	}

	public virtual float GetStealthFactor()
	{
		return 1f;
	}

	private void UpdateNoise(float dt)
	{
		m_noiseRange = Mathf.Max(0f, m_noiseRange - dt * 4f);
		m_syncNoiseTimer += dt;
		if (m_syncNoiseTimer > 0.5f)
		{
			m_syncNoiseTimer = 0f;
			m_nview.GetZDO().Set("noise", m_noiseRange);
		}
	}

	public void AddNoise(float range)
	{
		if (m_nview.IsValid())
		{
			if (m_nview.IsOwner())
			{
				RPC_AddNoise(0L, range);
				return;
			}
			m_nview.InvokeRPC("AddNoise", range);
		}
	}

	private void RPC_AddNoise(long sender, float range)
	{
		if (m_nview.IsOwner() && range > m_noiseRange)
		{
			m_noiseRange = range;
			m_seman.ModifyNoise(m_noiseRange, ref m_noiseRange);
		}
	}

	public float GetNoiseRange()
	{
		if (!m_nview.IsValid())
		{
			return 0f;
		}
		if (m_nview.IsOwner())
		{
			return m_noiseRange;
		}
		return m_nview.GetZDO().GetFloat("noise");
	}

	public virtual bool InGodMode()
	{
		return false;
	}

	public virtual bool InGhostMode()
	{
		return false;
	}

	public virtual bool IsDebugFlying()
	{
		return false;
	}

	public virtual string GetHoverText()
	{
		Tameable component = GetComponent<Tameable>();
		if ((bool)component)
		{
			return component.GetHoverText();
		}
		return "";
	}

	public virtual string GetHoverName()
	{
		return Localization.get_instance().Localize(m_name);
	}

	public virtual bool IsHoldingAttack()
	{
		return false;
	}

	public virtual bool InAttack()
	{
		return false;
	}

	protected virtual void StopEmote()
	{
	}

	public virtual bool InMinorAction()
	{
		return false;
	}

	public virtual bool InDodge()
	{
		return false;
	}

	public virtual bool IsDodgeInvincible()
	{
		return false;
	}

	public virtual bool InEmote()
	{
		return false;
	}

	public virtual bool IsBlocking()
	{
		return false;
	}

	public bool IsFlying()
	{
		return m_flying;
	}

	public bool IsKnockedBack()
	{
		return m_pushForce != Vector3.zero;
	}

	private void OnDrawGizmosSelected()
	{
		if (m_nview != null && m_nview.GetZDO() != null)
		{
			float @float = m_nview.GetZDO().GetFloat("noise");
			Gizmos.DrawWireSphere(base.transform.position, @float);
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * m_swimDepth, new Vector3(1f, 0.05f, 1f));
		if (IsOnGround())
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(m_lastGroundPoint, m_lastGroundPoint + m_lastGroundNormal);
		}
	}

	public virtual bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		return false;
	}

	private void SyncVelocity()
	{
		m_nview.GetZDO().Set("BodyVelocity", m_body.get_velocity());
	}

	public Vector3 GetVelocity()
	{
		if (!m_nview.IsValid())
		{
			return Vector3.zero;
		}
		if (m_nview.IsOwner())
		{
			return m_body.get_velocity();
		}
		return m_nview.GetZDO().GetVec3("BodyVelocity", Vector3.zero);
	}

	public void AddRootMotion(Vector3 vel)
	{
		if (InDodge() || InAttack() || InEmote())
		{
			m_rootMotion += vel;
		}
	}

	private void ApplyRootMotion(ref Vector3 vel)
	{
		Vector3 vector = m_rootMotion * 55f;
		if (vector.magnitude > vel.magnitude)
		{
			vel = vector;
		}
		m_rootMotion = Vector3.zero;
	}

	public static void GetCharactersInRange(Vector3 point, float radius, List<Character> characters)
	{
		foreach (Character character in m_characters)
		{
			if (Vector3.Distance(character.transform.position, point) < radius)
			{
				characters.Add(character);
			}
		}
	}

	public static List<Character> GetAllCharacters()
	{
		return m_characters;
	}

	public static bool IsCharacterInRange(Vector3 point, float range)
	{
		foreach (Character character in m_characters)
		{
			if (Vector3.Distance(character.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	public virtual void OnTargeted(bool sensed, bool alerted)
	{
	}

	public GameObject GetVisual()
	{
		return m_visual;
	}

	protected void UpdateLodgroup()
	{
		if (!(m_lodGroup == null))
		{
			Renderer[] componentsInChildren = m_visual.GetComponentsInChildren<Renderer>();
			LOD[] lODs = m_lodGroup.GetLODs();
			lODs[0].renderers = componentsInChildren;
			m_lodGroup.SetLODs(lODs);
		}
	}

	public virtual float GetEquipmentMovementModifier()
	{
		return 0f;
	}

	protected virtual float GetJogSpeedFactor()
	{
		return 1f;
	}

	protected virtual float GetRunSpeedFactor()
	{
		return 1f;
	}

	protected virtual float GetAttackSpeedFactorMovement()
	{
		return 1f;
	}

	protected virtual float GetAttackSpeedFactorRotation()
	{
		return 1f;
	}

	public virtual void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
	}

	public virtual Skills GetSkills()
	{
		return null;
	}

	public virtual float GetSkillFactor(Skills.SkillType skill)
	{
		return 0f;
	}

	public virtual float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return UnityEngine.Random.Range(0.75f, 1f);
	}

	public bool IsMonsterFaction()
	{
		if (IsTamed())
		{
			return false;
		}
		if (m_faction != Faction.ForestMonsters && m_faction != Faction.Undead && m_faction != Faction.Demon && m_faction != Faction.PlainsMonsters && m_faction != Faction.MountainMonsters)
		{
			return m_faction == Faction.SeaMonsters;
		}
		return true;
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public Collider GetLastGroundCollider()
	{
		return m_lastGroundCollider;
	}

	public Vector3 GetLastGroundNormal()
	{
		return m_groundContactNormal;
	}

	public void ResetCloth()
	{
		m_nview.InvokeRPC(ZNetView.Everybody, "ResetCloth");
	}

	private void RPC_ResetCloth(long sender)
	{
		Cloth[] componentsInChildren = GetComponentsInChildren<Cloth>();
		foreach (Cloth val in componentsInChildren)
		{
			if (val.get_enabled())
			{
				val.set_enabled(false);
				val.set_enabled(true);
			}
		}
	}

	public virtual bool GetRelativePosition(out ZDOID parent, out Vector3 relativePos, out Vector3 relativeVel)
	{
		relativeVel = Vector3.zero;
		if (IsOnGround() && (bool)(UnityEngine.Object)(object)m_lastGroundBody)
		{
			ZNetView component = ((Component)(object)m_lastGroundBody).GetComponent<ZNetView>();
			if ((bool)component && component.IsValid())
			{
				parent = component.GetZDO().m_uid;
				relativePos = component.transform.InverseTransformPoint(base.transform.position);
				relativeVel = component.transform.InverseTransformVector(m_body.get_velocity() - m_lastGroundBody.get_velocity());
				return true;
			}
		}
		parent = ZDOID.None;
		relativePos = Vector3.zero;
		return false;
	}

	public Quaternion GetLookYaw()
	{
		return m_lookYaw;
	}

	public Vector3 GetMoveDir()
	{
		return m_moveDir;
	}

	public BaseAI GetBaseAI()
	{
		return m_baseAI;
	}

	public float GetMass()
	{
		return m_body.get_mass();
	}

	protected void SetVisible(bool visible)
	{
		if (!(m_lodGroup == null) && m_lodVisible != visible)
		{
			m_lodVisible = visible;
			if (m_lodVisible)
			{
				m_lodGroup.localReferencePoint = m_originalLocalRef;
			}
			else
			{
				m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
			}
		}
	}

	public void SetTamed(bool tamed)
	{
		if (m_nview.IsValid() && m_tamed != tamed)
		{
			m_nview.InvokeRPC("SetTamed", tamed);
		}
	}

	private void RPC_SetTamed(long sender, bool tamed)
	{
		if (m_nview.IsOwner() && m_tamed != tamed)
		{
			m_tamed = tamed;
			m_nview.GetZDO().Set("tamed", m_tamed);
		}
	}

	public bool IsTamed()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (!m_nview.IsOwner() && Time.time - m_lastTamedCheck > 1f)
		{
			m_lastTamedCheck = Time.time;
			m_tamed = m_nview.GetZDO().GetBool("tamed", m_tamed);
		}
		return m_tamed;
	}

	public SEMan GetSEMan()
	{
		return m_seman;
	}

	public bool InInterior()
	{
		return base.transform.position.y > 3000f;
	}

	public static void SetDPSDebug(bool enabled)
	{
		m_dpsDebugEnabled = enabled;
	}

	public static bool IsDPSDebugEnabled()
	{
		return m_dpsDebugEnabled;
	}
}
