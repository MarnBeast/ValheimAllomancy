using System.Collections.Generic;
using UnityEngine;

public class ShipEffects : MonoBehaviour
{
	public Transform m_shadow;

	public float m_offset = 0.01f;

	public float m_minimumWakeVel = 5f;

	public GameObject m_speedWakeRoot;

	public GameObject m_wakeSoundRoot;

	public GameObject m_inWaterSoundRoot;

	public float m_audioFadeDuration = 2f;

	public AudioSource m_sailSound;

	public float m_sailFadeDuration = 1f;

	public GameObject m_splashEffects;

	private float m_sailBaseVol = 1f;

	private ParticleSystem[] m_wakeParticles;

	private List<KeyValuePair<AudioSource, float>> m_wakeSounds = new List<KeyValuePair<AudioSource, float>>();

	private List<KeyValuePair<AudioSource, float>> m_inWaterSounds = new List<KeyValuePair<AudioSource, float>>();

	private Rigidbody m_body;

	private Ship m_ship;

	private void Awake()
	{
		ZNetView componentInParent = GetComponentInParent<ZNetView>();
		if ((bool)componentInParent && componentInParent.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		m_body = GetComponentInParent<Rigidbody>();
		m_ship = GetComponentInParent<Ship>();
		if ((bool)m_speedWakeRoot)
		{
			m_wakeParticles = m_speedWakeRoot.GetComponentsInChildren<ParticleSystem>();
		}
		if ((bool)m_wakeSoundRoot)
		{
			AudioSource[] componentsInChildren = m_wakeSoundRoot.GetComponentsInChildren<AudioSource>();
			foreach (AudioSource val in componentsInChildren)
			{
				val.set_pitch(Random.Range(0.9f, 1.1f));
				m_wakeSounds.Add(new KeyValuePair<AudioSource, float>(val, val.get_volume()));
			}
		}
		if ((bool)m_inWaterSoundRoot)
		{
			AudioSource[] componentsInChildren = m_inWaterSoundRoot.GetComponentsInChildren<AudioSource>();
			foreach (AudioSource val2 in componentsInChildren)
			{
				val2.set_pitch(Random.Range(0.9f, 1.1f));
				m_inWaterSounds.Add(new KeyValuePair<AudioSource, float>(val2, val2.get_volume()));
			}
		}
		if ((bool)(Object)(object)m_sailSound)
		{
			m_sailBaseVol = m_sailSound.get_volume();
			m_sailSound.set_pitch(Random.Range(0.9f, 1.1f));
		}
	}

	private void LateUpdate()
	{
		float waterLevel = WaterVolume.GetWaterLevel(base.transform.position);
		Vector3 position = base.transform.position;
		float deltaTime = Time.deltaTime;
		if (position.y > waterLevel)
		{
			m_shadow.gameObject.SetActive(value: false);
			SetWake(enabled: false, deltaTime);
			FadeSounds(m_inWaterSounds, enabled: false, deltaTime);
			return;
		}
		m_shadow.gameObject.SetActive(value: true);
		bool enabled = m_body.get_velocity().magnitude > m_minimumWakeVel;
		FadeSounds(m_inWaterSounds, enabled: true, deltaTime);
		SetWake(enabled, deltaTime);
		if ((bool)(Object)(object)m_sailSound)
		{
			float target = (m_ship.IsSailUp() ? m_sailBaseVol : 0f);
			FadeSound(m_sailSound, target, m_sailFadeDuration, deltaTime);
		}
		if (m_splashEffects != null)
		{
			m_splashEffects.SetActive(m_ship.HasPlayerOnboard());
		}
	}

	private void SetWake(bool enabled, float dt)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		ParticleSystem[] wakeParticles = m_wakeParticles;
		for (int i = 0; i < wakeParticles.Length; i++)
		{
			EmissionModule emission = wakeParticles[i].get_emission();
			((EmissionModule)(ref emission)).set_enabled(enabled);
		}
		FadeSounds(m_wakeSounds, enabled, dt);
	}

	private void FadeSounds(List<KeyValuePair<AudioSource, float>> sources, bool enabled, float dt)
	{
		foreach (KeyValuePair<AudioSource, float> source in sources)
		{
			if (enabled)
			{
				FadeSound(source.Key, source.Value, m_audioFadeDuration, dt);
			}
			else
			{
				FadeSound(source.Key, 0f, m_audioFadeDuration, dt);
			}
		}
	}

	private void FadeSound(AudioSource source, float target, float fadeDuration, float dt)
	{
		float maxDelta = dt / fadeDuration;
		if (target > 0f)
		{
			if (!source.get_isPlaying())
			{
				source.Play();
			}
			source.set_volume(Mathf.MoveTowards(source.get_volume(), target, maxDelta));
		}
		else if (source.get_isPlaying())
		{
			source.set_volume(Mathf.MoveTowards(source.get_volume(), 0f, maxDelta));
			if (source.get_volume() <= 0f)
			{
				source.Stop();
			}
		}
	}
}
