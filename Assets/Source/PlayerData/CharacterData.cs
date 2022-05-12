using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterData
{
	public static string s_PlayerActorId = "Actor0201";

	public static string GetAddressByActorId(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return "";
		return actorTableData.prefabAddress;
	}

	public static string GetNameByActorId(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return "";
		return UIString.instance.GetString(actorTableData.nameId);
	}
}