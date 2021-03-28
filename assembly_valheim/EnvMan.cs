using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityStandardAssets.ImageEffects;

public class EnvMan : MonoBehaviour
{
	private static EnvMan m_instance;

	public Light m_dirLight;

	public bool m_debugTimeOfDay;

	[Range(0f, 1f)]
	public float m_debugTime = 0.5f;

	public string m_debugEnv = "";

	public bool m_debugWind;

	[Range(0f, 360f)]
	public float m_debugWindAngle;

	[Range(0f, 1f)]
	public float m_debugWindIntensity = 1f;

	private const float m_morningL = 0.15f;

	public float m_sunHorizonTransitionH = 0.08f;

	public float m_sunHorizonTransitionL = 0.02f;

	public long m_dayLengthSec = 1200L;

	public float m_transitionDuration = 2f;

	public long m_environmentDuration = 20L;

	public long m_windPeriodDuration = 10L;

	public float m_windTransitionDuration = 5f;

	public List<EnvSetup> m_environments = new List<EnvSetup>();

	public List<BiomeEnvSetup> m_biomes = new List<BiomeEnvSetup>();

	public string m_introEnvironment = "ThunderStorm";

	public float m_edgeOfWorldWidth = 500f;

	[Header("Music")]
	public float m_randomMusicIntervalMin = 60f;

	public float m_randomMusicIntervalMax = 200f;

	[Header("Other")]
	public MeshRenderer m_clouds;

	public MeshRenderer m_rainClouds;

	public MeshRenderer m_rainCloudsDownside;

	public float m_wetTransitionDuration = 15f;

	private bool m_skipTime;

	private double m_skipToTime;

	private double m_timeSkipSpeed = 1.0;

	private const double m_timeSkipDuration = 12.0;

	private double m_totalSeconds;

	private float m_smoothDayFraction;

	private Color m_sunFogColor = Color.white;

	private GameObject[] m_currentPSystems;

	private GameObject m_currentEnvObject;

	private Vector4 m_windDir1 = new Vector4(0f, 0f, -1f, 0f);

	private Vector4 m_windDir2 = new Vector4(0f, 0f, -1f, 0f);

	private Vector4 m_wind = new Vector4(0f, 0f, -1f, 0f);

	private float m_windTransitionTimer = -1f;

	private Vector3 m_cloudOffset = Vector3.zero;

	private string m_forceEnv = "";

	private EnvSetup m_currentEnv;

	private EnvSetup m_prevEnv;

	private EnvSetup m_nextEnv;

	private string m_ambientMusic;

	private float m_ambientMusicTimer;

	private Heightmap m_cachedHeightmap;

	private Heightmap.Biome m_currentBiome;

	private long m_environmentPeriod;

	private float m_transitionTimer;

	private bool m_firstEnv = true;

	private int _NetRefPos = Shader.PropertyToID("_NetRefPos");

	private int _SkyboxSunDir = Shader.PropertyToID("_SkyboxSunDir");

	private int _SunDir = Shader.PropertyToID("_SunDir");

	private int _SunFogColor = Shader.PropertyToID("_SunFogColor");

	private int _Wet = Shader.PropertyToID("_Wet");

	private int _SunColor = Shader.PropertyToID("_SunColor");

	private int _AmbientColor = Shader.PropertyToID("_AmbientColor");

	private int _GlobalWind1 = Shader.PropertyToID("_GlobalWind1");

	private int _GlobalWind2 = Shader.PropertyToID("_GlobalWind2");

	private int _GlobalWindAlpha = Shader.PropertyToID("_GlobalWindAlpha");

	private int _CloudOffset = Shader.PropertyToID("_CloudOffset");

	private int _GlobalWindForce = Shader.PropertyToID("_GlobalWindForce");

	private int _Rain = Shader.PropertyToID("_Rain");

