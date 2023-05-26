using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ActorStatusDefine;

public class EquipListStatusInfo : MonoBehaviour
{
	public Image gradeBackImage;
	public Text gradeText;
	public Text enhanceText;
	public Text nameText;
	public Button detailShowButton;

	public EquipCanvasListItem equipListItem;

	public Button lockButton;
	public Button unlockButton;

	public Text mainStatusText;
	public Image[] optionGradeCircleImageList;
	public Image[] optionGradeCircleLineImageList;
	public GameObject[] optionStatusObjectList;
	public Text[] optionStatusTextList;
	public Text[] optionStatusValueTextList;

	public GameObject equipSkillRootObject;
	public Image equipSkillIconImage;
	public Coffee.UIExtensions.UIEffect iconGrayscaleEffect;
	public Text equipSkillNameText;
	public Image equipSkillGradeCircleImage;
	public Image equipSkillGradeCircleLineImage;
	public GameObject noSpecialOptionTextObject;

	public GameObject equipButtonObject;
	public GameObject unequipButtonObject;

	bool _equipped = false;
	EquipData _equipData = null;
	public EquipData equipData { get { return _equipData; } }
	public void RefreshInfo(EquipData equipData, bool equipped)
	{
		_equipped = equipped;
		_equipData = equipData;
		gradeBackImage.color = GetGradeTitleBarColor(equipData.cachedEquipLevelTableData.grade);
		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", equipData.cachedEquipLevelTableData.grade)));
		enhanceText.gameObject.SetActive(equipData.enhanceLevel > 0);
		enhanceText.text = string.Format("+ {0}", equipData.enhanceLevel);
		nameText.SetLocalizedText(UIString.instance.GetString(equipData.cachedEquipTableData.nameId));
#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.LeftShift))
			Debug.LogFormat("equipId : {0}", equipData.cachedEquipLevelTableData.equipId);
		if (Input.GetKey(KeyCode.LeftControl))
			Debug.LogFormat("uniqueId : {0}", equipData.uniqueId);
#endif
		if (detailShowButton != null) detailShowButton.gameObject.SetActive(!equipped);

		RefreshLockInfo();
		RefreshStatus();

		if (equipButtonObject != null) equipButtonObject.gameObject.SetActive(!equipped);
		if (unequipButtonObject != null) unequipButtonObject.gameObject.SetActive(equipped);
	}

	void RefreshLockInfo()
	{
		if (lockButton != null) lockButton.gameObject.SetActive(_equipData.isLock);
		if (unlockButton != null) unlockButton.gameObject.SetActive(!_equipData.isLock);
	}

	public static Color GetGradeTitleBarColor(int grade)
	{
		switch (grade)
		{
			case 0: return new Color(0.5f, 0.5f, 0.5f);
			case 1: return new Color(0.1f, 0.84f, 0.1f);
			case 2: return new Color(0.0f, 0.51f, 1.0f);
			case 3: return new Color(0.63f, 0.0f, 1.0f);
			case 4: return new Color(1.0f, 0.5f, 0.0f);
			case 5: return new Color(0.87f, 0.2f, 0.04f);
			case 6: return new Color(0.8f, 0.8f, 0.2f);
		}
		return Color.white;
	}

	public static Color GetGradeDropObjectNameColor(int grade)
	{
		switch (grade)
		{
			case 0: return new Color(0.85f, 0.85f, 0.85f);
			case 1: return new Color(0.35f, 0.84f, 0.35f);
			case 2: return new Color(0.38f, 0.65f, 1.0f);
			case 3: return new Color(0.75f, 0.42f, 1.0f);
			case 4: return new Color(1.0f, 0.5f, 0.2f);
		}
		return Color.white;
	}

