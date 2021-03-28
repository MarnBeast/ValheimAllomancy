using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageHud : MonoBehaviour
{
	public enum MessageType
	{
		TopLeft = 1,
		Center
	}

	private class UnlockMsg
	{
		public Sprite m_icon;

		public string m_topic;

		public string m_description;
	}

	private class MsgData
	{
		public Sprite m_icon;

		public string m_text;

		public int m_amount;
	}

	private class BiomeMessage
	{
		public string m_text;

		public bool m_playStinger;
	}

	private MsgData currentMsg = new MsgData();

	private static MessageHud m_instance;

	public Text m_messageText;

	public Image m_messageIcon;

	public Text m_messageCenterText;

	public GameObject m_unlockMsgPrefab;

	public int m_maxUnlockMsgSpace = 110;

	public int m_maxUnlockMessages = 4;

	public int m_maxLogMessages = 50;

	public GameObject m_biomeFoundPrefab;

	public GameObject m_biomeFoundStinger;

	private Queue<BiomeMessage> m_biomeFoundQueue = new Queue<BiomeMessage>();

	private List<string> m_messageLog = new List<string>();

	private List<GameObject> m_unlockMessages = new List<GameObject>();

	private Queue<UnlockMsg> m_unlockMsgQueue = new Queue<UnlockMsg>();

	private Queue<MsgData> m_msgQeue = new Queue<MsgData>();

	private float m_msgQueueTimer = -1f;

	private GameObject m_biomeMsgInstance;

	public static MessageHud instance => m_instance;

	private void Awake()
	{
		m_instance = this;
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Start()
	{
		((Graphic)m_messageText).get_canvasRenderer().SetAlpha(0f);
		((Graphic)m_messageIcon).get_canvasRenderer().SetAlpha(0f);
		((Graphic)m_messageCenterText).get_canvasRenderer().SetAlpha(0f);
		for (int i = 0; i < m_maxUnlockMessages; i++)
		{
			m_unlockMessages.Add(null);
		}
		ZRoutedRpc.instance.Register<int, string>("ShowMessage", RPC_ShowMessage);
	}

	private void Update()
	{
		if (Hud.IsUserHidden())
		{
			HideAll();
			return;
		}
		UpdateUnlockMsg(Time.deltaTime);
		UpdateMessage(Time.deltaTime);
		UpdateBiomeFound(Time.deltaTime);
	}

	private void HideAll()
	{
		for (int i = 0; i < m_maxUnlockMessages; i++)
		{
			if (m_unlockMessages[i] != null)
			{
				Object.Destroy(m_unlockMessages[i]);
				m_unlockMessages[i] = null;
			}
		}
		((Graphic)m_messageText).get_canvasRenderer().SetAlpha(0f);
		((Graphic)m_messageIcon).get_canvasRenderer().SetAlpha(0f);
		((Graphic)m_messageCenterText).get_canvasRenderer().SetAlpha(0f);
		if ((bool)m_biomeMsgInstance)
		{
			Object.Destroy(m_biomeMsgInstance);
			m_biomeMsgInstance = null;
		}
	}

	public void MessageAll(MessageType type, string text)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", (int)type, text);
	}

	private void RPC_ShowMessage(long sender, int type, string text)
	{
		ShowMessage((MessageType)type, text);
	}

	public void ShowMessage(MessageType type, string text, int amount = 0, Sprite icon = null)
	{
		if (!Hud.IsUserHidden())
		{
			text = Localization.get_instance().Localize(text);
			switch (type)
			{
			case MessageType.TopLeft:
			{
				MsgData msgData = new MsgData();
				msgData.m_icon = icon;
				msgData.m_text = text;
				msgData.m_amount = amount;
				m_msgQeue.Enqueue(msgData);
				AddLog(text);
				break;
			}
			case MessageType.Center:
				m_messageCenterText.set_text(text);
				((Graphic)m_messageCenterText).get_canvasRenderer().SetAlpha(1f);
				((Graphic)m_messageCenterText).CrossFadeAlpha(0f, 4f, true);
				break;
			}
		}
	}

	private void UpdateMessage(float dt)
	{
		m_msgQueueTimer += dt;
		if (m_msgQeue.Count <= 0)
		{
			return;
		}
		MsgData msgData = m_msgQeue.Peek();
		bool flag = m_msgQueueTimer < 4f && msgData.m_text == currentMsg.m_text && msgData.m_icon == currentMsg.m_icon;
		if (m_msgQueueTimer >= 1f || flag)
		{
			MsgData msgData2 = m_msgQeue.Dequeue();
			m_messageText.set_text(msgData2.m_text);
			if (flag)
			{
				msgData2.m_amount += currentMsg.m_amount;
			}
			if (msgData2.m_amount > 1)
			{
				Text messageText = m_messageText;
				messageText.set_text(messageText.get_text() + " x" + msgData2.m_amount);
			}
			((Graphic)m_messageText).get_canvasRenderer().SetAlpha(1f);
			((Graphic)m_messageText).CrossFadeAlpha(0f, 4f, true);
			if (msgData2.m_icon != null)
			{
				m_messageIcon.set_sprite(msgData2.m_icon);
				((Graphic)m_messageIcon).get_canvasRenderer().SetAlpha(1f);
				((Graphic)m_messageIcon).CrossFadeAlpha(0f, 4f, true);
			}
			else
			{
				((Graphic)m_messageIcon).get_canvasRenderer().SetAlpha(0f);
			}
			currentMsg = msgData2;
			m_msgQueueTimer = 0f;
		}
	}

	private void UpdateBiomeFound(float dt)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (m_biomeMsgInstance != null)
		{
			AnimatorStateInfo currentAnimatorStateInfo = m_biomeMsgInstance.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0);
			if (((AnimatorStateInfo)(ref currentAnimatorStateInfo)).IsTag("done"))
			{
				Object.Destroy(m_biomeMsgInstance);
				m_biomeMsgInstance = null;
			}
		}
		if (m_biomeFoundQueue.Count > 0 && m_biomeMsgInstance == null && m_msgQeue.Count == 0 && m_msgQueueTimer > 2f)
		{
			BiomeMessage biomeMessage = m_biomeFoundQueue.Dequeue();
			m_biomeMsgInstance = Object.Instantiate(m_biomeFoundPrefab, base.transform);
			Text component = Utils.FindChild(m_biomeMsgInstance.transform, "Title").GetComponent<Text>();
			string text = Localization.get_instance().Localize(biomeMessage.m_text);
			component.set_text(text);
			if (biomeMessage.m_playStinger && (bool)m_biomeFoundStinger)
			{
				Object.Instantiate(m_biomeFoundStinger);
			}
		}
	}

	public void ShowBiomeFoundMsg(string text, bool playStinger)
	{
		BiomeMessage biomeMessage = new BiomeMessage();
		biomeMessage.m_text = text;
		biomeMessage.m_playStinger = playStinger;
		m_biomeFoundQueue.Enqueue(biomeMessage);
	}

	public void QueueUnlockMsg(Sprite icon, string topic, string description)
	{
		UnlockMsg unlockMsg = new UnlockMsg();
		unlockMsg.m_icon = icon;
		unlockMsg.m_topic = Localization.get_instance().Localize(topic);
		unlockMsg.m_description = Localization.get_instance().Localize(description);
		m_unlockMsgQueue.Enqueue(unlockMsg);
		AddLog(topic + ":" + description);
		ZLog.Log((object)("Queue unlock msg:" + topic + ":" + description));
	}

	private int GetFreeUnlockMsgSlot()
	{
		for (int i = 0; i < m_unlockMessages.Count; i++)
		{
			if (m_unlockMessages[i] == null)
			{
				return i;
			}
		}
		return -1;
	}

	private void UpdateUnlockMsg(float dt)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < m_unlockMessages.Count; i++)
		{
			GameObject gameObject = m_unlockMessages[i];
			if (!(gameObject == null))
			{
				AnimatorStateInfo currentAnimatorStateInfo = gameObject.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0);
				if (((AnimatorStateInfo)(ref currentAnimatorStateInfo)).IsTag("done"))
				{
					Object.Destroy(gameObject);
					m_unlockMessages[i] = null;
					break;
				}
			}
		}
		if (m_unlockMsgQueue.Count > 0)
		{
			int freeUnlockMsgSlot = GetFreeUnlockMsgSlot();
			if (freeUnlockMsgSlot != -1)
			{
				Transform transform = base.transform;
				GameObject gameObject2 = Object.Instantiate(m_unlockMsgPrefab, transform);
				m_unlockMessages[freeUnlockMsgSlot] = gameObject2;
				RectTransform obj = gameObject2.transform as RectTransform;
				Vector3 v = obj.anchoredPosition;
				v.y -= m_maxUnlockMsgSpace * freeUnlockMsgSlot;
				obj.anchoredPosition = v;
				UnlockMsg unlockMsg = m_unlockMsgQueue.Dequeue();
				Image component = obj.Find("UnlockMessage/icon_bkg/UnlockIcon").GetComponent<Image>();
				Text component2 = obj.Find("UnlockMessage/UnlockTitle").GetComponent<Text>();
				Text component3 = obj.Find("UnlockMessage/UnlockDescription").GetComponent<Text>();
				component.set_sprite(unlockMsg.m_icon);
				component2.set_text(unlockMsg.m_topic);
				component3.set_text(unlockMsg.m_description);
			}
		}
	}

	private void AddLog(string logText)
	{
		m_messageLog.Add(logText);
		while (m_messageLog.Count > m_maxLogMessages)
		{
			m_messageLog.RemoveAt(0);
		}
	}

	public List<string> GetLog()
	{
		return m_messageLog;
	}
}
