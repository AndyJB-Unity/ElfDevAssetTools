#define EXCLUDE_STREAMING_ASSETS		// They have no dependencies anyway, no need to waste time or space on them

// Dependency Database in more useful form

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElfDev
{
	public class DependencyDatabase
	{
		StringMemoizer sdb = new StringMemoizer();

		public string getString(string key)
		{
			return sdb.get(key);
		}

		public class AssetGraphNode : IComparable<AssetGraphNode>
		{
			public string assetPath;
			public string guid;
			public string localName;

			public bool inBuild = false;

			public string displayString = null;

			public HashSet<AssetGraphNode> dependsOn = new HashSet<AssetGraphNode>();
			public HashSet<AssetGraphNode> isDependedOnBy = new HashSet<AssetGraphNode>();

			// todo: mark sorted lists dirty when base changes

			List<AssetGraphNode> sortedDependsOn_ = null;
			List<AssetGraphNode> sortedIsDependedOnBy_ = null;

			// IComparable
			public int CompareTo(AssetGraphNode rhs)
			{
				return System.String.Compare(this.assetPath, rhs.assetPath);

			}

			public List<AssetGraphNode> sortedDependsOn
			{
				get
				{
					if (sortedDependsOn_ == null)
					{
						sortedDependsOn_ = new List<AssetGraphNode>(dependsOn);
						sortedDependsOn_.Sort();
					}

					return sortedDependsOn_;
				}
			}

			public List<AssetGraphNode> sortedIsDependedOnBy
			{
				get
				{
					if (sortedIsDependedOnBy_ == null)
					{
						sortedIsDependedOnBy_ = new List<AssetGraphNode>(isDependedOnBy);
						sortedIsDependedOnBy_.Sort();
					}

					return sortedIsDependedOnBy_;
				}
			}

			public AssetTreeNode dir = null;
		}

		public Dictionary<string, AssetGraphNode> assets = new Dictionary<string, AssetGraphNode>(); // Indexed by assetPath			todo: as property

		public class AssetTreeNode
		{
			public string path;
			public string localName;

			public bool unFolded = false;

			public AssetTreeNode parent = null;
			public HashSet<AssetTreeNode> children = new HashSet<AssetTreeNode>();
			public HashSet<AssetGraphNode> contents = new HashSet<AssetGraphNode>();

			static void GetAllAssetsRecurse_(List<AssetGraphNode> lst, AssetTreeNode root)
			{
				lst.AddRange(root.contents);
				foreach (var node in root.children)
				{
					GetAllAssetsRecurse_(lst, node);
				}
			}

			public List<AssetGraphNode> GetAllAssets()
			{
				List<AssetGraphNode> lst = new List<AssetGraphNode>();
				GetAllAssetsRecurse_(lst, this);
				return lst;
			}

		}

		public Dictionary<string, AssetTreeNode> treeNodes = null; // todo: properties
		public AssetTreeNode assetsRoot = null;
		public AssetTreeNode packagesRoot = null;

		public void Reset()
		{
			assets.Clear();
			treeNodes = null;
			assetsRoot = null;
			packagesRoot = null;
		}

		void _RecursiveCreate(AssetTreeNode atn)
		{
			if (atn.parent != null) return;

			if (atn.path == "Assets")
			{
				assetsRoot = atn;
				return;
			}

			if (atn.path == "Packages")
			{
				packagesRoot = atn;
				return;
			}

			// Find or create parent
			string parentDir = sdb.get(System.IO.Path.GetDirectoryName(atn.path) );

			AssetTreeNode atnParent = null;
			if ( treeNodes.ContainsKey(parentDir) )
			{
				atnParent = treeNodes[parentDir];
			}
			else
			{
				atnParent = new AssetTreeNode();
				atnParent.path = parentDir;
				atnParent.localName = sdb.get(System.IO.Path.GetFileName(parentDir) );

				//Debug.Log( "FILL IN:" + atnParent.path + "(" + atnParent.localName + ")" );
				treeNodes[parentDir] = atnParent;
			}

			atnParent.children.Add(atn);

			_RecursiveCreate(atnParent);
		}

		void MakeTree(bool showProgress)
		{
			treeNodes = new Dictionary<string, AssetTreeNode>();

			if (showProgress)
				EditorUtility.DisplayProgressBar("Assigning Files to branches", "", 0);

			int updateProgressGranularity = Mathf.Min( Mathf.Max(assets.Count/50, 10), 5000 ); 
			
			int index = 0;
			foreach (var agn in assets.Values)
			{
				agn.localName = sdb.get(System.IO.Path.GetFileName(agn.assetPath));

				string dirName = sdb.get(System.IO.Path.GetDirectoryName(agn.assetPath)); // This should be okay now since the assets data no longer contains path assets

				AssetTreeNode atn = null;
				if (treeNodes.ContainsKey(dirName))
				{
					atn = treeNodes[dirName];
				}
				else
				{
					atn = new AssetTreeNode();
					atn.path = dirName;
					atn.localName = sdb.get(System.IO.Path.GetFileName(dirName));

					//Debug.Log( "ADDED:" + atn.path + "(" + atn.localName + ")" );
					treeNodes[dirName] = atn;
				}

				agn.dir = atn;
				atn.contents.Add(agn);

				if (showProgress && (index % updateProgressGranularity == 0))
				{
					float prog = (float)index / (float)assets.Count;
					EditorUtility.DisplayProgressBar("Assigning Files to branches", dirName, prog);
				}

				++index;
			}

			if (showProgress)
				EditorUtility.ClearProgressBar();

			// Now build the hierarchy
			updateProgressGranularity = Mathf.Min( Mathf.Max(treeNodes.Count/50, 10), 5000 ); 

			index = 0;
			foreach (var atn in treeNodes.Values.ToArray())
			{
				_RecursiveCreate(atn);
				if (showProgress && (index % updateProgressGranularity == 0))
				{
					float prog = (float)index / (float)treeNodes.Count;
					EditorUtility.DisplayProgressBar("Wiring up tree", "", prog);
				}

				++index;
			}

			if (showProgress)
				EditorUtility.ClearProgressBar();
		}

		public void LightScan(bool showProgress)
		{
			// Don't build a tree, just fill in the assets
			
			assets = new Dictionary<string, AssetGraphNode>();

			List<string> allGuids = new List<string>();
			allGuids.AddRange(AssetDatabase.FindAssets("t:Object"));

			int updateProgressGranularity = Mathf.Min( Mathf.Max(allGuids.Count/50, 10), 5000 ); 
			
			if (showProgress)
				EditorUtility.DisplayProgressBar("Getting list of assets", "...", 0f);

			// List of Assets
			int index = 0;
			foreach (var guid in allGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);

				if (System.IO.Directory.Exists(assetPath))
				{
					// This is a directory not an asset, hmm
				}
				else
				{
					if (assets.ContainsKey(assetPath))
					{
						//Debug.LogWarning( "Asset already in list: " + assetPath );
					}
					else
					{
#if EXCLUDE_STREAMING_ASSETS
						if (assetPath.StartsWith("Assets/StreamingAssets"))
						{
							//Debug.LogWarning( "Skipping Streaming Asset: " + assetPath );
						}
						else
#endif
						{
							AssetGraphNode agn = new AssetGraphNode();
							agn.assetPath = sdb.get(assetPath);
							agn.guid = sdb.get(guid);

							assets[assetPath] = agn;
						}
					}
				}

				if (showProgress && (index % updateProgressGranularity == 0))
				{
					float prog = (float)index / (float)allGuids.Count;
					EditorUtility.DisplayProgressBar("Getting list of assets", assetPath, prog);
				}

				++index;
			}

			if (showProgress)
				EditorUtility.DisplayProgressBar("Gathering Unity dependency info", "...", 0f);

			// Asset Dependency Information
			allGuids = null;
			index = 0;
			foreach (var agn in assets.Values)
			{
				string[] dependsOn = AssetDatabase.GetDependencies(new string[] { agn.assetPath });

				foreach (var dep in dependsOn)
				{
					string depPath = sdb.get(dep);
					if (depPath == agn.assetPath) // Don't depend on yourself, that way lies madness
						continue;

					if (assets.ContainsKey(depPath))
					{
						AssetGraphNode depAss = assets[depPath];
						agn.dependsOn.Add(depAss);
						depAss.isDependedOnBy.Add(agn);
					}
					else
					{
						Debug.LogWarning("Asset dependency is unknown: " + depPath);
					}
				}

				if (showProgress && (index % updateProgressGranularity == 0))
				{
					float prog = (float)index / (float)assets.Count;
					EditorUtility.DisplayProgressBar("Gathering Unity dependency info", agn.assetPath, prog);
				}

				++index;
			}

			if (showProgress)
				EditorUtility.ClearProgressBar();
		}

		void MarkBuildFiles(bool enabledScenesOnly)
		{
			string[] sceneFiles = enabledScenesOnly
				? EditorBuildSettings.scenes.Where(a => a.enabled).Select((b) => b.path).ToArray()
				: // Only Enabled
				EditorBuildSettings.scenes.Select((b) => b.path).ToArray(); // All

			//public Dictionary<string, AssetGraphNode> assets

			// Check each texture is in the build
			// 1. A dependant is in the build list
			// 2. It's in a Resources Folder
			// 3. It's in the StreamingResources folder
			foreach (var tx in assets.Values)
			{
				if (tx.assetPath.Contains("/Resources/"))
				{
					tx.inBuild = true;
					continue;
				}

				if (tx.assetPath.StartsWith("Assets/StreamingAssets/"))
				{
					tx.inBuild = true;
					continue;
				}

				foreach (var dep in tx.isDependedOnBy)
				{
					if (sceneFiles.Contains(dep.assetPath))
					{
						tx.inBuild = true;
						continue;
					}
				}
			}
		}

		public void FullScan(bool showProgress) 
		{
			LightScan(showProgress);
			MakeTree(showProgress);
			MarkBuildFiles(true);
		}

	}
}


// AssetModificationProcessor - https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.html


