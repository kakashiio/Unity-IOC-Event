using System;
using System.Collections.Generic;
using System.Reflection;
using IO.Unity3D.Source.IOC;
using IO.Unity3D.Source.IOCUnity;
using IO.Unity3D.Source.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IO.Unity3D.Source.IOCEvent
{
    public class BasicDemo : MonoBehaviour
    {
        void Start()
        {
            ITypeContainer typeContainer = new TypeContainerCollection(new List<ITypeContainer>
            {
                new TypeContainer(Assembly.GetExecutingAssembly()),
                new TypeContainer(typeof(IOCComponent).Assembly),
                new TypeContainer(typeof(UnityIOCContainer).Assembly),
                new TypeContainer(typeof(EventManager).Assembly)
            });
            new UnityIOCContainer(typeContainer);
        }
    }

    class GameEvent
    {
        public const string EVT_INIT = nameof(EVT_INIT);
        public const string EVT_LOAD_INIT_SCENE = nameof(EVT_LOAD_INIT_SCENE);
        public const string EVT_INIT_PROGRESS = nameof(EVT_INIT_PROGRESS);
    }

    [IOCComponent]
    class Game : IInstanceLifeCycle
    {
        [Autowired]
        private EventManager _EventManager;
        
        public void BeforePropertiesOrFieldsSet()
        {
        }

        public void AfterPropertiesOrFieldsSet()
        {
        }

        public void AfterAllInstanceInit()
        {
            _EventManager.FireEvent(GameEvent.EVT_INIT_PROGRESS, 0f);
            _EventManager.FireEvent(GameEvent.EVT_INIT);
        }
    }

    [IOCComponent]
    class GameFlowController
    {
        [Autowired]
        private AssetLoader _AssetLoader;
        [Autowired]
        private EventManager _EventManager;
        
        [Event(GameEvent.EVT_INIT)]
        public void OnInit()
        {
            Debug.LogError("GameFlow OnInit");
            _AssetLoader.LoadScene("ScenePath", (scene) => _EventManager.FireEvent(GameEvent.EVT_LOAD_INIT_SCENE, scene));
        }
        
        [Event(GameEvent.EVT_INIT_PROGRESS)]
        public void OnProgress(float progress)
        {
            Debug.LogError("GameFlow OnProgress " + progress);
        }
        
        [Event(GameEvent.EVT_LOAD_INIT_SCENE)]
        public void OnEnterInitScene(Scene scene)
        {
            Debug.LogError($"GameFlow OnEnterInitScene scene={scene}");
            _EventManager.FireEvent(GameEvent.EVT_INIT_PROGRESS, 100f);
        }
    }

    [IOCComponent]
    class AssetLoader
    {
        public void LoadScene(string scenePath, Action<Scene> onLoadedScene)
        {
            onLoadedScene(SceneManager.GetActiveScene());
        }
    }
}