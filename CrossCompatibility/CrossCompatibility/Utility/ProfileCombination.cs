using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerShell.CrossCompatibility.Data;
using Microsoft.PowerShell.CrossCompatibility.Data.Modules;
using Microsoft.PowerShell.CrossCompatibility.Data.Platform;
using Microsoft.PowerShell.CrossCompatibility.Data.Types;

namespace Microsoft.PowerShell.CrossCompatibility.Utility
{
    public static class ProfileCombination
    {
        public static CompatibilityProfileData IntersectMany(IEnumerable<CompatibilityProfileData> profiles, PlatformData newPlatform)
        {
            CompatibilityProfileData intersectedProfile = CombineProfiles(profiles, Intersect);

            // It's up to the caller to come up with a new platform descriptor for the intersected platform
            intersectedProfile.Platform = newPlatform;

            return intersectedProfile;
        }

        public static CompatibilityProfileData UnionMany(IEnumerable<CompatibilityProfileData> profiles)
        {
            return CombineProfiles(profiles, Union);
        }

        public static object Intersect(CompatibilityProfileData thisProfile, CompatibilityProfileData thatProfile)
        {
            thisProfile.Runtime = (RuntimeData)Intersect(thisProfile.Runtime, thatProfile.Runtime);

            // We have no generic algorithm for generating intersected platform information,
            // so it's left up to the caller to correct that information
            thisProfile.Platform = null;

            return thisProfile;
        }

        public static object Intersect(RuntimeData thisRuntime, RuntimeData thatRuntime)
        {
            thisRuntime.Types = (AvailableTypeData)Intersect(thisRuntime.Types, thatRuntime.Types);

            // Intersect modules first at the whole module level
            thisRuntime.Modules = (JsonCaseInsensitiveStringDictionary<JsonDictionary<Version, ModuleData>>)Intersect(thisRuntime.Modules, thatRuntime.Modules, keyComparer: StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, JsonDictionary<Version, ModuleData>> moduleVersions in thatRuntime.Modules)
            {
                string name = moduleVersions.Key;

                if (!thisRuntime.Modules.ContainsKey(name))
                {
                    continue;
                }

                // Modules are intersected by:
                // - Union of all versions on each side, versioned to highest version
                // - Intersection of both sides, versioned to lowest version
                KeyValuePair<Version, ModuleData> intersectedModule = IntersectMultiVersionModules(thisRuntime.Modules[name], thatRuntime.Modules[name]);
                thisRuntime.Modules[name] = new JsonDictionary<Version, ModuleData>()
                {
                    { intersectedModule.Key, intersectedModule.Value }
                };
            }

            return thisRuntime;
        }

        public static object Intersect(AvailableTypeData thisTypes, AvailableTypeData thatTypes)
        {
            thisTypes.Assemblies = (JsonCaseInsensitiveStringDictionary<AssemblyData>)Intersect(thisTypes.Assemblies, thatTypes.Assemblies, Intersect);

            thisTypes.TypeAccelerators = (JsonCaseInsensitiveStringDictionary<TypeAcceleratorData>)Intersect(thisTypes.TypeAccelerators, thatTypes.TypeAccelerators);
            
            return thisTypes;
        }

        public static object Intersect(AssemblyData thisAsm, AssemblyData thatAsm)
        {
            if (thisAsm == thatAsm)
            {

            }

            thisAsm.AssemblyName = (AssemblyNameData)Intersect(thisAsm.AssemblyName, thatAsm.AssemblyName);

            thisAsm.Types = (JsonCaseInsensitiveStringDictionary<JsonCaseInsensitiveStringDictionary<TypeData>>)Intersect(thisAsm.Types, thatAsm.Types);
            
            if (thisAsm.Types != null)
            {
                if (thatAsm.Types == null)
                {
                    thisAsm.Types = null;
                }
                else
                {
                    foreach (KeyValuePair<string, JsonCaseInsensitiveStringDictionary<TypeData>> typeNamespace in thatAsm.Types)
                    {
                        if (!thisAsm.Types.ContainsKey(typeNamespace.Key))
                        {
                            continue;
                        }

                        thisAsm.Types[typeNamespace.Key] = (JsonCaseInsensitiveStringDictionary<TypeData>)Intersect(thisAsm.Types[typeNamespace.Key], typeNamespace.Value);
                    }
                }
            }

            return thisAsm;
        }

        public static object Intersect(AssemblyNameData thisAsmName, AssemblyNameData thatAsmName)
        {
            // Having different cultures downgrades to culture neutral
            if (thisAsmName.Culture != thatAsmName.Culture)
            {
                thisAsmName.Culture = null;
            }

