// Using Unity Dependency Information to help clean up project

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AssetGraphNode = ElfDev.DependencyDatabase.AssetGraphNode;

namespace ElfDev
{
	public partial class DependencyTool : ElfDevEditorWindow<DependencyTool>
	{
		class DeadPool
		{
			HashSet<AssetGraphNode> deadPool = new HashSet<AssetGraphNode>(); // Marked for destruction!
			HashSet<string> deadPoolSubPaths = new HashSet<string>();

			HashSet<AssetGraphNode> deadPoolDependencies = new HashSet<AssetGraphNode>(); // Depend on marked objects
			HashSet<AssetGraphNode> lockList = new HashSet<AssetGraphNode>();

			HashSet<string> lockListAssetPaths = new HashSet<string>(); // Fast 'are we locked or not' lookup.  
			HashSet<string> lockListAssetSubPaths = new HashSet<string>(); // Fast 'are we locked or not' for paths prefixes (eg. Assets/_Export/Badge.fbx will store Assets and Assets/_Export here) 

			// Objects we are not allowed to destroy ( objects CANNOT be added if they inalvidate items in this list )

			bool deadPoolUpdated = true;

			public bool ContainsPath(string lowercasedPath)
			{
				return (deadPoolSubPaths.Contains(lowercasedPath));
			}

			public void Clear()
			{
				deadPool.Clear();
				deadPoolSubPaths.Clear();
				deadPoolDependencies.Clear();
				lockList.Clear();
			}

			// todo: why am I not using linq instead?

			public void Add(AssetGraphNode node)
			{
				deadPool.Add(node);
				deadPoolUpdated = true;
			}

			public void Remove(AssetGraphNode node)
			{
				deadPool.Remove(node);
				deadPoolUpdated = true;
			}

			void FinishedChangingDeadPool()
			{
				deadPoolSubPaths.Clear();

				foreach (var node in deadPool)
				{
					string subAssetPath = node.assetPath.ToLowerInvariant();

					while (true)
					{
						int indexOfRightPathSeparator = subAssetPath.LastIndexOf('/');
						if (indexOfRightPathSeparator > 0)
						{
							subAssetPath = subAssetPath.Remove(indexOfRightPathSeparator);

							deadPoolSubPaths.Add(subAssetPath);
						}
						else
						{
							break; //Ran out of sub-paths, finished
						}
					}
				}

				foreach (var node in deadPoolDependencies)
				{
					string subAssetPath = node.assetPath.ToLowerInvariant();

					while (true)
					{
						int indexOfRightPathSeparator = subAssetPath.LastIndexOf('/');
						if (indexOfRightPathSeparator > 0)
						{
							subAssetPath = subAssetPath.Remove(indexOfRightPathSeparator);

							deadPoolSubPaths.Add(subAssetPath);
						}
						else
						{
							break; //Ran out of sub-paths, finished
						}
					}
				}
			}

			public void Update() // Make sure deadpool is correct
			{
				if (deadPoolUpdated == false)
					return;

				deadPoolDependencies.Clear();

				foreach (var node in deadPool)
				{
					foreach (var dep in node.isDependedOnBy)
					{
						if (!deadPool.Contains(dep)) // Already explicitly marked?
						{
							deadPoolDependencies.Add(dep);
						}
					}
				}

				FinishedChangingDeadPool();

				deadPoolUpdated = false;
			}

			public void RemoveRecursive(AssetGraphNode node)
			{
				foreach (var agnDep in node.dependsOn)
				{
					if (deadPool.Contains(agnDep))
						RemoveRecursive(agnDep);
					if (deadPoolDependencies.Contains(agnDep))
					{
						RemoveRecursive(agnDep);
						Update();
					}
				}

				if (deadPool.Contains(node))
					deadPool.Remove(node);

				deadPoolUpdated = true;
			}

			public void RemoveByExtension(string ext)
			{
				List<AssetGraphNode> discards = new List<AssetGraphNode>();

				foreach (var agn in deadPool)
				{
					if (agn.assetPath.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase))
					{
						discards.Add(agn);
					}
				}

				foreach (var agn in discards)
				{
					deadPool.Remove(agn);
				}
			}

			// Removes all locked files from the list, as well as anything they depend on
			public bool RemoveAllLockedFiles()
			{
				HashSet<AssetGraphNode> umarkList = new HashSet<AssetGraphNode>();

				foreach (var agn in deadPool)
				{
					if (lockList.Contains(agn))
					{
						umarkList.Add(agn);
						umarkList.UnionWith(agn.dependsOn);
					}
				}

				foreach (var agn in deadPoolDependencies)
				{
					if (lockList.Contains(agn))
					{
						umarkList.Add(agn);
						umarkList.UnionWith(agn.dependsOn);
					}
				}

				foreach (var agn in umarkList)
				{
					if (deadPool.Contains(agn))
					{
						Remove(agn);
					}
				}

				return true;
			}

