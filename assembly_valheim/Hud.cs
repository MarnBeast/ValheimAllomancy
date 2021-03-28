using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
	private class PieceIconData
	{
		public GameObject m_go;

		public Image m_icon;

		public GameObject m_marker;

		public GameObject m_upgrade;

		public UITooltip m_tooltip;
	}

	private static Hud m_instance;

	public GameObject m_rootObject;

	public Text m_buildSelection;

	public Text m_pieceDescription;

	public Image m_buildIcon;

	public GameObject m_buildHud;

	public GameObject m_saveIcon;

	public GameObject m_badConnectionIcon;

	public GameObject m_betaText;

	[Header("Piece")]
	public GameObject[] m_requirementItems = new GameObject[0];

	public GameObject[] m_pieceCategoryTabs = new GameObject[0];

	public GameObject m_pieceSelectionWindow;

	public GameObject m_pieceCategoryRoot;

	public RectTransform m_pieceListRoot;

	public RectTransform m_pieceListMask;

	public GameObject m_pieceIconPrefab;

	public UIInputHandler m_closePieceSelectionButton;

	public EffectList m_selectItemEffect = new EffectList();

	public float m_pieceIconSpacing = 64f;

	private float m_pieceBarPosX;

	private float m_pieceBarTargetPosX;

	private Piece.PieceCategory m_lastPieceCategory = Piece.PieceCategory.Max;

	[Header("Health")]
	public RectTransform m_healthBarRoot;

	public RectTransform m_healthPanel;

	private const float m_healthPanelBuffer = 56f;

	private const float m_healthPanelMinSize = 138f;

	public Animator m_healthAnimator;

	public GuiBar m_healthBarFast;

	public GuiBar m_healthBarSlow;

	public Text m_healthText;

	public Text m_healthMaxText;

	[Header("Food")]
	public Image[] m_foodBars;

	public Image[] m_foodIcons;

	public RectTransform m_foodBarRoot;

	public RectTransform m_foodBaseBar;

	public Image m_foodIcon;

	public Color m_foodColorHungry = Color.white;

	public Color m_foodColorFull = Color.white;

	public Text m_foodText;

	[Header("Action bar")]
	public GameObject m_actionBarRoot;

	public GuiBar m_actionProgress;

	public Text m_actionName;

	[Header("Guardian power")]
	public RectTransform m_gpRoot;

	public Text m_gpName;

	public Text m_gpCooldown;

	public Image m_gpIcon;

	[Header("Stamina")]
	public GameObject m_staminaBar;

	public GuiBar m_staminaBarFast;

	public GuiBar m_staminaBarSlow;

	public Animator m_staminaAnimator;

	private float m_staminaBarBorderBuffer = 16f;

	public RectTransform m_staminaBar2Root;

	public GuiBar m_staminaBar2Fast;

	public GuiBar m_staminaBar2Slow;

	[Header("Loading")]
	public CanvasGroup m_loadingScreen;

	public GameObject m_loadingProgress;

	public GameObject m_sleepingProgress;

	public GameObject m_teleportingProgress;

	public Image m_loadingImage;

	public Text m_loadingTip;

	public bool m_useRandomImages = true;

	public string m_loadingImagePath = "/loadingscreens/";

	public int m_loadingImages = 2;

	public List<string> m_loadingTips = new List<string>();

	[Header("Crosshair")]
	public Image m_crosshair;

	public Image m_crosshairBow;

	public Text m_hoverName;

	public RectTransform m_pieceHealthRoot;

	public GuiBar m_pieceHealthBar;

	public Image m_damageScreen;

	[Header("Target")]
	public GameObject m_targetedAlert;

	public GameObject m_targeted;

	public GameObject m_hidden;

	public GuiBar m_stealthBar;

	[Header("Status effect")]
	public RectTransform m_statusEffectListRoot;

	public RectTransform m_statusEffectTemplate;

	public float m_statusEffectSpacing = 55f;

	private List<RectTransform> m_statusEffects = new List<RectTransform>();

	[Header("Ship hud")]
	public GameObject m_shipHudRoot;

	public GameObject m_shipControlsRoot;

	public GameObject m_rudderLeft;

	public GameObject m_rudderRight;

	public GameObject m_rudderSlow;

	public GameObject m_rudderForward;

	public GameObject m_rudderFastForward;

	public GameObject m_rudderBackward;

	public GameObject m_halfSail;

	public GameObject m_fullSail;

	public GameObject m_rudder;

	public RectTransform m_shipWindIndicatorRoot;

	public Image m_shipWindIcon;

	public RectTransform m_shipWindIconRoot;

	public Image m_shipRudderIndicator;

	public Image m_shipRudderIcon;

	[Header("Event")]
	public GameObject m_eventBar;

	public Text m_eventName;

	private bool m_userHidden;

	private CraftingStation m_currentCraftingStation;

	private List<string> m_buildCategoryNames = new List<string>();

	private List<StatusEffect> m_tempStatusEffects = new List<StatusEffect>();

	private List<PieceIconData> m_pieceIcons = new List<PieceIconData>();

	private int m_pieceIconUpdateIndex;

	private bool m_haveSetupLoadScreen;

	private int m_closePieceSelection;

	private Piece m_hoveredPiece;

	public static Hud instance => m_instance;

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Awake()
	{
		m_instance = this;
		m_pieceSelectionWindow.SetActive(value: false);
		((Component)(object)m_loadingScreen).gameObject.SetActive(value: false);
		m_statusEffectTemplate.gameObject.SetActive(value: false);
		m_eventBar.SetActive(value: false);
		m_gpRoot.gameObject.SetActive(value: false);
		m_betaText.SetActive(value: false);
		UIInputHandler closePieceSelectionButton = m_closePieceSelectionButton;
		closePieceSelectionButton.m_onLeftClick = (Action<UIInputHandler>)Delegate.Combine(closePieceSelectionButton.m_onLeftClick, new Action<UIInputHandler>(OnClosePieceSelection));
		UIInputHandler closePieceSelectionButton2 = m_closePieceSelectionButton;
		closePieceSelectionButton2.m_onRightClick = (Action<UIInputHandler>)Delegate.Combine(closePieceSelectionButton2.m_onRightClick, new Action<UIInputHandler>(OnClosePieceSelection));
		if (SteamManager.APP_ID == 1223920)
		{
			m_betaText.SetActive(value: true);
		}
		GameObject[] pieceCategoryTabs = m_pieceCategoryTabs;
		foreach (GameObject gameObject in pieceCategoryTabs)
		{
			m_buildCategoryNames.Add(gameObject.transform.Find("Text").GetComponent<Text>().get_text());
			UIInputHandler component = gameObject.GetComponent<UIInputHandler>();
			component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(OnLeftClickCategory));
		}
	}

	private void SetVisible(bool visible)
	{
		if (visible != IsVisible())
		{
			if (visible)
			{
				m_rootObject.transform.localPosition = new Vector3(0f, 0f, 0f);
			}
			else
			{
				m_rootObject.transform.localPosition = new Vector3(10000f, 0f, 0f);
			}
		}
	}

	private bool IsVisible()
	{
		return m_rootObject.transform.localPosition.x < 1000f;
	}

	private void Update()
	{
		m_saveIcon.SetActive(ZNet.instance != null && ZNet.instance.IsSaving());
		m_badConnectionIcon.SetActive(ZNet.instance != null && ZNet.instance.HasBadConnection() && Mathf.Sin(Time.time * 10f) > 0f);
		Player localPlayer = Player.m_localPlayer;
		UpdateDamageFlash(Time.deltaTime);
		if ((bool)localPlayer)
		{
			if (Input.GetKeyDown(KeyCode.F3) && Input.GetKey(KeyCode.LeftControl))
			{
				m_userHidden = !m_userHidden;
			}
			SetVisible(!m_userHidden && !localPlayer.InCutscene());
			UpdateBuild(localPlayer, forceUpdateAllBuildStatuses: false);
			m_tempStatusEffects.Clear();
			localPlayer.GetSEMan().GetHUDStatusEffects(m_tempStatusEffects);
			UpdateStatusEffects(m_tempStatusEffects);
			UpdateGuardianPower(localPlayer);
			float attackDrawPercentage = localPlayer.GetAttackDrawPercentage();
			UpdateFood(localPlayer);
			UpdateHealth(localPlayer);
			UpdateStamina(localPlayer);
			UpdateStealth(localPlayer, attackDrawPercentage);
			UpdateCrosshair(localPlayer, attackDrawPercentage);
			UpdateEvent(localPlayer);
			UpdateActionProgress(localPlayer);
		}
	}

	private void LateUpdate()
	{
		UpdateBlackScreen(Player.m_localPlayer, Time.deltaTime);
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			UpdateShipHud(localPlayer, Time.deltaTime);
		}
	}

	private float GetFadeDuration(Player player)
	{
		if (player != null)
		{
			if (player.IsDead())
			{
				return 9.5f;
			}
			if (player.IsSleeping())
			{
				return 3f;
			}
		}
		return 1f;
	}

	private void UpdateBlackScreen(Player player, float dt)
	{
		if (player == null || player.IsDead() || player.IsTeleporting() || Game.instance.IsShuttingDown() || player.IsSleeping())
		{
			((Component)(object)m_loadingScreen).gameObject.SetActive(value: true);
			float alpha = m_loadingScreen.get_alpha();
			float fadeDuration = GetFadeDuration(player);
			alpha = Mathf.MoveTowards(alpha, 1f, dt / fadeDuration);
			if (Game.instance.IsShuttingDown())
			{
				alpha = 1f;
			}
			m_loadingScreen.set_alpha(alpha);
			if (player != null && player.IsSleeping())
			{
				m_sleepingProgress.SetActive(value: true);
				m_loadingProgress.SetActive(value: false);
				m_teleportingProgress.SetActive(value: false);
			}
			else if (player != null && player.ShowTeleportAnimation())
			{
				m_loadingProgress.SetActive(value: false);
				m_sleepingProgress.SetActive(value: false);
				m_teleportingProgress.SetActive(value: true);
			}
			else if ((bool)Game.instance && Game.instance.WaitingForRespawn())
			{
				if (!m_haveSetupLoadScreen)
				{
					m_haveSetupLoadScreen = true;
					if (m_useRandomImages)
					{
						string text = string.Concat(str2: UnityEngine.Random.Range(0, m_loadingImages).ToString(), str0: m_loadingImagePath, str1: "loading");
						ZLog.Log((object)("Loading image:" + text));
						m_loadingImage.set_sprite(Resources.Load<Sprite>(text));
					}
					string text2 = m_loadingTips[UnityEngine.Random.Range(0, m_loadingTips.Count)];
					ZLog.Log((object)("tip:" + text2));
					m_loadingTip.set_text(Localization.get_instance().Localize(text2));
				}
				m_loadingProgress.SetActive(value: true);
				m_sleepingProgress.SetActive(value: false);
				m_teleportingProgress.SetActive(value: false);
			}
			else
			{
				m_loadingProgress.SetActive(value: false);
				m_sleepingProgress.SetActive(value: false);
				m_teleportingProgress.SetActive(value: false);
			}
		}
		else
		{
			m_haveSetupLoadScreen = false;
			float fadeDuration2 = GetFadeDuration(player);
			float alpha2 = m_loadingScreen.get_alpha();
			alpha2 = Mathf.MoveTowards(alpha2, 0f, dt / fadeDuration2);
			m_loadingScreen.set_alpha(alpha2);
			if (m_loadingScreen.get_alpha() <= 0f)
			{
				((Component)(object)m_loadingScreen).gameObject.SetActive(value: false);
			}
		}
	}

	private void UpdateShipHud(Player player, float dt)
	{
		Ship controlledShip = player.GetControlledShip();
		if (controlledShip == null)
		{
			m_shipHudRoot.gameObject.SetActive(value: false);
			return;
		}
		Ship.Speed speedSetting = controlledShip.GetSpeedSetting();
		float rudder = controlledShip.GetRudder();
		float rudderValue = controlledShip.GetRudderValue();
		m_shipHudRoot.SetActive(value: true);
		m_rudderSlow.SetActive(speedSetting == Ship.Speed.Slow);
		m_rudderForward.SetActive(speedSetting == Ship.Speed.Half);
		m_rudderFastForward.SetActive(speedSetting == Ship.Speed.Full);
		m_rudderBackward.SetActive(speedSetting == Ship.Speed.Back);
		m_rudderLeft.SetActive(value: false);
		m_rudderRight.SetActive(value: false);
		m_fullSail.SetActive(speedSetting == Ship.Speed.Full);
		m_halfSail.SetActive(speedSetting == Ship.Speed.Half);
		GameObject rudder2 = m_rudder;
		int active;
		switch (speedSetting)
		{
		case Ship.Speed.Stop:
			active = ((Mathf.Abs(rudderValue) > 0.2f) ? 1 : 0);
			break;
		default:
			active = 0;
			break;
		case Ship.Speed.Back:
		case Ship.Speed.Slow:
			active = 1;
			break;
		}
		rudder2.SetActive((byte)active != 0);
		if ((rudder > 0f && rudderValue < 1f) || (rudder < 0f && rudderValue > -1f))
		{
			((Component)(object)m_shipRudderIcon).transform.Rotate(new Vector3(0f, 0f, 200f * (0f - rudder) * dt));
		}
		if (Mathf.Abs(rudderValue) < 0.02f)
		{
			((Component)(object)m_shipRudderIndicator).gameObject.SetActive(value: false);
		}
		else
		{
			((Component)(object)m_shipRudderIndicator).gameObject.SetActive(value: true);
			if (rudderValue > 0f)
			{
				m_shipRudderIndicator.set_fillClockwise(true);
				m_shipRudderIndicator.set_fillAmount(rudderValue * 0.25f);
			}
			else
			{
				m_shipRudderIndicator.set_fillClockwise(false);
				m_shipRudderIndicator.set_fillAmount((0f - rudderValue) * 0.25f);
			}
		}
		float shipYawAngle = controlledShip.GetShipYawAngle();
		m_shipWindIndicatorRoot.localRotation = Quaternion.Euler(0f, 0f, shipYawAngle);
		float windAngle = controlledShip.GetWindAngle();
		m_shipWindIconRoot.localRotation = Quaternion.Euler(0f, 0f, windAngle);
		float windAngleFactor = controlledShip.GetWindAngleFactor();
		((Graphic)m_shipWindIcon).set_color(Color.Lerp(new Color(0.2f, 0.2f, 0.2f, 1f), Color.white, windAngleFactor));
		Camera mainCamera = Utils.GetMainCamera();
		if (!(mainCamera == null))
		{
			m_shipControlsRoot.transform.position = mainCamera.WorldToScreenPoint(controlledShip.m_controlGuiPos.position);
		}
	}

	private void UpdateActionProgress(Player player)
	{
		player.GetActionProgress(out var name, out var progress);
		if (!string.IsNullOrEmpty(name))
		{
			m_actionBarRoot.SetActive(value: true);
			m_actionProgress.SetValue(progress);
			m_actionName.set_text(Localization.get_instance().Localize(name));
		}
		else
		{
			m_actionBarRoot.SetActive(value: false);
		}
	}

	private void UpdateCrosshair(Player player, float bowDrawPercentage)
	{
		GameObject hoverObject = player.GetHoverObject();
		Hoverable hoverable = (hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null);
		if (hoverable != null && !TextViewer.instance.IsVisible())
		{
			m_hoverName.set_text(hoverable.GetHoverText());
			((Graphic)m_crosshair).set_color((m_hoverName.get_text().Length > 0) ? Color.yellow : new Color(1f, 1f, 1f, 0.5f));
		}
		else
		{
			((Graphic)m_crosshair).set_color(new Color(1f, 1f, 1f, 0.5f));
			m_hoverName.set_text("");
		}
		Piece hoveringPiece = player.GetHoveringPiece();
		if ((bool)hoveringPiece)
		{
			WearNTear component = hoveringPiece.GetComponent<WearNTear>();
			if ((bool)component)
			{
				m_pieceHealthRoot.gameObject.SetActive(value: true);
				m_pieceHealthBar.SetValue(component.GetHealthPercentage());
			}
			else
			{
				m_pieceHealthRoot.gameObject.SetActive(value: false);
			}
		}
		else
		{
			m_pieceHealthRoot.gameObject.SetActive(value: false);
		}
		if (bowDrawPercentage > 0f)
		{
			float num = Mathf.Lerp(1f, 0.15f, bowDrawPercentage);
			((Component)(object)m_crosshairBow).gameObject.SetActive(value: true);
			((Component)(object)m_crosshairBow).transform.localScale = new Vector3(num, num, num);
			((Graphic)m_crosshairBow).set_color(Color.Lerp(new Color(1f, 1f, 1f, 0f), Color.yellow, bowDrawPercentage));
		}
		else
		{
			((Component)(object)m_crosshairBow).gameObject.SetActive(value: false);
		}
	}

	private void FixedUpdate()
	{
		UpdatePieceBar(Time.fixedDeltaTime);
	}

	private void UpdateStealth(Player player, float bowDrawPercentage)
	{
		float stealthFactor = player.GetStealthFactor();
		if ((player.IsCrouching() || stealthFactor < 1f) && bowDrawPercentage == 0f)
		{
			if (player.IsSensed())
			{
				m_targetedAlert.SetActive(value: true);
				m_targeted.SetActive(value: false);
				m_hidden.SetActive(value: false);
			}
			else if (player.IsTargeted())
			{
				m_targetedAlert.SetActive(value: false);
				m_targeted.SetActive(value: true);
				m_hidden.SetActive(value: false);
			}
			else
			{
				m_targetedAlert.SetActive(value: false);
				m_targeted.SetActive(value: false);
				m_hidden.SetActive(value: true);
			}
			((Component)(object)m_stealthBar).gameObject.SetActive(value: true);
			m_stealthBar.SetValue(stealthFactor);
		}
		else
		{
			m_targetedAlert.SetActive(value: false);
			m_hidden.SetActive(value: false);
			m_targeted.SetActive(value: false);
			((Component)(object)m_stealthBar).gameObject.SetActive(value: false);
		}
	}

	private void SetHealthBarSize(float size)
	{
		size = Mathf.Ceil(size);
		float size2 = Mathf.Max(size + 56f, 138f);
		m_healthPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size2);
		m_healthBarRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		m_healthBarSlow.SetWidth(size);
		m_healthBarFast.SetWidth(size);
	}

	private void SetStaminaBarSize(float size)
	{
		m_staminaBar2Root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size + m_staminaBarBorderBuffer);
		m_staminaBar2Slow.SetWidth(size);
		m_staminaBar2Fast.SetWidth(size);
	}

	private void UpdateFood(Player player)
	{
		List<Player.Food> foods = player.GetFoods();
		float num = player.GetBaseFoodHP() / 25f * 32f;
		m_foodBaseBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num);
		float num2 = num;
		for (int i = 0; i < m_foodBars.Length; i++)
		{
			Image val = m_foodBars[i];
			Image val2 = m_foodIcons[i];
			if (i < foods.Count)
			{
				((Component)(object)val).gameObject.SetActive(value: true);
				Player.Food food = foods[i];
				float num3 = food.m_health / 25f * 32f;
				((Graphic)val).set_color(food.m_item.m_shared.m_foodColor);
				((Graphic)val).get_rectTransform().anchoredPosition = new Vector2(num2, 0f);
				((Graphic)val).get_rectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Ceil(num3));
				num2 += num3;
				((Component)(object)val2).gameObject.SetActive(value: true);
				val2.set_sprite(food.m_item.GetIcon());
				if (food.CanEatAgain())
				{
					((Graphic)val2).set_color(new Color(1f, 1f, 1f, 0.6f + Mathf.Sin(Time.time * 10f) * 0.4f));
				}
				else
				{
					((Graphic)val2).set_color(Color.white);
				}
			}
			else
			{
				((Component)(object)val).gameObject.SetActive(value: false);
				((Component)(object)val2).gameObject.SetActive(value: false);
			}
		}
		float size = Mathf.Ceil(player.GetMaxHealth() / 25f * 32f);
		m_foodBarRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
	}

	private void UpdateHealth(Player player)
	{
		float maxHealth = player.GetMaxHealth();
		SetHealthBarSize(maxHealth / 25f * 32f);
		float health = player.GetHealth();
		m_healthBarFast.SetMaxValue(maxHealth);
		m_healthBarFast.SetValue(health);
		m_healthBarSlow.SetMaxValue(maxHealth);
		m_healthBarSlow.SetValue(health);
		string text = Mathf.CeilToInt(player.GetHealth()).ToString();
		string text2 = Mathf.CeilToInt(player.GetMaxHealth()).ToString();
		m_healthText.set_text(text.ToString());
		m_healthMaxText.set_text(text2.ToString());
	}

	private void UpdateStamina(Player player)
	{
		float stamina = player.GetStamina();
		float maxStamina = player.GetMaxStamina();
		m_staminaBar.SetActive(value: false);
		m_staminaAnimator.SetBool("Visible", stamina < maxStamina);
		SetStaminaBarSize(player.GetMaxStamina() / 25f * 32f);
		RectTransform rectTransform = m_staminaBar2Root.transform as RectTransform;
		if (m_buildHud.activeSelf || m_shipHudRoot.activeSelf)
		{
			rectTransform.anchoredPosition = new Vector2(0f, 190f);
		}
		else
		{
			rectTransform.anchoredPosition = new Vector2(0f, 130f);
		}
		m_staminaBar2Slow.SetValue(stamina / maxStamina);
		m_staminaBar2Fast.SetValue(stamina / maxStamina);
	}

	public void DamageFlash()
	{
		Color color = ((Graphic)m_damageScreen).get_color();
		color.a = 1f;
		((Graphic)m_damageScreen).set_color(color);
		((Component)(object)m_damageScreen).gameObject.SetActive(value: true);
	}

	private void UpdateDamageFlash(float dt)
	{
		Color color = ((Graphic)m_damageScreen).get_color();
		color.a = Mathf.MoveTowards(color.a, 0f, dt * 4f);
		((Graphic)m_damageScreen).set_color(color);
		if (color.a <= 0f)
		{
			((Component)(object)m_damageScreen).gameObject.SetActive(value: false);
		}
	}

	private void UpdatePieceList(Player player, Vector2Int selectedNr, Piece.PieceCategory category, bool updateAllBuildStatuses)
	{
		List<Piece> buildPieces = player.GetBuildPieces();
		int num = 10;
		int num2 = 5;
		if (buildPieces.Count <= 1)
		{
			num = 1;
			num2 = 1;
		}
		if (m_pieceIcons.Count != num * num2)
		{
			foreach (PieceIconData pieceIcon in m_pieceIcons)
			{
				UnityEngine.Object.Destroy(pieceIcon.m_go);
			}
			m_pieceIcons.Clear();
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(m_pieceIconPrefab, m_pieceListRoot);
					(gameObject.transform as RectTransform).anchoredPosition = new Vector2((float)j * m_pieceIconSpacing, (float)(-i) * m_pieceIconSpacing);
					PieceIconData pieceIconData = new PieceIconData();
					pieceIconData.m_go = gameObject;
					pieceIconData.m_tooltip = gameObject.GetComponent<UITooltip>();
					pieceIconData.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
					pieceIconData.m_marker = gameObject.transform.Find("selected").gameObject;
					pieceIconData.m_upgrade = gameObject.transform.Find("upgrade").gameObject;
					((Graphic)pieceIconData.m_icon).set_color(new Color(1f, 0f, 1f, 0f));
					UIInputHandler component = gameObject.GetComponent<UIInputHandler>();
					component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(OnLeftClickPiece));
					component.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onRightDown, new Action<UIInputHandler>(OnRightClickPiece));
					component.m_onPointerEnter = (Action<UIInputHandler>)Delegate.Combine(component.m_onPointerEnter, new Action<UIInputHandler>(OnHoverPiece));
					component.m_onPointerExit = (Action<UIInputHandler>)Delegate.Combine(component.m_onPointerExit, new Action<UIInputHandler>(OnHoverPieceExit));
					m_pieceIcons.Add(pieceIconData);
				}
			}
		}
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num; l++)
			{
				int num3 = k * num + l;
				PieceIconData pieceIconData2 = m_pieceIcons[num3];
				pieceIconData2.m_marker.SetActive(new Vector2Int(l, k) == selectedNr);
				if (num3 < buildPieces.Count)
				{
					Piece piece = buildPieces[num3];
					pieceIconData2.m_icon.set_sprite(piece.m_icon);
					((Behaviour)(object)pieceIconData2.m_icon).enabled = true;
					pieceIconData2.m_tooltip.m_text = piece.m_name;
					pieceIconData2.m_upgrade.SetActive(piece.m_isUpgrade);
				}
				else
				{
					((Behaviour)(object)pieceIconData2.m_icon).enabled = false;
					pieceIconData2.m_tooltip.m_text = "";
					pieceIconData2.m_upgrade.SetActive(value: false);
				}
			}
		}
		UpdatePieceBuildStatus(buildPieces, player);
		if (updateAllBuildStatuses)
		{
			UpdatePieceBuildStatusAll(buildPieces, player);
		}
		if (m_lastPieceCategory != category)
		{
			m_lastPieceCategory = category;
			m_pieceBarPosX = m_pieceBarTargetPosX;
			UpdatePieceBuildStatusAll(buildPieces, player);
		}
	}

	private void OnLeftClickCategory(UIInputHandler ih)
	{
		for (int i = 0; i < m_pieceCategoryTabs.Length; i++)
		{
			if (m_pieceCategoryTabs[i] == ((Component)(object)ih).gameObject)
			{
				Player.m_localPlayer.SetBuildCategory(i);
				break;
			}
		}
	}

	private void OnLeftClickPiece(UIInputHandler ih)
	{
		SelectPiece(ih);
		HidePieceSelection();
	}

	private void OnRightClickPiece(UIInputHandler ih)
	{
		if (IsQuickPieceSelectEnabled())
		{
			SelectPiece(ih);
			HidePieceSelection();
		}
	}

	private void OnHoverPiece(UIInputHandler ih)
	{
		Vector2Int selectedGrid = GetSelectedGrid(ih);
		if (selectedGrid.x != -1)
		{
			m_hoveredPiece = Player.m_localPlayer.GetPiece(selectedGrid);
		}
	}

	private void OnHoverPieceExit(UIInputHandler ih)
	{
		m_hoveredPiece = null;
	}

	public bool IsQuickPieceSelectEnabled()
	{
		return PlayerPrefs.GetInt("QuickPieceSelect", 0) == 1;
	}

	private Vector2Int GetSelectedGrid(UIInputHandler ih)
	{
		int num = 10;
		int num2 = 5;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int index = i * num + j;
				if (m_pieceIcons[index].m_go == ((Component)(object)ih).gameObject)
				{
					return new Vector2Int(j, i);
				}
			}
		}
		return new Vector2Int(-1, -1);
	}

	private void SelectPiece(UIInputHandler ih)
	{
		Vector2Int selectedGrid = GetSelectedGrid(ih);
		if (selectedGrid.x != -1)
		{
			Player.m_localPlayer.SetSelectedPiece(selectedGrid);
			m_selectItemEffect.Create(base.transform.position, Quaternion.identity);
		}
	}

	private void UpdatePieceBuildStatus(List<Piece> pieces, Player player)
	{
		if (m_pieceIcons.Count != 0)
		{
			if (m_pieceIconUpdateIndex >= m_pieceIcons.Count)
			{
				m_pieceIconUpdateIndex = 0;
			}
			PieceIconData pieceIconData = m_pieceIcons[m_pieceIconUpdateIndex];
			if (m_pieceIconUpdateIndex < pieces.Count)
			{
				Piece piece = pieces[m_pieceIconUpdateIndex];
				bool flag = player.HaveRequirements(piece, Player.RequirementMode.CanBuild);
				((Graphic)pieceIconData.m_icon).set_color(flag ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0f, 1f, 0f));
			}
			m_pieceIconUpdateIndex++;
		}
	}

	private void UpdatePieceBuildStatusAll(List<Piece> pieces, Player player)
	{
		for (int i = 0; i < m_pieceIcons.Count; i++)
		{
			PieceIconData pieceIconData = m_pieceIcons[i];
			if (i < pieces.Count)
			{
				Piece piece = pieces[i];
				bool flag = player.HaveRequirements(piece, Player.RequirementMode.CanBuild);
				((Graphic)pieceIconData.m_icon).set_color(flag ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0f, 1f, 0f));
			}
			else
			{
				((Graphic)pieceIconData.m_icon).set_color(Color.white);
			}
		}
		m_pieceIconUpdateIndex = 0;
	}

	private void UpdatePieceBar(float dt)
	{
		m_pieceBarPosX = Mathf.Lerp(m_pieceBarPosX, m_pieceBarTargetPosX, 0.1f);
		Vector3 vector = m_pieceListRoot.anchoredPosition;
		vector.x = Mathf.Round(m_pieceBarPosX);
	}

	public void TogglePieceSelection()
	{
		m_hoveredPiece = null;
		if (m_pieceSelectionWindow.activeSelf)
		{
			m_pieceSelectionWindow.SetActive(value: false);
			return;
		}
		m_pieceSelectionWindow.SetActive(value: true);
		UpdateBuild(Player.m_localPlayer, forceUpdateAllBuildStatuses: true);
	}

	private void OnClosePieceSelection(UIInputHandler ih)
	{
		HidePieceSelection();
	}

	public static void HidePieceSelection()
	{
		if (!(m_instance == null))
		{
			m_instance.m_closePieceSelection = 2;
		}
	}

	public static bool IsPieceSelectionVisible()
	{
		if (m_instance == null)
		{
			return false;
		}
		if (m_instance.m_buildHud.activeSelf)
		{
			return m_instance.m_pieceSelectionWindow.activeSelf;
		}
		return false;
	}

	private void UpdateBuild(Player player, bool forceUpdateAllBuildStatuses)
	{
		if (player.InPlaceMode())
		{
			if (m_closePieceSelection > 0)
			{
				m_closePieceSelection--;
				if (m_closePieceSelection <= 0 && m_pieceSelectionWindow.activeSelf)
				{
					m_pieceSelectionWindow.SetActive(value: false);
				}
			}
			player.GetBuildSelection(out var go, out var id, out var _, out var category, out var useCategory);
			m_buildHud.SetActive(value: true);
			if (m_pieceSelectionWindow.activeSelf)
			{
				UpdatePieceList(player, id, category, forceUpdateAllBuildStatuses);
				m_pieceCategoryRoot.SetActive(useCategory);
				if (useCategory)
				{
					for (int i = 0; i < m_pieceCategoryTabs.Length; i++)
					{
						GameObject gameObject = m_pieceCategoryTabs[i];
						Transform transform = gameObject.transform.Find("Selected");
						string text = m_buildCategoryNames[i] + " [<color=yellow>" + player.GetAvailableBuildPiecesInCategory((Piece.PieceCategory)i) + "</color>]";
						if (i == (int)category)
						{
							transform.gameObject.SetActive(value: true);
							transform.GetComponentInChildren<Text>().set_text(text);
						}
						else
						{
							transform.gameObject.SetActive(value: false);
							gameObject.GetComponentInChildren<Text>().set_text(text);
						}
					}
				}
			}
			if ((bool)m_hoveredPiece && (ZInput.IsGamepadActive() || !player.IsPieceAvailable(m_hoveredPiece)))
			{
				m_hoveredPiece = null;
			}
			if ((bool)m_hoveredPiece)
			{
				SetupPieceInfo(m_hoveredPiece);
			}
			else
			{
				SetupPieceInfo(go);
			}
		}
		else
		{
			m_buildHud.SetActive(value: false);
			m_pieceSelectionWindow.SetActive(value: false);
		}
	}

	private void SetupPieceInfo(Piece piece)
	{
		if (piece == null)
		{
			m_buildSelection.set_text(Localization.get_instance().Localize("$hud_nothingtobuild"));
			m_pieceDescription.set_text("");
			((Behaviour)(object)m_buildIcon).enabled = false;
			for (int i = 0; i < m_requirementItems.Length; i++)
			{
				m_requirementItems[i].SetActive(value: false);
			}
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		m_buildSelection.set_text(Localization.get_instance().Localize(piece.m_name));
		m_pieceDescription.set_text(Localization.get_instance().Localize(piece.m_description));
		((Behaviour)(object)m_buildIcon).enabled = true;
		m_buildIcon.set_sprite(piece.m_icon);
		for (int j = 0; j < m_requirementItems.Length; j++)
		{
			if (j < piece.m_resources.Length)
			{
				Piece.Requirement req = piece.m_resources[j];
				m_requirementItems[j].SetActive(value: true);
				InventoryGui.SetupRequirement(m_requirementItems[j].transform, req, localPlayer, craft: false, 0);
			}
			else
			{
				m_requirementItems[j].SetActive(value: false);
			}
		}
		if ((bool)piece.m_craftingStation)
		{
			CraftingStation craftingStation = CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, localPlayer.transform.position);
			GameObject obj = m_requirementItems[piece.m_resources.Length];
			obj.SetActive(value: true);
			Image component = obj.transform.Find("res_icon").GetComponent<Image>();
			Text component2 = obj.transform.Find("res_name").GetComponent<Text>();
			Text component3 = obj.transform.Find("res_amount").GetComponent<Text>();
			UITooltip component4 = obj.GetComponent<UITooltip>();
			component.set_sprite(piece.m_craftingStation.m_icon);
			component2.set_text(Localization.get_instance().Localize(piece.m_craftingStation.m_name));
			component4.m_text = piece.m_craftingStation.m_name;
			if (craftingStation != null)
			{
				craftingStation.ShowAreaMarker();
				((Graphic)component).set_color(Color.white);
				component3.set_text("");
				((Graphic)component3).set_color(Color.white);
			}
			else
			{
				((Graphic)component).set_color(Color.gray);
				component3.set_text("None");
				((Graphic)component3).set_color((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
			}
		}
	}

	private void UpdateGuardianPower(Player player)
	{
		player.GetGuardianPowerHUD(out var se, out var cooldown);
		if ((bool)se)
		{
			m_gpRoot.gameObject.SetActive(value: true);
			m_gpIcon.set_sprite(se.m_icon);
			((Graphic)m_gpIcon).set_color((cooldown <= 0f) ? Color.white : new Color(1f, 0f, 1f, 0f));
			m_gpName.set_text(Localization.get_instance().Localize(se.m_name));
			if (cooldown > 0f)
			{
				m_gpCooldown.set_text(StatusEffect.GetTimeString(cooldown));
			}
			else
			{
				m_gpCooldown.set_text(Localization.get_instance().Localize("$hud_ready"));
			}
		}
		else
		{
			m_gpRoot.gameObject.SetActive(value: false);
		}
	}

	private void UpdateStatusEffects(List<StatusEffect> statusEffects)
	{
		if (m_statusEffects.Count != statusEffects.Count)
		{
			foreach (RectTransform statusEffect2 in m_statusEffects)
			{
				UnityEngine.Object.Destroy(statusEffect2.gameObject);
			}
			m_statusEffects.Clear();
			for (int i = 0; i < statusEffects.Count; i++)
			{
				RectTransform rectTransform = UnityEngine.Object.Instantiate(m_statusEffectTemplate, m_statusEffectListRoot);
				rectTransform.gameObject.SetActive(value: true);
				rectTransform.anchoredPosition = new Vector3(-4f - (float)i * m_statusEffectSpacing, 0f, 0f);
				m_statusEffects.Add(rectTransform);
			}
		}
		for (int j = 0; j < statusEffects.Count; j++)
		{
			StatusEffect statusEffect = statusEffects[j];
			RectTransform rectTransform2 = m_statusEffects[j];
			Image component = rectTransform2.Find("Icon").GetComponent<Image>();
			component.set_sprite(statusEffect.m_icon);
			if (statusEffect.m_flashIcon)
			{
				((Graphic)component).set_color((Mathf.Sin(Time.time * 10f) > 0f) ? new Color(1f, 0.5f, 0.5f, 1f) : Color.white);
			}
			else
			{
				((Graphic)component).set_color(Color.white);
			}
			rectTransform2.Find("Cooldown").gameObject.SetActive(statusEffect.m_cooldownIcon);
			rectTransform2.GetComponentInChildren<Text>().set_text(Localization.get_instance().Localize(statusEffect.m_name));
			Text component2 = rectTransform2.Find("TimeText").GetComponent<Text>();
			string iconText = statusEffect.GetIconText();
			if (!string.IsNullOrEmpty(iconText))
			{
				((Component)(object)component2).gameObject.SetActive(value: true);
				component2.set_text(iconText);
			}
			else
			{
				((Component)(object)component2).gameObject.SetActive(value: false);
			}
			if (statusEffect.m_isNew)
			{
				statusEffect.m_isNew = false;
				rectTransform2.GetComponentInChildren<Animator>().SetTrigger("flash");
			}
		}
	}

	private void UpdateEvent(Player player)
	{
		RandomEvent activeEvent = RandEventSystem.instance.GetActiveEvent();
		if (activeEvent != null && !EnemyHud.instance.ShowingBossHud() && activeEvent.GetTime() > 3f)
		{
			m_eventBar.SetActive(value: true);
			m_eventName.set_text(Localization.get_instance().Localize(activeEvent.GetHudText()));
		}
		else
		{
			m_eventBar.SetActive(value: false);
		}
	}

	public void ToggleBetaTextVisible()
	{
		m_betaText.SetActive(!m_betaText.activeSelf);
	}

	public void FlashHealthBar()
	{
		m_healthAnimator.SetTrigger("Flash");
	}

	public void StaminaBarUppgradeFlash()
	{
		m_staminaAnimator.SetBool("Visible", true);
		m_staminaAnimator.SetTrigger("Flash");
	}

	public void StaminaBarNoStaminaFlash()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		AnimatorStateInfo currentAnimatorStateInfo = m_staminaAnimator.GetCurrentAnimatorStateInfo(0);
		if (!((AnimatorStateInfo)(ref currentAnimatorStateInfo)).IsTag("nostamina"))
		{
			m_staminaAnimator.SetBool("Visible", true);
			m_staminaAnimator.SetTrigger("NoStamina");
		}
	}

	public static bool IsUserHidden()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_userHidden;
		}
		return false;
	}
}
