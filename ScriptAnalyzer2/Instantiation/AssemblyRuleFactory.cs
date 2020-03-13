using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class AssemblyRuleFactoryFactory
    {
        public static AssemblyRuleFactoryFactory FromPaths(
            string assemblyPath,
            string configurationPath)
        {
            Ast settingsAst = Parser.ParseFile(configurationPath, out Token[] _, out ParseError[] parseErrors);

            if (parseErrors.Length > 0)
            {
                throw new ArgumentException($"Parse errors occurred while parsing settings file");
            }

            var settingsHashtableAst = (HashtableAst)
                ((CommandExpressionAst)
                    ((PipelineAst)
                        ((ScriptBlockAst)settingsAst).EndBlock.Statements[0])
                    .PipelineElements[0])
                .Expression;

            Assembly ruleAssembly = Assembly.LoadFile(assemblyPath);

            return AssemblyRuleFactoryFactory.Create(ruleAssembly, settingsHashtableAst);
        }

        public static AssemblyRuleFactoryFactory Create(
            Assembly ruleAssembly,
            HashtableAst settingsAst)
        {
            var psdConverter = new PsdTypedObjectConverter();
            var settings = psdConverter.Convert<IReadOnlyDictionary<string, HashtableAst>>(settingsAst);
            return new AssemblyRuleFactoryFactory(psdConverter, ruleAssembly, settings);
        }

        private readonly PsdTypedObjectConverter _psdConverter;

        private readonly Assembly _ruleAssembly;

        private readonly IReadOnlyDictionary<string, HashtableAst> _configuration;

        private AssemblyRuleFactoryFactory(
            PsdTypedObjectConverter psdConverter,
            Assembly ruleAssembly,
            IReadOnlyDictionary<string, HashtableAst> configuration)
        {
            _psdConverter = psdConverter;
            _ruleAssembly = ruleAssembly;
            _configuration = configuration;
        }

        public AssemblyRuleFactory CreateAssemblyRuleFactory()
        {
            var astRuleFactories = new Dictionary<Type, TypeRuleFactory<AstRule>>();
            var tokenRuleFactories = new Dictionary<Type, TypeRuleFactory<TokenRule>>();

            Type genericAstRuleBaseType = typeof(AstRule<>);
            Type genericTokenRuleBaseType = typeof(TokenRule<>);

            foreach (Type exportedType in _ruleAssembly.GetExportedTypes())
            {
                if (!RuleInfo.TryGetFromRuleType(exportedType, out RuleInfo ruleInfo))
                {
                    continue;
                }

                if (typeof(AstRule).IsAssignableFrom(exportedType))
                {
                    if (TryGetRuleFactory(genericAstRuleBaseType, ruleInfo, exportedType, _configuration, out TypeRuleFactory<AstRule> factory))
                    {
                        astRuleFactories[exportedType] = factory;
                    }

                    continue;
                }

                if (typeof(TokenRule).IsAssignableFrom(exportedType))
                {
                    if (TryGetRuleFactory(genericTokenRuleBaseType, ruleInfo, exportedType, _configuration, out TypeRuleFactory<TokenRule> factory))
                    {
                        tokenRuleFactories[exportedType] = factory;
                    }

                    continue;
                }
            }

            return new AssemblyRuleFactory(astRuleFactories, tokenRuleFactories);
        }

        private bool TryGetRuleFactory<TRuleBase>(
            Type genericRuleBaseType,
            RuleInfo ruleInfo,
            Type ruleType,
            IReadOnlyDictionary<string, HashtableAst> ruleSettings,
            out TypeRuleFactory<TRuleBase> factory)
        {
            ConstructorInfo[] ruleConstructors = ruleType.GetConstructors();
            if (ruleConstructors.Length != 1)
            {
                factory = null;
                return false;
            }
            ConstructorInfo ruleConstructor = ruleConstructors[0];

            IRuleConfiguration ruleConfiguration = null;
            if (ruleSettings.TryGetValue(ruleInfo.Fullname, out HashtableAst settingsAst))
            {
                Type baseType = ruleType.BaseType;
                Type configurationType = null;
                while (baseType != null)
                {
                    if (baseType.IsGenericType
                        && baseType.GetGenericTypeDefinition() == genericRuleBaseType)
                    {
                        configurationType = baseType.GetGenericArguments()[0];
                        break;
                    }

                    baseType = baseType.BaseType;
                }

                ruleConfiguration = (IRuleConfiguration)_psdConverter.Convert(configurationType, settingsAst);
            }

            if (ruleInfo.IsIdempotent)
            {
                factory = new ConstructorInjectionIdempotentRuleFactory<TRuleBase>(
                    ruleInfo,
                    ruleConstructor,
                    ruleConfiguration);
                return true;
            }

            if (typeof(IResettable).IsAssignableFrom(ruleType))
            {
                factory = new ConstructorInjectingResettableRulePoolingFactory<TRuleBase>(
                    ruleInfo,
                    ruleConstructor,
                    ruleConfiguration);
                return true;
            }

            if (typeof(IDisposable).IsAssignableFrom(ruleType))
            {
                factory = new ConstructorInjectingDisposableRuleFactory<TRuleBase>(
                    ruleInfo,
                    ruleConstructor,
                    ruleConfiguration);
                return true;
            }

            factory = new ConstructorInjectingRuleFactory<TRuleBase>(
                ruleInfo,
                ruleConstructor,
                ruleConfiguration);
            return true;
        }
    }

    public class AssemblyRuleFactory : IRuleProvider
    {
        private readonly IReadOnlyDictionary<Type, TypeRuleFactory<AstRule>> _astRuleFactories;

        private readonly IReadOnlyDictionary<Type, TypeRuleFactory<TokenRule>> _tokenRuleFactories;

        internal AssemblyRuleFactory(
            IReadOnlyDictionary<Type, TypeRuleFactory<AstRule>> astRuleFactories,
            IReadOnlyDictionary<Type, TypeRuleFactory<TokenRule>> tokenRuleFactories)
        {
            _astRuleFactories = astRuleFactories;
            _tokenRuleFactories = tokenRuleFactories;
        }

        public IEnumerable<AstRule> GetAstRules()
        {
            foreach (TypeRuleFactory<AstRule> astRuleFactory in _astRuleFactories.Values)
            {
                yield return astRuleFactory.GetRuleInstance();
            }
        }

        public IEnumerable<TokenRule> GetTokenRules()
        {
            foreach (TypeRuleFactory<TokenRule> tokenRuleFactory in _tokenRuleFactories.Values)
            {
                yield return tokenRuleFactory.GetRuleInstance();
            }
        }

        public void ReturnRule(Rule rule)
        {
            if (_astRuleFactories.TryGetValue(rule.GetType(), out TypeRuleFactory<AstRule> astRuleFactory))
            {
                astRuleFactory.ReturnRuleInstance((AstRule)rule);
                return;
            }

            if (_tokenRuleFactories.TryGetValue(rule.GetType(), out TypeRuleFactory<TokenRule> tokenRuleFactory))
            {
                tokenRuleFactory.ReturnRuleInstance((TokenRule)rule);
                return;
            }
        }
    }
}
