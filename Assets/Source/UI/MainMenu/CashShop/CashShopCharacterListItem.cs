using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class CashShopCharacterListItem : MonoBehaviour
{
	public int index = 0;

	const float moreRate = 1.5f;

	public GameObject moreTextObject;
	public Text countText;
	public Text prevCountText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;

	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	ObscuredInt _resultCount;
	ShopActorTableData _shopActorTableData;
	void OnEnable()
	{
		//bool eventApplied = CashShopData.instance.IsShowEvent("ev10");
		bool eventApplied = false;

		ShopActorTableData shopActorTableData = TableDataManager.instance.FindShopActorTableDataByIndex(index);
		if (shopActorTableData == null)
			return;

		if (eventApplied)
		{
			// 아직 이벤트가 정해져있지 않아서 기존 코드를 냅두고 else부분만 짜도록 한다.
			moreTextObject.SetActive(true);
			prevCountText.text = string.Format("X {0:N0}", shopActorTableData.count);
			prevCountText.gameObject.SetActive(false);
			prevCountText.gameObject.SetActive(true);
			_resultCount = (int)(shopActorTableData.count * moreRate);
			countText.text = string.Format("X {0:N0}", _resultCount);
			priceText.text = shopActorTableData.price.ToString("N0");
			RefreshLineImage();
			_updateRefreshLineImageCount = 3;
			_shopActorTableData = shopActorTableData;
		}
		else
		{
			moreTextObject.SetActive(false);
			prevCountText.gameObject.SetActive(false);
			_resultCount = shopActorTableData.count;
			countText.text = string.Format("X {0:N0}", _resultCount);
			priceText.text = shopActorTableData.price.ToString("N0");
			_shopActorTableData = shopActorTableData;
		}
	}

	void RefreshLineImage()
	{
		Vector3 diff = rightTopRectTransform.position - lineImageRectTransform.position;
		lineImageRectTransform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg);
		if (CashShopCanvas.instance != null)
			lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude * CashShopCanvas.instance.lineLengthRatio);
	}

	int _updateRefreshLineImageCount;
	void Update()
	{
		if (_updateRefreshLineImageCount > 0)
		{
			RefreshLineImage();
			--_updateRefreshLineImageCount;
		}
	}

	public void OnClickInfoButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("GachaCharacterInfoCanvas", null);
	}

	int _count;
	public void OnClickButton()
	{
		if (CurrencyData.instance.gold < _shopActorTableData.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		// 연출 및 보상 처리. 100개씩 뽑으면 느릴 수 있으니 패킷 대기 없이 바로 시작한다.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 연출창 시작과 동시에 패킷을 보내고
			//List<ObscuredString> listActorId = CharacterManager.instance.GetRandomIdList(_resultCount);
			//_count = listActorId.Count;
			//PlayFabApiManager.instance.RequestOpenCharacterBox(listActorId, _shopActorTableData.count, _shopActorTableData.price, moreTextObject.activeSelf, OnRecvResult);
		});
	}

	void OnRecvResult(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		//List<ItemInstance> listItemInstance = CharacterManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		//if (listItemInstance == null)
		//	return;

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		//if (RandomBoxScreenCanvas.instance != null)
		//	RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Character, listItemInstance);
	}
}