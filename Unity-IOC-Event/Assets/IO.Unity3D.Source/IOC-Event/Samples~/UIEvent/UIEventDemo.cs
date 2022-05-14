using System;
using System.Collections.Generic;
using System.Reflection;
using IO.Unity3D.Source.IOC;
using IO.Unity3D.Source.Reflection;
using UnityEngine;

namespace IO.Unity3D.Source.IOCEvent.Samples.UIEvent
{

    public class UIEventDemo : MonoBehaviour
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
}