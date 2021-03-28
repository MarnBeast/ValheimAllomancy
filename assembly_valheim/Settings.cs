using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
	[Serializable]
	public class KeySetting
	{
		public string m_keyName = "";

		public RectTransform m_keyTransform;
	}

	private static Settings m_instance;

	[Header("Inout")]
	public Slider m_sensitivitySlider;

	public Toggle m_invertMouse;

	public Toggle m_gamepadEnabled;

	public GameObject m_bindDialog;

	public List<KeySetting> m_keys = new List<KeySetting>();

	[Header("Misc")]
	public Toggle m_cameraShake;

	public Toggle m_shipCameraTilt;

	public Toggle m_quickPieceSelect;

	public Toggle m_showKeyHints;

	public Slider m_guiScaleSlider;

	public Text m_guiScaleText;

	public Text m_language;

	public Button m_resetTutorial;

	[Header("Audio")]
	public Slider m_volumeSlider;

	public Slider m_sfxVolumeSlider;

	public Slider m_musicVolumeSlider;

	public Toggle m_continousMusic;

	public AudioMixer m_masterMixer;

	[Header("Graphics")]
	public Toggle m_dofToggle;

	public Toggle m_vsyncToggle;

	public Toggle m_bloomToggle;

	public Toggle m_ssaoToggle;

	public Toggle m_sunshaftsToggle;

	public Toggle m_aaToggle;

	public Toggle m_caToggle;

	public Toggle m_motionblurToggle;

	public Toggle m_tesselationToggle;

	public Toggle m_softPartToggle;

	public Toggle m_fullscreenToggle;

	public Slider m_shadowQuality;

	public Text m_shadowQualityText;

	public Slider m_lod;

	public Text m_lodText;

	public Slider m_lights;

	public Text m_lightsText;

	public Slider m_vegetation;

	public Text m_vegetationText;

	public Text m_resButtonText;

	public GameObject m_resDialog;

	public GameObject m_resListElement;

	public RectTransform m_resListRoot;

	public Scrollbar m_resListScroll;

	public float m_resListSpace = 20f;

	public GameObject m_resSwitchDialog;

	public Text m_resSwitchCountdown;

	public int m_minResWidth = 1280;

	public int m_minResHeight = 720;

	private string m_languageKey = "";

	private bool m_oldFullscreen;

	private Resolution m_oldRes;

	private Resolution m_selectedRes;

	private List<GameObject> m_resObjects = new List<GameObject>();

	private List<Resolution> m_resolutions = new List<Resolution>();

	private float m_resListBaseSize;

	private bool m_modeApplied;

	private float m_resCountdownTimer = 1f;

	public static Settings instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_bindDialog.SetActive(value: false);
		m_resDialog.SetActive(value: false);
		m_resSwitchDialog.SetActive(value: false);
		m_resListBaseSize = m_resListRoot.rect.height;
		LoadSettings();
		SetupKeys();
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Update()
	{
		if (m_bindDialog.activeSelf)
		{
			UpdateBinding();
			return;
		}
		UpdateResSwitch(Time.deltaTime);
		AudioListener.set_volume(m_volumeSlider.get_value());
		MusicMan.m_masterMusicVolume = m_musicVolumeSlider.get_value();
		AudioMan.SetSFXVolume(m_sfxVolumeSlider.get_value());
		SetQualityText(m_shadowQualityText, (int)m_shadowQuality.get_value());
		SetQualityText(m_lodText, (int)m_lod.get_value());
		SetQualityText(m_lightsText, (int)m_lights.get_value());
		SetQualityText(m_vegetationText, (int)m_vegetation.get_value());
		m_resButtonText.set_text(m_selectedRes.width + "x" + m_selectedRes.height + "  " + m_selectedRes.refreshRate + "hz");
		m_guiScaleText.set_text(m_guiScaleSlider.get_value() + "%");
		GuiScaler.SetScale(m_guiScaleSlider.get_value() / 100f);
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			OnBack();
		}
	}

	private void SetQualityText(Text text, int level)
	{
		switch (level)
		{
		case 0:
			text.set_text(Localization.get_instance().Localize("[$settings_low]"));
			break;
		case 1:
			text.set_text(Localization.get_instance().Localize("[$settings_medium]"));
			break;
		case 2:
			text.set_text(Localization.get_instance().Localize("[$settings_high]"));
			break;
		case 3:
			text.set_text(Localization.get_instance().Localize("[$settings_veryhigh]"));
			break;
		}
	}

	public void OnBack()
	{
		RevertMode();
		LoadSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnOk()
	{
		SaveSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void SaveSettings()
	{
		PlayerPrefs.SetFloat("MasterVolume", m_volumeSlider.get_value());
		PlayerPrefs.SetFloat("MouseSensitivity", m_sensitivitySlider.get_value());
		PlayerPrefs.SetFloat("MusicVolume", m_musicVolumeSlider.get_value());
		PlayerPrefs.SetFloat("SfxVolume", m_sfxVolumeSlider.get_value());
		PlayerPrefs.SetInt("ContinousMusic", m_continousMusic.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("InvertMouse", m_invertMouse.get_isOn() ? 1 : 0);
		PlayerPrefs.SetFloat("GuiScale", m_guiScaleSlider.get_value() / 100f);
		PlayerPrefs.SetInt("CameraShake", m_cameraShake.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("ShipCameraTilt", m_shipCameraTilt.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("QuickPieceSelect", m_quickPieceSelect.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("KeyHints", m_showKeyHints.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("DOF", m_dofToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("VSync", m_vsyncToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("Bloom", m_bloomToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("SSAO", m_ssaoToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("SunShafts", m_sunshaftsToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("AntiAliasing", m_aaToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("ChromaticAberration", m_caToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("MotionBlur", m_motionblurToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("SoftPart", m_softPartToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("Tesselation", m_tesselationToggle.get_isOn() ? 1 : 0);
		PlayerPrefs.SetInt("ShadowQuality", (int)m_shadowQuality.get_value());
		PlayerPrefs.SetInt("LodBias", (int)m_lod.get_value());
		PlayerPrefs.SetInt("Lights", (int)m_lights.get_value());
		PlayerPrefs.SetInt("ClutterQuality", (int)m_vegetation.get_value());
		ZInput.SetGamepadEnabled(m_gamepadEnabled.get_isOn());
		ZInput.get_instance().Save();
		if ((bool)GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if ((bool)CameraEffects.instance)
		{
			CameraEffects.instance.ApplySettings();
		}
		if ((bool)ClutterSystem.instance)
		{
			ClutterSystem.instance.ApplySettings();
		}
		if ((bool)MusicMan.instance)
		{
			MusicMan.instance.ApplySettings();
		}
		if ((bool)GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if ((bool)KeyHints.instance)
		{
			KeyHints.instance.ApplySettings();
		}
		ApplyQualitySettings();
		ApplyMode();
		PlayerController.m_mouseSens = m_sensitivitySlider.get_value();
		PlayerController.m_invertMouse = m_invertMouse.get_isOn();
		Localization.get_instance().SetLanguage(m_languageKey);
		GuiScaler.LoadGuiScale();
		PlayerPrefs.Save();
	}

	public static void ApplyStartupSettings()
	{
		QualitySettings.vSyncCount = (((PlayerPrefs.GetInt("VSync", 0) == 1) ? true : false) ? 1 : 0);
		ApplyQualitySettings();
	}

	private static void ApplyQualitySettings()
	{
		QualitySettings.softParticles = ((PlayerPrefs.GetInt("SoftPart", 1) == 1) ? true : false);
		if (PlayerPrefs.GetInt("Tesselation", 1) == 1)
		{
			Shader.EnableKeyword("TESSELATION_ON");
		}
		else
		{
			Shader.DisableKeyword("TESSELATION_ON");
		}
		switch (PlayerPrefs.GetInt("LodBias", 2))
		{
		case 0:
			QualitySettings.lodBias = 1f;
			break;
		case 1:
			QualitySettings.lodBias = 1.5f;
			break;
		case 2:
			QualitySettings.lodBias = 2f;
			break;
		case 3:
			QualitySettings.lodBias = 5f;
			break;
		}
		switch (PlayerPrefs.GetInt("Lights", 2))
		{
		case 0:
			QualitySettings.pixelLightCount = 2;
			break;
		case 1:
			QualitySettings.pixelLightCount = 4;
			break;
		case 2:
			QualitySettings.pixelLightCount = 8;
			break;
		}
		ApplyShadowQuality();
	}

	private static void ApplyShadowQuality()
	{
		switch (PlayerPrefs.GetInt("ShadowQuality", 2))
		{
		case 0:
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 80f;
			QualitySettings.shadowResolution = ShadowResolution.Low;
			break;
		case 1:
			QualitySettings.shadowCascades = 3;
			QualitySettings.shadowDistance = 120f;
			QualitySettings.shadowResolution = ShadowResolution.Medium;
			break;
		case 2:
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 150f;
			QualitySettings.shadowResolution = ShadowResolution.High;
			break;
		}
	}

	private void LoadSettings()
	{
		ZInput.get_instance().Load();
		AudioListener.set_volume(PlayerPrefs.GetFloat("MasterVolume", AudioListener.get_volume()));
		MusicMan.m_masterMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
		AudioMan.SetSFXVolume(PlayerPrefs.GetFloat("SfxVolume", 1f));
		m_continousMusic.set_isOn((PlayerPrefs.GetInt("ContinousMusic", 1) == 1) ? true : false);
		PlayerController.m_mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		PlayerController.m_invertMouse = ((PlayerPrefs.GetInt("InvertMouse", 0) == 1) ? true : false);
		float @float = PlayerPrefs.GetFloat("GuiScale", 1f);
		m_volumeSlider.set_value(AudioListener.get_volume());
		m_sensitivitySlider.set_value(PlayerController.m_mouseSens);
		m_sfxVolumeSlider.set_value(AudioMan.GetSFXVolume());
		m_musicVolumeSlider.set_value(MusicMan.m_masterMusicVolume);
		m_guiScaleSlider.set_value(@float * 100f);
		m_invertMouse.set_isOn(PlayerController.m_invertMouse);
		m_gamepadEnabled.set_isOn(ZInput.IsGamepadEnabled());
		m_languageKey = Localization.get_instance().GetSelectedLanguage();
		m_language.set_text(Localization.get_instance().Localize("$language_" + m_languageKey.ToLower()));
		m_cameraShake.set_isOn((PlayerPrefs.GetInt("CameraShake", 1) == 1) ? true : false);
		m_shipCameraTilt.set_isOn((PlayerPrefs.GetInt("ShipCameraTilt", 1) == 1) ? true : false);
		m_quickPieceSelect.set_isOn((PlayerPrefs.GetInt("QuickPieceSelect", 0) == 1) ? true : false);
		m_showKeyHints.set_isOn((PlayerPrefs.GetInt("KeyHints", 1) == 1) ? true : false);
		m_dofToggle.set_isOn((PlayerPrefs.GetInt("DOF", 1) == 1) ? true : false);
		m_vsyncToggle.set_isOn((PlayerPrefs.GetInt("VSync", 0) == 1) ? true : false);
		m_bloomToggle.set_isOn((PlayerPrefs.GetInt("Bloom", 1) == 1) ? true : false);
		m_ssaoToggle.set_isOn((PlayerPrefs.GetInt("SSAO", 1) == 1) ? true : false);
		m_sunshaftsToggle.set_isOn((PlayerPrefs.GetInt("SunShafts", 1) == 1) ? true : false);
		m_aaToggle.set_isOn((PlayerPrefs.GetInt("AntiAliasing", 1) == 1) ? true : false);
		m_caToggle.set_isOn((PlayerPrefs.GetInt("ChromaticAberration", 1) == 1) ? true : false);
		m_motionblurToggle.set_isOn((PlayerPrefs.GetInt("MotionBlur", 1) == 1) ? true : false);
		m_softPartToggle.set_isOn((PlayerPrefs.GetInt("SoftPart", 1) == 1) ? true : false);
		m_tesselationToggle.set_isOn((PlayerPrefs.GetInt("Tesselation", 1) == 1) ? true : false);
		m_shadowQuality.set_value((float)PlayerPrefs.GetInt("ShadowQuality", 2));
		m_lod.set_value((float)PlayerPrefs.GetInt("LodBias", 2));
		m_lights.set_value((float)PlayerPrefs.GetInt("Lights", 2));
		m_vegetation.set_value((float)PlayerPrefs.GetInt("ClutterQuality", 2));
		m_fullscreenToggle.set_isOn(Screen.fullScreen);
		m_oldFullscreen = m_fullscreenToggle.get_isOn();
		m_oldRes = Screen.currentResolution;
		m_oldRes.width = Screen.width;
		m_oldRes.height = Screen.height;
		m_selectedRes = m_oldRes;
		ZLog.Log((object)("Current res " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + "     " + Screen.width + "x" + Screen.height));
	}

	private void SetupKeys()
	{
		foreach (KeySetting key in m_keys)
		{
			((UnityEvent)(object)key.m_keyTransform.GetComponentInChildren<Button>().get_onClick()).AddListener((UnityAction)OnKeySet);
		}
		UpdateBindings();
	}

	private void UpdateBindings()
	{
		foreach (KeySetting key in m_keys)
		{
			((Component)(object)key.m_keyTransform.GetComponentInChildren<Button>()).GetComponentInChildren<Text>().set_text(Localization.get_instance().GetBoundKeyString(key.m_keyName));
		}
	}

	private void OnKeySet()
	{
		foreach (KeySetting key in m_keys)
		{
			if (((Component)(object)key.m_keyTransform.GetComponentInChildren<Button>()).gameObject == EventSystem.get_current().get_currentSelectedGameObject())
			{
				OpenBindDialog(key.m_keyName);
				return;
			}
		}
		ZLog.Log((object)"NOT FOUND");
	}

	private void OpenBindDialog(string keyName)
	{
		ZLog.Log((object)("BInding key " + keyName));
		ZInput.get_instance().StartBindKey(keyName);
		m_bindDialog.SetActive(value: true);
	}

	private void UpdateBinding()
	{
		if (m_bindDialog.activeSelf && ZInput.get_instance().EndBindKey())
		{
			m_bindDialog.SetActive(value: false);
			UpdateBindings();
		}
	}

	public void ResetBindings()
	{
		ZInput.get_instance().Reset();
		UpdateBindings();
	}

	public void OnLanguageLeft()
	{
		m_languageKey = Localization.get_instance().GetPrevLanguage(m_languageKey);
		m_language.set_text(Localization.get_instance().Localize("$language_" + m_languageKey.ToLower()));
	}

	public void OnLanguageRight()
	{
		m_languageKey = Localization.get_instance().GetNextLanguage(m_languageKey);
		m_language.set_text(Localization.get_instance().Localize("$language_" + m_languageKey.ToLower()));
	}

	public void OnShowResList()
	{
		m_resDialog.SetActive(value: true);
		FillResList();
	}

	private void UpdateValidResolutions()
	{
		Resolution[] array = Screen.resolutions;
		if (array.Length == 0)
		{
			array = new Resolution[1]
			{
				m_oldRes
			};
		}
		m_resolutions.Clear();
		Resolution[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Resolution item = array2[i];
			if ((item.width >= m_minResWidth && item.height >= m_minResHeight) || item.width == m_oldRes.width || item.height == m_oldRes.height)
			{
				m_resolutions.Add(item);
			}
		}
		if (m_resolutions.Count == 0)
		{
			Resolution item2 = default(Resolution);
			item2.width = 1280;
			item2.height = 720;
			item2.refreshRate = 60;
			m_resolutions.Add(item2);
		}
	}

	private void FillResList()
	{
		foreach (GameObject resObject in m_resObjects)
		{
			UnityEngine.Object.Destroy(resObject);
		}
		m_resObjects.Clear();
		UpdateValidResolutions();
		float num = 0f;
		foreach (Resolution resolution in m_resolutions)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(m_resListElement, m_resListRoot.transform);
			gameObject.SetActive(value: true);
			((UnityEvent)(object)gameObject.GetComponentInChildren<Button>().get_onClick()).AddListener((UnityAction)OnResClick);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, num * (0f - m_resListSpace));
			gameObject.GetComponentInChildren<Text>().set_text(resolution.width + "x" + resolution.height + "  " + resolution.refreshRate + "hz");
			m_resObjects.Add(gameObject);
			num += 1f;
		}
		float size = Mathf.Max(m_resListBaseSize, num * m_resListSpace);
		m_resListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		m_resListScroll.set_value(1f);
	}

	public void OnResCancel()
	{
		m_resDialog.SetActive(value: false);
	}

	private void OnResClick()
	{
		m_resDialog.SetActive(value: false);
		GameObject currentSelectedGameObject = EventSystem.get_current().get_currentSelectedGameObject();
		for (int i = 0; i < m_resObjects.Count; i++)
		{
			if (currentSelectedGameObject == m_resObjects[i])
			{
				m_selectedRes = m_resolutions[i];
				break;
			}
		}
	}

	public void OnApplyMode()
	{
		ApplyMode();
		ShowResSwitchCountdown();
	}

	private void ApplyMode()
	{
		if (Screen.width != m_selectedRes.width || Screen.height != m_selectedRes.height || m_fullscreenToggle.get_isOn() != Screen.fullScreen)
		{
			Screen.SetResolution(m_selectedRes.width, m_selectedRes.height, m_fullscreenToggle.get_isOn());
			m_modeApplied = true;
		}
	}

	private void RevertMode()
	{
		if (m_modeApplied)
		{
			m_modeApplied = false;
			m_selectedRes = m_oldRes;
			m_fullscreenToggle.set_isOn(m_oldFullscreen);
			Screen.SetResolution(m_oldRes.width, m_oldRes.height, m_oldFullscreen);
		}
	}

	private void ShowResSwitchCountdown()
	{
		m_resSwitchDialog.SetActive(value: true);
		m_resCountdownTimer = 5f;
	}

	public void OnResSwitchOK()
	{
		m_resSwitchDialog.SetActive(value: false);
	}

	private void UpdateResSwitch(float dt)
	{
		if (m_resSwitchDialog.activeSelf)
		{
			m_resCountdownTimer -= dt;
			m_resSwitchCountdown.set_text(Mathf.CeilToInt(m_resCountdownTimer).ToString());
			if (m_resCountdownTimer <= 0f)
			{
				RevertMode();
				m_resSwitchDialog.SetActive(value: false);
			}
		}
	}

	public void OnResetTutorial()
	{
		Player.ResetSeenTutorials();
	}
}
