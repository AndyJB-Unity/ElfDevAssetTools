// Using Unity Dependency Information to help clean up project

using UnityEngine;
using UnityEditor;
using AssetGraphNode = ElfDev.DependencyDatabase.AssetGraphNode;
using AssetTreeNode = ElfDev.DependencyDatabase.AssetTreeNode;

namespace ElfDev
{
	public partial class DependencyTool : ElfDevEditorWindow<DependencyTool>
	{
		class Palette : StylePalette
		{
			public static Color selectedFolder(bool inBuild)
			{
				return inBuild ? blue : darkblue;
			}

			public static Color selectedAsset(bool inBuild)
			{
				return inBuild ? yellow : darkyellow;
			}

			public static Color asset(bool inBuild)
			{
				return inBuild ? white : grey;
			}

			public static Color folder(bool inBuild)
			{
				return inBuild ? white : grey;
			}

			public static Color deadpool_asset(bool inBuild)
			{
				return inBuild ? red : darkred;
			}

			public static Color deadpooldependent_asset(bool inBuild)
			{
				return inBuild ? orange : darkorange;
			}

			public static Color locked_asset(bool inBuild)
			{
				return inBuild ? cyan : darkcyan;
			}

			public static Color asset_dependency(bool inBuild)
			{
                //return inBuild ? blue : darkblue;
				return inBuild ? yellow : darkyellow;
			}

			public static Color asset_dependent(bool inBuild)
			{
				return inBuild ? green : darkgreen;
			}

			public static GUIStyleState selectedAssetState(bool inBuild)
			{
				return inBuild ? yellowText : yellowText;
			}

			public static GUIStyleState assetState(bool inBuild)
			{
				return inBuild ? whiteText : greyText;
			}

			public static GUIStyleState deadpool_assetState(bool inBuild)
			{
				return inBuild ? redText : darkredText;
			}

			public static GUIStyleState deadpooldependent_assetState(bool inBuild)
			{
				return inBuild ? orangeText : darkorangeText;
			}

			public static GUIStyleState locked_assetState(bool inBuild)
			{
				return inBuild ? cyanText : darkcyanText;
			}
		}

		Vector2 scrollPositionMain = new Vector2(0, 0);
		Vector2 scrollPositionDeadPool = new Vector2(0, 0);

		public  DependencyTool(): base("Dependency Tool")
		{
			minSize = new Vector2(400f, 300f);
		}
		
		[MenuItem("ElfDev Asset Insights/Dependency Tool")]
		static void OpenWindow()
		{
			ShowWindow(true);
		}

		DependencyDatabase db = new DependencyDatabase();

		void _RecursiveShowClickableFoldoutTree(string prefix, AssetTreeNode atn)
		{
			GUILayout.BeginHorizontal();

			bool labelClicked = false;
			ClickableFoldout.Foldout(ref atn.unFolded, out labelClicked, ToolStyles.TempContent(prefix + atn.localName));

			if (labelClicked)
			{
				lastClickedNode = atn;
			}

			bool lockFolder = GUILayout.Button("Lock", GUILayout.Width(40f));
			if (lockFolder)
			{
				LockObjectsInPath(atn.path);
			}

			string insensitivePath = atn.path.ToLowerInvariant();

			bool hasLockedChildren = deadpool.ContainsLockedPath(insensitivePath);

			GUI.enabled = hasLockedChildren;
			bool unlockFolder = GUILayout.Button("Unlock", GUILayout.Width(50f));
			if (unlockFolder)
			{
				UnlockObjectsInPath(atn.path);
			}

			GUI.enabled = true;

			bool hasMarkedChildren = deadpool.ContainsPath(insensitivePath);
			GUI.enabled = hasMarkedChildren;
			bool removeFolder = GUILayout.Button("<", GUILayout.Width(18f));
			if (removeFolder)
			{
				RemoveObjectsInPath(atn.path);
			}

			GUI.enabled = true;
			bool addFolder = GUILayout.Button(">", GUILayout.Width(20f));
			if (addFolder)
			{
				AddObjectsInPath(atn.path);
			}

			GUILayout.EndHorizontal();

			if (atn.unFolded)
			{
				foreach (var atnChild in atn.children)
				{
					_RecursiveShowClickableFoldoutTree(prefix + "   ", atnChild);
				}

				prefix += "      ";

				foreach (var agn in atn.contents)
				{
					if ( agn.displayString == null )	// Lazy init
						agn.displayString = db.getString(prefix + agn.localName + "(" + agn.dependsOn.Count + "/" + agn.isDependedOnBy.Count + ")");

					EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

					bool isLocked = deadpool.isLocked(agn);
					bool isMarked = deadpool.isMarked(agn);
					bool isMarkedExplicitly = deadpool.isMarkedExplicitly(agn);

					GUIStyleState displayColour = Palette.assetState(agn.inBuild);

					if (isMarkedExplicitly)
						displayColour = Palette.deadpool_assetState(agn.inBuild);
					else if (isMarked)
						displayColour = Palette.deadpooldependent_assetState(agn.inBuild);

					if (isLocked)
						displayColour = Palette.locked_assetState(agn.inBuild);

					if (lastClickedNode == agn)
						displayColour = Palette.selectedAssetState(agn.inBuild);

					GUIStyle textButton = ToolStyles.textButtonWithStyleState(displayColour);
					textButton.alignment = TextAnchor.LowerLeft;
					textButton.stretchWidth = true;

					bool clicked = GUILayout.Button( agn.displayString, textButton, GUILayout.ExpandWidth(true) );
					if (clicked)
					{
						lastClickedNode = agn;

						//Debug.LogError( "YOU CLICKED " + agn.displayString + " YOU MASTERFUL DEMIGOD!" );
					}

					var oldButtonColour = GUI.contentColor;		
					GUI.contentColor = displayColour.textColor;	// Tint buttons to match selection state
					
					if (isLocked)
					{
						bool unlockItem = GUILayout.Button("Unlock", GUILayout.Width(50f));
						if (unlockItem)
						{
							deadpool.RemoveLocked(agn, true);
						}
					}
					else
					{
						bool lockItem = GUILayout.Button("Lock", GUILayout.Width(50f));
						if (lockItem)
						{
							deadpool.AddLocked(agn, true);
						}
					}

					if (isMarkedExplicitly)
					{
						bool removeItem = GUILayout.Button("<", GUILayout.Width(18f));
						if (removeItem)
						{
							deadpool.Remove(agn);
						}
					}
					else
					{
						bool addItem = GUILayout.Button(">", GUILayout.Width(20f));
						if (addItem)
						{
							deadpool.Add(agn);
						}
					}

					GUI.contentColor = oldButtonColour;
					
					EditorGUILayout.EndHorizontal();
				}
			}
		}

