using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PetHeartConfirmCanvas : MonoBehaviour
{
	public static PetHeartConfirmCanvas instance;

	public Text countText;
	public Image minusButtonImage;
	public Image plusButtonImage;
	public Text maxText;

	void Awake()
	{
		instance = this;
	}

	int _baseCount = 1;
	int _maxCount = 0;
	PetData _petData;
	public void RefreshInfo(PetData petData, int maxCount)
	{
		_petData = petData;
		_baseCount = _maxCount = maxCount;
		RefreshCount();
	}

	public void OnClickMinusButton()
	{
		if (_baseCount > 1)
		{
			_baseCount -= 1;
			RefreshCount();
		}
	}

	public void OnClickPlusButton()
	{
		if (_baseCount < _maxCount)
		{
			_baseCount += 1;
			RefreshCount();
		}
	}

	public void OnClickMaxButton()
	{
		if (_baseCount != _maxCount)
		{
			_baseCount = _maxCount;
			RefreshCount();
		}
	}

	void RefreshCount()
	{
		countText.text = _baseCount.ToString("N0");
		minusButtonImage.color = (_baseCount == 1) ? Color.gray : Color.white;
		plusButtonImage.color = (_baseCount == _maxCount) ? Color.gray : Color.white;
		maxText.color = (_baseCount == _maxCount) ? Color.gray : Color.white;
	}

	public void OnClickHeartButton()
	{
		float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestHeartPet(_petData, _petData.heart + _baseCount, () =>
		{
			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			// 변경 완료를 알리고
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(prevValue, nextValue);
			});

			gameObject.SetActive(false);
			if (PetInfoCanvas.instance != null)
				PetInfoCanvas.instance.OnRecvHeartPet();
		});
	}
}