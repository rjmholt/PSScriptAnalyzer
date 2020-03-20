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

    public class SessionStateCommandDatabase : PowerShellCommandDatabase
    {
        public SessionStateCommandDatabase Create(CommandInvocationIntrinsics invokeCommandProvider)
        {
            var commandAliases = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var aliasTargets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (AliasInfo aliasInfo in invokeCommandProvider.GetCommands("*", CommandTypes.Alias, nameIsPattern: true))
            {
                aliasTargets[aliasInfo.Name] = aliasInfo.Definition;

                if (commandAliases.TryGetValue(aliasInfo.Definition, out IReadOnlyList<string> aliases))
                {
                    ((List<string>)aliases).Add(aliasInfo.Name);
                }
                else
                {
                    commandAliases[aliasInfo.Definition] = new List<string> { aliasInfo.Name };
                }
            }

            return new SessionStateCommandDatabase(commandAliases, aliasTargets);
        }

        private readonly IReadOnlyDictionary<string, string> _aliasTargets;

        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _commandAliases;

        private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _commandNames;

        private SessionStateCommandDatabase(
            IReadOnlyDictionary<string, IReadOnlyList<string>> commandAliases,
            IReadOnlyDictionary<string, string> aliasTargets)
        {
            _aliasTargets = aliasTargets;
            _commandAliases = commandAliases;
            _commandNames = new ConcurrentDictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public override string GetAliasTarget(string alias)
        {
            if (_aliasTargets.TryGetValue(alias, out string target))
            {
                return target;
            }

            return null;
        }

        public override IReadOnlyList<string> GetCommandAliases(string command)
        {
            if (_commandAliases.TryGetValue(command, out IReadOnlyList<string> aliases))
            {
                return aliases;
            }

            return null;
        }

        public override IReadOnlyList<string> GetAllNamesForCommand(string command)
        {
            return _commandNames.GetOrAdd(command, GenerateCommandNameList);
        }

        private IReadOnlyList<string> GenerateCommandNameList(string command)
        {
            var names = new List<string>();

            if (_commandAliases.TryGetValue(command, out IReadOnlyList<string> aliases))
            {
                names.AddRange(aliases);
            }

            if (_aliasTargets.TryGetValue(command, out string target))
            {
                names.Add(target);
            }

            return names.Count > 0 ? names : null;
        }
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
}
