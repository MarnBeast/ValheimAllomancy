using System;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldSubmit : MonoBehaviour
{
	public Action<string> m_onSubmit;

	private InputField m_field;

	private void Awake()
	{
		m_field = GetComponent<InputField>();
	}

	private void Update()
	{
		if (m_field.get_text() != "" && Input.GetKey(KeyCode.Return))
		{
			m_onSubmit(m_field.get_text());
			m_field.set_text("");
		}
	}
}
