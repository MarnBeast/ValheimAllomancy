using UnityEngine;
using UnityEngine.UI;

public class ToggleImage : MonoBehaviour
{
	private Toggle m_toggle;

	public Image m_targetImage;

	public Sprite m_onImage;

	public Sprite m_offImage;

	private void Awake()
	{
		m_toggle = GetComponent<Toggle>();
	}

	private void Update()
	{
		if (m_toggle.get_isOn())
		{
			m_targetImage.set_sprite(m_onImage);
		}
		else
		{
			m_targetImage.set_sprite(m_offImage);
		}
	}
}
