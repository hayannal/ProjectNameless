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
		SetValue(CurrencyData.instance.goldBoxTargetReward);
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
	public void SetValue(int targetValue)
	{
		targetValueText.text = targetValue.ToString("N0");
		_needAdjustRect = true;
		_setOneMoreFrame = true;
	}
}