using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RewardIcon : MonoBehaviour
{
	public string eventRewardId;
	public int num;

	public Text countText;
	public GameObject goldObject;
	public GameObject energyObject;
	public GameObject sevenDaysObject;

	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image lineColorImage;

	public GameObject[] frameObjectList;
	public Transform iconRootTransform;
	public DOTweenAnimation punchTweenAnimation;

	void OnEnable()
	{
		if (eventRewardId == "")
			return;

		EventRewardTableData eventRewardTableData = TableDataManager.instance.FindEventRewardTableData(eventRewardId, num);
		if (eventRewardTableData == null)
			return;

		RefreshReward(eventRewardTableData.rewardType, eventRewardTableData.rewardValue, eventRewardTableData.rewardCount);
	}

	public void RefreshReward(string rewardType, string rewardValue, int rewardCount)
	{
		goldObject.SetActive(false);
		energyObject.SetActive(false);
		sevenDaysObject.SetActive(false);
		countText.text = rewardCount.ToString("N0");
		switch (rewardType)
		{
			case "cu":
				if (blurImage != null) blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				if (gradient != null) gradient.color1 = Color.white;
				if (gradient != null) gradient.color2 = Color.black;
				if (lineColorImage != null) lineColorImage.color = new Color(0.5f, 0.5f, 0.5f);
				switch (rewardValue)
				{
					case "GO":
						goldObject.SetActive(true);
						countText.color = _showOnlyIcon ? MailCanvasListItem.GetGoldTextColor() : Color.white;
						break;
					case "EN":
						energyObject.SetActive(true);
						countText.color = _showOnlyIcon ? MailCanvasListItem.GetEnergyTextColor() : Color.white;
						break;
				}
				break;
			case "it":
				switch (rewardValue)
				{
					case "Cash_sEv8SevenSumPoint":
						sevenDaysObject.SetActive(true);
						countText.color = Color.white;
						break;
				}
				break;
		}
	}

	bool _showOnlyIcon = false;
	public void ShowOnlyIcon(bool onlyIcon, float onlyIconScale = 1.5f)
	{
		_showOnlyIcon = onlyIcon;
		for (int i = 0; i < frameObjectList.Length; ++i)
			frameObjectList[i].SetActive(!onlyIcon);

		iconRootTransform.localScale = onlyIcon ? new Vector3(onlyIconScale, onlyIconScale, onlyIconScale) : Vector3.one;
	}

	public void ActivePunchAnimation(bool active)
	{
		if (active)
			punchTweenAnimation.DORestart();
		else
			punchTweenAnimation.DOPause();
	}


	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}