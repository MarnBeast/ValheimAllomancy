using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class Player : Humanoid
{
	public enum RequirementMode
	{
		CanBuild,
		IsKnown,
		CanAlmostBuild
	}

	public class Food
	{
		public string m_name = "";

		public ItemDrop.ItemData m_item;

		public float m_health;

		public float m_stamina;

		public bool CanEatAgain()
		{
			return m_health < m_item.m_shared.m_food / 2f;
		}
	}

	public class EquipQueueData
	{
		public ItemDrop.ItemData m_item;

		public bool m_equip = true;

		public float m_time;

		public float m_duration;
	}

	private enum PlacementStatus
	{
		Valid,
		Invalid,
		BlockedbyPlayer,
		NoBuildZone,
		PrivateZone,
		MoreSpace,
		NoTeleportArea,
		ExtensionMissingStation,
		WrongBiome,
		NeedCultivated,
		NotInDungeon
	}

	private float m_rotatePieceTimer;

	private float m_baseValueUpdatetimer;

	private const int dataVersion = 24;

	private float m_equipQueuePause;

	public static Player m_localPlayer = null;

	private static List<Player> m_players = new List<Player>();

	public static bool m_debugMode = false;

	[Header("Player")]
	public float m_maxPlaceDistance = 5f;

	public float m_maxInteractDistance = 5f;

	public float m_scrollSens = 4f;

	public float m_staminaRegen = 5f;

	public float m_staminaRegenTimeMultiplier = 1f;

	public float m_staminaRegenDelay = 1f;

	public float m_runStaminaDrain = 10f;

	public float m_sneakStaminaDrain = 5f;

	public float m_swimStaminaDrainMinSkill = 5f;

	public float m_swimStaminaDrainMaxSkill = 2f;

	public float m_dodgeStaminaUsage = 10f;

	public float m_weightStaminaFactor = 0.1f;

	public float m_autoPickupRange = 2f;

	public float m_maxCarryWeight = 300f;

	public float m_encumberedStaminaDrain = 10f;

	public float m_hardDeathCooldown = 10f;

	public float m_baseCameraShake = 4f;

	public EffectList m_drownEffects = new EffectList();

	public EffectList m_spawnEffects = new EffectList();

	public EffectList m_removeEffects = new EffectList();

	public EffectList m_dodgeEffects = new EffectList();

	public EffectList m_autopickupEffects = new EffectList();

	public EffectList m_skillLevelupEffects = new EffectList();

	public EffectList m_equipStartEffects = new EffectList();

	public GameObject m_placeMarker;

	public GameObject m_tombstone;

	public GameObject m_valkyrie;

	public Sprite m_textIcon;

	private Skills m_skills;

	private PieceTable m_buildPieces;

	private bool m_noPlacementCost;

	private bool m_hideUnavailable;

	private HashSet<string> m_knownRecipes = new HashSet<string>();

	private Dictionary<string, int> m_knownStations = new Dictionary<string, int>();

	private HashSet<string> m_knownMaterial = new HashSet<string>();

	private HashSet<string> m_shownTutorials = new HashSet<string>();

	private HashSet<string> m_uniques = new HashSet<string>();

	private HashSet<string> m_trophies = new HashSet<string>();

	private HashSet<Heightmap.Biome> m_knownBiome = new HashSet<Heightmap.Biome>();

	private Dictionary<string, string> m_knownTexts = new Dictionary<string, string>();

	private float m_stationDiscoverTimer;

	private bool m_debugFly;

	private bool m_godMode;

	private bool m_ghostMode;

	private float m_lookPitch;

	private const float m_baseHP = 25f;

	private const float m_baseStamina = 75f;

	private const int m_maxFoods = 3;

	private const float m_foodDrainPerSec = 0.1f;

	private float m_foodUpdateTimer;

	private float m_foodRegenTimer;

	private List<Food> m_foods = new List<Food>();

	private float m_stamina = 100f;

	private float m_maxStamina = 100f;

	private float m_staminaRegenTimer;

	private string m_guardianPower = "";

	private float m_guardianPowerCooldown;

	private StatusEffect m_guardianSE;

	private GameObject m_placementMarkerInstance;

	private GameObject m_placementGhost;

	private PlacementStatus m_placementStatus = PlacementStatus.Invalid;

	private int m_placeRotation;

	private int m_placeRayMask;

	private int m_placeGroundRayMask;

	private int m_placeWaterRayMask;

	private int m_removeRayMask;

	private int m_interactMask;

	private int m_autoPickupMask;

	private List<EquipQueueData> m_equipQueue = new List<EquipQueueData>();

	private GameObject m_hovering;

	private Character m_hoveringCreature;

	private float m_lastHoverInteractTime;

	private bool m_pvp;

	private float m_updateCoverTimer;

	private float m_coverPercentage;

	private bool m_underRoof = true;

	private float m_nearFireTimer;

	private bool m_isLoading;

	private float m_queuedAttackTimer;

	private float m_queuedSecondAttackTimer;

	private float m_queuedDodgeTimer;

	private Vector3 m_queuedDodgeDir = Vector3.zero;

	private bool m_inDodge;

	private bool m_dodgeInvincible;

	private CraftingStation m_currentStation;

	private Ragdoll m_ragdoll;

	private Piece m_hoveringPiece;

	private string m_emoteState = "";

	private int m_emoteID;

	private bool m_intro;

	private bool m_firstSpawn = true;

	private bool m_crouchToggled;

	private bool m_autoRun;

	private bool m_safeInHome;

	private ShipControlls m_shipControl;

	private bool m_attached;

	private string m_attachAnimation = "";

	private bool m_sleeping;

	private Transform m_attachPoint;

	private Vector3 m_detachOffset = Vector3.zero;

	private int m_modelIndex;

	private Vector3 m_skinColor = Vector3.one;

	private Vector3 m_hairColor = Vector3.one;

	private bool m_teleporting;

	private bool m_distantTeleport;

	private float m_teleportTimer;

	private float m_teleportCooldown;

	private Vector3 m_teleportFromPos;

	private Quaternion m_teleportFromRot;

	private Vector3 m_teleportTargetPos;

	private Quaternion m_teleportTargetRot;

	private Heightmap.Biome m_currentBiome;

	private float m_biomeTimer;

	private int m_baseValue;

	private int m_comfortLevel;

	private float m_drownDamageTimer;

	private float m_timeSinceTargeted;

	private float m_timeSinceSensed;

	private float m_stealthFactorUpdateTimer;

	private float m_stealthFactor;

	private float m_stealthFactorTarget;

	private Vector3 m_lastStealthPosition = Vector3.zero;

	private float m_wakeupTimer = -1f;

	private float m_timeSinceDeath = 999999f;

	private float m_runSkillImproveTimer;

	private float m_swimSkillImproveTimer;

	private float m_sneakSkillImproveTimer;

	private float m_equipmentMovementModifier;

	private static int crouching = 0;

	protected static int m_attackMask = 0;

	protected static int m_animatorTagDodge = Animator.StringToHash("dodge");

	protected static int m_animatorTagCutscene = Animator.StringToHash("cutscene");

	protected static int m_animatorTagCrouch = Animator.StringToHash("crouch");

	protected static int m_animatorTagMinorAction = Animator.StringToHash("minoraction");

	protected static int m_animatorTagEmote = Animator.StringToHash("emote");

	private List<PieceTable> m_tempOwnedPieceTables = new List<PieceTable>();

	private List<Transform> m_tempSnapPoints1 = new List<Transform>();

	private List<Transform> m_tempSnapPoints2 = new List<Transform>();

	private List<Piece> m_tempPieces = new List<Piece>();

	protected override void Awake()
	{
		base.Awake();
		m_players.Add(this);
		m_skills = GetComponent<Skills>();
		SetupAwake();
		if (m_nview.GetZDO() == null)
		{
			return;
		}
		m_placeRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
		m_placeWaterRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "Water", "vehicle");
		m_removeRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
		m_interactMask = LayerMask.GetMask("item", "piece", "piece_nonsolid", "Default", "static_solid", "Default_small", "character", "character_net", "terrain", "vehicle");
		m_autoPickupMask = LayerMask.GetMask("item");
		Inventory inventory = m_inventory;
		inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(OnInventoryChanged));
		if (m_attackMask == 0)
		{
			m_attackMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
		}
		if (crouching == 0)
		{
			crouching = ZSyncAnimation.GetHash("crouching");
		}
		m_nview.Register("OnDeath", RPC_OnDeath);
		if (m_nview.IsOwner())
		{
			m_nview.Register<int, string, int>("Message", RPC_Message);
			m_nview.Register<bool, bool>("OnTargeted", RPC_OnTargeted);
			m_nview.Register<float>("UseStamina", RPC_UseStamina);
			if ((bool)MusicMan.instance)
			{
				MusicMan.instance.TriggerMusic("Wakeup");
			}
			UpdateKnownRecipesList();
			UpdateAvailablePiecesList();
			SetupPlacementGhost();
		}
	}

	public void SetLocalPlayer()
	{
		if (!(m_localPlayer == this))
		{
			m_localPlayer = this;
			ZNet.instance.SetReferencePosition(base.transform.position);
			EnvMan.instance.SetForceEnvironment("");
		}
	}

	public void SetPlayerID(long playerID, string name)
	{
		if (m_nview.GetZDO() != null && GetPlayerID() == 0L)
		{
			m_nview.GetZDO().Set("playerID", playerID);
			m_nview.GetZDO().Set("playerName", name);
		}
	}

	public long GetPlayerID()
	{
		if (m_nview.IsValid())
		{
			return m_nview.GetZDO().GetLong("playerID", 0L);
		}
		return 0L;
	}

	public string GetPlayerName()
	{
		if (m_nview.IsValid())
		{
			return m_nview.GetZDO().GetString("playerName", "...");
		}
		return "";
	}

	public override string GetHoverText()
	{
		return "";
	}

	public override string GetHoverName()
	{
		return GetPlayerName();
	}

	protected override void Start()
	{
		base.Start();
		m_nview.GetZDO();
	}

	public override void OnDestroy()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		ZDO zDO = m_nview.GetZDO();
		if (zDO != null && ZNet.instance != null)
		{
			ZLog.LogWarning((object)string.Concat("Player destroyed sec:", zDO.GetSector(), "  pos:", base.transform.position, "  zdopos:", zDO.GetPosition(), "  ref ", ZNet.instance.GetReferencePosition()));
		}
		if ((bool)m_placementGhost)
		{
			UnityEngine.Object.Destroy(m_placementGhost);
			m_placementGhost = null;
		}
		base.OnDestroy();
		m_players.Remove(this);
		if (m_localPlayer == this)
		{
			ZLog.LogWarning((object)"Local player destroyed");
			m_localPlayer = null;
		}
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		float fixedDeltaTime = Time.fixedDeltaTime;
		UpdateAwake(fixedDeltaTime);
		if (m_nview.GetZDO() == null)
		{
			return;
		}
		UpdateTargeted(fixedDeltaTime);
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (m_localPlayer != this)
		{
			ZLog.Log((object)"Destroying old local player");
			ZNetScene.instance.Destroy(base.gameObject);
		}
		else if (!IsDead())
		{
			UpdateEquipQueue(fixedDeltaTime);
			PlayerAttackInput(fixedDeltaTime);
			UpdateAttach();
			UpdateShipControl(fixedDeltaTime);
			UpdateCrouch(fixedDeltaTime);
			UpdateDodge(fixedDeltaTime);
			UpdateCover(fixedDeltaTime);
			UpdateStations(fixedDeltaTime);
			UpdateGuardianPower(fixedDeltaTime);
			UpdateBaseValue(fixedDeltaTime);
			UpdateStats(fixedDeltaTime);
			UpdateTeleport(fixedDeltaTime);
			AutoPickup(fixedDeltaTime);
			EdgeOfWorldKill(fixedDeltaTime);
			UpdateBiome(fixedDeltaTime);
			UpdateStealth(fixedDeltaTime);
			if ((bool)GameCamera.instance && Vector3.Distance(GameCamera.instance.transform.position, base.transform.position) < 2f)
			{
				SetVisible(visible: false);
			}
			AudioMan.instance.SetIndoor(InShelter());
		}
	}

	private void Update()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		bool flag = TakeInput();
		UpdateHover();
		if (flag)
		{
			if (m_debugMode && Console.instance.IsCheatsEnabled())
			{
				if (Input.GetKeyDown(KeyCode.Z))
				{
					m_debugFly = !m_debugFly;
					m_nview.GetZDO().Set("DebugFly", m_debugFly);
					Message(MessageHud.MessageType.TopLeft, "Debug fly:" + m_debugFly);
				}
				if (Input.GetKeyDown(KeyCode.B))
				{
					m_noPlacementCost = !m_noPlacementCost;
					Message(MessageHud.MessageType.TopLeft, "No placement cost:" + m_noPlacementCost);
					UpdateAvailablePiecesList();
				}
				if (Input.GetKeyDown(KeyCode.K))
				{
					int num = 0;
					foreach (Character allCharacter in Character.GetAllCharacters())
					{
						if (!allCharacter.IsPlayer())
						{
							HitData hitData = new HitData();
							hitData.m_damage.m_damage = 99999f;
							allCharacter.Damage(hitData);
							num++;
						}
					}
					Message(MessageHud.MessageType.TopLeft, "Killing all the monsters:" + num);
				}
			}
			if (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse"))
			{
				if ((bool)m_hovering)
				{
					Interact(m_hovering, hold: false);
				}
				else if ((bool)m_shipControl)
				{
					StopShipControl();
				}
			}
			else if ((ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")) && (bool)m_hovering)
			{
				Interact(m_hovering, hold: true);
			}
			if (ZInput.GetButtonDown("Hide") || ZInput.GetButtonDown("JoyHide"))
			{
				if (GetRightItem() != null || GetLeftItem() != null)
				{
					if (!InAttack())
					{
						HideHandItems();
					}
				}
				else if (!IsSwiming() || IsOnGround())
				{
					ShowHandItems();
				}
			}
			if (ZInput.GetButtonDown("ToggleWalk"))
			{
				SetWalk(!GetWalk());
				if (GetWalk())
				{
					Message(MessageHud.MessageType.TopLeft, "$msg_walk 1");
				}
				else
				{
					Message(MessageHud.MessageType.TopLeft, "$msg_walk 0");
				}
			}
			if (ZInput.GetButtonDown("Sit") || (!InPlaceMode() && ZInput.GetButtonDown("JoySit")))
			{
				if (InEmote() && IsSitting())
				{
					StopEmote();
				}
				else
				{
					StartEmote("sit", oneshot: false);
				}
			}
			if (ZInput.GetButtonDown("GPower") || ZInput.GetButtonDown("JoyGPower"))
			{
				StartGuardianPower();
			}
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				UseHotbarItem(1);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				UseHotbarItem(2);
			}
			if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				UseHotbarItem(3);
			}
			if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				UseHotbarItem(4);
			}
			if (Input.GetKeyDown(KeyCode.Alpha5))
			{
				UseHotbarItem(5);
			}
			if (Input.GetKeyDown(KeyCode.Alpha6))
			{
				UseHotbarItem(6);
			}
			if (Input.GetKeyDown(KeyCode.Alpha7))
			{
				UseHotbarItem(7);
			}
			if (Input.GetKeyDown(KeyCode.Alpha8))
			{
				UseHotbarItem(8);
			}
		}
		UpdatePlacement(flag, Time.deltaTime);
	}

	private void UpdatePlacement(bool takeInput, float dt)
	{
		UpdateWearNTearHover();
		if (InPlaceMode())
		{
			if (!takeInput)
			{
				return;
			}
			UpdateBuildGuiInput();
			if (Hud.IsPieceSelectionVisible())
			{
				return;
			}
			ItemDrop.ItemData rightItem = GetRightItem();
			if ((ZInput.GetButtonDown("Remove") || ZInput.GetButtonDown("JoyRemove")) && rightItem.m_shared.m_buildPieces.m_canRemovePieces)
			{
				if (HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
				{
					if (RemovePiece())
					{
						AddNoise(50f);
						UseStamina(rightItem.m_shared.m_attack.m_attackStamina);
						if (rightItem.m_shared.m_useDurability)
						{
							rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
						}
					}
				}
				else
				{
					Hud.instance.StaminaBarNoStaminaFlash();
				}
			}
			if (ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace"))
			{
				Piece selectedPiece = m_buildPieces.GetSelectedPiece();
				if (selectedPiece != null)
				{
					if (HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
					{
						if (selectedPiece.m_repairPiece)
						{
							Repair(rightItem, selectedPiece);
						}
						else if (m_placementGhost != null)
						{
							if (m_noPlacementCost || HaveRequirements(selectedPiece, RequirementMode.CanBuild))
							{
								if (PlacePiece(selectedPiece))
								{
									ConsumeResources(selectedPiece.m_resources, 0);
									UseStamina(rightItem.m_shared.m_attack.m_attackStamina);
									if (rightItem.m_shared.m_useDurability)
									{
										rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
									}
								}
							}
							else
							{
								Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
							}
						}
					}
					else
					{
						Hud.instance.StaminaBarNoStaminaFlash();
					}
				}
			}
			if (Input.GetAxis("Mouse ScrollWheel") < 0f)
			{
				m_placeRotation--;
			}
			if (Input.GetAxis("Mouse ScrollWheel") > 0f)
			{
				m_placeRotation++;
			}
			float joyRightStickX = ZInput.GetJoyRightStickX();
			if (ZInput.GetButton("JoyRotate") && Mathf.Abs(joyRightStickX) > 0.5f)
			{
				if (m_rotatePieceTimer == 0f)
				{
					if (joyRightStickX < 0f)
					{
						m_placeRotation++;
					}
					else
					{
						m_placeRotation--;
					}
				}
				else if (m_rotatePieceTimer > 0.25f)
				{
					if (joyRightStickX < 0f)
					{
						m_placeRotation++;
					}
					else
					{
						m_placeRotation--;
					}
					m_rotatePieceTimer = 0.17f;
				}
				m_rotatePieceTimer += dt;
			}
			else
			{
				m_rotatePieceTimer = 0f;
			}
		}
		else if ((bool)m_placementGhost)
		{
			m_placementGhost.SetActive(value: false);
		}
	}

	private void UpdateBuildGuiInput()
	{
		if (Hud.instance.IsQuickPieceSelectEnabled())
		{
			if (!Hud.IsPieceSelectionVisible() && ZInput.GetButtonDown("BuildMenu"))
			{
				Hud.instance.TogglePieceSelection();
			}
		}
		else if (ZInput.GetButtonDown("BuildMenu"))
		{
			Hud.instance.TogglePieceSelection();
		}
		if (ZInput.GetButtonDown("JoyUse"))
		{
			Hud.instance.TogglePieceSelection();
		}
		if (Hud.IsPieceSelectionVisible())
		{
			if (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
			{
				Hud.HidePieceSelection();
			}
			if (ZInput.GetButtonDown("JoyTabLeft") || ZInput.GetButtonDown("BuildPrev") || Input.GetAxis("Mouse ScrollWheel") > 0f)
			{
				m_buildPieces.PrevCategory();
				UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyTabRight") || ZInput.GetButtonDown("BuildNext") || Input.GetAxis("Mouse ScrollWheel") < 0f)
			{
				m_buildPieces.NextCategory();
				UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyLStickLeft"))
			{
				m_buildPieces.LeftPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickRight"))
			{
				m_buildPieces.RightPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				m_buildPieces.UpPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				m_buildPieces.DownPiece();
				SetupPlacementGhost();
			}
		}
	}

	public void SetSelectedPiece(Vector2Int p)
	{
		if ((bool)m_buildPieces && m_buildPieces.GetSelectedIndex() != p)
		{
			m_buildPieces.SetSelected(p);
			SetupPlacementGhost();
		}
	}

	public Piece GetPiece(Vector2Int p)
	{
		if ((bool)m_buildPieces)
		{
			return m_buildPieces.GetPiece(p);
		}
		return null;
	}

	public bool IsPieceAvailable(Piece piece)
	{
		if ((bool)m_buildPieces)
		{
			return m_buildPieces.IsPieceAvailable(piece);
		}
		return false;
	}

	public Piece GetSelectedPiece()
	{
		if ((bool)m_buildPieces)
		{
			return m_buildPieces.GetSelectedPiece();
		}
		return null;
	}

	private void LateUpdate()
	{
		if (m_nview.IsValid())
		{
			UpdateEmote();
			if (m_nview.IsOwner())
			{
				ZNet.instance.SetReferencePosition(base.transform.position);
				UpdatePlacementGhost(flashGuardStone: false);
			}
		}
	}

	private void SetupAwake()
	{
		if (m_nview.GetZDO() == null)
		{
			m_animator.SetBool("wakeup", false);
			return;
		}
		bool @bool = m_nview.GetZDO().GetBool("wakeup", defaultValue: true);
		m_animator.SetBool("wakeup", @bool);
		if (@bool)
		{
			m_wakeupTimer = 0f;
		}
	}

	private void UpdateAwake(float dt)
	{
		if (!(m_wakeupTimer >= 0f))
		{
			return;
		}
		m_wakeupTimer += dt;
		if (m_wakeupTimer > 1f)
		{
			m_wakeupTimer = -1f;
			m_animator.SetBool("wakeup", false);
			if (m_nview.IsOwner())
			{
				m_nview.GetZDO().Set("wakeup", value: false);
			}
		}
	}

	private void EdgeOfWorldKill(float dt)
	{
		if (!IsDead())
		{
			float magnitude = base.transform.position.magnitude;
			float num = 10420f;
			if (magnitude > num && (IsSwiming() || base.transform.position.y < ZoneSystem.instance.m_waterLevel))
			{
				Vector3 a = Vector3.Normalize(base.transform.position);
				float d = Utils.LerpStep(num, 10500f, magnitude) * 10f;
				m_body.MovePosition(m_body.get_position() + a * d * dt);
			}
			if (magnitude > num && base.transform.position.y < ZoneSystem.instance.m_waterLevel - 40f)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = 99999f;
				Damage(hitData);
			}
		}
	}

	private void AutoPickup(float dt)
	{
		if (IsTeleporting())
		{
			return;
		}
		Vector3 vector = base.transform.position + Vector3.up;
		Collider[] array = Physics.OverlapSphere(vector, m_autoPickupRange, m_autoPickupMask);
		foreach (Collider val in array)
		{
			if (!(UnityEngine.Object)(object)val.get_attachedRigidbody())
			{
				continue;
			}
			ItemDrop component = ((Component)(object)val.get_attachedRigidbody()).GetComponent<ItemDrop>();
			if (component == null || !component.m_autoPickup || HaveUniqueKey(component.m_itemData.m_shared.m_name) || !component.GetComponent<ZNetView>().IsValid())
			{
				continue;
			}
			if (!component.CanPickup())
			{
				component.RequestOwn();
			}
			else
			{
				if (!m_inventory.CanAddItem(component.m_itemData) || component.m_itemData.GetWeight() + m_inventory.GetTotalWeight() > GetMaxCarryWeight())
				{
					continue;
				}
				float num = Vector3.Distance(component.transform.position, vector);
				if (!(num > m_autoPickupRange))
				{
					if (num < 0.3f)
					{
						Pickup(component.gameObject);
						continue;
					}
					Vector3 a = Vector3.Normalize(vector - component.transform.position);
					float d = 15f;
					component.transform.position = component.transform.position + a * d * dt;
				}
			}
		}
	}

	private void PlayerAttackInput(float dt)
	{
		if (InPlaceMode())
		{
			return;
		}
		ItemDrop.ItemData currentWeapon = GetCurrentWeapon();
		if (currentWeapon != null && currentWeapon.m_shared.m_holdDurationMin > 0f)
		{
			if (m_blocking || InMinorAction())
			{
				m_attackDrawTime = -1f;
				if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
				{
					m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, value: false);
				}
				return;
			}
			bool flag = currentWeapon.m_shared.m_holdStaminaDrain <= 0f || HaveStamina();
			if (m_attackDrawTime < 0f)
			{
				if (!m_attackDraw)
				{
					m_attackDrawTime = 0f;
				}
			}
			else if (m_attackDraw && flag && m_attackDrawTime >= 0f)
			{
				if (m_attackDrawTime == 0f)
				{
					if (!currentWeapon.m_shared.m_attack.StartDraw(this, currentWeapon))
					{
						m_attackDrawTime = -1f;
						return;
					}
					currentWeapon.m_shared.m_holdStartEffect.Create(base.transform.position, Quaternion.identity, base.transform);
				}
				m_attackDrawTime += Time.fixedDeltaTime;
				if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
				{
					m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, value: true);
				}
				UseStamina(currentWeapon.m_shared.m_holdStaminaDrain * dt);
			}
			else if (m_attackDrawTime > 0f)
			{
				if (flag)
				{
					StartAttack(null, secondaryAttack: false);
				}
				if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
				{
					m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, value: false);
				}
				m_attackDrawTime = 0f;
			}
		}
		else
		{
			if (m_attack)
			{
				m_queuedAttackTimer = 0.5f;
				m_queuedSecondAttackTimer = 0f;
			}
			if (m_secondaryAttack)
			{
				m_queuedSecondAttackTimer = 0.5f;
				m_queuedAttackTimer = 0f;
			}
			m_queuedAttackTimer -= Time.fixedDeltaTime;
			m_queuedSecondAttackTimer -= Time.fixedDeltaTime;
			if (m_queuedAttackTimer > 0f && StartAttack(null, secondaryAttack: false))
			{
				m_queuedAttackTimer = 0f;
			}
			if (m_queuedSecondAttackTimer > 0f && StartAttack(null, secondaryAttack: true))
			{
				m_queuedSecondAttackTimer = 0f;
			}
		}
	}

	protected override bool HaveQueuedChain()
	{
		if (m_queuedAttackTimer > 0f && GetCurrentWeapon() != null && m_currentAttack != null)
		{
			return m_currentAttack.CanStartChainAttack();
		}
		return false;
	}

	private void UpdateBaseValue(float dt)
	{
		m_baseValueUpdatetimer += dt;
		if (m_baseValueUpdatetimer > 2f)
		{
			m_baseValueUpdatetimer = 0f;
			m_baseValue = EffectArea.GetBaseValue(base.transform.position, 20f);
			m_nview.GetZDO().Set("baseValue", m_baseValue);
			m_comfortLevel = SE_Rested.CalculateComfortLevel(this);
		}
	}

	public int GetComfortLevel()
	{
		return m_comfortLevel;
	}

	public int GetBaseValue()
	{
		if (!m_nview.IsValid())
		{
			return 0;
		}
		if (m_nview.IsOwner())
		{
			return m_baseValue;
		}
		return m_nview.GetZDO().GetInt("baseValue");
	}

	public bool IsSafeInHome()
	{
		return m_safeInHome;
	}

	private void UpdateBiome(float dt)
	{
		if (InIntro())
		{
			return;
		}
		m_biomeTimer += dt;
		if (m_biomeTimer > 1f)
		{
			m_biomeTimer = 0f;
			Heightmap.Biome biome = Heightmap.FindBiome(base.transform.position);
			if (m_currentBiome != biome)
			{
				m_currentBiome = biome;
				AddKnownBiome(biome);
			}
		}
	}

	public Heightmap.Biome GetCurrentBiome()
	{
		return m_currentBiome;
	}

	public override void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
		float multiplier = 1f;
		m_seman.ModifyRaiseSkill(skill, ref multiplier);
		value *= multiplier;
		m_skills.RaiseSkill(skill, value);
	}

	private void UpdateStats(float dt)
	{
		if (InIntro() || IsTeleporting())
		{
			return;
		}
		m_timeSinceDeath += dt;
		UpdateMovementModifier();
		UpdateFood(dt, forceUpdate: false);
		bool flag = IsEncumbered();
		float maxStamina = GetMaxStamina();
		float num = 1f;
		if (IsBlocking())
		{
			num *= 0.8f;
		}
		if ((IsSwiming() && !IsOnGround()) || InAttack() || InDodge() || m_wallRunning || flag)
		{
			num = 0f;
		}
		float num2 = (m_staminaRegen + (1f - m_stamina / maxStamina) * m_staminaRegen * m_staminaRegenTimeMultiplier) * num;
		float staminaMultiplier = 1f;
		m_seman.ModifyStaminaRegen(ref staminaMultiplier);
		num2 *= staminaMultiplier;
		m_staminaRegenTimer -= dt;
		if (m_stamina < maxStamina && m_staminaRegenTimer <= 0f)
		{
			m_stamina = Mathf.Min(maxStamina, m_stamina + num2 * dt);
		}
		m_nview.GetZDO().Set("stamina", m_stamina);
		if (flag)
		{
			if (m_moveDir.magnitude > 0.1f)
			{
				UseStamina(m_encumberedStaminaDrain * dt);
			}
			m_seman.AddStatusEffect("Encumbered");
			ShowTutorial("encumbered");
		}
		else
		{
			m_seman.RemoveStatusEffect("Encumbered");
		}
		if (!HardDeath())
		{
			m_seman.AddStatusEffect("SoftDeath");
		}
		else
		{
			m_seman.RemoveStatusEffect("SoftDeath");
		}
		UpdateEnvStatusEffects(dt);
	}

	private void UpdateEnvStatusEffects(float dt)
	{
		m_nearFireTimer += dt;
		HitData.DamageModifiers damageModifiers = GetDamageModifiers();
		bool flag = m_nearFireTimer < 0.25f;
		bool flag2 = m_seman.HaveStatusEffect("Burning");
		bool flag3 = InShelter();
		HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
		bool flag4 = EnvMan.instance.IsFreezing();
		bool num = EnvMan.instance.IsCold();
		bool flag5 = EnvMan.instance.IsWet();
		bool flag6 = IsSensed();
		bool flag7 = m_seman.HaveStatusEffect("Wet");
		bool flag8 = IsSitting();
		bool flag9 = flag4 && !flag && !flag3;
		bool flag10 = (num && !flag) || (flag4 && flag && !flag3) || (flag4 && !flag && flag3);
		if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant)
		{
			flag9 = false;
			flag10 = false;
		}
		if (flag5 && !m_underRoof)
		{
			m_seman.AddStatusEffect("Wet", resetTime: true);
		}
		if (flag3)
		{
			m_seman.AddStatusEffect("Shelter");
		}
		else
		{
			m_seman.RemoveStatusEffect("Shelter");
		}
		if (flag)
		{
			m_seman.AddStatusEffect("CampFire");
		}
		else
		{
			m_seman.RemoveStatusEffect("CampFire");
		}
		bool flag11 = !flag6 && (flag8 || flag3) && !flag10 && !flag9 && !flag7 && !flag2 && flag;
		if (flag11)
		{
			m_seman.AddStatusEffect("Resting");
		}
		else
		{
			m_seman.RemoveStatusEffect("Resting");
		}
		m_safeInHome = flag11 && flag3;
		if (flag9)
		{
			if (!m_seman.RemoveStatusEffect("Cold", quiet: true))
			{
				m_seman.AddStatusEffect("Freezing");
			}
		}
		else if (flag10)
		{
			if (!m_seman.RemoveStatusEffect("Freezing", quiet: true) && (bool)m_seman.AddStatusEffect("Cold"))
			{
				ShowTutorial("cold");
			}
		}
		else
		{
			m_seman.RemoveStatusEffect("Cold");
			m_seman.RemoveStatusEffect("Freezing");
		}
	}

	private bool CanEat(ItemDrop.ItemData item, bool showMessages)
	{
		foreach (Food food in m_foods)
		{
			if (food.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food.CanEatAgain())
				{
					return true;
				}
				Message(MessageHud.MessageType.Center, Localization.get_instance().Localize("$msg_nomore", new string[1]
				{
					item.m_shared.m_name
				}));
				return false;
			}
		}
		foreach (Food food2 in m_foods)
		{
			if (food2.CanEatAgain())
			{
				return true;
			}
		}
		if (m_foods.Count >= 3)
		{
			Message(MessageHud.MessageType.Center, "$msg_isfull");
			return false;
		}
		return true;
	}

	private Food GetMostDepletedFood()
	{
		Food food = null;
		foreach (Food food2 in m_foods)
		{
			if (food2.CanEatAgain() && (food == null || food2.m_health < food.m_health))
			{
				food = food2;
			}
		}
		return food;
	}

	public void ClearFood()
	{
		m_foods.Clear();
	}

	private bool EatFood(ItemDrop.ItemData item)
	{
		if (!CanEat(item, showMessages: false))
		{
			return false;
		}
		foreach (Food food2 in m_foods)
		{
			if (food2.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food2.CanEatAgain())
				{
					food2.m_health = item.m_shared.m_food;
					food2.m_stamina = item.m_shared.m_foodStamina;
					UpdateFood(0f, forceUpdate: true);
					return true;
				}
				return false;
			}
		}
		if (m_foods.Count < 3)
		{
			Food food = new Food();
			food.m_name = item.m_dropPrefab.name;
			food.m_item = item;
			food.m_health = item.m_shared.m_food;
			food.m_stamina = item.m_shared.m_foodStamina;
			m_foods.Add(food);
			UpdateFood(0f, forceUpdate: true);
			return true;
		}
		Food mostDepletedFood = GetMostDepletedFood();
		if (mostDepletedFood != null)
		{
			mostDepletedFood.m_name = item.m_dropPrefab.name;
			mostDepletedFood.m_item = item;
			mostDepletedFood.m_health = item.m_shared.m_food;
			mostDepletedFood.m_stamina = item.m_shared.m_foodStamina;
			return true;
		}
		return false;
	}

	private void UpdateFood(float dt, bool forceUpdate)
	{
		m_foodUpdateTimer += dt;
		if (m_foodUpdateTimer >= 1f || forceUpdate)
		{
			m_foodUpdateTimer = 0f;
			foreach (Food food in m_foods)
			{
				food.m_health -= food.m_item.m_shared.m_food / food.m_item.m_shared.m_foodBurnTime;
				food.m_stamina -= food.m_item.m_shared.m_foodStamina / food.m_item.m_shared.m_foodBurnTime;
				if (food.m_health < 0f)
				{
					food.m_health = 0f;
				}
				if (food.m_stamina < 0f)
				{
					food.m_stamina = 0f;
				}
				if (food.m_health <= 0f)
				{
					Message(MessageHud.MessageType.Center, "$msg_food_done");
					m_foods.Remove(food);
					break;
				}
			}
			GetTotalFoodValue(out var hp, out var stamina);
			SetMaxHealth(hp, flashBar: true);
			SetMaxStamina(stamina, flashBar: true);
		}
		if (forceUpdate)
		{
			return;
		}
		m_foodRegenTimer += dt;
		if (!(m_foodRegenTimer >= 10f))
		{
			return;
		}
		m_foodRegenTimer = 0f;
		float num = 0f;
		foreach (Food food2 in m_foods)
		{
			num += food2.m_item.m_shared.m_foodRegen;
		}
		if (num > 0f)
		{
			float regenMultiplier = 1f;
			m_seman.ModifyHealthRegen(ref regenMultiplier);
			num *= regenMultiplier;
			Heal(num);
		}
	}

	private void GetTotalFoodValue(out float hp, out float stamina)
	{
		hp = 25f;
		stamina = 75f;
		foreach (Food food in m_foods)
		{
			hp += food.m_health;
			stamina += food.m_stamina;
		}
	}

	public float GetBaseFoodHP()
	{
		return 25f;
	}

	public List<Food> GetFoods()
	{
		return m_foods;
	}

	public void OnSpawned()
	{
		m_spawnEffects.Create(base.transform.position, Quaternion.identity);
		if (m_firstSpawn)
		{
			if (m_valkyrie != null)
			{
				UnityEngine.Object.Instantiate(m_valkyrie, base.transform.position, Quaternion.identity);
			}
			m_firstSpawn = false;
		}
	}

	protected override bool CheckRun(Vector3 moveDir, float dt)
	{
		if (!base.CheckRun(moveDir, dt))
		{
			return false;
		}
		bool flag = HaveStamina();
		float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Run);
		float num = Mathf.Lerp(1f, 0.5f, skillFactor);
		float drain = m_runStaminaDrain * num;
		m_seman.ModifyRunStaminaDrain(drain, ref drain);
		UseStamina(dt * drain);
		if (HaveStamina())
		{
			m_runSkillImproveTimer += dt;
			if (m_runSkillImproveTimer > 1f)
			{
				m_runSkillImproveTimer = 0f;
				RaiseSkill(Skills.SkillType.Run);
			}
			AbortEquipQueue();
			return true;
		}
		if (flag)
		{
			Hud.instance.StaminaBarNoStaminaFlash();
		}
		return false;
	}

	private void UpdateMovementModifier()
	{
		m_equipmentMovementModifier = 0f;
		if (m_rightItem != null)
		{
			m_equipmentMovementModifier += m_rightItem.m_shared.m_movementModifier;
		}
		if (m_leftItem != null)
		{
			m_equipmentMovementModifier += m_leftItem.m_shared.m_movementModifier;
		}
		if (m_chestItem != null)
		{
			m_equipmentMovementModifier += m_chestItem.m_shared.m_movementModifier;
		}
		if (m_legItem != null)
		{
			m_equipmentMovementModifier += m_legItem.m_shared.m_movementModifier;
		}
		if (m_helmetItem != null)
		{
			m_equipmentMovementModifier += m_helmetItem.m_shared.m_movementModifier;
		}
		if (m_shoulderItem != null)
		{
			m_equipmentMovementModifier += m_shoulderItem.m_shared.m_movementModifier;
		}
		if (m_utilityItem != null)
		{
			m_equipmentMovementModifier += m_utilityItem.m_shared.m_movementModifier;
		}
	}

	public void OnSkillLevelup(Skills.SkillType skill, float level)
	{
		m_skillLevelupEffects.Create(m_head.position, m_head.rotation, m_head);
	}

	protected override void OnJump()
	{
		AbortEquipQueue();
		float staminaUse = m_jumpStaminaUsage - m_jumpStaminaUsage * m_equipmentMovementModifier;
		m_seman.ModifyJumpStaminaUsage(staminaUse, ref staminaUse);
		UseStamina(staminaUse);
	}

	protected override void OnSwiming(Vector3 targetVel, float dt)
	{
		base.OnSwiming(targetVel, dt);
		if (targetVel.magnitude > 0.1f)
		{
			float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Swim);
			float num = Mathf.Lerp(m_swimStaminaDrainMinSkill, m_swimStaminaDrainMaxSkill, skillFactor);
			UseStamina(dt * num);
			m_swimSkillImproveTimer += dt;
			if (m_swimSkillImproveTimer > 1f)
			{
				m_swimSkillImproveTimer = 0f;
				RaiseSkill(Skills.SkillType.Swim);
			}
		}
		if (!HaveStamina())
		{
			m_drownDamageTimer += dt;
			if (m_drownDamageTimer > 1f)
			{
				m_drownDamageTimer = 0f;
				float damage = Mathf.Ceil(GetMaxHealth() / 20f);
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = damage;
				hitData.m_point = GetCenterPoint();
				hitData.m_dir = Vector3.down;
				hitData.m_pushForce = 10f;
				Damage(hitData);
				Vector3 position = base.transform.position;
				position.y = m_waterLevel;
				m_drownEffects.Create(position, base.transform.rotation);
			}
		}
	}

	protected override bool TakeInput()
	{
		bool result = (!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !TextInput.IsVisible() && !StoreGui.IsVisible() && !InventoryGui.IsVisible() && !Menu.IsVisible() && (!TextViewer.instance || !TextViewer.instance.IsVisible()) && !Minimap.IsOpen() && !GameCamera.InFreeFly();
		if (IsDead() || InCutscene() || IsTeleporting())
		{
			result = false;
		}
		return result;
	}

	public void UseHotbarItem(int index)
	{
		ItemDrop.ItemData itemAt = m_inventory.GetItemAt(index - 1, 0);
		if (itemAt != null)
		{
			UseItem(null, itemAt, fromInventoryGui: false);
		}
	}

	public bool RequiredCraftingStation(Recipe recipe, int qualityLevel, bool checkLevel)
	{
		CraftingStation requiredStation = recipe.GetRequiredStation(qualityLevel);
		if (requiredStation != null)
		{
			if (m_currentStation == null)
			{
				return false;
			}
			if (requiredStation.m_name != m_currentStation.m_name)
			{
				return false;
			}
			if (checkLevel)
			{
				int requiredStationLevel = recipe.GetRequiredStationLevel(qualityLevel);
				if (m_currentStation.GetLevel() < requiredStationLevel)
				{
					return false;
				}
			}
		}
		else if (m_currentStation != null && !m_currentStation.m_showBasicRecipies)
		{
			return false;
		}
		return true;
	}

	public bool HaveRequirements(Recipe recipe, bool discover, int qualityLevel)
	{
		if (discover)
		{
			if ((bool)recipe.m_craftingStation && !KnowStationLevel(recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
			{
				return false;
			}
		}
		else if (!RequiredCraftingStation(recipe, qualityLevel, checkLevel: true))
		{
			return false;
		}
		if (recipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc))
		{
			return false;
		}
		if (!HaveRequirements(recipe.m_resources, discover, qualityLevel))
		{
			return false;
		}
		return true;
	}

	private bool HaveRequirements(Piece.Requirement[] resources, bool discover, int qualityLevel)
	{
		foreach (Piece.Requirement requirement in resources)
		{
			if (!requirement.m_resItem)
			{
				continue;
			}
			if (discover)
			{
				if (requirement.m_amount > 0 && !m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
				{
					return false;
				}
				continue;
			}
			int amount = requirement.GetAmount(qualityLevel);
			if (m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < amount)
			{
				return false;
			}
		}
		return true;
	}

	public bool HaveRequirements(Piece piece, RequirementMode mode)
	{
		if ((bool)piece.m_craftingStation)
		{
			if (mode == RequirementMode.IsKnown || mode == RequirementMode.CanAlmostBuild)
			{
				if (!m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
				{
					return false;
				}
			}
			else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position))
			{
				return false;
			}
		}
		if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
		{
			return false;
		}
		Piece.Requirement[] resources = piece.m_resources;
		foreach (Piece.Requirement requirement in resources)
		{
			if (!requirement.m_resItem || requirement.m_amount <= 0)
			{
				continue;
			}
			switch (mode)
			{
			case RequirementMode.IsKnown:
				if (!m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
				{
					return false;
				}
				break;
			case RequirementMode.CanAlmostBuild:
				if (!m_inventory.HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
				{
					return false;
				}
				break;
			case RequirementMode.CanBuild:
				if (m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount)
				{
					return false;
				}
				break;
			}
		}
		return true;
	}

	public void SetCraftingStation(CraftingStation station)
	{
		if (!(m_currentStation == station))
		{
			if ((bool)station)
			{
				AddKnownStation(station);
				station.PokeInUse();
			}
			m_currentStation = station;
			HideHandItems();
			int value = (m_currentStation ? m_currentStation.m_useAnimation : 0);
			m_zanim.SetInt("crafting", value);
		}
	}

	public CraftingStation GetCurrentCraftingStation()
	{
		return m_currentStation;
	}

	public void ConsumeResources(Piece.Requirement[] requirements, int qualityLevel)
	{
		foreach (Piece.Requirement requirement in requirements)
		{
			if ((bool)requirement.m_resItem)
			{
				int amount = requirement.GetAmount(qualityLevel);
				if (amount > 0)
				{
					m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, amount);
				}
			}
		}
	}

	private void UpdateHover()
	{
		if (InPlaceMode() || IsDead() || m_shipControl != null)
		{
			m_hovering = null;
			m_hoveringCreature = null;
		}
		else
		{
			FindHoverObject(out m_hovering, out m_hoveringCreature);
		}
	}

	private bool CheckCanRemovePiece(Piece piece)
	{
		if (!m_noPlacementCost && piece.m_craftingStation != null && !CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position))
		{
			Message(MessageHud.MessageType.Center, "$msg_missingstation");
			return false;
		}
		return true;
	}

	private bool RemovePiece()
	{
		RaycastHit val = default(RaycastHit);
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, ref val, 50f, m_removeRayMask) && Vector3.Distance(((RaycastHit)(ref val)).get_point(), m_eye.position) < m_maxPlaceDistance)
		{
			Piece piece = ((Component)(object)((RaycastHit)(ref val)).get_collider()).GetComponentInParent<Piece>();
			if (piece == null && (bool)((Component)(object)((RaycastHit)(ref val)).get_collider()).GetComponent<Heightmap>())
			{
				piece = TerrainModifier.FindClosestModifierPieceInRange(((RaycastHit)(ref val)).get_point(), 2.5f);
			}
			if ((bool)piece)
			{
				if (!piece.m_canBeRemoved)
				{
					return false;
				}
				if (Location.IsInsideNoBuildLocation(piece.transform.position))
				{
					Message(MessageHud.MessageType.Center, "$msg_nobuildzone");
					return false;
				}
				if (!PrivateArea.CheckAccess(piece.transform.position))
				{
					Message(MessageHud.MessageType.Center, "$msg_privatezone");
					return false;
				}
				if (!CheckCanRemovePiece(piece))
				{
					return false;
				}
				ZNetView component = piece.GetComponent<ZNetView>();
				if (component == null)
				{
					return false;
				}
				if (!piece.CanBeRemoved())
				{
					Message(MessageHud.MessageType.Center, "$msg_cantremovenow");
					return false;
				}
				WearNTear component2 = piece.GetComponent<WearNTear>();
				if ((bool)component2)
				{
					component2.Remove();
				}
				else
				{
					ZLog.Log((object)("Removing non WNT object with hammer " + piece.name));
					component.ClaimOwnership();
					piece.DropResources();
					piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform);
					m_removeEffects.Create(piece.transform.position, Quaternion.identity);
					ZNetScene.instance.Destroy(piece.gameObject);
				}
				ItemDrop.ItemData rightItem = GetRightItem();
				if (rightItem != null)
				{
					FaceLookDirection();
					m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
				}
				return true;
			}
		}
		return false;
	}

	public void FaceLookDirection()
	{
		base.transform.rotation = GetLookYaw();
	}

	private bool PlacePiece(Piece piece)
	{
		UpdatePlacementGhost(flashGuardStone: true);
		Vector3 position = m_placementGhost.transform.position;
		Quaternion rotation = m_placementGhost.transform.rotation;
		GameObject gameObject = piece.gameObject;
		switch (m_placementStatus)
		{
		case PlacementStatus.NoBuildZone:
			Message(MessageHud.MessageType.Center, "$msg_nobuildzone");
			return false;
		case PlacementStatus.BlockedbyPlayer:
			Message(MessageHud.MessageType.Center, "$msg_blocked");
			return false;
		case PlacementStatus.PrivateZone:
			Message(MessageHud.MessageType.Center, "$msg_privatezone");
			return false;
		case PlacementStatus.MoreSpace:
			Message(MessageHud.MessageType.Center, "$msg_needspace");
			return false;
		case PlacementStatus.NoTeleportArea:
			Message(MessageHud.MessageType.Center, "$msg_noteleportarea");
			return false;
		case PlacementStatus.Invalid:
			Message(MessageHud.MessageType.Center, "$msg_invalidplacement");
			return false;
		case PlacementStatus.ExtensionMissingStation:
			Message(MessageHud.MessageType.Center, "$msg_extensionmissingstation");
			return false;
		case PlacementStatus.WrongBiome:
			Message(MessageHud.MessageType.Center, "$msg_wrongbiome");
			return false;
		case PlacementStatus.NeedCultivated:
			Message(MessageHud.MessageType.Center, "$msg_needcultivated");
			return false;
		case PlacementStatus.NotInDungeon:
			Message(MessageHud.MessageType.Center, "$msg_notindungeon");
			return false;
		default:
		{
			TerrainModifier.SetTriggerOnPlaced(trigger: true);
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, position, rotation);
			TerrainModifier.SetTriggerOnPlaced(trigger: false);
			CraftingStation componentInChildren = gameObject2.GetComponentInChildren<CraftingStation>();
			if ((bool)componentInChildren)
			{
				AddKnownStation(componentInChildren);
			}
			Piece component = gameObject2.GetComponent<Piece>();
			if ((bool)component)
			{
				component.SetCreator(GetPlayerID());
			}
			PrivateArea component2 = gameObject2.GetComponent<PrivateArea>();
			if ((bool)component2)
			{
				component2.Setup(Game.instance.GetPlayerProfile().GetName());
			}
			WearNTear component3 = gameObject2.GetComponent<WearNTear>();
			if ((bool)component3)
			{
				component3.OnPlaced();
			}
			ItemDrop.ItemData rightItem = GetRightItem();
			if (rightItem != null)
			{
				FaceLookDirection();
				m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
			}
			piece.m_placeEffect.Create(position, rotation, gameObject2.transform);
			AddNoise(50f);
			Game.instance.GetPlayerProfile().m_playerStats.m_builds++;
			ZLog.Log((object)("Placed " + gameObject.name));
			Gogan.LogEvent("Game", "PlacedPiece", gameObject.name, 0L);
			return true;
		}
		}
	}

	public override bool IsPlayer()
	{
		return true;
	}

	public void GetBuildSelection(out Piece go, out Vector2Int id, out int total, out Piece.PieceCategory category, out bool useCategory)
	{
		category = m_buildPieces.m_selectedCategory;
		useCategory = m_buildPieces.m_useCategories;
		if (m_buildPieces.GetAvailablePiecesInSelectedCategory() == 0)
		{
			go = null;
			id = Vector2Int.zero;
			total = 0;
		}
		else
		{
			GameObject selectedPrefab = m_buildPieces.GetSelectedPrefab();
			go = (selectedPrefab ? selectedPrefab.GetComponent<Piece>() : null);
			id = m_buildPieces.GetSelectedIndex();
			total = m_buildPieces.GetAvailablePiecesInSelectedCategory();
		}
	}

	public List<Piece> GetBuildPieces()
	{
		if ((bool)m_buildPieces)
		{
			return m_buildPieces.GetPiecesInSelectedCategory();
		}
		return null;
	}

	public int GetAvailableBuildPiecesInCategory(Piece.PieceCategory cat)
	{
		if ((bool)m_buildPieces)
		{
			return m_buildPieces.GetAvailablePiecesInCategory(cat);
		}
		return 0;
	}

	private void RPC_OnDeath(long sender)
	{
		m_visual.SetActive(value: false);
	}

	private void CreateDeathEffects()
	{
		GameObject[] array = m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if ((bool)component)
			{
				Vector3 velocity = m_body.get_velocity();
				if (m_pushForce.magnitude * 0.5f > velocity.magnitude)
				{
					velocity = m_pushForce * 0.5f;
				}
				component.Setup(velocity, 0f, 0f, 0f, null);
				OnRagdollCreated(component);
				m_ragdoll = component;
			}
		}
	}

	public void UnequipDeathDropItems()
	{
		if (m_rightItem != null)
		{
			UnequipItem(m_rightItem, triggerEquipEffects: false);
		}
		if (m_leftItem != null)
		{
			UnequipItem(m_leftItem, triggerEquipEffects: false);
		}
		if (m_ammoItem != null)
		{
			UnequipItem(m_ammoItem, triggerEquipEffects: false);
		}
		if (m_utilityItem != null)
		{
			UnequipItem(m_utilityItem, triggerEquipEffects: false);
		}
	}

	private void CreateTombStone()
	{
		if (m_inventory.NrOfItems() != 0)
		{
			UnequipAllItems();
			GameObject gameObject = UnityEngine.Object.Instantiate(m_tombstone, GetCenterPoint(), base.transform.rotation);
			gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(m_inventory);
			TombStone component = gameObject.GetComponent<TombStone>();
			PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
			component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
		}
	}

	private bool HardDeath()
	{
		return m_timeSinceDeath > m_hardDeathCooldown;
	}

	protected override void OnDeath()
	{
		bool num = HardDeath();
		m_nview.GetZDO().Set("dead", value: true);
		m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath");
		Game.instance.GetPlayerProfile().m_playerStats.m_deaths++;
		Game.instance.GetPlayerProfile().SetDeathPoint(base.transform.position);
		CreateDeathEffects();
		CreateTombStone();
		m_foods.Clear();
		if (num)
		{
			m_skills.OnDeath();
		}
		Game.instance.RequestRespawn(10f);
		m_timeSinceDeath = 0f;
		if (!num)
		{
			Message(MessageHud.MessageType.TopLeft, "$msg_softdeath");
		}
		Message(MessageHud.MessageType.Center, "$msg_youdied");
		ShowTutorial("death");
		string eventLabel = "biome:" + GetCurrentBiome();
		Gogan.LogEvent("Game", "Death", eventLabel, 0L);
	}

	public void OnRespawn()
	{
		m_nview.GetZDO().Set("dead", value: false);
		SetHealth(GetMaxHealth());
	}

	private void SetupPlacementGhost()
	{
		if ((bool)m_placementGhost)
		{
			UnityEngine.Object.Destroy(m_placementGhost);
			m_placementGhost = null;
		}
		if (m_buildPieces == null)
		{
			return;
		}
		GameObject selectedPrefab = m_buildPieces.GetSelectedPrefab();
		if (selectedPrefab == null || selectedPrefab.GetComponent<Piece>().m_repairPiece)
		{
			return;
		}
		bool enabled = false;
		TerrainModifier componentInChildren = selectedPrefab.GetComponentInChildren<TerrainModifier>();
		if ((bool)componentInChildren)
		{
			enabled = componentInChildren.enabled;
			componentInChildren.enabled = false;
		}
		ZNetView.m_forceDisableInit = true;
		m_placementGhost = UnityEngine.Object.Instantiate(selectedPrefab);
		ZNetView.m_forceDisableInit = false;
		m_placementGhost.name = selectedPrefab.name;
		if ((bool)componentInChildren)
		{
			componentInChildren.enabled = enabled;
		}
		Joint[] componentsInChildren = m_placementGhost.GetComponentsInChildren<Joint>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)componentsInChildren[i]);
		}
		Rigidbody[] componentsInChildren2 = m_placementGhost.GetComponentsInChildren<Rigidbody>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)componentsInChildren2[i]);
		}
		Collider[] componentsInChildren3 = m_placementGhost.GetComponentsInChildren<Collider>();
		foreach (Collider val in componentsInChildren3)
		{
			if (((1 << ((Component)(object)val).gameObject.layer) & m_placeRayMask) == 0)
			{
				ZLog.Log((object)("Disabling " + ((Component)(object)val).gameObject.name + "  " + LayerMask.LayerToName(((Component)(object)val).gameObject.layer)));
				val.set_enabled(false);
			}
		}
		Transform[] componentsInChildren4 = m_placementGhost.GetComponentsInChildren<Transform>();
		int layer = LayerMask.NameToLayer("ghost");
		Transform[] array = componentsInChildren4;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.layer = layer;
		}
		TerrainModifier[] componentsInChildren5 = m_placementGhost.GetComponentsInChildren<TerrainModifier>();
		for (int i = 0; i < componentsInChildren5.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren5[i]);
		}
		GuidePoint[] componentsInChildren6 = m_placementGhost.GetComponentsInChildren<GuidePoint>();
		for (int i = 0; i < componentsInChildren6.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren6[i]);
		}
		Light[] componentsInChildren7 = m_placementGhost.GetComponentsInChildren<Light>();
		for (int i = 0; i < componentsInChildren7.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren7[i]);
		}
		AudioSource[] componentsInChildren8 = m_placementGhost.GetComponentsInChildren<AudioSource>();
		for (int i = 0; i < componentsInChildren8.Length; i++)
		{
			((Behaviour)(object)componentsInChildren8[i]).enabled = false;
		}
		ZSFX[] componentsInChildren9 = m_placementGhost.GetComponentsInChildren<ZSFX>();
		for (int i = 0; i < componentsInChildren9.Length; i++)
		{
			componentsInChildren9[i].enabled = false;
		}
		Windmill componentInChildren2 = m_placementGhost.GetComponentInChildren<Windmill>();
		if ((bool)componentInChildren2)
		{
			componentInChildren2.enabled = false;
		}
		ParticleSystem[] componentsInChildren10 = m_placementGhost.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren10.Length; i++)
		{
			((Component)(object)componentsInChildren10[i]).gameObject.SetActive(value: false);
		}
		Transform transform = m_placementGhost.transform.Find("_GhostOnly");
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: true);
		}
		m_placementGhost.transform.position = base.transform.position;
		m_placementGhost.transform.localScale = selectedPrefab.transform.localScale;
		MeshRenderer[] componentsInChildren11 = m_placementGhost.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in componentsInChildren11)
		{
			if (!(meshRenderer.sharedMaterial == null))
			{
				Material[] sharedMaterials = meshRenderer.sharedMaterials;
				for (int j = 0; j < sharedMaterials.Length; j++)
				{
					Material material = new Material(sharedMaterials[j]);
					material.SetFloat("_RippleDistance", 0f);
					material.SetFloat("_ValueNoise", 0f);
					sharedMaterials[j] = material;
				}
				meshRenderer.sharedMaterials = sharedMaterials;
				meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		}
	}

	private void SetPlacementGhostValid(bool valid)
	{
		m_placementGhost.GetComponent<Piece>().SetInvalidPlacementHeightlight(!valid);
	}

	protected override void SetPlaceMode(PieceTable buildPieces)
	{
		base.SetPlaceMode(buildPieces);
		m_buildPieces = buildPieces;
		UpdateAvailablePiecesList();
	}

	public void SetBuildCategory(int index)
	{
		if (m_buildPieces != null)
		{
			m_buildPieces.SetCategory(index);
			UpdateAvailablePiecesList();
		}
	}

	public override bool InPlaceMode()
	{
		return m_buildPieces != null;
	}

	private void Repair(ItemDrop.ItemData toolItem, Piece repairPiece)
	{
		if (!InPlaceMode())
		{
			return;
		}
		Piece hoveringPiece = GetHoveringPiece();
		if (!hoveringPiece || !CheckCanRemovePiece(hoveringPiece) || !PrivateArea.CheckAccess(hoveringPiece.transform.position))
		{
			return;
		}
		bool flag = false;
		WearNTear component = hoveringPiece.GetComponent<WearNTear>();
		if ((bool)component && component.Repair())
		{
			flag = true;
		}
		if (flag)
		{
			FaceLookDirection();
			m_zanim.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
			hoveringPiece.m_placeEffect.Create(hoveringPiece.transform.position, hoveringPiece.transform.rotation);
			Message(MessageHud.MessageType.TopLeft, Localization.get_instance().Localize("$msg_repaired", new string[1]
			{
				hoveringPiece.m_name
			}));
			UseStamina(toolItem.m_shared.m_attack.m_attackStamina);
			if (toolItem.m_shared.m_useDurability)
			{
				toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain;
			}
		}
		else
		{
			Message(MessageHud.MessageType.TopLeft, hoveringPiece.m_name + " $msg_doesnotneedrepair");
		}
	}

	private void UpdateWearNTearHover()
	{
		if (!InPlaceMode())
		{
			m_hoveringPiece = null;
			return;
		}
		m_hoveringPiece = null;
		RaycastHit val = default(RaycastHit);
		if (!Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, ref val, 50f, m_removeRayMask) || !(Vector3.Distance(m_eye.position, ((RaycastHit)(ref val)).get_point()) < m_maxPlaceDistance))
		{
			return;
		}
		Piece piece = (m_hoveringPiece = ((Component)(object)((RaycastHit)(ref val)).get_collider()).GetComponentInParent<Piece>());
		if ((bool)piece)
		{
			WearNTear component = piece.GetComponent<WearNTear>();
			if ((bool)component)
			{
				component.Highlight();
			}
		}
	}

	public Piece GetHoveringPiece()
	{
		if (InPlaceMode())
		{
			return m_hoveringPiece;
		}
		return null;
	}

	private void UpdatePlacementGhost(bool flashGuardStone)
	{
		if (m_placementGhost == null)
		{
			if ((bool)m_placementMarkerInstance)
			{
				m_placementMarkerInstance.SetActive(value: false);
			}
			return;
		}
		bool flag = ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
		Piece component = m_placementGhost.GetComponent<Piece>();
		bool water = component.m_waterPiece || component.m_noInWater;
		if (PieceRayTest(out var point, out var normal, out var piece, out var heightmap, out var waterSurface, water))
		{
			m_placementStatus = PlacementStatus.Valid;
			if (m_placementMarkerInstance == null)
			{
				m_placementMarkerInstance = UnityEngine.Object.Instantiate(m_placeMarker, point, Quaternion.identity);
			}
			m_placementMarkerInstance.SetActive(value: true);
			m_placementMarkerInstance.transform.position = point;
			m_placementMarkerInstance.transform.rotation = Quaternion.LookRotation(normal);
			if (component.m_groundOnly || component.m_groundPiece || component.m_cultivatedGroundOnly)
			{
				m_placementMarkerInstance.SetActive(value: false);
			}
			WearNTear wearNTear = ((piece != null) ? piece.GetComponent<WearNTear>() : null);
			StationExtension component2 = component.GetComponent<StationExtension>();
			if (component2 != null)
			{
				CraftingStation craftingStation = component2.FindClosestStationInRange(point);
				if ((bool)craftingStation)
				{
					component2.StartConnectionEffect(craftingStation);
				}
				else
				{
					component2.StopConnectionEffect();
					m_placementStatus = PlacementStatus.ExtensionMissingStation;
				}
				if (component2.OtherExtensionInRange(component.m_spaceRequirement))
				{
					m_placementStatus = PlacementStatus.MoreSpace;
				}
			}
			if ((bool)wearNTear && !wearNTear.m_supports)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_waterPiece && (UnityEngine.Object)(object)waterSurface == null && !flag)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_noInWater && (UnityEngine.Object)(object)waterSurface != null)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_groundPiece && heightmap == null)
			{
				m_placementGhost.SetActive(value: false);
				m_placementStatus = PlacementStatus.Invalid;
				return;
			}
			if (component.m_groundOnly && heightmap == null)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_cultivatedGroundOnly && (heightmap == null || !heightmap.IsCultivated(point)))
			{
				m_placementStatus = PlacementStatus.NeedCultivated;
			}
			if (component.m_notOnWood && (bool)piece && (bool)wearNTear && (wearNTear.m_materialType == WearNTear.MaterialType.Wood || wearNTear.m_materialType == WearNTear.MaterialType.HardWood))
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_notOnTiltingSurface && normal.y < 0.8f)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_inCeilingOnly && normal.y > -0.5f)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_notOnFloor && normal.y > 0.1f)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(point, EffectArea.Type.Teleport))
			{
				m_placementStatus = PlacementStatus.NoTeleportArea;
			}
			if (!component.m_allowedInDungeons && InInterior())
			{
				m_placementStatus = PlacementStatus.NotInDungeon;
			}
			if ((bool)heightmap)
			{
				normal = Vector3.up;
			}
			m_placementGhost.SetActive(value: true);
			Quaternion rotation = Quaternion.Euler(0f, 22.5f * (float)m_placeRotation, 0f);
			if (((component.m_groundPiece || component.m_clipGround) && (bool)heightmap) || component.m_clipEverything)
			{
				if ((bool)m_buildPieces.GetSelectedPrefab().GetComponent<TerrainModifier>() && component.m_allowAltGroundPlacement && component.m_groundPiece && !ZInput.GetButton("AltPlace") && !ZInput.GetButton("JoyAltPlace"))
				{
					float num = (point.y = ZoneSystem.instance.GetGroundHeight(base.transform.position));
				}
				m_placementGhost.transform.position = point;
				m_placementGhost.transform.rotation = rotation;
			}
			else
			{
				Collider[] componentsInChildren = m_placementGhost.GetComponentsInChildren<Collider>();
				if (componentsInChildren.Length != 0)
				{
					m_placementGhost.transform.position = point + normal * 50f;
					m_placementGhost.transform.rotation = rotation;
					Vector3 b = Vector3.zero;
					float num2 = 999999f;
					Collider[] array = componentsInChildren;
					foreach (Collider val in array)
					{
						if (val.get_isTrigger() || !val.get_enabled())
						{
							continue;
						}
						MeshCollider val2 = val as MeshCollider;
						if (!((UnityEngine.Object)(object)val2 != null) || val2.get_convex())
						{
							Vector3 vector = val.ClosestPoint(point);
							float num3 = Vector3.Distance(vector, point);
							if (num3 < num2)
							{
								b = vector;
								num2 = num3;
							}
						}
					}
					Vector3 b2 = m_placementGhost.transform.position - b;
					if (component.m_waterPiece)
					{
						b2.y = 3f;
					}
					m_placementGhost.transform.position = point + b2;
					m_placementGhost.transform.rotation = rotation;
				}
			}
			if (!flag)
			{
				m_tempPieces.Clear();
				if (FindClosestSnapPoints(m_placementGhost.transform, 0.5f, out var a, out var b3, m_tempPieces))
				{
					_ = b3.parent.position;
					Vector3 vector2 = b3.position - (a.position - m_placementGhost.transform.position);
					if (!IsOverlapingOtherPiece(vector2, m_placementGhost.name, m_tempPieces))
					{
						m_placementGhost.transform.position = vector2;
					}
				}
			}
			if (Location.IsInsideNoBuildLocation(m_placementGhost.transform.position))
			{
				m_placementStatus = PlacementStatus.NoBuildZone;
			}
			float radius = (component.GetComponent<PrivateArea>() ? component.GetComponent<PrivateArea>().m_radius : 0f);
			if (!PrivateArea.CheckAccess(m_placementGhost.transform.position, radius, flashGuardStone))
			{
				m_placementStatus = PlacementStatus.PrivateZone;
			}
			if (CheckPlacementGhostVSPlayers())
			{
				m_placementStatus = PlacementStatus.BlockedbyPlayer;
			}
			if (component.m_onlyInBiome != 0 && (Heightmap.FindBiome(m_placementGhost.transform.position) & component.m_onlyInBiome) == 0)
			{
				m_placementStatus = PlacementStatus.WrongBiome;
			}
			if (component.m_noClipping && TestGhostClipping(m_placementGhost, 0.2f))
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
		}
		else
		{
			if ((bool)m_placementMarkerInstance)
			{
				m_placementMarkerInstance.SetActive(value: false);
			}
			m_placementGhost.SetActive(value: false);
			m_placementStatus = PlacementStatus.Invalid;
		}
		SetPlacementGhostValid(m_placementStatus == PlacementStatus.Valid);
	}

	private bool IsOverlapingOtherPiece(Vector3 p, string pieceName, List<Piece> pieces)
	{
		foreach (Piece tempPiece in m_tempPieces)
		{
			if (Vector3.Distance(p, tempPiece.transform.position) < 0.05f && tempPiece.gameObject.name.StartsWith(pieceName))
			{
				return true;
			}
		}
		return false;
	}

	private bool FindClosestSnapPoints(Transform ghost, float maxSnapDistance, out Transform a, out Transform b, List<Piece> pieces)
	{
		m_tempSnapPoints1.Clear();
		ghost.GetComponent<Piece>().GetSnapPoints(m_tempSnapPoints1);
		m_tempSnapPoints2.Clear();
		m_tempPieces.Clear();
		Piece.GetSnapPoints(ghost.transform.position, 10f, m_tempSnapPoints2, m_tempPieces);
		float num = 9999999f;
		a = null;
		b = null;
		foreach (Transform item in m_tempSnapPoints1)
		{
			if (FindClosestSnappoint(item.position, m_tempSnapPoints2, maxSnapDistance, out var closest, out var distance) && distance < num)
			{
				num = distance;
				a = item;
				b = closest;
			}
		}
		return a != null;
	}

	private bool FindClosestSnappoint(Vector3 p, List<Transform> snapPoints, float maxDistance, out Transform closest, out float distance)
	{
		closest = null;
		distance = 999999f;
		foreach (Transform snapPoint in snapPoints)
		{
			float num = Vector3.Distance(snapPoint.position, p);
			if (!(num > maxDistance) && num < distance)
			{
				closest = snapPoint;
				distance = num;
			}
		}
		return closest != null;
	}

	private bool TestGhostClipping(GameObject ghost, float maxPenetration)
	{
		Collider[] componentsInChildren = ghost.GetComponentsInChildren<Collider>();
		Collider[] array = Physics.OverlapSphere(ghost.transform.position, 10f, m_placeRayMask);
		Collider[] array2 = componentsInChildren;
		Vector3 vector = default(Vector3);
		float num = default(float);
		foreach (Collider val in array2)
		{
			Collider[] array3 = array;
			foreach (Collider val2 in array3)
			{
				if (Physics.ComputePenetration(val, ((Component)(object)val).transform.position, ((Component)(object)val).transform.rotation, val2, ((Component)(object)val2).transform.position, ((Component)(object)val2).transform.rotation, ref vector, ref num) && num > maxPenetration)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CheckPlacementGhostVSPlayers()
	{
		if (m_placementGhost == null)
		{
			return false;
		}
		List<Character> list = new List<Character>();
		Character.GetCharactersInRange(base.transform.position, 30f, list);
		Collider[] componentsInChildren = m_placementGhost.GetComponentsInChildren<Collider>();
		Vector3 vector = default(Vector3);
		float num = default(float);
		foreach (Collider val in componentsInChildren)
		{
			if (val.get_isTrigger() || !val.get_enabled())
			{
				continue;
			}
			MeshCollider val2 = val as MeshCollider;
			if ((UnityEngine.Object)(object)val2 != null && !val2.get_convex())
			{
				continue;
			}
			foreach (Character item in list)
			{
				CapsuleCollider collider = item.GetCollider();
				if (Physics.ComputePenetration(val, ((Component)(object)val).transform.position, ((Component)(object)val).transform.rotation, (Collider)(object)collider, ((Component)(object)collider).transform.position, ((Component)(object)collider).transform.rotation, ref vector, ref num))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool PieceRayTest(out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
	{
		int num = m_placeRayMask;
		if (water)
		{
			num = m_placeWaterRayMask;
		}
		RaycastHit val = default(RaycastHit);
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, ref val, 50f, num) && (bool)(UnityEngine.Object)(object)((RaycastHit)(ref val)).get_collider() && !(UnityEngine.Object)(object)((RaycastHit)(ref val)).get_collider().get_attachedRigidbody() && Vector3.Distance(m_eye.position, ((RaycastHit)(ref val)).get_point()) < m_maxPlaceDistance)
		{
			point = ((RaycastHit)(ref val)).get_point();
			normal = ((RaycastHit)(ref val)).get_normal();
			piece = ((Component)(object)((RaycastHit)(ref val)).get_collider()).GetComponentInParent<Piece>();
			heightmap = ((Component)(object)((RaycastHit)(ref val)).get_collider()).GetComponent<Heightmap>();
			if (((Component)(object)((RaycastHit)(ref val)).get_collider()).gameObject.layer == LayerMask.NameToLayer("Water"))
			{
				waterSurface = ((RaycastHit)(ref val)).get_collider();
			}
			else
			{
				waterSurface = null;
			}
			return true;
		}
		point = Vector3.zero;
		normal = Vector3.zero;
		piece = null;
		heightmap = null;
		waterSurface = null;
		return false;
	}

	private void FindHoverObject(out GameObject hover, out Character hoverCreature)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		hover = null;
		hoverCreature = null;
		RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, m_interactMask);
		Array.Sort(array, (RaycastHit x, RaycastHit y) => ((RaycastHit)(ref x)).get_distance().CompareTo(((RaycastHit)(ref y)).get_distance()));
		RaycastHit[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			RaycastHit val = array2[i];
			if ((bool)(UnityEngine.Object)(object)((RaycastHit)(ref val)).get_collider().get_attachedRigidbody() && ((Component)(object)((RaycastHit)(ref val)).get_collider().get_attachedRigidbody()).gameObject == base.gameObject)
			{
				continue;
			}
			if (hoverCreature == null)
			{
				Character character = (((UnityEngine.Object)(object)((RaycastHit)(ref val)).get_collider().get_attachedRigidbody()) ? ((Component)(object)((RaycastHit)(ref val)).get_collider().get_attachedRigidbody()).GetComponent<Character>() : ((Component)(object)((RaycastHit)(ref val)).get_collider()).GetComponent<Character>());
				if (character != null)
				{
					hoverCreature = character;
				}
			}
			if (Vector3.Distance(m_eye.position, ((RaycastHit)(ref val)).get_point()) < m_maxInteractDistance)
			{
				if (((Component)(object)((RaycastHit)(ref val)).get_collider()).GetComponent<Hoverable>() != null)
				{
					hover = ((Component)(object)((RaycastHit)(ref val)).get_collider()).gameObject;
				}
				else if ((bool)(UnityEngine.Object)(object)((RaycastHit)(ref val)).get_collider().get_attachedRigidbody())
				{
					hover = ((Component)(object)((RaycastHit)(ref val)).get_collider().get_attachedRigidbody()).gameObject;
				}
				else
				{
					hover = ((Component)(object)((RaycastHit)(ref val)).get_collider()).gameObject;
				}
			}
			break;
		}
	}

	private void Interact(GameObject go, bool hold)
	{
		if (InAttack() || InDodge() || (hold && Time.time - m_lastHoverInteractTime < 0.2f))
		{
			return;
		}
		Interactable componentInParent = go.GetComponentInParent<Interactable>();
		if (componentInParent != null)
		{
			m_lastHoverInteractTime = Time.time;
			if (componentInParent.Interact(this, hold))
			{
				Vector3 forward = go.transform.position - base.transform.position;
				forward.y = 0f;
				forward.Normalize();
				base.transform.rotation = Quaternion.LookRotation(forward);
				m_zanim.SetTrigger("interact");
			}
		}
	}

	private void UpdateStations(float dt)
	{
		m_stationDiscoverTimer += dt;
		if (m_stationDiscoverTimer > 1f)
		{
			m_stationDiscoverTimer = 0f;
			CraftingStation.UpdateKnownStationsInRange(this);
		}
		if (!(m_currentStation != null))
		{
			return;
		}
		if (!m_currentStation.InUseDistance(this))
		{
			InventoryGui.instance.Hide();
			SetCraftingStation(null);
			return;
		}
		if (!InventoryGui.IsVisible())
		{
			SetCraftingStation(null);
			return;
		}
		m_currentStation.PokeInUse();
		if ((bool)m_currentStation && !AlwaysRotateCamera())
		{
			Vector3 normalized = (m_currentStation.transform.position - base.transform.position).normalized;
			normalized.y = 0f;
			normalized.Normalize();
			Quaternion to = Quaternion.LookRotation(normalized);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, m_turnSpeed * dt);
		}
	}

	private void UpdateCover(float dt)
	{
		m_updateCoverTimer += dt;
		if (m_updateCoverTimer > 1f)
		{
			m_updateCoverTimer = 0f;
			Cover.GetCoverForPoint(GetCenterPoint(), ref m_coverPercentage, ref m_underRoof);
		}
	}

	public Character GetHoverCreature()
	{
		return m_hoveringCreature;
	}

	public override GameObject GetHoverObject()
	{
		return m_hovering;
	}

	public override void OnNearFire(Vector3 point)
	{
		m_nearFireTimer = 0f;
	}

	public bool InShelter()
	{
		if (m_coverPercentage >= 0.8f)
		{
			return m_underRoof;
		}
		return false;
	}

	public float GetStamina()
	{
		return m_stamina;
	}

	public override float GetMaxStamina()
	{
		return m_maxStamina;
	}

	public override float GetStaminaPercentage()
	{
		return m_stamina / m_maxStamina;
	}

	public void SetGodMode(bool godMode)
	{
		m_godMode = godMode;
	}

	public override bool InGodMode()
	{
		return m_godMode;
	}

	public void SetGhostMode(bool ghostmode)
	{
		m_ghostMode = ghostmode;
	}

	public override bool InGhostMode()
	{
		return m_ghostMode;
	}

	public override bool IsDebugFlying()
	{
		if (m_nview.IsOwner())
		{
			return m_debugFly;
		}
		return m_nview.GetZDO().GetBool("DebugFly");
	}

	public override void AddStamina(float v)
	{
		m_stamina += v;
		if (m_stamina > m_maxStamina)
		{
			m_stamina = m_maxStamina;
		}
	}

	public override void UseStamina(float v)
	{
		if (v != 0f && m_nview.IsValid())
		{
			if (m_nview.IsOwner())
			{
				RPC_UseStamina(0L, v);
				return;
			}
			m_nview.InvokeRPC("UseStamina", v);
		}
	}

	private void RPC_UseStamina(long sender, float v)
	{
		if (v != 0f)
		{
			m_stamina -= v;
			if (m_stamina < 0f)
			{
				m_stamina = 0f;
			}
			m_staminaRegenTimer = m_staminaRegenDelay;
		}
	}

	public override bool HaveStamina(float amount = 0f)
	{
		if (m_nview.IsValid() && !m_nview.IsOwner())
		{
			return m_nview.GetZDO().GetFloat("stamina", m_maxStamina) > amount;
		}
		return m_stamina > amount;
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(24);
		pkg.Write(GetMaxHealth());
		pkg.Write(GetHealth());
		pkg.Write(GetMaxStamina());
		pkg.Write(m_firstSpawn);
		pkg.Write(m_timeSinceDeath);
		pkg.Write(m_guardianPower);
		pkg.Write(m_guardianPowerCooldown);
		m_inventory.Save(pkg);
		pkg.Write(m_knownRecipes.Count);
		foreach (string knownRecipe in m_knownRecipes)
		{
			pkg.Write(knownRecipe);
		}
		pkg.Write(m_knownStations.Count);
		foreach (KeyValuePair<string, int> knownStation in m_knownStations)
		{
			pkg.Write(knownStation.Key);
			pkg.Write(knownStation.Value);
		}
		pkg.Write(m_knownMaterial.Count);
		foreach (string item in m_knownMaterial)
		{
			pkg.Write(item);
		}
		pkg.Write(m_shownTutorials.Count);
		foreach (string shownTutorial in m_shownTutorials)
		{
			pkg.Write(shownTutorial);
		}
		pkg.Write(m_uniques.Count);
		foreach (string unique in m_uniques)
		{
			pkg.Write(unique);
		}
		pkg.Write(m_trophies.Count);
		foreach (string trophy in m_trophies)
		{
			pkg.Write(trophy);
		}
		pkg.Write(m_knownBiome.Count);
		foreach (Heightmap.Biome item2 in m_knownBiome)
		{
			pkg.Write((int)item2);
		}
		pkg.Write(m_knownTexts.Count);
		foreach (KeyValuePair<string, string> knownText in m_knownTexts)
		{
			pkg.Write(knownText.Key);
			pkg.Write(knownText.Value);
		}
		pkg.Write(m_beardItem);
		pkg.Write(m_hairItem);
		pkg.Write(m_skinColor);
		pkg.Write(m_hairColor);
		pkg.Write(m_modelIndex);
		pkg.Write(m_foods.Count);
		foreach (Food food in m_foods)
		{
			pkg.Write(food.m_name);
			pkg.Write(food.m_health);
			pkg.Write(food.m_stamina);
		}
		m_skills.Save(pkg);
	}

	public void Load(ZPackage pkg)
	{
		m_isLoading = true;
		UnequipAllItems();
		int num = pkg.ReadInt();
		if (num >= 7)
		{
			SetMaxHealth(pkg.ReadSingle(), flashBar: false);
		}
		float num2 = pkg.ReadSingle();
		float maxHealth = GetMaxHealth();
		if (num2 <= 0f || num2 > maxHealth || float.IsNaN(num2))
		{
			num2 = maxHealth;
		}
		SetHealth(num2);
		if (num >= 10)
		{
			float stamina = pkg.ReadSingle();
			SetMaxStamina(stamina, flashBar: false);
			m_stamina = stamina;
		}
		if (num >= 8)
		{
			m_firstSpawn = pkg.ReadBool();
		}
		if (num >= 20)
		{
			m_timeSinceDeath = pkg.ReadSingle();
		}
		if (num >= 23)
		{
			string guardianPower = pkg.ReadString();
			SetGuardianPower(guardianPower);
		}
		if (num >= 24)
		{
			m_guardianPowerCooldown = pkg.ReadSingle();
		}
		if (num == 2)
		{
			pkg.ReadZDOID();
		}
		m_inventory.Load(pkg);
		int num3 = pkg.ReadInt();
		for (int i = 0; i < num3; i++)
		{
			string item = pkg.ReadString();
			m_knownRecipes.Add(item);
		}
		if (num < 15)
		{
			int num4 = pkg.ReadInt();
			for (int j = 0; j < num4; j++)
			{
				pkg.ReadString();
			}
		}
		else
		{
			int num5 = pkg.ReadInt();
			for (int k = 0; k < num5; k++)
			{
				string key = pkg.ReadString();
				int value = pkg.ReadInt();
				m_knownStations.Add(key, value);
			}
		}
		int num6 = pkg.ReadInt();
		for (int l = 0; l < num6; l++)
		{
			string item2 = pkg.ReadString();
			m_knownMaterial.Add(item2);
		}
		if (num < 19 || num >= 21)
		{
			int num7 = pkg.ReadInt();
			for (int m = 0; m < num7; m++)
			{
				string item3 = pkg.ReadString();
				m_shownTutorials.Add(item3);
			}
		}
		if (num >= 6)
		{
			int num8 = pkg.ReadInt();
			for (int n = 0; n < num8; n++)
			{
				string item4 = pkg.ReadString();
				m_uniques.Add(item4);
			}
		}
		if (num >= 9)
		{
			int num9 = pkg.ReadInt();
			for (int num10 = 0; num10 < num9; num10++)
			{
				string item5 = pkg.ReadString();
				m_trophies.Add(item5);
			}
		}
		if (num >= 18)
		{
			int num11 = pkg.ReadInt();
			for (int num12 = 0; num12 < num11; num12++)
			{
				Heightmap.Biome item6 = (Heightmap.Biome)pkg.ReadInt();
				m_knownBiome.Add(item6);
			}
		}
		if (num >= 22)
		{
			int num13 = pkg.ReadInt();
			for (int num14 = 0; num14 < num13; num14++)
			{
				string key2 = pkg.ReadString();
				string value2 = pkg.ReadString();
				m_knownTexts.Add(key2, value2);
			}
		}
		if (num >= 4)
		{
			string beard = pkg.ReadString();
			string hair = pkg.ReadString();
			SetBeard(beard);
			SetHair(hair);
		}
		if (num >= 5)
		{
			Vector3 skinColor = pkg.ReadVector3();
			Vector3 hairColor = pkg.ReadVector3();
			SetSkinColor(skinColor);
			SetHairColor(hairColor);
		}
		if (num >= 11)
		{
			int playerModel = pkg.ReadInt();
			SetPlayerModel(playerModel);
		}
		if (num >= 12)
		{
			m_foods.Clear();
			int num15 = pkg.ReadInt();
			for (int num16 = 0; num16 < num15; num16++)
			{
				if (num >= 14)
				{
					Food food = new Food();
					food.m_name = pkg.ReadString();
					food.m_health = pkg.ReadSingle();
					if (num >= 16)
					{
						food.m_stamina = pkg.ReadSingle();
					}
					GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(food.m_name);
					if (itemPrefab == null)
					{
						ZLog.LogWarning((object)("FAiled to find food item " + food.m_name));
						continue;
					}
					food.m_item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
					m_foods.Add(food);
				}
				else
				{
					pkg.ReadString();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					if (num >= 13)
					{
						pkg.ReadSingle();
					}
				}
			}
		}
		if (num >= 17)
		{
			m_skills.Load(pkg);
		}
		m_isLoading = false;
		UpdateAvailablePiecesList();
		EquipIventoryItems();
	}

	private void EquipIventoryItems()
	{
		foreach (ItemDrop.ItemData equipedtem in m_inventory.GetEquipedtems())
		{
			if (!EquipItem(equipedtem, triggerEquipEffects: false))
			{
				equipedtem.m_equiped = false;
			}
		}
	}

	public override bool CanMove()
	{
		if (m_teleporting)
		{
			return false;
		}
		if (InCutscene())
		{
			return false;
		}
		if (IsEncumbered() && !HaveStamina())
		{
			return false;
		}
		return base.CanMove();
	}

	public override bool IsEncumbered()
	{
		return m_inventory.GetTotalWeight() > GetMaxCarryWeight();
	}

	public float GetMaxCarryWeight()
	{
		float limit = m_maxCarryWeight;
		m_seman.ModifyMaxCarryWeight(limit, ref limit);
		return limit;
	}

	public override bool HaveUniqueKey(string name)
	{
		return m_uniques.Contains(name);
	}

	public override void AddUniqueKey(string name)
	{
		if (!m_uniques.Contains(name))
		{
			m_uniques.Add(name);
		}
	}

	public bool IsBiomeKnown(Heightmap.Biome biome)
	{
		return m_knownBiome.Contains(biome);
	}

	public void AddKnownBiome(Heightmap.Biome biome)
	{
		if (!m_knownBiome.Contains(biome))
		{
			m_knownBiome.Add(biome);
			if (biome != Heightmap.Biome.Meadows && biome != 0)
			{
				string text = "$biome_" + biome.ToString().ToLower();
				MessageHud.instance.ShowBiomeFoundMsg(text, playStinger: true);
			}
			if (biome == Heightmap.Biome.BlackForest && !ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))
			{
				ShowTutorial("blackforest");
			}
			Gogan.LogEvent("Game", "BiomeFound", biome.ToString(), 0L);
		}
	}

	public bool IsRecipeKnown(string name)
	{
		return m_knownRecipes.Contains(name);
	}

	public void AddKnownRecipe(Recipe recipe)
	{
		if (!m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name))
		{
			m_knownRecipes.Add(recipe.m_item.m_itemData.m_shared.m_name);
			MessageHud.instance.QueueUnlockMsg(recipe.m_item.m_itemData.GetIcon(), "$msg_newrecipe", recipe.m_item.m_itemData.m_shared.m_name);
			Gogan.LogEvent("Game", "RecipeFound", recipe.m_item.m_itemData.m_shared.m_name, 0L);
		}
	}

	public void AddKnownPiece(Piece piece)
	{
		if (!m_knownRecipes.Contains(piece.m_name))
		{
			m_knownRecipes.Add(piece.m_name);
			MessageHud.instance.QueueUnlockMsg(piece.m_icon, "$msg_newpiece", piece.m_name);
			Gogan.LogEvent("Game", "PieceFound", piece.m_name, 0L);
		}
	}

	public void AddKnownStation(CraftingStation station)
	{
		int level = station.GetLevel();
		if (m_knownStations.TryGetValue(station.m_name, out var value))
		{
			if (value < level)
			{
				m_knownStations[station.m_name] = level;
				MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation_level", station.m_name + " $msg_level " + level);
				UpdateKnownRecipesList();
			}
		}
		else
		{
			m_knownStations.Add(station.m_name, level);
			MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation", station.m_name);
			Gogan.LogEvent("Game", "StationFound", station.m_name, 0L);
			UpdateKnownRecipesList();
		}
	}

	private bool KnowStationLevel(string name, int level)
	{
		if (m_knownStations.TryGetValue(name, out var value))
		{
			return value >= level;
		}
		return false;
	}

	public void AddKnownText(string label, string text)
	{
		if (label.Length == 0)
		{
			ZLog.LogWarning((object)("Text " + text + " Is missing label"));
		}
		else if (!m_knownTexts.ContainsKey(label))
		{
			m_knownTexts.Add(label, text);
			Message(MessageHud.MessageType.TopLeft, Localization.get_instance().Localize("$msg_newtext", new string[1]
			{
				label
			}), 0, m_textIcon);
		}
	}

	public List<KeyValuePair<string, string>> GetKnownTexts()
	{
		return m_knownTexts.ToList();
	}

	public void AddKnownItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
		{
			AddTrophie(item);
		}
		if (!m_knownMaterial.Contains(item.m_shared.m_name))
		{
			m_knownMaterial.Add(item.m_shared.m_name);
			if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newmaterial", item.m_shared.m_name);
			}
			else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newtrophy", item.m_shared.m_name);
			}
			else
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newitem", item.m_shared.m_name);
			}
			Gogan.LogEvent("Game", "ItemFound", item.m_shared.m_name, 0L);
			UpdateKnownRecipesList();
		}
	}

	private void AddTrophie(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie && !m_trophies.Contains(item.m_dropPrefab.name))
		{
			m_trophies.Add(item.m_dropPrefab.name);
		}
	}

	public List<string> GetTrophies()
	{
		List<string> list = new List<string>();
		list.AddRange(m_trophies);
		return list;
	}

	private void UpdateKnownRecipesList()
	{
		if (Game.instance == null)
		{
			return;
		}
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			if (recipe.m_enabled && !m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && HaveRequirements(recipe, discover: true, 0))
			{
				AddKnownRecipe(recipe);
			}
		}
		m_tempOwnedPieceTables.Clear();
		m_inventory.GetAllPieceTables(m_tempOwnedPieceTables);
		bool flag = false;
		foreach (PieceTable tempOwnedPieceTable in m_tempOwnedPieceTables)
		{
			foreach (GameObject piece in tempOwnedPieceTable.m_pieces)
			{
				Piece component = piece.GetComponent<Piece>();
				if (component.m_enabled && !m_knownRecipes.Contains(component.m_name) && HaveRequirements(component, RequirementMode.IsKnown))
				{
					AddKnownPiece(component);
					flag = true;
				}
			}
		}
		if (flag)
		{
			UpdateAvailablePiecesList();
		}
	}

	private void UpdateAvailablePiecesList()
	{
		if (m_buildPieces != null)
		{
			m_buildPieces.UpdateAvailable(m_knownRecipes, this, m_hideUnavailable, m_noPlacementCost);
		}
		SetupPlacementGhost();
	}

	public override void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return;
		}
		if (m_nview.IsOwner())
		{
			if ((bool)MessageHud.instance)
			{
				MessageHud.instance.ShowMessage(type, msg, amount, icon);
			}
		}
		else
		{
			m_nview.InvokeRPC("Message", (int)type, msg, amount);
		}
	}

	private void RPC_Message(long sender, int type, string msg, int amount)
	{
		if (m_nview.IsOwner() && (bool)MessageHud.instance)
		{
			MessageHud.instance.ShowMessage((MessageHud.MessageType)type, msg, amount);
		}
	}

	public static Player GetPlayer(long playerID)
	{
		foreach (Player player in m_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return player;
			}
		}
		return null;
	}

	public static Player GetClosestPlayer(Vector3 point, float maxRange)
	{
		Player result = null;
		float num = 999999f;
		foreach (Player player in m_players)
		{
			float num2 = Vector3.Distance(player.transform.position, point);
			if (num2 < num && num2 < maxRange)
			{
				num = num2;
				result = player;
			}
		}
		return result;
	}

	public static bool IsPlayerInRange(Vector3 point, float range, long playerID)
	{
		foreach (Player player in m_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return Utils.DistanceXZ(player.transform.position, point) < range;
			}
		}
		return false;
	}

	public static void MessageAllInRange(Vector3 point, float range, MessageHud.MessageType type, string msg, Sprite icon = null)
	{
		foreach (Player player in m_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				player.Message(type, msg, 0, icon);
			}
		}
	}

	public static int GetPlayersInRangeXZ(Vector3 point, float range)
	{
		int num = 0;
		foreach (Player player in m_players)
		{
			if (Utils.DistanceXZ(player.transform.position, point) < range)
			{
				num++;
			}
		}
		return num;
	}

	public static void GetPlayersInRange(Vector3 point, float range, List<Player> players)
	{
		foreach (Player player in m_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				players.Add(player);
			}
		}
	}

	public static bool IsPlayerInRange(Vector3 point, float range)
	{
		foreach (Player player in m_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsPlayerInRange(Vector3 point, float range, float minNoise)
	{
		foreach (Player player in m_players)
		{
			if (Vector3.Distance(player.transform.position, point) < range)
			{
				float noiseRange = player.GetNoiseRange();
				if (range <= noiseRange && noiseRange >= minNoise)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Player GetPlayerNoiseRange(Vector3 point, float noiseRangeScale = 1f)
	{
		foreach (Player player in m_players)
		{
			float num = Vector3.Distance(player.transform.position, point);
			float noiseRange = player.GetNoiseRange();
			if (num < noiseRange * noiseRangeScale)
			{
				return player;
			}
		}
		return null;
	}

	public static List<Player> GetAllPlayers()
	{
		return m_players;
	}

	public static Player GetRandomPlayer()
	{
		if (m_players.Count == 0)
		{
			return null;
		}
		return m_players[UnityEngine.Random.Range(0, m_players.Count)];
	}

	public void GetAvailableRecipes(ref List<Recipe> available)
	{
		available.Clear();
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			if (recipe.m_enabled && (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && (m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) || m_noPlacementCost) && (RequiredCraftingStation(recipe, 1, checkLevel: false) || m_noPlacementCost))
			{
				available.Add(recipe);
			}
		}
	}

	private void OnInventoryChanged()
	{
		if (m_isLoading)
		{
			return;
		}
		foreach (ItemDrop.ItemData allItem in m_inventory.GetAllItems())
		{
			AddKnownItem(allItem);
			if (allItem.m_shared.m_name == "$item_hammer")
			{
				ShowTutorial("hammer");
			}
			else if (allItem.m_shared.m_name == "$item_hoe")
			{
				ShowTutorial("hoe");
			}
			else if (allItem.m_shared.m_name == "$item_pickaxe_antler")
			{
				ShowTutorial("pickaxe");
			}
			if (allItem.m_shared.m_name == "$item_trophy_eikthyr")
			{
				ShowTutorial("boss_trophy");
			}
			if (allItem.m_shared.m_name == "$item_wishbone")
			{
				ShowTutorial("wishbone");
			}
			else if (allItem.m_shared.m_name == "$item_copperore" || allItem.m_shared.m_name == "$item_tinore")
			{
				ShowTutorial("ore");
			}
			else if (allItem.m_shared.m_food > 0f)
			{
				ShowTutorial("food");
			}
		}
		UpdateKnownRecipesList();
		UpdateAvailablePiecesList();
	}

	public bool InDebugFlyMode()
	{
		return m_debugFly;
	}

	public void ShowTutorial(string name, bool force = false)
	{
		if (!HaveSeenTutorial(name))
		{
			Tutorial.instance.ShowText(name, force);
		}
	}

	public void SetSeenTutorial(string name)
	{
		if (name.Length != 0 && !m_shownTutorials.Contains(name))
		{
			m_shownTutorials.Add(name);
		}
	}

	public bool HaveSeenTutorial(string name)
	{
		if (name.Length == 0)
		{
			return false;
		}
		return m_shownTutorials.Contains(name);
	}

	public static bool IsSeenTutorialsCleared()
	{
		if ((bool)m_localPlayer)
		{
			return m_localPlayer.m_shownTutorials.Count == 0;
		}
		return true;
	}

	public static void ResetSeenTutorials()
	{
		if ((bool)m_localPlayer)
		{
			m_localPlayer.m_shownTutorials.Clear();
		}
	}

	public void SetMouseLook(Vector2 mouseLook)
	{
		m_lookYaw *= Quaternion.Euler(0f, mouseLook.x, 0f);
		m_lookPitch = Mathf.Clamp(m_lookPitch - mouseLook.y, -89f, 89f);
		UpdateEyeRotation();
		m_lookDir = m_eye.forward;
	}

	protected override void UpdateEyeRotation()
	{
		m_eye.rotation = m_lookYaw * Quaternion.Euler(m_lookPitch, 0f, 0f);
	}

	public Ragdoll GetRagdoll()
	{
		return m_ragdoll;
	}

	public void OnDodgeMortal()
	{
		m_dodgeInvincible = false;
	}

	private void UpdateDodge(float dt)
	{
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		m_queuedDodgeTimer -= dt;
		if (m_queuedDodgeTimer > 0f && IsOnGround() && !IsDead() && !InAttack() && !IsEncumbered() && !InDodge())
		{
			float num = m_dodgeStaminaUsage - m_dodgeStaminaUsage * m_equipmentMovementModifier;
			if (HaveStamina(num))
			{
				AbortEquipQueue();
				m_queuedDodgeTimer = 0f;
				m_dodgeInvincible = true;
				base.transform.rotation = Quaternion.LookRotation(m_queuedDodgeDir);
				m_body.set_rotation(base.transform.rotation);
				m_zanim.SetTrigger("dodge");
				AddNoise(5f);
				UseStamina(num);
				m_dodgeEffects.Create(base.transform.position, Quaternion.identity, base.transform);
			}
			else
			{
				Hud.instance.StaminaBarNoStaminaFlash();
			}
		}
		AnimatorStateInfo currentAnimatorStateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
		AnimatorStateInfo nextAnimatorStateInfo = m_animator.GetNextAnimatorStateInfo(0);
		bool flag = m_animator.IsInTransition(0);
		bool flag2 = m_animator.GetBool("dodge") || (((AnimatorStateInfo)(ref currentAnimatorStateInfo)).get_tagHash() == m_animatorTagDodge && !flag) || (flag && ((AnimatorStateInfo)(ref nextAnimatorStateInfo)).get_tagHash() == m_animatorTagDodge);
		bool value = flag2 && m_dodgeInvincible;
		m_nview.GetZDO().Set("dodgeinv", value);
		m_inDodge = flag2;
	}

	public override bool IsDodgeInvincible()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetBool("dodgeinv");
	}

	public override bool InDodge()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return false;
		}
		return m_inDodge;
	}

	public override bool IsDead()
	{
		return m_nview.GetZDO()?.GetBool("dead") ?? false;
	}

	protected void Dodge(Vector3 dodgeDir)
	{
		m_queuedDodgeTimer = 0.5f;
		m_queuedDodgeDir = dodgeDir;
	}

	public override bool AlwaysRotateCamera()
	{
		if ((GetCurrentWeapon() != null && m_currentAttack != null && m_lastCombatTimer < 1f && m_currentAttack.m_attackType != Attack.AttackType.None && ZInput.IsMouseActive()) || IsHoldingAttack() || m_blocking)
		{
			return true;
		}
		if (InPlaceMode())
		{
			Vector3 from = GetLookYaw() * Vector3.forward;
			Vector3 forward = base.transform.forward;
			if (Vector3.Angle(from, forward) > 90f)
			{
				return true;
			}
		}
		return false;
	}

	public override bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (IsTeleporting())
		{
			return false;
		}
		if (m_teleportCooldown < 2f)
		{
			return false;
		}
		m_teleporting = true;
		m_distantTeleport = distantTeleport;
		m_teleportTimer = 0f;
		m_teleportCooldown = 0f;
		m_teleportFromPos = base.transform.position;
		m_teleportFromRot = base.transform.rotation;
		m_teleportTargetPos = pos;
		m_teleportTargetRot = rot;
		return true;
	}

	private void UpdateTeleport(float dt)
	{
		if (!m_teleporting)
		{
			m_teleportCooldown += dt;
			return;
		}
		m_teleportCooldown = 0f;
		m_teleportTimer += dt;
		if (!(m_teleportTimer > 2f))
		{
			return;
		}
		Vector3 lookDir = m_teleportTargetRot * Vector3.forward;
		base.transform.position = m_teleportTargetPos;
		base.transform.rotation = m_teleportTargetRot;
		m_body.set_velocity(Vector3.zero);
		m_maxAirAltitude = base.transform.position.y;
		SetLookDir(lookDir);
		if ((!(m_teleportTimer > 8f) && m_distantTeleport) || !ZNetScene.instance.IsAreaReady(m_teleportTargetPos))
		{
			return;
		}
		float height = 0f;
		if (ZoneSystem.instance.FindFloor(m_teleportTargetPos, out height))
		{
			m_teleportTimer = 0f;
			m_teleporting = false;
			ResetCloth();
		}
		else if (m_teleportTimer > 15f || !m_distantTeleport)
		{
			if (m_distantTeleport)
			{
				Vector3 position = base.transform.position;
				position.y = ZoneSystem.instance.GetSolidHeight(m_teleportTargetPos) + 0.5f;
				base.transform.position = position;
			}
			else
			{
				base.transform.rotation = m_teleportFromRot;
				base.transform.position = m_teleportFromPos;
				m_maxAirAltitude = base.transform.position.y;
				Message(MessageHud.MessageType.Center, "$msg_portal_blocked");
			}
			m_teleportTimer = 0f;
			m_teleporting = false;
			ResetCloth();
		}
	}

	public override bool IsTeleporting()
	{
		return m_teleporting;
	}

	public bool ShowTeleportAnimation()
	{
		if (m_teleporting)
		{
			return m_distantTeleport;
		}
		return false;
	}

	public void SetPlayerModel(int index)
	{
		if (m_modelIndex != index)
		{
			m_modelIndex = index;
			m_visEquipment.SetModel(index);
		}
	}

	public int GetPlayerModel()
	{
		return m_modelIndex;
	}

	public void SetSkinColor(Vector3 color)
	{
		if (!(color == m_skinColor))
		{
			m_skinColor = color;
			m_visEquipment.SetSkinColor(m_skinColor);
		}
	}

	public void SetHairColor(Vector3 color)
	{
		if (!(m_hairColor == color))
		{
			m_hairColor = color;
			m_visEquipment.SetHairColor(m_hairColor);
		}
	}

	protected override void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
	{
		base.SetupVisEquipment(visEq, isRagdoll);
		visEq.SetModel(m_modelIndex);
		visEq.SetSkinColor(m_skinColor);
		visEq.SetHairColor(m_hairColor);
	}

	public override bool CanConsumeItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
		{
			return false;
		}
		if (item.m_shared.m_food > 0f && !CanEat(item, showMessages: true))
		{
			return false;
		}
		if ((bool)item.m_shared.m_consumeStatusEffect)
		{
			StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
			if (m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.name) || m_seman.HaveStatusEffectCategory(consumeStatusEffect.m_category))
			{
				Message(MessageHud.MessageType.Center, "$msg_cantconsume");
				return false;
			}
		}
		return true;
	}

	public override bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item)
	{
		if (!CanConsumeItem(item))
		{
			return false;
		}
		if ((bool)item.m_shared.m_consumeStatusEffect)
		{
			_ = item.m_shared.m_consumeStatusEffect;
			m_seman.AddStatusEffect(item.m_shared.m_consumeStatusEffect, resetTime: true);
		}
		if (item.m_shared.m_food > 0f)
		{
			EatFood(item);
		}
		inventory.RemoveOneItem(item);
		return true;
	}

	public void SetIntro(bool intro)
	{
		if (m_intro != intro)
		{
			m_intro = intro;
			m_zanim.SetBool("intro", intro);
		}
	}

	public override bool InIntro()
	{
		return m_intro;
	}

	public override bool InCutscene()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		AnimatorStateInfo currentAnimatorStateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
		if (((AnimatorStateInfo)(ref currentAnimatorStateInfo)).get_tagHash() == m_animatorTagCutscene)
		{
			return true;
		}
		if (InIntro())
		{
			return true;
		}
		if (m_sleeping)
		{
			return true;
		}
		return base.InCutscene();
	}

	public void SetMaxStamina(float stamina, bool flashBar)
	{
		if (flashBar && Hud.instance != null && stamina > m_maxStamina)
		{
			Hud.instance.StaminaBarUppgradeFlash();
		}
		m_maxStamina = stamina;
		m_stamina = Mathf.Clamp(m_stamina, 0f, m_maxStamina);
	}

	public void SetMaxHealth(float health, bool flashBar)
	{
		if (flashBar && Hud.instance != null && health > GetMaxHealth())
		{
			Hud.instance.FlashHealthBar();
		}
		SetMaxHealth(health);
	}

	public override bool IsPVPEnabled()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (m_nview.IsOwner())
		{
			return m_pvp;
		}
		return m_nview.GetZDO().GetBool("pvp");
	}

	public void SetPVP(bool enabled)
	{
		if (m_pvp != enabled)
		{
			m_pvp = enabled;
			m_nview.GetZDO().Set("pvp", m_pvp);
			if (m_pvp)
			{
				Message(MessageHud.MessageType.Center, "$msg_pvpon");
			}
			else
			{
				Message(MessageHud.MessageType.Center, "$msg_pvpoff");
			}
		}
	}

	public bool CanSwitchPVP()
	{
		return m_lastCombatTimer > 10f;
	}

	public bool NoCostCheat()
	{
		return m_noPlacementCost;
	}

	public void StartEmote(string emote, bool oneshot = true)
	{
		if (CanMove() && !InAttack() && !IsHoldingAttack())
		{
			SetCrouch(crouch: false);
			int @int = m_nview.GetZDO().GetInt("emoteID");
			m_nview.GetZDO().Set("emoteID", @int + 1);
			m_nview.GetZDO().Set("emote", emote);
			m_nview.GetZDO().Set("emote_oneshot", oneshot);
		}
	}

	protected override void StopEmote()
	{
		if (m_nview.GetZDO().GetString("emote") != "")
		{
			int @int = m_nview.GetZDO().GetInt("emoteID");
			m_nview.GetZDO().Set("emoteID", @int + 1);
			m_nview.GetZDO().Set("emote", "");
		}
	}

	private void UpdateEmote()
	{
		if (m_nview.IsOwner() && InEmote() && m_moveDir != Vector3.zero)
		{
			StopEmote();
		}
		int @int = m_nview.GetZDO().GetInt("emoteID");
		if (@int == m_emoteID)
		{
			return;
		}
		m_emoteID = @int;
		if (!string.IsNullOrEmpty(m_emoteState))
		{
			m_animator.SetBool("emote_" + m_emoteState, false);
		}
		m_emoteState = "";
		m_animator.SetTrigger("emote_stop");
		string @string = m_nview.GetZDO().GetString("emote");
		if (!string.IsNullOrEmpty(@string))
		{
			bool @bool = m_nview.GetZDO().GetBool("emote_oneshot");
			m_animator.ResetTrigger("emote_stop");
			if (@bool)
			{
				m_animator.SetTrigger("emote_" + @string);
				return;
			}
			m_emoteState = @string;
			m_animator.SetBool("emote_" + @string, true);
		}
	}

	public override bool InEmote()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(m_emoteState))
		{
			return true;
		}
		AnimatorStateInfo currentAnimatorStateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
		return ((AnimatorStateInfo)(ref currentAnimatorStateInfo)).get_tagHash() == m_animatorTagEmote;
	}

	public override bool IsCrouching()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		AnimatorStateInfo currentAnimatorStateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
		return ((AnimatorStateInfo)(ref currentAnimatorStateInfo)).get_tagHash() == m_animatorTagCrouch;
	}

	private void UpdateCrouch(float dt)
	{
		if (m_crouchToggled)
		{
			if (!HaveStamina() || IsSwiming() || InBed() || InPlaceMode() || m_run || IsBlocking() || IsFlying())
			{
				SetCrouch(crouch: false);
			}
			bool flag = InAttack() || IsHoldingAttack();
			m_zanim.SetBool(crouching, m_crouchToggled && !flag);
		}
		else
		{
			m_zanim.SetBool(crouching, value: false);
		}
	}

	protected override void SetCrouch(bool crouch)
	{
		if (m_crouchToggled != crouch)
		{
			m_crouchToggled = crouch;
		}
	}

	public void SetGuardianPower(string name)
	{
		m_guardianPower = name;
		m_guardianSE = ObjectDB.instance.GetStatusEffect(m_guardianPower);
	}

	public string GetGuardianPowerName()
	{
		return m_guardianPower;
	}

	public void GetGuardianPowerHUD(out StatusEffect se, out float cooldown)
	{
		se = m_guardianSE;
		cooldown = m_guardianPowerCooldown;
	}

	public bool StartGuardianPower()
	{
		if (m_guardianSE == null)
		{
			return false;
		}
		if ((InAttack() && !HaveQueuedChain()) || InDodge() || !CanMove() || IsKnockedBack() || IsStaggering() || InMinorAction())
		{
			return false;
		}
		if (m_guardianPowerCooldown > 0f)
		{
			Message(MessageHud.MessageType.Center, "$hud_powernotready");
			return false;
		}
		m_zanim.SetTrigger("gpower");
		return true;
	}

	public bool ActivateGuardianPower()
	{
		if (m_guardianPowerCooldown > 0f)
		{
			return false;
		}
		if (m_guardianSE == null)
		{
			return false;
		}
		List<Player> list = new List<Player>();
		GetPlayersInRange(base.transform.position, 10f, list);
		foreach (Player item in list)
		{
			item.GetSEMan().AddStatusEffect(m_guardianSE.name, resetTime: true);
		}
		m_guardianPowerCooldown = m_guardianSE.m_cooldown;
		return false;
	}

	private void UpdateGuardianPower(float dt)
	{
		m_guardianPowerCooldown -= dt;
		if (m_guardianPowerCooldown < 0f)
		{
			m_guardianPowerCooldown = 0f;
		}
	}

	public override void AttachStart(Transform attachPoint, bool hideWeapons, bool isBed, string attachAnimation, Vector3 detachOffset)
	{
		if (!m_attached)
		{
			m_attached = true;
			m_attachPoint = attachPoint;
			m_detachOffset = detachOffset;
			m_attachAnimation = attachAnimation;
			m_zanim.SetBool(attachAnimation, value: true);
			m_nview.GetZDO().Set("inBed", isBed);
			if (hideWeapons)
			{
				HideHandItems();
			}
			ResetCloth();
		}
	}

	private void UpdateAttach()
	{
		if (m_attached)
		{
			if (m_attachPoint != null)
			{
				base.transform.position = m_attachPoint.position;
				base.transform.rotation = m_attachPoint.rotation;
				Rigidbody componentInParent = m_attachPoint.GetComponentInParent<Rigidbody>();
				m_body.set_useGravity(false);
				m_body.set_velocity(((UnityEngine.Object)(object)componentInParent) ? componentInParent.GetPointVelocity(base.transform.position) : Vector3.zero);
				m_body.set_angularVelocity(Vector3.zero);
				m_maxAirAltitude = base.transform.position.y;
			}
			else
			{
				AttachStop();
			}
		}
	}

	public override bool IsAttached()
	{
		return m_attached;
	}

	public override bool InBed()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetBool("inBed");
	}

	public override void AttachStop()
	{
		if (!m_sleeping && m_attached)
		{
			if (m_attachPoint != null)
			{
				base.transform.position = m_attachPoint.TransformPoint(m_detachOffset);
			}
			m_body.set_useGravity(true);
			m_attached = false;
			m_attachPoint = null;
			m_zanim.SetBool(m_attachAnimation, value: false);
			m_nview.GetZDO().Set("inBed", value: false);
			ResetCloth();
		}
	}

	public void StartShipControl(ShipControlls shipControl)
	{
		m_shipControl = shipControl;
		ZLog.Log((object)("ship controlls set " + shipControl.GetShip().gameObject.name));
	}

	public void StopShipControl()
	{
		if (m_shipControl != null)
		{
			if ((bool)m_shipControl)
			{
				m_shipControl.OnUseStop(this);
			}
			ZLog.Log((object)"Stop ship controlls");
			m_shipControl = null;
		}
	}

	private void SetShipControl(ref Vector3 moveDir)
	{
		m_shipControl.GetShip().ApplyMovementControlls(moveDir);
		moveDir = Vector3.zero;
	}

	public Ship GetControlledShip()
	{
		if ((bool)m_shipControl)
		{
			return m_shipControl.GetShip();
		}
		return null;
	}

	public ShipControlls GetShipControl()
	{
		return m_shipControl;
	}

	private void UpdateShipControl(float dt)
	{
		if ((bool)m_shipControl)
		{
			Vector3 forward = m_shipControl.GetShip().transform.forward;
			forward.y = 0f;
			forward.Normalize();
			Quaternion to = Quaternion.LookRotation(forward);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, 100f * dt);
			if (Vector3.Distance(m_shipControl.transform.position, base.transform.position) > m_maxInteractDistance)
			{
				StopShipControl();
			}
		}
	}

	public bool IsSleeping()
	{
		return m_sleeping;
	}

	public void SetSleeping(bool sleep)
	{
		if (m_sleeping != sleep)
		{
			m_sleeping = sleep;
			if (!sleep)
			{
				Message(MessageHud.MessageType.Center, "$msg_goodmorning");
				m_seman.AddStatusEffect("Rested", resetTime: true);
			}
		}
	}

	public void SetControls(Vector3 movedir, bool attack, bool attackHold, bool secondaryAttack, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun)
	{
		if ((movedir != Vector3.zero || attack || secondaryAttack || block || blockHold || jump || crouch) && GetControlledShip() == null)
		{
			StopEmote();
			AttachStop();
		}
		if ((bool)m_shipControl)
		{
			SetShipControl(ref movedir);
			if (jump)
			{
				StopShipControl();
			}
		}
		if (run)
		{
			m_walk = false;
		}
		if (!m_autoRun)
		{
			Vector3 lookDir = m_lookDir;
			lookDir.y = 0f;
			lookDir.Normalize();
			m_moveDir = movedir.z * lookDir + movedir.x * Vector3.Cross(Vector3.up, lookDir);
		}
		if (!m_autoRun && autoRun && !InPlaceMode())
		{
			m_autoRun = true;
			SetCrouch(crouch: false);
			m_moveDir = m_lookDir;
			m_moveDir.y = 0f;
			m_moveDir.Normalize();
		}
		else if (m_autoRun)
		{
			if (attack || jump || crouch || movedir != Vector3.zero || InPlaceMode() || attackHold)
			{
				m_autoRun = false;
			}
			else if (autoRun || blockHold)
			{
				m_moveDir = m_lookDir;
				m_moveDir.y = 0f;
				m_moveDir.Normalize();
				blockHold = false;
				block = false;
			}
		}
		m_attack = attack;
		m_attackDraw = attackHold;
		m_secondaryAttack = secondaryAttack;
		m_blocking = blockHold;
		m_run = run;
		if (crouch)
		{
			SetCrouch(!m_crouchToggled);
		}
		if (!jump)
		{
			return;
		}
		if (m_blocking)
		{
			Vector3 dodgeDir = m_moveDir;
			if (dodgeDir.magnitude < 0.1f)
			{
				dodgeDir = -m_lookDir;
				dodgeDir.y = 0f;
				dodgeDir.Normalize();
			}
			Dodge(dodgeDir);
		}
		else if (IsCrouching() || m_crouchToggled)
		{
			Vector3 dodgeDir2 = m_moveDir;
			if (dodgeDir2.magnitude < 0.1f)
			{
				dodgeDir2 = m_lookDir;
				dodgeDir2.y = 0f;
				dodgeDir2.Normalize();
			}
			Dodge(dodgeDir2);
		}
		else
		{
			Jump();
		}
	}

	private void UpdateTargeted(float dt)
	{
		m_timeSinceTargeted += dt;
		m_timeSinceSensed += dt;
	}

	public override void OnTargeted(bool sensed, bool alerted)
	{
		if (sensed)
		{
			if (m_timeSinceSensed > 0.5f)
			{
				m_timeSinceSensed = 0f;
				m_nview.InvokeRPC("OnTargeted", sensed, alerted);
			}
		}
		else if (m_timeSinceTargeted > 0.5f)
		{
			m_timeSinceTargeted = 0f;
			m_nview.InvokeRPC("OnTargeted", sensed, alerted);
		}
	}

	private void RPC_OnTargeted(long sender, bool sensed, bool alerted)
	{
		m_timeSinceTargeted = 0f;
		if (sensed)
		{
			m_timeSinceSensed = 0f;
		}
		if (alerted)
		{
			MusicMan.instance.ResetCombatTimer();
		}
	}

	protected override void OnDamaged(HitData hit)
	{
		base.OnDamaged(hit);
		Hud.instance.DamageFlash();
	}

	public bool IsTargeted()
	{
		return m_timeSinceTargeted < 1f;
	}

	public bool IsSensed()
	{
		return m_timeSinceSensed < 1f;
	}

	protected override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
		if (m_chestItem != null)
		{
			mods.Apply(m_chestItem.m_shared.m_damageModifiers);
		}
		if (m_legItem != null)
		{
			mods.Apply(m_legItem.m_shared.m_damageModifiers);
		}
		if (m_helmetItem != null)
		{
			mods.Apply(m_helmetItem.m_shared.m_damageModifiers);
		}
		if (m_shoulderItem != null)
		{
			mods.Apply(m_shoulderItem.m_shared.m_damageModifiers);
		}
	}

	public override float GetBodyArmor()
	{
		float num = 0f;
		if (m_chestItem != null)
		{
			num += m_chestItem.GetArmor();
		}
		if (m_legItem != null)
		{
			num += m_legItem.GetArmor();
		}
		if (m_helmetItem != null)
		{
			num += m_helmetItem.GetArmor();
		}
		if (m_shoulderItem != null)
		{
			num += m_shoulderItem.GetArmor();
		}
		return num;
	}

	protected override void OnSneaking(float dt)
	{
		float t = Mathf.Pow(m_skills.GetSkillFactor(Skills.SkillType.Sneak), 0.5f);
		float num = Mathf.Lerp(1f, 0.25f, t);
		UseStamina(dt * m_sneakStaminaDrain * num);
		if (!HaveStamina())
		{
			Hud.instance.StaminaBarNoStaminaFlash();
		}
		m_sneakSkillImproveTimer += dt;
		if (m_sneakSkillImproveTimer > 1f)
		{
			m_sneakSkillImproveTimer = 0f;
			if (BaseAI.InStealthRange(this))
			{
				RaiseSkill(Skills.SkillType.Sneak);
			}
			else
			{
				RaiseSkill(Skills.SkillType.Sneak, 0.1f);
			}
		}
	}

	private void UpdateStealth(float dt)
	{
		m_stealthFactorUpdateTimer += dt;
		if (m_stealthFactorUpdateTimer > 0.5f)
		{
			m_stealthFactorUpdateTimer = 0f;
			m_stealthFactorTarget = 0f;
			if (IsCrouching())
			{
				m_lastStealthPosition = base.transform.position;
				float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Sneak);
				float lightFactor = StealthSystem.instance.GetLightFactor(GetCenterPoint());
				m_stealthFactorTarget = Mathf.Lerp(0.5f + lightFactor * 0.5f, 0.2f + lightFactor * 0.4f, skillFactor);
				m_stealthFactorTarget = Mathf.Clamp01(m_stealthFactorTarget);
				m_seman.ModifyStealth(m_stealthFactorTarget, ref m_stealthFactorTarget);
				m_stealthFactorTarget = Mathf.Clamp01(m_stealthFactorTarget);
			}
			else
			{
				m_stealthFactorTarget = 1f;
			}
		}
		m_stealthFactor = Mathf.MoveTowards(m_stealthFactor, m_stealthFactorTarget, dt / 4f);
		m_nview.GetZDO().Set("Stealth", m_stealthFactor);
	}

	public override float GetStealthFactor()
	{
		if (!m_nview.IsValid())
		{
			return 0f;
		}
		if (m_nview.IsOwner())
		{
			return m_stealthFactor;
		}
		return m_nview.GetZDO().GetFloat("Stealth");
	}

	public override bool InAttack()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		if (m_animator.IsInTransition(0))
		{
			AnimatorStateInfo nextAnimatorStateInfo = m_animator.GetNextAnimatorStateInfo(0);
			if (((AnimatorStateInfo)(ref nextAnimatorStateInfo)).get_tagHash() == Humanoid.m_animatorTagAttack)
			{
				return true;
			}
			AnimatorStateInfo nextAnimatorStateInfo2 = m_animator.GetNextAnimatorStateInfo(1);
			if (((AnimatorStateInfo)(ref nextAnimatorStateInfo2)).get_tagHash() == Humanoid.m_animatorTagAttack)
			{
				return true;
			}
			return false;
		}
		AnimatorStateInfo currentAnimatorStateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
		if (((AnimatorStateInfo)(ref currentAnimatorStateInfo)).get_tagHash() == Humanoid.m_animatorTagAttack)
		{
			return true;
		}
		AnimatorStateInfo currentAnimatorStateInfo2 = m_animator.GetCurrentAnimatorStateInfo(1);
		if (((AnimatorStateInfo)(ref currentAnimatorStateInfo2)).get_tagHash() == Humanoid.m_animatorTagAttack)
		{
			return true;
		}
		return false;
	}

	public override float GetEquipmentMovementModifier()
	{
		return m_equipmentMovementModifier;
	}

	protected override float GetJogSpeedFactor()
	{
		return 1f + m_equipmentMovementModifier;
	}

	protected override float GetRunSpeedFactor()
	{
		float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Run);
		return (1f + skillFactor * 0.25f) * (1f + m_equipmentMovementModifier * 1.5f);
	}

	public override bool InMinorAction()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		AnimatorStateInfo val = (m_animator.IsInTransition(1) ? m_animator.GetNextAnimatorStateInfo(1) : m_animator.GetCurrentAnimatorStateInfo(1));
		return ((AnimatorStateInfo)(ref val)).get_tagHash() == m_animatorTagMinorAction;
	}

	public override bool GetRelativePosition(out ZDOID parent, out Vector3 relativePos, out Vector3 relativeVel)
	{
		if (m_attached && (bool)m_attachPoint)
		{
			ZNetView componentInParent = m_attachPoint.GetComponentInParent<ZNetView>();
			if ((bool)componentInParent && componentInParent.IsValid())
			{
				parent = componentInParent.GetZDO().m_uid;
				relativePos = componentInParent.transform.InverseTransformPoint(base.transform.position);
				relativeVel = Vector3.zero;
				return true;
			}
		}
		return base.GetRelativePosition(out parent, out relativePos, out relativeVel);
	}

	public override Skills GetSkills()
	{
		return m_skills;
	}

	public override float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return m_skills.GetRandomSkillFactor(skill);
	}

	public override float GetSkillFactor(Skills.SkillType skill)
	{
		return m_skills.GetSkillFactor(skill);
	}

	protected override void DoDamageCameraShake(HitData hit)
	{
		if ((bool)GameCamera.instance && hit.GetTotalPhysicalDamage() > 0f)
		{
			float num = Mathf.Clamp01(hit.GetTotalPhysicalDamage() / GetMaxHealth());
			GameCamera.instance.AddShake(base.transform.position, 50f, m_baseCameraShake * num, continous: false);
		}
	}

	protected override bool ToggleEquiped(ItemDrop.ItemData item)
	{
		if (item.IsEquipable())
		{
			if (InAttack())
			{
				return true;
			}
			if (item.m_shared.m_equipDuration <= 0f)
			{
				if (IsItemEquiped(item))
				{
					UnequipItem(item);
				}
				else
				{
					EquipItem(item);
				}
			}
			else if (IsItemEquiped(item))
			{
				QueueUnequipItem(item);
			}
			else
			{
				QueueEquipItem(item);
			}
			return true;
		}
		return false;
	}

	public void GetActionProgress(out string name, out float progress)
	{
		if (m_equipQueue.Count > 0)
		{
			EquipQueueData equipQueueData = m_equipQueue[0];
			if (equipQueueData.m_duration > 0.5f)
			{
				if (equipQueueData.m_equip)
				{
					name = "$hud_equipping " + equipQueueData.m_item.m_shared.m_name;
				}
				else
				{
					name = "$hud_unequipping " + equipQueueData.m_item.m_shared.m_name;
				}
				progress = Mathf.Clamp01(equipQueueData.m_time / equipQueueData.m_duration);
				return;
			}
		}
		name = null;
		progress = 0f;
	}

	private void UpdateEquipQueue(float dt)
	{
		if (m_equipQueuePause > 0f)
		{
			m_equipQueuePause -= dt;
			m_zanim.SetBool("equipping", value: false);
			return;
		}
		m_zanim.SetBool("equipping", m_equipQueue.Count > 0);
		if (m_equipQueue.Count == 0)
		{
			return;
		}
		EquipQueueData equipQueueData = m_equipQueue[0];
		if (equipQueueData.m_time == 0f && equipQueueData.m_duration >= 1f)
		{
			m_equipStartEffects.Create(base.transform.position, Quaternion.identity);
		}
		equipQueueData.m_time += dt;
		if (equipQueueData.m_time > equipQueueData.m_duration)
		{
			m_equipQueue.RemoveAt(0);
			if (equipQueueData.m_equip)
			{
				EquipItem(equipQueueData.m_item);
			}
			else
			{
				UnequipItem(equipQueueData.m_item);
			}
			m_equipQueuePause = 0.3f;
		}
	}

	private void QueueEquipItem(ItemDrop.ItemData item)
	{
		if (item != null)
		{
			if (IsItemQueued(item))
			{
				RemoveFromEquipQueue(item);
				return;
			}
			EquipQueueData equipQueueData = new EquipQueueData();
			equipQueueData.m_item = item;
			equipQueueData.m_equip = true;
			equipQueueData.m_duration = item.m_shared.m_equipDuration;
			m_equipQueue.Add(equipQueueData);
		}
	}

	private void QueueUnequipItem(ItemDrop.ItemData item)
	{
		if (item != null)
		{
			if (IsItemQueued(item))
			{
				RemoveFromEquipQueue(item);
				return;
			}
			EquipQueueData equipQueueData = new EquipQueueData();
			equipQueueData.m_item = item;
			equipQueueData.m_equip = false;
			equipQueueData.m_duration = item.m_shared.m_equipDuration;
			m_equipQueue.Add(equipQueueData);
		}
	}

	public override void AbortEquipQueue()
	{
		m_equipQueue.Clear();
	}

	public override void RemoveFromEquipQueue(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		foreach (EquipQueueData item2 in m_equipQueue)
		{
			if (item2.m_item == item)
			{
				m_equipQueue.Remove(item2);
				break;
			}
		}
	}

	public bool IsItemQueued(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return false;
		}
		foreach (EquipQueueData item2 in m_equipQueue)
		{
			if (item2.m_item == item)
			{
				return true;
			}
		}
		return false;
	}

	public void ResetCharacter()
	{
		m_guardianPowerCooldown = 0f;
		ResetSeenTutorials();
		m_knownRecipes.Clear();
		m_knownStations.Clear();
		m_knownMaterial.Clear();
		m_uniques.Clear();
		m_trophies.Clear();
		m_skills.Clear();
		m_knownBiome.Clear();
		m_knownTexts.Clear();
	}
}
