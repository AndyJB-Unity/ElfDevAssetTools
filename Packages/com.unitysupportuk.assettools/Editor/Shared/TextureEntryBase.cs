using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Linq;

// todo: Figure out how to share code with AssetFileEntry

namespace ElfDev
{
	class TextureEntryBase
	{
		public string guid;
		public string assetPath;
		public string importerPreset;
		public Texture asset;
		public HashSet<string> dependents = new HashSet<string>();

		public int estimatedSizeBytes = 0;
		public bool inBuild = false;

		public TextureEntryBase(string assetGuid)
		{
			string textureAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

			guid = assetGuid;
			assetPath = textureAssetPath;
			asset = null; // Load deferred till later, in case we don't want it
			importerPreset = ""; // Optional
		}

		public virtual Texture InitAsset()
		{
			asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture)) as Texture;

			if (asset != null)
			{
				estimatedSizeBytes = TextureInfo.RuntimeTextureSizeBytes(asset);
			}

			return asset;
		}

		public static void CollectDependencies<T>(bool showProgress, ref Dictionary<string, T> texturesByGuid) where T : TextureEntryBase
		{
			if (showProgress)
				EditorUtility.DisplayProgressBar("Gather Dependencies... ", "...", 0.0f);

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
						if (texturesByGuid.ContainsKey(guidDep))
						{
							if (guidDep != guidAsset) // You can't depend on yourself
							{
								texturesByGuid[guidDep].dependents.Add(assetPath);
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

		// Requires dependencies to be collected first
		public static void MarkBuildFiles<T>(bool enabledScenesOnly, ref Dictionary<string, T> texturesByGuid) where T : TextureEntryBase
		{
			string[] sceneFiles = enabledScenesOnly
				? EditorBuildSettings.scenes.Where(a => a.enabled).Select((b) => b.path).ToArray()
				: // Only Enabled
				EditorBuildSettings.scenes.Select((b) => b.path).ToArray(); // All

			// Check each texture is in the build
			// 1. A dependant is in the build list
			// 2. It's in a Resources Folder
			// 3. It's in the StreamingResources folder
			foreach (var tx in texturesByGuid.Values)
			{
				if (tx.assetPath.Contains("/Resources/") && !tx.assetPath.Contains("Editor/Resources/"))
				{
					tx.inBuild = true;
					continue;
				}

				if (tx.assetPath.StartsWith("Assets/StreamingAssets/"))
				{
					tx.inBuild = true;
					continue;
				}

				foreach (var depPath in tx.dependents)
				{
					if (sceneFiles.Contains(depPath))
					{
						tx.inBuild = true;
						continue;
					}
				}
			}
		}

		//
	}

}