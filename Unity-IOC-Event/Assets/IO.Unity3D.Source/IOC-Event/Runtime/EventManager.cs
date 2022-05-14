using System;
using System.Collections.Generic;
using System.Reflection;
using IO.Unity3D.Source.IOC;
using UnityEngine;

namespace IO.Unity3D.Source.IOCEvent
{
    //******************************************
    //  
    //
    // @Author: Kakashi
    // @Email: john.cha@qq.com
    // @Date: 2022-05-09 23:08
    //******************************************
    [IOCComponent]
    public class EventManager : IContainerLifeCycle, IInstanceLifeCycle
    {
        private IIOCContainer _IOCContainer;
        
        private Dictionary<Type, List<Action<EventArg>>> _Listeners = new Dictionary<Type, List<Action<EventArg>>>();
        private Dictionary<object, object> _Src2Wrapper = new Dictionary<object, object>();

        private int _FiringEvent;
        private List<Action> _DelayProcessings = new List<Action>();
        
        public void Register<T>(Action<T> listener) where T : EventArg
        {
            if (listener == null)
            {
                return;
            }
            _Register(listener);
        }

        public void Unregister<T>(Action<T> listener) where T : EventArg
        {
            if (_Src2Wrapper.ContainsKey(listener))
            {
                return;
            }

            _Unregister(listener);
        }
        
        public void Register(object instance) 
        {
            if (instance == null)
            {
                return;
            }
            _Register(_IOCContainer.FindMethods(instance, typeof(Event)));
        }
        
        public void Unregister(object instance) 
        {
            if (instance == null)
            {
                return;
            }

            var instanceMethods = _IOCContainer.FindMethods(instance, typeof(Event));
            _Unregister(instanceMethods);
        }

        public void FireEvent<T>(T eventArg) where T : EventArg
        {
            var type = typeof(T);
            if (!_Listeners.ContainsKey(type))
            {
                return;
            }

            IncreaseFiring();
            var listeners = _Listeners[type];
            foreach (var listener in listeners)
            {
                listener(eventArg);
            }
            DecreaseFiring();
        }
        
        public void FireEvent<T>() where T : EventArg
        {
            FireEvent(default(T));
        }

        private void _Unregister<T>(Action<T> listener) where T : EventArg
        {
            if (_FiringEvent == 0)
            {
                _UnregisterDirectly(listener);
            }
            else
            {
                _DelayProcessings.Add(() => _UnregisterDirectly(listener));
            }
        }
        
        private void _Unregister(InstanceMethods instanceMethods)
        {
            foreach (var methodInfo in instanceMethods.Methods)
            {
                _Unregister(instanceMethods.Instance, methodInfo);
            }
        }

        private void _Unregister(object instance, MethodInfo methodInfo)
        {
            if (_FiringEvent == 0)
            {
                _UnregisterDirectly(instance, methodInfo);
            }
            else
            {
                _DelayProcessings.Add(() => _UnregisterDirectly(instance, methodInfo));
            }
        }

        private void _UnregisterDirectly(object instance, MethodInfo methodInfo)
        {
            var instanceMethod = new InstanceMethod(methodInfo, instance);
            if (!_Src2Wrapper.ContainsKey(instanceMethod))
            {
                return;
            }

            var action = (Action<EventArg>)_Src2Wrapper[instanceMethod];
            _Src2Wrapper.Remove(instanceMethod);
            _GetOrCreateListenerCollection(methodInfo.GetParameters()[0].ParameterType).Remove(action);
        }

        private void _UnregisterDirectly<T>(Action<T> listener) where T : EventArg
        {
            var wrapper = (Action<EventArg>) _Src2Wrapper[listener];
            _Src2Wrapper.Remove(listener);

            var type = typeof(T);
            if (!_Listeners.ContainsKey(type))
            {
                return;
            }

            _Listeners[type].Remove(wrapper);
        }
        
        public void OnContainerAware(IIOCContainer iocContainer)
        {
            _IOCContainer = iocContainer;
        }

        public void BeforePropertiesOrFieldsSet()
        {
            List<InstanceMethods> eventMethods = _IOCContainer.FindMethods<Event>();
            foreach (var eventMethod in eventMethods)
            {
                _Register(eventMethod, false);
            }
        }

