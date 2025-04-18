#define PLAYFAB				// 싱글버전으로 돌아가는 디파인이다. 테스트용을 위해 남겨둔다.
//#define NEWPLAYER_LEVEL1	// 실제 튜토리얼 들어갈때 무조건 없애야하는 디파인이다. 1레벨 임시 캐릭 생성용 버전.
//#define NEWPLAYER_ADD_KEEP	// 사실 킵이 있는게 1챕터의 시작이라 위 LEVEL1가지고는 정상적인 흐름대로 진행하기가 어렵다. 위와 마찬가지로 지워야한다. 지울때 꼭!! 서버의 rules에서 OnCreatePlayer4 빼야함

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

// 별도의 로딩씬을 만들지 않고 메인씬에서 모든걸 처리한다. 이래야 로딩속도를 최대한 줄일 수 있다.
public class MainSceneBuilder : MonoBehaviour
{
	public static MainSceneBuilder instance;
	public static bool s_initializedAddressable;
	public static bool s_firstTimeAfterLaunch = true;
	/*
	public static bool s_buildReturnScrollUsedScene = false;
	*/

	void Awake()
	{
		instance = this;

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder Awake");
#endif
	}

	public bool mainSceneBuilding { get; private set; }
	public bool waitSpawnFlag { get; set; }
	public bool lobby { get; private set; }
	public bool buildTutorialScene { get; private set; }

	void OnDestroy()
	{
#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder OnDestroy");
#endif

		Addressables.Release<GameObject>(_handleTableDataManager);

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder OnDestroy 1");
#endif

		Addressables.Release<GameObject>(_handleCommonCanvasGroup);

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder OnDestroy 2");
#endif

		// 서버오류로 인해 접속못했을 경우 대비해서 체크해둔다.
		if (_handleStageManager.IsValid() == false && mainSceneBuilding) return;

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder OnDestroy 3");
#endif

		Addressables.Release<GameObject>(_handleStageManager);
		Addressables.Release<GameObject>(_handleStartCharacter);
		Addressables.Release<GameObject>(_handleCommonBattleGroup);

		Addressables.Release<GameObject>(_handleCommonMenuGroup);
		Addressables.Release<GameObject>(_handleMainCanvas);

		/*
		Addressables.Release<GameObject>(_handleLobbyCanvas);
		Addressables.Release<GameObject>(_handleTreasureChest);

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder OnDestroy 3");
#endif

		// 이벤트용이라 항상 로드되는게 아니다보니 IsValid체크가 필수다.
		if (_handleEventGatePillar.IsValid())
			Addressables.Release<GameObject>(_handleEventGatePillar);

		// TimeSpace역시 초반에 로드하지 않을때가 많으니 IsValid체크 해야한다.
		if (_handleTimeSpacePortal.IsValid())
			Addressables.Release<GameObject>(_handleTimeSpacePortal);
		if (_handleNodeWarPortal.IsValid())
			Addressables.Release<GameObject>(_handleNodeWarPortal);
		if (_handleEventBoard.IsValid())
			Addressables.Release<GameObject>(_handleEventBoard);

		// 로딩속도를 위해 배틀매니저는 천천히 로딩한다. 그래서 다른 로딩 오브젝트와 달리 Valid 검사를 해야한다.
		if (_handleBattleManager.IsValid())
			Addressables.Release<GameObject>(_handleBattleManager);
		if (_handleDropObjectGroup.IsValid())
			Addressables.Release<GameObject>(_handleDropObjectGroup);
		*/

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder OnDestroy 4");
#endif

		// 게임을 오래 켜두면 번들데이터가 점점 커지게 된다.
		// 해제를 할만한 가장 적당한 곳은 씬이 파괴될때이다.
		AddressableAssetLoadManager.CheckRelease();

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder OnDestroy 5");
#endif

		System.GC.Collect();
		System.GC.WaitForPendingFinalizers();
	}

	AsyncOperationHandle<GameObject> _handleTableDataManager;
	AsyncOperationHandle<GameObject> _handleCommonCanvasGroup;

	AsyncOperationHandle<GameObject> _handleStageManager;
	AsyncOperationHandle<GameObject> _handleStartCharacter;
	AsyncOperationHandle<GameObject> _handleCommonBattleGroup;

	// 컨텐츠 중에서의 필수항목
	AsyncOperationHandle<GameObject> _handleCommonMenuGroup;
	AsyncOperationHandle<GameObject> _handleMainCanvas;
	/*
	AsyncOperationHandle<GameObject> _handleBattleManager;
	AsyncOperationHandle<GameObject> _handleDropObjectGroup;
	
	AsyncOperationHandle<GameObject> _handleTitleCanvas;
	AsyncOperationHandle<GameObject> _handleLobbyCanvas;

	AsyncOperationHandle<GameObject> _handleTreasureChest;
	AsyncOperationHandle<GameObject> _handleEventGatePillar;
	AsyncOperationHandle<GameObject> _handleTimeSpacePortal;
	AsyncOperationHandle<GameObject> _handleNodeWarPortal;
	AsyncOperationHandle<GameObject> _handleEventBoard;
	*/

