using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

namespace ElfDev
{
    public class ProjectHelpers
    {
        public static bool SceneIsInPackage( string pathToScene )
        {
            var info = PackageInfo.FindForAssetPath( pathToScene );
            if ( info == null )
                return false;

            return true;
        }

        public static bool SceneCanBeLoaded( string pathToScene )
        {
            var info = PackageInfo.FindForAssetPath( pathToScene );
            if ( info == null )
                return true;

            if ( info.source == PackageSource.Embedded )
                return true;

            if ( info.source == PackageSource.Local )
                return true;

            return false;
        }
    }
}


/*
From: https://forum.unity.com/threads/check-if-asset-inside-package-is-readonly.900902/

Hi there,

There is no direct way to verify this, but you can follow these steps to avoid opening the scene if it's going to fail due to this read-only state:

1) Before opening the scene, call `UnityEditor.PackageManager.PackageInfo.FindForAssetPath(<path-to-scene-asset>);`
2) If the above is "null", the scene is not in a package and can be opened.
3) Otherwise, check the `source` property of the returned PackageInfo. If it's `PackageSource.Embedded` or `PackageSource.Local`, it can safely be opened. Otherwise, it's in an immutable package and cannot be opened.

*/

// https://docs.unity3d.com/ScriptReference/PackageManager.PackageInfo.html
