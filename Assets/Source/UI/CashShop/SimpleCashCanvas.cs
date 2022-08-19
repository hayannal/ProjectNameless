using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class SimpleCashCanvas : MonoBehaviour
{
	public Text priceText;
	public Button iapBridgeButton;

	public void RefreshPrice(string serverItemId, int kor, float eng)
	{
		if (priceText == null)
			return;

		Product product = CodelessIAPStoreListener.Instance.GetProduct(serverItemId);
		if (product != null && product.metadata != null && product.metadata.localizedPrice > 0)
			priceText.text = product.metadata.localizedPriceString;
		else
		{
			if (Application.systemLanguage == SystemLanguage.Korean)
				priceText.text = string.Format("{0}{1:N0}", BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"), kor);
			else
				priceText.text = string.Format("$ {0:0.##}", eng);
		}
	}

	public void OnClickButton()
	{
		// 실제 구매
		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
	}

	public void OnResponse()
	{
		// 인풋 차단
		WaitingNetworkCanvas.Show(true);

		// 멀리 숨겨둔 IAP 버튼을 호출해서 결제 진행
		iapBridgeButton.onClick.Invoke();
	}

	public void OnPurchaseComplete(Product product)
	{
		RequestServerPacket(product);
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
	{
		WaitingNetworkCanvas.Show(false);

		if (reason == PurchaseFailureReason.UserCancelled)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_UserCancel"), 2.0f);
		else if (reason == PurchaseFailureReason.DuplicateTransaction)
		{
			// 미처리된 상품이 있는걸 감지하고 캐시샵에 들어오면 복구할거냐는 창을 띄우는데
			// 이때 No를 누르고 직접 구매했던 상품을 눌러서 구글결제 코드를 작동시키면 이미 구입한 상품이라는 오류 메세지를 보여주고 이걸 닫으면
			// OnPurchaseFailed 를 PurchaseFailureReason.DuplicateTransaction 인자와 호출함과 동시에
			// 곧바로 OnPurchaseComplete 함수도 호출해서 어떤 상품을 구매했었는지 보내온다.
			// 즉 Failed함수와 Complete함수가 동시에 실행되는 것.
			// 예전 IAP 버전초기때는 이 Failed함수만 호출되었던거 같은데 이렇게 Complete도 오다보니 굳이 여기서 예외처리를 할 필요가 없게 되었다
			//
			// IAP 버전을 3.0.3으로 올리고나서 테스트해보니 다시 예전처럼 Failed함수만 호출된다.
			// 버전이 바뀌면서 정책이 바뀐듯 하여 직접 컴플릿 된거처럼 처리하기로 한다.
			WaitingNetworkCanvas.Show(true);
			RequestServerPacket(product);
		}
		else
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_PurchaseFailure"), 2.0f);
			Debug.LogFormat("PurchaseFailed reason {0}", reason.ToString());
		}
	}

	protected virtual void RequestServerPacket(Product product)
	{

	}

	public void RetryPurchase(Product product)
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", product.metadata.localizedTitle), () =>
		{
			WaitingNetworkCanvas.Show(true);
			RequestServerPacket(product);
		}, () =>
		{
		}, true);
	}
}
