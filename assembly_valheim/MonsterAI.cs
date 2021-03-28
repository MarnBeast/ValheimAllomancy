using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : BaseAI
{
	public Action<ItemDrop> m_onConsumedItem;

	private const float m_giveUpTime = 15f;

	private const float m_bossGiveUpTime = 15f;

	private const float m_updateTargetFarRange = 32f;

	private const float m_updateTargetIntervalNear = 3f;

	private const float m_updateTargetIntervalFar = 10f;

	private const float m_updateWeaponInterval = 1f;

	[Header("Monster AI")]
	public float m_alertRange = 9999f;

	private const float m_alertOthersRange = 10f;

	public bool m_fleeIfHurtWhenTargetCantBeReached = true;

	public bool m_fleeIfNotAlerted;

	public float m_fleeIfLowHealth;

	public bool m_circulateWhileCharging;

	public bool m_circulateWhileChargingFlying;

	public bool m_enableHuntPlayer;

	public bool m_attackPlayerObjects = true;

	public bool m_attackPlayerObjectsWhenAlerted = true;

	public float m_interceptTimeMax;

	public float m_interceptTimeMin;

	public float m_maxChaseDistance;

	public float m_minAttackInterval;

	[Header("Circle target")]
	public float m_circleTargetInterval;

	public float m_circleTargetDuration = 5f;

	public float m_circleTargetDistance = 10f;

	[Header("Sleep")]
	public bool m_sleeping;

	public bool m_noiseWakeup;

	public float m_noiseRangeScale = 1f;

	public float m_wakeupRange = 5f;

	public EffectList m_wakeupEffects = new EffectList();

	[Header("Other")]
	public bool m_avoidLand;

	[Header("Consume items")]
	public List<ItemDrop> m_consumeItems;

	public float m_consumeRange = 2f;

	public float m_consumeSearchRange = 5f;

	public float m_consumeSearchInterval = 10f;

	public float m_consumeHeal;

	private ItemDrop m_consumeTarget;

	private float m_consumeSearchTimer;

	private static int m_itemMask;

	private string m_aiStatus = "";

	private bool m_despawnInDay;

	private bool m_eventCreature;

	private Character m_targetCreature;

	private bool m_havePathToTarget;

	private Vector3 m_lastKnownTargetPos = Vector3.zero;

	private bool m_beenAtLastPos;

	private StaticTarget m_targetStatic;

	private float m_timeSinceAttacking;

	private float m_timeSinceSensedTargetCreature;

	private float m_updateTargetTimer;

	private float m_updateWeaponTimer;

	private float m_lastAttackTime = -1000f;

	private float m_interceptTime;

	private float m_pauseTimer;

	private bool m_goingHome;

	private float m_sleepTimer;

	private GameObject m_follow;

	private Tameable m_tamable;

	protected override void Awake()
	{
		base.Awake();
		m_tamable = GetComponent<Tameable>();
		m_despawnInDay = m_nview.GetZDO().GetBool("DespawnInDay", m_despawnInDay);
		m_eventCreature = m_nview.GetZDO().GetBool("EventCreature", m_eventCreature);
		m_animator.SetBool("sleeping", IsSleeping());
		m_interceptTime = UnityEngine.Random.Range(m_interceptTimeMin, m_interceptTimeMax);
		m_pauseTimer = UnityEngine.Random.Range(0f, m_circleTargetInterval);
		m_updateTargetTimer = UnityEngine.Random.Range(0f, 3f);
		if (m_enableHuntPlayer)
		{
			SetHuntPlayer(hunt: true);
		}
	}

	private void Start()
	{
		if ((bool)m_nview && m_nview.IsValid() && m_nview.IsOwner())
		{
			Humanoid humanoid = m_character as Humanoid;
			if ((bool)humanoid)
			{
				humanoid.EquipBestWeapon(null, null, null, null);
			}
		}
	}

	protected override void OnDamaged(float damage, Character attacker)
	{
		base.OnDamaged(damage, attacker);
		SetAlerted(alert: true);
		if (attacker != null && m_targetCreature == null && (!attacker.IsPlayer() || !m_character.IsTamed()))
		{
			m_targetCreature = attacker;
			m_lastKnownTargetPos = attacker.transform.position;
			m_beenAtLastPos = false;
			m_havePathToTarget = HavePath(m_targetCreature.transform.position);
			m_targetStatic = null;
		}
	}

	public void MakeTame()
	{
		m_character.SetTamed(tamed: true);
		SetAlerted(alert: false);
		m_targetCreature = null;
		m_targetStatic = null;
	}

	private void UpdateTarget(Humanoid humanoid, float dt, out bool canHearTarget, out bool canSeeTarget)
	{
		m_updateTargetTimer -= dt;
		if (m_updateTargetTimer <= 0f && !m_character.InAttack())
		{
			m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 32f) ? 3f : 10f);
			Character character = FindEnemy();
			if ((bool)character)
			{
				m_targetCreature = character;
				m_targetStatic = null;
			}
			if (m_targetCreature != null)
			{
				m_havePathToTarget = HavePath(m_targetCreature.transform.position);
			}
			if (!m_character.IsTamed() && (m_attackPlayerObjects || (m_attackPlayerObjectsWhenAlerted && IsAlerted())) && (m_targetCreature == null || ((bool)m_targetCreature && !m_havePathToTarget)))
			{
				StaticTarget staticTarget = FindClosestStaticPriorityTarget(99999f);
				if ((bool)staticTarget)
				{
					m_targetStatic = staticTarget;
					m_targetCreature = null;
				}
				if (m_targetStatic != null)
				{
					m_havePathToTarget = HavePath(m_targetStatic.transform.position);
				}
				if ((!staticTarget || ((bool)m_targetStatic && !m_havePathToTarget)) && IsAlerted())
				{
					StaticTarget staticTarget2 = FindRandomStaticTarget(10f, priorityTargetsOnly: false);
					if ((bool)staticTarget2)
					{
						m_targetStatic = staticTarget2;
						m_targetCreature = null;
					}
				}
			}
		}
		if ((bool)m_targetCreature && m_character.IsTamed())
		{
			if (GetPatrolPoint(out var point))
			{
				if (Vector3.Distance(m_targetCreature.transform.position, point) > m_alertRange)
				{
					m_targetCreature = null;
				}
			}
			else if ((bool)m_follow && Vector3.Distance(m_targetCreature.transform.position, m_follow.transform.position) > m_alertRange)
			{
				m_targetCreature = null;
			}
		}
		if ((bool)m_targetCreature && m_targetCreature.IsDead())
		{
			m_targetCreature = null;
		}
		canHearTarget = false;
		canSeeTarget = false;
		if ((bool)m_targetCreature)
		{
			canHearTarget = CanHearTarget(m_targetCreature);
			canSeeTarget = CanSeeTarget(m_targetCreature);
			if (canSeeTarget | canHearTarget)
			{
				m_timeSinceSensedTargetCreature = 0f;
			}
			if (m_targetCreature.IsPlayer())
			{
				m_targetCreature.OnTargeted(canSeeTarget | canHearTarget, IsAlerted());
			}
			SetTargetInfo(m_targetCreature.GetZDOID());
		}
		else
		{
			SetTargetInfo(ZDOID.None);
		}
		m_timeSinceSensedTargetCreature += dt;
		if (IsAlerted() || m_targetCreature != null)
		{
			m_timeSinceAttacking += dt;
			float num = (m_character.IsBoss() ? 15f : 15f);
			float num2 = num * 2f;
			float num3 = Vector3.Distance(m_spawnPoint, base.transform.position);
			bool flag = HuntPlayer() && (bool)m_targetCreature && m_targetCreature.IsPlayer();
			if (m_timeSinceSensedTargetCreature > num || (!flag && (m_timeSinceAttacking > num2 || (m_maxChaseDistance > 0f && m_timeSinceSensedTargetCreature > 1f && num3 > m_maxChaseDistance))))
			{
				SetAlerted(alert: false);
				m_targetCreature = null;
				m_targetStatic = null;
				m_timeSinceAttacking = 0f;
				m_updateTargetTimer = 5f;
			}
		}
	}

	protected override void UpdateAI(float dt)
	{
		base.UpdateAI(dt);
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (IsSleeping())
		{
			UpdateSleep(dt);
			return;
		}
		m_aiStatus = "";
		Humanoid humanoid = m_character as Humanoid;
		UpdateTarget(humanoid, dt, out var canHearTarget, out var canSeeTarget);
		if (m_avoidLand && !m_character.IsSwiming())
		{
			m_aiStatus = "Move to water";
			MoveToWater(dt, 20f);
			return;
		}
		if (((m_despawnInDay && EnvMan.instance.IsDay()) || (m_eventCreature && !RandEventSystem.HaveActiveEvent())) && (m_targetCreature == null || !canSeeTarget))
		{
			MoveAwayAndDespawn(dt, run: true);
			m_aiStatus = "Trying to despawn ";
			return;
		}
		if (m_fleeIfNotAlerted && !HuntPlayer() && (bool)m_targetCreature && !IsAlerted() && Vector3.Distance(m_targetCreature.transform.position, base.transform.position) - m_targetCreature.GetRadius() > m_alertRange)
		{
			Flee(dt, m_targetCreature.transform.position);
			m_aiStatus = "Avoiding conflict";
			return;
		}
		if (m_fleeIfLowHealth > 0f && m_character.GetHealthPercentage() < m_fleeIfLowHealth && m_timeSinceHurt < 20f && m_targetCreature != null)
		{
			Flee(dt, m_targetCreature.transform.position);
			m_aiStatus = "Low health, flee";
			return;
		}
		if ((m_afraidOfFire || m_avoidFire) && AvoidFire(dt, m_targetCreature, m_afraidOfFire))
		{
			if (m_afraidOfFire)
			{
				m_targetStatic = null;
				m_targetCreature = null;
			}
			m_aiStatus = "Avoiding fire";
			return;
		}
		if (m_circleTargetInterval > 0f && (bool)m_targetCreature)
		{
			if ((bool)m_targetCreature)
			{
				m_pauseTimer += dt;
				if (m_pauseTimer > m_circleTargetInterval)
				{
					if (m_pauseTimer > m_circleTargetInterval + m_circleTargetDuration)
					{
						m_pauseTimer = 0f;
					}
					RandomMovementArroundPoint(dt, m_targetCreature.transform.position, m_circleTargetDistance, IsAlerted());
					m_aiStatus = "Attack pause";
					return;
				}
			}
			else
			{
				m_pauseTimer = 0f;
			}
		}
		if (m_targetCreature != null)
		{
			if ((bool)EffectArea.IsPointInsideArea(m_targetCreature.transform.position, EffectArea.Type.NoMonsters))
			{
				Flee(dt, m_targetCreature.transform.position);
				m_aiStatus = "Avoid no-monster area";
				return;
			}
		}
		else
		{
			EffectArea effectArea = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.NoMonsters, 15f);
			if (effectArea != null)
			{
				Flee(dt, effectArea.transform.position);
				m_aiStatus = "Avoid no-monster area";
				return;
			}
		}
		if (m_fleeIfHurtWhenTargetCantBeReached && m_targetCreature != null && !m_havePathToTarget && m_timeSinceHurt < 20f)
		{
			m_aiStatus = "Hide from unreachable target";
			Flee(dt, m_targetCreature.transform.position);
			return;
		}
		if ((!IsAlerted() || (m_targetStatic == null && m_targetCreature == null)) && UpdateConsumeItem(humanoid, dt))
		{
			m_aiStatus = "Consume item";
			return;
		}
		ItemDrop.ItemData itemData = SelectBestAttack(humanoid, dt);
		bool flag = itemData != null && Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval && Time.time - m_lastAttackTime > m_minAttackInterval && !IsTakingOff();
		if ((m_character.IsFlying() ? m_circulateWhileChargingFlying : m_circulateWhileCharging) && (m_targetStatic != null || m_targetCreature != null) && itemData != null && !flag && !m_character.InAttack())
		{
			m_aiStatus = "Move around target weapon ready:" + flag;
			if (itemData != null)
			{
				m_aiStatus = m_aiStatus + " Weapon:" + itemData.m_shared.m_name;
			}
			Vector3 point = (m_targetCreature ? m_targetCreature.transform.position : m_targetStatic.transform.position);
			RandomMovementArroundPoint(dt, point, m_randomMoveRange, IsAlerted());
		}
		else if ((m_targetStatic == null && m_targetCreature == null) || itemData == null)
		{
			if ((bool)m_follow)
			{
				Follow(m_follow, dt);
				m_aiStatus = "Follow";
				return;
			}
			m_aiStatus = string.Concat("Random movement (weapon: ", (itemData != null) ? itemData.m_shared.m_name : "none", ") (targetpiece: ", m_targetStatic, ") (target: ", m_targetCreature ? m_targetCreature.gameObject.name : "none", ")");
			IdleMovement(dt);
		}
		else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
		{
			if ((bool)m_targetStatic)
			{
				Vector3 vector = m_targetStatic.FindClosestPoint(base.transform.position);
				if (Vector3.Distance(vector, base.transform.position) < itemData.m_shared.m_aiAttackRange && CanSeeTarget(m_targetStatic))
				{
					LookAt(m_targetStatic.GetCenter());
					if (IsLookingAt(m_targetStatic.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle) && flag)
					{
						m_aiStatus = "Attacking piece";
						DoAttack(null, isFriend: false);
					}
					else
					{
						StopMoving();
					}
				}
				else
				{
					m_aiStatus = "Move to static target";
					MoveTo(dt, vector, 0f, IsAlerted());
				}
			}
			else
			{
				if (!m_targetCreature)
				{
					return;
				}
				if (canHearTarget || canSeeTarget || (HuntPlayer() && m_targetCreature.IsPlayer()))
				{
					m_beenAtLastPos = false;
					m_lastKnownTargetPos = m_targetCreature.transform.position;
					float num = Vector3.Distance(m_lastKnownTargetPos, base.transform.position) - m_targetCreature.GetRadius();
					float num2 = m_alertRange * m_targetCreature.GetStealthFactor();
					if ((canSeeTarget && num < num2) || HuntPlayer())
					{
						SetAlerted(alert: true);
					}
					bool flag2 = num < itemData.m_shared.m_aiAttackRange;
					if (!flag2 || !canSeeTarget || itemData.m_shared.m_aiAttackRangeMin < 0f || !IsAlerted())
					{
						m_aiStatus = "Move closer";
						Vector3 velocity = m_targetCreature.GetVelocity();
						Vector3 vector2 = velocity * m_interceptTime;
						Vector3 lastKnownTargetPos = m_lastKnownTargetPos;
						if (num > vector2.magnitude / 4f)
						{
							lastKnownTargetPos += velocity * m_interceptTime;
						}
						if (MoveTo(dt, lastKnownTargetPos, 0f, IsAlerted()))
						{
							flag2 = true;
						}
					}
					else
					{
						StopMoving();
					}
					if (flag2 && canSeeTarget && IsAlerted())
					{
						m_aiStatus = "In attack range";
						LookAt(m_targetCreature.GetTopPoint());
						if (flag && IsLookingAt(m_lastKnownTargetPos, itemData.m_shared.m_aiAttackMaxAngle))
						{
							m_aiStatus = "Attacking creature";
							DoAttack(m_targetCreature, isFriend: false);
						}
					}
				}
				else
				{
					m_aiStatus = "Searching for target";
					if (m_beenAtLastPos)
					{
						RandomMovement(dt, m_lastKnownTargetPos);
					}
					else if (MoveTo(dt, m_lastKnownTargetPos, 0f, IsAlerted()))
					{
						m_beenAtLastPos = true;
					}
				}
			}
		}
		else
		{
			if (itemData.m_shared.m_aiTargetType != ItemDrop.ItemData.AiTarget.FriendHurt && itemData.m_shared.m_aiTargetType != ItemDrop.ItemData.AiTarget.Friend)
			{
				return;
			}
			m_aiStatus = "Helping friend";
			Character character = ((itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt) ? HaveHurtFriendInRange(m_viewRange) : HaveFriendInRange(m_viewRange));
			if ((bool)character)
			{
				if (Vector3.Distance(character.transform.position, base.transform.position) < itemData.m_shared.m_aiAttackRange)
				{
					if (flag)
					{
						StopMoving();
						LookAt(character.transform.position);
						DoAttack(character, isFriend: true);
					}
					else
					{
						RandomMovement(dt, character.transform.position);
					}
				}
				else
				{
					MoveTo(dt, character.transform.position, 0f, IsAlerted());
				}
			}
			else
			{
				RandomMovement(dt, base.transform.position);
			}
		}
	}

	private bool UpdateConsumeItem(Humanoid humanoid, float dt)
	{
		if (m_consumeItems == null || m_consumeItems.Count == 0)
		{
			return false;
		}
		m_consumeSearchTimer += dt;
		if (m_consumeSearchTimer > m_consumeSearchInterval)
		{
			m_consumeSearchTimer = 0f;
			if ((bool)m_tamable && !m_tamable.IsHungry())
			{
				return false;
			}
			m_consumeTarget = FindClosestConsumableItem(m_consumeSearchRange);
		}
		if ((bool)m_consumeTarget)
		{
			if (MoveTo(dt, m_consumeTarget.transform.position, m_consumeRange, run: false))
			{
				LookAt(m_consumeTarget.transform.position);
				if (IsLookingAt(m_consumeTarget.transform.position, 20f) && m_consumeTarget.RemoveOne())
				{
					if (m_onConsumedItem != null)
					{
						m_onConsumedItem(m_consumeTarget);
					}
					humanoid.m_consumeItemEffects.Create(base.transform.position, Quaternion.identity);
					m_animator.SetTrigger("consume");
					m_consumeTarget = null;
					if (m_consumeHeal > 0f)
					{
						m_character.Heal(m_consumeHeal);
					}
				}
			}
			return true;
		}
		return false;
	}

	private ItemDrop FindClosestConsumableItem(float maxRange)
	{
		if (m_itemMask == 0)
		{
			m_itemMask = LayerMask.GetMask("item");
		}
		Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, m_itemMask);
		ItemDrop itemDrop = null;
		float num = 999999f;
		Collider[] array2 = array;
		foreach (Collider val in array2)
		{
			if (!(UnityEngine.Object)(object)val.get_attachedRigidbody())
			{
				continue;
			}
			ItemDrop component = ((Component)(object)val.get_attachedRigidbody()).GetComponent<ItemDrop>();
			if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && CanConsume(component.m_itemData))
			{
				float num2 = Vector3.Distance(component.transform.position, base.transform.position);
				if (itemDrop == null || num2 < num)
				{
					itemDrop = component;
					num = num2;
				}
			}
		}
		if ((bool)itemDrop && HavePath(itemDrop.transform.position))
		{
			return itemDrop;
		}
		return null;
	}

	private bool CanConsume(ItemDrop.ItemData item)
	{
		foreach (ItemDrop consumeItem in m_consumeItems)
		{
			if (consumeItem.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				return true;
			}
		}
		return false;
	}

	private ItemDrop.ItemData SelectBestAttack(Humanoid humanoid, float dt)
	{
		if ((bool)m_targetCreature || (bool)m_targetStatic)
		{
			m_updateWeaponTimer -= dt;
			if (m_updateWeaponTimer <= 0f && !m_character.InAttack())
			{
				m_updateWeaponTimer = 1f;
				HaveFriendsInRange(m_viewRange, out var hurtFriend, out var friend);
				humanoid.EquipBestWeapon(m_targetCreature, m_targetStatic, hurtFriend, friend);
			}
		}
		return humanoid.GetCurrentWeapon();
	}

	private bool DoAttack(Character target, bool isFriend)
	{
		ItemDrop.ItemData currentWeapon = (m_character as Humanoid).GetCurrentWeapon();
		if (currentWeapon != null)
		{
			if (!BaseAI.CanUseAttack(m_character, currentWeapon))
			{
				return false;
			}
			bool num = m_character.StartAttack(target, charge: false);
			if (num)
			{
				m_timeSinceAttacking = 0f;
				m_lastAttackTime = Time.time;
			}
			return num;
		}
		return false;
	}

	public void SetDespawnInDay(bool despawn)
	{
		m_despawnInDay = despawn;
		m_nview.GetZDO().Set("DespawnInDay", despawn);
	}

	public void SetEventCreature(bool despawn)
	{
		m_eventCreature = despawn;
		m_nview.GetZDO().Set("EventCreature", despawn);
	}

	public bool IsEventCreature()
	{
		return m_eventCreature;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
	}

	public override Character GetTargetCreature()
	{
		return m_targetCreature;
	}

	private void UpdateSleep(float dt)
	{
		if (!IsSleeping())
		{
			return;
		}
		m_sleepTimer += dt;
		if (m_sleepTimer < 0.5f)
		{
			return;
		}
		if (HuntPlayer())
		{
			Wakeup();
			return;
		}
		if (m_wakeupRange > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, m_wakeupRange);
			if ((bool)closestPlayer && !closestPlayer.InGhostMode() && !closestPlayer.IsDebugFlying())
			{
				Wakeup();
				return;
			}
		}
		if (m_noiseWakeup)
		{
			Player playerNoiseRange = Player.GetPlayerNoiseRange(base.transform.position, m_noiseRangeScale);
			if ((bool)playerNoiseRange && !playerNoiseRange.InGhostMode() && !playerNoiseRange.IsDebugFlying())
			{
				Wakeup();
			}
		}
	}

	private void Wakeup()
	{
		if (IsSleeping())
		{
			m_animator.SetBool("sleeping", value: false);
			m_nview.GetZDO().Set("sleeping", value: false);
			m_wakeupEffects.Create(base.transform.position, base.transform.rotation);
		}
	}

	public override bool IsSleeping()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetBool("sleeping", m_sleeping);
	}

	protected override void SetAlerted(bool alert)
	{
		if (alert)
		{
			m_timeSinceSensedTargetCreature = 0f;
		}
		base.SetAlerted(alert);
	}

	public override bool HuntPlayer()
	{
		if (base.HuntPlayer())
		{
			if (m_eventCreature && !RandEventSystem.InEvent())
			{
				return false;
			}
			if (m_despawnInDay && EnvMan.instance.IsDay())
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public GameObject GetFollowTarget()
	{
		return m_follow;
	}

	public void SetFollowTarget(GameObject go)
	{
		m_follow = go;
	}
}
