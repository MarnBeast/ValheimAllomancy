using System;
using System.Collections.Generic;
using UnityEngine;

public class Vagon : MonoBehaviour, Hoverable, Interactable
{
	[Serializable]
	public class LoadData
	{
		public GameObject m_gameobject;

		public float m_minPercentage;
	}

	private static List<Vagon> m_instances = new List<Vagon>();

	public Transform m_attachPoint;

	public string m_name = "Wagon";

	public float m_detachDistance = 2f;

	public Vector3 m_attachOffset = new Vector3(0f, 0.8f, 0f);

	public Container m_container;

	public Transform m_lineAttachPoints0;

	public Transform m_lineAttachPoints1;

	public Vector3 m_lineAttachOffset = new Vector3(0f, 1f, 0f);

	public float m_breakForce = 10000f;

	public float m_spring = 5000f;

	public float m_springDamping = 1000f;

	public float m_baseMass = 20f;

	public float m_itemWeightMassFactor = 1f;

	public AudioSource[] m_wheelLoops;

	public float m_minPitch = 1f;

	public float m_maxPitch = 1.5f;

	public float m_maxPitchVel = 10f;

	public float m_maxVol = 1f;

	public float m_maxVolVel = 10f;

	public float m_audioChangeSpeed = 2f;

	public Rigidbody[] m_wheels = (Rigidbody[])(object)new Rigidbody[0];

	public List<LoadData> m_loadVis = new List<LoadData>();

	private ZNetView m_nview;

	private ConfigurableJoint m_attachJoin;

	private Rigidbody m_body;

	private LineRenderer m_lineRenderer;

	private Rigidbody[] m_bodies;

