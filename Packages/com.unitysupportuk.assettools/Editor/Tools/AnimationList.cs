#define EXCLUDE_STREAMING_ASSETS		// They have no dependencies anyway, no need to waste time or space on them
#define USING_DEPS

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Linq;
using System.Text.RegularExpressions;


namespace ElfDev
{
	public class AnimationList : ElfDevEditorWindow<AnimationList>
	{
		public AnimationList(): base("Animation List")
		{
			minSize = new Vector2(400f, 300f);
		}
		
		private static StringMemoizer pathMemoizer = new StringMemoizer();
		private static StringMemoizer displayFieldMemoizer = new StringMemoizer();

		[MenuItem("ElfDev Asset Insights/Animation List Tool")]
		static void OpenWindow()
		{
			ShowWindow(true);
		}

#if USING_DEPS
		Dictionary<string, AssetFileEntry> assetDependencyInfo = null;
#endif

		class AnimationEntry
		{
			public string guid;
			public string assetPath;

			public AnimationClip asset = null;

#if USING_DEPS
			public AssetFileEntry fileInfo = null;
#endif

			public string d_name;

			public string d_frameRate;
			public bool d_isLegacy;
			public string d_eventCount;
			public string d_lengthInSeconds;
			public string d_wrapMode;

			public long estimatedSizeBytes;
			public float workingLength;
			public int workingEventCount;

			public bool d_inBuild;

			public int countAnimations; // For AssetMode
			public float countLength; // For AssetMode
			public long countBytes; // For AssetMode

			public string d_estimatedSize;

			public long EstimateAnimationSize()
			{
				// todo: our best guess
				return UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(asset);
			}

			public void Cache()
			{
				d_name = displayFieldMemoizer.get(asset.name);

				d_frameRate = displayFieldMemoizer.get(asset.frameRate.ToString("F1"));
				d_isLegacy = asset.legacy;
				workingEventCount = (asset.events != null) ? asset.events.Length : 0;
				d_eventCount = workingEventCount.ToString();
				d_lengthInSeconds = displayFieldMemoizer.get(asset.length.ToString("F2"));
				d_wrapMode = displayFieldMemoizer.get(asset.wrapMode.ToString());

				estimatedSizeBytes = EstimateAnimationSize();

				d_estimatedSize = displayFieldMemoizer.get( Formatting.ByteClumpedString(estimatedSizeBytes) );

				workingLength = asset.length;

#if USING_DEPS
				d_inBuild = fileInfo != null ? fileInfo.inBuild : true;
#else
    			d_inBuild = true;
#endif
			}

			public void CacheAssetMode()
			{
				string savedName = d_name;

				d_name = displayFieldMemoizer.get(savedName);

				estimatedSizeBytes = countBytes;
				d_estimatedSize =  displayFieldMemoizer.get( Formatting.ByteClumpedString(estimatedSizeBytes) );

				workingLength = countLength;
				d_lengthInSeconds = displayFieldMemoizer.get(workingLength.ToString("F2"));

				d_eventCount = workingEventCount.ToString();
			}
		}

		List<AnimationEntry> anims = null;
		HashSet<string> uniqueAssetGuids = null;

		List<AnimationEntry> animsFiltered = null; // Actually just filtered for now

		public enum ColumnTag : int
		{
			MainAsset = 0,
			FPS,
			Legacy,
			Events,
			Length,
			Wrap,
			Size,
			Anim,
			ColummTagMax_
		}

		SortColumnButton.ColumnState[] sortCols = new SortColumnButton.ColumnState[(int)ColumnTag.ColummTagMax_];

		string[] FindAllAnimationsInProject()
		{
			//return AssetDatabase.FindAssets( "t:animationclip" );
			return AssetDatabase.FindAssets(""); // Look at everything..
		}


