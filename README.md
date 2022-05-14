# IOC-Unity-Event Library in Source Framework for Unity3D

The `EventManager` for `IOCContainer`.

# Add dependencies

You can add package from git url through the Package Manager.

All the following package should be added.

|Package|Description|
|--|--|
|[https://github.com/kakashiio/Unity-Reflection.git#1.0.0](https://github.com/kakashiio/Unity-Reflection.git#1.0.0)|Reflection Library|
|[https://github.com/kakashiio/Unity-IOC.git#1.0.0](https://github.com/kakashiio/Unity-IOC.git#1.0.0)|IOC Library|
|[https://github.com/kakashiio/Unity-IOC-Event.git#1.0.0](https://github.com/kakashiio/Unity-IOC-Event.git#1.0.0)|IOC-Event Library|

# Tutorial

[中文向导](https://unity3d.io/2022/05/10/Unity-IOC-Event/)

# Sample : Global Event Sample

Imagine we want to implement the following game flow:

1. Fire `EventInit` while `IOCContainer` finish its initialization
2. `GameFlowController` listen & react to all events
    1. While `EventInit` event is receieved
        1. Print `[GameFlow] OnInit`
        2. Fire `EventFinishInit`
    2. While `EventFinishInit` event is receieved
        1. Print `[GameFlow] OnFinishInit`
        2. Fire `EventLoadingMain` with progress 0
        3. Start loading the scene
    3. While `EventLoadingMain` event is receieved
        1. Print `[GameFlow] OnLoadingMain progress`
    4. While `EventLoadedScene` event is receieved
        1. Print `[GameFlow] OnEnterScene scene=Current Active Scene`

## Create an `IOCContainer` 

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

## Define all events first

```csharp
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
```

## `Game` to react the `IOCContainer`'s life cycle'

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
        _EventManager.FireEvent<EventInit>();
    }
}
```

## `GameFlowController` to listen & react the event

```csharp
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
```

## `AssetLoader` to simulate the asset loader

```csharp
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
```

## All is done

Runing the demo and the following messages will be output in the console:

```
[GameFlow] OnInit
[GameFlow] OnFinishInit
[GameFlow] OnLoadingMain 0
[GameFlow] OnLoadingMain 100
[GameFlow] OnEnterScene scene=UnityEngine.SceneManagement.Scene
```

Amazing! Right? The `IOCContainer` & `EventManager` help you to register all the methods with `Event` attribute in the `IOCContainer` into the `EventManager`, so that when a `EventManager`'s `FireEvent` is invoke, the relevant method will be invoked automatically.

This sample is useful for global event,such as network message or game's data message which you want to update all the time even though whether the associate UI is open.

Sometimes you need some local event but not global, such the UI will listen the data's event only when the UI is opened. That is what we want to demonstrate the next sample.

# Sample : Local Event Sample

Imagine we want to implement the following game flow:

After the `IOCContainer` was inited:

1. Open `BagUI` through `UIManager`
2. Fire `EventBagItemDataChange` with `ID`=`1` and `Count`=`100`
3. Fire `EventBagItemDataChange` with `ID`=`1` and `Count`=`200`
4. Fire `EventBagDeleteItem` with `ID`=`1` 
5. Close `BagUI` through `UIManager`
6. Fire `EventBagItemDataChange` with `ID`=`1` and `Count`=`300`
7. Fire `EventBagDeleteItem` with `ID`=`2` 

> Let's see whether the `BagUI` can receieve the events in `6th` & `7th` step


## Create an IOCContainer

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

## Define all events first

```csharp
class EventBagItemDataChange : EventArg
{
    public int ID;
    public int Count;

    public EventBagItemDataChange(int id, int count)
    {
        ID = id;
        Count = count;
    }

    public override string ToString()
    {
        return $"ID={ID} Count={Count}";
    }
}

class EventBagDeleteItem : EventArg
{
    public int ID;
    
    public EventBagDeleteItem(int id)
    {
        ID = id;
    }

    public override string ToString()
    {
        return $"ID={ID}";
    }
}
```

## Define `BagUI`

```csharp
class BagUI
{
    [Event]
    public void OnBagDataChange(EventBagItemDataChange eventBagItemDataChange)
    {
        Debug.LogError($"[OnBagDataChange] {eventBagItemDataChange}");
    }
    [Event]
    public void OnEventBagDeleteItem(EventBagDeleteItem eventBagDeleteItem)
    {
        Debug.LogError($"[OnEventBagDeleteItem] {eventBagDeleteItem}");
    }
}
```

## Define `UIManager`

```csharp
[IOCComponent]
class UIManager 
{
    [Autowired]
    private EventManager _EventManager;

    private IIOCContainer _IOCContainer;

    private Dictionary<Type, object> _UIs = new Dictionary<Type, object>();

    public void Open<T>(Action<T> onOpened) where T : new()
    {
        var uiType = typeof(T);
        if (_UIs.ContainsKey(uiType))
        {
            onOpened((T) _UIs[uiType]);
            return;
        }
        var t = new T();
        _EventManager.Register(t);
        _UIs.Add(uiType, t);
        onOpened.Invoke(t);
    }

    public void Close<T>()
    {
        var uiType = typeof(T);
        if (!_UIs.ContainsKey(uiType))
        {
            return;
        }

        var ui = _UIs[uiType];
        _UIs.Remove(uiType);
        _EventManager.Unregister(ui);
    }
}
```

We can each time the `Open` method is invoked, we will `Register` the new create ui object to the `EventManager`, the `Unregister` method will be invoked if the `Close` method is invoked. We do not need to register all the events in the ui one by one, just register the ui object into the `EventManager` and then all the methods with `Event` attribute in the ui object will be register into the `EventManager`.

## `Game`

```csharp
[IOCComponent]
class Game : IInstanceLifeCycle
{
    [Autowired]
    private UIManager _UIManager;
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
        _UIManager.Open<BagUI>((ui) => { });
        _EventManager.FireEvent(new EventBagItemDataChange(1, 100));
        _EventManager.FireEvent(new EventBagItemDataChange(1, 200));
        _EventManager.FireEvent(new EventBagDeleteItem(1));
        _UIManager.Close<BagUI>();
        _EventManager.FireEvent(new EventBagItemDataChange(1, 300));
        _EventManager.FireEvent(new EventBagDeleteItem(2));
    }
}
```

## All is done

Runing the demo and the following messages will be output in the console:

```
[OnBagDataChange] ID=1 Count=100
[OnBagDataChange] ID=1 Count=200
[OnEventBagDeleteItem] ID=1
```

We can find that after we call `_UIManager.Close<BagUI>()`, `BagUI` doesn't react to the last two events, that is what we want.