﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Michsky.UI.Hexart;
using MEC;

public class SettingCanvas : MonoBehaviour
{
	public static SettingCanvas instance = null;

	public Slider systemVolumeSlider;
	public Text systemVolumeText;
	public Slider bgmVolumeSlider;
	public Text bgmVolumeText;

	public Text languageButtonText;
	public Transform accountTextTransform;
	public Text serverIdText;
	public GameObject googleImageObject;
	public GameObject appleImageObject;
	public Text accountButtonText;
	public Text accountDeleteText;
	public Slider frameRateSlider;
	public Text frameRateText;
	public Text cafeText;
	//public SwitchAnim spinAlarmSwitch;
	//public Text spinAlarmOnOffText;

	public Text termsText;
	public Text policyText;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		//_ignoreStartEvent = true;
#if UNITY_ANDROID
		googleImageObject.SetActive(true);
		appleImageObject.SetActive(false);
#elif UNITY_IOS
		googleImageObject.SetActive(false);
		appleImageObject.SetActive(true);
		// minValue를 고치는건 한번만 하면 된다.
		frameRateSlider.minValue = 5;
#endif
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);

		LoadOption();

		serverIdText.text = PlayFabApiManager.instance.playFabId;
		accountDeleteText.SetLocalizedText(UIString.instance.GetString("GameUI_DeleteAccount"));
		accountDeleteText.fontStyle = FontStyle.Italic;

		termsText.SetLocalizedText(UIString.instance.GetString("GameUI_TermsOfService"));
		termsText.fontStyle = FontStyle.Italic;
		policyText.SetLocalizedText(UIString.instance.GetString("GameUI_PrivacyPolicy"));
		policyText.fontStyle = FontStyle.Italic;

		if (OptionManager.instance.language == "KOR")
			cafeText.SetLocalizedText(UIString.instance.GetString("GameUI_OfficialCafe"));
		else
			cafeText.SetLocalizedText(UIString.instance.GetString("GameUI_OfficialTelegram"));
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (MainCanvas.instance == null)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public void OnClickHomeButton()
	{
		SaveOption();
		gameObject.SetActive(false);
	}


	#region GameOption
	void LoadOption()
	{
		systemVolumeSlider.value = OptionManager.instance.systemVolume;
		bgmVolumeSlider.value = OptionManager.instance.bgmVolume;

		LoadLanguage();
		RefreshAccount();
		frameRateSlider.value = OptionManager.instance.frame;

		//_notUserSetting = true;
		//spinAlarmSwitch.isOn = (OptionManager.instance.energyAlarm == 1);
		//_notUserSetting = false;
	}

	public void SaveOption()
	{
		OptionManager.instance.SavePlayerPrefs();
	}

	public void OnValueChangedSystem(float value)
	{
		OptionManager.instance.systemVolume = value;
		systemVolumeText.text = Mathf.RoundToInt(value * 100.0f).ToString();
	}

	public void OnValueChangedBgm(float value)
	{
		OptionManager.instance.bgmVolume = value;
		bgmVolumeText.text = Mathf.RoundToInt(value * 100.0f).ToString();
	}
	#endregion

	#region Language
	void LoadLanguage()
	{
		LanguageTableData languageTableData = UIString.instance.FindLanguageTableData(OptionManager.instance.language);
		if (languageTableData != null)
			languageButtonText.SetLocalizedText(languageTableData.languageName);
	}

	public void OnClickLanguageButton()
	{
		/*
		if (PlayerData.instance.lobbyDownloadState)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterDownload"), 2.0f);
			return;
		}
		*/

		UIInstanceManager.instance.ShowCanvasAsync("SelectLanguageCanvas", null);
	}
	#endregion

	#region Auth
	void RefreshAccount()
	{
		AuthManager.eAuthType lastAuthType = AuthManager.instance.GetLastLoginType();
		switch (lastAuthType)
		{
			case AuthManager.eAuthType.Guest:
				accountButtonText.SetLocalizedText(UIString.instance.GetString("GameUI_SignIn"));
				break;
			case AuthManager.eAuthType.Google:
			case AuthManager.eAuthType.Apple:
				accountButtonText.SetLocalizedText(UIString.instance.GetString("GameUI_LogOut"));
				break;
		}
		accountDeleteText.gameObject.SetActive(lastAuthType == AuthManager.eAuthType.Apple);
	}

	public void OnClickAccountMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, UIString.instance.GetString("GameUI_AccountMore"), 300, accountTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickGoogleButton()
	{
#if UNITY_IOS
		OnClickAppleButton();
		return;
#endif

		// 구현부 없이 그냥 호출하면 아무일 안일어나는 소셜 함수다.
		// 게다가 구글플레이서비스나 iOS 게임센터에 연결되는 형태니 Sign-in 과는 상관없으므로 사용하지 않는다.
		//Social.localUser.Authenticate((bool success) =>
		//{
		//	Debug.Log("1111");
		//});

		AuthManager.eAuthType lastAuthType = AuthManager.instance.GetLastLoginType();
		switch (lastAuthType)
		{
			case AuthManager.eAuthType.Guest:
				// 게스트 상태면 로그인을 해야한다.
				AuthManager.instance.LinkGoogleAccount(() =>
				{
					RefreshAccount();
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_SignInDone"), 2.0f);

					// 그렇지만 따로 표시는 안해도 커스텀 아이디를 삭제해주는 처리가 필요하다.
					AuthManager.instance.SetNeedUnlinkCustomId();

				}, (cancel, failure) =>
				{
					// 유저의 캔슬이라면 아무것도 띄우지 않는다.
					if (cancel)
						return;

					// 구글의 로그인이 실패하는건데 이 경우에도 아무것도 띄우지 않는다.
					if (failure == PlayFab.PlayFabErrorCode.Unknown)
						return;

					// 실패의 이유가 이미 연동시킨 구글계정이라 안되는거라면 해당 계정을 로드할지 물어봐야한다.
					// 참고로 PlayFab.PlayFabErrorCode.AccountAlreadyLinked 오류는 이미 해당 계정이 이미 구글에 링크된 상태일때 나오는 에러인데
					// 현재 스탭에서는 구글로그인 연동 상태라면 로그아웃만 가능하므로 로그인을 시도할 수 없다. 그러니 저 경우는 아예 빼두기로 한다.
					if (failure == PlayFab.PlayFabErrorCode.LinkedAccountAlreadyClaimed)
					{
						YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_SignInAlready"), () =>
						{
							// 이미 구글로그인은 되어있는 상태지만 Last AuthType는 CustomId인 상태다. 그러니 Last AuthType을 바꾸고 씬을 재시작시켜야한다.
							AuthManager.instance.RestartWithGoogle();
						}, () =>
						{
							// 아니오를 눌렀을 경우엔 게스트 상태로 남아야하므로 방금 연결하려고 했던 구글 계정에서 로그아웃만 해두면 된다.
							AuthManager.instance.LogoutWithGoogle(true);
						});
					}
				});
				break;
			case AuthManager.eAuthType.Google:
				// 구글 연동할때 CustomId 계정 연결된걸 해제하는 처리를 해야하는데
				// 혹시라도 실패했다면 계속해서 Unlink 재시도 처리를 하고있을거다.
				// 이 상황에서는 로그아웃 하면 안되므로 예외처리 하기로 한다.
				if (AuthManager.instance.needUnlinkCustomId)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_LogOutException"), 2.0f);
					return;
				}

				// 이미 연동되어있는 상태라면 확인창을 띄우고 로그아웃을 해야한다.
				string message = string.Format("{0}\n\n{1}", AuthManager.instance.GetGoogleUserId(), UIString.instance.GetString("GameUI_LogOutConfirm"));
				YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), message, () =>
				{
					AuthManager.instance.LogoutWithGoogle();
				});
				break;
		}
	}

