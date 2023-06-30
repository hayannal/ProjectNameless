using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ActorStatusDefine;

public class StageFloorInfoCanvas : MonoBehaviour
{
	public static StageFloorInfoCanvas instance;

	public Text stageText;
	public GameObject repeatTextObject;
	public GameObject challengeTextObject;
	public GameObject infoButtonObject;

	public Text combatPowerText;

	void Awake()
	{
		instance = this;
	}

	public void RefreshStageInfo(int stage, bool repeat)
	{
		stageText.text = string.Format("STAGE {0:N0}", stage);
		repeatTextObject.SetActive(repeat);
		challengeTextObject.SetActive(!repeat);

		if (repeat)
			infoButtonObject.SetActive(false);
		else
		{
			// repeat이 아닐때에는 현재 스테이지 방어력 구해와서 비교해서 표시해야한다.
			float diff = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.Attack) - StageManager.instance.currentMonstrStandardDef;
			float maxHp = StageManager.instance.currentMonstrStandardHp;
			float minValue = maxHp * BattleInstanceManager.instance.GetCachedGlobalConstantInt("RepeatDamageMinValue10000") * 0.0001f;
			if (diff < minValue || diff < 1.5f)
				infoButtonObject.SetActive(true);
			else
				infoButtonObject.SetActive(false);
		}
	}

	public void RefreshCombatPower()
	{
		if (BattleInstanceManager.instance.playerActor == null)
			return;

		float value = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		combatPowerText.text = value.ToString("N0");
	}

	public void OnClickInfoButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("BossInfoDetailCanvas", null);
	}



	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = transform;
			return _transform;
		}
	}
}