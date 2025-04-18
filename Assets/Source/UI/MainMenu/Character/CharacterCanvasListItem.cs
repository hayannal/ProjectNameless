﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCanvasListItem : MonoBehaviour
{
	public RectTransform contentRectTransform;
	public Image characterImage;
	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image lineColorImage;
	public GameObject levelObject;
	public Text levelText;
	public GameObject[] transcendGroupList;
	public Text nameText;
	public Text powerSourceText;
	public Text recommandedText;
	public GameObject selectObject;
	public GameObject equippedObject;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	/*
	public enum ePowerLevelColorState
	{
		Normal,     // 기본 상태
		LimitBreak, // 한계돌파에 막혀있는 상태
		RedAlert,   // 한계돌파에 막혀있지만 pp수량이 다음 레벨업 수치를 넘어선 상태. 얼른 한계돌파를 풀고 레벨업을 하란 의미.
	}

	public static ePowerLevelColorState GetPowerLevelColorState(CharacterData characterData)
	{
		if (characterData == null)
			return ePowerLevelColorState.Normal;

		if (characterData.needLimitBreak)
		{
			PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(characterData.powerLevel + 1);
			if (characterData.pp >= nextPowerLevelTableData.requiredAccumulatedPowerPoint)
				return ePowerLevelColorState.RedAlert;
			return ePowerLevelColorState.LimitBreak;
		}

		return ePowerLevelColorState.Normal;
	}
	*/

	public string actorId { get; set; }
	public void Initialize(string actorId, int level, int transcendLevel, bool shopItem, int suggestedPowerLevel, string[] suggestedActorIdList, List<int> listPenaltyPowerSource,  Action<string> clickCallback)
	{
		this.actorId = actorId;

		//bool mercenary = MercenaryData.IsMercenaryActor(actorId);
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);

		characterImage.gameObject.SetActive(false);
		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
			characterImage.gameObject.SetActive(true);
		});

		levelObject.SetActive(level > 0);
		levelText.text = UIString.instance.GetString("GameUI_Lv", level);
		levelText.color = Color.white;
		//powerLevelText.color = (characterData.powerLevel < suggestedPowerLevel) ? new Color(1.0f, 0.1f, 0.1f) : Color.white;
		/*
		switch (colorState)
		{
			case ePowerLevelColorState.Normal: powerLevelText.color = Color.white; break;
			case ePowerLevelColorState.LimitBreak: powerLevelText.color = Color.gray; break;
			case ePowerLevelColorState.RedAlert: powerLevelText.color = new Color(1.0f, 0.5f, 0.5f); break;
		}
		*/
		for (int i = 0; i < transcendGroupList.Length; ++i)
			transcendGroupList[i].SetActive(i == (transcendLevel - 1));
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		powerSourceText.SetLocalizedText(PowerSource.Index2Name(actorTableData.powerSource));

		InitializeGrade(actorTableData.grade);

		if (shopItem == false && CharacterManager.instance.listTeamPositionId.Contains(actorId))
			equippedObject.SetActive(true);
		else
			equippedObject.SetActive(false);

		recommandedText.gameObject.SetActive(false);
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (CheckSuggestedActor(suggestedActorIdList, actorId))
		{
			recommandedText.color = (listPenaltyPowerSource != null && listPenaltyPowerSource.Contains(actorTableData.powerSource)) ? new Color(0.831f, 0.831f, 0.831f) : new Color(0.074f, 1.0f, 0.0f);
			recommandedText.SetLocalizedText(UIString.instance.GetString("GameUI_Suggested"));
			recommandedText.gameObject.SetActive(true);
		}
		/*
		if (lobby == false && mercenary)
		{
			recommandedText.color = new Color(0.074f, 1.0f, 0.0f);
			recommandedText.SetLocalizedText(UIString.instance.GetString("GameUI_TodayMercenary"));
			recommandedText.gameObject.SetActive(true);
		}
		*/
		bool showBlackObject = false;
		if (lobby && level == 0 && shopItem == false) showBlackObject = true;
		/*
		if (InvasionEnterCanvas.instance != null && InvasionEnterCanvas.instance.gameObject.activeSelf && ContentsData.instance.listInvasionEnteredActorId.Contains(actorId)) showBlackObject = true;
		*/
		blackObject.SetActive(showBlackObject);

		selectObject.SetActive(false);
		_clickAction = clickCallback;
	}

	public static bool CheckSuggestedActor(string[] suggestedActorIdList, string actorId)
	{
		if (suggestedActorIdList == null)
			return false;
		for (int i = 0; i < suggestedActorIdList.Length; ++i)
		{
			if (suggestedActorIdList[i] == actorId)
				return true;
		}
		return false;
	}

	public void InitializeGrade(int grade, bool questionCharacter = false)
	{
		switch (grade)
		{
			case 0:
				blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				gradient.color1 = Color.white;
				gradient.color2 = Color.black;
				lineColorImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				blurImage.color = new Color(0.28f, 0.78f, 1.0f, 0.0f);
				gradient.color1 = new Color(0.0f, 0.7f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 2:
				blurImage.color = new Color(1.0f, 0.78f, 0.31f, 0.0f);
				gradient.color1 = new Color(1.0f, 0.5f, 0.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
		}

		if (questionCharacter == false)
			return;

		// 특별히 이름 자리에 등급을 표시한다.
		nameText.SetLocalizedText(string.Format("<size=30>{0}</size>", UIString.instance.GetString(string.Format("GameUI_CharGrade{0}", grade))));
		levelObject.SetActive(false);
		recommandedText.gameObject.SetActive(false);

		characterImage.gameObject.SetActive(false);
		AddressableAssetLoadManager.GetAddressableSprite("Portrait_Nobody", "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
			characterImage.color = new Color(1.0f, 1.0f, 1.0f, 0.9f);
			characterImage.gameObject.SetActive(true);
		});
	}

	Action<string> _clickAction;
	public void OnClickButton()
	{
		if (_clickAction != null)
			_clickAction.Invoke(actorId);
		//SoundManager.instance.PlaySFX("GridOn");
	}
	

	#region Alarm
	// 다른 Alarm 가진 오브젝트들과 달리 캐릭터창은 다른 창들과 GridItem을 공유하면서도 해당 캔버스에서만 보여야하기 때문에 ListItem 단에서 처리하지 않는다.
	// 그래서 밖에서 컨트롤 할 수 있게 public 함수로만 만들어두고 사용한다.
	public void ShowAlarm(bool show)
	{
		if (show)
		{
			AlarmObject.Show(alarmRootTransform, true, true, false, false, lineColorImage.color);
		}
		else
		{
			AlarmObject.Hide(alarmRootTransform);
		}
	}
	#endregion




	RectTransform _rectTransform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}
}