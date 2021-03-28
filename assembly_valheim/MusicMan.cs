using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicMan : MonoBehaviour
{
	[Serializable]
	public class NamedMusic
	{
		public string m_name = "";

		public AudioClip[] m_clips;

		public float m_volume = 1f;

		public float m_fadeInTime = 3f;

		public bool m_alwaysFadeout;

		public bool m_loop;

		public bool m_resume;

		public bool m_enabled = true;

		public bool m_ambientMusic;

		[NonSerialized]
		public int m_savedPlaybackPos;

		[NonSerialized]
		public float m_lastPlayedTime;
	}

	private string m_triggeredMusic = "";

	private static MusicMan m_instance;

	public static float m_masterMusicVolume = 1f;

	public AudioMixerGroup m_musicMixer;

	public List<NamedMusic> m_music = new List<NamedMusic>();

	[Header("Combat")]
	public float m_combatMusicTimeout = 4f;

	[Header("Sailing")]
	public float m_sailMusicShipSpeedThreshold = 3f;

	public float m_sailMusicMinSailTime = 20f;

	[Header("Ambient music")]
	public float m_randomMusicIntervalMin = 300f;

	public float m_randomMusicIntervalMax = 500f;

	private NamedMusic m_queuedMusic;

	private NamedMusic m_currentMusic;

	private float m_musicVolume = 1f;

	private float m_musicFadeTime = 3f;

	private bool m_alwaysFadeout;

	private bool m_stopMusic;

	private string m_randomEventMusic;

	private float m_lastAmbientMusicTime;

	private float m_randomAmbientInterval;

	private string m_triggerMusic;

	private float m_combatTimer;

	private AudioSource m_musicSource;

	private float m_currentMusicVol;

	private float m_sailDuration;

	private float m_notSailDuration;

	public static MusicMan instance => m_instance;

	private void Awake()
	{
		if ((bool)m_instance)
		{
			return;
		}
		m_instance = this;
		GameObject gameObject = new GameObject("music");
		gameObject.transform.SetParent(base.transform);
		m_musicSource = gameObject.AddComponent<AudioSource>();
		m_musicSource.set_loop(true);
		m_musicSource.set_spatialBlend(0f);
		m_musicSource.set_outputAudioMixerGroup(m_musicMixer);
		m_musicSource.set_bypassReverbZones(true);
		m_randomAmbientInterval = UnityEngine.Random.Range(m_randomMusicIntervalMin, m_randomMusicIntervalMax);
		m_masterMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
		ApplySettings();
		foreach (NamedMusic item in m_music)
		{
			AudioClip[] clips = item.m_clips;
			foreach (AudioClip val in clips)
			{
				if ((UnityEngine.Object)(object)val == null || !(UnityEngine.Object)(object)val)
				{
					item.m_enabled = false;
					ZLog.LogWarning((object)("Missing audio clip in music " + item.m_name));
					break;
				}
			}
		}
	}

	public void ApplySettings()
	{
		bool flag = ((PlayerPrefs.GetInt("ContinousMusic", 1) == 1) ? true : false);
		foreach (NamedMusic item in m_music)
		{
			if (item.m_ambientMusic)
			{
				item.m_loop = flag;
				if (!flag && GetCurrentMusic() == item.m_name && m_musicSource.get_loop())
				{
					StopMusic();
				}
			}
		}
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	private void Update()
	{
		if (!(m_instance != this))
		{
			float deltaTime = Time.deltaTime;
			UpdateCurrentMusic(deltaTime);
			UpdateCombatMusic(deltaTime);
			UpdateMusic(deltaTime);
		}
	}

	private void UpdateCurrentMusic(float dt)
	{
		string currentMusic = GetCurrentMusic();
		if (Game.instance != null)
		{
			if (Player.m_localPlayer == null)
			{
				StartMusic("respawn");
				return;
			}
			if (currentMusic == "respawn")
			{
				StopMusic();
			}
		}
		if ((bool)Player.m_localPlayer && Player.m_localPlayer.InIntro())
		{
			StartMusic("intro");
			return;
		}
		if (currentMusic == "intro")
		{
			StopMusic();
		}
		if (!HandleEventMusic(currentMusic) && !HandleSailingMusic(dt, currentMusic) && !HandleTriggerMusic(currentMusic))
		{
			HandleEnvironmentMusic(dt, currentMusic);
		}
	}

	private bool HandleEnvironmentMusic(float dt, string currentMusic)
	{
		if (!EnvMan.instance)
		{
			return false;
		}
		NamedMusic environmentMusic = GetEnvironmentMusic();
		if (environmentMusic == null || (!environmentMusic.m_loop && environmentMusic.m_name != GetCurrentMusic()))
		{
			StopMusic();
			return true;
		}
		if (!environmentMusic.m_loop)
		{
			if (Time.time - m_lastAmbientMusicTime < m_randomAmbientInterval)
			{
				return false;
			}
			m_randomAmbientInterval = UnityEngine.Random.Range(m_randomMusicIntervalMin, m_randomMusicIntervalMax);
			m_lastAmbientMusicTime = Time.time;
		}
		StartMusic(environmentMusic);
		return true;
	}

	private NamedMusic GetEnvironmentMusic()
	{
		string text = null;
		text = ((!Player.m_localPlayer || !Player.m_localPlayer.IsSafeInHome()) ? EnvMan.instance.GetAmbientMusic() : "home");
		return FindMusic(text);
	}

	private bool HandleTriggerMusic(string currentMusic)
	{
		if (m_triggerMusic != null)
		{
			StartMusic(m_triggerMusic);
			m_triggeredMusic = m_triggerMusic;
			m_triggerMusic = null;
			return true;
		}
		if (m_triggeredMusic != null)
		{
			if (currentMusic == m_triggeredMusic)
			{
				return true;
			}
			m_triggeredMusic = null;
		}
		return false;
	}

	private bool HandleEventMusic(string currentMusic)
	{
		if ((bool)RandEventSystem.instance)
		{
			string musicOverride = RandEventSystem.instance.GetMusicOverride();
			if (musicOverride != null)
			{
				StartMusic(musicOverride);
				m_randomEventMusic = musicOverride;
				return true;
			}
			if (currentMusic == m_randomEventMusic)
			{
				m_randomEventMusic = null;
				StopMusic();
			}
		}
		return false;
	}

	private bool HandleCombatMusic(string currentMusic)
	{
		if (InCombat())
		{
			StartMusic("combat");
			return true;
		}
		if (currentMusic == "combat")
		{
			StopMusic();
		}
		return false;
	}

	private bool HandleSailingMusic(float dt, string currentMusic)
	{
		if (IsSailing())
		{
			m_notSailDuration = 0f;
			m_sailDuration += dt;
			if (m_sailDuration > m_sailMusicMinSailTime)
			{
				StartMusic("sailing");
				return true;
			}
		}
		else
		{
			m_sailDuration = 0f;
			m_notSailDuration += dt;
			if (m_notSailDuration > m_sailMusicMinSailTime / 2f && currentMusic == "sailing")
			{
				StopMusic();
			}
		}
		return false;
	}

	private bool IsSailing()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		Ship localShip = Ship.GetLocalShip();
		if ((bool)localShip && localShip.GetSpeed() > m_sailMusicShipSpeedThreshold)
		{
			return true;
		}
		return false;
	}

	private void UpdateMusic(float dt)
	{
		if (m_queuedMusic != null || m_stopMusic)
		{
			if (!m_musicSource.get_isPlaying() || m_currentMusicVol <= 0f)
			{
				if (m_musicSource.get_isPlaying() && m_currentMusic != null && m_currentMusic.m_loop && m_currentMusic.m_resume)
				{
					m_currentMusic.m_lastPlayedTime = Time.time;
					m_currentMusic.m_savedPlaybackPos = m_musicSource.get_timeSamples();
					ZLog.Log((object)("Stoped music " + m_currentMusic.m_name + " at " + m_currentMusic.m_savedPlaybackPos));
				}
				m_musicSource.Stop();
				m_stopMusic = false;
				m_currentMusic = null;
				if (m_queuedMusic != null)
				{
					m_musicSource.set_clip(m_queuedMusic.m_clips[UnityEngine.Random.Range(0, m_queuedMusic.m_clips.Length)]);
					m_musicSource.set_loop(m_queuedMusic.m_loop);
					m_musicSource.set_volume(0f);
					m_musicSource.set_timeSamples(0);
					m_musicSource.Play();
					if (m_queuedMusic.m_loop && m_queuedMusic.m_resume && Time.time - m_queuedMusic.m_lastPlayedTime < m_musicSource.get_clip().get_length() * 2f)
					{
						m_musicSource.set_timeSamples(m_queuedMusic.m_savedPlaybackPos);
						ZLog.Log((object)("Resumed music " + m_queuedMusic.m_name + " at " + m_queuedMusic.m_savedPlaybackPos));
					}
					m_currentMusicVol = 0f;
					m_musicVolume = m_queuedMusic.m_volume;
					m_musicFadeTime = m_queuedMusic.m_fadeInTime;
					m_alwaysFadeout = m_queuedMusic.m_alwaysFadeout;
					m_currentMusic = m_queuedMusic;
					m_queuedMusic = null;
				}
			}
			else
			{
				float num = ((m_queuedMusic != null) ? Mathf.Min(m_queuedMusic.m_fadeInTime, m_musicFadeTime) : m_musicFadeTime);
				m_currentMusicVol = Mathf.MoveTowards(m_currentMusicVol, 0f, dt / num);
				m_musicSource.set_volume(Utils.SmoothStep(0f, 1f, m_currentMusicVol) * m_musicVolume * m_masterMusicVolume);
			}
		}
		else if (m_musicSource.get_isPlaying())
		{
			float num2 = m_musicSource.get_clip().get_length() - m_musicSource.get_time();
			if (m_alwaysFadeout && !m_musicSource.get_loop() && num2 < m_musicFadeTime)
			{
				m_currentMusicVol = Mathf.MoveTowards(m_currentMusicVol, 0f, dt / m_musicFadeTime);
				m_musicSource.set_volume(Utils.SmoothStep(0f, 1f, m_currentMusicVol) * m_musicVolume * m_masterMusicVolume);
			}
			else
			{
				m_currentMusicVol = Mathf.MoveTowards(m_currentMusicVol, 1f, dt / m_musicFadeTime);
				m_musicSource.set_volume(Utils.SmoothStep(0f, 1f, m_currentMusicVol) * m_musicVolume * m_masterMusicVolume);
			}
		}
		else if (m_currentMusic != null && !m_musicSource.get_isPlaying())
		{
			m_currentMusic = null;
		}
	}

	private void UpdateCombatMusic(float dt)
	{
		if (m_combatTimer > 0f)
		{
			m_combatTimer -= Time.deltaTime;
		}
	}

	public void ResetCombatTimer()
	{
		m_combatTimer = m_combatMusicTimeout;
	}

	private bool InCombat()
	{
		return m_combatTimer > 0f;
	}

	public void TriggerMusic(string name)
	{
		m_triggerMusic = name;
	}

	private void StartMusic(string name)
	{
		if (!(GetCurrentMusic() == name))
		{
			NamedMusic music = FindMusic(name);
			StartMusic(music);
		}
	}

	private void StartMusic(NamedMusic music)
	{
		if (music == null || !(GetCurrentMusic() == music.m_name))
		{
			if (music != null)
			{
				m_queuedMusic = music;
				m_stopMusic = false;
			}
			else
			{
				StopMusic();
			}
		}
	}

	private NamedMusic FindMusic(string name)
	{
		if (name == null || name.Length == 0)
		{
			return null;
		}
		foreach (NamedMusic item in m_music)
		{
			if (item.m_name == name && item.m_enabled && item.m_clips.Length != 0 && (bool)(UnityEngine.Object)(object)item.m_clips[0])
			{
				return item;
			}
		}
		return null;
	}

	public bool IsPlaying()
	{
		return m_musicSource.get_isPlaying();
	}

	private string GetCurrentMusic()
	{
		if (m_stopMusic)
		{
			return "";
		}
		if (m_queuedMusic != null)
		{
			return m_queuedMusic.m_name;
		}
		if (m_currentMusic != null)
		{
			return m_currentMusic.m_name;
		}
		return "";
	}

	private void StopMusic()
	{
		m_queuedMusic = null;
		m_stopMusic = true;
	}

	public void Reset()
	{
		StopMusic();
		m_combatTimer = 0f;
		m_randomEventMusic = null;
		m_triggerMusic = null;
	}
}