		void ScanProject()
		{
			if (anims != null)
				return;

			EditorUtility.DisplayProgressBar("Scanning... ", "Scanning for Animations", 0.0f);

			anims = new List<AnimationEntry>();

			string[] animGuids = FindAllAnimationsInProject();
			uniqueAssetGuids = new HashSet<string>(animGuids);

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

				if (assetPath.EndsWith(".unity")) // Don't search scene assets
				{
					++index;
					continue;
				}

				Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

				foreach (var o in allAssets)
				{
					AnimationClip an = o as AnimationClip;
					if (an == null)
						continue;

					if (an.name.StartsWith("__preview__"))
						continue;

					AnimationEntry ane = new AnimationEntry();
					ane.guid = pathMemoizer.get(guid);
					ane.assetPath = assetPath;
					ane.asset = an;

#if USING_DEPS
					ane.fileInfo = assetDependencyInfo[guid];
#endif

					ane.Cache();

					anims.Add(ane);
				}

				EditorUtility.DisplayProgressBar("Scanning... ", assetPath, ((float)index / uniqueAssetGuids.Count));
				++index;
			}

			EditorUtility.ClearProgressBar();
		}

		bool listIsFiltered = true;
		bool listIsAssetMode = false;

		long animTotalSize = 0;

		Vector2 scrollPositionA = new Vector2(0, 0);
		Vector2 scrollPositionB = new Vector2(0, 0);

		AnimationEntry SelectedEntry = null;

		string filterString = "";

		void FilterAnimations()
		{
			if (listIsFiltered)
			{
				// Filter the List
				animsFiltered = new List<AnimationEntry>(anims);
				animsFiltered = animsFiltered.Where((t) => t.d_inBuild).ToList();
			}
			else
			{
				// Unfilter the list
				animsFiltered = new List<AnimationEntry>(anims);
			}

			// Calculated Size Totals
			animTotalSize = 0;
			foreach (var me in animsFiltered)
			{
				animTotalSize += me.estimatedSizeBytes;
			}

			if (listIsAssetMode)
			{
				// Aggregate the list
				var animsAggregated = new Dictionary<string, AnimationEntry>();
				foreach (var ane in animsFiltered)
				{
					if (animsAggregated.ContainsKey(ane.assetPath))
					{
						AnimationEntry targetEntry = animsAggregated[ane.assetPath];
						targetEntry.countAnimations += 1;
						targetEntry.countBytes += ane.estimatedSizeBytes;
						targetEntry.countLength += ane.asset.length;
						targetEntry.d_frameRate = ane.d_frameRate;
						targetEntry.d_isLegacy = targetEntry.d_isLegacy || ane.d_isLegacy;
						targetEntry.workingEventCount += ane.workingEventCount;
						targetEntry.d_name = targetEntry.d_name + ", " + ane.d_name;
					}
					else
					{
						AnimationEntry targetEntry = new AnimationEntry();
						targetEntry.guid = ane.guid;
						targetEntry.assetPath = ane.assetPath;
						targetEntry.asset = ane.asset;
#if USING_DEPS
						targetEntry.fileInfo = ane.fileInfo;
#endif
						targetEntry.d_name = ane.d_name;
						targetEntry.asset = ane.asset;

						targetEntry.d_frameRate = ane.d_frameRate;
						targetEntry.d_isLegacy = ane.d_isLegacy;
						targetEntry.workingEventCount = ane.workingEventCount;
						targetEntry.d_wrapMode = "-";

						targetEntry.countAnimations = 1;
						targetEntry.countLength = ane.asset.length;
						;
						targetEntry.countBytes = ane.estimatedSizeBytes;
						targetEntry.d_inBuild = ane.d_inBuild;

						animsAggregated[ane.assetPath] = targetEntry;
					}
				}

				foreach (var me in animsAggregated.Values)
				{
					me.CacheAssetMode();
				}

				animsFiltered = new List<AnimationEntry>(animsAggregated.Values);

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

				animsFiltered = animsFiltered.Where(
					a => filterString == "" ? true : filterRegex != null ? filterRegex.IsMatch(a.assetPath) : true
				).ToList();
			}

		}

