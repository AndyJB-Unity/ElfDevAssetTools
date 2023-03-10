#if USE_TRACKER

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class AssetTracker : AssetPostprocessor
{
	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
	{
		foreach (string str in importedAssets)
		{
			Debug.Log("[AssetTracker] ** Reimported Asset: " + str);
		}
		foreach (string str in deletedAssets)
		{
			Debug.Log("[AssetTracker] ** Deleted Asset: " + str);
		}

		for (int i = 0; i < movedAssets.Length; i++)
		{
			Debug.Log("[AssetTracker] ** Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
		}

		if (didDomainReload)
		{
			Debug.Log("[AssetTracker] ** Domain has been reloaded");
		}
	}
}

#endif