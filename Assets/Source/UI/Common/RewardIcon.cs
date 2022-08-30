using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardIcon : MonoBehaviour
{
	public string eventRewardId;
	public int num;

	public Text countText;
	public GameObject goldObject;
	public GameObject energyObject;

	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image lineColorImage;

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
					case "GO": goldObject.SetActive(true); break;
					case "EN": energyObject.SetActive(true); break;
				}
				break;
			case "it":
				break;
		}
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