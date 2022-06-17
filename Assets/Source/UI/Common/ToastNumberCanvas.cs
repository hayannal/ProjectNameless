using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToastNumberCanvas : MonoBehaviour
{
	public static ToastNumberCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.toastNumberCanvasPrefab).GetComponent<ToastNumberCanvas>();
			}
			return _instance;
		}
	}
	static ToastNumberCanvas _instance = null;

	public CanvasGroup canvasGroup;
	public Text toastText;

	public void ShowToast(string text, float remainTime, float maxAlpha = 0.8f)
	{
		toastText.text = text;
		_showRemainTime = remainTime;
		_maxAlpha = maxAlpha;

		gameObject.SetActive(false);
		gameObject.SetActive(true);
	}

	float _maxAlpha;
	float _showRemainTime;
	void Update()
	{
		if (_showRemainTime > 0.0f)
		{
			_showRemainTime -= Time.unscaledDeltaTime;
			canvasGroup.alpha = Mathf.Min(_maxAlpha, _showRemainTime * 2.0f);
			if (_showRemainTime <= 0.0f)
			{
				_showRemainTime = 0.0f;
				gameObject.SetActive(false);
			}
		}
	}
}
