using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public abstract class RuleComponentProvider
    {
        public abstract object GetComponentInstance(Type componentType);
    }

    public class SimpleRuleComponentProviderBuilder
    {
        private readonly Dictionary<Type, Func<object>> _componentRegistrations;

        public SimpleRuleComponentProviderBuilder()
        {
            _componentRegistrations = new Dictionary<Type, Func<object>>();
        }
    }
}
