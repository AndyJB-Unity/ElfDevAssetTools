// Using Unity Dependency Information to help clean up project

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

using AssetGraphNode = ElfDev.DependencyDatabase.AssetGraphNode;
using AssetTreeNode = ElfDev.DependencyDatabase.AssetTreeNode;

namespace ElfDev
{
	public partial class DependencyTool : ElfDevEditorWindow<DependencyTool>
	{

		Vector2 scrollPositionLowerA = new Vector2(0, 0);
		Vector2 scrollPositionLowerB = new Vector2(0, 0);

		List<AssetGraphNode> cachedFolderContents = null;
		string cachedFolder = "";

		HashSet<AssetGraphNode> cachedFolderDependsOn = null;
		HashSet<AssetGraphNode> cachedFolderDependedOnBy = null;

		// Details : FOLDER
		private void OnGUI_DetailsPane_Folder(float scrollHeight, float scrollWidth)
		{
			float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			float svHeight = scrollHeight;

			GUILayout.Label("FOLDER: " + lastClickedNodeAsFolder.localName);
			svHeight -= itemHeight;

			AssetTreeNode atn = lastClickedNodeAsFolder;

			// Refresh if required
			if (atn.path != cachedFolder)
			{
				cachedFolder = atn.path;
				cachedFolderContents = atn.GetAllAssets();

				cachedFolderDependsOn = new HashSet<AssetGraphNode>();
				cachedFolderDependedOnBy = new HashSet<AssetGraphNode>();

				foreach (var node in cachedFolderContents)
				{
					if (node.dependsOn.Count > 0)
						cachedFolderDependsOn.UnionWith(node.dependsOn);
					if (node.isDependedOnBy.Count > 0)
						cachedFolderDependedOnBy.UnionWith(node.isDependedOnBy);
				}

				// Discard internal dependents & dependencies, we're only interested in those outside the selected folder
				List<AssetGraphNode> tempAgn = cachedFolderDependsOn.Where(a => !a.assetPath.StartsWith(atn.path)).ToList();
				cachedFolderDependsOn.Clear();
				cachedFolderDependsOn.UnionWith(tempAgn);
				tempAgn = cachedFolderDependedOnBy.Where(a => !a.assetPath.StartsWith(atn.path)).ToList();
				cachedFolderDependedOnBy.Clear();
				cachedFolderDependedOnBy.UnionWith(tempAgn);
			}

			// Display

			Color savedColour = GUI.color;

			GUILayout.BeginHorizontal();

			// Depends on
			GUILayout.BeginVertical(GUILayout.Width(scrollWidth / 2));
			GUI.color = Palette.asset_dependency(true);
			GUILayout.Label("Depends On:");
			if (cachedFolderDependsOn.Count > 0)
			{
				scrollPositionLowerA = GUILayout.BeginScrollView(scrollPositionLowerA, false, true, GUILayout.ExpandHeight(true));
				int firstIndex = (int)(scrollPositionLowerA.y / itemHeight);

				int itemNo = 0;
				foreach (var agnDep in cachedFolderDependsOn)
				{
					if (itemNo < firstIndex)
					{
						GUILayout.Space(itemHeight);
						itemNo++;
						continue;
					}

					if ((itemNo - firstIndex) * itemHeight > svHeight + 32) // Arbitrary fudge factor!
					{
						GUILayout.Space(itemHeight);
						itemNo++;
						continue;
					}

					GUI.color = Palette.asset_dependency(agnDep.inBuild);
					if (deadpool.isMarked(agnDep))
						GUI.color = Palette.deadpool_asset(agnDep.inBuild); // Required item is marked for deletion
					GUILayout.Label(agnDep.assetPath);

					++itemNo;
				}

				GUILayout.EndScrollView();
			}

			GUILayout.EndVertical();

			// Used by
			GUILayout.BeginVertical();
			GUI.color = Palette.asset_dependent(true);
			GUILayout.Label("Depended On By:");
			if (cachedFolderDependedOnBy.Count > 0)
			{
				scrollPositionLowerB = GUILayout.BeginScrollView(scrollPositionLowerB, false, true, GUILayout.ExpandHeight(true));
				int firstIndex = (int)(scrollPositionLowerB.y / itemHeight);

				int itemNo = 0;
				foreach (var agnDep in cachedFolderDependedOnBy)
				{
					if (itemNo < firstIndex)
					{
						GUILayout.Space(itemHeight);
						itemNo++;
						continue;
					}

					if ((itemNo - firstIndex) * itemHeight > svHeight + 32) // Arbitrary fudge factor!
					{
						GUILayout.Space(itemHeight);
						itemNo++;
						continue;
					}

					GUI.color = Palette.asset_dependent(agnDep.inBuild);
					if (deadpool.isLocked(agnDep))
						GUI.color = Palette.locked_asset(agnDep.inBuild); // Locked Item
					GUILayout.Label(agnDep.assetPath);

					++itemNo;
				}

				GUILayout.EndScrollView();
			}

			GUILayout.EndVertical();

			GUILayout.EndHorizontal();

			GUI.color = savedColour;
		}

