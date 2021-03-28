using UnityEngine;
using UnityEngine.PostProcessing;
using UnityStandardAssets.ImageEffects;

public class CameraEffects : MonoBehaviour
{
	private static CameraEffects m_instance;

	public bool m_forceDof;

	public LayerMask m_dofRayMask;

	public bool m_dofAutoFocus;

	public float m_dofMinDistance = 50f;

	public float m_dofMinDistanceShip = 50f;

	public float m_dofMaxDistance = 3000f;

	private PostProcessingBehaviour m_postProcessing;

	private DepthOfField m_dof;

	public static CameraEffects instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_postProcessing = GetComponent<PostProcessingBehaviour>();
		m_dof = GetComponent<DepthOfField>();
		ApplySettings();
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public void ApplySettings()
	{
		SetDof((PlayerPrefs.GetInt("DOF", 1) == 1) ? true : false);
		SetBloom((PlayerPrefs.GetInt("Bloom", 1) == 1) ? true : false);
		SetSSAO((PlayerPrefs.GetInt("SSAO", 1) == 1) ? true : false);
		SetSunShafts((PlayerPrefs.GetInt("SunShafts", 1) == 1) ? true : false);
		SetAntiAliasing((PlayerPrefs.GetInt("AntiAliasing", 1) == 1) ? true : false);
		SetCA((PlayerPrefs.GetInt("ChromaticAberration", 1) == 1) ? true : false);
		SetMotionBlur((PlayerPrefs.GetInt("MotionBlur", 1) == 1) ? true : false);
	}

	public void SetSunShafts(bool enabled)
	{
		SunShafts component = GetComponent<SunShafts>();
		if ((Object)(object)component != null)
		{
			((Behaviour)(object)component).enabled = enabled;
		}
	}

	private void SetBloom(bool enabled)
	{
		((PostProcessingModel)m_postProcessing.profile.bloom).set_enabled(enabled);
	}

	private void SetSSAO(bool enabled)
	{
		((PostProcessingModel)m_postProcessing.profile.ambientOcclusion).set_enabled(enabled);
	}

	private void SetMotionBlur(bool enabled)
	{
		((PostProcessingModel)m_postProcessing.profile.motionBlur).set_enabled(enabled);
	}

	private void SetAntiAliasing(bool enabled)
	{
		((PostProcessingModel)m_postProcessing.profile.antialiasing).set_enabled(enabled);
	}

	private void SetCA(bool enabled)
	{
		((PostProcessingModel)m_postProcessing.profile.chromaticAberration).set_enabled(enabled);
	}

	private void SetDof(bool enabled)
	{
		((Behaviour)(object)m_dof).enabled = enabled || m_forceDof;
	}

	private void LateUpdate()
	{
		UpdateDOF();
	}

	private bool ControllingShip()
	{
		if (Player.m_localPlayer == null || Player.m_localPlayer.GetControlledShip() != null)
		{
			return true;
		}
		return false;
	}

	private void UpdateDOF()
	{
		if (((Behaviour)(object)m_dof).enabled && m_dofAutoFocus)
		{
			float num = m_dofMaxDistance;
			RaycastHit val = default(RaycastHit);
			if (Physics.Raycast(base.transform.position, base.transform.forward, ref val, m_dofMaxDistance, (int)m_dofRayMask))
			{
				num = ((RaycastHit)(ref val)).get_distance();
			}
			if (ControllingShip() && num < m_dofMinDistanceShip)
			{
				num = m_dofMinDistanceShip;
			}
			if (num < m_dofMinDistance)
			{
				num = m_dofMinDistance;
			}
			m_dof.focalLength = Mathf.Lerp(m_dof.focalLength, num, 0.2f);
		}
	}
}