	IEnumerator Start()
    {
#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder Start");
		if (s_initializedAddressable == false)
		{
			AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> handleInitialize = Addressables.InitializeAsync();
			yield return handleInitialize;
			s_initializedAddressable = true;
		}
		Debug.LogWarning("MainSceneBuilder Start 1");
#endif

		// 씬 빌더는 항상 이 씬이 시작될때 1회만 동작하며 로딩씬을 띄워놓고 현재 상황에 맞춰서 스텝을 진행한다.
		// 나중에 어드레서블 에셋시스템에도 적어두겠지만, 이번 구조는 1챕터까진 추가 다운로드 없이 진행하는게 목표고 이후 번들을 받는 구조가 되어야한다.
		// 그렇다고 씬에 넣어두면 Start때 로드하는거라 로딩창이 늦게 나오게 된다. 그러니 결국 Resources 폴더에 넣어두고 로딩하는 방법 말고는 없다.
		mainSceneBuilding = true;
		QualitySettings.asyncUploadTimeSlice = 8;
		LoadingCanvas.instance.gameObject.SetActive(true);
		// 2번은 호출해야 로딩화면이 온전히 보인다.
		yield return new WaitForEndOfFrame();
		LoadingCanvas.instance.SetProgressBarPoint(0.1f, 0.0f, true);
		yield return new WaitForEndOfFrame();

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder Start 2");
#endif

		// 씬 초기화를 하기전에 필수항목부터 로드해야한다.
		// 테이블을 새로 받으려고 해도 창을 띄우려면 적절한 폰트와 번역된 스트링이 필요하기 때문.
		// 당연히 사운드 볼륨 설정 같은 것도 로드해야한다.
		// 그런데 OptionManager는 내부적으로 LanguageTable을 필요로 한다.
		// 괜히 파일을 나눠서 로드하는건 별로이기 때문에 어차피 로딩해야하는 UIString 프리팹에 넣어두기로 한다.
		// 대신 UIString의 Initialize는 옵션매니저 생성 후에 호출해야한다.
		//
		// step 1-1. UIString 프리팹 로드
		if (UIString.instance != null) { }

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder Start 3");
#endif

		// step 1-2. 옵션 매니저
		if (OptionManager.instance != null) { }

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder Start 4");
#endif

		// step 1-3. initialize font & string
		UIString.instance.Initialize(OptionManager.instance.language);

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder Start 5");
#endif

		// 사실은 여기서 스트링 및 폰트 로딩이 끝나야 제대로 된 에러 메세지창을 띄울 수 있는건데
		// 문제가 발생하지 않는다면 괜히 기다리는게 되버린다.
		// 그래서 차라리 문제가 발생했을때 호출되는 AuthManager.RestartProcess 에서 로드를 기다리기로 하고
		// 여기서는 그냥 넘어가기로 한다.
		//while (UIString.instance.IsDoneLoadAsyncStringData() == false)
		//	yield return null;
		//while (UIString.instance.IsDoneLoadAsyncFont() == false)
		//	yield return null;

		// 초기화 해야할 항목들은 다음과 같다.
		// 1. 옵션매니저 초기화 및 스트링 데이터를 로드해둔다. 이래야 에러시 번역된 UI를 띄울 수 있다.
		// 2. 테이블 번들패치. 테이블은 다른 번들과 달리 물어보지 않고 곧바로 패치한다. LoadorCache 함수 쓸테니 변경시에만 받게될거다. 현재는 번들구조가 없으므로 그냥 로드
		// 3. 로그인을 해야한다. 최초 기동시엔 자동으로 게스트로 들어가고 이후 연동을 하고나면 해당 로그인으로 진행해서 플레이어 데이터를 받는다. 현재는 임시로 처리.
		// - 플레이할 캐릭터와 마지막 스테이지 정보를 받았으면 이 정보를 가지고 데이터 로딩을 시작한다.
		// 4. 데이터들을 로드하기전에 우선 이곳이 로비라는 것을 알려둔다.(강종되서 복구하는 중이더라도 로비에서 시작하고 복구 팝업을 띄우는게 맞다.)
		// 5. 이미 씬에 컨트롤러 캔버스 같은건 다 들어있다. 로컬 플레이어 캐릭터를 만들어야한다.
		// 6. 맵도 로드해야하는데 맵을 알기 위해선 StageManager도 필요하다.
		// - 5, 6번 스탭은 이전의 로드와 달리 동시에 이뤄져도 상관없는 항목들이다. 조금이라도 로딩 시간을 줄이기 위해 한번에 로드한다.
		// 7. 스테이지 매니저가 만들어지면 맵을 생성할 수 있으므로 로비맵을 로드한다.
		// 8. 게임에 진입할 수 있게 게이트 필라를 소환한다.(원래 몬스터 다 잡고 나오는거라 SceneBuild중에는 이렇게 직접 호출해야한다.)
		// 9. 로비 UI를 구축한다.
		// 10. 플레이어의 첫공격 렉을 없애기 위해 플레이어 액터에 등록된 캐시 오브젝트들을 하나씩 만들어낸다.
		// - 바로 다음에 오는 Update는 렌더하기 전이기 때문에 다다음에 오는 Update가 지나야 렌더링이 되었다고 판단할 수 있다. 2번 기다리자.
		// - 이제부터 하단은 로비인지 아닌지를 판단해서 처리해야한다.(강종 복구라면 로비가 아니다.)
		// 11. 앱을 켰을때인지 판단해서 (s_firstTimeAfterLaunch 사용) 타이틀 UI를 띄워준다. 페이드 연출 처리도 같이 한다. 회사 로고도 이때 같이 띄워준다.
		// 12. 필수로딩은 끝. 가운데 돌고있는 로딩과 하단 로딩게이지를 페이드로 지우고
		//
		// 13. 상자를 어싱크로 로딩해서 등장 연출과 함께 나온다. 만약에 이때 너무 이동이 느려지면 위로 올릴 수도 있다.
		// 14. 번들이 없는 채로 상자를 열려고 하거나 좌하단 메뉴를 누르면 튜토중이라거나 번들을 받아야함을 알린다.
		// 15. 현재 로비의 다음판을 미리 어싱크로 로딩한다. 1-0이라면 1-1의 Plane, Ground, Wall등 맵 정보를 모두 로딩해놔야한다.
		// 16. 게이트필라를 치는 순간 배틀매니저를 어싱크로 로딩하고 화면이 하얗게 된 상태에서 배틀매니저 및 다음판 로딩이 끝남을 체크한다. 끝나면 페이드인되면서 전투가 시작된다.
		// - 만약 이 로딩이 오래 걸려서 1초를 넘어가면 우하단에 작게 로딩중을 표시해준다.
		// - 매판 몹을 다 죽이고 게이트필라가 뜨는 순간마다 다음판의 맵 정보를 어싱크로 로딩해둔다.

		// step 2. 테이블 로드.
		// 용량도 작으니 언제나 리모트에서 몰래 받는다. 그래도 여러버전대로 받다보면 쌓일테니 지우고 받아야한다.
		LoadingCanvas.instance.SetProgressBarPoint(0.3f);
		const string tableDataManagerKey = "TableDataManager";
		AsyncOperationHandle<long> getDownloadSizeHandle = Addressables.GetDownloadSizeAsync(tableDataManagerKey);
		yield return getDownloadSizeHandle;
		if (getDownloadSizeHandle.Result > 0)
		{
			Debug.LogFormat("TableData Size = {0}", getDownloadSizeHandle.Result / 1024);
			AsyncOperationHandle<bool> clearHandle = Addressables.ClearDependencyCacheAsync(tableDataManagerKey, false);
			yield return clearHandle;
			Addressables.Release<bool>(clearHandle);
		}
		Addressables.Release<long>(getDownloadSizeHandle);
		_handleTableDataManager = Addressables.LoadAssetAsync<GameObject>(tableDataManagerKey);
		//yield return _handleTableDataManager;
		while (_handleTableDataManager.IsValid() && !_handleTableDataManager.IsDone)
			yield return null;
		if (_handleTableDataManager.Status != AsyncOperationStatus.Succeeded)
		{
			// 처음으로 시도하는 url 다운로드에서 에러가 난거니 서버와의 접속 오류를 표시하고 씬을 재시작시켜본다.
			PlayFabApiManager.instance.HandleCommonError();
			yield break;
		}
		Instantiate<GameObject>(_handleTableDataManager.Result);

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder Start 6");
#endif

		// step 3-1. common ui
		// 오류가 날걸 대비해서라도 UI 로딩은 필요했다. 테이블 로드 후 바로 로딩해둔다.
		_handleCommonCanvasGroup = Addressables.LoadAssetAsync<GameObject>("CommonCanvasGroup");
		//yield return _handleCommonCanvasGroup;
		while (_handleCommonCanvasGroup.IsValid() && !_handleCommonCanvasGroup.IsDone)
			yield return null;
		Instantiate<GameObject>(_handleCommonCanvasGroup.Result);

#if !UNITY_EDITOR
		Debug.LogWarning("MainSceneBuilder Start 7");
#endif

		/*
		// step 3-0. return scroll
		// 로그인 하기 전에 귀환 스크롤 사용인지 확인해서 마지막 파워소스로 되돌리는 상황인지 확인해야한다.
		if (s_buildReturnScrollUsedScene)
		{
			yield return BuildReturnScrollSceneCoroutine();
			yield break;
		}
		*/

		// step 3. login
#if PLAYFAB
		// 예전과 달리 deviceUniqueIdentifier로 게스트 로그인을 하기때문에 재설치했다고 무조건 새로운 계정을 생성하면 안된다.
		// createAccount true로 해서 서버에 로그인 패킷 보낸 후
		// 응답으로 진짜 새계정이라 하면 튜토리얼쪽으로 보내는거고 아니면 기존계정 정보 불러와서 로딩해야한다.
		if (AuthManager.instance.IsCachedLastLoginInfo() == false)
		{
#if NEWPLAYER_ADD_KEEP
			PlayerData.instance.newPlayerAddKeep = true;
#endif
			float createAccountStartTime = Time.time;
			AuthManager.instance.RequestCreateGuestAccount();
			while (PlayerData.instance.loginned == false) yield return null;
			Debug.LogFormat("Create Account Time : {0:0.###}", Time.time - createAccountStartTime);

#if NEWPLAYER_LEVEL1
			// 원래라면 아래 BuildTutorialSceneCoroutine호출하는게 맞다.
			// 그러나 튜토를 나중에 만들거고 설령 지금 만든다해도 매번 튜토챕터로 시작하는게 불편해서
			// 개발용으로 쓸 신캐 생성버전을 이 디파인에 묶어서 쓰도록 한다.
			// 처음 캐릭터를 만들면 게스트로그인으로 생성되며 챕터는 1이 선택되어있고 0스테이지 로비에서 시작된다.
#else
			/*
			 * 지금은 없어서 그냥 주석처리 해두는데 최초 생성일때 뭔가 처리할거면 이거로 씬 구축하면 될듯
			// 서버가 새 계정이라고 알려줄때만 BuildTutorialSceneCoroutine을 진행하고 아니라면 기본 로직을 따라 씬을 구축한다.
			if (PlayerData.instance.newlyCreated)
			{
				// 사이사이에 플래그 쓰면서 할까 하다가 너무 코드가 지저분해져서 그냥 따로 빼기로 한다.
				yield return BuildTutorialSceneCoroutine();
				yield break;
			}
			*/
#endif
		}

		if (PlayerData.instance.loginned == false)
		{
			float serverLoginStartTime = Time.time;
			AuthManager.instance.LoginWithLastLoginType();
			while (PlayerData.instance.loginned == false) yield return null;
			Debug.LogFormat("Server Login Time : {0:0.###}", Time.time - serverLoginStartTime);
		}
#if !UNITY_EDITOR
		Debug.LogWarning("000000000");
#endif
#else
		// only client
		if (PlayerData.instance.loginned == false)
		{
			// login and recv player data
			PlayerData.instance.OnRecvPlayerInfoForClient();
			PlayerData.instance.OnRecvCharacterListForClient();
		}
#endif

		// 마무리 셋팅하기 직전에 IAP Listener 초기화. 대신 튜토중이라면 패스한다.
		// 어차피 캐시샵을 열지 못하는 상황이라 복구가 가능해져도 뽑기가 되버리니 이상할거다. 그러니 패스.
		// 원래는 이렇게 마무리 셋팅부분에서 하려고 했었는데
		// IAP로부터 상품 가격을 받아와서 처리하는 방식으로 바꾸다보니 이 초기화가 늦어지면 캐시샵 뜨는게 느려지게 된다.
		// 그래서 로그인 후 즉시 하는거로 시점을 바꾸기로 한다.
		// 이쯤되서 로딩시켜두면 느릴일도 없을거 같다.
		//if (ContentsManager.IsTutorialChapter() == false)
			IAPListenerWrapper.instance.EnableListener(true);

		// step 4. set lobby
		lobby = true;
#if !UNITY_EDITOR
		Debug.LogWarning("222222222");
#endif
		_handleCommonBattleGroup = Addressables.LoadAssetAsync<GameObject>("CommonBattleGroup");
		_handleCommonMenuGroup = Addressables.LoadAssetAsync<GameObject>("CommonMenuGroup");
		_handleMainCanvas = Addressables.LoadAssetAsync<GameObject>("MainCanvas");

#if !UNITY_EDITOR
		Debug.LogWarning("333333333");
#endif
		/*
		if (s_firstTimeAfterLaunch)
			_handleTitleCanvas = Addressables.LoadAssetAsync<GameObject>("TitleCanvas");
		*/
#if !UNITY_EDITOR
		Debug.LogWarning("444444444");
#endif

		// step 5, 6
		LoadingCanvas.instance.SetProgressBarPoint(0.6f);
#if !UNITY_EDITOR
		Debug.LogWarning("555555555");
#endif

		_handleStageManager = Addressables.LoadAssetAsync<GameObject>("StageManager");
#if !UNITY_EDITOR
		Debug.LogWarning("666666666");
#endif
		_handleStartCharacter = Addressables.LoadAssetAsync<GameObject>(CostumeManager.instance.GetCurrentPlayerPrefabAddress());
#if !UNITY_EDITOR
		Debug.LogWarning("777777777");
#endif
		// 이런식으로 직접 IsDone을 돌리면 안된다. 데드락이 발생할 가능성이 있다.
		//while (!_handleStageManager.IsDone || !_handleStartCharacter.IsDone) yield return null;
		// 라고 해서 yield return 으로 했는데도 여전히 데드락이 발생해서 찾아보니
		// 로딩 쓰레드쪽에서 자동으로 이 핸들을 릴리즈 시키기때문에 yield return 대기중에 익셉션이 뜰 수 있는 구조라고 한다.
		// Exception: Attempting to use an invalid operation handle
		// UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1[TObject].get_InternalOp ()
		// (at Library/PackageCache/com.unity.addressables@1.6.2/Runtime/ResourceManager/AsyncOperations/AsyncOperationHandle.cs:186)
		// 이렇게 뜬다고 한다.
		// 그래서 Task를 써서 await로 하거나 Completed 콜백으로 처리해야 제대로 처리할 수 있다고 한다.
		// 근데 async await는 작은 단위를 Task로 묶어서 멀티쓰레딩을 하기 위함이니 이런 씬 로드 같이 거대함수에 쓰는게 맞는건가 싶기도 하고
		// 그렇다고 Completed 콜백으로 하기엔 구조를 싹 바꿔야해서 새로 짜야한다.
		// 그러다가 찾은 방법이
		// while (handle.IsValid() && !handle.IsDone)
		//	yield return null;
		// 이 방법이다. 이건 기존 코루틴 코드를 그대로 유지하면서 핸들이 릴리즈 되더라도 익셉션이 뜨지 않을거라 해서 테스트 해보기로 한다.
		//yield return _handleStageManager;
		//yield return _handleStartCharacter;
		while (_handleStageManager.IsValid() && !_handleStageManager.IsDone)
			yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("888888888");
#endif
		Instantiate<GameObject>(_handleStageManager.Result);
#if !UNITY_EDITOR
		Debug.LogWarning("888888888-1");
#endif

		// 씬을 구축할때는 항상 반복모드로 시작하고 그 이후에 도전모드로 전환하는 형태다.
		StageManager.instance.InitializeStageFloor(PlayerData.instance.selectedStage, true);

		/*
		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("DDDDDDDDD");
#endif
		StageManager.instance.MoveToNextStage(true);
#if !UNITY_EDITOR
		Debug.LogWarning("EEEEEEEEE");
#endif
		*/

		// step 8. gate pillar & TreasureChest
		yield return new WaitUntil(() => waitSpawnFlag);

		// 흠.. 어드레서블 에셋으로 뺐더니 5.7초까지 늘어났다. 번들에서 읽으니 어쩔 수 없는건가.

		// 그냥 Resources.Load는 4.111초 4.126초 이정도 걸린다.
		// Resoures.LoadAsync는 4.025초 정도. 로딩화면 갱신도 못하는데 느려서 안쓴다.
		//Instantiate<GameObject>(Resources.Load<GameObject>("Manager"));
		//Instantiate<GameObject>(Resources.Load<GameObject>("Character/Ganfaul"));

		// step 7. 스테이지
		// 차후에는 챕터의 0스테이지에서 시작하게 될텐데 0스테이지에서 쓸 맵을 알아내려면
		// 진입전에 아래 함수를 수행해서 캐싱할 수 있어야한다.
		// 방법은 세가지인데,
		// 1. static으로 빼서 데이터 처리만 먼저 할 수 있게 하는 방법
		// 2. DataManager 를 분리해서 데이터만 처리할 수 있게 하는 방법
		// 3. 스테이지 매니저가 언제나 살아있는 싱글톤 클래스가 되는 방법
		// 3은 다른 리소스도 들고있는데 살려둘 순 없으니 패스고 1은 너무 어거지다.
		// 결국 재부팅시 데이터 캐싱등의 처리까지 하려면 2번이 제일 낫다.
#if !UNITY_EDITOR
		Debug.LogWarning("999999999");
#endif
		LoadingCanvas.instance.SetProgressBarPoint(0.9f);
#if !UNITY_EDITOR
		Debug.LogWarning("AAAAAAAAA");
#endif

		/*
		_handleTreasureChest = Addressables.LoadAssetAsync<GameObject>("TreasureChest");
#if !UNITY_EDITOR
		Debug.LogWarning("BBBBBBBBB");
#endif
		bool useTimeSpace = false;
		if (EventManager.instance.IsStandbyClientEvent(EventManager.eClientEvent.OpenTimeSpace))
		{
			_handleTimeSpacePortal = Addressables.LoadAssetAsync<GameObject>("OpenTimeSpacePortal");
			useTimeSpace = true;
		}
		else if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace))
		{
			_handleTimeSpacePortal = Addressables.LoadAssetAsync<GameObject>("TimeSpacePortal");
			useTimeSpace = true;
		}
#if !UNITY_EDITOR
		Debug.LogWarning("BBBBBBBBB-1");
#endif
		bool useNodeWar = false;
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.NodeWar))
		{
			_handleNodeWarPortal = Addressables.LoadAssetAsync<GameObject>("NodeWarPortal");
			useNodeWar = true;
		}
#if !UNITY_EDITOR
		Debug.LogWarning("BBBBBBBBB-2");
#endif
		bool useEventBoard = false;
		if (ContentsManager.IsTutorialChapter() == false && PlayerData.instance.lobbyDownloadState == false && CumulativeEventData.instance.disableEvent == false)
		{
			_handleEventBoard = Addressables.LoadAssetAsync<GameObject>("EventBoard");
			useEventBoard = true;
		}
		*/
#if !UNITY_EDITOR
		Debug.LogWarning("BBBBBBBBB-3");
#endif
		while (_handleStartCharacter.IsValid() && !_handleStartCharacter.IsDone)
			yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("CCCCCCCCC");
#endif

#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(_handleStartCharacter.Result);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		Instantiate<GameObject>(_handleStartCharacter.Result);
#endif

		//
		TeamManager.instance.InitializeTeamMember(true);

#if !UNITY_EDITOR
		Debug.LogWarning("FFFFFFFFF");
#endif
		SoundManager.instance.PlayLobbyBgm();

		/*
		if (EventManager.instance.IsStandbyOpenChaosEvent())
		{
			_handleEventGatePillar = Addressables.LoadAssetAsync<GameObject>("OpenChaosGatePillar");
#if !UNITY_EDITOR
			Debug.LogWarning("GGGGGGGGG-1");
#endif
			//yield return _handleEventGatePillar;
			while (_handleEventGatePillar.IsValid() && !_handleEventGatePillar.IsDone)
				yield return null;
			Instantiate<GameObject>(_handleEventGatePillar.Result, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
		}
		else
		{
			BattleInstanceManager.instance.GetCachedObject(GetCurrentGatePillarPrefab(), StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
#if !UNITY_EDITOR
			Debug.LogWarning("GGGGGGGGG");
#endif
			HitRimBlink.ShowHitRimBlink(GatePillar.instance.cachedTransform, Vector3.forward, true);
		}
#if !UNITY_EDITOR
		Debug.LogWarning("HHHHHHHHH");
#endif
		//yield return _handleTreasureChest;
		while (_handleTreasureChest.IsValid() && !_handleTreasureChest.IsDone)
			yield return null;
#if UNITY_EDITOR
		newObject = Instantiate<GameObject>(_handleTreasureChest.Result);
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		Instantiate<GameObject>(_handleTreasureChest.Result);
#endif
#if !UNITY_EDITOR
		Debug.LogWarning("HHHHHHHHH-0");
#endif
		if (useTimeSpace)
		{
			//yield return _handleTimeSpacePortal;
			while (_handleTimeSpacePortal.IsValid() && !_handleTimeSpacePortal.IsDone)
				yield return null;
#if UNITY_EDITOR
			newObject = Instantiate<GameObject>(_handleTimeSpacePortal.Result);
			if (settings.ActivePlayModeDataBuilderIndex == 2)
				ObjectUtil.ReloadShader(newObject);
#else
			Instantiate<GameObject>(_handleTimeSpacePortal.Result);
#endif
#if !UNITY_EDITOR
			Debug.LogWarning("HHHHHHHHH-1");
#endif
		}

		if (useNodeWar)
		{
			//yield return _handleNodeWarPortal;
			while (_handleNodeWarPortal.IsValid() && !_handleNodeWarPortal.IsDone)
				yield return null;
#if UNITY_EDITOR
			newObject = Instantiate<GameObject>(_handleNodeWarPortal.Result);
			if (settings.ActivePlayModeDataBuilderIndex == 2)
				ObjectUtil.ReloadShader(newObject);
#else
			Instantiate<GameObject>(_handleNodeWarPortal.Result);
#endif

			// 이벤트를 보여주려는 상태라면 만들어놓고 숨겨둔다.
			if (EventManager.instance.IsStandbyServerEvent(EventManager.eServerEvent.node))
				NodeWarPortal.instance.gameObject.SetActive(false);
#if !UNITY_EDITOR
			Debug.LogWarning("HHHHHHHHH-2");
#endif
		}

		if (useEventBoard)
		{
			while (_handleEventBoard.IsValid() && !_handleEventBoard.IsDone)
				yield return null;
#if UNITY_EDITOR
			newObject = Instantiate<GameObject>(_handleEventBoard.Result);
			if (settings.ActivePlayModeDataBuilderIndex == 2)
				ObjectUtil.ReloadShader(newObject);
#else
			Instantiate<GameObject>(_handleEventBoard.Result);
#endif
		}

#if !UNITY_EDITOR
		Debug.LogWarning("HHHHHHHHH-4");
#endif
		// 현재맵의 로딩이 끝나면 다음맵의 프리팹을 로딩해놔야 게이트 필라로 이동시 곧바로 이동할 수 있게 된다.
		// 원래라면 몹 다 죽이고 호출되는 함수인데 초기 씬 구축에선 할 타이밍이 로비맵 로딩 직후밖에 없다.
		StageManager.instance.GetNextStageInfo();

#if !UNITY_EDITOR
		Debug.LogWarning("IIIIIIIII");
#endif
		*/

		// step 9-1. 첫번재 UI를 소환하기 전에 UIString Font의 로드가 완료되어있는지 체크해야하고 StringTable을 캐싱해둔다.
		while (UIString.instance.IsDoneLoadAsyncStringData() == false)
			yield return null;
		while (UIString.instance.IsDoneLoadAsyncFont() == false)
			yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("JJJJJJJJJ");
#endif
		// step 9-2. lobby ui
		//yield return _handleLobbyCanvas;
		//yield return _handleCommonCanvasGroup;
		yield return _handleMainCanvas;
		/*
		while (_handleLobbyCanvas.IsValid() && !_handleLobbyCanvas.IsDone)
			yield return null;
		while (_handleCommonCanvasGroup.IsValid() && !_handleCommonCanvasGroup.IsDone)
			yield return null;
		*/
		while (_handleCommonBattleGroup.IsValid() && !_handleCommonBattleGroup.IsDone)
			yield return null;
		while (_handleCommonMenuGroup.IsValid() && !_handleCommonMenuGroup.IsDone)
			yield return null;
		while (_handleMainCanvas.IsValid() && !_handleMainCanvas.IsDone)
			yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("KKKKKKKKK");
#endif
		/*
		Instantiate<GameObject>(_handleLobbyCanvas.Result);
		*/
		Instantiate<GameObject>(_handleMainCanvas.Result);
		Instantiate<GameObject>(_handleCommonMenuGroup.Result);
		Instantiate<GameObject>(_handleCommonBattleGroup.Result);
#if !UNITY_EDITOR
		Debug.LogWarning("LLLLLLLLL");
#endif

		// step 9-3. clean memory
		yield return new WaitForEndOfFrame();
		System.GC.Collect();
		// 유니티 버그 같긴 한데.. 풀패치를 받고나서 유니티 에디터에서 플레이를 멈췄다가 다시 플레이 할때 최초 씬을 로딩하는건데도 여기서 멈춘다. 유니티를 껐다켜면 발생하지 않는다..
		// 디바이스에서 발생하지만 않으면 상관없으니 따로 처리하지는 않는다.
		yield return Resources.UnloadUnusedAssets();
		System.GC.Collect();
		System.GC.WaitForPendingFinalizers();
		yield return new WaitForEndOfFrame();

		// step 10. player hit object caching
		LoadingCanvas.instance.SetProgressBarPoint(1.0f, 0.0f, true);
#if !UNITY_EDITOR
		Debug.LogWarning("MMMMMMMMM");
#endif

		// 캐싱 오브젝트를 만들어내기 전에 사운드부터 줄여놔야 안들린다.
		SoundManager.instance.SetUiVolume(0.0f);
		if (BattleInstanceManager.instance.playerActor.cachingObjectList != null && BattleInstanceManager.instance.playerActor.cachingObjectList.Length > 0)
		{
			_listCachingObject = new List<GameObject>();
			for (int i = 0; i < BattleInstanceManager.instance.playerActor.cachingObjectList.Length; ++i)
				_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleInstanceManager.instance.playerActor.cachingObjectList[i], Vector3.right, Quaternion.identity));
		}
