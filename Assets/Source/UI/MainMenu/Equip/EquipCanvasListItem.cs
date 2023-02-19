using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipCanvasListItem : MonoBehaviour
{
	public RectTransform contentRectTransform;
	public Image equipIconImage;
	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image lineColorImage;
	public Text gradeText;
	public Image enhanceBackgroundImage;
	public Text enhanceLevelText;
	public Text rarityText;
	public Coffee.UIExtensions.UIGradient rarityGradient;
	public GameObject[] optionObjectList;
	public Image lockImage;
	public Text equippedText;
	public GameObject selectObject;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	public EquipData equipData { get; set; }
	public void Initialize(EquipData equipData, Action<EquipData> clickCallback)
	{
		this.equipData = equipData;
		Initialize(equipData.cachedEquipTableData);
		RefreshEnhanceLevel(equipData.enhanceLevel);
		_clickAction = clickCallback;
	}

	public void Initialize(EquipTableData equipTableData, int enhanceLevel = 0)
	{
		AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});

		InitializeGrade(equipTableData.grade, false);
		RefreshRarity(equipTableData.rarity, rarityText, rarityGradient);
		RefreshEnhanceLevel(enhanceLevel);
		RefreshStatus();

		for (int i = 0; i < optionObjectList.Length; ++i)
			optionObjectList[i].SetActive(false);

		equippedText.gameObject.SetActive(false);
		selectObject.SetActive(false);
		blackObject.SetActive(false);
	}

	public void InitializeGrade(int grade, bool questionEquip = false)
	{
		RefreshGrade(grade, blurImage, gradient, lineColorImage);

		if (questionEquip == false)
			return;

		// 등급을 표시
		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", grade)));
		gradeText.gameObject.SetActive(true);
		//levelObject.SetActive(false);
		//recommandedText.gameObject.SetActive(false);

		AddressableAssetLoadManager.GetAddressableSprite("Shot_NoEquip", "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});
	}

	public static void RefreshGrade(int grade, Image blurImage, Coffee.UIExtensions.UIGradient gradient, Image lineColorImage)
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
				blurImage.color = new Color(0.28f, 1.0f, 0.53f, 0.0f);
				gradient.color1 = new Color(0.0f, 1.0f, 0.3f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.1f, 0.84f, 0.1f);
				break;
			case 2:
				blurImage.color = new Color(0.28f, 0.78f, 1.0f, 0.0f);
				gradient.color1 = new Color(0.0f, 0.7f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 3:
				blurImage.color = new Color(0.73f, 0.31f, 1.0f, 0.0f);
				gradient.color1 = new Color(0.66f, 0.0f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.63f, 0.0f, 1.0f);
				break;
			case 4:
				blurImage.color = new Color(1.0f, 0.78f, 0.31f, 0.0f);
				gradient.color1 = new Color(1.0f, 0.5f, 0.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
			case 5:
				blurImage.color = new Color(1.0f, 0.27f, 0.31f, 0.0f);
				gradient.color1 = new Color(1.0f, 0.05f, 0.0f);
				gradient.color2 = new Color(0.91f, 0.68f, 0.68f);
				lineColorImage.color = new Color(0.85f, 0.15f, 0.06f);
				break;
			case 6:
				blurImage.color = new Color(1.0f, 1.0f, 0.0f, 0.2f);
				gradient.color1 = new Color(1.0f, 1.0f, 0.0f);
				gradient.color2 = new Color(0.93f, 0.56f, 0.12f);
				lineColorImage.color = new Color(0.93f, 0.93f, 0.29f);
				break;
		}
	}

	public static void RefreshRarity(int rarity, Text rarityText, Coffee.UIExtensions.UIGradient rarityGradient)
	{
		rarityText.font = UIString.instance.GetUnlocalizedFont();
		rarityText.fontStyle = UIString.instance.useSystemUnlocalizedFont ? FontStyle.BoldAndItalic : FontStyle.Italic;

		switch (rarity)
		{
			case 0:
				rarityText.text = "A";
				rarityGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				rarityGradient.color1 = new Color(0.81f, 0.92f, 1.0f);
				rarityGradient.color2 = new Color(0.52f, 0.53f, 1.0f);
				rarityGradient.rotation = 155.0f;
				rarityGradient.offset = -0.19f;
				break;
			case 1:
				rarityText.text = "S";
				rarityGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				rarityGradient.color1 = new Color(1.0f, 0.45f, 0.5f);
				rarityGradient.color2 = new Color(1.0f, 1.0f, 0.48f);
				rarityGradient.rotation = 155.0f;
				rarityGradient.offset = -0.19f;
				break;
			case 2:
				rarityText.text = "SS";
				rarityGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				rarityGradient.color1 = new Color(1.0f, 0.45f, 0.5f);
				rarityGradient.color2 = new Color(1.0f, 1.0f, 0.0f);
				rarityGradient.rotation = 155.0f;
				rarityGradient.offset = 0.22f;
				break;
		}
	}

	// 변할 수 있는 정보들만 따로 빼둔다.
	public void RefreshStatus()
	{
		if (equipData == null)
		{
			lockImage.gameObject.SetActive(false);
			return;
		}

		// isLock
		lockImage.gameObject.SetActive(equipData.isLock);
	}

	public void RefreshEnhanceLevel(int enhanceLevel)
	{
		enhanceBackgroundImage.gameObject.SetActive(enhanceLevel > 0);
		enhanceBackgroundImage.color = lineColorImage.color;
		enhanceLevelText.text = enhanceLevel.ToString();
	}

	Action<EquipData> _clickAction;
	public void OnClickButton()
	{
		if (_clickAction == null)
			return;

		_clickAction.Invoke(equipData);
		SoundManager.instance.PlaySFX(selectObject.activeSelf ? "GridOff" : "GridOn");
	}

	public void ShowSelectObject(bool show)
	{
		selectObject.SetActive(show);
	}

	void Update()
	{
		UpdateSelectPosition();
	}

	void UpdateSelectPosition()
	{
		Vector2 selectOffset = new Vector2(-11.0f, 7.0f);
		if (selectObject.activeSelf)
		{
			if (contentRectTransform.anchoredPosition != selectOffset)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, selectOffset, Time.deltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition - selectOffset;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = selectOffset;
			}
		}
		else
		{
			if (contentRectTransform.anchoredPosition != Vector2.zero)
			{
				contentRectTransform.anchoredPosition = Vector2.Lerp(contentRectTransform.anchoredPosition, Vector2.zero, Time.deltaTime * 15.0f);
				Vector2 diff = contentRectTransform.anchoredPosition;
				if (diff.sqrMagnitude < 0.001f)
					contentRectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}


	#region Alarm
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