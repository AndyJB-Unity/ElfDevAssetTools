using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Linq;

namespace ElfDev
{
	class AssetFileEntry
	{
		public HashSet<string> dependents = new HashSet<string>();
		public string assetPath;
		public bool inBuild = false;

		public static void CollectDependencies(bool showProgress, HashSet<string> uniqueAssetGuids, out Dictionary<string, AssetFileEntry> assetDependencyInfo)
		{
			if (showProgress)
				EditorUtility.DisplayProgressBar("Gather Dependencies... ", "...", 0.0f);

			assetDependencyInfo = new Dictionary<string, AssetFileEntry>( uniqueAssetGuids.Count );
			foreach (var guidAsset in uniqueAssetGuids)
			{
				AssetFileEntry newAssetEntry = new AssetFileEntry();
				newAssetEntry.assetPath = AssetDatabase.GUIDToAssetPath(guidAsset);
				assetDependencyInfo.Add(guidAsset, newAssetEntry);
			}

			List<string> allGuids = new List<string>();
			allGuids.AddRange(AssetDatabase.FindAssets("t:Object"));

			int index = 0;
			foreach (var guidAsset in allGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guidAsset);

				if (assetPath.StartsWith("Assets/StreamingAssets")) // LOL nope!
				{
					continue;
				}

				if (System.IO.Directory.Exists(assetPath))
				{
					// This is a directory not an asset, hmm
				}
				else
				{
					// Check what this asset depends on, if it's one of our textures make a note of
					string[] dependsOn = AssetDatabase.GetDependencies(new string[] { assetPath });

					foreach (var dep in dependsOn)
					{
						var guidDep = AssetDatabase.AssetPathToGUID(dep);
						if (uniqueAssetGuids.Contains(guidDep))
						{
							if (guidDep != guidAsset) // You can't depend on yourself
							{
								assetDependencyInfo[guidDep].dependents.Add(assetPath);
							}
						}
					}
				}

				if (showProgress)
				{
					if (index % 10 == 0)
					{
						float prog = (float)index / (float)allGuids.Count;
						EditorUtility.DisplayProgressBar("Gather Dependencies...", assetPath, prog);
					}
				}

				++index;
			}

			// End progress bar
			if (showProgress)
				EditorUtility.ClearProgressBar();
		}

		public static void UnMarkBuildFiles(ref Dictionary<string, AssetFileEntry> assetDependencyInfo)
		{
			foreach (var ax in assetDependencyInfo.Values)
			{
				ax.inBuild = false;
			}
		}

		// Requires dependencies to be collected first
		public static void MarkBuildFiles(bool enabledScenesOnly, ref Dictionary<string, AssetFileEntry> assetDependencyInfo)
		{
			string[] sceneFiles = enabledScenesOnly
				? EditorBuildSettings.scenes.Where(a => a.enabled).Select((b) => b.path).ToArray()
				: // Only Enabled
				EditorBuildSettings.scenes.Select((b) => b.path).ToArray(); // All

			// Check each asset is in the build
			// 1. A dependant is in the build list
			// 2. It's in a Resources Folder
			// 3. It's in the StreamingResources folder
			foreach (var ax in assetDependencyInfo.Values)
			{
				if (ax.assetPath.Contains("/Resources/"))
				{
					ax.inBuild = true;
					continue;
				}

				if (ax.assetPath.StartsWith("Assets/StreamingAssets/"))
				{
					ax.inBuild = true;
					continue;
				}

				foreach (var depPath in ax.dependents)
				{
					if (sceneFiles.Contains(depPath))
					{
						ax.inBuild = true;
						continue;
					}
				}
			}
		}
	}
}


