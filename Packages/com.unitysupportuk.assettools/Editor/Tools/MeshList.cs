#define EXCLUDE_STREAMING_ASSETS		// They have no dependencies anyway, no need to waste time or space on them
#define USING_DEPS

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Linq;
using System.Text.RegularExpressions;

namespace ElfDev
{
	public class MeshList : ElfDevEditorWindow<MeshList>
	{

		private static StringMemoizer pathMemoizer = new StringMemoizer();
		private static StringMemoizer displayFieldMemoizer = new StringMemoizer();

		public  MeshList(): base("Mesh List")
		{
			minSize = new Vector2(400f, 300f);
		}
		
		[MenuItem("ElfDev Asset Insights/Mesh List Tool")]
		static void OpenWindow()
		{
			ShowWindow(true);
		}

#if USING_DEPS
        Dictionary<string, AssetFileEntry> assetDependencyInfo = null;
#endif

		class MeshEntry
		{
			public string guid;
			public string assetPath;

			public Mesh asset = null;

#if USING_DEPS
			public AssetFileEntry fileInfo = null;
#endif

			public string d_name;

			public string d_vertexCount;
			public string d_submeshes;
			public bool has_colors;
			public bool has_normals;
			public bool has_tangents;
			public bool has_uv1;
			public bool has_uv2;
			public bool has_uv3;
			public bool has_uv4;
			public long estimatedSizeBytes;

			public bool d_inBuild;

			public int countMeshes; // For AssetMode
			public int countVerts; // For AssetMode
			public long countBytes; // For AssetMode

			public string d_estimatedSize;

			public long EstimateMeshSize()
			{
				long t = asset.vertexCount * (3 * 4);
				t += has_colors ? asset.vertexCount * (1 * 4) : 0;
				t += has_normals ? asset.vertexCount * (3 * 4) : 0;
				t += has_tangents ? asset.vertexCount * (3 * 4) : 0;
				t += has_uv1 ? asset.vertexCount * (2 * 4) : 0;
				t += has_uv2 ? asset.vertexCount * (2 * 4) : 0;
				t += has_uv3 ? asset.vertexCount * (2 * 4) : 0;
				t += has_uv4 ? asset.vertexCount * (2 * 4) : 0;
				t += asset.triangles.Length * 2;
				return t;
			}

			public void Cache()
			{
				d_name = displayFieldMemoizer.get(asset.name);

				d_vertexCount = displayFieldMemoizer.get(asset.vertexCount.ToString());
				d_submeshes = displayFieldMemoizer.get(asset.subMeshCount.ToString());
				has_colors = (asset.colors != null) && (asset.colors.Length > 0);
				has_normals = (asset.normals != null) && (asset.normals.Length > 0);
				has_tangents = (asset.tangents != null) && (asset.tangents.Length > 0);
				has_uv1 = (asset.uv != null) && (asset.uv.Length > 0);
				has_uv2 = (asset.uv2 != null) && (asset.uv2.Length > 0);
				has_uv3 = (asset.uv3 != null) && (asset.uv3.Length > 0);
				has_uv4 = (asset.uv4 != null) && (asset.uv4.Length > 0);

				//estimatedSizeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong( asset );	// Over estimates a lot
				estimatedSizeBytes = EstimateMeshSize(); // Underestimates a bit

				d_estimatedSize = displayFieldMemoizer.get( Formatting.ByteClumpedString(estimatedSizeBytes) );

				d_inBuild = fileInfo != null ? fileInfo.inBuild : true;
			}

			public void CacheAssetMode()
			{
				string savedName = d_name;

				d_name = displayFieldMemoizer.get(savedName);
				d_vertexCount = displayFieldMemoizer.get(countVerts.ToString());
				d_submeshes = displayFieldMemoizer.get(countMeshes.ToString());

				estimatedSizeBytes = countBytes;

				d_estimatedSize = displayFieldMemoizer.get( Formatting.ByteClumpedString(estimatedSizeBytes) );
			}
		}

		List<MeshEntry> meshes = null;
		HashSet<string> uniqueAssetGuids = null;

		List<MeshEntry> meshesFiltered = null; // Actually just filtered for now

		public enum ColumnTag : int
		{
			MainAsset = 0,
			Verts,
			Submeshes,
			Col,
			Nrm,
			Tan,
			uv1,
			uv2,
			uv3,
			uv4,
			Size,
			Mesh,
			ColummTagMax_
		}

		SortColumnButton.ColumnState[] sortCols = new SortColumnButton.ColumnState[(int)ColumnTag.ColummTagMax_];

