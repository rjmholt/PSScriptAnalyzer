﻿using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class AssemblyRuleProvider : IRuleProvider
    {
        public static AssemblyRuleProvider FromAssembly(string assemblyPath)
        {
            return FromAssembly(Assembly.LoadFile(assemblyPath));
        }

        public static AssemblyRuleProvider FromAssembly(Assembly assembly)
        {
            return new AssemblyRuleProvider(assembly);
        }

        private readonly Assembly _assembly;

        private AssemblyRuleProvider(Assembly assembly)
        {
            _assembly = assembly;
        }

        public IEnumerable<IAstRule> GetAstRules()
        {
            var rules = new List<IAstRule>();
            foreach (Type type in _assembly.GetTypes())
            {
                if (typeof(IAstRule).IsAssignableFrom(type))
                {
                    try
                    {
                        IAstRule rule = (IAstRule)Activator.CreateInstance(type);
                        rules.Add(rule);
                    }
                    catch
                    {
                        // continue
                    }

                    continue;
                }
            }

            return rules;
        }

        public IEnumerable<ITokenRule> GetTokenRules()
        {
            var rules = new List<ITokenRule>();
            foreach (Type type in _assembly.GetTypes())
            {
                if (typeof(ITokenRule).IsAssignableFrom(type))
                {
                    try
                    {
                        ITokenRule rule = (ITokenRule)Activator.CreateInstance(type);
                        rules.Add(rule);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"Failed to instantiate rule {type.FullName}");
                        // continue
                    }

                    continue;
                }
            }

            return rules;
        }
    }

}
