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

	// spawn
	public GameObject playerSpawnEffectPrefab;

	// reserve
	public GameObject playerLevelUpEffectPrefab;
	public GameObject levelPackGainEffectPrefab;
	public GameObject healEffectPrefab;

	// gauge
	public GameObject monsterHPGaugeRootCanvasPrefab;
	public GameObject monsterHPGaugePrefab;
	public GameObject bossMonsterHPGaugePrefab;
	public GameObject playerHPGaugePrefab;
	public GameObject playerIgnoreEvadeCanvasPrefab;

	// skill
	public GameObject skillSlotCanvasPrefab;
	public GameObject ultimateCirclePrefab;
	public GameObject battleToastCanvasPrefab;

	// damage
	public GameObject damageCanvasPrefab;
	public GameObject floatingDamageTextRootCanvasPrefab;
	public GameObject pauseCanvasPrefab;



	void Awake()
	{
		instance = this;
	}
}