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

        private Dictionary<string, HashSet<Action>> _EventWithoutArg = new Dictionary<string, HashSet<Action>>();
        private Dictionary<string, HashSet<Action<int>>> _EventWithIntArg = new Dictionary<string, HashSet<Action<int>>>();
        private Dictionary<string, HashSet<Action<long>>> _EventWithLongArg = new Dictionary<string, HashSet<Action<long>>>();
        private Dictionary<string, HashSet<Action<bool>>> _EventWithBoolArg = new Dictionary<string, HashSet<Action<bool>>>();
        private Dictionary<string, HashSet<Action<float>>> _EventWithFloatArg = new Dictionary<string, HashSet<Action<float>>>();
        private Dictionary<string, HashSet<Action<double>>> _EventWithDoubleArg = new Dictionary<string, HashSet<Action<double>>>();
        private Dictionary<string, HashSet<Action<string>>> _EventWithStringArg = new Dictionary<string, HashSet<Action<string>>>();
        private Dictionary<string, HashSet<Action<object>>> _EventWithObjectArg = new Dictionary<string, HashSet<Action<object>>>();

        public void FireEvent(string evt)
        {
            if (!_EventWithoutArg.ContainsKey(evt))
            {
                return;
            }

            foreach (var action in _EventWithoutArg[evt])
            {
                action();
            }
        }

        public void FireEvent(string evt, int arg)
        {
            _FireEvent(evt, arg, _EventWithIntArg);
        }

        public void FireEvent(string evt, bool arg)
        {
            _FireEvent(evt, arg, _EventWithBoolArg);
        }

        public void FireEvent(string evt, float arg)
        {
            _FireEvent(evt, arg, _EventWithFloatArg);
        }

        public void FireEvent(string evt, double arg)
        {
            _FireEvent(evt, arg, _EventWithDoubleArg);
        }

        public void FireEvent(string evt, object arg)
        {
            _FireEvent(evt, arg, _EventWithObjectArg);
        }

        public void FireEvent(string evt, string arg)
        {
            _FireEvent(evt, arg, _EventWithStringArg);
        }

        public void FireEvent(string evt, long arg)
        {
            _FireEvent(evt, arg, _EventWithLongArg);
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
                var instance = eventMethod.Instance;
                foreach (var methodInfo in eventMethod.Methods)
                {
                    Type parameterType;
                    if (!_CheckParameter(methodInfo, out string error, out parameterType))
                    {
                        Debug.LogError(error);
                        continue;
                    }

                    if (parameterType == null)
                    {
                        Action action = (Action) Delegate.CreateDelegate(typeof(Action), instance, methodInfo);
                        _RegisterEvent(action, _EventWithoutArg, methodInfo.GetCustomAttributes<Event>());
                    }
                    else if (parameterType == typeof(int))
                    {
                        _RegisterEvent(_Delegate<int>(instance, methodInfo), _EventWithIntArg, methodInfo.GetCustomAttributes<Event>());
                    }
                    else if (parameterType == typeof(long))
                    {
                        _RegisterEvent(_Delegate<long>(instance, methodInfo), _EventWithLongArg, methodInfo.GetCustomAttributes<Event>());
                    }
                    else if (parameterType == typeof(bool))
                    {
                        _RegisterEvent(_Delegate<bool>(instance, methodInfo), _EventWithBoolArg, methodInfo.GetCustomAttributes<Event>());
                    }
                    else if (parameterType == typeof(float))
                    {
                        _RegisterEvent(_Delegate<float>(instance, methodInfo), _EventWithFloatArg, methodInfo.GetCustomAttributes<Event>());
                    }
                    else if (parameterType == typeof(double))
                    {
                        _RegisterEvent(_Delegate<double>(instance, methodInfo), _EventWithDoubleArg, methodInfo.GetCustomAttributes<Event>());
                    }
                    else if (parameterType == typeof(string))
                    {
                        _RegisterEvent(_Delegate<string>(instance, methodInfo), _EventWithStringArg, methodInfo.GetCustomAttributes<Event>());
                    }
                    else 
                    {
                        _RegisterEvent(_DelegateObject(instance, methodInfo), _EventWithObjectArg, methodInfo.GetCustomAttributes<Event>());
                    }
                }
            }
        }

        private void _RegisterEvent<T>(Action<T> action, Dictionary<string, HashSet<Action<T>>> eventDict, IEnumerable<Event> events)
        {
            foreach (var evt in events)
            {
                HashSet<Action<T>> evtActions;
                if (eventDict.ContainsKey(evt.Evt))
                {
                    evtActions = eventDict[evt.Evt];
                }
                else
                {
                    evtActions = new HashSet<Action<T>>();
                    eventDict.Add(evt.Evt, evtActions);
                }
                evtActions.Add(action);
            }
        }

        private void _RegisterEvent(Action action, Dictionary<string, HashSet<Action>> eventDict, IEnumerable<Event> events)
        {
            foreach (var evt in events)
            {
                HashSet<Action> evtActions;
                if (eventDict.ContainsKey(evt.Evt))
                {
                    evtActions = eventDict[evt.Evt];
                }
                else
                {
                    evtActions = new HashSet<Action>();
                    eventDict.Add(evt.Evt, evtActions);
                }

                evtActions.Add(action);
            }
        }

        private Action<T> _Delegate<T>(object instance, MethodInfo methodInfo)
        {
            var methodDelegate = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(typeof(T)), instance, methodInfo);
            return (Action<T>) methodDelegate;
        }

        private Action<object> _DelegateObject(object instance, MethodInfo methodInfo)
        {
            Action<object> action = (obj) => methodInfo.Invoke(instance, new [] {obj});
            return action;
        }

        private bool _CheckParameter(MethodInfo methodInfo, out string s, out Type type)
        {
            s = null;
            type = null;
            var parameters = methodInfo.GetParameters();
            if (parameters.Length > 1)
            {
                s = $"Parameter count of an event method mustn't more than one! Type={methodInfo.DeclaringType} Name={methodInfo.Name} Parameter=${parameters.Length}";
                return false;
            }

            if (parameters.Length == 1)
            {
                type = parameters[0].ParameterType;
            }

            return true;
        }

        private void _FireEvent<T>(string evt, T arg, Dictionary<string, HashSet<Action<T>>> events)
        {
            if (!events.ContainsKey(evt))
            {
                return;
            }

            foreach (var action in events[evt])
            {
                action(arg);
            }
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
    }
}