		string[] FindAllMeshesInProject()
		{
			return AssetDatabase.FindAssets("t:mesh");
		}


		void ScanProject()
		{
			if (meshes != null)
				return;

			EditorUtility.DisplayProgressBar("Scanning... ", "Scanning for Meshes", 0.0f);

			meshes = new List<MeshEntry>();

			string[] meshGuids = FindAllMeshesInProject();
			uniqueAssetGuids = new HashSet<string>(meshGuids);

#if USING_DEPS
			AssetFileEntry.CollectDependencies(true, uniqueAssetGuids, out assetDependencyInfo);
			AssetFileEntry.MarkBuildFiles(true, ref assetDependencyInfo);
#endif

			int index = 0;
			foreach (var guid in uniqueAssetGuids)
			{
				string assetPath = pathMemoizer.get(AssetDatabase.GUIDToAssetPath(guid));

#if EXCLUDE_STREAMING_ASSETS
				if (assetPath.StartsWith("Assets/StreamingAssets"))
				{
					++index;
					continue;
				}
#endif

				Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

				foreach (var o in allAssets)
				{
					Mesh m = o as Mesh;
					if (m == null)
						continue;

					MeshEntry me = new MeshEntry();
					me.guid = pathMemoizer.get(guid);
					me.assetPath = assetPath;
					me.asset = m;

#if USING_DEPS
					me.fileInfo = assetDependencyInfo[guid];

                    //me.DeepTrimDeps();
#endif

					me.Cache();
					meshes.Add(me);
				}

				EditorUtility.DisplayProgressBar("Scanning... ", assetPath, ((float)index / uniqueAssetGuids.Count));
				++index;
			}

			EditorUtility.ClearProgressBar();
		}

		bool listIsFiltered = true;
		bool listIsAssetMode = false;

		long meshTotalSize = 0;

		Vector2 scrollPositionA = new Vector2(0, 0);
		Vector2 scrollPositionB = new Vector2(0, 0);

		MeshEntry SelectedEntry = null;

		string filterString = "";

		void FilterMeshes()
		{
			if (listIsFiltered)
			{
				// Filter the List
				meshesFiltered = new List<MeshEntry>(meshes);
				meshesFiltered = meshesFiltered.Where((t) => t.d_inBuild).ToList();
			}
			else
			{
				// Unfilter the list
				meshesFiltered = new List<MeshEntry>(meshes);
			}

			// Calculated Size Totals
			meshTotalSize = 0;
			foreach (var me in meshesFiltered)
			{
				meshTotalSize += me.estimatedSizeBytes;
			}

			if (listIsAssetMode)
			{
				// Aggregate the list
				var meshesAggregated = new Dictionary<string, MeshEntry>();
				foreach (var me in meshesFiltered)
				{
					if (meshesAggregated.ContainsKey(me.assetPath))
					{
						MeshEntry targetEntry = meshesAggregated[me.assetPath];
						targetEntry.countMeshes += 1;
						targetEntry.countVerts += me.asset.vertexCount;
						targetEntry.countBytes += me.estimatedSizeBytes;
						targetEntry.has_colors = targetEntry.has_colors || me.has_colors;
						targetEntry.has_normals = targetEntry.has_normals || me.has_normals;
						targetEntry.has_tangents = targetEntry.has_tangents || me.has_tangents;
						targetEntry.has_uv1 = targetEntry.has_uv1 || me.has_uv1;
						targetEntry.has_uv2 = targetEntry.has_uv2 || me.has_uv2;
						targetEntry.has_uv3 = targetEntry.has_uv3 || me.has_uv3;
						targetEntry.has_uv4 = targetEntry.has_uv4 || me.has_uv4;
						targetEntry.d_name = targetEntry.d_name + ", " + me.d_name;
					}
					else
					{
						MeshEntry targetEntry = new MeshEntry();
						targetEntry.guid = me.guid;
						targetEntry.assetPath = me.assetPath;
						targetEntry.asset = me.asset;
#if USING_DEPS
						targetEntry.fileInfo = me.fileInfo;
#endif
						targetEntry.d_name = me.d_name;
						targetEntry.asset = me.asset;
						targetEntry.countMeshes = 1;
						targetEntry.countVerts = me.asset.vertexCount;
						targetEntry.countBytes = me.estimatedSizeBytes;
						targetEntry.has_colors = me.has_colors;
						targetEntry.has_normals = me.has_normals;
						targetEntry.has_tangents = me.has_tangents;
						targetEntry.has_uv1 = me.has_uv1;
						targetEntry.has_uv2 = me.has_uv2;
						targetEntry.has_uv3 = me.has_uv3;
						targetEntry.has_uv4 = me.has_uv4;
						targetEntry.has_uv4 = me.has_uv4;
						targetEntry.d_inBuild = me.d_inBuild;

						meshesAggregated[me.assetPath] = targetEntry;
					}
				}

				foreach (var me in meshesAggregated.Values)
				{
					me.CacheAssetMode();
				}

				meshesFiltered = new List<MeshEntry>(meshesAggregated.Values);

			}

			// else, leave the list alone, it's fine

			// but filter it if required..
			if (filterString != "")
			{
				Regex filterRegex = null;
				try
				{
					filterRegex = new Regex(filterString, RegexOptions.IgnoreCase);
				}
				catch { }

				meshesFiltered = meshesFiltered.Where(
					a => filterString == "" ? true : filterRegex != null ? filterRegex.IsMatch(a.assetPath) : true
				).ToList();
			}

		}

