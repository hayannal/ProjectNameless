using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ActorStatusDefine;

public class EquipListStatusInfo : MonoBehaviour
{
	public Image gradeBackImage;
	public Text gradeText;
	public Text nameText;
	public Button detailShowButton;

	public EquipCanvasListItem equipListItem;

	public Button lockButton;
	public Button unlockButton;

	public Text mainStatusText;
	public Image mainStatusFillImage;
	public GameObject[] optionStatusObjectList;
	public Text[] optionStatusTextList;
	public Text[] optionStatusValueTextList;
	public Image[] optionStatusFillImageList;
	public GameObject noOptionTextObject;

	public GameObject equipButtonObject;
	public GameObject unequipButtonObject;
	public Image optionButtonImage;
	public Text optionButtonText;

	bool _equipped = false;
	EquipData _equipData = null;
	public EquipData equipData { get { return _equipData; } }
	public void RefreshInfo(EquipData equipData, bool equipped)
	{
		_equipped = equipped;
		_equipData = equipData;
		gradeBackImage.color = GetGradeTitleBarColor(equipData.cachedEquipTableData.grade);
		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", equipData.cachedEquipTableData.grade)));
		nameText.SetLocalizedText(UIString.instance.GetString(equipData.cachedEquipTableData.nameId));
#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.LeftShift))
			Debug.LogFormat("equipId : {0}", equipData.cachedEquipTableData.equipId);
		if (Input.GetKey(KeyCode.LeftControl))
			Debug.LogFormat("uniqueId : {0}", equipData.uniqueId);
#endif
		if (detailShowButton != null) detailShowButton.gameObject.SetActive(!equipped);

		RefreshLockInfo();
		RefreshStatus();

		if (equipButtonObject != null) equipButtonObject.gameObject.SetActive(!equipped);
		if (unequipButtonObject != null) unequipButtonObject.gameObject.SetActive(equipped);

		bool usableEquipOption = false;
		optionButtonImage.color = usableEquipOption ? Color.white : ColorUtil.halfGray;
		optionButtonText.color = usableEquipOption ? Color.white : Color.gray;
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

		//mainStatusFillImage.fillAmount = _equipData.GetMainStatusRatio();
		mainStatusFillImage.fillAmount = 1.0f;
		bool fullGauge = (mainStatusFillImage.fillAmount == 1.0f);
		mainStatusText.text = _equipData.mainStatusValue.ToString("N0");
		mainStatusFillImage.color = GetGaugeColor(fullGauge);
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

		PlayFabApiManager.instance.RequestEquip(_equipData, () =>
		{
			/*
			if (GuideQuestData.instance.CheckEquipType((TimeSpaceData.eEquipSlotType)_equipData.cachedEquipTableData.equipType))
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EquipType);
			*/

			// 대부분 다 EquipList가 해야하는 것들이라 ListCanvas에게 알린다.
			EquipListCanvas.instance.OnEquip(_equipData);
		});
	}

	public void OnClickUnequipButton()
	{
		if (!_equipped)
			return;
		if (_equipData == null)
			return;

		PlayFabApiManager.instance.RequestUnequip(_equipData, () =>
		{
			EquipListCanvas.instance.OnUnequip(_equipData);
		});
	}

	public void OnClickEnhanceButton()
	{
	}

	public void OnClickOptionButton()
	{
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
}