		void ShowClickableFoldoutTree(bool dataChanged)
		{
			if (db.treeNodes == null)
				return;
			
			if (db.assetsRoot != null)
			{
				_RecursiveShowClickableFoldoutTree("", db.assetsRoot);
			}
			if (db.packagesRoot != null)
			{
				_RecursiveShowClickableFoldoutTree("", db.packagesRoot);
			}

		}

		bool refreshDisplay = false;

		void LockObjectsInPath(string path)
		{
			foreach (var node in db.assets.Values)
			{
				if (node.assetPath.StartsWith(path, System.StringComparison.Ordinal))
				{
					deadpool.AddLocked(node, false);
				}
			}

			deadpool.FinishedChangingLockList();
		}

		void UnlockObjectsInPath(string path)
		{
			foreach (var node in db.assets.Values)
			{
				if (node.assetPath.StartsWith(path, System.StringComparison.Ordinal))
				{
					deadpool.RemoveLocked(node, false);
				}
			}

			deadpool.FinishedChangingLockList();
		}


		void AddObjectsInPath(string path)
		{
			foreach (var node in db.assets.Values)
			{
				if (node.assetPath.StartsWith(path, System.StringComparison.Ordinal))
				{
					deadpool.Add(node);
				}
			}
		}

		void RemoveObjectsInPath(string path)
		{
			foreach (var node in db.assets.Values)
			{
				if (node.assetPath.StartsWith(path, System.StringComparison.Ordinal))
				{
					deadpool.Remove(node);
				}
			}
		}

		object lastClickedNode = null;

		bool isLastClickedNodeAnAsset
		{
			get { return lastClickedNode is AssetGraphNode; }
		}

		bool isLastClickedNodeAFolder
		{
			get { return lastClickedNode is AssetTreeNode; }
		}

		AssetGraphNode lastClickedNodeAsAsset
		{
			get { return lastClickedNode as AssetGraphNode; }
		}

		AssetTreeNode lastClickedNodeAsFolder
		{
			get { return lastClickedNode as AssetTreeNode; }
		}
		
		protected override Color BackgroundColour()
		{
			return DefaultBackgroundColor * 1.25f;
		}
		
		protected override void DrawGui()
		{
			ShowDialog();
		}
		
