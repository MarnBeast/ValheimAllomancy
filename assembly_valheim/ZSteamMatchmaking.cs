using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Steamworks;

public class ZSteamMatchmaking
{
	private static ZSteamMatchmaking m_instance;

	private const int maxServers = 200;

	private List<ServerData> m_matchmakingServers = new List<ServerData>();

	private List<ServerData> m_dedicatedServers = new List<ServerData>();

	private List<ServerData> m_friendServers = new List<ServerData>();

	private int m_serverListRevision;

	private int m_updateTriggerAccumulator;

	private CallResult<LobbyCreated_t> m_lobbyCreated;

	private CallResult<LobbyMatchList_t> m_lobbyMatchList;

	private CallResult<LobbyEnter_t> m_lobbyEntered;

	private Callback<GameServerChangeRequested_t> m_changeServer;

	private Callback<GameLobbyJoinRequested_t> m_joinRequest;

	private Callback<LobbyDataUpdate_t> m_lobbyDataUpdate;

	private Callback<GetAuthSessionTicketResponse_t> m_authSessionTicketResponse;

	private Callback<SteamServerConnectFailure_t> m_steamServerConnectFailure;

	private Callback<SteamServersConnected_t> m_steamServersConnected;

	private Callback<SteamServersDisconnected_t> m_steamServersDisconnected;

	private CSteamID m_myLobby = CSteamID.Nil;

	private CSteamID m_joinUserID = CSteamID.Nil;

	private CSteamID m_queuedJoinLobby = CSteamID.Nil;

	private bool m_haveJoinAddr;

	private SteamNetworkingIPAddr m_joinAddr;

	private List<KeyValuePair<CSteamID, string>> m_requestedFriendGames = new List<KeyValuePair<CSteamID, string>>();

	private ISteamMatchmakingServerListResponse m_steamServerCallbackHandler;

	private ISteamMatchmakingPingResponse m_joinServerCallbackHandler;

	private HServerQuery m_joinQuery;

	private HServerListRequest m_serverListRequest;

	private bool m_haveListRequest;

	private bool m_refreshingDedicatedServers;

	private bool m_refreshingPublicGames;

	private string m_registerServerName = "";

	private bool m_registerPassword;

	private string m_registerVerson = "";

	private string m_nameFilter = "";

	private bool m_friendsFilter = true;

	private HAuthTicket m_authTicket = HAuthTicket.Invalid;

	public static ZSteamMatchmaking instance => m_instance;

