using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaIndicatorCanvas : ObjectIndicatorCanvas
{
	public static GachaIndicatorCanvas instance;

	public GameObject infoRootObject;
	public Transform goldBoxTargetRootTransform;
	public Text targetValueText;
	public Text gradeText;
	public Coffee.UIExtensions.UIGradient gradeGradient;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
		SetValue(CurrencyData.instance.goldBoxTargetReward, CurrencyData.instance.goldBoxTargetGrade);
	}

	void OnDisable()
	{
		infoRootObject.SetActive(false);
	}

	// Update is called once per frame
	bool _setOneMoreFrame = false;
	void Update()
	{
		UpdateObjectIndicator();

		if (_needAdjustRect && infoRootObject.activeSelf)
		{
			goldBoxTargetRootTransform.gameObject.SetActive(false);
			goldBoxTargetRootTransform.gameObject.SetActive(true);

			if (_setOneMoreFrame)
			{
				_setOneMoreFrame = false;
				return;
			}
			_needAdjustRect = false;
		}
	}

	bool _needAdjustRect = false;
	public void SetValue(int targetValue, int grade)
	{
		targetValueText.text = targetValue.ToString("N0");
		RefreshGrade(grade);
		_needAdjustRect = true;
		_setOneMoreFrame = true;
	}

	void RefreshGrade(int grade)
	{
		switch (grade)
		{
			case 0:
				gradeText.text = "C";
				gradeGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				gradeGradient.color1 = new Color(1.0f, 1.0f, 1.0f);
				gradeGradient.color2 = new Color(0.44f, 0.44f, 0.44f);
				gradeGradient.rotation = 155.0f;
				gradeGradient.offset = 0.15f;
				break;
			case 1:
				gradeText.text = "B";
				gradeGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				gradeGradient.color1 = new Color(0.81f, 1.0f, 0.89f);
				gradeGradient.color2 = new Color(0.42f, 1.0f, 0.45f);
				gradeGradient.rotation = 155.0f;
				gradeGradient.offset = -0.02f;
				break;

			// Equip의 Rarity에서 그대로 가져와서 쓴다.
			case 2:
				gradeText.text = "A";
				gradeGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				gradeGradient.color1 = new Color(0.81f, 0.92f, 1.0f);
				gradeGradient.color2 = new Color(0.52f, 0.53f, 1.0f);
				gradeGradient.rotation = 155.0f;
				gradeGradient.offset = -0.19f;
				break;
			case 3:
				gradeText.text = "S";
				gradeGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				gradeGradient.color1 = new Color(1.0f, 0.45f, 0.5f);
				gradeGradient.color2 = new Color(1.0f, 1.0f, 0.48f);
				gradeGradient.rotation = 155.0f;
				gradeGradient.offset = -0.19f;
				break;
			case 4:
				gradeText.text = "SS";
				gradeGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				gradeGradient.color1 = new Color(1.0f, 0.45f, 0.5f);
				gradeGradient.color2 = new Color(1.0f, 1.0f, 0.0f);
				gradeGradient.rotation = 155.0f;
				gradeGradient.offset = 0.22f;
				break;
		}
	}
}