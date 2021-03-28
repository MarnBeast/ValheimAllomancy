using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Console : MonoBehaviour
{
	private static Console m_instance;

	public RectTransform m_chatWindow;

	public Text m_output;

	public InputField m_input;

	private const int m_maxBufferLength = 30;

	private List<string> m_chatBuffer = new List<string>();

	private bool m_cheat;

	private string m_lastEntry = "";

	public static Console instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		AddString("Valheim " + Version.GetVersionString());
		AddString("");
		AddString("type \"help\" - for commands");
		AddString("");
		m_chatWindow.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if ((bool)ZNet.instance && ZNet.instance.InPasswordDialog())
		{
			m_chatWindow.gameObject.SetActive(value: false);
			return;
		}
		if (Input.GetKeyDown(KeyCode.F5) || (IsVisible() && Input.GetKeyDown(KeyCode.Escape)))
		{
			m_chatWindow.gameObject.SetActive(!m_chatWindow.gameObject.activeSelf);
		}
		if (!m_chatWindow.gameObject.activeInHierarchy)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			m_input.set_text(m_lastEntry);
			m_input.set_caretPosition(m_input.get_text().Length);
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			m_input.set_text("");
		}
		((Component)(object)m_input).gameObject.SetActive(value: true);
		m_input.ActivateInputField();
		if (Input.GetKeyDown(KeyCode.Return))
		{
			if (!string.IsNullOrEmpty(m_input.get_text()))
			{
				InputText();
				m_lastEntry = m_input.get_text();
				m_input.set_text("");
			}
			EventSystem.get_current().SetSelectedGameObject((GameObject)null);
			((Component)(object)m_input).gameObject.SetActive(value: false);
		}
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_chatWindow.gameObject.activeInHierarchy;
		}
		return false;
	}

	public void Print(string text)
	{
		AddString(text);
	}

	private void AddString(string text)
	{
		m_chatBuffer.Add(text);
		while (m_chatBuffer.Count > 30)
		{
			m_chatBuffer.RemoveAt(0);
		}
		UpdateChat();
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

	private void InputText()
	{
		string text = m_input.get_text();
		AddString(text);
		string[] array = text.Split(' ');
		if (text.StartsWith("help"))
		{
			AddString("kick [name/ip/userID] - kick user");
			AddString("ban [name/ip/userID] - ban user");
			AddString("unban [ip/userID] - unban user");
			AddString("banned - list banned users");
			AddString("ping - ping server");
			AddString("lodbias - set distance lod bias");
			AddString("info - print system info");
			if (IsCheatsEnabled())
			{
				AddString("genloc - regenerate all locations.");
				AddString("debugmode - fly mode");
				AddString("spawn [amount] [level] - spawn something");
				AddString("pos - print current player position");
				AddString("goto [x,z]- teleport");
				AddString("exploremap - explore entire map");
				AddString("resetmap - reset map exploration");
				AddString("killall - kill nearby enemies");
				AddString("tame - tame all nearby tameable creatures");
				AddString("hair");
				AddString("beard");
				AddString("location - spawn location");
				AddString("raiseskill [skill] [amount]");
				AddString("resetskill [skill]");
				AddString("freefly - freefly photo mode");
				AddString("ffsmooth - freefly smoothness");
				AddString("tod -1 OR [0-1]");
				AddString("env [env]");
				AddString("resetenv");
				AddString("wind [angle] [intensity]");
				AddString("resetwind");
				AddString("god");
				AddString("event [name] - start event");
				AddString("stopevent - stop current event");
				AddString("randomevent");
				AddString("save - force saving of world");
				AddString("resetcharacter - reset character data");
				AddString("removedrops - remove all item-drops in area");
				AddString("setkey [name]");
				AddString("resetkeys [name]");
				AddString("listkeys");
				AddString("players [nr] - force diffuculty scale ( 0 = reset)");
				AddString("dpsdebug - toggle dps debug print");
			}
		}
		if (text.StartsWith("imacheater"))
		{
			m_cheat = !m_cheat;
			AddString("Cheats: " + m_cheat);
			Gogan.LogEvent("Cheat", "CheatsEnabled", m_cheat.ToString(), 0L);
			return;
		}
		if (array[0] == "hidebetatext" && (bool)Hud.instance)
		{
			Hud.instance.ToggleBetaTextVisible();
		}
		if (array[0] == "ping")
		{
			if ((bool)Game.instance)
			{
				Game.instance.Ping();
			}
			return;
		}
		if (array[0] == "dpsdebug")
		{
			Character.SetDPSDebug(!Character.IsDPSDebugEnabled());
			AddString("DPS debug " + Character.IsDPSDebugEnabled());
		}
		if (array[0] == "lodbias")
		{
			float result;
			if (array.Length == 1)
			{
				Print("Lod bias:" + QualitySettings.lodBias);
			}
			else if (float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				Print("Setting lod bias:" + result);
				QualitySettings.lodBias = result;
			}
			return;
		}
		if (array[0] == "info")
		{
			Print("Render threading mode:" + SystemInfo.renderingThreadingMode);
			long totalMemory = GC.GetTotalMemory(forceFullCollection: false);
			Print("Total allocated mem: " + (totalMemory / 1048576).ToString("0") + "mb");
		}
		if (array[0] == "gc")
		{
			long totalMemory2 = GC.GetTotalMemory(forceFullCollection: false);
			GC.Collect();
			long totalMemory3 = GC.GetTotalMemory(forceFullCollection: true);
			long num = totalMemory3 - totalMemory2;
			Print("GC collect, Delta: " + (num / 1048576).ToString("0") + "mb   Total left:" + (totalMemory3 / 1048576).ToString("0") + "mb");
		}
		if (array[0] == "fov")
		{
			Camera mainCamera = Utils.GetMainCamera();
			if ((bool)mainCamera)
			{
				float result2;
				if (array.Length == 1)
				{
					Print("Fov:" + mainCamera.fieldOfView);
				}
				else if (float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out result2) && result2 > 5f)
				{
					Print("Setting fov to " + result2);
					Camera[] componentsInChildren = mainCamera.GetComponentsInChildren<Camera>();
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						componentsInChildren[i].fieldOfView = result2;
					}
				}
			}
		}
		if ((bool)ZNet.instance)
		{
			if (text.StartsWith("kick "))
			{
				string user = text.Substring(5);
				ZNet.instance.Kick(user);
				return;
			}
			if (text.StartsWith("ban "))
			{
				string user2 = text.Substring(4);
				ZNet.instance.Ban(user2);
				return;
			}
			if (text.StartsWith("unban "))
			{
				string user3 = text.Substring(6);
				ZNet.instance.Unban(user3);
				return;
			}
			if (text.StartsWith("banned"))
			{
				ZNet.instance.PrintBanned();
				return;
			}
			if (array.Length != 0 && array[0] == "save")
			{
				ZNet.instance.ConsoleSave();
			}
		}
		if (!ZNet.instance || !ZNet.instance.IsServer() || !Player.m_localPlayer || !IsCheatsEnabled())
		{
			return;
		}
		if (array[0] == "genloc")
		{
			ZoneSystem.instance.GenerateLocations();
			return;
		}
		if (array[0] == "players" && array.Length >= 2)
		{
			if (int.TryParse(array[1], out var result3))
			{
				Game.instance.SetForcePlayerDifficulty(result3);
				Print("Setting players to " + result3);
			}
			return;
		}
		if (array[0] == "setkey")
		{
			if (array.Length >= 2)
			{
				ZoneSystem.instance.SetGlobalKey(array[1]);
				Print("Setting global key " + array[1]);
			}
			else
			{
				Print("Syntax: setkey [key]");
			}
		}
		if (array[0] == "resetkeys")
		{
			ZoneSystem.instance.ResetGlobalKeys();
			Print("Global keys cleared");
		}
		if (array[0] == "listkeys")
		{
			List<string> globalKeys = ZoneSystem.instance.GetGlobalKeys();
			Print("Keys " + globalKeys.Count);
			foreach (string item in globalKeys)
			{
				Print(item);
			}
		}
		if (array[0] == "debugmode")
		{
			Player.m_debugMode = !Player.m_debugMode;
			Print("Debugmode " + Player.m_debugMode);
		}
		if (array[0] == "raiseskill")
		{
			if (array.Length > 2)
			{
				string name = array[1];
				int num2 = int.Parse(array[2]);
				Player.m_localPlayer.GetSkills().CheatRaiseSkill(name, num2);
			}
			else
			{
				Print("Syntax: raiseskill [skill] [amount]");
			}
			return;
		}
		if (array[0] == "resetskill")
		{
			if (array.Length > 1)
			{
				string name2 = array[1];
				Player.m_localPlayer.GetSkills().CheatResetSkill(name2);
			}
			else
			{
				Print("Syntax: resetskill [skill]");
			}
			return;
		}
		if (text == "sleep")
		{
			EnvMan.instance.SkipToMorning();
			return;
		}
		if (array[0] == "skiptime")
		{
			double timeSeconds = ZNet.instance.GetTimeSeconds();
			float num3 = 240f;
			if (array.Length > 1)
			{
				num3 = float.Parse(array[1]);
			}
			timeSeconds += (double)num3;
			ZNet.instance.SetNetTime(timeSeconds);
			Print("Skipping " + num3.ToString("0") + "s , Day:" + EnvMan.instance.GetDay(timeSeconds));
			return;
		}
		if (text == "resetcharacter")
		{
			AddString("Reseting character");
			Player.m_localPlayer.ResetCharacter();
			return;
		}
		if (array[0] == "randomevent")
		{
			RandEventSystem.instance.StartRandomEvent();
		}
		if (text.StartsWith("event "))
		{
			if (array.Length > 1)
			{
				string text2 = text.Substring(6);
				if (!RandEventSystem.instance.HaveEvent(text2))
				{
					Print("Random event not found:" + text2);
				}
				else
				{
					RandEventSystem.instance.SetRandomEventByName(text2, Player.m_localPlayer.transform.position);
				}
			}
			return;
		}
		if (array[0] == "stopevent")
		{
			RandEventSystem.instance.ResetRandomEvent();
			return;
		}
		if (text.StartsWith("removedrops"))
		{
			AddString("Removing item drops");
			ItemDrop[] array2 = UnityEngine.Object.FindObjectsOfType<ItemDrop>();
			for (int i = 0; i < array2.Length; i++)
			{
				ZNetView component = array2[i].GetComponent<ZNetView>();
				if ((bool)component && component.IsValid() && component.IsOwner())
				{
					component.Destroy();
				}
			}
		}
		if (text.StartsWith("freefly"))
		{
			Print("Toggling free fly camera");
			GameCamera.instance.ToggleFreeFly();
			return;
		}
		if (array[0] == "ffsmooth")
		{
			if (array.Length <= 1)
			{
				Print(GameCamera.instance.GetFreeFlySmoothness().ToString());
				return;
			}
			if (!float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result4))
			{
				Print("syntax error");
				return;
			}
			Print("Setting free fly camera smoothing:" + result4);
			GameCamera.instance.SetFreeFlySmoothness(result4);
			return;
		}
		if (text.StartsWith("location "))
		{
			if (array.Length <= 1)
			{
				return;
			}
			string name3 = text.Substring(9);
			Vector3 pos = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 10f;
			ZoneSystem.instance.TestSpawnLocation(name3, pos);
		}
		if (array[0] == "spawn")
		{
			if (array.Length <= 1)
			{
				return;
			}
			string text3 = array[1];
			int num4 = ((array.Length < 3) ? 1 : int.Parse(array[2]));
			int num5 = ((array.Length < 4) ? 1 : int.Parse(array[3]));
			GameObject prefab = ZNetScene.instance.GetPrefab(text3);
			if (!prefab)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Missing object " + text3);
				return;
			}
			DateTime now = DateTime.Now;
			if (num4 == 1)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Spawning object " + text3);
				Character component2 = UnityEngine.Object.Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity).GetComponent<Character>();
				if ((bool)component2 && num5 > 1)
				{
					component2.SetLevel(num5);
				}
			}
			else
			{
				for (int j = 0; j < num4; j++)
				{
					Vector3 b = UnityEngine.Random.insideUnitSphere * 0.5f;
					Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Spawning object " + text3);
					Character component3 = UnityEngine.Object.Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + b, Quaternion.identity).GetComponent<Character>();
					if ((bool)component3 && num5 > 1)
					{
						component3.SetLevel(num5);
					}
				}
			}
			ZLog.Log((object)("Spawn time :" + (DateTime.Now - now).TotalMilliseconds + " ms"));
			Gogan.LogEvent("Cheat", "Spawn", text3, num4);
			return;
		}
		if (array[0] == "pos")
		{
			Player localPlayer = Player.m_localPlayer;
			if ((bool)localPlayer)
			{
				AddString("Player position (X,Y,Z):" + localPlayer.transform.position.ToString("F0"));
			}
		}
		if (text.StartsWith("goto "))
		{
			string text4 = text.Substring(5);
			char[] separator = new char[2]
			{
				',',
				' '
			};
			string[] array3 = text4.Split(separator);
			if (array3.Length < 2)
			{
				AddString("Syntax /goto x,y");
				return;
			}
			try
			{
				float x = float.Parse(array3[0]);
				float z = float.Parse(array3[1]);
				Player localPlayer2 = Player.m_localPlayer;
				if ((bool)localPlayer2)
				{
					Vector3 pos2 = new Vector3(x, localPlayer2.transform.position.y, z);
					localPlayer2.TeleportTo(pos2, localPlayer2.transform.rotation, distantTeleport: true);
				}
			}
			catch (Exception ex)
			{
				ZLog.Log((object)("parse error:" + ex.ToString() + "  " + text4));
			}
			Gogan.LogEvent("Cheat", "Goto", "", 0L);
			return;
		}
		if (text.StartsWith("exploremap"))
		{
			Minimap.instance.ExploreAll();
			return;
		}
		if (text.StartsWith("resetmap"))
		{
			Minimap.instance.Reset();
			return;
		}
		if (text.StartsWith("puke") && (bool)Player.m_localPlayer)
		{
			Player.m_localPlayer.ClearFood();
		}
		if (text.StartsWith("tame"))
		{
			Tameable.TameAllInArea(Player.m_localPlayer.transform.position, 20f);
		}
		if (text.StartsWith("killall"))
		{
			foreach (Character allCharacter in Character.GetAllCharacters())
			{
				if (!allCharacter.IsPlayer())
				{
					HitData hitData = new HitData();
					hitData.m_damage.m_damage = 1E+10f;
					allCharacter.Damage(hitData);
				}
			}
			return;
		}
		if (text.StartsWith("heal"))
		{
			Player.m_localPlayer.Heal(Player.m_localPlayer.GetMaxHealth());
			return;
		}
		if (text.StartsWith("god"))
		{
			Player.m_localPlayer.SetGodMode(!Player.m_localPlayer.InGodMode());
			Print("God mode:" + Player.m_localPlayer.InGodMode());
			Gogan.LogEvent("Cheat", "God", Player.m_localPlayer.InGodMode().ToString(), 0L);
		}
		if (text.StartsWith("ghost"))
		{
			Player.m_localPlayer.SetGhostMode(!Player.m_localPlayer.InGhostMode());
			Print("Ghost mode:" + Player.m_localPlayer.InGhostMode());
			Gogan.LogEvent("Cheat", "Ghost", Player.m_localPlayer.InGhostMode().ToString(), 0L);
		}
		if (text.StartsWith("beard"))
		{
			string beard = ((text.Length >= 6) ? text.Substring(6) : "");
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.SetBeard(beard);
			}
			return;
		}
		if (text.StartsWith("hair"))
		{
			string hair = ((text.Length >= 5) ? text.Substring(5) : "");
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.SetHair(hair);
			}
			return;
		}
		if (text.StartsWith("model "))
		{
			string s = text.Substring(6);
			if ((bool)Player.m_localPlayer && int.TryParse(s, out var result5))
			{
				Player.m_localPlayer.SetPlayerModel(result5);
			}
			return;
		}
		if (text.StartsWith("tod "))
		{
			if (!float.TryParse(text.Substring(4), NumberStyles.Float, CultureInfo.InvariantCulture, out var result6))
			{
				return;
			}
			Print("Setting time of day:" + result6);
			if (result6 < 0f)
			{
				EnvMan.instance.m_debugTimeOfDay = false;
			}
			else
			{
				EnvMan.instance.m_debugTimeOfDay = true;
				EnvMan.instance.m_debugTime = Mathf.Clamp01(result6);
			}
		}
		if (array[0] == "env" && array.Length > 1)
		{
			string text5 = text.Substring(4);
			Print("Setting debug enviornment:" + text5);
			EnvMan.instance.m_debugEnv = text5;
			return;
		}
		if (text.StartsWith("resetenv"))
		{
			Print("Reseting debug enviornment");
			EnvMan.instance.m_debugEnv = "";
			return;
		}
		if (array[0] == "wind" && array.Length == 3)
		{
			float angle = float.Parse(array[1]);
			float intensity = float.Parse(array[2]);
			EnvMan.instance.SetDebugWind(angle, intensity);
		}
		if (array[0] == "resetwind")
		{
			EnvMan.instance.ResetDebugWind();
		}
	}

	public bool IsCheatsEnabled()
	{
		if (m_cheat)
		{
			if ((bool)ZNet.instance)
			{
				return ZNet.instance.IsServer();
			}
			return false;
		}
		return false;
	}
}
