﻿using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ChangeTracking
{
    internal class ComplexPropertyInterceptor<T> : IInterceptor, IInterceptorSettings
    {
        private static readonly List<PropertyInfo> _Properties;
        private static Dictionary<string, Action<IInvocation, Dictionary<string, object>, object, bool, bool>> _Actions;
        private readonly Dictionary<string, object> _Trackables;
        private readonly object _TrackablesLock;
        private readonly bool _MakeComplexPropertiesTrackable;
        private readonly bool _MakeCollectionPropertiesTrackable;
        private bool _AreAllPropertiesTrackable;

        public bool IsInitialized { get; set; }

        static ComplexPropertyInterceptor()
        {
            _Actions = new Dictionary<string, Action<IInvocation, Dictionary<string, object>, object, bool, bool>>();
            _Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            var getters = _Properties.Where(pi => pi.CanRead).Select(pi => new KeyValuePair<string, Action<IInvocation, Dictionary<string, object>, object, bool, bool>>(pi.Name, GetGetterAction(pi)));
            foreach (var getter in getters)
            {
                _Actions.Add("get_" + getter.Key, getter.Value);
            }
            var setters = _Properties.Where(pi => pi.CanWrite).Select(pi => new KeyValuePair<string, Action<IInvocation, Dictionary<string, object>, object, bool, bool>>(pi.Name, GetSetterAction(pi)));
            foreach (var setter in setters)
            {
                _Actions.Add("set_" + setter.Key, setter.Value);
            }
        }

        internal ComplexPropertyInterceptor(bool makeComplexPropertiesTrackable, bool makeCollectionPropertiesTrackable)
        {
            _MakeComplexPropertiesTrackable = makeComplexPropertiesTrackable;
            _MakeCollectionPropertiesTrackable = makeCollectionPropertiesTrackable;
            _Trackables = new Dictionary<string, object>();
            _TrackablesLock = new object();
        }

        private static Action<IInvocation, Dictionary<string, object>, object, bool, bool> GetGetterAction(PropertyInfo propertyInfo)
        {
            if (CanComplexPropertyBeTrackable(propertyInfo))
            {
                return (invocation, trackables, trackablesLock, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
                {
                    string propertyName = invocation.Method.PropertyName();
                    lock (trackablesLock)
                    {
                        if (!trackables.ContainsKey(propertyName))
                        {
                            object childTarget = propertyInfo.GetValue(invocation.InvocationTarget, null);
                            if (childTarget == null)
                            {
                                return;
                            }
                            trackables.Add(propertyName, Core.AsTrackableChild(propertyInfo.PropertyType, childTarget, null, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable));
                        }
                        invocation.ReturnValue = trackables[propertyName];
                    }
                };
            }
            return (invocation, _, __, ___, ____) =>
            {
                invocation.Proceed();
            };
        }

        private static Action<IInvocation, Dictionary<string, object>, object, bool, bool> GetSetterAction(PropertyInfo propertyInfo)
        {
            if (CanComplexPropertyBeTrackable(propertyInfo))
            {
                return (invocation, trackables, trackablesLock, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable) =>
                {
                    string parentPropertyName = invocation.Method.PropertyName();
                    invocation.Proceed();

                    bool lockWasTaken = false;
                    try
                    {
                        object childTarget = invocation.Arguments[0];
                        object newValue;
                        if (childTarget == null)
                        {
                            newValue = null;
                        }
                        else if (childTarget.GetType().GetInterfaces().FirstOrDefault(t => t == typeof(IChangeTrackable<>)) != null)
                        {
                            newValue = invocation.Arguments[0];
                        }
                        else
                        {
                            Monitor.Enter(trackablesLock, ref lockWasTaken);
                            newValue = Core.AsTrackableChild(propertyInfo.PropertyType, childTarget, null, makeComplexPropertiesTrackable, makeCollectionPropertiesTrackable);
                        }
                        if (!lockWasTaken)
                        {
                            Monitor.Enter(trackablesLock, ref lockWasTaken);
                        }
                        trackables[parentPropertyName] = newValue;
                    }
                    finally
                    {
                        if (lockWasTaken)
                        {
                            Monitor.Exit(trackablesLock);
                        }
                    }
                };
            }
            return (invocation, _, __, ___, ____) =>
            {
                invocation.Proceed();
            };
        }

        private static bool CanComplexPropertyBeTrackable(PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            return propertyType.IsClass &&
                !propertyType.IsSealed &&
                propertyType.GetConstructor(Type.EmptyTypes) != null &&
                propertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).All(pi => pi.GetAccessors()[0].IsVirtual);
        }

        public void Intercept(IInvocation invocation)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (invocation.Method.Name == "get_ComplexPropertyTrackables")
            {
                invocation.ReturnValue = ComplexPropertyTrackables(invocation.Proxy);
                return;
            }
            if (_MakeComplexPropertiesTrackable && _Actions.TryGetValue(invocation.Method.Name, out Action<IInvocation, Dictionary<string, object>, object, bool, bool> action))
            {
                action(invocation, _Trackables, _TrackablesLock, _MakeComplexPropertiesTrackable, _MakeCollectionPropertiesTrackable);
            }
            else
            {
                invocation.Proceed();
            }
        }

        private IEnumerable<object> ComplexPropertyTrackables(object proxy)
        {
            if (!_MakeComplexPropertiesTrackable)
            {
                return Enumerable.Empty<object>();
            }
            if (!_AreAllPropertiesTrackable)
            {
                MakeAllPropertiesTrackable(proxy);
            }
            lock (_TrackablesLock)
            {
                return _Trackables.Values.ToArray();
            }
        }

        private void MakeAllPropertiesTrackable(object proxy)
        {
            var notTrackedProperties = _Properties.Where(pi => !_Trackables.ContainsKey(pi.Name) && CanComplexPropertyBeTrackable(pi));
            foreach (var property in notTrackedProperties)
            {
                property.GetValue(proxy, null);
            }
            _AreAllPropertiesTrackable = true;
        }
    }
}