            return thisAsmName;
        }

        public static object Intersect(TypeData thisType, TypeData thatType)
        {
            thisType.Instance = (MemberData)Intersect(thisType.Instance, thatType.Instance);
            thisType.Static = (MemberData)Intersect(thisType.Static, thatType.Static);

            return thisType;
        }

        public static object Intersect(MemberData thisMembers, MemberData thatMembers)
        {
            thisMembers.Events = (JsonCaseInsensitiveStringDictionary<EventData>)Intersect(thisMembers.Events, thatMembers.Events);
            thisMembers.Fields = (JsonCaseInsensitiveStringDictionary<FieldData>)Intersect(thisMembers.Fields, thatMembers.Fields);
            thisMembers.Properties = (JsonCaseInsensitiveStringDictionary<PropertyData>)Intersect(thisMembers.Properties, thatMembers.Properties, Intersect);
            thisMembers.NestedTypes = (JsonCaseInsensitiveStringDictionary<TypeData>)Intersect(thisMembers.NestedTypes, thatMembers.NestedTypes, Intersect);
            thisMembers.Methods = (JsonCaseInsensitiveStringDictionary<MethodData>)Intersect(thisMembers.Methods, thatMembers.Methods);

            // Recollect only constructors that occur in both left and right sets
            var thisConstructors = new List<string[]>();
            foreach (string[] thisParams in thisMembers.Constructors)
            {
                foreach (string[] thatParams in thatMembers.Constructors)
                {
                    if (new ParameterListComparer().Equals(thisParams, thatParams))
                    {
                        thisConstructors.Add(thisParams);
                    }
                }
            }
            thisMembers.Constructors = thisConstructors.ToArray();

            if (thisMembers.Indexers != null)
            {
                if (thatMembers.Indexers == null)
                {
                    thisMembers.Indexers = null;
                }
                else
                {
                    // Recollect indexers that occur in both left and right sets
                    var thisIndexers = new List<IndexerData>();
                    foreach (IndexerData thisIndexer in thisMembers.Indexers)
                    {
                        foreach (IndexerData thatIndexer in thatMembers.Indexers)
                        {
                            if (new ParameterListComparer().Equals(thisIndexer.Parameters, thatIndexer.Parameters))
                            {
                                IndexerData indexer = (IndexerData)Intersect(thisIndexer, thatIndexer);
                                thisIndexers.Add(indexer);
                            }
                        }
                    }
                    thisMembers.Indexers = thisIndexers.ToArray();
                }
            }

            return thisMembers;
        }

        public static object Intersect(IndexerData thisIndexer, IndexerData thatIndexer)
        {
            thisIndexer.Accessors = thisIndexer.Accessors.Intersect(thatIndexer.Accessors).ToArray();
            return thisIndexer;
        }

        public static object Intersect(PropertyData thisProperty, PropertyData thatProperty)
        {
            thisProperty.Accessors = thisProperty.Accessors.Intersect(thatProperty.Accessors).ToArray();
            return thisProperty;
        }

        public static object Intersect(ModuleData thisModule, ModuleData thatModule)
        {
            thisModule.Aliases = (JsonCaseInsensitiveStringDictionary<string>)Intersect(thisModule.Aliases, thatModule.Aliases);
            thisModule.Variables = thisModule.Variables?.Intersect(thatModule.Variables ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase).ToArray();
            thisModule.Cmdlets = (JsonCaseInsensitiveStringDictionary<CmdletData>)Intersect(thisModule.Cmdlets, thatModule.Cmdlets, Intersect);
            thisModule.Functions = (JsonCaseInsensitiveStringDictionary<FunctionData>)Intersect(thisModule.Functions, thatModule.Functions, Intersect);

            return thisModule;
        }

        public static object Intersect(CommandData thisCommand, CommandData thatCommand)
        {
            thisCommand.OutputType = thisCommand.OutputType?.Intersect(thatCommand.OutputType ?? Enumerable.Empty<string>()).ToArray();
            thisCommand.ParameterSets = thisCommand.ParameterSets?.Intersect(thatCommand.ParameterSets ?? Enumerable.Empty<string>()).ToArray();

            thisCommand.ParameterAliases = (JsonCaseInsensitiveStringDictionary<string>)Intersect(thisCommand.ParameterAliases, thatCommand.ParameterAliases);
            thisCommand.Parameters = (JsonCaseInsensitiveStringDictionary<ParameterData>)Intersect(thisCommand.Parameters, thatCommand.Parameters, Intersect);

            return thisCommand;
        }

