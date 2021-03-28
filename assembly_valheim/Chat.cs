using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
	public class WorldTextInstance
	{
		public long m_talkerID;

		public GameObject m_go;

		public Vector3 m_position;

		public float m_timer;

		public GameObject m_gui;

		public Text m_textField;

		public string m_name = "";

		public Talker.Type m_type;

		public string m_text = "";
	}

	public class NpcText
	{
		public GameObject m_go;

		public Vector3 m_offset = Vector3.zero;

		public float m_cullDistance = 20f;

		public GameObject m_gui;

		public Animator m_animator;

		public Text m_textField;

		public Text m_topicField;

		public float m_ttl;

		public bool m_timeout;

		public void SetVisible(bool visible)
		{
			m_animator.SetBool("visible", visible);
		}

		public bool IsVisible()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			AnimatorStateInfo currentAnimatorStateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
			if (((AnimatorStateInfo)(ref currentAnimatorStateInfo)).IsTag("visible"))
			{
				return true;
			}
			return m_animator.GetBool("visible");
		}
	}

	private static Chat m_instance;

	public RectTransform m_chatWindow;

	public Text m_output;

	public InputField m_input;

	public float m_hideDelay = 10f;

	public float m_worldTextTTL = 5f;

	public GameObject m_worldTextBase;

	public GameObject m_npcTextBase;

	public GameObject m_npcTextBaseLarge;

	private List<WorldTextInstance> m_worldTexts = new List<WorldTextInstance>();

	private List<NpcText> m_npcTexts = new List<NpcText>();

	private float m_hideTimer = 9999f;

	private bool m_wasFocused;

	private const int m_maxBufferLength = 15;

	private List<string> m_chatBuffer = new List<string>();

	public static Chat instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		ZRoutedRpc.instance.Register<Vector3, int, string, string>("ChatMessage", RPC_ChatMessage);
		AddString(Localization.get_instance().Localize("/w [text] - $chat_whisper"));
		AddString(Localization.get_instance().Localize("/s [text] - $chat_shout"));
		AddString(Localization.get_instance().Localize("/killme - $chat_kill"));
		AddString(Localization.get_instance().Localize("/resetspawn - $chat_resetspawn"));
		AddString(Localization.get_instance().Localize("/[emote]"));
		AddString(Localization.get_instance().Localize("Emotes: sit,wave,challenge,cheer,nonono,thumbsup,point"));
		AddString("");
		((Component)(object)m_input).gameObject.SetActive(value: false);
		m_worldTextBase.SetActive(value: false);
	}

	public bool HasFocus()
	{
		if (m_chatWindow.gameObject.activeInHierarchy)
		{
			return m_input.get_isFocused();
		}
		return false;
	}

	public bool IsChatDialogWindowVisible()
	{
		return m_chatWindow.gameObject.activeSelf;
	}

	private void Update()
	{
		m_hideTimer += Time.deltaTime;
		m_chatWindow.gameObject.SetActive(m_hideTimer < m_hideDelay);
		if (!m_wasFocused)
		{
			if (Input.GetKeyDown(KeyCode.Return) && Player.m_localPlayer != null && !Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && !Menu.IsVisible())
			{
				m_hideTimer = 0f;
				m_chatWindow.gameObject.SetActive(value: true);
				((Component)(object)m_input).gameObject.SetActive(value: true);
				m_input.ActivateInputField();
			}
		}
		else if (m_wasFocused)
		{
			m_hideTimer = 0f;
			if (Input.GetKeyDown(KeyCode.Return))
			{
				if (!string.IsNullOrEmpty(m_input.get_text()))
				{
					InputText();
					m_input.set_text("");
				}
				EventSystem.get_current().SetSelectedGameObject((GameObject)null);
				((Component)(object)m_input).gameObject.SetActive(value: false);
			}
		}
		m_wasFocused = m_input.get_isFocused();
	}

	private void LateUpdate()
	{
		UpdateWorldTexts(Time.deltaTime);
		UpdateNpcTexts(Time.deltaTime);
	}

	private void UpdateChat()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item in m_chatBuffer)
		{
			stringBuilder.Append(item);
			stringBuilder.Append("\n");
		}
		m_output.set_text(stringBuilder.ToString());
	}

	public void OnNewChatMessage(GameObject go, long senderID, Vector3 pos, Talker.Type type, string user, string text)
	{
		text = text.Replace('<', ' ');
		text = text.Replace('>', ' ');
		AddString(user, text, type);
		AddInworldText(go, senderID, pos, type, user, text);
	}

	private void UpdateWorldTexts(float dt)
	{
		WorldTextInstance worldTextInstance = null;
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			worldText.m_timer += dt;
			if (worldText.m_timer > m_worldTextTTL && worldTextInstance == null)
			{
				worldTextInstance = worldText;
			}
			worldText.m_position.y += dt * 0.15f;
			Vector3 zero = Vector3.zero;
			if ((bool)worldText.m_go)
			{
				Character component = worldText.m_go.GetComponent<Character>();
				zero = ((!component) ? (worldText.m_go.transform.position + Vector3.up * 0.3f) : (component.GetHeadPoint() + Vector3.up * 0.3f));
			}
			else
			{
				zero = worldText.m_position + Vector3.up * 0.3f;
			}
			Vector3 position = mainCamera.WorldToScreenPoint(zero);
			if (position.x < 0f || position.x > (float)Screen.width || position.y < 0f || position.y > (float)Screen.height || position.z < 0f)
			{
				Vector3 vector = zero - mainCamera.transform.position;
				bool flag = Vector3.Dot(mainCamera.transform.right, vector) < 0f;
				Vector3 vector2 = vector;
				vector2.y = 0f;
				float magnitude = vector2.magnitude;
				float y = vector.y;
				Vector3 forward = mainCamera.transform.forward;
				forward.y = 0f;
				forward.Normalize();
				forward *= magnitude;
				Vector3 b = forward + Vector3.up * y;
				position = mainCamera.WorldToScreenPoint(mainCamera.transform.position + b);
				position.x = ((!flag) ? Screen.width : 0);
			}
			RectTransform rectTransform = worldText.m_gui.transform as RectTransform;
			position.x = Mathf.Clamp(position.x, rectTransform.rect.width / 2f, (float)Screen.width - rectTransform.rect.width / 2f);
			position.y = Mathf.Clamp(position.y, rectTransform.rect.height / 2f, (float)Screen.height - rectTransform.rect.height);
			position.z = Mathf.Min(position.z, 100f);
			worldText.m_gui.transform.position = position;
		}
		if (worldTextInstance != null)
		{
			Object.Destroy(worldTextInstance.m_gui);
			m_worldTexts.Remove(worldTextInstance);
		}
	}

	private void AddInworldText(GameObject go, long senderID, Vector3 position, Talker.Type type, string user, string text)
	{
		WorldTextInstance worldTextInstance = FindExistingWorldText(senderID);
		if (worldTextInstance == null)
		{
			worldTextInstance = new WorldTextInstance();
			worldTextInstance.m_talkerID = senderID;
			worldTextInstance.m_gui = Object.Instantiate(m_worldTextBase, base.transform);
			worldTextInstance.m_gui.gameObject.SetActive(value: true);
			worldTextInstance.m_textField = worldTextInstance.m_gui.transform.Find("Text").GetComponent<Text>();
			m_worldTexts.Add(worldTextInstance);
		}
		worldTextInstance.m_name = user;
		worldTextInstance.m_type = type;
		worldTextInstance.m_go = go;
		worldTextInstance.m_position = position;
		Color color;
		switch (type)
		{
		case Talker.Type.Shout:
			color = Color.yellow;
			text = text.ToUpper();
			break;
		case Talker.Type.Whisper:
			color = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
			break;
		case Talker.Type.Ping:
			color = new Color(0.6f, 0.7f, 1f, 1f);
			text = "PING";
			break;
		default:
			color = Color.white;
			break;
		}
		((Graphic)worldTextInstance.m_textField).set_color(color);
		((Behaviour)(object)((Component)(object)worldTextInstance.m_textField).GetComponent<Outline>()).enabled = type != Talker.Type.Whisper;
		worldTextInstance.m_timer = 0f;
		worldTextInstance.m_text = text;
		UpdateWorldTextField(worldTextInstance);
	}

	private void UpdateWorldTextField(WorldTextInstance wt)
	{
		string str = "";
		if (wt.m_type == Talker.Type.Shout || wt.m_type == Talker.Type.Ping)
		{
			str = wt.m_name + ": ";
		}
		str += wt.m_text;
		wt.m_textField.set_text(str);
	}

	private WorldTextInstance FindExistingWorldText(long senderID)
	{
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			if (worldText.m_talkerID == senderID)
			{
				return worldText;
			}
		}
		return null;
	}

	private void AddString(string user, string text, Talker.Type type)
	{
		Color white = Color.white;
		switch (type)
		{
		case Talker.Type.Shout:
			white = Color.yellow;
			text = text.ToUpper();
			break;
		case Talker.Type.Whisper:
			white = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
			break;
		default:
			white = Color.white;
			break;
		}
		string text2 = "<color=orange>" + user + "</color>: <color=#" + ColorUtility.ToHtmlStringRGBA(white) + ">" + text + "</color>";
		AddString(text2);
	}

	private void AddString(string text)
	{
		m_chatBuffer.Add(text);
		while (m_chatBuffer.Count > 15)
		{
			m_chatBuffer.RemoveAt(0);
		}
		UpdateChat();
	}

	private void InputText()
	{
		string text = m_input.get_text();
		if (text == "/resetspawn")
		{
			Game.instance.GetPlayerProfile()?.ClearCustomSpawnPoint();
			AddString("Reseting spawn point");
			return;
		}
		if (text == "/killme")
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 99999f;
			Player.m_localPlayer.Damage(hitData);
			return;
		}
		Talker.Type type = Talker.Type.Normal;
		if (text.StartsWith("/s ") || text.StartsWith("/S "))
		{
			type = Talker.Type.Shout;
			text = text.Substring(3);
		}
		if (text.StartsWith("/w ") || text.StartsWith("/W "))
		{
			type = Talker.Type.Whisper;
			text = text.Substring(3);
		}
		if (text.StartsWith("/wave"))
		{
			Player.m_localPlayer.StartEmote("wave");
		}
		else if (text.StartsWith("/sit"))
		{
			Player.m_localPlayer.StartEmote("sit", oneshot: false);
		}
		else if (text.StartsWith("/challenge"))
		{
			Player.m_localPlayer.StartEmote("challenge");
		}
		else if (text.StartsWith("/cheer"))
		{
			Player.m_localPlayer.StartEmote("cheer");
		}
		else if (text.StartsWith("/nonono"))
		{
			Player.m_localPlayer.StartEmote("nonono");
		}
		else if (text.StartsWith("/thumbsup"))
		{
			Player.m_localPlayer.StartEmote("thumbsup");
		}
		else if (text.StartsWith("/point"))
		{
			Player.m_localPlayer.FaceLookDirection();
			Player.m_localPlayer.StartEmote("point");
		}
		else
		{
			SendText(type, text);
		}
	}

	private void RPC_ChatMessage(long sender, Vector3 position, int type, string name, string text)
	{
		OnNewChatMessage(null, sender, position, (Talker.Type)type, name, text);
	}

	public void SendText(Talker.Type type, string text)
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			if (type == Talker.Type.Shout)
			{
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", localPlayer.GetHeadPoint(), 2, localPlayer.GetPlayerName(), text);
			}
			else
			{
				localPlayer.GetComponent<Talker>().Say(type, text);
			}
		}
	}

	public void SendPing(Vector3 position)
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			Vector3 vector = position;
			vector.y = localPlayer.transform.position.y;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", vector, 3, localPlayer.GetPlayerName(), "");
		}
	}

	public void GetShoutWorldTexts(List<WorldTextInstance> texts)
	{
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			if (worldText.m_type == Talker.Type.Shout)
			{
				texts.Add(worldText);
			}
		}
	}

	public void GetPingWorldTexts(List<WorldTextInstance> texts)
	{
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			if (worldText.m_type == Talker.Type.Ping)
			{
				texts.Add(worldText);
			}
		}
	}

	private void UpdateNpcTexts(float dt)
	{
		NpcText npcText = null;
		Camera mainCamera = Utils.GetMainCamera();
		foreach (NpcText npcText2 in m_npcTexts)
		{
			if (!npcText2.m_go)
			{
				npcText2.m_gui.SetActive(value: false);
				if (npcText == null)
				{
					npcText = npcText2;
				}
				continue;
			}
			if (npcText2.m_timeout)
			{
				npcText2.m_ttl -= dt;
				if (npcText2.m_ttl <= 0f)
				{
					npcText2.SetVisible(visible: false);
					if (!npcText2.IsVisible())
					{
						npcText = npcText2;
					}
					continue;
				}
			}
			Vector3 vector = npcText2.m_go.transform.position + npcText2.m_offset;
			Vector3 position = mainCamera.WorldToScreenPoint(vector);
			if (position.x < 0f || position.x > (float)Screen.width || position.y < 0f || position.y > (float)Screen.height || position.z < 0f)
			{
				npcText2.SetVisible(visible: false);
			}
			else
			{
				npcText2.SetVisible(visible: true);
				RectTransform rectTransform = npcText2.m_gui.transform as RectTransform;
				position.x = Mathf.Clamp(position.x, rectTransform.rect.width / 2f, (float)Screen.width - rectTransform.rect.width / 2f);
				position.y = Mathf.Clamp(position.y, rectTransform.rect.height / 2f, (float)Screen.height - rectTransform.rect.height);
				npcText2.m_gui.transform.position = position;
			}
			if (Vector3.Distance(mainCamera.transform.position, vector) > npcText2.m_cullDistance)
			{
				npcText2.SetVisible(visible: false);
				if (npcText == null && !npcText2.IsVisible())
				{
					npcText = npcText2;
				}
			}
		}
		if (npcText != null)
		{
			ClearNpcText(npcText);
		}
	}

	public void SetNpcText(GameObject talker, Vector3 offset, float cullDistance, float ttl, string topic, string text, bool large)
	{
		NpcText npcText = FindNpcText(talker);
		if (npcText != null)
		{
			ClearNpcText(npcText);
		}
		npcText = new NpcText();
		npcText.m_go = talker;
		npcText.m_gui = Object.Instantiate(large ? m_npcTextBaseLarge : m_npcTextBase, base.transform);
		npcText.m_gui.SetActive(value: true);
		npcText.m_animator = npcText.m_gui.GetComponent<Animator>();
		npcText.m_topicField = npcText.m_gui.transform.Find("Topic").GetComponent<Text>();
		npcText.m_textField = npcText.m_gui.transform.Find("Text").GetComponent<Text>();
		npcText.m_ttl = ttl;
		npcText.m_timeout = ttl > 0f;
		npcText.m_offset = offset;
		npcText.m_cullDistance = cullDistance;
		if (topic.Length > 0)
		{
			npcText.m_textField.set_text("<color=orange>" + Localization.get_instance().Localize(topic) + "</color>\n" + Localization.get_instance().Localize(text));
		}
		else
		{
			npcText.m_textField.set_text(Localization.get_instance().Localize(text));
		}
		m_npcTexts.Add(npcText);
	}

	public bool IsDialogVisible(GameObject talker)
	{
		return FindNpcText(talker)?.IsVisible() ?? false;
	}

	public void ClearNpcText(GameObject talker)
	{
		NpcText npcText = FindNpcText(talker);
		if (npcText != null)
		{
			ClearNpcText(npcText);
		}
	}

	private void ClearNpcText(NpcText npcText)
	{
		Object.Destroy(npcText.m_gui);
		m_npcTexts.Remove(npcText);
	}

	private NpcText FindNpcText(GameObject go)
	{
		foreach (NpcText npcText in m_npcTexts)
		{
			if (npcText.m_go == go)
			{
				return npcText;
			}
		}
		return null;
	}
}
