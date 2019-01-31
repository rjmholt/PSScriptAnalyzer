using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerShell.CrossCompatibility.Utility;
using AvailableTypeDataMut = Microsoft.PowerShell.CrossCompatibility.Data.Types.AvailableTypeData;

namespace Microsoft.PowerShell.CrossCompatibility.Query.Types
{
    public class AvailableTypeData
    {
        private readonly Lazy<IReadOnlyDictionary<string, TypeData>> _types;

        public AvailableTypeData(AvailableTypeDataMut availableTypeData)
        {
            TypeAccelerators = availableTypeData.TypeAccelerators.ToDictionary(ta => ta.Key, ta => new TypeAcceleratorData(ta.Key, ta.Value));
            Assemblies = availableTypeData.Assemblies.ToDictionary(asm => asm.Key, asm => new AssemblyData(asm.Value));
            _types = new Lazy<IReadOnlyDictionary<string, TypeData>>(() => CreateTypeLookupTable(Assemblies));
        }

        public IReadOnlyDictionary<string, TypeAcceleratorData> TypeAccelerators { get; }

        public IReadOnlyDictionary<string, AssemblyData> Assemblies { get; }

        public IReadOnlyDictionary<string, TypeData> Types => _types.Value;

        private static IReadOnlyDictionary<string, TypeData> CreateTypeLookupTable(IReadOnlyDictionary<string, AssemblyData> assemblies)
        {
            var typeDict = new Dictionary<string, TypeData>(StringComparer.OrdinalIgnoreCase);

            foreach (AssemblyData asm in assemblies.Values)
            {
                foreach (KeyValuePair<string, IReadOnlyDictionary<string, TypeData>> nspace in asm.Types)
                {
                    foreach (TypeData type in nspace.Value.Values)
                    {
                        if (TypeNaming.IsGenericName(type.Name))
                        {
                            string strippedTypeName = TypeNaming.StripGenericQuantifiers(type.Name);
                            string strippedTypeFullName = TypeNaming.AssembleFullName(nspace.Key, strippedTypeName);
                            typeDict[strippedTypeFullName] = type;
                        }

                        string typeFullName = TypeNaming.AssembleFullName(nspace.Key, type.Name);
                        typeDict.Add(typeFullName, type);
                    }
                }
            }

            return typeDict;
        }
    }
}