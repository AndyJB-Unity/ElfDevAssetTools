#define EXCLUDE_STREAMING_ASSETS		// They have no dependencies anyway, no need to waste time or space on them
#define USING_DEPS

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;

namespace ElfDev
{
	public partial class TextureList : ElfDevEditorWindow<TextureList>
	{
		Vector2 scrollPosition = new Vector2(0, 0);
		Vector2 scrollPositionDep = new Vector2(0, 0);

		public  TextureList(): base("Texture List")
		{
			minSize = new Vector2(400f, 300f);
		}
		
		[MenuItem("ElfDev Asset Insights/Texture List Tool")]
		static void OpenWindow()
		{
			ShowWindow(true);
		}
		
		string[] FindAllTexturesInProject()
		{
			// Array of GUIDs
			return AssetDatabase.FindAssets("t:Texture2D");
		}

        private static StringMemoizer displayFieldMemoizer = new StringMemoizer();

		Dictionary<string, TextureEntry> texturesByGuid = null;

		List<TextureEntry> texturesSorted = null;

		TextureEntry selectedTextureEntry = null;

		void ResetProject()
		{
			texturesByGuid = null;
		}

		void MarkBuildFiles()
		{
			TextureEntryBase.MarkBuildFiles<TextureEntry>(true, ref texturesByGuid);
		}

		void CollectDependencies(bool showProgress)
		{
			TextureEntryBase.CollectDependencies<TextureEntry>(showProgress, ref texturesByGuid);
		}

		void ScanProject()
		{
			EditorUtility.DisplayProgressBar("Scanning... ", "Scanning Textures", 0.0f);

			string[] guids = FindAllTexturesInProject();

			texturesByGuid = new Dictionary<string, TextureEntry>();

			// Gather Textures
			int index = 0;
			foreach (var guid in guids)
			{
#if EXCLUDE_STREAMING_ASSETS
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if (assetPath.StartsWith("Assets/StreamingAssets"))
				{
					++index;
					continue;
				}
#endif

				TextureEntry te = new TextureEntry(guid);

				texturesByGuid[guid] = te;

				EditorUtility.DisplayProgressBar("Scanning... ", te.assetPath, ((float)index / guids.Length));
				++index;
			}

			// End progress bar
			EditorUtility.ClearProgressBar();

			// Load Textures for active buckets ( todo: should I further trim out unreferenced textures? - though always keep those in Resources folders? )
			EditorUtility.DisplayProgressBar("Loading... ", "Loading Textures", 0.0f);
			index = 0;
			foreach (var te in texturesByGuid.Values)
			{
				EditorUtility.DisplayProgressBar("Loading... ", te.assetPath, ((float)index / texturesByGuid.Count));

				te.InitAsset(); // NOTE! Call after setting the import preset as it calls display prep!!
				++index;
			}

			// End progress bar
			EditorUtility.ClearProgressBar();

#if USING_DEPS

			// Get things that depend on these textures
			CollectDependencies(true);
#endif

			// Setup the Build FIlter
			MarkBuildFiles();

			selectedTextureEntry = null;
		}

		class Palette : StylePalette { }

		bool listIsFiltered = false;		// todo: scope enum (see Callback finder)

		// todo - sort active column?
		public enum ColumnTag : int
		{
			Count = 0,
			AssetPath,
			Dim,
			W,
			H,
			Filter,
			Wrap,
			Mips,
			Format,
			Space,
			Size,
			ColummTagMax_
		}

		SortColumnButton.ColumnState[] sortCols = new SortColumnButton.ColumnState[(int)ColumnTag.ColummTagMax_];

