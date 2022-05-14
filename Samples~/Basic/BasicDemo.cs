using System;
using System.Collections.Generic;
using System.Reflection;
using IO.Unity3D.Source.IOC;
using IO.Unity3D.Source.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IO.Unity3D.Source.IOCEvent.Samples.Basic
{

    public class BasicDemo : MonoBehaviour
    {
        void Start()
        {
            ITypeContainer typeContainer = new TypeContainerCollection(new List<ITypeContainer>
            {
                new TypeContainer(Assembly.GetExecutingAssembly()),
                new TypeContainer(typeof(IOCComponent).Assembly),
                new TypeContainer(typeof(EventManager).Assembly)
            });
            
            new IOCContainerBuilder(typeContainer).Build();
        }
    }

    class EventInit : EventArg {}
    
    class EventFinishInit : EventArg {}

    class EventLoadingMain : EventArg
    {
        public float Progress;

        public EventLoadingMain(float progress)
        {
            Progress = progress;
        }
    }
    
    class EventLoadedScene : EventArg
    {
        public Scene Scene;

        public EventLoadedScene(Scene scene)
        {
            Scene = scene;
        }
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
            _EventManager.FireEvent<EventInit>();
        }
    }

    [IOCComponent]
    class GameFlowController
    {
        [Autowired]
        private AssetLoader _AssetLoader;
        [Autowired]
        private EventManager _EventManager;
        
        [Event]
        public void OnInit(EventInit eventInit)
        {
            Debug.LogError("[GameFlow] OnInit");
            _EventManager.FireEvent<EventFinishInit>();
        }
        
        [Event]
        public void OnFinishInit(EventFinishInit eventFinishInit)
        {
            Debug.LogError("[GameFlow] OnFinishInit");
            _EventManager.FireEvent(new EventLoadingMain(0));
            _AssetLoader.LoadScene("ScenePath", (scene) =>
            {
                _EventManager.FireEvent(new EventLoadingMain(100));
            });
        }
        
        [Event]
        public void OnLoadingMain(EventLoadingMain eventLoadingMain)
        {
            Debug.LogError($"[GameFlow] OnLoadingMain {eventLoadingMain.Progress}");
        }
        
        [Event]
        public void OnEnterScene(EventLoadedScene eventLoadedScene)
        {
            Debug.LogError($"[GameFlow] OnEnterScene scene={eventLoadedScene.Scene}");
        }
    }

    [IOCComponent]
    class AssetLoader
    {
        [Autowired]
        private EventManager _EventManager;
        
        public void LoadScene(string scenePath, Action<Scene> onLoadedScene)
        {
            var scene = SceneManager.GetActiveScene();
            onLoadedScene(scene);
            _EventManager.FireEvent(new EventLoadedScene(scene));
        }
    }
}