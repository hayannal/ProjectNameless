using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class CharacterInfoGrowthCanvas : MonoBehaviour
{
	public static CharacterInfoGrowthCanvas instance;

	public Image gradeBackImage;
	public Text gradeText;
	public Text nameText;
	public Text powerSourceText;

	public Text levelText;
	public Text atkText;

	public GameObject[] fillImageObjectList;
	public GameObject[] tweenAnimationObjectList;
	public GameObject[] subTweenAnimationObjectList;

	public GameObject maxInfoObject;
	public GameObject materialInfoObject;
	public Transform needOriginTextTransform;
	public Text needOriginCountText;
	public GameObject transcendButtonObject;
	public GameObject transcendEmptyObject;

	public GameObject sliderRectObject;
	public Slider ppSlider;
	public Image sliderFillImage;
	public Text ppText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;
	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	#region Info
	string _actorId;
	bool _contains;
	public void RefreshInfo()
	{
		string actorId = CharacterListCanvas.instance.selectedActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);

		switch (actorTableData.grade)
		{
			case 0:
				gradeBackImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				gradeBackImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 2:
				gradeBackImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
		}
		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_CharGrade{0}", actorTableData.grade)));
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		powerSourceText.SetLocalizedText(PowerSource.Index2Name(actorTableData.powerSource));

		bool defaultTranscend = false;
		CharacterData characterData = CharacterManager.instance.GetCharacterData(actorId);
		if (characterData == null || characterData.transcendPoint == 0)
			defaultTranscend = true;

		if (defaultTranscend)
		{
			for (int i = 0; i < fillImageObjectList.Length; ++i)
			{
				fillImageObjectList[i].gameObject.SetActive(false);
				tweenAnimationObjectList[i].gameObject.SetActive(false);
				subTweenAnimationObjectList[i].gameObject.SetActive(false);
			}
			maxInfoObject.SetActive(false);
			materialInfoObject.SetActive(true);
			needOriginCountText.text = "0 / 1";
			transcendButtonObject.SetActive(false);
			transcendEmptyObject.SetActive(true);
		}
		else if (characterData != null)
		{
		}

		_actorId = actorId;
		_contains = CharacterManager.instance.ContainsActor(actorId);
		//RefreshStatus();
		//RefreshRequired();
	}
	#endregion
	
	public void OnClickStoryButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		string desc = UIString.instance.GetString(actorTableData.descId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, desc, 200, nameText.transform, new Vector2(0.0f, -35.0f));

		// 뽑기창에서는 이와 다르게
		// Char CharDesc는 기본으로 나오고 돋보기로만 Story를 본다.
	}

	public void OnClickOrganizeButton()
	{
		if (_contains == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterDontHave"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("TeamPositionCanvas", null);
	}
}