		void SortTextures(ColumnTag active)
		{
			bool sortUp = sortCols[(int)active].sortUp;

			texturesSorted.Sort(
				(r1, r2) =>
				{
					int result = 0;
					switch (active)
					{
						case ColumnTag.Count:
							result = r1.dependents.Count.CompareTo(r2.dependents.Count);
							break;
						case ColumnTag.AssetPath:
							result = r1.assetPath.CompareTo(r2.assetPath);
							break;
						case ColumnTag.Dim:
							result = r1.asset.dimension.CompareTo(r2.asset.dimension);
							break;
						case ColumnTag.W:
							result = r1.asset.width.CompareTo(r2.asset.width);
							break;
						case ColumnTag.H:
							result = r1.asset.height.CompareTo(r2.asset.height);
							break;
						case ColumnTag.Filter:
							result = r1.asset.filterMode.CompareTo(r2.asset.filterMode);
							break;
						case ColumnTag.Wrap:
							result = r1.asset.wrapMode.CompareTo(r2.asset.wrapMode);
							break;
						case ColumnTag.Mips:
							int m1 = 0;
							int m2 = 0;
							Texture2D r1x = r1.asset as Texture2D;
							Texture2D r2x = r2.asset as Texture2D;
							if (r1x != null)
								m1 = r1x.mipmapCount;
							if (r2x != null)
								m2 = r2x.mipmapCount;
							result = m1.CompareTo(m2);
							break;
						case ColumnTag.Format:
							result = r1.d_format.CompareTo(r2.d_format);
							break;
						case ColumnTag.Space:
							result = r1.d_sRGBTexture.CompareTo(r2.d_sRGBTexture);
							break;
						case ColumnTag.Size:
							result = r1.estimatedSizeBytes.CompareTo(r2.estimatedSizeBytes);
							break;
					}

					return sortUp ? result : -result;
				}
			);
		}

		int showMip = 0;
		int previewMip = 0;
		Texture2D mipPreview = null;

		long textureTotalSize = 0;

		string filterString = "";