#if UNITY_IOS
	// 아이폰에서는 애플쪽으로 처리해준다.
	void OnClickAppleButton()
	{
		AuthManager.eAuthType lastAuthType = AuthManager.instance.GetLastLoginType();
		switch (lastAuthType)
		{
			case AuthManager.eAuthType.Guest:
				AuthManager.instance.LinkAppleAccount(() =>
				{
					RefreshAccount();
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_SignInDone"), 2.0f);
					AuthManager.instance.SetNeedUnlinkCustomId();

				}, (cancel, failure) =>
				{
					if (cancel)
						return;
					if (failure == PlayFab.PlayFabErrorCode.Unknown)
						return;

					// 이상하게 아이폰에선 LinkedAccountAlreadyClaimed 대신 아래 에러로 날아온다.
					if (failure == PlayFab.PlayFabErrorCode.LinkedIdentifierAlreadyClaimed)
					{
						YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_SignInAlready"), () =>
						{
							// 이미 애플 로그인은 되어있는 상태지만 Last AuthType는 CustomId인 상태다. 그러니 Last AuthType을 바꾸고 씬을 재시작시켜야한다.
							AuthManager.instance.RestartWithApple();
						}, () =>
						{
							AuthManager.instance.LogoutWithApple(true);
						});
					}
				});
				break;
			case AuthManager.eAuthType.Apple:
				// 애플은 기기 외부에서 로그아웃 해야하므로 안내메세지만 보여준다. 꼭 이거로 해야하나. 직접 로그아웃 하는게 편하긴 한데.
				//ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_LogOutAppleToast"), 2.0f);
				//return;

				// 로그아웃 예외처리는 구글과 마찬가지로 처리
				if (AuthManager.instance.needUnlinkCustomId)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_LogOutException"), 2.0f);
					return;
				}

				// 이미 연동되어있는 상태라면 확인창을 띄우고 로그아웃을 해야한다.
				YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_LogOutConfirm"), () =>
				{
					AuthManager.instance.LogoutWithApple();
				});
				break;
		}
	}