		// MAIN
		void ShowDialog()
		{
			Color savedColour = GUI.color;

			Rect window = this.position;

			//GUILayout.Label( string.Format( "R=( {0}, {1} {2} x {3} )", window.left, window.top, window.width, window.height ) );
			//EditorGUILayout.Separator();

			float svUpperPanel = window.height / 2f;
			float svHorizontalPanelWidth = window.width / 2f;
			float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			GUILayout.BeginHorizontal(GUILayout.Height(svUpperPanel));

			refreshDisplay = false;

			// HIERARCHY VIEW
			{
				GUILayout.BeginVertical(GUILayout.Width(svHorizontalPanelWidth));

				GUILayout.Label("Unity Asset Dependency Graph");

				if (GUILayout.Button("Scan"))
				{
					db.FullScan(true);
					refreshDisplay = true;
				}

				//scrollPositionMain = GUILayout.BeginScrollView( scrollPositionMain, false, true, GUILayout.Height( window.height / 2f ) );
				scrollPositionMain = GUILayout.BeginScrollView(scrollPositionMain, false, true);

				//ShowSimpleFoldoutTree( refreshDisplay );
				ShowClickableFoldoutTree(refreshDisplay);

				GUILayout.EndScrollView();

				GUILayout.EndVertical();
			}

			// DEAD POOL
			{
				GUILayout.BeginVertical();

				GUILayout.Label("Dead Pool");

				OnGUI_DeadPool(svUpperPanel - itemHeight); // todo: would prefer to get the actual sizes from the layout engine but I can't get it to work yet

				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();

			EditorGUILayout.Separator();

			if (lastClickedNode == null)
			{
				GUILayout.Label("Asset: <none selected>");
			}
			else
			{
				// Selected Node Info
				OnGUI_DetailsPane(window.height - svUpperPanel, window.width);
			}

			EditorGUILayout.Separator();

			if (db.assets != null)
			{
				EditorGUILayout.Separator();
				GUILayout.Label("Found " + db.assets.Count + " assets"); // so far.....
			}

			EditorGUILayout.Separator();

		}

		// DEAD POOL
		private void OnGUI_DeadPool(float scrollHeight)
		{
			float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			float svHeight = scrollHeight;

			Color savedColour = GUI.color;

			// MARK
			{
				GUILayout.BeginHorizontal();
				savedColour = GUI.color;
				if ((lastClickedNodeAsAsset != null) && (!deadpool.isMarkedExplicitly(lastClickedNodeAsAsset)))
				{
					GUI.color = Color.red;
					if (GUILayout.Button("Mark " + lastClickedNodeAsAsset.localName))
					{
						deadpool.Add(lastClickedNodeAsAsset);
					}

					svHeight -= itemHeight;
				}

				if ((lastClickedNodeAsAsset != null) && (deadpool.isMarkedExplicitly(lastClickedNodeAsAsset)))
				{
					GUI.color = Color.red;
					if (GUILayout.Button("Unmark " + lastClickedNodeAsAsset.localName))
					{
						deadpool.Remove(lastClickedNodeAsAsset);
					}

					svHeight -= itemHeight;
				}

				GUI.color = savedColour;
				GUILayout.EndHorizontal();
			}

			// LIST
			{
				scrollPositionDeadPool = GUILayout.BeginScrollView(scrollPositionDeadPool, false, true);

				int firstIndex = (int)(scrollPositionDeadPool.y / itemHeight);

				deadpool.Update();

				GUIStyle textButton = ToolStyles.textButtonWithStyleState(Palette.whiteText);
				textButton.alignment = TextAnchor.LowerLeft;

				int itemNo = 0;
				foreach (var agn in deadpool.marked)
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

					if (deadpool.isLocked(agn))
						GUI.color = Palette.locked_asset(agn.inBuild);
					else
						GUI.color = Palette.deadpool_asset(agn.inBuild);

					bool clicked = GUILayout.Button(agn.assetPath, textButton, GUILayout.ExpandWidth(true));
					if (clicked)
					{
						lastClickedNode = agn;
					}

					itemNo++;
				}

				foreach (var agn in deadpool.markedDependants)
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


					if (deadpool.isLocked(agn))
						GUI.color = Palette.locked_asset(agn.inBuild);
					else
						GUI.color = Palette.deadpooldependent_asset(agn.inBuild);

					bool clicked = GUILayout.Button(agn.assetPath, textButton, GUILayout.ExpandWidth(true));
					if (clicked)
					{
						lastClickedNode = agn;
					}

					itemNo++;
				}

				GUILayout.EndScrollView();
			}

			// DELETE
			{
				GUI.enabled = deadpool.SafeToDelete();
				GUI.color = Color.red;
				if (GUILayout.Button("[DANGER!] Delete these files"))
				{
					//! only if there are no locked files in our list!
					deadpool.DeadPoolExecute(true);

					// Invalidate DB
					db.Reset();

					// And display
					refreshDisplay = true;
				}
			}

			// UNMARK
			{
				GUILayout.BeginHorizontal();
				GUI.color = Color.green;
				GUI.enabled = true;
				if (GUILayout.Button("Put Back Code files (.cs/.js/.dll)"))
				{
					deadpool.RemoveByExtension("cs");
					deadpool.RemoveByExtension("js");
					deadpool.RemoveByExtension("dll");
				}

				GUI.color = Color.cyan;

				if (GUILayout.Button("Unmark all locked files"))
				{
					deadpool.RemoveAllLockedFiles();
				}
			}
			GUILayout.EndHorizontal();

			GUI.color = savedColour;
		}
	}
}