		void SortMeshes(ColumnTag active)
		{
			bool sortUp = sortCols[(int)active].sortUp;

			meshesFiltered.Sort(
				(r1, r2) =>
				{
					int result = 0;
					switch (active)
					{
						case ColumnTag.MainAsset:
							result = r1.assetPath.CompareTo(r2.assetPath);
							break;
						case ColumnTag.Verts:
							result = int.Parse(r1.d_vertexCount).CompareTo(int.Parse(r2.d_vertexCount));
							break;
						case ColumnTag.Submeshes:
							result = int.Parse(r1.d_submeshes).CompareTo(int.Parse(r2.d_submeshes));
							break;
						case ColumnTag.Col:
							result = r1.has_colors.CompareTo(r2.has_colors);
							break;
						case ColumnTag.Nrm:
							result = r1.has_normals.CompareTo(r2.has_normals);
							break;
						case ColumnTag.Tan:
							result = r1.has_tangents.CompareTo(r2.has_tangents);
							break;
						case ColumnTag.uv1:
							result = r1.has_uv1.CompareTo(r2.has_uv1);
							break;
						case ColumnTag.uv2:
							result = r1.has_uv2.CompareTo(r2.has_uv2);
							break;
						case ColumnTag.uv3:
							result = r1.has_uv3.CompareTo(r2.has_uv3);
							break;
						case ColumnTag.uv4:
							result = r1.has_uv4.CompareTo(r2.has_uv4);
							break;
						case ColumnTag.Size:
							result = r1.estimatedSizeBytes.CompareTo(r2.estimatedSizeBytes);
							break;
						case ColumnTag.Mesh:
							result = r1.d_name.CompareTo(r2.d_name);
							break;
					}

					return sortUp ? result : -result;
				}
			);
		}

		void ShowDialogTop(float svHeight)
		{
			float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			bool sortedNeeded = false;

			bool filtered = listIsFiltered;
			bool assetMode = listIsAssetMode;

			bool initialList = false;

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.HorizontalScope(GUILayout.Width(600f)))
				{
					sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.MainAsset], sortCols, "Asset Path", GUILayout.Width(100f)) | sortedNeeded;
					filtered = GUILayout.Toggle(listIsFiltered, "In Build");
					assetMode = GUILayout.Toggle(listIsAssetMode, "Grouped");
					if (meshesFiltered != null)
					{
						GUILayout.Label(" [ " + Formatting.ByteClumpedString(meshTotalSize) + " in " + meshesFiltered.Count + " Meshes ]");
					}

					GUILayout.Label(" Filter(Regex)", GUILayout.Width(84f));
					string beforeFilter = filterString;
					filterString = EditorGUILayout.TextField(filterString, GUILayout.ExpandWidth(true));
					if (filterString != beforeFilter)
						initialList = true;

