using UnityEngine;

public class GlobalWind : MonoBehaviour
{
	public float m_multiplier = 1f;

	public bool m_smoothUpdate;

	public bool m_alignToWindDirection;

	[Header("Particles")]
	public bool m_particleVelocity = true;

	public bool m_particleForce;

	public bool m_particleEmission;

	public int m_particleEmissionMin;

	public int m_particleEmissionMax = 1;

	[Header("Cloth")]
	public float m_clothRandomAccelerationFactor = 0.5f;

	public bool m_checkPlayerShelter;

	private ParticleSystem m_ps;

	private Cloth m_cloth;

	private Player m_player;

	private void Start()
	{
		if (!(EnvMan.instance == null))
		{
			m_ps = GetComponent<ParticleSystem>();
			m_cloth = GetComponent<Cloth>();
			if (m_checkPlayerShelter)
			{
				m_player = GetComponentInParent<Player>();
			}
			if (m_smoothUpdate)
			{
				InvokeRepeating("UpdateWind", 0f, 0.01f);
				return;
			}
			InvokeRepeating("UpdateWind", Random.Range(1.5f, 2.5f), 2f);
			UpdateWind();
		}
	}

	private void UpdateWind()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		if (m_alignToWindDirection)
		{
			Vector3 windDir = EnvMan.instance.GetWindDir();
			base.transform.rotation = Quaternion.LookRotation(windDir, Vector3.up);
		}
		if ((bool)(Object)(object)m_ps)
		{
			EmissionModule emission = m_ps.get_emission();
			if (!((EmissionModule)(ref emission)).get_enabled())
			{
				return;
			}
			Vector3 windForce = EnvMan.instance.GetWindForce();
			if (m_particleVelocity)
			{
				VelocityOverLifetimeModule velocityOverLifetime = m_ps.get_velocityOverLifetime();
				((VelocityOverLifetimeModule)(ref velocityOverLifetime)).set_space((ParticleSystemSimulationSpace)1);
				((VelocityOverLifetimeModule)(ref velocityOverLifetime)).set_x(MinMaxCurve.op_Implicit(windForce.x * m_multiplier));
				((VelocityOverLifetimeModule)(ref velocityOverLifetime)).set_z(MinMaxCurve.op_Implicit(windForce.z * m_multiplier));
			}
			if (m_particleForce)
			{
				ForceOverLifetimeModule forceOverLifetime = m_ps.get_forceOverLifetime();
				((ForceOverLifetimeModule)(ref forceOverLifetime)).set_space((ParticleSystemSimulationSpace)1);
				((ForceOverLifetimeModule)(ref forceOverLifetime)).set_x(MinMaxCurve.op_Implicit(windForce.x * m_multiplier));
				((ForceOverLifetimeModule)(ref forceOverLifetime)).set_z(MinMaxCurve.op_Implicit(windForce.z * m_multiplier));
			}
			if (m_particleEmission)
			{
				EmissionModule emission2 = m_ps.get_emission();
				((EmissionModule)(ref emission2)).set_rateOverTimeMultiplier(Mathf.Lerp(m_particleEmissionMin, m_particleEmissionMax, EnvMan.instance.GetWindIntensity()));
			}
		}
		if ((bool)(Object)(object)m_cloth)
		{
			Vector3 a = EnvMan.instance.GetWindForce();
			if (m_checkPlayerShelter && m_player != null && m_player.InShelter())
			{
				a = Vector3.zero;
			}
			m_cloth.set_externalAcceleration(a * m_multiplier);
			m_cloth.set_randomAcceleration(a * m_multiplier * m_clothRandomAccelerationFactor);
		}
	}
}