        public static object Intersect(ParameterData thisParam, ParameterData thatParam)
        {
            thisParam.ParameterSets = (JsonCaseInsensitiveStringDictionary<ParameterSetData>)Intersect(thisParam.ParameterSets, thatParam.ParameterSets);
            return thisParam;
        }

        public static object Union(CompatibilityProfileData thisProfile, CompatibilityProfileData thatProfile)
        {
            // There's no simple solution to this currently.
            // We can revisit, but platform unions don't make much sense out of context
            thisProfile.Platform = null;

            Union(thisProfile.Runtime, thatProfile.Runtime);

            return thisProfile;
        }

        public static object Union(RuntimeData thisRuntime, RuntimeData thatRuntime)
        {
            foreach (KeyValuePair<string, JsonDictionary<Version, ModuleData>> moduleVersions in thatRuntime.Modules)
            {
                if (!thisRuntime.Modules.ContainsKey(moduleVersions.Key))
                {
                    thisRuntime.Modules.Add(moduleVersions.Key, moduleVersions.Value);
                    continue;
                }

                thisRuntime.Modules[moduleVersions.Key] = DictionaryUnion(thisRuntime.Modules[moduleVersions.Key], moduleVersions.Value);
            }

            thisRuntime.NativeCommands = StringDictionaryUnion(thisRuntime.NativeCommands, thatRuntime.NativeCommands, ArrayUnion);

            Union(thisRuntime.Types, thatRuntime.Types);

            return thisRuntime;
        }

        public static object Union(ModuleData thisModule, ModuleData thatModule)
        {
            thisModule.Aliases = StringDictionaryUnion(thisModule.Aliases, thatModule.Aliases);

            thisModule.Variables = ArrayUnion(thisModule.Variables, thatModule.Variables);

            thisModule.Cmdlets = StringDictionaryUnion(thisModule.Cmdlets, thatModule.Cmdlets, Union);

            thisModule.Functions = StringDictionaryUnion(thisModule.Functions, thatModule.Functions, Union);

            return thisModule;
        }

        public static object Union(CommandData thisCommand, CommandData thatCommand)
        {
            thisCommand.OutputType = ArrayUnion(thisCommand.OutputType, thatCommand.OutputType);
            thisCommand.ParameterSets = ArrayUnion(thisCommand.ParameterSets, thatCommand.ParameterSets);

            thisCommand.ParameterAliases = StringDictionaryUnion(thisCommand.ParameterAliases, thatCommand.ParameterAliases);
            thisCommand.Parameters = StringDictionaryUnion(thisCommand.Parameters, thatCommand.Parameters, Union);

            return thisCommand;
        }

        public static object Union(ParameterData thisParameter, ParameterData thatParameter)
        {
            thisParameter.ParameterSets = StringDictionaryUnion(thisParameter.ParameterSets, thatParameter.ParameterSets, Union);

            return thisParameter;
        }

        public static object Union(ParameterSetData thisParameterSet, ParameterSetData thatParameterSet)
        {
            thisParameterSet.Flags = ArrayUnion(thisParameterSet.Flags, thatParameterSet.Flags);

            return thisParameterSet;
        }

        public static object Union(AvailableTypeData thisTypes, AvailableTypeData thatTypes)
        {
            thisTypes.Assemblies = StringDictionaryUnion(thisTypes.Assemblies, thatTypes.Assemblies, Union);
            thisTypes.TypeAccelerators = StringDictionaryUnion(thisTypes.TypeAccelerators, thatTypes.TypeAccelerators);

            return thisTypes;
        }

        public static object Union(AssemblyData thisAssembly, AssemblyData thatAssembly)
        {
            Union(thisAssembly.AssemblyName, thatAssembly.AssemblyName);

            if (thatAssembly.Types != null)
            {
                if (thisAssembly.Types == null)
                {
                    thisAssembly.Types = new JsonCaseInsensitiveStringDictionary<JsonCaseInsensitiveStringDictionary<TypeData>>();
                }

                foreach (KeyValuePair<string, JsonCaseInsensitiveStringDictionary<TypeData>> nspace in thatAssembly.Types)
                {
                    if (!thisAssembly.Types.ContainsKey(nspace.Key))
                    {
                        thisAssembly.Types.Add(nspace.Key, nspace.Value);
                        continue;
                    }

                    thisAssembly.Types[nspace.Key] = StringDictionaryUnion(thisAssembly.Types[nspace.Key], nspace.Value, Union);
                }
            }

            return thisAssembly;
        }

        public static object Union(AssemblyNameData thisAsmName, AssemblyNameData thatAsmName)
        {
            if (thatAsmName.Version > thisAsmName.Version)
            {
                thisAsmName.Version = thatAsmName.Version;
            }