        private void _Register(InstanceMethods eventMethod, bool saveWrapperMapping = true)
        {
            var instance = eventMethod.Instance;
            foreach (var methodInfo in eventMethod.Methods)
            {
                Type parameterType;
                if (!_CheckParameter(methodInfo, out string error, out parameterType))
                {
                    Debug.LogError(error);
                    continue;
                }
                _Register(instance, methodInfo, saveWrapperMapping);
            }
        }

        private void _Register(object instance, MethodInfo methodInfo, bool saveWrapperMapping)
        {
            if (_FiringEvent == 0)
            {
                _RegisterDirectly(instance, methodInfo, saveWrapperMapping);
            }
            else
            {
                _DelayProcessings.Add(() => _RegisterDirectly(instance, methodInfo, saveWrapperMapping));
            }
        }

        private void _RegisterDirectly(object instance, MethodInfo methodInfo, bool saveWrapperMapping)
        {
            var action = _Delegate(instance, methodInfo);
            if (saveWrapperMapping)
            {
                _Src2Wrapper.Add(new InstanceMethod(methodInfo, instance), action);
            }
            var type = methodInfo.GetParameters()[0].ParameterType;
            List<Action<EventArg>> actions = _GetOrCreateListenerCollection(type);
            actions.Add(action);
        }

        private void _Register<T>(Action<T> listener) where T : EventArg
        {
            if (_FiringEvent == 0)
            {
                _RegisterDirectly(listener);
            }
            else
            {
                _DelayProcessings.Add(() => _RegisterDirectly(listener));
            }
        }

        private void _RegisterDirectly<T>(Action<T> listener)  where T : EventArg
        {
            var eventType = typeof(T);
            var actions = _GetOrCreateListenerCollection(eventType);
            Action<EventArg> action = (evtArgs) => { listener((T) evtArgs); };
            _Src2Wrapper.Add(listener, action);
            actions.Add(action);
        }

        private List<Action<EventArg>> _GetOrCreateListenerCollection(Type type)
        {
            List<Action<EventArg>> actions;
            if (_Listeners.ContainsKey(type))
            {
                actions = _Listeners[type];
            }
            else
            {
                actions = new List<Action<EventArg>>();
                _Listeners.Add(type, actions);
            }

            return actions;
        }

        private Action<EventArg> _Delegate(object instance, MethodInfo methodInfo)
        {
            Action<EventArg> action = (obj) => methodInfo.Invoke(instance, new [] {obj});
            return action;
        }

        private bool _CheckParameter(MethodInfo methodInfo, out string s, out Type type)
        {
            s = null;
            type = null;
            var parameters = methodInfo.GetParameters();
            if (parameters.Length == 1)
            {
                type = parameters[0].ParameterType;
                if (!typeof(EventArg).IsAssignableFrom(type))
                {
                    s = $"Event method's must have one parameter which is the subclass of {nameof(EventArg)}, but {methodInfo} does not";
                    return false;
                }
                return true;
            }

            s = $"Parameter mu";
            return false;
        }

        private void _ProcessDelayProcessing()
        {
            if (_DelayProcessings.Count == 0)
            {
                return;
            }

            foreach (var processing in _DelayProcessings)
            {
                processing();
            }
            _DelayProcessings.Clear();
        }

        public void AfterAllInstanceInit()
        {
        }

        public void OnContainerDestroy(IIOCContainer iocContainer)
        {
        }

        public void AfterPropertiesOrFieldsSet()
        {
        }

        public void IncreaseFiring()
        {
            _FiringEvent++;
        }
        public void DecreaseFiring()
        {
            _FiringEvent--;
            if (_FiringEvent == 0)
            {
                _ProcessDelayProcessing();
            }
        }

        private class InstanceMethod
        {
            private MethodInfo _MethodInfo;
            private object _Instance;

            public InstanceMethod(MethodInfo methodInfo, object instance)
            {
                _MethodInfo = methodInfo;
                _Instance = instance;
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }

                if (obj == this)
                {
                    return true;
                }

                var instanceMethod = obj as InstanceMethod;
                if (instanceMethod == null)
                {
                    return false;
                }
                
                return _MethodInfo.Equals(instanceMethod._MethodInfo) && _Instance.Equals(instanceMethod._Instance);
            }

            public override int GetHashCode()
            {
                return _MethodInfo.GetHashCode() ^ _Instance.GetHashCode();
            }
        }
    }
}