					//GUILayout.FlexibleSpace();
				}

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Verts], sortCols, "Verts", GUILayout.Width(60f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Submeshes], sortCols, assetMode ? "Meshes" : "Submeshes", GUILayout.Width(80f)) | sortedNeeded;

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Col], sortCols, "Col", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Nrm], sortCols, "Nrm", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Tan], sortCols, "Tan", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.uv1], sortCols, "uv1", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.uv2], sortCols, "uv2", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.uv3], sortCols, "uv3", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.uv4], sortCols, "uv4", GUILayout.Width(40f)) | sortedNeeded;

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Size], sortCols, "Size", GUILayout.Width(70f)) | sortedNeeded;

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Mesh], sortCols, "Mesh", GUILayout.ExpandWidth(true)) | sortedNeeded;
			} // End Horizontal

			svHeight -= itemHeight;

			using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPositionA, false, true))
			{
				scrollPositionA = scrollView.scrollPosition;

				GUI.color = Color.white;

				GUIContent contentUncheck = new GUIContent();
				GUIContent contentCheck = new GUIContent();
				contentCheck.text = "X";

				int firstIndex = (int)(scrollPositionA.y / itemHeight);

				if ((meshesFiltered == null) && (meshes.Count > 0))
				{
					// Lazy Init the list
					initialList = true;
				}

				if (initialList || (filtered != listIsFiltered) || (listIsAssetMode != assetMode))
				{
					initialList = false;

					// Reset view
					scrollPositionA = new Vector2(0, 0);

					// Filter state changed
					listIsFiltered = filtered;

					// Asset Mode changed
					listIsAssetMode = assetMode;

					// Do the filtering
					FilterMeshes();

					// Filtering changed, better re-sort too
					sortedNeeded = true;
				}

				// Yuk, I should probably functionaly decompose this function, it's getting excessively long...
				if (sortedNeeded)
				{
					ColumnTag active = ColumnTag.MainAsset;
					for (int t = 0; t < sortCols.Length; ++t)
					{
						if (sortCols[t].active)
						{
							active = (ColumnTag)t;
							break;
						}
					}

					SortMeshes(active);
				}

				if (meshesFiltered == null)
				{
					meshesFiltered = new List<MeshEntry>();
				}
				
				int itemNo = 0;
				foreach (var me in meshesFiltered)
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

					if (!me.d_inBuild)
						GUI.color = Color.grey;

					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button(me.assetPath, GUI.skin.label, GUILayout.Width(600f)))
						{
							// Clicked!
							EditorUtility.FocusProjectWindow();
							Selection.activeObject = me.asset;
							SelectedEntry = me;
						}

						GUILayout.Label(me.d_vertexCount, GUILayout.Width(60));
						GUILayout.Label(me.d_submeshes, GUILayout.Width(80));

						GUILayout.Label(me.has_colors ? contentCheck : contentUncheck, GUILayout.Width(40));
						GUILayout.Label(me.has_normals ? contentCheck : contentUncheck, GUILayout.Width(40));
						GUILayout.Label(me.has_tangents ? contentCheck : contentUncheck, GUILayout.Width(40));
						GUILayout.Label(me.has_uv1 ? contentCheck : contentUncheck, GUILayout.Width(40));
						GUILayout.Label(me.has_uv2 ? contentCheck : contentUncheck, GUILayout.Width(40));
						GUILayout.Label(me.has_uv3 ? contentCheck : contentUncheck, GUILayout.Width(40));
						GUILayout.Label(me.has_uv4 ? contentCheck : contentUncheck, GUILayout.Width(40));

						GUILayout.Label(me.d_estimatedSize, GUILayout.Width(70));

						GUILayout.Label(me.d_name, GUILayout.ExpandWidth(true));
					}

					GUI.color = Color.white;

					itemNo++;
				}

			} // Scroll View
		}

		void ShowDialogBottom()
		{
			if (SelectedEntry == null)
			{
				GUILayout.Label("No Mesh Asset Selected");
				return;

			}

			GUILayout.Label("Used by:");

			using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPositionB, false, true))
			{
				scrollPositionB = scrollView.scrollPosition;

				foreach (var dep in SelectedEntry.fileInfo.dependents) // todo: how to locate in project?
				{
					if (GUILayout.Button(dep, GUI.skin.label))
					{
						// Clicked!
						EditorUtility.FocusProjectWindow();
						Selection.activeObject = AssetDatabase.LoadAssetAtPath(dep, typeof(Object));
					}
				}
			}
		}

		void ShowDialog()
		{
			if (meshes == null)
			{
				EditorGUILayout.HelpBox("Compile a List of Meshes in your project/build and present them in list for easy comparison", MessageType.Info, true );
				
				if ( GUILayout.Button( "Scan" ) )
					ScanProject();

				if (meshes == null)
					return;
			}

			// Lazy Init
			for (int sc = 0; sc < sortCols.Length; ++sc)
			{
				if (sortCols[sc] == null)
					sortCols[sc] = new SortColumnButton.ColumnState();
			}

			Rect window = this.position;

			float svHeight = window.height;
			float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

#if USING_DEPS
			svHeight -= (itemHeight * 9);

			using (new EditorGUILayout.VerticalScope(GUILayout.Height(svHeight)))
			{
				ShowDialogTop(svHeight - (itemHeight * 5));
			}

			using (new EditorGUILayout.VerticalScope())
			{
				ShowDialogBottom();
			}

#else
		ShowDialogTop( svHeight );
#endif
		}

		protected override void DrawGui()
		{
			ShowDialog();
		}
	}
}