            if (thisAsmName.PublicKeyToken == null && thatAsmName.PublicKeyToken != null)
            {
                thisAsmName.PublicKeyToken = thatAsmName.PublicKeyToken;
            }

            return thisAsmName;
        }

        public static object Union(TypeData thisType, TypeData thatType)
        {
            thisType.Instance = (MemberData)Union(thisType.Instance, thatType.Instance);
            thisType.Static = (MemberData)Union(thisType.Instance, thatType.Instance);
            return thisType;
        }

        public static object Union(MemberData thisMembers, MemberData thatMembers)
        {
            if (thatMembers == null)
            {
                return thisMembers;
            }

            if (thisMembers == null)
            {
                return thatMembers.Clone();
            }

            thisMembers.Indexers = ArrayUnion(thisMembers.Indexers, thatMembers.Indexers);

            thisMembers.Constructors = ParameterUnion(thisMembers.Constructors, thatMembers.Constructors);

            thisMembers.Events = StringDictionaryUnion(thisMembers.Events, thatMembers.Events);
            thisMembers.Fields = StringDictionaryUnion(thisMembers.Fields, thatMembers.Fields);
            thisMembers.Methods = StringDictionaryUnion(thisMembers.Methods, thatMembers.Methods, Union);
            thisMembers.NestedTypes = StringDictionaryUnion(thisMembers.NestedTypes, thatMembers.NestedTypes, Union);
            thisMembers.Properties = StringDictionaryUnion(thisMembers.Properties, thatMembers.Properties, Union);

            return thisMembers;
        }

        public static object Union(PropertyData thisProperty, PropertyData thatProperty)
        {
            thisProperty.Accessors = ArrayUnion(thisProperty.Accessors, thatProperty.Accessors);
            return thisProperty;
        }

        public static object Union(MethodData thisMethod, MethodData thatMethod)
        {
            thisMethod.OverloadParameters = ParameterUnion(thisMethod.OverloadParameters, thatMethod.OverloadParameters);
            return thisMethod;
        }

        private static object Intersect<K, V>(
            JsonDictionary<K, V> thisDict,
            JsonDictionary<K, V> thatDict,
            Func<V, V, object> intersector = null,
            IEqualityComparer<K> keyComparer = null)
            where K : ICloneable
            where V : ICloneable
        {
            if (thatDict == null)
            {
                return thisDict;
            }

            if (thisDict == null)
            {
                return thatDict.Clone();
            }

            // Remove all the keys from left that aren't in right (and rest easy that we never added keys from right into left)
            foreach (K thisKey in thisDict.Keys.ToArray())
            {
                if (!thatDict.ContainsKey(thisKey))
                {
                    thisDict.Remove(thisKey);
                    continue;
                }

                if (intersector != null)
                {
                    thisDict[thisKey] = (V)intersector(thisDict[thisKey], thatDict[thisKey]);
                }
            }

            return thisDict;
        }

        private static CompatibilityProfileData CombineProfiles(IEnumerable<CompatibilityProfileData> profiles, Func<CompatibilityProfileData, CompatibilityProfileData, object> combinator)
        {
            IEnumerator<CompatibilityProfileData> profileEnumerator = profiles.GetEnumerator();

            if (!profileEnumerator.MoveNext())
            {
                return null;
            }

            CompatibilityProfileData mutProfileBase = (CompatibilityProfileData)profileEnumerator.Current.Clone();

            while(profileEnumerator.MoveNext())
            {
                mutProfileBase = (CompatibilityProfileData)combinator(mutProfileBase, profileEnumerator.Current);
            }

            return mutProfileBase;
        }

        private static T[] ArrayUnion<T>(T[] thisArray, T[] thatArray)
        {
            if (thatArray == null)
            {
                return thisArray;
            }

            bool canClone = typeof(ICloneable).IsAssignableFrom(typeof(T));

            var clonedThat = new T[thatArray.Length];
            if (canClone)
            {
                for (int i = 0; i < thatArray.Length; i++)
                {
                    clonedThat[i] = (T)((dynamic)thatArray[i]).Clone();
                }
            }
            else
            {
                for (int i = 0; i < thatArray.Length; i++)
                {
                    clonedThat[i] = (T)thatArray[i];
                }
            }

            if (thisArray == null)
            {
                return clonedThat;
            }

            return thisArray.Union(thatArray).ToArray();
        }

