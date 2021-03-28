using System;
using System.Collections.Generic;
using System.Threading;
using Steamworks;

public class ZSteamSocketOLD : IDisposable, ISocket
{
	private static List<ZSteamSocketOLD> m_sockets = new List<ZSteamSocketOLD>();

	private static Callback<P2PSessionRequest_t> m_SessionRequest;

	private static Callback<P2PSessionConnectFail_t> m_connectionFailed;

	private Queue<ZSteamSocketOLD> m_pendingConnections = new Queue<ZSteamSocketOLD>();

	private CSteamID m_peerID = CSteamID.Nil;

	private bool m_listner;

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_gotData;

	public ZSteamSocketOLD()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		m_sockets.Add(this);
		RegisterGlobalCallbacks();
	}

	public ZSteamSocketOLD(CSteamID peerID)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		m_sockets.Add(this);
		m_peerID = peerID;
		RegisterGlobalCallbacks();
	}

	private static void RegisterGlobalCallbacks()
	{
		if (m_connectionFailed == null)
		{
			ZLog.Log((object)"ZSteamSocketOLD  Registering global callbacks");
			m_connectionFailed = Callback<P2PSessionConnectFail_t>.Create((DispatchDelegate<P2PSessionConnectFail_t>)OnConnectionFailed);
		}
		if (m_SessionRequest == null)
		{
			m_SessionRequest = Callback<P2PSessionRequest_t>.Create((DispatchDelegate<P2PSessionRequest_t>)OnSessionRequest);
		}
	}

	private static void UnregisterGlobalCallbacks()
	{
		ZLog.Log((object)("ZSteamSocket  UnregisterGlobalCallbacks, existing sockets:" + m_sockets.Count));
		if (m_connectionFailed != null)
		{
			m_connectionFailed.Dispose();
			m_connectionFailed = null;
		}
		if (m_SessionRequest != null)
		{
			m_SessionRequest.Dispose();
			m_SessionRequest = null;
		}
	}

	private static void OnConnectionFailed(P2PSessionConnectFail_t data)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)("Got connection failed callback: " + data.m_steamIDRemote));
		foreach (ZSteamSocketOLD socket in m_sockets)
		{
			if (socket.IsPeer(data.m_steamIDRemote))
			{
				socket.Close();
			}
		}
	}

	private static void OnSessionRequest(P2PSessionRequest_t data)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)("Got session request from " + data.m_steamIDRemote));
		if (SteamNetworking.AcceptP2PSessionWithUser(data.m_steamIDRemote))
		{
			GetListner()?.QueuePendingConnection(data.m_steamIDRemote);
		}
	}

	public void Dispose()
	{
		ZLog.Log((object)"Disposing socket");
		Close();
		m_pkgQueue.Clear();
		m_sockets.Remove(this);
		if (m_sockets.Count == 0)
		{
			ZLog.Log((object)"Last socket, unregistering callback");
			UnregisterGlobalCallbacks();
		}
	}

	public void Close()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		ZLog.Log((object)("Closing socket " + GetEndPointString()));
		if (m_peerID != CSteamID.Nil)
		{
			Flush();
			ZLog.Log((object)("  send queue size:" + m_sendQueue.Count));
			Thread.Sleep(100);
			P2PSessionState_t val = default(P2PSessionState_t);
			SteamNetworking.GetP2PSessionState(m_peerID, ref val);
			ZLog.Log((object)("  P2P state, bytes in send queue:" + val.m_nBytesQueuedForSend));
			SteamNetworking.CloseP2PSessionWithUser(m_peerID);
			SteamUser.EndAuthSession(m_peerID);
			m_peerID = CSteamID.Nil;
		}
		m_listner = false;
	}

	public bool StartHost()
	{
		m_listner = true;
		m_pendingConnections.Clear();
		return true;
	}

	private ZSteamSocketOLD QueuePendingConnection(CSteamID id)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		foreach (ZSteamSocketOLD pendingConnection in m_pendingConnections)
		{
			if (pendingConnection.IsPeer(id))
			{
				return pendingConnection;
			}
		}
		ZSteamSocketOLD zSteamSocketOLD = new ZSteamSocketOLD(id);
		m_pendingConnections.Enqueue(zSteamSocketOLD);
		return zSteamSocketOLD;
	}

	public ISocket Accept()
	{
		if (!m_listner)
		{
			return null;
		}
		if (m_pendingConnections.Count > 0)
		{
			return m_pendingConnections.Dequeue();
		}
		return null;
	}

	public bool IsConnected()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return m_peerID != CSteamID.Nil;
	}

	public void Send(ZPackage pkg)
	{
		if (pkg.Size() != 0 && IsConnected())
		{
			byte[] array = pkg.GetArray();
			byte[] bytes = BitConverter.GetBytes(array.Length);
			byte[] array2 = new byte[array.Length + bytes.Length];
			bytes.CopyTo(array2, 0);
			array.CopyTo(array2, 4);
			m_sendQueue.Enqueue(array);
			SendQueuedPackages();
		}
	}

	public bool Flush()
	{
		SendQueuedPackages();
		return m_sendQueue.Count == 0;
	}

	private void SendQueuedPackages()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (!IsConnected())
		{
			return;
		}
		while (m_sendQueue.Count > 0)
		{
			byte[] array = m_sendQueue.Peek();
			EP2PSend val = (EP2PSend)2;
			if (SteamNetworking.SendP2PPacket(m_peerID, array, (uint)array.Length, val, 0))
			{
				m_totalSent += array.Length;
				m_sendQueue.Dequeue();
				continue;
			}
			break;
		}
	}

	public static void Update()
	{
		foreach (ZSteamSocketOLD socket in m_sockets)
		{
			socket.SendQueuedPackages();
		}
		ReceivePackages();
	}

	private static void ReceivePackages()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		uint num = default(uint);
		uint num2 = default(uint);
		CSteamID sender = default(CSteamID);
		while (SteamNetworking.IsP2PPacketAvailable(ref num, 0))
		{
			byte[] array = new byte[num];
			if (SteamNetworking.ReadP2PPacket(array, num, ref num2, ref sender, 0))
			{
				QueueNewPkg(sender, array);
				continue;
			}
			break;
		}
	}

	private static void QueueNewPkg(CSteamID sender, byte[] data)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		foreach (ZSteamSocketOLD socket in m_sockets)
		{
			if (socket.IsPeer(sender))
			{
				socket.QueuePackage(data);
				return;
			}
		}
		ZSteamSocketOLD listner = GetListner();
		if (listner != null)
		{
			ZLog.Log((object)("Got package from unconnected peer " + sender));
			listner.QueuePendingConnection(sender).QueuePackage(data);
		}
		else
		{
			ZLog.Log((object)string.Concat("Got package from unkown peer ", sender, " but no active listner"));
		}
	}

	private static ZSteamSocketOLD GetListner()
	{
		foreach (ZSteamSocketOLD socket in m_sockets)
		{
			if (socket.IsHost())
			{
				return socket;
			}
		}
		return null;
	}

	private void QueuePackage(byte[] data)
	{
		ZPackage item = new ZPackage(data);
		m_pkgQueue.Enqueue(item);
		m_gotData = true;
		m_totalRecv += data.Length;
	}

	public ZPackage Recv()
	{
		if (!IsConnected())
		{
			return null;
		}
		if (m_pkgQueue.Count > 0)
		{
			return m_pkgQueue.Dequeue();
		}
		return null;
	}

	public string GetEndPointString()
	{
		return ((object)(CSteamID)(ref m_peerID)).ToString();
	}

	public string GetHostName()
	{
		return ((object)(CSteamID)(ref m_peerID)).ToString();
	}

	public CSteamID GetPeerID()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return m_peerID;
	}

	public bool IsPeer(CSteamID peer)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		if (!IsConnected())
		{
			return false;
		}
		return peer == m_peerID;
	}

	public bool IsHost()
	{
		return m_listner;
	}

	public bool IsSending()
	{
		if (!IsConnected())
		{
			return false;
		}
		return m_sendQueue.Count > 0;
	}

	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
	}

	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = m_totalSent;
		totalRecv = m_totalRecv;
		m_totalSent = 0;
		m_totalRecv = 0;
	}

	public bool GotNewData()
	{
		bool gotData = m_gotData;
		m_gotData = false;
		return gotData;
	}

	public int GetHostPort()
	{
		if (IsHost())
		{
			return 1;
		}
		return -1;
	}
}