	public static EnvMan instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		foreach (EnvSetup environment in m_environments)
		{
			SetParticleArrayEnabled(environment.m_psystems, enabled: false);
			if ((bool)environment.m_envObject)
			{
				environment.m_envObject.SetActive(value: false);
			}
		}
		foreach (BiomeEnvSetup biome in m_biomes)
		{
			foreach (EnvEntry environment2 in biome.m_environments)
			{
				environment2.m_env = GetEnv(environment2.m_environment);
			}
		}
		m_currentEnv = GetDefaultEnv();
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void SetParticleArrayEnabled(GameObject[] psystems, bool enabled)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		foreach (GameObject gameObject in psystems)
		{
			ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				EmissionModule emission = componentsInChildren[j].get_emission();
				((EmissionModule)(ref emission)).set_enabled(enabled);
			}
			MistEmitter componentInChildren = gameObject.GetComponentInChildren<MistEmitter>();
			if ((bool)componentInChildren)
			{
				componentInChildren.enabled = enabled;
			}
		}
	}

	private float RescaleDayFraction(float fraction)
	{
		if (fraction >= 0.15f && fraction <= 0.85f)
		{
			float num = (fraction - 0.15f) / 0.7f;
			fraction = 0.25f + num * 0.5f;
		}
		else if (fraction < 0.5f)
		{
			fraction = fraction / 0.15f * 0.25f;
		}
		else
		{
			float num2 = (fraction - 0.85f) / 0.15f;
			fraction = 0.75f + num2 * 0.25f;
		}
		return fraction;
	}

	private void Update()
	{
		Vector3 windForce = instance.GetWindForce();
		m_cloudOffset += windForce * Time.deltaTime * 0.01f;
		Shader.SetGlobalVector(_CloudOffset, m_cloudOffset);
		Shader.SetGlobalVector(_NetRefPos, ZNet.instance.GetReferencePosition());
	}

	private void FixedUpdate()
	{
		UpdateTimeSkip(Time.fixedDeltaTime);
		m_totalSeconds = ZNet.instance.GetTimeSeconds();
		long num = (long)m_totalSeconds;
		double num2 = m_totalSeconds * 1000.0;
		long num3 = m_dayLengthSec * 1000;
		float fraction = Mathf.Clamp01((float)(num2 % (double)num3 / 1000.0) / (float)m_dayLengthSec);
		fraction = RescaleDayFraction(fraction);
		float smoothDayFraction = m_smoothDayFraction;
		float t = Mathf.LerpAngle(m_smoothDayFraction * 360f, fraction * 360f, 0.01f);
		m_smoothDayFraction = Mathf.Repeat(t, 360f) / 360f;
		if (m_debugTimeOfDay)
		{
			m_smoothDayFraction = m_debugTime;
		}
		float num4 = Mathf.Pow(Mathf.Max(1f - Mathf.Clamp01(m_smoothDayFraction / 0.25f), Mathf.Clamp01((m_smoothDayFraction - 0.75f) / 0.25f)), 0.5f);
		float num5 = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(m_smoothDayFraction - 0.5f) / 0.25f), 0.5f);
		float num6 = Mathf.Min(Mathf.Clamp01(1f - (m_smoothDayFraction - 0.26f) / (0f - m_sunHorizonTransitionL)), Mathf.Clamp01(1f - (m_smoothDayFraction - 0.26f) / m_sunHorizonTransitionH));
		float num7 = Mathf.Min(Mathf.Clamp01(1f - (m_smoothDayFraction - 0.74f) / (0f - m_sunHorizonTransitionH)), Mathf.Clamp01(1f - (m_smoothDayFraction - 0.74f) / m_sunHorizonTransitionL));
		float num8 = 1f / (num4 + num5 + num6 + num7);
		num4 *= num8;
		num5 *= num8;
		num6 *= num8;
		num7 *= num8;
		Heightmap.Biome biome = GetBiome();
		UpdateTriggers(smoothDayFraction, m_smoothDayFraction, biome, Time.fixedDeltaTime);
		UpdateEnvironment(num, biome);
		InterpolateEnvironment(Time.fixedDeltaTime);
		UpdateWind(num, Time.fixedDeltaTime);
		if (!string.IsNullOrEmpty(m_forceEnv))
		{
			EnvSetup env = GetEnv(m_forceEnv);
			if (env != null)
			{
				SetEnv(env, num5, num4, num6, num7, Time.fixedDeltaTime);
			}
		}
		else
		{
			SetEnv(m_currentEnv, num5, num4, num6, num7, Time.fixedDeltaTime);
		}
	}

	private int GetCurrentDay()
	{
		return (int)(m_totalSeconds / (double)m_dayLengthSec);
	}

	private void UpdateTriggers(float oldDayFraction, float newDayFraction, Heightmap.Biome biome, float dt)
	{
		if (Player.m_localPlayer == null || biome == Heightmap.Biome.None)
		{
			return;
		}
		EnvSetup currentEnvironment = GetCurrentEnvironment();
		if (currentEnvironment != null)
		{
			UpdateAmbientMusic(biome, currentEnvironment, dt);
			if (oldDayFraction > 0.2f && oldDayFraction < 0.25f && newDayFraction > 0.25f && newDayFraction < 0.3f)
			{
				OnMorning(biome, currentEnvironment);
			}
			if (oldDayFraction > 0.7f && oldDayFraction < 0.75f && newDayFraction > 0.75f && newDayFraction < 0.8f)
			{
				OnEvening(biome, currentEnvironment);
			}
		}
	}

	private void UpdateAmbientMusic(Heightmap.Biome biome, EnvSetup currentEnv, float dt)
	{
		m_ambientMusicTimer += dt;
		if (!(m_ambientMusicTimer > 2f))
		{
			return;
		}
		m_ambientMusicTimer = 0f;
		m_ambientMusic = null;
		BiomeEnvSetup biomeEnvSetup = GetBiomeEnvSetup(biome);
		if (IsDay())
		{
			if (currentEnv.m_musicDay.Length > 0)
			{
				m_ambientMusic = currentEnv.m_musicDay;
			}
			else if (biomeEnvSetup.m_musicDay.Length > 0)
			{
				m_ambientMusic = biomeEnvSetup.m_musicDay;
			}
		}
		else if (currentEnv.m_musicNight.Length > 0)
		{
			m_ambientMusic = currentEnv.m_musicNight;
		}
		else if (biomeEnvSetup.m_musicNight.Length > 0)
		{
			m_ambientMusic = biomeEnvSetup.m_musicNight;
		}
	}

	public string GetAmbientMusic()
	{
		return m_ambientMusic;
	}

	private void OnMorning(Heightmap.Biome biome, EnvSetup currentEnv)
	{
		string name = "morning";
		if (currentEnv.m_musicMorning.Length > 0)
		{
			name = currentEnv.m_musicMorning;
		}
		else
		{
			BiomeEnvSetup biomeEnvSetup = GetBiomeEnvSetup(biome);
			if (biomeEnvSetup.m_musicMorning.Length > 0)
			{
				name = biomeEnvSetup.m_musicMorning;
			}
		}
		MusicMan.instance.TriggerMusic(name);
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.get_instance().Localize("$msg_newday", new string[1]
		{
			GetCurrentDay().ToString()
		}));
	}

	private void OnEvening(Heightmap.Biome biome, EnvSetup currentEnv)
	{
		string name = "evening";
		if (currentEnv.m_musicEvening.Length > 0)
		{
			name = currentEnv.m_musicEvening;
		}
		else
		{
			BiomeEnvSetup biomeEnvSetup = GetBiomeEnvSetup(biome);
			if (biomeEnvSetup.m_musicEvening.Length > 0)
			{
				name = biomeEnvSetup.m_musicEvening;
			}
		}
		MusicMan.instance.TriggerMusic(name);
	}

	public void SetForceEnvironment(string env)
	{
		if (!(m_forceEnv == env))
		{
			ZLog.Log((object)("Setting forced environment " + env));
			m_forceEnv = env;
			FixedUpdate();
			if ((bool)ReflectionUpdate.instance)
			{
				ReflectionUpdate.instance.UpdateReflection();
			}
		}
	}

	private EnvSetup SelectWeightedEnvironment(List<EnvEntry> environments)
	{
		float num = 0f;
		foreach (EnvEntry environment in environments)
		{
			num += environment.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (EnvEntry environment2 in environments)
		{
			num3 += environment2.m_weight;
			if (num3 >= num2)
			{
				return environment2.m_env;
			}
		}
		return environments[environments.Count - 1].m_env;
	}

	private string GetEnvironmentOverride()
	{
		if (!string.IsNullOrEmpty(m_debugEnv))
		{
			return m_debugEnv;
		}
		if (Player.m_localPlayer != null && Player.m_localPlayer.InIntro())
		{
			return m_introEnvironment;
		}
		string envOverride = RandEventSystem.instance.GetEnvOverride();
		if (!string.IsNullOrEmpty(envOverride))
		{
			return envOverride;
		}
		string environment = EnvZone.GetEnvironment();
		if (!string.IsNullOrEmpty(environment))
		{
			return environment;
		}
		return null;
	}

	private void UpdateEnvironment(long sec, Heightmap.Biome biome)
	{
		string environmentOverride = GetEnvironmentOverride();
		if (!string.IsNullOrEmpty(environmentOverride))
		{
			m_environmentPeriod = -1L;
			m_currentBiome = GetBiome();
			QueueEnvironment(environmentOverride);
			return;
		}
		long num = sec / m_environmentDuration;
		if (m_environmentPeriod != num || m_currentBiome != biome)
		{
			m_environmentPeriod = num;
			m_currentBiome = biome;
			UnityEngine.Random.State state = UnityEngine.Random.state;
			UnityEngine.Random.InitState((int)num);
			List<EnvEntry> availableEnvironments = GetAvailableEnvironments(biome);
			if (availableEnvironments != null && availableEnvironments.Count > 0)
			{
				EnvSetup env = SelectWeightedEnvironment(availableEnvironments);
				QueueEnvironment(env);
			}
			UnityEngine.Random.state = state;
		}
	}

	private BiomeEnvSetup GetBiomeEnvSetup(Heightmap.Biome biome)
	{
		foreach (BiomeEnvSetup biome2 in m_biomes)
		{
			if (biome2.m_biome == biome)
			{
				return biome2;
			}
		}
		return null;
	}

	private List<EnvEntry> GetAvailableEnvironments(Heightmap.Biome biome)
	{
		return GetBiomeEnvSetup(biome)?.m_environments;
	}

	private Heightmap.Biome GetBiome()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return Heightmap.Biome.None;
		}
		Vector3 position = mainCamera.transform.position;
		if (m_cachedHeightmap == null || !m_cachedHeightmap.IsPointInside(position))
		{
			m_cachedHeightmap = Heightmap.FindHeightmap(position);
		}
		if ((bool)m_cachedHeightmap)
		{
			return m_cachedHeightmap.GetBiome(position);
		}
		return Heightmap.Biome.None;
	}

	private void InterpolateEnvironment(float dt)
	{
		if (m_nextEnv != null)
		{
			m_transitionTimer += dt;
			float num = Mathf.Clamp01(m_transitionTimer / m_transitionDuration);
			m_currentEnv = InterpolateEnvironment(m_prevEnv, m_nextEnv, num);
			if (num >= 1f)
			{
				m_currentEnv = m_nextEnv;
				m_prevEnv = null;
				m_nextEnv = null;
			}
		}
	}

	private void QueueEnvironment(string name)
	{
		if (!(m_currentEnv.m_name == name) && (m_nextEnv == null || !(m_nextEnv.m_name == name)))
		{
			EnvSetup env = GetEnv(name);
			if (env != null)
			{
				QueueEnvironment(env);
			}
		}
	}

	private void QueueEnvironment(EnvSetup env)
	{
		if (m_firstEnv)
		{
			m_firstEnv = false;
			m_currentEnv = env;
		}
		else
		{
			m_prevEnv = m_currentEnv.Clone();
			m_nextEnv = env;
			m_transitionTimer = 0f;
		}
	}

	private EnvSetup InterpolateEnvironment(EnvSetup a, EnvSetup b, float i)
	{
		EnvSetup envSetup = a.Clone();
		envSetup.m_name = b.m_name;
		if (i >= 0.5f)
		{
			envSetup.m_isFreezingAtNight = b.m_isFreezingAtNight;
			envSetup.m_isFreezing = b.m_isFreezing;
			envSetup.m_isCold = b.m_isCold;
			envSetup.m_isColdAtNight = b.m_isColdAtNight;
			envSetup.m_isColdAtNight = b.m_isColdAtNight;
		}
		envSetup.m_ambColorDay = Color.Lerp(a.m_ambColorDay, b.m_ambColorDay, i);
		envSetup.m_ambColorNight = Color.Lerp(a.m_ambColorNight, b.m_ambColorNight, i);
		envSetup.m_fogColorDay = Color.Lerp(a.m_fogColorDay, b.m_fogColorDay, i);
		envSetup.m_fogColorEvening = Color.Lerp(a.m_fogColorEvening, b.m_fogColorEvening, i);
		envSetup.m_fogColorMorning = Color.Lerp(a.m_fogColorMorning, b.m_fogColorMorning, i);
		envSetup.m_fogColorNight = Color.Lerp(a.m_fogColorNight, b.m_fogColorNight, i);
		envSetup.m_fogColorSunDay = Color.Lerp(a.m_fogColorSunDay, b.m_fogColorSunDay, i);
		envSetup.m_fogColorSunEvening = Color.Lerp(a.m_fogColorSunEvening, b.m_fogColorSunEvening, i);
		envSetup.m_fogColorSunMorning = Color.Lerp(a.m_fogColorSunMorning, b.m_fogColorSunMorning, i);
		envSetup.m_fogColorSunNight = Color.Lerp(a.m_fogColorSunNight, b.m_fogColorSunNight, i);
		envSetup.m_fogDensityDay = Mathf.Lerp(a.m_fogDensityDay, b.m_fogDensityDay, i);
		envSetup.m_fogDensityEvening = Mathf.Lerp(a.m_fogDensityEvening, b.m_fogDensityEvening, i);
		envSetup.m_fogDensityMorning = Mathf.Lerp(a.m_fogDensityMorning, b.m_fogDensityMorning, i);
		envSetup.m_fogDensityNight = Mathf.Lerp(a.m_fogDensityNight, b.m_fogDensityNight, i);
		envSetup.m_sunColorDay = Color.Lerp(a.m_sunColorDay, b.m_sunColorDay, i);
		envSetup.m_sunColorEvening = Color.Lerp(a.m_sunColorEvening, b.m_sunColorEvening, i);
		envSetup.m_sunColorMorning = Color.Lerp(a.m_sunColorMorning, b.m_sunColorMorning, i);
		envSetup.m_sunColorNight = Color.Lerp(a.m_sunColorNight, b.m_sunColorNight, i);
		envSetup.m_lightIntensityDay = Mathf.Lerp(a.m_lightIntensityDay, b.m_lightIntensityDay, i);
		envSetup.m_lightIntensityNight = Mathf.Lerp(a.m_lightIntensityNight, b.m_lightIntensityNight, i);
		envSetup.m_sunAngle = Mathf.Lerp(a.m_sunAngle, b.m_sunAngle, i);
		envSetup.m_windMin = Mathf.Lerp(a.m_windMin, b.m_windMin, i);
		envSetup.m_windMax = Mathf.Lerp(a.m_windMax, b.m_windMax, i);
		envSetup.m_rainCloudAlpha = Mathf.Lerp(a.m_rainCloudAlpha, b.m_rainCloudAlpha, i);
		envSetup.m_ambientLoop = ((i > 0.75f) ? b.m_ambientLoop : a.m_ambientLoop);
		envSetup.m_ambientVol = ((i > 0.75f) ? b.m_ambientVol : a.m_ambientVol);
		envSetup.m_musicEvening = b.m_musicEvening;
		envSetup.m_musicMorning = b.m_musicMorning;
		envSetup.m_musicDay = b.m_musicDay;
		envSetup.m_musicNight = b.m_musicNight;
		return envSetup;
	}

	private void SetEnv(EnvSetup env, float dayInt, float nightInt, float morningInt, float eveningInt, float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		m_dirLight.transform.rotation = Quaternion.Euler(-90f + env.m_sunAngle, 0f, 0f) * Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(-90f + 360f * m_smoothDayFraction, 0f, 0f);
		Vector3 v = -m_dirLight.transform.forward;
		m_dirLight.intensity = env.m_lightIntensityDay * dayInt;
		m_dirLight.intensity += env.m_lightIntensityNight * nightInt;
		if (nightInt > 0f)
		{
			m_dirLight.transform.rotation = m_dirLight.transform.rotation * Quaternion.Euler(180f, 0f, 0f);
		}
		m_dirLight.transform.position = mainCamera.transform.position - m_dirLight.transform.forward * 3000f;
		m_dirLight.color = new Color(0f, 0f, 0f, 0f);
		m_dirLight.color += env.m_sunColorNight * nightInt;
		if (dayInt > 0f)
		{
			m_dirLight.color += env.m_sunColorDay * dayInt;
			m_dirLight.color += env.m_sunColorMorning * morningInt;
			m_dirLight.color += env.m_sunColorEvening * eveningInt;
		}
		RenderSettings.fogColor = new Color(0f, 0f, 0f, 0f);
		RenderSettings.fogColor += env.m_fogColorNight * nightInt;
		RenderSettings.fogColor += env.m_fogColorDay * dayInt;
		RenderSettings.fogColor += env.m_fogColorMorning * morningInt;
		RenderSettings.fogColor += env.m_fogColorEvening * eveningInt;
		m_sunFogColor = new Color(0f, 0f, 0f, 0f);
		m_sunFogColor += env.m_fogColorSunNight * nightInt;
		if (dayInt > 0f)
		{
			m_sunFogColor += env.m_fogColorSunDay * dayInt;
			m_sunFogColor += env.m_fogColorSunMorning * morningInt;
			m_sunFogColor += env.m_fogColorSunEvening * eveningInt;
		}
		m_sunFogColor = Color.Lerp(RenderSettings.fogColor, m_sunFogColor, Mathf.Clamp01(Mathf.Max(nightInt, dayInt) * 3f));
		RenderSettings.fogDensity = 0f;
		RenderSettings.fogDensity += env.m_fogDensityNight * nightInt;
		RenderSettings.fogDensity += env.m_fogDensityDay * dayInt;
		RenderSettings.fogDensity += env.m_fogDensityMorning * morningInt;
		RenderSettings.fogDensity += env.m_fogDensityEvening * eveningInt;
		RenderSettings.ambientMode = AmbientMode.Flat;
		RenderSettings.ambientLight = Color.Lerp(env.m_ambColorNight, env.m_ambColorDay, dayInt);
		SunShafts component = mainCamera.GetComponent<SunShafts>();
		if ((bool)(UnityEngine.Object)(object)component)
		{
			component.sunColor = m_dirLight.color;
		}
		if (env.m_envObject != m_currentEnvObject)
		{
			if ((bool)m_currentEnvObject)
			{
				m_currentEnvObject.SetActive(value: false);
				m_currentEnvObject = null;
			}
			if ((bool)env.m_envObject)
			{
				m_currentEnvObject = env.m_envObject;
				m_currentEnvObject.SetActive(value: true);
			}
		}
		if (env.m_psystems != m_currentPSystems)
		{
			if (m_currentPSystems != null)
			{
				SetParticleArrayEnabled(m_currentPSystems, enabled: false);
				m_currentPSystems = null;
			}
			if (env.m_psystems != null && (!env.m_psystemsOutsideOnly || ((bool)Player.m_localPlayer && !Player.m_localPlayer.InShelter())))
			{
				SetParticleArrayEnabled(env.m_psystems, enabled: true);
				m_currentPSystems = env.m_psystems;
			}
		}
		m_clouds.material.SetFloat(_Rain, env.m_rainCloudAlpha);
		if ((bool)(UnityEngine.Object)(object)env.m_ambientLoop)
		{
			AudioMan.instance.QueueAmbientLoop(env.m_ambientLoop, env.m_ambientVol);
		}
		else
		{
			AudioMan.instance.StopAmbientLoop();
		}
		Shader.SetGlobalVector(_SkyboxSunDir, v);
		Shader.SetGlobalVector(_SkyboxSunDir, v);
		Shader.SetGlobalVector(_SunDir, -m_dirLight.transform.forward);
		Shader.SetGlobalColor(_SunFogColor, m_sunFogColor);
		Shader.SetGlobalColor(_SunColor, m_dirLight.color * m_dirLight.intensity);
		Shader.SetGlobalColor(_AmbientColor, RenderSettings.ambientLight);
		float globalFloat = Shader.GetGlobalFloat(_Wet);
		globalFloat = Mathf.MoveTowards(globalFloat, env.m_isWet ? 1f : 0f, dt / m_wetTransitionDuration);
		Shader.SetGlobalFloat(_Wet, globalFloat);
	}

	private float GetDayFraction()
	{
		return m_smoothDayFraction;
	}

	public int GetDay(double time)
	{
		return (int)(time / (double)m_dayLengthSec);
	}

	public double GetMorningStartSec(int day)
	{
		return (float)(day * m_dayLengthSec) + (float)m_dayLengthSec * 0.15f;
	}

	private void UpdateTimeSkip(float dt)
	{
		if (ZNet.instance.IsServer() && m_skipTime)
		{
			double timeSeconds = ZNet.instance.GetTimeSeconds();
			timeSeconds += (double)dt * m_timeSkipSpeed;
			if (timeSeconds >= m_skipToTime)
			{
				timeSeconds = m_skipToTime;
				m_skipTime = false;
			}
			ZNet.instance.SetNetTime(timeSeconds);
		}
	}

	public bool IsTimeSkipping()
	{
		return m_skipTime;
	}

	public void SkipToMorning()
	{
		double timeSeconds = ZNet.instance.GetTimeSeconds();
		double time = timeSeconds - (double)((float)m_dayLengthSec * 0.15f);
		int day = GetDay(time);
		double morningStartSec = GetMorningStartSec(day + 1);
		m_skipTime = true;
		m_skipToTime = morningStartSec;
		double num = morningStartSec - timeSeconds;
		m_timeSkipSpeed = num / 12.0;
		ZLog.Log((object)("Time " + timeSeconds + ", day:" + day + "    nextm:" + morningStartSec + "  skipspeed:" + m_timeSkipSpeed));
	}

	public bool IsDay()
	{
		float dayFraction = GetDayFraction();
		if (dayFraction >= 0.25f)
		{
			return dayFraction <= 0.75f;
		}
		return false;
	}

	public bool IsAfternoon()
	{
		float dayFraction = GetDayFraction();
		if (dayFraction >= 0.5f)
		{
			return dayFraction <= 0.75f;
		}
		return false;
	}

	public bool IsNight()
	{
		float dayFraction = GetDayFraction();
		if (!(dayFraction <= 0.25f))
		{
			return dayFraction >= 0.75f;
		}
		return true;
	}

	public bool IsDaylight()
	{
		EnvSetup currentEnvironment = GetCurrentEnvironment();
		if (currentEnvironment != null && currentEnvironment.m_alwaysDark)
		{
			return false;
		}
		return IsDay();
	}

	public Heightmap.Biome GetCurrentBiome()
	{
		return m_currentBiome;
	}

	public bool IsEnvironment(string name)
	{
		return GetCurrentEnvironment().m_name == name;
	}

	public bool IsEnvironment(List<string> names)
	{
		EnvSetup currentEnvironment = GetCurrentEnvironment();
		return names.Contains(currentEnvironment.m_name);
	}

	public EnvSetup GetCurrentEnvironment()
	{
		if (!string.IsNullOrEmpty(m_forceEnv))
		{
			EnvSetup env = GetEnv(m_forceEnv);
			if (env != null)
			{
				return env;
			}
		}
		return m_currentEnv;
	}

	public bool IsFreezing()
	{
		EnvSetup currentEnvironment = GetCurrentEnvironment();
		if (currentEnvironment == null)
		{
			return false;
		}
		if (currentEnvironment.m_isFreezing)
		{
			return true;
		}
		if (currentEnvironment.m_isFreezingAtNight && !IsDay())
		{
			return true;
		}
		return false;
	}

	public bool IsCold()
	{
		EnvSetup currentEnvironment = GetCurrentEnvironment();
		if (currentEnvironment == null)
		{
			return false;
		}
		if (currentEnvironment.m_isCold)
		{
			return true;
		}
		if (currentEnvironment.m_isColdAtNight && !IsDay())
		{
			return true;
		}
		return false;
	}

	public bool IsWet()
	{
		return GetCurrentEnvironment()?.m_isWet ?? false;
	}

	public Color GetSunFogColor()
	{
		return m_sunFogColor;
	}

	public Vector3 GetSunDirection()
	{
		return m_dirLight.transform.forward;
	}

	private EnvSetup GetEnv(string name)
	{
		foreach (EnvSetup environment in m_environments)
		{
			if (environment.m_name == name)
			{
				return environment;
			}
		}
		return null;
	}

	private EnvSetup GetDefaultEnv()
	{
		foreach (EnvSetup environment in m_environments)
		{
			if (environment.m_default)
			{
				return environment;
			}
		}
		return null;
	}

	public void SetDebugWind(float angle, float intensity)
	{
		m_debugWind = true;
		m_debugWindAngle = angle;
		m_debugWindIntensity = Mathf.Clamp01(intensity);
	}

	public void ResetDebugWind()
	{
		m_debugWind = false;
	}

	public Vector3 GetWindForce()
	{
		return GetWindDir() * m_wind.w;
	}

	public Vector3 GetWindDir()
	{
		return new Vector3(m_wind.x, m_wind.y, m_wind.z);
	}

	public float GetWindIntensity()
	{
		return m_wind.w;
	}

	private void UpdateWind(long timeSec, float dt)
	{
		if (m_debugWind)
		{
			float f = (float)Math.PI / 180f * m_debugWindAngle;
			Vector3 dir = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f));
			SetTargetWind(dir, m_debugWindIntensity);
		}
		else
		{
			EnvSetup currentEnvironment = GetCurrentEnvironment();
			if (currentEnvironment != null)
			{
				UnityEngine.Random.State state = UnityEngine.Random.state;
				float angle = 0f;
				float intensity = 0.5f;
				AddWindOctave(timeSec, 1, ref angle, ref intensity);
				AddWindOctave(timeSec, 2, ref angle, ref intensity);
				AddWindOctave(timeSec, 4, ref angle, ref intensity);
				AddWindOctave(timeSec, 8, ref angle, ref intensity);
				UnityEngine.Random.state = state;
				Vector3 dir2 = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
				intensity = Mathf.Lerp(currentEnvironment.m_windMin, currentEnvironment.m_windMax, intensity);
				if ((bool)Player.m_localPlayer)
				{
					float magnitude = Player.m_localPlayer.transform.position.magnitude;
					if (magnitude > 10500f - m_edgeOfWorldWidth)
					{
						float num = Utils.LerpStep(10500f - m_edgeOfWorldWidth, 10500f, magnitude);
						num = 1f - Mathf.Pow(1f - num, 2f);
						dir2 = Player.m_localPlayer.transform.position.normalized;
						intensity = Mathf.Lerp(intensity, 1f, num);
					}
					else
					{
						Ship localShip = Ship.GetLocalShip();
						if ((bool)localShip && localShip.IsWindControllActive())
						{
							dir2 = localShip.transform.forward;
						}
					}
				}
				SetTargetWind(dir2, intensity);
			}
		}
		UpdateWindTransition(dt);
	}

	private void AddWindOctave(long timeSec, int octave, ref float angle, ref float intensity)
	{
		UnityEngine.Random.InitState((int)(timeSec / (m_windPeriodDuration / octave)));
		angle += UnityEngine.Random.value * ((float)Math.PI * 2f / (float)octave);
		intensity += 0f - 0.5f / (float)octave + UnityEngine.Random.value / (float)octave;
	}

	private void SetTargetWind(Vector3 dir, float intensity)
	{
		if (!(m_windTransitionTimer >= 0f))
		{
			intensity = Mathf.Clamp(intensity, 0.05f, 1f);
			if (!Mathf.Approximately(dir.x, m_windDir1.x) || !Mathf.Approximately(dir.y, m_windDir1.y) || !Mathf.Approximately(dir.z, m_windDir1.z) || !Mathf.Approximately(intensity, m_windDir1.w))
			{
				m_windTransitionTimer = 0f;
				m_windDir2 = new Vector4(dir.x, dir.y, dir.z, intensity);
			}
		}
	}

	private void UpdateWindTransition(float dt)
	{
		if (m_windTransitionTimer >= 0f)
		{
			m_windTransitionTimer += dt;
			float num = Mathf.Clamp01(m_windTransitionTimer / m_windTransitionDuration);
			Shader.SetGlobalVector(_GlobalWind1, m_windDir1);
			Shader.SetGlobalVector(_GlobalWind2, m_windDir2);
			Shader.SetGlobalFloat(_GlobalWindAlpha, num);
			m_wind = Vector4.Lerp(m_windDir1, m_windDir2, num);
			if (num >= 1f)
			{
				m_windDir1 = m_windDir2;
				m_windTransitionTimer = -1f;
			}
		}
		else
		{
			Shader.SetGlobalVector(_GlobalWind1, m_windDir1);
			Shader.SetGlobalFloat(_GlobalWindAlpha, 0f);
			m_wind = m_windDir1;
		}
		Shader.SetGlobalVector(_GlobalWindForce, GetWindForce());
	}

	public void GetWindData(out Vector4 wind1, out Vector4 wind2, out float alpha)
	{
		wind1 = m_windDir1;
		wind2 = m_windDir2;
		if (m_windTransitionTimer >= 0f)
		{
			alpha = Mathf.Clamp01(m_windTransitionTimer / m_windTransitionDuration);
		}
		else
		{
			alpha = 0f;
		}
	}
}
