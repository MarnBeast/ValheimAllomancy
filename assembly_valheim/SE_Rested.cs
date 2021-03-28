using System.Collections.Generic;
using UnityEngine;

public class SE_Rested : SE_Stats
{
	private static int ghostLayer = 0;

	private static List<Piece> m_tempPieces = new List<Piece>();

	[Header("__SE_Rested__")]
	public float m_baseTTL = 300f;

	public float m_TTLPerComfortLevel = 60f;

	private const float m_comfortRadius = 10f;

	private float m_timeSinceComfortUpdate;

	public override void Setup(Character character)
	{
		base.Setup(character);
		UpdateTTL();
		Player player = m_character as Player;
		m_character.Message(MessageHud.MessageType.Center, "$se_rested_start ($se_rested_comfort:" + player.GetComfortLevel() + ")");
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timeSinceComfortUpdate -= dt;
	}

	public override void ResetTime()
	{
		UpdateTTL();
	}

	private void UpdateTTL()
	{
		Player player = m_character as Player;
		float num = m_baseTTL + (float)(player.GetComfortLevel() - 1) * m_TTLPerComfortLevel;
		float num2 = m_ttl - m_time;
		if (num > num2)
		{
			m_ttl = num;
			m_time = 0f;
		}
	}

	private static int PieceComfortSort(Piece x, Piece y)
	{
		if (x.m_comfortGroup != y.m_comfortGroup)
		{
			return x.m_comfortGroup.CompareTo(y.m_comfortGroup);
		}
		if (x.m_comfort != y.m_comfort)
		{
			return x.m_comfort.CompareTo(y.m_comfort);
		}
		return y.m_name.CompareTo(x.m_name);
	}

	public static int CalculateComfortLevel(Player player)
	{
		if (ghostLayer == 0)
		{
			ghostLayer = LayerMask.NameToLayer("ghost");
		}
		List<Piece> nearbyPieces = GetNearbyPieces(player.transform.position);
		nearbyPieces.Sort(PieceComfortSort);
		int num = 1;
		if (player.InShelter())
		{
			num++;
			for (int i = 0; i < nearbyPieces.Count; i++)
			{
				Piece piece = nearbyPieces[i];
				if (i > 0)
				{
					Piece piece2 = nearbyPieces[i - 1];
					if (piece2.gameObject.layer == ghostLayer || (piece.m_comfortGroup != 0 && piece.m_comfortGroup == piece2.m_comfortGroup) || piece.m_name == piece2.m_name)
					{
						continue;
					}
				}
				num += piece.m_comfort;
			}
		}
		return num;
	}

	private static List<Piece> GetNearbyPieces(Vector3 point)
	{
		m_tempPieces.Clear();
		Piece.GetAllPiecesInRadius(point, 10f, m_tempPieces);
		return m_tempPieces;
	}
}
