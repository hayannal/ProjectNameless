using System;
using System.Collections.Concurrent;
using PlayFab.Internal;

namespace PlayFab
{
    public class PluginManager
    {
        private ConcurrentDictionary<PluginContractKey, IPlayFabPlugin> plugins = new ConcurrentDictionary<PluginContractKey, IPlayFabPlugin>(new PluginContractKeyComparator());

        /// <summary>
        /// The singleton instance of plugin manager.
        /// </summary>
        private static readonly PluginManager Instance = new PluginManager();

        private PluginManager()
        {
        }

        /// <summary>
        /// Gets a plugin.
        /// If a plugin with specified contract and optional instance name does not exist, it will create a new one.
        /// </summary>
        /// <param name="contract">The plugin contract.</param>
        /// <param name="instanceName">The optional plugin instance name. Instance names allow to have mulptiple plugins with the same contract.</param>
        /// <returns>The plugin instance.</returns>
        public static T GetPlugin<T>(PluginContract contract, string instanceName = "") where T : IPlayFabPlugin
        {
            return (T)Instance.GetPluginInternal(contract, instanceName);
        }

        /// <summary>
        /// Sets a custom plugin.
        /// If a plugin with specified contract and optional instance name already exists, it will be replaced with specified instance.
        /// </summary>
        /// <param name="plugin">The plugin instance.</param>
        /// <param name="contract">The app contract of plugin.</param>
        /// <param name="instanceName">The optional plugin instance name. Instance names allow to have mulptiple plugins with the same contract.</param>
        public static void SetPlugin(IPlayFabPlugin plugin, PluginContract contract, string instanceName = "")
        {
            Instance.SetPluginInternal(plugin, contract, instanceName);
        }

        private IPlayFabPlugin GetPluginInternal(PluginContract contract, string instanceName)
        {
            var key = new PluginContractKey { _pluginContract = contract, _pluginName = instanceName };
            IPlayFabPlugin plugin;
            if (!this.plugins.TryGetValue(key, out plugin))
            {
                // Requested plugin is not in the cache, create the default one
                switch (contract)
                {
                    case PluginContract.PlayFab_Serializer:
                        plugin = this.CreatePlugin<PlayFab.Json.SimpleJsonInstance>();
                        break;
                    case PluginContract.PlayFab_Transport:
                        plugin = this.CreatePlayFabTransportPlugin();
                        break;
                    default:
                        throw new ArgumentException("This contract is not supported", "contract");
                }

                this.plugins[key] = plugin;
            }

            return plugin;
        }

        private void SetPluginInternal(IPlayFabPlugin plugin, PluginContract contract, string instanceName)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin", "Plugin instance cannot be null");
            }

            var key = new PluginContractKey { _pluginContract = contract, _pluginName = instanceName };
            this.plugins[key] = plugin;
        }

        private IPlayFabPlugin CreatePlugin<T>() where T : IPlayFabPlugin, new()
        {
            return (IPlayFabPlugin)System.Activator.CreateInstance(typeof(T));
        }

        private ITransportPlugin CreatePlayFabTransportPlugin()
        {
            ITransportPlugin transport = null;
#if !UNITY_WSA && !UNITY_WP8
            if (PlayFabSettings.RequestType == WebRequestType.HttpWebRequest)
                transport = new PlayFabWebRequest();
#endif
#if UNITY_IOS
			// 유니티 2019 + 플레이팹 예전버전 쓸때 아이폰에서 메모리 누수 버그가 있어서 추가했던 코드다.
			// 그런데 유니티 2021 + 플레이팹 2023년 초반 최신버전으로 바꾸고나니
			// 오히려 WebRequest를 사용할 경우 대용량 뽑기에서 WebException이 떠서 패킷이 제대로 오지 않게 되었다.
			// 그래서 이 코드 주석걸고 원래의 PlayFabUnityHttp 방식으로 돌려놓으니 대용량 뽑기부터 메모리누수까지 없이 잘 플레이 되길래
			// 다시 주석처리해놓기로 한다.
			// 되돌려놓긴 했지만 히스토리를 기억하기 위해 주석으로 남겨둔다. 하면 된다는거다.
			//if (transport == null)
			//	transport = new PlayFabWebRequest();
#endif

#if UNITY_2018_2_OR_NEWER // PlayFabWww will throw warnings as Unity has deprecated Www
            if (transport == null)
                transport = new PlayFabUnityHttp();
#elif UNITY_2017_2_OR_NEWER
            if (PlayFabSettings.RequestType == WebRequestType.UnityWww)
                transport = new PlayFabWww();

            if (transport == null)
                transport = new PlayFabUnityHttp();
#else
            if (transport == null)
                transport = new PlayFabWww();
#endif

            return transport;
        }
    }
}