			public bool ContainsLockedPath(string lowercasedPath)
			{
				//PBr: Optimisation looking up paths in dictionaries rather than iterating all the nodes
				if (lockListAssetPaths.Contains(lowercasedPath) || lockListAssetSubPaths.Contains(lowercasedPath))
				{
					return true;
				}

				return false;
			}

			public void AddLocked(AssetGraphNode node, bool finishedChangingLockList)
			{
				//Debug.Log("Adding to Lock List: " + node.assetPath);
				lockList.Add(node);
				lockListAssetPaths.Add(node.assetPath.ToLowerInvariant());
				if (finishedChangingLockList)
				{
					FinishedChangingLockList(); //This will re-build the sub-paths dictionary
				}

				deadPoolUpdated = true;
			}

			public void FinishedChangingLockList()
			{
				// Rebuild lockListAssetSubPaths, which are all the sub-directories with one locked child or more
				lockListAssetSubPaths.Clear();
				foreach (var node in lockList)
				{
					//Break down the asset path from the right hand side, so Assets/_AssetExports/Models/NiftyClock.fbx injects Assets/_AssetExports/Models, then Assets/_AssetExports, then Assets
					string subAssetPath = node.assetPath.ToLowerInvariant();
					while (true)
					{
						int indexOfRightPathSeparator = subAssetPath.LastIndexOf('/');
						if (indexOfRightPathSeparator > 0)
						{
							subAssetPath = subAssetPath.Remove(indexOfRightPathSeparator);

							lockListAssetSubPaths.Add(subAssetPath);
						}
						else
						{
							break; //Ran out of sub-paths, finished
						}
					}
				}
			}

			public void RemoveLocked(AssetGraphNode node, bool finishedChangingLockList)
			{
				lockList.Remove(node);
				lockListAssetPaths.Remove(node.assetPath.ToLowerInvariant());

				if (finishedChangingLockList)
				{
					FinishedChangingLockList(); //This will re-build the sub-paths dictionary
				}

				deadPoolUpdated = true;
			}

			public bool SafeToDelete()
			{
				if (deadPool.Count == 0)
					return false;

				foreach (var node in deadPool)
				{
					if (lockList.Contains(node))
					{
						return false;
					}
				}

				foreach (var node in deadPoolDependencies)
				{
					if (lockList.Contains(node))
					{
						return false;
					}
				}

				return true;
			}

			public void DeadPoolExecute(bool progressBar)
			{
				if (progressBar)
					EditorUtility.DisplayProgressBar("Deleting", "I hope you know what you're doing..", 0f);

				int index = 0;
				foreach (var node in deadPool)
				{
					AssetDatabase.DeleteAsset(node.assetPath);

					//UnityEditor.VersionControl.Provider.Delete( node.assetPath ).Wait();

					if (progressBar && (index % 10 == 0))
					{
						float p = ((float)index++) / (float)(deadPool.Count + deadPoolDependencies.Count);
						EditorUtility.DisplayProgressBar("Deleting", node.assetPath, p);
					}
				}

				foreach (var node in deadPoolDependencies)
				{
					AssetDatabase.DeleteAsset(node.assetPath);

					//UnityEditor.VersionControl.Provider.Delete( node.assetPath ).Wait();

					if (progressBar && (index % 10 == 0))
					{
						float p = ((float)index++) / (float)(deadPool.Count + deadPoolDependencies.Count);
						EditorUtility.DisplayProgressBar("Deleting", node.assetPath, p);
					}
				}

				if (progressBar)
					EditorUtility.ClearProgressBar();

				Clear();
			}

			public bool isMarked(AssetGraphNode agn) // Will be Deleted. Explicitly added files and dependants
			{
				return deadPool.Contains(agn) || deadPoolDependencies.Contains(agn);
			}

			public bool isMarkedExplicitly(AssetGraphNode agn) // Will be Deleted. Explicitly added files only
			{
				return deadPool.Contains(agn);
			}

			public bool isLocked(AssetGraphNode agn)
			{
				return lockList.Contains(agn);
				;
			}

			public IEnumerable<AssetGraphNode> locked
			{
				get { return lockList; }
			}

			public IEnumerable<AssetGraphNode> marked
			{
				get { return deadPool; }
			}

			public IEnumerable<AssetGraphNode> markedDependants
			{
				get { return deadPoolDependencies; }
			}

		}

		DeadPool deadpool = new DeadPool();

	}
}


