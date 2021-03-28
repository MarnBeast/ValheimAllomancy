using UnityEngine;
using UnityEngine.UI;

public class Feedback : MonoBehaviour
{
	private static Feedback m_instance;

	public Text m_subject;

	public Text m_text;

	public Button m_sendButton;

	public Toggle m_catBug;

	public Toggle m_catFeedback;

	public Toggle m_catIdea;

	private void Awake()
	{
		m_instance = this;
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public static bool IsVisible()
	{
		return m_instance != null;
	}

	private void LateUpdate()
	{
		((Selectable)m_sendButton).set_interactable(IsValid());
		if (IsVisible() && (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMenu")))
		{
			OnBack();
		}
	}

	private bool IsValid()
	{
		if (m_subject.get_text().Length == 0)
		{
			return false;
		}
		if (m_text.get_text().Length == 0)
		{
			return false;
		}
		return true;
	}

	public void OnBack()
	{
		Object.Destroy(base.gameObject);
	}

	public void OnSend()
	{
		if (IsValid())
		{
			string category = GetCategory();
			Gogan.LogEvent("Feedback_" + category, m_subject.get_text(), m_text.get_text(), 0L);
			Object.Destroy(base.gameObject);
		}
	}

	private string GetCategory()
	{
		if (m_catBug.get_isOn())
		{
			return "Bug";
		}
		if (m_catFeedback.get_isOn())
		{
			return "Feedback";
		}
		if (m_catIdea.get_isOn())
		{
			return "Idea";
		}
		return "";
	}
}
