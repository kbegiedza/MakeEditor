using System;
using System.Collections.Generic;

namespace Bloodstone.MakeEditor
{
    /// <summary>
    /// Modification and serialization helper(model) class for Unity's 
    /// <see href="https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html"> asmdef files</see>
    /// </summary>
    [Serializable]
    public class AssemblyDefinition
    {
        public string name;
        public bool autoReferenced = true;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public List<string> references;
        public List<string> versionDefines;
        public List<string> includePlatforms;
        public List<string> excludePlatforms;
        public List<string> defineConstraints;
        public List<string> precompiledReferences;
        public List<string> optionalUnityReferences;

        public AssemblyDefinition()
        {
            references = new List<string>();
            versionDefines = new List<string>();
            includePlatforms = new List<string>();
            excludePlatforms = new List<string>();
            defineConstraints = new List<string>();
            precompiledReferences = new List<string>();
            optionalUnityReferences = new List<string>();
        }

        public AssemblyDefinition(string name)
            : base()
        {
            name.ThrowIfNull(nameof(name));

            this.name = name;
        }
    }
}