        private static string[][] ParameterUnion(string[][] thisParameters, string[][] thatParameters)
        {
            if (thatParameters == null)
            {
                return thisParameters;
            }

            if (thisParameters == null)
            {
                return thatParameters.Select(arr => (string[])arr.Clone()).ToArray();
            }

            var parameters = new HashSet<string[]>(thisParameters, new ParameterListComparer());

            foreach (string[] thatParameter in thatParameters)
            {
                parameters.Add(thatParameter);
            }

            return parameters.ToArray();
        }

        private static JsonCaseInsensitiveStringDictionary<TValue> StringDictionaryUnion<TValue>(
            JsonCaseInsensitiveStringDictionary<TValue> thisStringDict,
            JsonCaseInsensitiveStringDictionary<TValue> thatStringDict,
            Func<TValue, TValue, object> valueUnionizer = null)
            where TValue : ICloneable
        {
            if (thatStringDict == null)
            {
                return thisStringDict;
            }

            if (thisStringDict == null)
            {
                return (JsonCaseInsensitiveStringDictionary<TValue>)thatStringDict.Clone();
            }

            foreach (KeyValuePair<string, TValue> item in thatStringDict)
            {
                if (!thisStringDict.ContainsKey(item.Key))
                {
                    thisStringDict.Add(item.Key, item.Value);
                    continue;
                }

                if (valueUnionizer != null)
                {
                    thisStringDict[item.Key] = (TValue)valueUnionizer(thisStringDict[item.Key], item.Value);
                }
            }

            return thisStringDict;
        }

        private static JsonDictionary<K, V> DictionaryUnion<K, V>(
            JsonDictionary<K, V> thisDict,
            JsonDictionary<K, V> thatDict, 
            Func<V, V, object> valueUnionizer = null)
            where K : ICloneable where V : ICloneable
        {
            if (thatDict == null)
            {
                return thisDict;
            }

            if (thisDict == null)
            {
                return (JsonDictionary<K, V>)thatDict.Clone();
            }

            foreach (KeyValuePair<K, V> item in thatDict)
            {
                if (!thisDict.ContainsKey(item.Key))
                {
                    thisDict.Add(item.Key, item.Value);
                    continue;
                }

                if (valueUnionizer != null)
                {
                    thisDict[item.Key] = (V)valueUnionizer(thisDict[item.Key], item.Value);
                }
            }

            return thisDict;
        }

        /// <summary>
        /// Intersect many versions of the same module in two runtimes by
        /// unioning all the versions in each runtime and intersecting the results.
        /// </summary>
        /// <param name="thisModules">The versions of the module in the left hand runtime.</param>
        /// <param name="thatModules">The versions of the module in the right hand runtime.</param>
        /// <returns></returns>
        private static KeyValuePair<Version, ModuleData> IntersectMultiVersionModules(
            JsonDictionary<Version, ModuleData> thisModules,
            JsonDictionary<Version, ModuleData> thatModules)
        {
            KeyValuePair<Version, ModuleData> thisUnionedModules = UnionVersionedModules(thisModules);
            KeyValuePair<Version, ModuleData> thatUnionedModules = UnionVersionedModules(thatModules);

            var intersectedModules = (ModuleData)Intersect(thisUnionedModules.Value, thatUnionedModules.Value);
            // Take the lower version
            Version version = thisUnionedModules.Key <= thatUnionedModules.Key
                ? thisUnionedModules.Key
                : thatUnionedModules.Key;

            return new KeyValuePair<Version, ModuleData>(version, intersectedModules);
        }

        private static KeyValuePair<Version, ModuleData> UnionVersionedModules(ICollection<KeyValuePair<Version, ModuleData>> modules)
        {
            ModuleData unionedModule = null;
            Version version = null;
            bool firstModule = true;
            foreach (KeyValuePair<Version, ModuleData> modVersion in modules)
            {
                if (firstModule)
                {
                    version = modVersion.Key;
                    unionedModule = (ModuleData)modVersion.Value.Clone();
                    firstModule = false;
                    continue;
                }

                version = version >= modVersion.Key ? version : modVersion.Key;
                unionedModule = (ModuleData)Union(unionedModule, modVersion.Value);
            }

            return new KeyValuePair<Version, ModuleData>(version, unionedModule);
        }


        private struct ParameterListComparer : IEqualityComparer<string[]>
        {
            public bool Equals(string[] x, string[] y)
            {
                if (x == y)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                if (x.Length != y.Length)
                {
                    return false;
                }

                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(string[] obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                unsafe
                {
                    int hc = 1;
                    foreach (string s in obj)
                    {
                        hc = 31 * hc + (s?.GetHashCode() ?? 0);
                    }
                    return hc;
                }
            }
        }
    }
}