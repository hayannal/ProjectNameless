using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToastZigzagCanvas : MonoBehaviour
{
	public static ToastZigzagCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.toastZigzagCanvasPrefab).GetComponent<ToastZigzagCanvas>();
			}
			return _instance;
		}
	}
	static ToastZigzagCanvas _instance = null;

	public CanvasGroup canvasGroup;
	public RectTransform defaultTransform;
	public RectTransform middleTransform;
	public RectTransform rootTransform;
	public Text toastText;

	public void ShowToast(string text, float remainTime, float maxAlpha = 0.8f, bool useMiddlePosition = false)
	{
		toastText.SetLocalizedText(string.Format("<i>{0}</i>", text));
		_showRemainTime = remainTime;
		_maxAlpha = maxAlpha;

		rootTransform.anchoredPosition = new Vector2(rootTransform.anchoredPosition.x, useMiddlePosition ? middleTransform.anchoredPosition.y : defaultTransform.anchoredPosition.y);

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