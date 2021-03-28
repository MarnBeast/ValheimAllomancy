using UnityEngine;

public class TestCollision : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
	}

	public void OnCollisionEnter(Collision info)
	{
		ZLog.Log((object)("Hit by " + ((Component)(object)info.get_rigidbody()).gameObject.name));
		ZLog.Log((object)string.Concat("rel vel ", info.get_relativeVelocity(), " ", info.get_relativeVelocity()));
		ZLog.Log((object)string.Concat("Vel ", info.get_rigidbody().get_velocity(), "  ", info.get_rigidbody().get_angularVelocity()));
	}
}
