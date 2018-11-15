using System.Collections.Generic;
using Modules = Microsoft.PowerShell.CrossCompatibility.Data.Modules;

namespace Microsoft.PowerShell.CrossCompatibility.Query
{
    public class ParameterSetData
    {
        private readonly Modules.ParameterSetData _parameterSet;

        public ParameterSetData(string name, Modules.ParameterSetData parameterSetData)
        {
            Name = name;
            _parameterSet = parameterSetData;
        }

        public string Name { get; }

        public IReadOnlyCollection<ParameterSetFlag> Flags => _parameterSet.Flags;

        public int Position => _parameterSet.Position;
    }
}