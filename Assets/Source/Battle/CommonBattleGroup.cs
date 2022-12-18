using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonBattleGroup : MonoBehaviour
{
	public static CommonBattleGroup instance = null;

	// essential
	public GameObject targetCircleObject;
	public GameObject targetCircleSleepObject;
	public GameObject monsterDieAshParticlePrefab;
	public AnimationCurveAsset monsterDieDissolveCurve;
	public AnimationCurveAsset bossMonsterDieDissolveCurve;
	public GameObject rangeIndicatorPrefab;
	public GameObject battleToastCanvasPrefab;

	// gauge
	public GameObject monsterHPGaugeRootCanvasPrefab;
	public GameObject monsterHPGaugePrefab;
	public GameObject bossMonsterHPGaugePrefab;
	public GameObject playerHPGaugePrefab;
	public GameObject playerIgnoreEvadeCanvasPrefab;

	// damage
	public GameObject damageCanvasPrefab;
	public GameObject floatingDamageTextRootCanvasPrefab;



	void Awake()
	{
		instance = this;
	}
}