		// Details : ASSET
		private void OnGUI_DetailsPane(float scrollHeight, float scrollWidth)
		{
			if (isLastClickedNodeAFolder)
			{
				OnGUI_DetailsPane_Folder(scrollHeight, scrollWidth);
				return;
			}

			// Note not using the scroll optimisation as single item dependency lists are generally quite small

			Color savedColour = GUI.color;

			GUI.color = Color.white;

			EditorGUILayout.BeginHorizontal();

			AssetGraphNode lastClickedAssetNode = lastClickedNodeAsAsset;

			GUILayout.Label("Asset: " + lastClickedAssetNode.localName);

			bool unmarkDependencies = GUILayout.Button("Unmark Dependencies");
			if (unmarkDependencies)
			{
				// Find all the things this asset depends on and unmark them from the dead-pool
				foreach (var agnDep in lastClickedAssetNode.dependsOn)
				{
					if (deadpool.isMarkedExplicitly(agnDep))
					{
						deadpool.Remove(agnDep);
					}
				}
			}

/*		bool unmarkDependenciesRecursive = GUILayout.Button( "Unmark ALL Dependencies" );				// todo: implement this, apparently I forgot to make it
		if ( unmarkDependenciesRecursive )
		{
			// Find all the things this asset depends on and unmark them from the dead-pool
			// if they're still marked because they depend on stuff we iterate till it isn't so
			foreach ( var agnDep in lastClickedAssetNode.dependsOn )
			{
				if ( deadPool.Contains( agnDep ) )
				{
					RemoveFromDeadPoolRecursive( agnDep );
				}
			}
		}  */

			bool lockDependencies = GUILayout.Button("Lock Dependencies");
			if (lockDependencies)
			{
				// Find all the things this asset depends on and Lock them 
				foreach (var agnDep in lastClickedAssetNode.dependsOn)
				{
					deadpool.AddLocked(agnDep, false);
				}

				deadpool.FinishedChangingLockList();
			}

			EditorGUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			// Depends on
			GUILayout.BeginVertical(GUILayout.Width(scrollWidth / 2));
			GUI.color = Palette.asset_dependency(true);
			GUILayout.Label("Depends On:");
			if (lastClickedAssetNode.dependsOn.Count > 0)
			{
				scrollPositionLowerA = GUILayout.BeginScrollView(scrollPositionLowerA, false, true, GUILayout.ExpandHeight(true));
				foreach (var agnDep in lastClickedAssetNode.sortedDependsOn)
				{
					GUI.color = Palette.asset_dependency(agnDep.inBuild);
					if (deadpool.isMarked(agnDep))
						GUI.color = Palette.deadpool_asset(agnDep.inBuild); // Required item is marked for deletion
					GUILayout.Label(agnDep.assetPath);
				}

				GUILayout.EndScrollView();
			}

			GUILayout.EndVertical();

			// Used by
			GUILayout.BeginVertical();
			GUI.color = Palette.asset_dependent(true);
			GUILayout.Label("Depended On By:");
			if (lastClickedAssetNode.isDependedOnBy.Count > 0)
			{
				scrollPositionLowerB = GUILayout.BeginScrollView(scrollPositionLowerB, false, true, GUILayout.ExpandHeight(true));
				foreach (var agnDep in lastClickedAssetNode.sortedIsDependedOnBy)
				{
					GUI.color = Palette.asset_dependent(agnDep.inBuild);
					if (deadpool.isLocked(agnDep))
						GUI.color = Palette.locked_asset(agnDep.inBuild); // Locked Item
					GUILayout.Label(agnDep.assetPath);
				}

				GUILayout.EndScrollView();
			}

			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUI.color = savedColour;
		}

	}
}