	static Color _gaugeColor = new Color(0.819f, 0.505f, 0.458f, 0.862f);
	static Color _fullGaugeColor = new Color(0.937f, 0.937f, 0.298f, 0.862f);
	public static Color GetGaugeColor(bool fullGauge)
	{
		if (fullGauge)
			return _fullGaugeColor;
		return _gaugeColor;
	}
	public void RefreshStatus()
	{
		equipListItem.Initialize(_equipData, null);
		mainStatusText.text = _equipData.mainStatusValue.ToString("N0");

		// option. 이젠 테이블에 있는거라 수량이 항상 동일하다.
		for (int i = 0; i < EquipData.EquipOptionCountMax; ++i)
		{
			optionGradeCircleImageList[i].color = EquipAltar.GetGradeOutlineColor(i + 3);
			optionGradeCircleLineImageList[i].gameObject.SetActive(_equipData.IsGetAvailable(i) == false);

			eActorStatus statusType = _equipData.GetOption(i);
			float value = _equipData.GetOptionValue(i);
			optionStatusTextList[i].SetLocalizedText(UIString.instance.GetString(string.Format("Op_{0}", statusType.ToString())));
			optionStatusTextList[i].color = _equipData.IsGetAvailable(i) ? Color.white : Color.gray;
			optionStatusValueTextList[i].text = string.Format("{0:0.##}%", value * 100.0f);
			optionStatusValueTextList[i].color = _equipData.IsGetAvailable(i) ? Color.white : Color.gray;
		}

		equipSkillRootObject.SetActive(false);
		noSpecialOptionTextObject.SetActive(true);
		if (string.IsNullOrEmpty(_equipData.cachedEquipTableData.skillId) == false)
		{
			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(_equipData.cachedEquipTableData.skillId);
			bool skillActived = (_equipData.cachedEquipLevelTableData.grade >= _equipData.cachedEquipTableData.skillActive);
			if (skillTableData != null)
			{
				AddressableAssetLoadManager.GetAddressableSprite(skillTableData.iconPrefab, "Icon", (sprite) =>
				{
					equipSkillIconImage.sprite = null;
					equipSkillIconImage.sprite = sprite;
				});
				equipSkillNameText.SetLocalizedText(UIString.instance.GetString(skillTableData.nameId));
				iconGrayscaleEffect.enabled = skillActived ? false : true;
				equipSkillNameText.color = skillActived ? Color.white : Color.gray;
				equipSkillRootObject.SetActive(true);
				noSpecialOptionTextObject.SetActive(false);
			}
			equipSkillGradeCircleImage.color = EquipAltar.GetGradeOutlineColor(_equipData.cachedEquipTableData.skillActive);
			equipSkillGradeCircleLineImage.gameObject.SetActive(skillActived == false);
		}
	}

