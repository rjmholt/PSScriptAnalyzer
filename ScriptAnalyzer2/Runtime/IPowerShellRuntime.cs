using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;
using SMA = System.Management.Automation;

namespace Microsoft.PowerShell.ScriptAnalyzer.Runtime
{
    public abstract class PowerShellCommandDatabase
    {
        public abstract IReadOnlyList<string> GetCommandAliases(string command);

        public abstract string GetAliasTarget(string alias);

        public abstract IReadOnlyList<string> GetAllNamesForCommand(string command);
    }

    public abstract class CachedPowerShellCommandDatabase : PowerShellCommandDatabase
    {
        private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _commandAliases;

        private readonly ConcurrentDictionary<string, string> _aliasTargets;

        private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _commandNames;

        public override IReadOnlyList<string> GetCommandAliases(string command)
        {
            return _commandAliases.GetOrAdd(command, CollectCommandAliases);
        }

        public override string GetAliasTarget(string alias)
        {
            return _aliasTargets.GetOrAdd(alias, CollectAliasTarget);
        }

        public override IReadOnlyList<string> GetAllNamesForCommand(string command)
        {
            return _commandNames.GetOrAdd(command, CollectAllNamesForCommand);
        }

        protected abstract IReadOnlyList<string> CollectCommandAliases(string command);

        protected abstract string CollectAliasTarget(string alias);

        private IReadOnlyList<string> CollectAllNamesForCommand(string command)
        {
            var list = new List<string>();
            list.AddRange(GetCommandAliases(command));
            list.Add(GetAliasTarget(command));
            return list;
        }
    }

    public class HostedPowerShellRuntimeCommandDatabase : CachedPowerShellCommandDatabase
    {
        protected override string CollectAliasTarget(string alias)
        {
            throw new NotImplementedException();
        }

        protected override IReadOnlyList<string> CollectCommandAliases(string command)
        {
            throw new NotImplementedException();
        }
    }
}
