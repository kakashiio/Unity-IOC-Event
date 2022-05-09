# IOC-Unity-Event Library in Source Framework for Unity3D

The `EventManager` for `IOCContainer`.

# Tutorial

[中文向导](https://unity3d.io/2022/05/10/Unity-IOC-Event/)

## Add dependencies

You can add package from git url through the Package Manager.

All the following package should be added.

|Package|Description|
|--|--|
|[https://github.com/kakashiio/Unity-Reflection.git#1.0.0](https://github.com/kakashiio/Unity-Reflection.git#1.0.0)|Reflection Library|
|[https://github.com/kakashiio/Unity-IOC.git#1.0.0](https://github.com/kakashiio/Unity-IOC.git#1.0.0)|IOC Library|
|[https://github.com/kakashiio/Unity-IOC-Event.git#1.0.0](https://github.com/kakashiio/Unity-IOC-Event.git#1.0.0)|IOC-Event Library|

## Create a `IOCContainer` 

```csharp
ITypeContainer typeContainer = new TypeContainerCollection(new List<ITypeContainer>
    {
        new TypeContainer(Assembly.GetExecutingAssembly()),
        new TypeContainer(typeof(IOCComponent).Assembly),
        new TypeContainer(typeof(EventManager).Assembly)
    });

    new IOCContainerBuilder(typeContainer).Build();
```

Add the code above at the first your application is startup.

## Examples

We want to implement a game flow:

1. Fire a `EVT_INIT_PROGRESS` event with progress 0 and fire a `EVT_INIT` event at the same time after the `IOCContainer` has been inited.
2. `GameFlowController` listen all the events and react to them. 
    1. When the `EVT_INIT_PROGRESS` event is receieved, print the progress.
    2. When the `EVT_INIT` event is receieved, print infomation and start loading scene and fire a `EVT_LOAD_INIT_SCENE` while finish loading the scene.
    3. When the `EVT_LOAD_INIT_SCENE` event is receieved, print infomation and fire a `EVT_INIT_PROGRESS` event with progress 100

### Let's define `GameEvent` first

```csharp
class GameEvent
{
    public const string EVT_INIT = nameof(EVT_INIT);
    public const string EVT_LOAD_INIT_SCENE = nameof(EVT_LOAD_INIT_SCENE);
    public const string EVT_INIT_PROGRESS = nameof(EVT_INIT_PROGRESS);
}
```

### Define `Game` to react the `IOCContainer`'s life cycle

```csharp
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
```

### `GameFlowController` to react to the event

```csharp
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
```

### `AssetLoader` to simulate the asset loader

```csharp
[IOCComponent]
class AssetLoader
{
    public void LoadScene(string scenePath, Action<Scene> onLoadedScene)
    {
        onLoadedScene(SceneManager.GetActiveScene());
    }
}
```

## All is done

Run the scene and you will see the output in the console:

```
GameFlow OnProgress 0
GameFlow OnInit
GameFlow OnEnterInitScene scene=UnityEngine.SceneManagement.Scene
GameFlow OnProgress 100
```

Amazing! Right? The `IOCContainer` & `EventManager` help you to register all the methods with `Event` attribute in the `IOCContainer` into the `EventManager`, so that when a `EventManager`'s `FireEvent` is invoke, the relevant method will be invoked automatically.

## Run Samples

You can also import Samples From Package Manager and run `Basic.unity` scene.

The basic usage of `UnityIOCContainer` is in the sample script `BasicDemo.cs`

# Future

Will increase more Unity API and Component for game development requirement. Looking forward to your suggestion if you have some requirements.