#endif

	public void OnClickAccountDeleteButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_DeleteAccountConfirm"), () =>
		{
			PlayFabApiManager.instance.RequestDeleteAccount(false, () =>
			{
				Application.Quit();
			});
		});
	}
	#endregion

	#region Frame Rate
	public void OnValueChangedFrameRate(float value)
	{
		int intValue = Mathf.RoundToInt(value);
		OptionManager.instance.frame = intValue;
		frameRateText.text = OptionManager.instance.GetTargetFrameRateText(intValue).ToString();
	}
	#endregion

	/*
	#region System
	// Hexart Switch 특성상 OnEnable 될때마다 자동으로 호출되서 두개 이상 Reserve가 쌓이는 문제가 있어서
	// WingCanvas에서 했던거 가져와서 유저 입력이 있을때만 Reserve / Cancel 하는거로 수정해둔다.
	bool _ignoreStartEvent = false;
	bool _notUserSetting = false;
	public void OnSwitchOnSpinAlarm()
	{
		OptionManager.instance.energyAlarm = 1;
		spinAlarmOnOffText.text = "ON";
		spinAlarmOnOffText.color = Color.white;

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

#if UNITY_ANDROID
		CurrencyData.instance.ReserveEnergyNotification();
		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.SpinChargeAlarm);
#elif UNITY_IOS
		MobileNotificationWrapper.instance.CheckAuthorization(() =>
		{
			CurrencyData.instance.ReserveEnergyNotification();
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.SpinChargeAlarm);
		}, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_EnergyNotiAppleLast"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
		});
#endif
	}

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		spinAlarmSwitch.AnimateSwitch();
	}

	public void OnSwitchOffSpinAlarm()
	{
		OptionManager.instance.energyAlarm = 0;
		spinAlarmOnOffText.text = "OFF";
		spinAlarmOnOffText.color = new Color(0.176f, 0.176f, 0.176f);

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

		CurrencyData.instance.CancelEnergyNotification();
	}
	#endregion
	*/

	#region Support
	public void OnClickSupportButton()
	{
		/*
		if (PlayerData.instance.lobbyDownloadState)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterDownload"), 2.0f);
			return;
		}
		*/

		UIInstanceManager.instance.ShowCanvasAsync("SupportListCanvas", null);
	}
	#endregion

	#region Cafe
	public void OnClickCafeButton()
	{
		if (OptionManager.instance.language == "KOR")
			Application.OpenURL(BattleInstanceManager.instance.GetCachedGlobalConstantString("OfficialCafe"));
		else
			Application.OpenURL(BattleInstanceManager.instance.GetCachedGlobalConstantString("OfficialTelegram"));
	}
	#endregion

	#region Terms
	public void OnClickTermsTextButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("TermsCanvas", () =>
		{
			TermsCanvas.instance.RefreshInfo(true);
		});
	}

	public void OnClickPolicyTextButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("TermsCanvas", () =>
		{
			TermsCanvas.instance.RefreshInfo(false);
		});
	}
	#endregion
}