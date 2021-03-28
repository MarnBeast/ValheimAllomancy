using System;
using System.IO;
using UnityEngine;

public class GameCamera : MonoBehaviour
{
	private Vector3 m_playerPos = Vector3.zero;

	private Vector3 m_currentBaseOffset = Vector3.zero;

	private Vector3 m_offsetBaseVel = Vector3.zero;

	private Vector3 m_playerVel = Vector3.zero;

	public Vector3 m_3rdOffset = Vector3.zero;

	public Vector3 m_3rdCombatOffset = Vector3.zero;

	public Vector3 m_fpsOffset = Vector3.zero;

	public float m_flyingDistance = 15f;

	public LayerMask m_blockCameraMask;

	public float m_minDistance;

	public float m_maxDistance = 6f;

	public float m_maxDistanceBoat = 6f;

	public float m_raycastWidth = 0.35f;

	public bool m_smoothYTilt;

	public float m_zoomSens = 10f;

	public float m_inventoryOffset = 0.1f;

	public float m_nearClipPlaneMin = 0.1f;

	public float m_nearClipPlaneMax = 0.5f;

	public float m_fov = 65f;

	public float m_freeFlyMinFov = 5f;

	public float m_freeFlyMaxFov = 120f;

	public float m_tiltSmoothnessShipMin = 0.1f;

	public float m_tiltSmoothnessShipMax = 0.5f;

	public float m_shakeFreq = 10f;

	public float m_shakeMovement = 1f;

	public float m_smoothness = 0.1f;

	public float m_minWaterDistance = 0.3f;

	public Camera m_skyCamera;

	private float m_distance = 4f;

	private bool m_freeFly;

	private float m_shakeIntensity;

	private float m_shakeTimer;

	private bool m_cameraShakeEnabled = true;

	private bool m_mouseCapture;

	private Quaternion m_freeFlyRef = Quaternion.identity;

	private float m_freeFlyYaw;

	private float m_freeFlyPitch;

	private float m_freeFlySpeed = 20f;

	private float m_freeFlySmooth;

	private Vector3 m_freeFlySavedVel = Vector3.zero;

	private Transform m_freeFlyTarget;

	private Vector3 m_freeFlyTargetOffset = Vector3.zero;

	private Transform m_freeFlyLockon;

	private Vector3 m_freeFlyLockonOffset = Vector3.zero;

	private Vector3 m_freeFlyVel = Vector3.zero;

	private Vector3 m_freeFlyAcc = Vector3.zero;

	private Vector3 m_freeFlyTurnVel = Vector3.zero;

	private bool m_shipCameraTilt = true;

	private Vector3 m_smoothedCameraUp = Vector3.up;

	private Vector3 m_smoothedCameraUpVel = Vector3.zero;

	private AudioListener m_listner;

	private Camera m_camera;

	private bool m_waterClipping;

	private static GameCamera m_instance;

