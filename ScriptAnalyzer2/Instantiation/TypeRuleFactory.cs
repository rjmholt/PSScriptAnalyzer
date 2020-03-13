using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Management.Automation.Language;
using System.Reflection;
using System.Threading;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public abstract class TypeRuleFactory<TRule>
    {
        public TypeRuleFactory(RuleInfo ruleInfo)
        {
            RuleInfo = ruleInfo;
        }

        public RuleInfo RuleInfo { get; }

        public abstract TRule GetRuleInstance();

        public abstract void ReturnRuleInstance(TRule rule);
    }

    public class ConstructorInjectingRuleFactory<TRule> : TypeRuleFactory<TRule>
    {
        private readonly RuleInfo _ruleInfo;

        private readonly ConstructorInfo _ctorInfo;

        private readonly IRuleConfiguration _ruleConfiguration;

        private readonly Lazy<object[]> _ctorArgsLazy;

        private readonly Lazy<Func<TRule>> _factoryDelegateLazy;

        private int _callCount;

        public ConstructorInjectingRuleFactory(
            RuleInfo ruleInfo,
            ConstructorInfo ctorInfo)
            : this(ruleInfo, ctorInfo, ruleConfiguration: null)
        {
        }

        public ConstructorInjectingRuleFactory(
            RuleInfo ruleInfo,
            ConstructorInfo ctorInfo,
            IRuleConfiguration ruleConfiguration)
            : base(ruleInfo)
        {
            _ruleInfo = ruleInfo;
            _ctorInfo = ctorInfo;
            _ruleConfiguration = ruleConfiguration;
            _ctorArgsLazy = new Lazy<object[]>(GetCtorArgs);
            _factoryDelegateLazy = new Lazy<Func<TRule>>(CreateFactoryDelegate);
            _callCount = 0;
        }

        private object[] CtorArgs => _ctorArgsLazy.Value;

        private Func<TRule> FactoryDelegate => _factoryDelegateLazy.Value;

        public override TRule GetRuleInstance() => InstantiateRuleInstance();

        public override void ReturnRuleInstance(TRule rule)
        {
            // Do nothing
        }

        protected TRule InstantiateRuleInstance()
        {
            // If the rule is being run repeatedly, optimise the constructor invocation
            return Interlocked.Increment(ref _callCount) > 4
                ? FactoryDelegate()
                : (TRule)_ctorInfo.Invoke(CtorArgs);
        }

        private object[] GetCtorArgs()
        {
            var ctorArgs = new List<object>();
            foreach (ParameterInfo ctorParameter in _ctorInfo.GetParameters())
            {
                if (ctorParameter.ParameterType == typeof(RuleInfo))
                {
                    ctorArgs.Add(_ruleInfo);
                    continue;
                }

                if (_ruleConfiguration != null
                    && ctorParameter.ParameterType == _ruleConfiguration.GetType())
                {
                    ctorArgs.Add(_ruleConfiguration);
                    continue;
                }

                throw new ArgumentException($"Rule constructor requires unknown argument: '{ctorParameter.Name}' of type '{ctorParameter.ParameterType.FullName}'");
            }

            return ctorArgs.ToArray();
        }

        private Func<TRule> CreateFactoryDelegate()
        {
            var args = new Expression[CtorArgs.Length];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = Expression.Constant(CtorArgs[i]);
            }

            return Expression.Lambda<Func<TRule>>(
                Expression.New(
                    _ctorInfo,
                    args)).Compile();
        }
    }

    public class ConstructorInjectingDisposableRuleFactory<TRule> : ConstructorInjectingRuleFactory<TRule>
    {
        public ConstructorInjectingDisposableRuleFactory(RuleInfo ruleInfo, ConstructorInfo ctorInfo, IRuleConfiguration ruleConfiguration)
            : base(ruleInfo, ctorInfo, ruleConfiguration)
        {
        }

        public override void ReturnRuleInstance(TRule rule)
        {
            ((IDisposable)rule).Dispose();
        }
    }

    public class ConstructorInjectionIdempotentRuleFactory<TRule> : ConstructorInjectingRuleFactory<TRule>
    {
        private readonly TRule _instance;

        public ConstructorInjectionIdempotentRuleFactory(RuleInfo ruleInfo, ConstructorInfo ctorInfo, IRuleConfiguration ruleConfiguration)
            : base(ruleInfo, ctorInfo, ruleConfiguration)
        {
            _instance = InstantiateRuleInstance();
        }

        public override TRule GetRuleInstance()
        {
            return _instance;
        }
    }

    public class ConstructorInjectingResettableRulePoolingFactory<TRule> : ConstructorInjectingRuleFactory<TRule>
    {
        private readonly ResettablePool _pool;

        public ConstructorInjectingResettableRulePoolingFactory(RuleInfo ruleInfo, ConstructorInfo ctorInfo, IRuleConfiguration ruleConfiguration)
            : base(ruleInfo, ctorInfo, ruleConfiguration)
        {
            _pool = new ResettablePool(() => (IResettable)InstantiateRuleInstance());
        }

        public override TRule GetRuleInstance()
        {
            return (TRule)_pool.Take();
        }

        public override void ReturnRuleInstance(TRule rule)
        {
            _pool.Release((IResettable)rule);
        }
    }

    internal class ResettablePool
    {
        private readonly Func<IResettable> _factory;

        private readonly ConcurrentBag<IResettable> _instances;

        public ResettablePool(Func<IResettable> factory)
        {
            _factory = factory;
            _instances = new ConcurrentBag<IResettable>();
        }

        public IResettable Take()
        {
            if (_instances.TryTake(out IResettable instance))
            {
                return instance;
            }

            return _factory();
        }

        public void Release(IResettable instance)
        {
            instance.Reset();
            _instances.Add(instance);
        }
    }
}