		void ShowDialogTop(float svHeight)
		{
			bool sortedNeeded = false;
			bool initialList = false;
			bool filtered = false;

			// Header
			using (new EditorGUILayout.HorizontalScope())
			{
				GUIContent spacerIcon = new GUIContent(EditorGUIUtility.whiteTexture);
				GUILayoutUtility.GetRect(spacerIcon, ToolStyles.fixedIconRect);
				GUILayout.Space(8f);

				// Lazy Init
				for (int sc = 0; sc < sortCols.Length; ++sc)
				{
					if (sortCols[sc] == null)
						sortCols[sc] = new SortColumnButton.ColumnState();
				}

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Count], sortCols, "", GUILayout.Width(24f)) | sortedNeeded;

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.AssetPath], sortCols, "Asset Path", GUILayout.ExpandWidth(true)) | sortedNeeded;

				using (new EditorGUILayout.HorizontalScope())
				{
					filtered = GUILayout.Toggle(listIsFiltered, "In Build");
					if (texturesSorted != null)
					{
						GUILayout.Label(" [ " + Formatting.ByteClumpedString(textureTotalSize) + " in " + texturesSorted.Count + " Textures ]");
					}

					GUILayout.Label(" Filter(Regex)", GUILayout.Width(84f));
					string beforeFilter = filterString;
					filterString = EditorGUILayout.TextField(filterString, GUILayout.ExpandWidth(true));
					if (filterString != beforeFilter)
						initialList = true;
				}

				GUILayout.FlexibleSpace();

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Dim], sortCols, "Dim", GUILayout.Width(60f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.W], sortCols, "W", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.H], sortCols, "H", GUILayout.Width(40f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Filter], sortCols, "Filter", GUILayout.Width(60f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Wrap], sortCols, "Wrap", GUILayout.Width(60f)) | sortedNeeded;

				GUILayout.Label("", GUILayout.Width(60f));
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Mips], sortCols, "Mips", GUILayout.Width(60f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Format], sortCols, "Fmt", GUILayout.Width(80f)) | sortedNeeded;
				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Space], sortCols, "Space", GUILayout.Width(60f)) | sortedNeeded;

				sortedNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Size], sortCols, "Size", GUILayout.Width(60f)) | sortedNeeded;

				GUILayout.Label("", GUILayout.Width(14f)); // Guess the size of the scrollbar!
			}

			if ((texturesSorted == null) && (texturesByGuid.Count > 0))
			{
				// Lazy Init the list
				initialList = true;
				sortCols[(int)ColumnTag.Size].active = true; // Start with size sorted
				sortCols[(int)ColumnTag.Size].sortUp = false; // Start with size sorted
			}

			if (initialList || (filtered != listIsFiltered))
			{
				initialList = false;

				scrollPosition = new Vector2(0, 0); // Reset view

				// Filter state changed
				listIsFiltered = filtered;

				if (listIsFiltered)
				{
					texturesSorted = texturesByGuid.Values.Where((t) => t.inBuild).ToList();
				}
				else
				{
					texturesSorted = new List<TextureEntry>(texturesByGuid.Values);
				}

				if (filterString != "")
				{
					Regex filterRegex = null;
					try
					{
						filterRegex = new Regex(filterString, RegexOptions.IgnoreCase);
					}
					catch { }

					texturesSorted = texturesSorted.Where(
						a => filterString == "" ? true : filterRegex != null ? filterRegex.IsMatch(a.assetPath) : true
					).ToList();
				}

				// Calculated Size Totals
				textureTotalSize = 0;
				foreach (var te in texturesSorted)
				{
					textureTotalSize += te.estimatedSizeBytes;
				}

				sortedNeeded = true;
			}

			if (sortedNeeded && (texturesSorted != null))
			{
				ColumnTag active = ColumnTag.Count;
				for (int t = 0; t < sortCols.Length; ++t)
				{
					if (sortCols[t].active)
					{
						active = (ColumnTag)t;
						break;
					}
				}

				SortTextures(active);
			}

			svHeight -= 32f;

			//dvHeight -= 32f;

			using (new EditorGUILayout.HorizontalScope(GUILayout.Height(svHeight)))
			{
				using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, false, true))
				{
					scrollPosition = scrollView.scrollPosition;

					// Optimisation Skipping Items not on screen. Assumes fixed item height
					float itemHeight = 32f + EditorGUIUtility.standardVerticalSpacing;
					int firstIndex = (int)(scrollPosition.y / itemHeight);

					int itemNo = 0;
					foreach (var te in texturesSorted)
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

						GUI.color = Color.white;

						if (te.dependents.Count <= 1)
						{
							if (te.assetPath.Contains("/Resources/") /*|| te.assetPath.Contains( "/StreamingAssets/" ) */)
							{
								// Assume Resources are always loaded
							}
							else
							{
								GUI.color = Color.grey;
							}
						}

						//GUIStyle textButtonStyle = ToolStyles.textButtonWithStyleState(Palette.whiteText);
						//GUIStyle textButtonStyle = ToolStyles.labelButtonWithStyleState(Palette.whiteText);
						GUIStyle textButtonStyle = PackageAsset.elfSkin.GetStyle("labelButton");
						textButtonStyle.alignment = TextAnchor.MiddleLeft;
						textButtonStyle.stretchWidth = true;

						using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
						{
							//GUILayout.Space( 5f );
							// ...apparently the only way to actually draw the texture at the size we want!
							GUIContent customIcon = new GUIContent(EditorGUIUtility.whiteTexture);
							Rect rr = GUILayoutUtility.GetRect(customIcon, ToolStyles.fixedIconRect);

							//GUI.DrawTexture( rr, te.asset );
							if (te.asset != null)
								EditorGUI.DrawPreviewTexture(rr, te.asset);

							GUILayout.Space(8f);

							GUILayout.Label(te.dependents.Count.ToString(), GUILayout.Width(24f));

							bool clicked = GUILayout.Button(te.assetPath, textButtonStyle, GUILayout.ExpandWidth(true));
							GUILayout.FlexibleSpace();
							if (clicked)
							{
								Selection.activeObject = te.asset;
								selectedTextureEntry = te;
								showMip = 0;
								previewMip = 0;
								if (mipPreview != null)
									DestroyImmediate(mipPreview);
							}

							GUILayout.Label(te.d_dimension, GUILayout.Width(60f));
							GUILayout.Label(te.d_width, GUILayout.Width(40f));
							GUILayout.Label(te.d_height, GUILayout.Width(40f));
							GUILayout.Label(te.d_filterMode, GUILayout.Width(60f));
							GUILayout.Label(te.d_wrapMode, GUILayout.Width(60f));

							//if ( te.isTexture2d )		// Fields will be empty if not texture2d
							{
								GUILayout.Label(te.d_alphaIsTransparency, GUILayout.Width(60f));
								GUILayout.Label(te.d_mipmapCount, GUILayout.Width(60f));
								GUILayout.Label(te.d_format, GUILayout.Width(80f));
								GUILayout.Label(te.d_sRGBTexture, GUILayout.Width(60f));
							}

							GUILayout.Label(te.d_estimatedSize, GUILayout.Width(60f));

							if (te.dependents.Count < 1)
							{
								GUI.color = Color.white;
							}

						} // End hscope

						GUI.color = Color.white;
						itemNo++;
					}
				}
			}
		}

		void ShowDialogBottom(float dvHeight)
		{
			if (selectedTextureEntry != null)
			{
				//EditorGUILayout.Separator();

				using (new EditorGUILayout.HorizontalScope(GUILayout.Height(dvHeight)))
				{
					using (new EditorGUILayout.HorizontalScope(GUILayout.Height(dvHeight), GUILayout.Width(dvHeight)))
					{
						GUIContent detailIcon = new GUIContent(EditorGUIUtility.whiteTexture);
						Rect rrd = GUILayoutUtility.GetRect(detailIcon, GUIStyle.none, GUILayout.ExpandHeight(true));

						//GUI.DrawTexture( rrd, selectedTextureEntry.asset );
						if (showMip > 0)
						{
							if (previewMip != showMip)
							{
								if (mipPreview != null)
									DestroyImmediate(mipPreview);

								Texture2D m0 = selectedTextureEntry.asset as Texture2D;

								while ((m0.width >> showMip < 4) || (m0.width >> showMip < 4))
								{
									// Sizes less than 4x4 seem to be aa problem, lets, just hack them for now, will "stick" the slider
									showMip--;
								}

								mipPreview = new Texture2D(m0.width >> showMip, m0.height >> showMip, m0.format, false);
								Graphics.CopyTexture(m0, 0, showMip, mipPreview, 0, 0);

								previewMip = showMip;
							}

							EditorGUI.DrawPreviewTexture(rrd, mipPreview);
						}
						else
						{
							EditorGUI.DrawPreviewTexture(rrd, selectedTextureEntry.asset);
						}
					}

					using (new EditorGUILayout.VerticalScope())
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							GUILayout.Label("Details");
							GUILayout.Space(32f);

							// todo: finish off mip support. Needs point sampling and to look the part for normals
							/*					if ( selectedTextureEntry.isTexture2d )
												{
													Texture2D t2d = selectedTextureEntry.asset as Texture2D;
													GUILayout.Label( "Mip ", GUILayout.Width( 24f ) );
													showMip = EditorGUILayout.IntSlider( showMip, 0, t2d.mipmapCount-1 );
												}
							*/
						}

						Color oldFg = GUI.color;

						GUI.color = Color.yellow;

						GUILayout.Label("Asset Path: " + selectedTextureEntry.assetPath + (selectedTextureEntry.inBuild ? "   [In Build]" : "   [NOT IN BUILD]"));
						GUILayout.Label("Type: " + selectedTextureEntry.d_dimension);
						GUILayout.Label(string.Format("Dimensions: {0}x{1}", selectedTextureEntry.d_width, selectedTextureEntry.d_height));
						GUILayout.Label("Mip Maps: " + selectedTextureEntry.d_mipmapCount);
						GUILayout.Label("Filter: " + selectedTextureEntry.d_filterMode);
						GUILayout.Label("Wrap: " + selectedTextureEntry.d_wrapMode);
						GUILayout.Label(string.Format("Format: {0} {1} {2}", selectedTextureEntry.d_format, selectedTextureEntry.d_sRGBTexture, selectedTextureEntry.d_alphaIsTransparency));
						GUILayout.Label("Size (Estimated): " + selectedTextureEntry.d_estimatedSize);

						GUI.color = oldFg;

						GUILayout.Space(5f);
						GUILayout.Label("Dependents");

						using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPositionDep, false, true))
						{
							scrollPositionDep = scrollView.scrollPosition;

							GUI.color = Color.cyan;

							foreach (var dep in selectedTextureEntry.dependents)
							{
								if (GUILayout.Button(dep, GUI.skin.label))
								{
									// Clicked!
									EditorUtility.FocusProjectWindow();
									Selection.activeObject = AssetDatabase.LoadAssetAtPath(dep, typeof(Object));
								}
							}

							GUI.color = oldFg;
						}
					}
				}
			}
			else
			{
				GUILayout.Label("Click on a texture for details");
			}
		}
		
		void ShowDialog()
		{
			if (texturesByGuid == null)
			{
				EditorGUILayout.HelpBox("Compile a List of Textures in your project/build and present them in list for easy comparison", MessageType.Info, true );
				
				if (GUILayout.Button("Scan"))
				{
					ScanProject();
				}

				return;
			}

			Rect window = this.position;

			float svHeight = window.height * 2f / 3f;
			float dvHeight = window.height - svHeight;

			ShowDialogTop(svHeight);
			ShowDialogBottom(dvHeight);
		}

		protected override void DrawGui()
		{
			ShowDialog();
		}

		// Fin
	}


}