	public static GameCamera instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_camera = GetComponent<Camera>();
		m_listner = GetComponentInChildren<AudioListener>();
		m_camera.depthTextureMode = DepthTextureMode.DepthNormals;
		ApplySettings();
		if (!Application.isEditor)
		{
			m_mouseCapture = true;
		}
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
		m_cameraShakeEnabled = ((PlayerPrefs.GetInt("CameraShake", 1) == 1) ? true : false);
		m_shipCameraTilt = ((PlayerPrefs.GetInt("ShipCameraTilt", 1) == 1) ? true : false);
	}

	private void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		if (Input.GetKeyDown(KeyCode.F11) || (m_freeFly && Input.GetKeyDown(KeyCode.Mouse1)))
		{
			ScreenShot();
		}
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			UpdateBaseOffset(localPlayer, deltaTime);
		}
		UpdateMouseCapture();
		UpdateCamera(deltaTime);
		UpdateListner();
	}

	private void UpdateMouseCapture()
	{
		if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F1))
		{
			m_mouseCapture = !m_mouseCapture;
		}
		if (m_mouseCapture && !InventoryGui.IsVisible() && !Menu.IsVisible() && !Minimap.IsOpen() && !StoreGui.IsVisible() && !Hud.IsPieceSelectionVisible())
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = ZInput.IsMouseActive();
		}
	}

	public static void ScreenShot()
	{
		DateTime now = DateTime.Now;
		Directory.CreateDirectory(Utils.GetSaveDataPath() + "/screenshots");
		string text = now.Hour.ToString("00") + now.Minute.ToString("00") + now.Second.ToString("00");
		string text2 = now.ToString("yyyy-MM-dd");
		string text3 = Utils.GetSaveDataPath() + "/screenshots/screenshot_" + text2 + "_" + text + ".png";
		if (!File.Exists(text3))
		{
			ScreenCapture.CaptureScreenshot(text3);
			ZLog.Log((object)("Screenshot saved:" + text3));
		}
	}

	private void UpdateListner()
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer && !m_freeFly)
		{
			((Component)(object)m_listner).transform.position = localPlayer.m_eye.position;
		}
		else
		{
			((Component)(object)m_listner).transform.localPosition = Vector3.zero;
		}
	}

	private void UpdateCamera(float dt)
	{
		if (m_freeFly)
		{
			UpdateFreeFly(dt);
			UpdateCameraShake(dt);
			return;
		}
		m_camera.fieldOfView = m_fov;
		m_skyCamera.fieldOfView = m_fov;
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			if ((!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !InventoryGui.IsVisible() && !StoreGui.IsVisible() && !Menu.IsVisible() && !Minimap.IsOpen() && !localPlayer.InCutscene() && !localPlayer.InPlaceMode())
			{
				float minDistance = m_minDistance;
				float axis = Input.GetAxis("Mouse ScrollWheel");
				m_distance -= axis * m_zoomSens;
				float max = ((localPlayer.GetControlledShip() != null) ? m_maxDistanceBoat : m_maxDistance);
				m_distance = Mathf.Clamp(m_distance, minDistance, max);
			}
			if (localPlayer.IsDead() && (bool)localPlayer.GetRagdoll())
			{
				Vector3 averageBodyPosition = localPlayer.GetRagdoll().GetAverageBodyPosition();
				base.transform.LookAt(averageBodyPosition);
			}
			else
			{
				GetCameraPosition(dt, out var pos, out var rot);
				base.transform.position = pos;
				base.transform.rotation = rot;
			}
			UpdateCameraShake(dt);
		}
	}

	private void GetCameraPosition(float dt, out Vector3 pos, out Quaternion rot)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			pos = base.transform.position;
			rot = base.transform.rotation;
			return;
		}
		Vector3 vector = GetOffsetedEyePos();
		float num = m_distance;
		if (localPlayer.InIntro())
		{
			vector = localPlayer.transform.position;
			num = m_flyingDistance;
		}
		Vector3 a = -localPlayer.m_eye.transform.forward;
		if (m_smoothYTilt && !localPlayer.InIntro())
		{
			num = Mathf.Lerp(num, 1.5f, Utils.SmoothStep(0f, -0.5f, a.y));
		}
		Vector3 end = vector + a * num;
		CollideRay2(localPlayer.m_eye.position, vector, ref end);
		UpdateNearClipping(vector, end, dt);
		float waterLevel = WaterVolume.GetWaterLevel(end);
		if (end.y < waterLevel + m_minWaterDistance)
		{
			end.y = waterLevel + m_minWaterDistance;
			m_waterClipping = true;
		}
		else
		{
			m_waterClipping = false;
		}
		pos = end;
		rot = localPlayer.m_eye.transform.rotation;
		if (m_shipCameraTilt)
		{
			ApplyCameraTilt(localPlayer, dt, ref rot);
		}
	}

	private void ApplyCameraTilt(Player player, float dt, ref Quaternion rot)
	{
		if (!player.InIntro())
		{
			Ship standingOnShip = player.GetStandingOnShip();
			float f = Mathf.Clamp01((m_distance - m_minDistance) / (m_maxDistanceBoat - m_minDistance));
			f = Mathf.Pow(f, 2f);
			float smoothTime = Mathf.Lerp(m_tiltSmoothnessShipMin, m_tiltSmoothnessShipMax, f);
			Vector3 up = Vector3.up;
			if (standingOnShip != null)
			{
				up = standingOnShip.transform.up;
			}
			else if (player.IsAttached())
			{
				up = player.GetVisual().transform.up;
			}
			Vector3 forward = player.m_eye.transform.forward;
			Vector3 target = Vector3.Lerp(up, Vector3.up, f * 0.5f);
			m_smoothedCameraUp = Vector3.SmoothDamp(m_smoothedCameraUp, target, ref m_smoothedCameraUpVel, smoothTime, 99f, dt);
			rot = Quaternion.LookRotation(forward, m_smoothedCameraUp);
		}
	}

	private void UpdateNearClipping(Vector3 eyePos, Vector3 camPos, float dt)
	{
		float num = m_nearClipPlaneMax;
		Vector3 normalized = (camPos - eyePos).normalized;
		if (m_waterClipping || Physics.CheckSphere(camPos - normalized * m_nearClipPlaneMax, m_nearClipPlaneMax, (int)m_blockCameraMask))
		{
			num = m_nearClipPlaneMin;
		}
		if (m_camera.nearClipPlane != num)
		{
			m_camera.nearClipPlane = num;
		}
	}

	private void CollideRay2(Vector3 eyePos, Vector3 offsetedEyePos, ref Vector3 end)
	{
		if (RayTestPoint(eyePos, offsetedEyePos, (end - offsetedEyePos).normalized, Vector3.Distance(eyePos, end), out var distance))
		{
			float t = Utils.LerpStep(0.5f, 2f, distance);
			Vector3 a = eyePos + (end - eyePos).normalized * distance;
			Vector3 b = offsetedEyePos + (end - offsetedEyePos).normalized * distance;
			end = Vector3.Lerp(a, b, t);
		}
	}

	private bool RayTestPoint(Vector3 point, Vector3 offsetedPoint, Vector3 dir, float maxDist, out float distance)
	{
		bool result = false;
		distance = maxDist;
		RaycastHit val = default(RaycastHit);
		if (Physics.SphereCast(offsetedPoint, m_raycastWidth, dir, ref val, maxDist, (int)m_blockCameraMask))
		{
			distance = ((RaycastHit)(ref val)).get_distance();
			result = true;
		}
		_ = offsetedPoint + dir * distance;
		if (Physics.SphereCast(point, m_raycastWidth, dir, ref val, maxDist, (int)m_blockCameraMask))
		{
			if (((RaycastHit)(ref val)).get_distance() < distance)
			{
				distance = ((RaycastHit)(ref val)).get_distance();
			}
			result = true;
		}
		if (Physics.Raycast(point, dir, ref val, maxDist, (int)m_blockCameraMask))
		{
			float num = ((RaycastHit)(ref val)).get_distance() - m_nearClipPlaneMin;
			if (num < distance)
			{
				distance = num;
			}
			result = true;
		}
		return result;
	}

	private bool RayTestPoint(Vector3 point, Vector3 dir, float maxDist, out Vector3 hitPoint)
	{
		RaycastHit val = default(RaycastHit);
		if (Physics.SphereCast(point, 0.2f, dir, ref val, maxDist, (int)m_blockCameraMask))
		{
			hitPoint = point + dir * ((RaycastHit)(ref val)).get_distance();
			return true;
		}
		if (Physics.Raycast(point, dir, ref val, maxDist, (int)m_blockCameraMask))
		{
			hitPoint = point + dir * (((RaycastHit)(ref val)).get_distance() - 0.05f);
			return true;
		}
		hitPoint = Vector3.zero;
		return false;
	}

	private void UpdateFreeFly(float dt)
	{
		if (Console.IsVisible())
		{
			return;
		}
		Vector2 zero = Vector2.zero;
		zero.x = Input.GetAxis("Mouse X");
		zero.y = Input.GetAxis("Mouse Y");
		zero.x += ZInput.GetJoyRightStickX() * 110f * dt;
		zero.y += (0f - ZInput.GetJoyRightStickY()) * 110f * dt;
		m_freeFlyYaw += zero.x;
		m_freeFlyPitch -= zero.y;
		if (Input.GetAxis("Mouse ScrollWheel") < 0f)
		{
			m_freeFlySpeed *= 0.8f;
		}
		if (Input.GetAxis("Mouse ScrollWheel") > 0f)
		{
			m_freeFlySpeed *= 1.2f;
		}
		if (Input.GetAxis("Mouse ScrollWheel") > 0f)
		{
			m_freeFlySpeed *= 1.2f;
		}
		if (ZInput.GetButton("JoyTabLeft"))
		{
			m_camera.fieldOfView = Mathf.Max(m_freeFlyMinFov, m_camera.fieldOfView - dt * 20f);
		}
		if (ZInput.GetButton("JoyTabRight"))
		{
			m_camera.fieldOfView = Mathf.Min(m_freeFlyMaxFov, m_camera.fieldOfView + dt * 20f);
		}
		m_skyCamera.fieldOfView = m_camera.fieldOfView;
		if (ZInput.GetButton("JoyButtonY"))
		{
			m_freeFlySpeed += m_freeFlySpeed * 0.1f * dt * 10f;
		}
		if (ZInput.GetButton("JoyButtonX"))
		{
			m_freeFlySpeed -= m_freeFlySpeed * 0.1f * dt * 10f;
		}
		m_freeFlySpeed = Mathf.Clamp(m_freeFlySpeed, 1f, 1000f);
		if (ZInput.GetButtonDown("JoyLStick") || ZInput.GetButtonDown("SecondAttack"))
		{
			if ((bool)m_freeFlyLockon)
			{
				m_freeFlyLockon = null;
			}
			else
			{
				int mask = LayerMask.GetMask("Default", "static_solid", "terrain", "vehicle", "character", "piece", "character_net", "viewblock");
				RaycastHit val = default(RaycastHit);
				if (Physics.Raycast(base.transform.position, base.transform.forward, ref val, 10000f, mask))
				{
					m_freeFlyLockon = ((Component)(object)((RaycastHit)(ref val)).get_collider()).transform;
					m_freeFlyLockonOffset = m_freeFlyLockon.InverseTransformPoint(base.transform.position);
				}
			}
		}
		Vector3 vector = Vector3.zero;
		if (ZInput.GetButton("Left"))
		{
			vector -= Vector3.right;
		}
		if (ZInput.GetButton("Right"))
		{
			vector += Vector3.right;
		}
		if (ZInput.GetButton("Forward"))
		{
			vector += Vector3.forward;
		}
		if (ZInput.GetButton("Backward"))
		{
			vector -= Vector3.forward;
		}
		if (ZInput.GetButton("Jump"))
		{
			vector += Vector3.up;
		}
		if (ZInput.GetButton("Crouch"))
		{
			vector -= Vector3.up;
		}
		vector += Vector3.up * ZInput.GetJoyRTrigger();
		vector -= Vector3.up * ZInput.GetJoyLTrigger();
		vector += Vector3.right * ZInput.GetJoyLeftStickX();
		vector += -Vector3.forward * ZInput.GetJoyLeftStickY();
		if (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("Block"))
		{
			m_freeFlySavedVel = vector;
		}
		float magnitude = m_freeFlySavedVel.magnitude;
		if (magnitude > 0.001f)
		{
			vector += m_freeFlySavedVel;
			if (vector.magnitude > magnitude)
			{
				vector = vector.normalized * magnitude;
			}
		}
		if (vector.magnitude > 1f)
		{
			vector.Normalize();
		}
		vector = base.transform.TransformVector(vector);
		vector *= m_freeFlySpeed;
		if (m_freeFlySmooth <= 0f)
		{
			m_freeFlyVel = vector;
		}
		else
		{
			m_freeFlyVel = Vector3.SmoothDamp(m_freeFlyVel, vector, ref m_freeFlyAcc, m_freeFlySmooth, 99f, dt);
		}
		if ((bool)m_freeFlyLockon)
		{
			m_freeFlyLockonOffset += m_freeFlyLockon.InverseTransformVector(m_freeFlyVel * dt);
			base.transform.position = m_freeFlyLockon.TransformPoint(m_freeFlyLockonOffset);
		}
		else
		{
			base.transform.position = base.transform.position + m_freeFlyVel * dt;
		}
		Quaternion quaternion = Quaternion.Euler(0f, m_freeFlyYaw, 0f) * Quaternion.Euler(m_freeFlyPitch, 0f, 0f);
		if ((bool)m_freeFlyLockon)
		{
			quaternion = m_freeFlyLockon.rotation * quaternion;
		}
		if (ZInput.GetButtonDown("JoyRStick") || ZInput.GetButtonDown("Attack"))
		{
			if ((bool)m_freeFlyTarget)
			{
				m_freeFlyTarget = null;
			}
			else
			{
				int mask2 = LayerMask.GetMask("Default", "static_solid", "terrain", "vehicle", "character", "piece", "character_net", "viewblock");
				RaycastHit val2 = default(RaycastHit);
				if (Physics.Raycast(base.transform.position, base.transform.forward, ref val2, 10000f, mask2))
				{
					m_freeFlyTarget = ((Component)(object)((RaycastHit)(ref val2)).get_collider()).transform;
					m_freeFlyTargetOffset = m_freeFlyTarget.InverseTransformPoint(((RaycastHit)(ref val2)).get_point());
				}
			}
		}
		if ((bool)m_freeFlyTarget)
		{
			quaternion = Quaternion.LookRotation((m_freeFlyTarget.TransformPoint(m_freeFlyTargetOffset) - base.transform.position).normalized, Vector3.up);
		}
		if (m_freeFlySmooth <= 0f)
		{
			base.transform.rotation = quaternion;
			return;
		}
		Quaternion rotation = Utils.SmoothDamp(base.transform.rotation, quaternion, ref m_freeFlyRef, m_freeFlySmooth, 9999f, dt);
		base.transform.rotation = rotation;
	}

	private void UpdateCameraShake(float dt)
	{
		m_shakeIntensity -= dt;
		if (m_shakeIntensity <= 0f)
		{
			m_shakeIntensity = 0f;
			return;
		}
		float num = m_shakeIntensity * m_shakeIntensity * m_shakeIntensity;
		m_shakeTimer += dt * Mathf.Clamp01(m_shakeIntensity) * m_shakeFreq;
		Quaternion rhs = Quaternion.Euler(Mathf.Sin(m_shakeTimer) * num * m_shakeMovement, Mathf.Cos(m_shakeTimer * 0.9f) * num * m_shakeMovement, 0f);
		base.transform.rotation = base.transform.rotation * rhs;
	}

	public void AddShake(Vector3 point, float range, float strength, bool continous)
	{
		if (!m_cameraShakeEnabled)
		{
			return;
		}
		float num = Vector3.Distance(point, base.transform.position);
		if (num > range)
		{
			return;
		}
		num = Mathf.Max(1f, num);
		float num2 = 1f - num / range;
		float num3 = strength * num2;
		if (!(num3 < m_shakeIntensity))
		{
			m_shakeIntensity = num3;
			if (continous)
			{
				m_shakeTimer = Time.time * Mathf.Clamp01(strength) * m_shakeFreq;
			}
			else
			{
				m_shakeTimer = Time.time * Mathf.Clamp01(m_shakeIntensity) * m_shakeFreq;
			}
		}
	}

	private float RayTest(Vector3 point, Vector3 dir, float maxDist)
	{
		RaycastHit val = default(RaycastHit);
		if (Physics.SphereCast(point, 0.2f, dir, ref val, maxDist, (int)m_blockCameraMask))
		{
			return ((RaycastHit)(ref val)).get_distance();
		}
		return maxDist;
	}

	private Vector3 GetCameraBaseOffset(Player player)
	{
		if (player.InBed())
		{
			return player.GetHeadPoint() - player.transform.position;
		}
		if (player.IsAttached() || player.IsSitting())
		{
			return player.GetHeadPoint() + Vector3.up * 0.3f - player.transform.position;
		}
		return player.m_eye.transform.position - player.transform.position;
	}

	private void UpdateBaseOffset(Player player, float dt)
	{
		Vector3 cameraBaseOffset = GetCameraBaseOffset(player);
		m_currentBaseOffset = Vector3.SmoothDamp(m_currentBaseOffset, cameraBaseOffset, ref m_offsetBaseVel, 0.5f, 999f, dt);
		if (Vector3.Distance(m_playerPos, player.transform.position) > 20f)
		{
			m_playerPos = player.transform.position;
		}
		m_playerPos = Vector3.SmoothDamp(m_playerPos, player.transform.position, ref m_playerVel, m_smoothness, 999f, dt);
	}

	private Vector3 GetOffsetedEyePos()
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			if (localPlayer.GetStandingOnShip() != null || localPlayer.IsAttached())
			{
				return localPlayer.transform.position + m_currentBaseOffset + GetCameraOffset(localPlayer);
			}
			return m_playerPos + m_currentBaseOffset + GetCameraOffset(localPlayer);
		}
		return base.transform.position;
	}

	private Vector3 GetCameraOffset(Player player)
	{
		if (m_distance <= 0f)
		{
			return player.m_eye.transform.TransformVector(m_fpsOffset);
		}
		if (player.InBed())
		{
			return Vector3.zero;
		}
		Vector3 vector = (player.UseMeleeCamera() ? m_3rdCombatOffset : m_3rdOffset);
		return player.m_eye.transform.TransformVector(vector);
	}

	public void ToggleFreeFly()
	{
		m_freeFly = !m_freeFly;
	}

	public void SetFreeFlySmoothness(float smooth)
	{
		m_freeFlySmooth = Mathf.Clamp(smooth, 0f, 1f);
	}

	public float GetFreeFlySmoothness()
	{
		return m_freeFlySmooth;
	}

	public static bool InFreeFly()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_freeFly;
		}
		return false;
	}
}
