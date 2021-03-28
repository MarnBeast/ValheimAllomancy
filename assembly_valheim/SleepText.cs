using UnityEngine;
using UnityEngine.UI;

public class SleepText : MonoBehaviour
{
	public Text m_textField;

	public Text m_dreamField;

	public DreamTexts m_dreamTexts;

	private void OnEnable()
	{
		((Graphic)m_textField).get_canvasRenderer().SetAlpha(0f);
		((Graphic)m_textField).CrossFadeAlpha(1f, 1f, true);
		((Behaviour)(object)m_dreamField).enabled = false;
		Invoke("HideZZZ", 2f);
		Invoke("ShowDreamText", 4f);
	}

	private void HideZZZ()
	{
		((Graphic)m_textField).CrossFadeAlpha(0f, 2f, true);
	}

	private void ShowDreamText()
	{
		DreamTexts.DreamText randomDreamText = m_dreamTexts.GetRandomDreamText();
		if (randomDreamText != null)
		{
			((Behaviour)(object)m_dreamField).enabled = true;
			((Graphic)m_dreamField).get_canvasRenderer().SetAlpha(0f);
			((Graphic)m_dreamField).CrossFadeAlpha(1f, 1.5f, true);
			m_dreamField.set_text(Localization.get_instance().Localize(randomDreamText.m_text));
			Invoke("HideDreamText", 6.5f);
		}
	}

	private void HideDreamText()
	{
		((Graphic)m_dreamField).CrossFadeAlpha(0f, 1.5f, true);
	}
}
