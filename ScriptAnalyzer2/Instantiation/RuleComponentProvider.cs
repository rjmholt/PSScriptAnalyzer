﻿using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public abstract class RuleComponentProvider
    {
        public bool TryGetComponentInstance<TComponent>(out TComponent component)
        {
            if (!TryGetComponentInstance(typeof(TComponent), out object componentObj))
            {
                component = default(TComponent);
                return false;
            }

            component = (TComponent)componentObj;
            return true;
        }

        public abstract bool TryGetComponentInstance(Type componentType, out object component);
    }

    internal class SimpleRuleComponentProvider : RuleComponentProvider
    {
        private readonly IReadOnlyDictionary<Type, Func<object>> _componentRegistrations;

        private readonly IReadOnlyDictionary<Type, IValueBox<object>> _singletonComponents;

        public SimpleRuleComponentProvider(
            IReadOnlyDictionary<Type, Func<object>> componentRegistrations,
            IReadOnlyDictionary<Type, IValueBox<object>> singletonComponents)
        {
            _componentRegistrations = componentRegistrations;
            _singletonComponents = singletonComponents;
        }

        public override bool TryGetComponentInstance(Type componentType, out object component)
        {
            if (_singletonComponents.TryGetValue(componentType, out IValueBox<object> lazyComponent))
            {
                component = lazyComponent.Value;
                return true;
            }

            if (_componentRegistrations.TryGetValue(componentType, out Func<object> componentFactory))
            {
                component = componentFactory();
                return true;
            }

            component = null;
            return false;
        }
    }

    public class RuleComponentProviderBuilder
    {
        private readonly Dictionary<Type, IValueBox<object>> _singletonComponents;

        private readonly Dictionary<Type, Func<object>> _componentRegistrations;

        public RuleComponentProviderBuilder()
        {
            _singletonComponents = new Dictionary<Type, IValueBox<object>>();
            _componentRegistrations = new Dictionary<Type, Func<object>>();
        }

        public RuleComponentProviderBuilder AddSingleton<T>() where T : class, new() => AddSingleton<T, T>();

        public RuleComponentProviderBuilder AddSingleton<T>(T instance) where T : class => AddSingleton<T, T>(instance);

        public RuleComponentProviderBuilder AddSingleton<T>(Func<T> factory) where T : class => AddSingleton<T, T>(factory);

        public RuleComponentProviderBuilder AddSingleton<TRegistered, TInstance>() where TInstance : class, TRegistered, new()
        {
            _singletonComponents[typeof(TRegistered)] = new StrictValueBox<TInstance>(new TInstance());
            return this;
        }

        public RuleComponentProviderBuilder AddSingleton<TRegistered, TInstance>(TInstance instance) where TInstance : class, TRegistered
        {
            _singletonComponents[typeof(TRegistered)] = new StrictValueBox<TInstance>(instance);
            return this;
        }

        public RuleComponentProviderBuilder AddSingleton<TRegistered, TInstance>(Func<TInstance> factory) where TInstance : class, TRegistered
        {
            _singletonComponents[typeof(TRegistered)] = new LazyValueBox<TInstance>(factory);
            return this;
        }

        public RuleComponentProviderBuilder AddSingleton(Type registeredType, object instance)
        {
            if (!registeredType.IsAssignableFrom(instance.GetType()))
            {
                throw new ArgumentException($"Cannot register object '{instance}' of type '{instance.GetType()}' for type '{registeredType}'");
            }

            _singletonComponents[registeredType] = new StrictValueBox<object>(instance);
            return this;
        }

        public RuleComponentProviderBuilder AddScoped<T>() where T : new()
        {
            _componentRegistrations[typeof(T)] = () => new T();
            return this;
        }

        public RuleComponentProviderBuilder AddScoped<T>(Func<T> factory) where T : class
        {
            _componentRegistrations[typeof(T)] = factory;
            return this;
        }

        public RuleComponentProviderBuilder AddScoped<TRegistered, TInstance>() where TInstance : TRegistered, new()
        {
            _componentRegistrations[typeof(TRegistered)] = () => new TInstance();
            return this;
        }

        public RuleComponentProviderBuilder AddScoped<TRegistered, TInstance>(Func<TInstance> factory) where TInstance : class, TRegistered 
        {
            _componentRegistrations[typeof(TRegistered)] = factory;
            return this;
        }

        public RuleComponentProvider Build()
        {
            return new SimpleRuleComponentProvider(_componentRegistrations, _singletonComponents);
        }
    }
}