#if !UNITY_EDITOR
		Debug.LogWarning("OOOOOOOOO");
#endif

		// step 11. title ui
		if (s_firstTimeAfterLaunch)
		{
			PlayerData.instance.checkRestartScene = true;
		}

		// 마무리 셋팅
		_waitUpdateRemainCount = 2;
		mainSceneBuilding = false;
		QualitySettings.asyncUploadTimeSlice = 2;
		s_firstTimeAfterLaunch = false;
	}

	// Update is called once per frame
	List<GameObject> _listCachingObject = null;
	int _waitUpdateRemainCount;
	public bool waitCachingObject { get { return _waitUpdateRemainCount > 0; } }
    void LateUpdate()
    {
		if (_waitUpdateRemainCount > 0)
		{
			_waitUpdateRemainCount -= 1;
			if (_waitUpdateRemainCount == 0)
			{
				if (_listCachingObject != null)
				{
					for (int i = 0; i < _listCachingObject.Count; ++i)
						_listCachingObject[i].SetActive(false);
					_listCachingObject.Clear();
				}
				// 캐싱 오브젝트 끌때 바로 복구
				SoundManager.instance.SetUiVolume(OptionManager.instance.systemVolume);

				// step 12. fade out
				if (buildTutorialScene)// || s_buildReturnScrollUsedScene)
				{
					// 0챕터 1스테이지에서 시작하는거라 강제로 전투모드로 바꿔준다.
					StartCoroutine(LateInitialize());
					LobbyCanvas.instance.OnExitLobby();

					if (buildTutorialScene)
					{
						// 튜토때만 보이는 계정연동 버튼 처리
						UIInstanceManager.instance.ShowCanvasAsync("TutorialLinkAccountCanvas", null);
					}
				}
				else
				{
					// 일반적인 경우엔 가운데 오브젝트만 FadeOut하고 LateInitialize를 호출해둔다.
					LoadingCanvas.instance.FadeOutObject();
					StartCoroutine(LateInitialize());
				}
			}
		}
    }

	IEnumerator LateInitialize()
	{
		EquipManager.instance.LateInitialize();

		bool battleScene = (buildTutorialScene);// || s_buildReturnScrollUsedScene);
		if (battleScene == false)
		{
			/*
			LobbyCanvas.instance.RefreshAlarmObject();
			if (EventBoard.instance != null)
				EventBoard.instance.RefreshBoardOnOff();

			PlayerData.instance.LateInitialize();
			DailyShopData.instance.LateInitialize();
			TimeSpaceData.instance.LateInitialize();
			QuestData.instance.LateInitialize();
			CumulativeEventData.instance.LateInitialize();
			RankingData.instance.LateInitialize();
			*/
		}

		/*
		if (ContentsData.instance.readyToReopenBossEnterCanvas)
		{
			BossBattleEnterCanvas.PreloadReadyToReopen();
			ContentsData.instance.readyToReopenBossEnterCanvas = false;
		}
		if (ContentsData.instance.readyToReopenInvasionEnterCanvas)
		{
			InvasionEnterCanvas.PreloadReadyToReopen();
			ContentsData.instance.readyToReopenInvasionEnterCanvas = false;
		}
		*/

		// 캐릭터는 언제나 같은 번들안에 있을테니 마지막 번호 하나만 검사하기로 했는데
		// 하필 마지막 자리에 드론캐릭터가 추가되면서 그 전꺼로 검사하기로 한다.
		// 다운로드 되어있어서 캐싱할 수 있을때만 프리로드를 걸어둔다.
		string portraitKey = TableDataManager.instance.actorTable.dataArray[TableDataManager.instance.actorTable.dataArray.Length - 2].portraitAddress;
		AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(portraitKey);
		yield return handle;
		long downloadSize = handle.Result;
		Addressables.Release<long>(handle);
		if (downloadSize == 0)
		{
			for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
			{
				if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_DroneActorId)
					continue;
				/*
				if (MercenaryData.IsMercenaryActor(TableDataManager.instance.actorTable.dataArray[i].actorId))
					continue;
				*/
				AddressableAssetLoadManager.GetAddressableSprite(TableDataManager.instance.actorTable.dataArray[i].portraitAddress, "Icon", null);
			}
		}
		else
			Debug.LogFormat("Actor Portrait pIcon Size = {0}", downloadSize / 1024);

		// 워낙 크기가 작으니 LateInitialize에서 해도 문제없을거다.
		SoundManager.instance.LoadInApkSFXContainer();

		/*
		// DropObject의 크기도 커지고 로비뽑기에서 써야해서 BattleManager에서 분리한다.
		_handleDropObjectGroup = Addressables.LoadAssetAsync<GameObject>("DropObjectGroup");
		//yield return _handleDropObjectGroup;
		while (_handleDropObjectGroup.IsValid() && !_handleDropObjectGroup.IsDone)
			yield return null;
		Instantiate<GameObject>(_handleDropObjectGroup.Result);
		_handleBattleManager = Addressables.LoadAssetAsync<GameObject>("BattleManager");
		//yield return _handleBattleManager;
		while (_handleBattleManager.IsValid() && !_handleBattleManager.IsDone)
			yield return null;
		Instantiate<GameObject>(_handleBattleManager.Result);

		if (battleScene)
		{
			BattleManager.instance.OnSpawnFlag();
			LoadingCanvas.instance.FadeOutObject();
			BattleInstanceManager.instance.playerActor.InitializeCanvas();
		}
		*/
	}

	public bool IsDoneLateInitialized(bool onlyDropObjectGroup = false)
	{
		/*
		if (_handleDropObjectGroup.IsValid() == false)
			return false;

		if (onlyDropObjectGroup)
			return true;

		return _handleBattleManager.IsValid();
		*/
		return true;
	}

	public void OnFinishTitleCanvas()
	{
		/*
		Addressables.Release<GameObject>(_handleTitleCanvas);
		*/
	}

	public void OnExitLobby()
	{
		lobby = false;
		/*
		TreasureChest.instance.gameObject.SetActive(false);
		if (TimeSpacePortal.instance != null)
			TimeSpacePortal.instance.gameObject.SetActive(false);
		LobbyCanvas.instance.OnExitLobby();
		if (EnergyGaugeCanvas.instance != null)
			EnergyGaugeCanvas.instance.gameObject.SetActive(false);
		if (DailyBoxGaugeCanvas.instance != null)
			DailyBoxGaugeCanvas.instance.gameObject.SetActive(false);
		if (ChaosPurifier.instance != null)
			ChaosPurifier.instance.gameObject.SetActive(false);
		if (NewChapterCanvas.instance != null)
			NewChapterCanvas.instance.gameObject.SetActive(false);
		if (BattleInstanceManager.instance.playerActor != null)
			BattleInstanceManager.instance.playerActor.InitializeCanvas();
		if (EventBoard.instance != null)
			EventBoard.instance.gameObject.SetActive(false);
		*/
	}


