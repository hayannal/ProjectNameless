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

	// 가이드처럼 단독 아이템을 줄때도 있다.
	public void ConsumeGacha(string rewardValue, int rewardCount)
	{
		_dicConsumeItem.Clear();
		_dicConsumeItem.Add(rewardValue, rewardCount);
		ProcessConsume();
	}

	// 샵프로덕트에 컨슘이 들어있을때도 있다.
	public static bool ConstainsConsumeGacha(ShopProductTableData shopProductTableData)
	{
		// type만 검사하고 value까진 검사하지 않는다. 팔거라고 가정한 곳에서만 호출할테니 이렇게만 해도 된다.
		if (shopProductTableData.rewardType1 == "it" ||
			shopProductTableData.rewardType2 == "it" ||
			shopProductTableData.rewardType3 == "it" ||
			shopProductTableData.rewardType4 == "it" ||
			shopProductTableData.rewardType5 == "it")
			return true;

		return false;
	}

	public void ConsumeGacha(ShopProductTableData shopProductTableData)
	{
		_dicConsumeItem.Clear();

		// 주의. type만 검사하고 value까진 검사하지 않으니 컨슘 아닌거 넣으면 큰일난다.
		if (shopProductTableData.rewardType1 == "it")
		{
			string value = shopProductTableData.rewardValue1;
			if (_dicConsumeItem.ContainsKey(value))
				_dicConsumeItem[value] += shopProductTableData.rewardCount1;
			else
				_dicConsumeItem.Add(value, shopProductTableData.rewardCount1);
		}
		if (shopProductTableData.rewardType2 == "it")
		{
			string value = shopProductTableData.rewardValue2;
			if (_dicConsumeItem.ContainsKey(value))
				_dicConsumeItem[value] += shopProductTableData.rewardCount2;
			else
				_dicConsumeItem.Add(value, shopProductTableData.rewardCount2);
		}
		if (shopProductTableData.rewardType3 == "it")
		{
			string value = shopProductTableData.rewardValue3;
			if (_dicConsumeItem.ContainsKey(value))
				_dicConsumeItem[value] += shopProductTableData.rewardCount3;
			else
				_dicConsumeItem.Add(value, shopProductTableData.rewardCount3);
		}
		if (shopProductTableData.rewardType4 == "it")
		{
			string value = shopProductTableData.rewardValue4;
			if (_dicConsumeItem.ContainsKey(value))
				_dicConsumeItem[value] += shopProductTableData.rewardCount4;
			else
				_dicConsumeItem.Add(value, shopProductTableData.rewardCount4);
		}
		if (shopProductTableData.rewardType5 == "it")
		{
			string value = shopProductTableData.rewardValue5;
			if (_dicConsumeItem.ContainsKey(value))
				_dicConsumeItem[value] += shopProductTableData.rewardCount5;
			else
				_dicConsumeItem.Add(value, shopProductTableData.rewardCount5);
		}
		ProcessConsume();
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
			case "Cash_sEquipGacha":
				ConsumeEquipGacha(firstCount);
				break;
			case "Cash_sSpell3Gacha":
				ConsumeSpellGacha(firstCount, 3);
				break;
			case "Cash_sSpell4Gacha":
				ConsumeSpellGacha(firstCount, 4);
				break;
			case "Cash_sSpell5Gacha":
				ConsumeSpellGacha(firstCount, 5);
				break;
		}
	}

	#region Spell
	void ConsumeSpellGacha(int itemCount, int fixedStar = 0)
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
			List<ObscuredString> listSpellId = SpellManager.instance.GetRandomIdList(itemCount, fixedStar);
			_count = listSpellId.Count;
			PlayFabApiManager.instance.RequestConsumeSpellGacha(listSpellId, fixedStar, OnRecvResultSpell);
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

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.SpellGacha, _count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Spell, listItemInstance);
	}
	#endregion

	#region Character
	int _characterRandomIdCount = 0;
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
			_characterRandomIdCount = itemCount;
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

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.CharacterGacha, _characterRandomIdCount);

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Character, listItemInstance);

		if (MainCanvas.instance != null)
			MainCanvas.instance.RefreshMenuButton();
	}
	#endregion

	#region Equip
	void ConsumeEquipGacha(int itemCount)
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
			List<ObscuredString> listEquipId = EquipManager.instance.GetRandomIdList(itemCount);
			_count = listEquipId.Count;
			PlayFabApiManager.instance.RequestConsumeEquipGacha(listEquipId, OnRecvResultEquip);
		});
	}

	void OnRecvResultEquip(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = EquipManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EquipGacha, _count);

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Equip, listItemInstance);

		if (MainCanvas.instance != null)
			MainCanvas.instance.RefreshMenuButton();
	}
	#endregion
}