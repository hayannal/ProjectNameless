using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class ConsumeProductProcessor : MonoBehaviour
{
	public static ConsumeProductProcessor instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("ConsumeProductProcessor")).AddComponent<ConsumeProductProcessor>();
			return _instance;
		}
	}
	static ConsumeProductProcessor _instance = null;

	Dictionary<string, int> _dicConsumeItem = new Dictionary<string, int>();

	bool _readyForConsume = false;
	void Update()
	{
		if (_readyForConsume)
		{
			_readyForConsume = false;
			ProcessConsume();
		}
	}

	// 이벤트에서는 이런 식으로 전달받는다.
	public void ConsumeGacha(List<string> listItemValue, List<int> listItemCount)
	{
		_dicConsumeItem.Clear();

		// 같은건 하나로 합쳐서 들고있어야한다.
		for (int i = 0; i < listItemValue.Count; ++i)
		{
			string value = listItemValue[i];
			if (_dicConsumeItem.ContainsKey(value))
				_dicConsumeItem[value] += listItemCount[i];
			else
				_dicConsumeItem.Add(value, listItemCount[i]);
		}

		ProcessConsume();
	}

	// 이런식으로 복구용도 위한 함수도 있다.
	public void AddConsumeGacha(string value, int count)
	{
		if (_dicConsumeItem.ContainsKey(value))
			_dicConsumeItem[value] += count;
		else
			_dicConsumeItem.Add(value, count);
	}

	public void ProcessConsume()
	{
		if (_dicConsumeItem.Keys.Count == 0)
			return;

		Dictionary<string, int>.Enumerator e = _dicConsumeItem.GetEnumerator();
		e.MoveNext();
		string firstKey = e.Current.Key;
		int firstCount = _dicConsumeItem[firstKey];
		_dicConsumeItem.Remove(firstKey);
		switch (firstKey)
		{
			case "Cash_sSpellGacha":
				ConsumeSpellGacha(firstCount);
				break;
			case "Cash_sCharacterGacha":
				ConsumeCharacterGacha(firstCount);
				break;
		}
	}

	#region Spell
	void ConsumeSpellGacha(int itemCount)
	{
		// 혹시나 로드 되어있지 않다면 로드 걸어둔다. 연출 다 되기 전엔 로딩 될거다.
		if (SpellSpriteContainer.instance == null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject("SpellSpriteContainer", "", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
			});
		}

		// 연출 및 보상 처리. 100개씩 뽑으면 느릴 수 있으니 패킷 대기 없이 바로 시작한다.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 남은 Consume이 있다면 다시 Consume 처리할 수 있도록 Callback을 설정해둔다.
			if (_dicConsumeItem.Count > 0)
			{
				RandomBoxScreenCanvas.instance.SetCloseCallback(() =>
				{
					_readyForConsume = true;
				});
			}

			// 연출창 시작과 동시에 패킷을 보내고
			List<ObscuredString> listSpellId = SpellManager.instance.GetRandomIdList(itemCount);
			_count = listSpellId.Count;
			PlayFabApiManager.instance.RequestConsumeSpellGacha(listSpellId, OnRecvResultSpell);
		});
	}

	int _count;
	void OnRecvResultSpell(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = SpellManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Spell, listItemInstance);
	}
	#endregion

	#region Character
	void ConsumeCharacterGacha(int itemCount)
	{
		// 연출 및 보상 처리. 100개씩 뽑으면 느릴 수 있으니 패킷 대기 없이 바로 시작한다.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 남은 Consume이 있다면 다시 Consume 처리할 수 있도록 Callback을 설정해둔다.
			if (_dicConsumeItem.Count > 0)
			{
				RandomBoxScreenCanvas.instance.SetCloseCallback(() =>
				{
					_readyForConsume = true;
				});
			}

			// 연출창 시작과 동시에 패킷을 보내고
			List<ObscuredString> listActorId = CharacterManager.instance.GetRandomIdList(itemCount);
			_count = listActorId.Count;
			PlayFabApiManager.instance.RequestConsumeCharacterGacha(listActorId, OnRecvResultCharacter);
		});
	}

	void OnRecvResultCharacter(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = CharacterManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Character, listItemInstance);

		if (MainCanvas.instance != null)
			MainCanvas.instance.RefreshMenuButton();
	}
	#endregion
}