#if PLAYFAB
#region Play After Installation
	// 설치 직후 플레이 혹은 데이터 리셋 후 플레이
	IEnumerator BuildTutorialSceneCoroutine()
	{
		buildTutorialScene = true;
		yield break;

		/*
		// step 4. set lobby
		_handleLobbyCanvas = Addressables.LoadAssetAsync<GameObject>("LobbyCanvas");
		_handleCommonCanvasGroup = Addressables.LoadAssetAsync<GameObject>("CommonCanvasGroup");

		// step 5, 6
		LoadingCanvas.instance.SetProgressBarPoint(0.6f);

		
		_handleStageManager = Addressables.LoadAssetAsync<GameObject>("StageManager");
		_handleStartCharacter = Addressables.LoadAssetAsync<GameObject>(CharacterData.GetAddressByActorId(PlayerData.instance.mainCharacterId));
		while (_handleStageManager.IsValid() && !_handleStageManager.IsDone)
			yield return null;
		while (_handleStartCharacter.IsValid() && !_handleStartCharacter.IsDone)
			yield return null;

		/*
		Instantiate<GameObject>(_handleStageManager.Result);
#if UNITY_EDITOR
		Vector3 tutorialPosition = new Vector3(BattleInstanceManager.instance.GetCachedGlobalConstantFloat("TutorialStartX"), 0.0f, BattleInstanceManager.instance.GetCachedGlobalConstantFloat("TutorialStartZ"));
		GameObject newObject = Instantiate<GameObject>(_handleStartCharacter.Result, tutorialPosition, Quaternion.identity);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		Instantiate<GameObject>(_handleStartCharacter.Result);
#endif

		// 로딩 자체를 안해버리면 handle없어서 오류 날 수 있으니 Instantiate는 안해도 로딩은 해두자.
		LoadingCanvas.instance.SetProgressBarPoint(0.9f);
		_handleTreasureChest = Addressables.LoadAssetAsync<GameObject>("TreasureChest");

		// 강제로 시작하는거니 항상 0챕터 1스테이지
		StageManager.instance.InitializeStage(0, 1);
		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return null;
		StageManager.instance.MoveToNextStage(true);

		// step 8. gate pillar & TreasureChest
		yield return new WaitUntil(() => waitSpawnFlag);
		SoundManager.instance.PlayBattleBgm(PlayerData.instance.mainCharacterId);

		StageManager.instance.GetNextStageInfo();
		while (UIString.instance.IsDoneLoadAsyncStringData() == false)
			yield return null;
		while (UIString.instance.IsDoneLoadAsyncFont() == false)
			yield return null;
		// step 9-2. lobby ui
		while (_handleLobbyCanvas.IsValid() && !_handleLobbyCanvas.IsDone)
			yield return null;
		while (_handleCommonCanvasGroup.IsValid() && !_handleCommonCanvasGroup.IsDone)
			yield return null;
		Instantiate<GameObject>(_handleLobbyCanvas.Result);
		Instantiate<GameObject>(_handleCommonCanvasGroup.Result);

		// step 10. player hit object caching
		LoadingCanvas.instance.SetProgressBarPoint(1.0f, 0.0f, true);
		if (BattleInstanceManager.instance.playerActor.cachingObjectList != null && BattleInstanceManager.instance.playerActor.cachingObjectList.Length > 0)
		{
			_listCachingObject = new List<GameObject>();
			for (int i = 0; i < BattleInstanceManager.instance.playerActor.cachingObjectList.Length; ++i)
				_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleInstanceManager.instance.playerActor.cachingObjectList[i], Vector3.right, Quaternion.identity));
		}

		// 마무리 셋팅하기 직전에 IAP Listener 초기화. 튜토 전투씬이라면 굳이 초기화할 필요 없다.
		//IAPListenerWrapper.instance.EnableListener(true);

		// 마무리 셋팅
		_waitUpdateRemainCount = 2;
		*/
		mainSceneBuilding = false;
		QualitySettings.asyncUploadTimeSlice = 2;
	}
