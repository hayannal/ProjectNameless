using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellCanvasListItem : MonoBehaviour
{
	public Transform iconPrefabRootTransform;
	public Text levelText;
	public Text nameText;
	public Transform tooltipRootTransform;
	public Text atkText;
	public Text[] noGainGrayTextList;
	public Image[] noGainGrayImageList;
	public Slider proceedingCountSlider;
	public Text proceedingCountText;
	public RectTransform alarmRootTransform;

	public void Initialize(SkillProcessor.SkillInfo skillInfo, SkillTableData skillTableData, SkillLevelTableData skillLevelTableData)
	{
		RefreshInfo(skillInfo.iconPrefab, skillInfo.nameId, skillInfo.descriptionId, skillLevelTableData);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = Color.white;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = Color.white;
	}

	public void InitializeForNoGain(SkillTableData skillTableData, SkillLevelTableData skillLevelTableData)
	{
		RefreshInfo(skillTableData.iconPrefab,
			skillTableData.useNameIdOverriding ? skillLevelTableData.nameId : skillTableData.nameId,
			skillTableData.useDescriptionIdOverriding ? skillLevelTableData.descriptionId : skillTableData.descriptionId,
			skillLevelTableData);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = Color.gray;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = Color.gray;

		proceedingCountSlider.value = 0.0f;
	}

	GameObject _cachedImageObject;
	void RefreshInfo(string iconPrefabAddress, string nameId, string descriptionId, SkillLevelTableData skillLevelTableData)
	{

		levelText.text = UIString.instance.GetString("GameUI_LevelPackLv", skillLevelTableData.level);
		nameText.SetLocalizedText(UIString.instance.GetString(nameId));
		_descString = UIString.instance.GetString(descriptionId, skillLevelTableData.parameter);

		if (_cachedImageObject != null)
		{
			_cachedImageObject.SetActive(false);
			_cachedImageObject = null;
		}

		if (string.IsNullOrEmpty(iconPrefabAddress) == false)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(iconPrefabAddress, "Preview", (prefab) =>
			{
				_cachedImageObject = UIInstanceManager.instance.GetCachedObject(prefab, iconPrefabRootTransform);
			});
		}
	}

	string _descString = "";
	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, _descString, 200, tooltipRootTransform, new Vector2(0.0f, 0.0f));
	}

	public void OnClickLevelUpButton()
	{
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