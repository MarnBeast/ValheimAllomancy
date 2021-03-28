using UnityEngine;
using UnityEngine.UI;

public class ChangeLog : MonoBehaviour
{
	public Text m_textField;

	public TextAsset m_changeLog;

	private void Start()
	{
		string text = m_changeLog.text;
		m_textField.set_text(text);
	}
}
