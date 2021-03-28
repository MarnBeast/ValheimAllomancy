using Steamworks;

public class ServerData
{
	public string m_name;

	public string m_host;

	public int m_port;

	public bool m_password;

	public bool m_upnp;

	public string m_version;

	public int m_players;

	public ulong m_steamHostID;

	public SteamNetworkingIPAddr m_steamHostAddr;

	public override bool Equals(object obj)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		ServerData serverData = obj as ServerData;
		if (serverData == null)
		{
			return false;
		}
		if (serverData.m_steamHostID == m_steamHostID)
		{
			return ((SteamNetworkingIPAddr)(ref serverData.m_steamHostAddr)).Equals(m_steamHostAddr);
		}
		return false;
	}

	public override string ToString()
	{
		if (m_steamHostID != 0L)
		{
			return m_steamHostID.ToString();
		}
		string result = default(string);
		((SteamNetworkingIPAddr)(ref m_steamHostAddr)).ToString(ref result, true);
		return result;
	}
}
