using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
	public enum Type
	{
		Player,
		Camera
	}

	public Type m_follow = Type.Camera;

	public bool m_lockYPos;

	public bool m_followCameraInFreefly;

	public float m_maxYPos = 1000000f;

	private void LateUpdate()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!(Player.m_localPlayer == null) && !(mainCamera == null))
		{
			Vector3 position = Vector3.zero;
			position = ((m_follow != Type.Camera && !GameCamera.InFreeFly()) ? Player.m_localPlayer.transform.position : mainCamera.transform.position);
			if (m_lockYPos)
			{
				position.y = base.transform.position.y;
			}
			if (position.y > m_maxYPos)
			{
				position.y = m_maxYPos;
			}
			base.transform.position = position;
		}
	}
}
