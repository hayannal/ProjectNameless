using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FloatingDamageText : MonoBehaviour
{
	public enum eFloatingDamageType
	{
		Miss,
		Invincible,
		Headshot,
		Immortal,
		ReduceContinuousDamage,
		DefenseStrongDamage,
		PaybackSp,
		HealSpOnAttack,
		Critical,
		MaxHpIncrease,
		Knockback,
	}

	public Text damageText;
	public Text localizedText;
	public Image strikeImage;
	public Transform positionAnimationTransform;
	public DOTweenAnimation alphaTweenAnimation;
	public CanvasGroup alphaCanvasGroup;

	public void InitializeText(float damage, bool critical, bool strike, Actor actor, int index)
	{
		int intDamage = (int)damage;

		damageText.font = UIString.instance.GetUnlocalizedFont();
		damageText.fontStyle = UIString.instance.useSystemUnlocalizedFont ? FontStyle.Bold : FontStyle.Normal;
		damageText.text = intDamage.ToString("N0");

		if (strike)
		{
			damageText.color = new Color(1.0f, 0.0f, 0.5f);
			damageText.gameObject.SetActive(true);
			localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_Strike"));
			localizedText.color = Color.white;
			localizedText.gameObject.SetActive(true);
			strikeImage.gameObject.SetActive(true);
			InitializeText(actor, index);
			return;
		}

		damageText.color = critical ? Color.red : Color.white;
		damageText.gameObject.SetActive(true);
		localizedText.gameObject.SetActive(false);
		strikeImage.gameObject.SetActive(false);
		InitializeText(actor, index);
	}

	public void InitializeText(eFloatingDamageType floatingDamageType, Actor actor, int index)
	{
		localizedText.color = Color.white;
		switch (floatingDamageType)
		{
			case eFloatingDamageType.Miss:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_Miss"));
				break;
			case eFloatingDamageType.Invincible:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_Invincible"));
				break;
			case eFloatingDamageType.Headshot:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_Headshot"));
				break;
			case eFloatingDamageType.Immortal:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_ImmortalWill"));
				break;
			case eFloatingDamageType.ReduceContinuousDamage:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_ReduceContinuousDmg"));
				break;
			case eFloatingDamageType.DefenseStrongDamage:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_DefenseStrongDmg"));
				break;
			case eFloatingDamageType.PaybackSp:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_PaybackSp"));
				break;
			case eFloatingDamageType.HealSpOnAttack:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_HealSp"));
				break;
			case eFloatingDamageType.Critical:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_Critical"));
				break;
			case eFloatingDamageType.MaxHpIncrease:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_MaxHpIncrease"));
				break;
			case eFloatingDamageType.Knockback:
				localizedText.SetLocalizedText(UIString.instance.GetString("GameUI_Knockback"));
				localizedText.color = new Color(0.0f, 0.75f, 1.0f);
				break;
		}
		localizedText.gameObject.SetActive(true);
		damageText.gameObject.SetActive(false);
		strikeImage.gameObject.SetActive(false);
		InitializeText(actor, index);
	}

	void InitializeText(Actor actor, int index)
	{
		alphaCanvasGroup.alpha = 1.0f;

		_offsetY = actor.gaugeOffsetY;
		_targetTransform = actor.cachedTransform;
		_targetHeight = ColliderUtil.GetHeight(actor.GetCollider());
		UpdateGaugePosition();

		//float rotateY = cachedTransform.position.x * 2.0f;
		//cachedTransform.rotation = Quaternion.Euler(0.0f, rotateY, 0.0f);

		// position ani
		positionAnimationTransform.localPosition = Vector3.zero;
		_targetPosition = FloatingDamageTextRootCanvas.instance.positionAnimationTargetList[index];
		_firstPositionAniRemainTime = FirstPositionAniDuration;
	}

	// Update is called once per frame
	Vector3 _prevTargetPosition = -Vector3.up;
	void Update()
	{
		if (_targetTransform != null)
		{
			if (_targetTransform.position != _prevTargetPosition)
			{
				UpdateGaugePosition();
				_prevTargetPosition = _targetTransform.position;
			}
		}

		UpdateFirstPositionAni();
	}

	Transform _targetTransform;
	float _targetHeight;
	float _offsetY;
	void UpdateGaugePosition()
	{
		Vector3 desiredPosition = _targetTransform.position;
		desiredPosition.y += _targetHeight;
		desiredPosition.y += _offsetY;
		cachedTransform.position = desiredPosition;
	}

	Vector3 _targetPosition;
	const float FirstPositionAniDuration = 1.0f;
	float _firstPositionAniRemainTime = 0.0f;
	void UpdateFirstPositionAni()
	{
		positionAnimationTransform.localPosition = Vector3.Lerp(positionAnimationTransform.localPosition, _targetPosition, Time.deltaTime * 10.0f);

		if (_firstPositionAniRemainTime > 0.0f)
		{
			_firstPositionAniRemainTime -= Time.deltaTime;
			if (_firstPositionAniRemainTime <= 0.0f)
			{
				_firstPositionAniRemainTime = 0.0f;
				alphaTweenAnimation.DORestart();
			}
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