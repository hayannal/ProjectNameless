using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;

public class BossStageNumberCanvas : MonoBehaviour
{
	public static BossStageNumberCanvas instance;

	public RectTransform nodeTransform;
	public Text stageText;

	public float nodeLeftValue = -120.0f;
	public float nodeRightValue = 120.0f;

	void Awake()
	{
		instance = this;
	}

	int _stage;
	void OnEnable()
	{
		nodeTransform.anchoredPosition = Vector2.zero;
		_stage = StageManager.instance.currentFloor;
		stageText.text = _stage.ToString("N0");
	}

	int _diff;
	public void SetNextStage(int stage)
	{
		_diff = stage - _stage;
		if (_diff == 0)
			return;

		Timing.RunCoroutine(MoveProcess());
	}
	
	float _halfTime;
	IEnumerator<float> MoveProcess()
	{
		float baseDuration = 0.3f;
		switch (_diff)
		{
			case 1:
			case 2:
			case 3:
				baseDuration = 0.3f;
				break;
			case 4:
			case 5:
			case 6:
				baseDuration = 0.375f;
				break;
			case 7:
			case 8:
			case 9:
			case 10:
				baseDuration = 0.45f;
				break;
		}


		float duration = baseDuration / _diff;
		int remainLoopCount = _diff - 1;

		// 항상 최초는 중앙 상태에서 왼쪽으로 보내는거다.
		nodeTransform.DOAnchorPosX(nodeLeftValue, duration).SetEase(Ease.Linear);
		yield return Timing.WaitForSeconds(duration);

		nodeTransform.anchoredPosition = new Vector2(nodeRightValue, nodeTransform.anchoredPosition.y);
		_stage += 1;
		stageText.text = _stage.ToString("N0");

		while (remainLoopCount > 0)
		{
			remainLoopCount -= 1;
			nodeTransform.DOAnchorPosX(nodeLeftValue, duration * 2.0f).SetEase(Ease.Linear);
			yield return Timing.WaitForSeconds(duration * 2.0f);

			nodeTransform.anchoredPosition = new Vector2(nodeRightValue, nodeTransform.anchoredPosition.y);
			_stage += 1;
			stageText.text = _stage.ToString("N0");
		}

		yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		// 이렇게까지 했는데 설마 왼쪽에 있진 않겠지. 간혹 버그 나는 경우가 있는거 같아서 예외처리 한번 더 넣어둔다.
		if (nodeTransform.anchoredPosition.x < 0.0f)
		{
			nodeTransform.anchoredPosition = new Vector2(nodeRightValue, nodeTransform.anchoredPosition.y);
			yield return Timing.WaitForOneFrame;
		}

		// 마지막은 중앙으로 되돌아오기.
		nodeTransform.DOAnchorPosX(0.0f, duration).SetEase(Ease.Linear);
		yield return Timing.WaitForSeconds(duration);
	}
}