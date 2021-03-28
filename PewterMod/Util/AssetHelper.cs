using System.Linq;
using System.Reflection;
using UnityEngine;
using ValheimLib;
using ValheimLib.ODB;
using UnityObject = UnityEngine.Object;

namespace ValheimMod.Util
{
    public static class AssetHelper
    {
        public static AssetBundle GetAssetBundleFromResources(string fileName)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            var resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(fileName));

            var stream = execAssembly.GetManifestResourceStream(resourceName);

            return AssetBundle.LoadFromStream(stream);
        }
    }
}