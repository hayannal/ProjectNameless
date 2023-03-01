using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public UnityEvent onPressInitialize;
	public UnityEvent onPress;
	public UnityEvent onComplete;

	void OnDisable()
	{
		// 만약 프레스 도중에 버튼이 사라진다면 
		if (_pointerDown)
		{
			// 강제로 릴리즈 처리를 해준다.
			OnPointerUp(null);
		}
	}

	float _executeRemainTime = 0.0f;
	void Update()
    {
        if (_executeRemainTime > 0.0f)
		{
			_executeRemainTime -= Time.deltaTime;
			if (_executeRemainTime <= 0.0f)
			{
				_executeRemainTime = 0.0f;
				
				if (onPress != null)
					onPress.Invoke();
				_sumCount += 1;
				if (_sumCount == 1)
				{
					// 최초 1회 실행 후에는 가장 큰 딜레이를 준다.
					_executeRemainTime = 0.5f;
				}
				else if (_sumCount > 20)
				{
					_executeRemainTime = 0.025f;
				}
				else if (_sumCount > 1)
				{
					_executeRemainTime = 0.1f;
				}
			}
		}
    }

	bool _pointerDown = false;
	int _sumCount = 0;
	public void OnPointerDown(PointerEventData eventData)
	{
		_pointerDown = true;

		//Debug.Log("Pointer Down!");

		if (_sumCount == 0)
		{
			if (onPressInitialize != null)
				onPressInitialize.Invoke();

			_executeRemainTime = 0.1f;
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		//Debug.Log("Pointer Up!");

		if (_sumCount == 0)
		{
			// 한번도 실행이 안된 상태로 컴플릿만 호출하는건 이상하기 때문에 강제로 호출하기로 한다.
			if (onPress != null)
				onPress.Invoke();
		}

		// 컴플릿 호출
		if (onComplete != null)
			onComplete.Invoke();

		// reset
		_sumCount = 0;
		_executeRemainTime = 0.0f;
		_pointerDown = false;
	}
}