		void SortMeshes(ColumnTag active)
		{
			bool sortUp = sortCols[(int)active].sortUp;

			animsFiltered.Sort(
				(r1, r2) =>
				{
					int result = 0;
					switch (active)
					{
						case ColumnTag.MainAsset:
							result = r1.assetPath.CompareTo(r2.assetPath);
							break;
						case ColumnTag.FPS:
							result = r1.asset.frameRate.CompareTo(r2.asset.frameRate);
							break;
						case ColumnTag.Legacy:
							result = r1.d_isLegacy.CompareTo(r2.d_isLegacy);
							break;
						case ColumnTag.Events:
							result = r1.workingEventCount.CompareTo(r2.workingEventCount);
							break;
						case ColumnTag.Length:
							result = r1.workingLength.CompareTo(r2.workingLength);
							break;
						case ColumnTag.Wrap:
							result = r1.d_wrapMode.CompareTo(r2.d_wrapMode);
							break;
						case ColumnTag.Size:
							result = r1.estimatedSizeBytes.CompareTo(r2.estimatedSizeBytes);
							break;
						case ColumnTag.Anim:
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
				using (new EditorGUILayout.HorizontalScope(GUILayout.Width(640f)))
				{
					sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.MainAsset], sortCols, "Asset Path", GUILayout.Width(100f)) | sortedNeeded;
					filtered = GUILayout.Toggle(listIsFiltered, "In Build");
					assetMode = GUILayout.Toggle(listIsAssetMode, "Grouped");
					if (animsFiltered != null)
					{
						GUILayout.Label(" [ " + Formatting.ByteClumpedString(animTotalSize) + " in " + animsFiltered.Count + " Anims ]");
					}

					GUILayout.Label(" Filter(Regex)", GUILayout.Width(84f));
					string beforeFilter = filterString;
					filterString = EditorGUILayout.TextField(filterString, GUILayout.ExpandWidth(true));
					if (filterString != beforeFilter)
						initialList = true;

					//GUILayout.FlexibleSpace();
				}

				if (!assetMode)
					sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.FPS], sortCols, "FPS", GUILayout.Width(50f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Legacy], sortCols, "Legacy", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Events], sortCols, "Events", GUILayout.Width(60f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Length], sortCols, "Time", GUILayout.Width(60f)) | sortedNeeded;
				if (!assetMode)
					sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Wrap], sortCols, "Wrap", GUILayout.Width(60f)) | sortedNeeded;

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Size], sortCols, "Size", GUILayout.Width(70f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Anim], sortCols, "Anim", GUILayout.ExpandWidth(true)) | sortedNeeded;
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

				if ((animsFiltered == null) && (anims.Count > 0))
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
					FilterAnimations();

					// Filtering changed, better re-sort too
					sortedNeeded = true;
				}

				if (animsFiltered == null)
					animsFiltered = new List<AnimationEntry>();		// An empty list is less crashy!
				
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

				int itemNo = 0;
				foreach (var me in animsFiltered)
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
						if (GUILayout.Button(me.assetPath, GUI.skin.label, GUILayout.Width(640f)))
						{
							// Clicked!
							EditorUtility.FocusProjectWindow();
							Selection.activeObject = me.asset;
							SelectedEntry = me;
						}

						if (!assetMode)
							GUILayout.Label(me.d_frameRate, GUILayout.Width(50f));
						GUILayout.Label(me.d_isLegacy ? "X" : "", GUILayout.Width(40f));
						GUILayout.Label(me.d_eventCount, GUILayout.Width(60f));
						GUILayout.Label(me.d_lengthInSeconds, GUILayout.Width(60f));
						if (!assetMode)
							GUILayout.Label(me.d_wrapMode, GUILayout.Width(60f));

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
				GUILayout.Label("No Animation Asset Selected");
				return;

			}

			GUILayout.Label("Used by:");

			using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPositionB, false, true))
			{
				scrollPositionB = scrollView.scrollPosition;

#if USING_DEPS
				foreach (var dep in SelectedEntry.fileInfo.dependents) // todo: how to locate in project?
				{
					if (GUILayout.Button(dep, GUI.skin.label))
					{
						// Clicked!
						EditorUtility.FocusProjectWindow();
						Selection.activeObject = AssetDatabase.LoadAssetAtPath(dep, typeof(Object));
					}
				}
#else
			GUILayout.Label( "Dependency data not enabled. Edit source coe to re-enable" );
#endif
			}
		}

		void ShowDialog()
		{
			if (anims == null)
			{
				EditorGUILayout.HelpBox("Compile a List of Animations in your project/build and present them in list for easy comparison", MessageType.Info, true );

				if (GUILayout.Button("Scan"))
				{
					ScanProject();
				}

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

