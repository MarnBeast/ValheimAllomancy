using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeBase : MonoBehaviour, IDestructible
{
	private ZNetView m_nview;

	public float m_health = 1f;

	public HitData.DamageModifiers m_damageModifiers;

	public int m_minToolTier;

	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public EffectList m_respawnEffect = new EffectList();

	public GameObject m_trunk;

	public GameObject m_stubPrefab;

	public GameObject m_logPrefab;

	public Transform m_logSpawnPoint;

	[Header("Drops")]
	public DropTable m_dropWhenDestroyed = new DropTable();

	public float m_spawnYOffset = 0.5f;

	public float m_spawnYStep = 0.3f;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_nview.Register<HitData>("Damage", RPC_Damage);
		m_nview.Register("Grow", RPC_Grow);
		m_nview.Register("Shake", RPC_Shake);
		if (m_nview.IsOwner() && m_nview.GetZDO().GetFloat("health", m_health) <= 0f)
		{
			m_nview.Destroy();
		}
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Tree;
	}

	public void Damage(HitData hit)
	{
		m_nview.InvokeRPC("Damage", hit);
	}

	public void Grow()
	{
		m_nview.InvokeRPC(ZNetView.Everybody, "Grow");
	}

	private void RPC_Grow(long uid)
	{
		StartCoroutine("GrowAnimation");
	}

	private IEnumerator GrowAnimation()
	{
		GameObject animatedTrunk = Object.Instantiate(m_trunk, m_trunk.transform.position, m_trunk.transform.rotation, base.transform);
		animatedTrunk.isStatic = false;
		LODGroup component = base.transform.GetComponent<LODGroup>();
		if ((bool)component)
		{
			component.fadeMode = LODFadeMode.None;
		}
		m_trunk.SetActive(value: false);
		for (float t = 0f; t < 0.3f; t += Time.deltaTime)
		{
			float d = Mathf.Clamp01(t / 0.3f);
			animatedTrunk.transform.localScale = m_trunk.transform.localScale * d;
			yield return null;
		}
		Object.Destroy(animatedTrunk);
		m_trunk.SetActive(value: true);
		if (m_nview.IsOwner())
		{
			m_respawnEffect.Create(base.transform.position, base.transform.rotation, base.transform);
		}
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		float @float = m_nview.GetZDO().GetFloat("health", m_health);
		if (@float <= 0f)
		{
			m_nview.Destroy();
			return;
		}
		hit.ApplyResistance(m_damageModifiers, out var significantModifier);
		float totalDamage = hit.GetTotalDamage();
		if (hit.m_toolTier < m_minToolTier)
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f);
			return;
		}
		DamageText.instance.ShowText(significantModifier, hit.m_point, totalDamage);
		if (totalDamage <= 0f)
		{
			return;
		}
		@float -= totalDamage;
		m_nview.GetZDO().Set("health", @float);
		Shake();
		m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform);
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
		if ((bool)closestPlayer)
		{
			closestPlayer.AddNoise(100f);
		}
		if (@float <= 0f)
		{
			m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform);
			SpawnLog(hit.m_dir);
			List<GameObject> dropList = m_dropWhenDestroyed.GetDropList();
			for (int i = 0; i < dropList.Count; i++)
			{
				Vector2 vector = Random.insideUnitCircle * 0.5f;
				Vector3 position = base.transform.position + Vector3.up * m_spawnYOffset + new Vector3(vector.x, m_spawnYStep * (float)i, vector.y);
				Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
				Object.Instantiate(dropList[i], position, rotation);
			}
			base.gameObject.SetActive(value: false);
			m_nview.Destroy();
		}
	}

	private void Shake()
	{
		m_nview.InvokeRPC(ZNetView.Everybody, "Shake");
	}

	private void RPC_Shake(long uid)
	{
		StopCoroutine("ShakeAnimation");
		StartCoroutine("ShakeAnimation");
	}

	private IEnumerator ShakeAnimation()
	{
		m_trunk.gameObject.isStatic = false;
		float t = Time.time;
		while (Time.time - t < 1f)
		{
			float time = Time.time;
			float num = 1f - Mathf.Clamp01((time - t) / 1f);
			float num2 = num * num * num * 1.5f;
			Quaternion localRotation = Quaternion.Euler(Mathf.Sin(time * 40f) * num2, 0f, Mathf.Cos(time * 0.9f * 40f) * num2);
			m_trunk.transform.localRotation = localRotation;
			yield return null;
		}
		m_trunk.transform.localRotation = Quaternion.identity;
		m_trunk.gameObject.isStatic = true;
	}

	private void SpawnLog(Vector3 hitDir)
	{
		GameObject gameObject = Object.Instantiate(m_logPrefab, m_logSpawnPoint.position, m_logSpawnPoint.rotation);
		gameObject.GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
		Rigidbody component = gameObject.GetComponent<Rigidbody>();
		component.set_mass(component.get_mass() * base.transform.localScale.x);
		component.ResetInertiaTensor();
		component.AddForceAtPosition(hitDir * 0.2f, gameObject.transform.position + Vector3.up * 4f * base.transform.localScale.y, (ForceMode)2);
		if ((bool)m_stubPrefab)
		{
			Object.Instantiate(m_stubPrefab, base.transform.position, base.transform.rotation).GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
		}
	}
}
