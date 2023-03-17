//#define NotUse
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Michsky.UI.Hexart
{
    public class SwitchAnim : MonoBehaviour
    {
        [Header("SWITCH")]
        public Animator switchAnimator;

        [Header("SETTINGS")]
        [Tooltip("IMPORTANT! EVERY SWITCH MUST HAVE A DIFFERENT ID")]
        public int switchID = 0;
        public bool isOn;
        public bool saveValue;
        [Tooltip("Use it if you're using this switch first time. 1 = ON, and 0 = OFF")]
        [Range(0, 1)] public int playerPrefsHelper;

        public UnityEvent OffEvents;
        public UnityEvent OnEvents;

        private Button offButton;
        private Button onButton;

        private string onTransition = "Switch On";
        private string offTransition = "Switch Off";
		private string onTransitionX10 = "Switch On x10";
		private string offTransitionX10 = "Switch Off x10";

		public Text onOffText;
		public Image handlerImage;

		bool _started = false;
        void Start()
        {
#if NotUse
            playerPrefsHelper = PlayerPrefs.GetInt(switchID + "Switch");

			if (saveValue == true)
            {
                if (playerPrefsHelper == 1)
                {
                    OnEvents.Invoke();
                    switchAnimator.Play(onTransition);
					OnOffText(true);
					isOn = true;
                }

                else
                {
                    OffEvents.Invoke();
					switchAnimator.Play(offTransition);
					OnOffText(false);
					isOn = false;
                }
            }

            else
            {
#endif
				if (isOn == true)
                {
                    switchAnimator.Play(onTransitionX10);
					OnOffText(true);
#if NotUse
					OnEvents.Invoke();
                    isOn = true;
#endif
                }

                else
                {
                    switchAnimator.Play(offTransitionX10);
					OnOffText(false);
#if NotUse
					OffEvents.Invoke();
                    isOn = false;
#endif
                }
#if NotUse
			}
#endif
			_started = true;
		}

		void OnEnable()
		{
			if (_started == false)
				return;

			if (isOn == true)
			{
				switchAnimator.Play(onTransitionX10);
				OnOffText(true);
			}
			else
			{
				switchAnimator.Play(offTransitionX10);
				OnOffText(false);
			}
		}


		public void AnimateSwitch()
        {
            if (isOn == true)
            {
                OffEvents.Invoke();
                switchAnimator.Play(offTransition);
				OnOffText(false);
				isOn = false;
                playerPrefsHelper = 0;
            }

            else
            {
                OnEvents.Invoke();
                switchAnimator.Play(onTransition);
				OnOffText(true);
				isOn = true;
                playerPrefsHelper = 1;
            }

#if NotUse
            if (saveValue == true)
            {
                PlayerPrefs.SetInt(switchID + "Switch", playerPrefsHelper);
            }
#endif
		}

		void OnOffText(bool on)
		{
			if (onOffText == null)
				return;
			onOffText.text = on ? "ON" : "OFF";

			if (handlerImage == null)
				return;
			onOffText.color = on ? Color.white : handlerImage.color;
		}
	}
}