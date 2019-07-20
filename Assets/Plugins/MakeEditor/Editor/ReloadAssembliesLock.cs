using System;
using UnityEditor;

namespace Bloodstone.MakeEditor
{
    internal class ReloadAssembliesLock : IDisposable
    {
        public ReloadAssembliesLock()
        {
            EditorApplication.LockReloadAssemblies();
        }

        public void Dispose()
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}