#endregion

#region Return Scroll Scene
	// 귀환 스크롤 쓸때는 씬을 재구축해야 버그가 생기지 않을 확률이 높아진다.
	public static string s_lastPowerSourceEnvironmentSettingAddress;
	IEnumerator BuildReturnScrollSceneCoroutine()
	{
		yield break;

		/*
		// step 4. set lobby
		_handleLobbyCanvas = Addressables.LoadAssetAsync<GameObject>("LobbyCanvas");
		_handleCommonCanvasGroup = Addressables.LoadAssetAsync<GameObject>("CommonCanvasGroup");

		// step 5, 6
		LoadingCanvas.instance.SetProgressBarPoint(0.6f);
		_handleStageManager = Addressables.LoadAssetAsync<GameObject>("StageManager");
		_handleStartCharacter = Addressables.LoadAssetAsync<GameObject>(CharacterData.GetAddressByActorId(ClientSaveData.instance.GetCachedBattleActor()));

		while (_handleStageManager.IsValid() && !_handleStageManager.IsDone)
			yield return null;
		while (_handleStartCharacter.IsValid() && !_handleStartCharacter.IsDone)
			yield return null;
		Instantiate<GameObject>(_handleStageManager.Result);
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(_handleStartCharacter.Result, Vector3.zero, Quaternion.identity);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(_handleStartCharacter.Result);
#endif
		/*
		// 부활 복구씬에서도 Mercenary 검사는 해야한다.
		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor != null)
		{
			if (MercenaryData.IsMercenaryActor(ClientSaveData.instance.GetCachedBattleActor()))
				playerActor.mercenary = true;
		}

		// 로딩 자체를 안해버리면 handle없어서 오류 날 수 있으니 Instantiate는 안해도 로딩은 해두자.
		LoadingCanvas.instance.SetProgressBarPoint(0.9f);
		_handleTreasureChest = Addressables.LoadAssetAsync<GameObject>("TreasureChest");

		// 마지막 부활지점
		StageManager.instance.InitializeStage(PlayerData.instance.selectedChapter, ClientSaveData.instance.GetCachedLastPowerSourceStage());
		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return null;
		StageManager.instance.MoveToNextStage(true);

		// step 8. gate pillar & TreasureChest
		yield return new WaitUntil(() => waitSpawnFlag);
		SoundManager.instance.PlayBattleBgm(ClientSaveData.instance.GetCachedBattleActor());

		StageManager.instance.GetNextStageInfo();
		while (UIString.instance.IsDoneLoadAsyncStringData() == false)
			yield return null;
		while (UIString.instance.IsDoneLoadAsyncFont() == false)
			yield return null;
		// step 9-2. lobby ui
		while (_handleLobbyCanvas.IsValid() && !_handleLobbyCanvas.IsDone)
			yield return null;
		while (_handleCommonCanvasGroup.IsValid() && !_handleCommonCanvasGroup.IsDone)
			yield return null;
		Instantiate<GameObject>(_handleLobbyCanvas.Result);
		Instantiate<GameObject>(_handleCommonCanvasGroup.Result);

		// step 10. player hit object caching
		LoadingCanvas.instance.SetProgressBarPoint(1.0f, 0.0f, true);
		if (BattleInstanceManager.instance.playerActor.cachingObjectList != null && BattleInstanceManager.instance.playerActor.cachingObjectList.Length > 0)
		{
			_listCachingObject = new List<GameObject>();
			for (int i = 0; i < BattleInstanceManager.instance.playerActor.cachingObjectList.Length; ++i)
				_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleInstanceManager.instance.playerActor.cachingObjectList[i], Vector3.right, Quaternion.identity));
		}
		*/

		// 마무리 셋팅하기 직전에 IAP Listener 초기화 해야하는데 전투씬이니까 패스
		//IAPListenerWrapper.instance.EnableListener(true);

		// 마무리 셋팅
		/*
		_waitUpdateRemainCount = 2;
		*/
		mainSceneBuilding = false;
		QualitySettings.asyncUploadTimeSlice = 2;
	}
#endregion
#endif
}