	public void OnClickDetailShowButton()
	{
		// 장착되지 않은거라 새로 로딩부터 해야한다.
		// 전환할 캔버스를 열어두고 로딩한답시고 대기할 순 없으니 여기서 로딩이 끝나는걸 확인한 후 처리하기로 한다.
		DelayedLoadingCanvas.Show(true);
		AddressableAssetLoadManager.GetAddressableGameObject(_equipData.cachedEquipTableData.prefabAddress, "Equip", (prefab) =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("EquipInfoDetailCanvas", () =>
			{
				// 비교템을 보여주는 모드로 전환
				EquipInfoGround.instance.ChangeDiffMode(_equipData);
				DelayedLoadingCanvas.Show(false);
			});
		});
	}

	public void OnClickEquipButton()
	{
		if (_equipped)
			return;
		if (_equipData == null)
			return;

		float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestEquip(_equipData, () =>
		{
			/*
			if (GuideQuestData.instance.CheckEquipType((TimeSpaceData.eEquipSlotType)_equipData.cachedEquipTableData.equipType))
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EquipType);
			*/

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			// 대부분 다 EquipList가 해야하는 것들이라 ListCanvas에게 알린다.
			EquipListCanvas.instance.OnEquip(_equipData);
			EquipGroundCanvas.instance.RefreshOptionViewButton();
			EquipGroundCanvas.instance.RefreshAutoEquipAlarmObject();
			MainCanvas.instance.RefreshEquipAlarmObject();

			// 변경 완료를 알리고
			int intPrevValue = (int)prevValue;
			int intNextValue = (int)nextValue;
			if (intPrevValue != intNextValue)
			{
				UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
				{
					ChangePowerCanvas.instance.ShowInfo(prevValue, nextValue);
				});
			}
		});
	}

	public void OnClickUnequipButton()
	{
		if (!_equipped)
			return;
		if (_equipData == null)
			return;

		float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestUnequip(_equipData, () =>
		{
			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			EquipListCanvas.instance.OnUnequip(_equipData);
			EquipGroundCanvas.instance.RefreshOptionViewButton();
			EquipGroundCanvas.instance.RefreshAutoEquipAlarmObject();
			MainCanvas.instance.RefreshEquipAlarmObject();

			// 변경 완료를 알리고
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(prevValue, nextValue);
			});
		});
	}
	
	public void OnClickUnlockButton()
	{
		if (_equipData == null)
			return;

		// 장비가 생성되면 기본이 언락상태고 언락 버튼이 보이게 된다.
		// 이 회색 언락버튼을 눌러야 lock 상태로 바뀌게 된다.
		PlayFabApiManager.instance.RequestLockEquip(_equipData, true, () =>
		{
			// 장착된 아이템이라면 정보창만 갱신하면 되지만
			// 장착되지 않은 아이템이라면 하단 그리드도 갱신해야하니 ListCanvas에 알려야한다.
			equipListItem.Initialize(_equipData, null);
			RefreshLockInfo();
			if (!_equipped)
				EquipListCanvas.instance.RefreshSelectedItem();

			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_Locked"), 2.0f);
		});
	}

	public void OnClickLockButton()
	{
		if (_equipData == null)
			return;

		PlayFabApiManager.instance.RequestLockEquip(_equipData, false, () =>
		{
			equipListItem.Initialize(_equipData, null);
			RefreshLockInfo();
			if (!_equipped)
				EquipListCanvas.instance.RefreshSelectedItem();

			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_Unlocked"), 2.0f);
		});
	}

	public void OnClickCloseButton()
	{
		if (EquipListCanvas.instance != null)
		{
			if (_equipped)
				EquipListCanvas.instance.OnCloseEquippedStatusInfo();
			else
				EquipListCanvas.instance.OnCloseDiffStatusInfo();
		}
		gameObject.SetActive(false);
	}

	public void OnClickSkillActiveInfoButton()
	{
		if (string.IsNullOrEmpty(_equipData.cachedEquipTableData.skillId))
			return;

		SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(_equipData.cachedEquipTableData.skillId);
		if (skillTableData == null)
			return;
		SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(_equipData.cachedEquipTableData.skillId, 1);
		if (skillLevelTableData == null)
			return;

		string nameString = UIString.instance.GetString(skillTableData.useNameIdOverriding ? skillLevelTableData.nameId : skillTableData.nameId);
		string descString = UIString.instance.GetString(skillTableData.useDescriptionIdOverriding ? skillLevelTableData.descriptionId : skillTableData.descriptionId, skillLevelTableData.parameter);
		float cooltime = skillTableData.useCooltimeOverriding ? skillLevelTableData.cooltime : skillTableData.cooltime;

		bool showGradeInfo = (_equipData.cachedEquipTableData.skillActive > _equipData.cachedEquipLevelTableData.grade);
		UIInstanceManager.instance.ShowCanvasAsync("EquipSkillInfoCanvas", () =>
		{
			EquipSkillInfoCanvas.instance.SetInfo(skillTableData, showGradeInfo, _equipData.cachedEquipTableData, nameString, descString, cooltime);
		});
	}
}