	private Humanoid m_useRequester;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		m_instances.Add(this);
		Heightmap.ForceGenerateAll();
		m_body = GetComponent<Rigidbody>();
		m_bodies = GetComponentsInChildren<Rigidbody>();
		m_lineRenderer = GetComponent<LineRenderer>();
		Rigidbody[] bodies = m_bodies;
		for (int i = 0; i < bodies.Length; i++)
		{
			bodies[i].set_maxDepenetrationVelocity(2f);
		}
		m_nview.Register("RequestOwn", RPC_RequestOwn);
		m_nview.Register("RequestDenied", RPC_RequestDenied);
		InvokeRepeating("UpdateMass", 0f, 5f);
		InvokeRepeating("UpdateLoadVisualization", 0f, 3f);
	}

	private void OnDestroy()
	{
		m_instances.Remove(this);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public string GetHoverText()
	{
		return Localization.get_instance().Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] Use");
	}

	public bool Interact(Humanoid character, bool hold)
	{
		if (hold)
		{
			return false;
		}
		m_useRequester = character;
		if (!m_nview.IsOwner())
		{
			m_nview.InvokeRPC("RequestOwn");
		}
		return false;
	}

	public void RPC_RequestOwn(long sender)
	{
		if (m_nview.IsOwner())
		{
			if (InUse())
			{
				ZLog.Log((object)"Requested use, but is already in use");
				m_nview.InvokeRPC(sender, "RequestDenied");
			}
			else
			{
				m_nview.GetZDO().SetOwner(sender);
			}
		}
	}

	private void RPC_RequestDenied(long sender)
	{
		ZLog.Log((object)"Got request denied");
		if ((bool)m_useRequester)
		{
			m_useRequester.Message(MessageHud.MessageType.Center, m_name + " is in use by someone else");
			m_useRequester = null;
		}
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		UpdateAudio(Time.fixedDeltaTime);
		if (m_nview.IsOwner())
		{
			if ((bool)m_useRequester)
			{
				if (IsAttached())
				{
					Detach();
				}
				else if (CanAttach(m_useRequester.gameObject))
				{
					AttachTo(m_useRequester.gameObject);
				}
				else
				{
					m_useRequester.Message(MessageHud.MessageType.Center, "Not in the right position");
				}
				m_useRequester = null;
			}
			if (IsAttached() && !CanAttach(((Component)(object)((Joint)m_attachJoin).get_connectedBody()).gameObject))
			{
				Detach();
			}
		}
		else if (IsAttached())
		{
			Detach();
		}
	}

	private void LateUpdate()
	{
		if (IsAttached())
		{
			m_lineRenderer.enabled = true;
			m_lineRenderer.SetPosition(0, m_lineAttachPoints0.position);
			m_lineRenderer.SetPosition(1, ((Component)(object)((Joint)m_attachJoin).get_connectedBody()).transform.position + m_lineAttachOffset);
			m_lineRenderer.SetPosition(2, m_lineAttachPoints1.position);
		}
		else
		{
			m_lineRenderer.enabled = false;
		}
	}

	public bool IsAttached(Character character)
	{
		if ((bool)(UnityEngine.Object)(object)m_attachJoin && ((Component)(object)((Joint)m_attachJoin).get_connectedBody()).gameObject == character.gameObject)
		{
			return true;
		}
		return false;
	}

	public bool InUse()
	{
		if ((bool)m_container && m_container.IsInUse())
		{
			return true;
		}
		return IsAttached();
	}

	private bool IsAttached()
	{
		return (UnityEngine.Object)(object)m_attachJoin != null;
	}

	private bool CanAttach(GameObject go)
	{
		if (base.transform.up.y < 0.1f)
		{
			return false;
		}
		Humanoid component = go.GetComponent<Humanoid>();
		if ((bool)component && (component.InDodge() || component.IsTeleporting()))
		{
			return false;
		}
		return Vector3.Distance(go.transform.position + m_attachOffset, m_attachPoint.position) < m_detachDistance;
	}

	private void AttachTo(GameObject go)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		DetachAll();
		m_attachJoin = base.gameObject.AddComponent<ConfigurableJoint>();
		((Joint)m_attachJoin).set_autoConfigureConnectedAnchor(false);
		((Joint)m_attachJoin).set_anchor(m_attachPoint.localPosition);
		((Joint)m_attachJoin).set_connectedAnchor(m_attachOffset);
		((Joint)m_attachJoin).set_breakForce(m_breakForce);
		m_attachJoin.set_xMotion((ConfigurableJointMotion)1);
		m_attachJoin.set_yMotion((ConfigurableJointMotion)1);
		m_attachJoin.set_zMotion((ConfigurableJointMotion)1);
		SoftJointLimit linearLimit = default(SoftJointLimit);
		((SoftJointLimit)(ref linearLimit)).set_limit(0.001f);
		m_attachJoin.set_linearLimit(linearLimit);
		SoftJointLimitSpring linearLimitSpring = default(SoftJointLimitSpring);
		((SoftJointLimitSpring)(ref linearLimitSpring)).set_spring(m_spring);
		((SoftJointLimitSpring)(ref linearLimitSpring)).set_damper(m_springDamping);
		m_attachJoin.set_linearLimitSpring(linearLimitSpring);
		m_attachJoin.set_zMotion((ConfigurableJointMotion)0);
		((Joint)m_attachJoin).set_connectedBody(go.GetComponent<Rigidbody>());
	}

	private static void DetachAll()
	{
		foreach (Vagon instance in m_instances)
		{
			instance.Detach();
		}
	}

	private void Detach()
	{
		if ((bool)(UnityEngine.Object)(object)m_attachJoin)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)m_attachJoin);
			m_attachJoin = null;
			m_body.WakeUp();
			m_body.AddForce(0f, 1f, 0f);
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void UpdateMass()
	{
		if (m_nview.IsOwner() && !(m_container == null))
		{
			float totalWeight = m_container.GetInventory().GetTotalWeight();
			float mass = m_baseMass + totalWeight * m_itemWeightMassFactor;
			SetMass(mass);
		}
	}

	private void SetMass(float mass)
	{
		float mass2 = mass / (float)m_bodies.Length;
		Rigidbody[] bodies = m_bodies;
		for (int i = 0; i < bodies.Length; i++)
		{
			bodies[i].set_mass(mass2);
		}
	}

	private void UpdateLoadVisualization()
	{
		if (m_container == null)
		{
			return;
		}
		float num = m_container.GetInventory().SlotsUsedPercentage();
		foreach (LoadData loadVi in m_loadVis)
		{
			loadVi.m_gameobject.SetActive(num >= loadVi.m_minPercentage);
		}
	}

	private void UpdateAudio(float dt)
	{
		float num = 0f;
		Rigidbody[] wheels = m_wheels;
		foreach (Rigidbody val in wheels)
		{
			num += val.get_angularVelocity().magnitude;
		}
		num /= (float)m_wheels.Length;
		float target = Mathf.Lerp(m_minPitch, m_maxPitch, Mathf.Clamp01(num / m_maxPitchVel));
		float target2 = m_maxVol * Mathf.Clamp01(num / m_maxVolVel);
		AudioSource[] wheelLoops = m_wheelLoops;
		foreach (AudioSource obj in wheelLoops)
		{
			obj.set_volume(Mathf.MoveTowards(obj.get_volume(), target2, m_audioChangeSpeed * dt));
			obj.set_pitch(Mathf.MoveTowards(obj.get_pitch(), target, m_audioChangeSpeed * dt));
		}
	}
}