	public static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new ZSteamMatchmaking();
		}
	}

	private ZSteamMatchmaking()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00b0: Expected O, but got Unknown
		//IL_00b0: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected O, but got Unknown
		//IL_00d3: Expected O, but got Unknown
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		m_steamServerCallbackHandler = new ISteamMatchmakingServerListResponse(new ServerResponded(OnServerResponded), new ServerFailedToRespond(OnServerFailedToRespond), new RefreshComplete(OnRefreshComplete));
		m_joinServerCallbackHandler = new ISteamMatchmakingPingResponse(new ServerResponded(OnJoinServerRespond), new ServerFailedToRespond(OnJoinServerFailed));
		m_lobbyCreated = CallResult<LobbyCreated_t>.Create((APIDispatchDelegate<LobbyCreated_t>)OnLobbyCreated);
		m_lobbyMatchList = CallResult<LobbyMatchList_t>.Create((APIDispatchDelegate<LobbyMatchList_t>)OnLobbyMatchList);
		m_changeServer = Callback<GameServerChangeRequested_t>.Create((DispatchDelegate<GameServerChangeRequested_t>)OnChangeServerRequest);
		m_joinRequest = Callback<GameLobbyJoinRequested_t>.Create((DispatchDelegate<GameLobbyJoinRequested_t>)OnJoinRequest);
		m_lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create((DispatchDelegate<LobbyDataUpdate_t>)OnLobbyDataUpdate);
		m_authSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create((DispatchDelegate<GetAuthSessionTicketResponse_t>)OnAuthSessionTicketResponse);
	}

	public byte[] RequestSessionTicket()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		ReleaseSessionTicket();
		byte[] array = new byte[1024];
		uint num = 0u;
		m_authTicket = SteamUser.GetAuthSessionTicket(array, 1024, ref num);
		if (m_authTicket == HAuthTicket.Invalid)
		{
			return null;
		}
		byte[] array2 = new byte[num];
		Buffer.BlockCopy(array, 0, array2, 0, (int)num);
		return array2;
	}

	public void ReleaseSessionTicket()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (!(m_authTicket == HAuthTicket.Invalid))
		{
			SteamUser.CancelAuthTicket(m_authTicket);
			m_authTicket = HAuthTicket.Invalid;
			ZLog.Log((object)"Released session ticket");
		}
	}

	public bool VerifySessionTicket(byte[] ticket, CSteamID steamID)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		return (int)SteamUser.BeginAuthSession(ticket, ticket.Length, steamID) == 0;
	}

	private void OnAuthSessionTicketResponse(GetAuthSessionTicketResponse_t data)
	{
		ZLog.Log((object)"Session auth respons callback");
	}

	private void OnSteamServersConnected(SteamServersConnected_t data)
	{
		ZLog.Log((object)"Game server connected");
	}

	private void OnSteamServersDisconnected(SteamServersDisconnected_t data)
	{
		ZLog.LogWarning((object)"Game server disconnected");
	}

	private void OnSteamServersConnectFail(SteamServerConnectFailure_t data)
	{
		ZLog.LogWarning((object)"Game server connected failed");
	}

	private void OnChangeServerRequest(GameServerChangeRequested_t data)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)("ZSteamMatchmaking got change server request to:" + data.m_rgchServer));
		QueueServerJoin(data.m_rgchServer);
	}

	private void OnJoinRequest(GameLobbyJoinRequested_t data)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)string.Concat("ZSteamMatchmaking got join request friend:", data.m_steamIDFriend, "  lobby:", data.m_steamIDLobby));
		if (!Game.instance)
		{
			QueueLobbyJoin(data.m_steamIDLobby);
		}
	}

	private IPAddress FindIP(string host)
	{
		try
		{
			if (IPAddress.TryParse(host, out var address))
			{
				return address;
			}
			ZLog.Log((object)("Not an ip address " + host + " doing dns lookup"));
			IPHostEntry hostEntry = Dns.GetHostEntry(host);
			if (hostEntry.AddressList.Length == 0)
			{
				ZLog.Log((object)"Dns lookup failed");
				return null;
			}
			ZLog.Log((object)("Got dns entries: " + hostEntry.AddressList.Length));
			IPAddress[] addressList = hostEntry.AddressList;
			foreach (IPAddress iPAddress in addressList)
			{
				if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					return iPAddress;
				}
			}
			return null;
		}
		catch (Exception ex)
		{
			ZLog.Log((object)("Exception while finding ip:" + ex.ToString()));
			return null;
		}
	}

	public void QueueServerJoin(string addr)
	{
		try
		{
			string[] array = addr.Split(':');
			if (array.Length >= 2)
			{
				IPAddress iPAddress = FindIP(array[0]);
				if (iPAddress == null)
				{
					ZLog.Log((object)("Invalid address " + array[0]));
					return;
				}
				uint num = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(iPAddress.GetAddressBytes(), 0));
				int num2 = int.Parse(array[1]);
				ZLog.Log((object)("connect to ip:" + iPAddress.ToString() + " port:" + num2));
				((SteamNetworkingIPAddr)(ref m_joinAddr)).SetIPv4(num, (ushort)num2);
				m_haveJoinAddr = true;
			}
		}
		catch (Exception arg)
		{
			ZLog.Log((object)("Server join exception:" + arg));
		}
	}

	private void OnJoinServerRespond(gameserveritem_t serverData)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)("Got join server data " + serverData.GetServerName() + "  " + serverData.m_steamID));
		((SteamNetworkingIPAddr)(ref m_joinAddr)).SetIPv4(((servernetadr_t)(ref serverData.m_NetAdr)).GetIP(), ((servernetadr_t)(ref serverData.m_NetAdr)).GetConnectionPort());
		m_haveJoinAddr = true;
	}

	private void OnJoinServerFailed()
	{
		ZLog.Log((object)"Failed to get join server data");
	}

	public void QueueLobbyJoin(CSteamID lobbyID)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		uint num = default(uint);
		ushort num2 = default(ushort);
		CSteamID val = default(CSteamID);
		if (SteamMatchmaking.GetLobbyGameServer(lobbyID, ref num, ref num2, ref val))
		{
			ZLog.Log((object)("  hostid: " + val));
			m_joinUserID = val;
			m_queuedJoinLobby = CSteamID.Nil;
		}
		else
		{
			ZLog.Log((object)string.Concat("Failed to get lobby data for lobby ", lobbyID, ", requesting lobby data"));
			m_queuedJoinLobby = lobbyID;
			SteamMatchmaking.RequestLobbyData(lobbyID);
		}
	}

	private void OnLobbyDataUpdate(LobbyDataUpdate_t data)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		CSteamID val = default(CSteamID);
		((CSteamID)(ref val))._002Ector(data.m_ulSteamIDLobby);
		if (val == m_queuedJoinLobby)
		{
			ZLog.Log((object)"Got lobby data, for queued lobby");
			uint num = default(uint);
			ushort num2 = default(ushort);
			CSteamID joinUserID = default(CSteamID);
			if (SteamMatchmaking.GetLobbyGameServer(val, ref num, ref num2, ref joinUserID))
			{
				m_joinUserID = joinUserID;
			}
			m_queuedJoinLobby = CSteamID.Nil;
			return;
		}
		ZLog.Log((object)"Got requested lobby data");
		foreach (KeyValuePair<CSteamID, string> requestedFriendGame in m_requestedFriendGames)
		{
			if (requestedFriendGame.Key == val)
			{
				ServerData lobbyServerData = GetLobbyServerData(val);
				if (lobbyServerData != null)
				{
					lobbyServerData.m_name = requestedFriendGame.Value + " [" + lobbyServerData.m_name + "]";
					m_friendServers.Add(lobbyServerData);
					m_serverListRevision++;
				}
			}
		}
	}

	public void RegisterServer(string name, bool password, string version, bool publicServer, string worldName)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		UnregisterServer();
		SteamAPICall_t val = SteamMatchmaking.CreateLobby((ELobbyType)((!publicServer) ? 1 : 2), 32);
		m_lobbyCreated.Set(val, (APIDispatchDelegate<LobbyCreated_t>)null);
		m_registerServerName = name;
		m_registerPassword = password;
		m_registerVerson = version;
		ZLog.Log((object)"Registering lobby");
	}

	private void OnLobbyCreated(LobbyCreated_t data, bool ioError)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)string.Concat("Lobby was created ", data.m_eResult, "  ", data.m_ulSteamIDLobby, "  error:", ioError.ToString()));
		if (!ioError)
		{
			m_myLobby = new CSteamID(data.m_ulSteamIDLobby);
			SteamMatchmaking.SetLobbyData(m_myLobby, "name", m_registerServerName);
			SteamMatchmaking.SetLobbyData(m_myLobby, "password", m_registerPassword ? "1" : "0");
			SteamMatchmaking.SetLobbyData(m_myLobby, "version", m_registerVerson);
			SteamMatchmaking.SetLobbyGameServer(m_myLobby, 0u, (ushort)0, SteamUser.GetSteamID());
		}
	}

	private void OnLobbyEnter(LobbyEnter_t data, bool ioError)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		ZLog.LogWarning((object)("Entering lobby " + data.m_ulSteamIDLobby));
	}

	public void UnregisterServer()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (m_myLobby != CSteamID.Nil)
		{
			SteamMatchmaking.SetLobbyJoinable(m_myLobby, false);
			SteamMatchmaking.LeaveLobby(m_myLobby);
			m_myLobby = CSteamID.Nil;
		}
	}

	public void RequestServerlist()
	{
		RequestFriendGames();
		RequestPublicLobbies();
		RequestDedicatedServers();
	}

	private void RequestFriendGames()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		m_friendServers.Clear();
		m_requestedFriendGames.Clear();
		int num = SteamFriends.GetFriendCount((EFriendFlags)4);
		if (num == -1)
		{
			ZLog.Log((object)"GetFriendCount returned -1, the current user is not logged in.");
			num = 0;
		}
		FriendGameInfo_t val = default(FriendGameInfo_t);
		for (int i = 0; i < num; i++)
		{
			CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, (EFriendFlags)4);
			string friendPersonaName = SteamFriends.GetFriendPersonaName(friendByIndex);
			if (SteamFriends.GetFriendGamePlayed(friendByIndex, ref val) && val.m_gameID == (CGameID)(ulong)SteamManager.APP_ID && val.m_steamIDLobby != CSteamID.Nil)
			{
				ZLog.Log((object)"Friend is in our game");
				m_requestedFriendGames.Add(new KeyValuePair<CSteamID, string>(val.m_steamIDLobby, friendPersonaName));
				SteamMatchmaking.RequestLobbyData(val.m_steamIDLobby);
			}
		}
		m_serverListRevision++;
	}

	private void RequestPublicLobbies()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		SteamAPICall_t val = SteamMatchmaking.RequestLobbyList();
		m_lobbyMatchList.Set(val, (APIDispatchDelegate<LobbyMatchList_t>)null);
		m_refreshingPublicGames = true;
	}

	private void RequestDedicatedServers()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (!m_refreshingDedicatedServers)
		{
			if (m_haveListRequest)
			{
				SteamMatchmakingServers.ReleaseRequest(m_serverListRequest);
				m_haveListRequest = false;
			}
			m_dedicatedServers.Clear();
			m_serverListRequest = SteamMatchmakingServers.RequestInternetServerList(SteamUtils.GetAppID(), (MatchMakingKeyValuePair_t[])(object)new MatchMakingKeyValuePair_t[0], 0u, m_steamServerCallbackHandler);
			m_refreshingDedicatedServers = true;
			m_haveListRequest = true;
		}
	}

	private void OnLobbyMatchList(LobbyMatchList_t data, bool ioError)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		m_refreshingPublicGames = false;
		m_matchmakingServers.Clear();
		for (int i = 0; i < data.m_nLobbiesMatching; i++)
		{
			CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(i);
			ServerData lobbyServerData = GetLobbyServerData(lobbyByIndex);
			if (lobbyServerData != null)
			{
				m_matchmakingServers.Add(lobbyServerData);
			}
		}
		m_serverListRevision++;
	}

	private ServerData GetLobbyServerData(CSteamID lobbyID)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		string lobbyData = SteamMatchmaking.GetLobbyData(lobbyID, "name");
		bool password = SteamMatchmaking.GetLobbyData(lobbyID, "password") == "1";
		string lobbyData2 = SteamMatchmaking.GetLobbyData(lobbyID, "version");
		int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
		uint num = default(uint);
		ushort num2 = default(ushort);
		CSteamID val = default(CSteamID);
		if (SteamMatchmaking.GetLobbyGameServer(lobbyID, ref num, ref num2, ref val))
		{
			return new ServerData
			{
				m_name = lobbyData,
				m_password = password,
				m_version = lobbyData2,
				m_players = numLobbyMembers,
				m_steamHostID = (ulong)val
			};
		}
		ZLog.Log((object)"Failed to get lobby gameserver");
		return null;
	}

	public void GetServers(List<ServerData> allServers)
	{
		if (m_friendsFilter)
		{
			FilterServers(m_friendServers, allServers);
			return;
		}
		FilterServers(m_matchmakingServers, allServers);
		FilterServers(m_dedicatedServers, allServers);
	}

	private void FilterServers(List<ServerData> input, List<ServerData> allServers)
	{
		string text = m_nameFilter.ToLowerInvariant();
		foreach (ServerData item in input)
		{
			if (text.Length == 0 || item.m_name.ToLowerInvariant().Contains(text))
			{
				allServers.Add(item);
			}
			if (allServers.Count >= 200)
			{
				break;
			}
		}
	}

	public bool GetJoinHost(out CSteamID steamID, out SteamNetworkingIPAddr addr)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		steamID = m_joinUserID;
		addr = m_joinAddr;
		if (((CSteamID)(ref m_joinUserID)).IsValid() || m_haveJoinAddr)
		{
			m_joinUserID = CSteamID.Nil;
			m_haveJoinAddr = false;
			((SteamNetworkingIPAddr)(ref m_joinAddr)).Clear();
			return true;
		}
		return false;
	}

	private void OnServerResponded(HServerListRequest request, int iServer)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(request, iServer);
		string serverName = serverDetails.GetServerName();
		ServerData serverData = new ServerData();
		serverData.m_name = serverName;
		((SteamNetworkingIPAddr)(ref serverData.m_steamHostAddr)).SetIPv4(((servernetadr_t)(ref serverDetails.m_NetAdr)).GetIP(), ((servernetadr_t)(ref serverDetails.m_NetAdr)).GetConnectionPort());
		serverData.m_password = serverDetails.m_bPassword;
		serverData.m_players = serverDetails.m_nPlayers;
		serverData.m_version = serverDetails.GetGameTags();
		m_dedicatedServers.Add(serverData);
		m_updateTriggerAccumulator++;
		if (m_updateTriggerAccumulator > 100)
		{
			m_updateTriggerAccumulator = 0;
			m_serverListRevision++;
		}
	}

	private void OnServerFailedToRespond(HServerListRequest request, int iServer)
	{
	}

	private void OnRefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)("Refresh complete " + m_dedicatedServers.Count + "  " + response));
		m_refreshingDedicatedServers = false;
		m_serverListRevision++;
	}

	public void SetNameFilter(string filter)
	{
		if (!(m_nameFilter == filter))
		{
			m_nameFilter = filter;
			m_serverListRevision++;
		}
	}

	public void SetFriendFilter(bool enabled)
	{
		if (m_friendsFilter != enabled)
		{
			m_friendsFilter = enabled;
			m_serverListRevision++;
		}
	}

	public int GetServerListRevision()
	{
		return m_serverListRevision;
	}

	public bool IsUpdating()
	{
		if (!m_refreshingDedicatedServers)
		{
			return m_refreshingPublicGames;
		}
		return true;
	}

	public int GetTotalNrOfServers()
	{
		return m_matchmakingServers.Count + m_dedicatedServers.Count + m_friendServers.Count;
	}
}
