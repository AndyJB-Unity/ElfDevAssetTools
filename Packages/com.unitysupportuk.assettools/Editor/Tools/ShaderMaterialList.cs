#define USING_DEPS
#define DEEP_SHADER_INFO

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using System.Linq;

using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace ElfDev
{
	public class ShaderMaterialList : ElfDevEditorWindow<ShaderMaterialList>
	{
#if USING_DEPS
		Dictionary<string, AssetFileEntry> assetDependencyInfo = null; // indexed by guid
#endif

		static bool showProgressBar = true;

		Vector2 scrollPositionLeft = new Vector2(0, 0);
		Vector2 scrollPositionRight = new Vector2(0, 0);
		Vector2 scrollPositionRightBottom = new Vector2(0, 0);

		public  ShaderMaterialList(): base("Shader and Material List")
		{
			minSize = new Vector2(400f, 300f);
		}
		
		// Evaluates all materials & shaders in the project
		[MenuItem("ElfDev Asset Insights/Shader And Material List Tool")]
		static void OpenWindow()
		{
			ShowWindow(true);
		}

		string[] FindAllMaterialInProject()
		{
			// Array of GUIDs
			return AssetDatabase.FindAssets("t:Material");
		}

		HashSet<Material> allMaterials = null;
		Dictionary<Shader, List<Material>> allShaders = null;

		List<Shader> filteredShaders = null;

#if DEEP_SHADER_INFO
		class ShaderInfo
		{
			// Additional Shader Info
			public Shader shader;

			public HashSet<string> grabPassNames = new HashSet<string>();

			bool Parse(Shader shady)
			{
				// Sift Shader source for goodies

				string assetPath = AssetDatabase.GetAssetPath(shady);
				string fullPath = Path.Combine(Path.Combine(Application.dataPath, ".."), assetPath);

				if (!File.Exists(fullPath))
					return false;

				string shaderCodeRaw = System.IO.File.ReadAllText(fullPath);

				var matches = Regex.Matches(shaderCodeRaw, "GrabPass\\s*\\{\\s*(\"(.*)\")?\\s*\\}");
				foreach (Match m in matches)
				{
					GroupCollection groups = m.Groups;

					//if ( groups.Count > 0 ) Debug.Log( "   GROUP[0] " + groups[ 0 ].Value + " @ " + groups[ 0 ].Index );
					//if ( groups.Count > 1 ) Debug.Log( "   GROUP[1] " + groups[ 1 ].Value + " @ " + groups[ 1 ].Index );
					//if ( groups.Count > 2 ) Debug.Log( "   GROUP[2] " + groups[ 2 ].Value + " @ " + groups[ 2 ].Index );

					// ugh!
					if (groups.Count > 2)
					{
						grabPassNames.Add(groups[2].Value);
					}
				}

				return true;
			}

			public bool Init(Shader sh)
			{
				shader = sh;

				bool parsed = Parse(sh);

				return parsed;
			}
		}

		Dictionary<Shader, ShaderInfo> shaderInfo = null;
		Dictionary<string, List<ShaderInfo>> grabPasses = null;
#endif

		void DoScan()
		{
			allMaterials = new HashSet<Material>();
			allShaders = new Dictionary<Shader, List<Material>>();

			string[] allMaterialGUIDS = FindAllMaterialInProject();

			int index = 0;
			foreach (string guid in allMaterialGUIDS)
			{
				++index;
				if (showProgressBar)
					EditorUtility.DisplayProgressBar("Scanning.. ", "Checking Materials " + index + "/" + allMaterialGUIDS.Length, (float)index / (float)allMaterialGUIDS.Length);

				string materialAssetPath = AssetDatabase.GUIDToAssetPath(guid);
				Material candidateMaterial = AssetDatabase.LoadAssetAtPath(materialAssetPath, typeof(Material)) as Material;
				if (candidateMaterial != null)
				{
					allMaterials.Add(candidateMaterial);

// todo: maybe this could be an option					
//					if (candidateMaterial.shader.name == "Standard") // Skip Standard Shader for now...
//						continue;

					if (!allShaders.ContainsKey(candidateMaterial.shader))
					{
						List<Material> newList = new List<Material>();
						newList.Add(candidateMaterial);
						allShaders.Add(candidateMaterial.shader, newList);
					}
					else
					{
						allShaders[candidateMaterial.shader].Add(candidateMaterial);
					}
				}
			}

#if DEEP_SHADER_INFO
			shaderInfo = new Dictionary<Shader, ShaderInfo>();

			foreach (var sh in allShaders.Keys)
			{
				ShaderInfo si = new ShaderInfo();
				if (si.Init(sh))
				{
					shaderInfo[sh] = si;
				}

				// Did not parse, don't add anything
			}

			// By GrabPass
			grabPasses = new Dictionary<string, List<ShaderInfo>>();

			foreach (var si in shaderInfo.Values)
			{
				foreach (var gpname in si.grabPassNames)
				{
					if (grabPasses.ContainsKey(gpname))
					{
						grabPasses[gpname].Add(si);
					}
					else
					{
						var sil = new List<ShaderInfo>();
						sil.Add(si);
						grabPasses.Add(gpname, sil);
					}
				}
			}

			// DEBUG
			foreach (var gpkv in grabPasses)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("Pass: \"" + gpkv.Key + "\"");
				sb.Append(" =>\n");
				foreach (var si in gpkv.Value)
				{
					sb.Append("     ");
					sb.Append(si.shader.name);
					sb.Append("\n");
				}

				Debug.Log(sb.ToString());
			}

#endif

#if USING_DEPS
			IEnumerable<string> allshaderGUIDS = allMaterialGUIDS.Select(
				matguid =>
				{
					string materialAssetPath = AssetDatabase.GUIDToAssetPath(matguid);
					Material candidateMaterial = AssetDatabase.LoadAssetAtPath(materialAssetPath, typeof(Material)) as Material;

					Shader sh = candidateMaterial.shader;
					string gooey = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sh));
					return gooey;
				}
			);

			// Store Asset Info for both shaders and materials
			HashSet<string> HSuniqueAssetGuids = new HashSet<string>(allshaderGUIDS);
			HSuniqueAssetGuids.UnionWith(allMaterialGUIDS);

			AssetFileEntry.CollectDependencies(true, HSuniqueAssetGuids, out assetDependencyInfo);
			AssetFileEntry.MarkBuildFiles(true, ref assetDependencyInfo);
#endif

			if (showProgressBar)
				EditorUtility.ClearProgressBar();
		}

		AssetFileEntry AssetInfoForMaterial(Material m)
		{
			string gooey = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m));

			if (assetDependencyInfo.ContainsKey(gooey))
			{
				return assetDependencyInfo[gooey];
			}

			return null;
		}

		AssetFileEntry AssetInfoForShader(Shader sh)
		{
			string gooey = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sh));

			if (assetDependencyInfo.ContainsKey(gooey))
			{
				return assetDependencyInfo[gooey];
			}

			return null;
		}

		Shader selectedShader = null;
		Material selectedMaterial = null;

		bool inBuildOnly = false;

		class Palette : StylePalette { }

		static GUIStyle flexibleIconRect_ = new GUIStyle(GUIStyle.none);

		static Texture bigFatNope = null;

		void ShowDialog()
		{
			bool doScan = false;
			if (allMaterials == null)
			{
				EditorGUILayout.HelpBox("Compile a List of Material, Shaders, and assigned Textures in your project/build and present them in list for easy consumption", MessageType.Info, true );
				doScan = GUILayout.Button("Scan");
			}

			if (doScan)
			{
				DoScan();
			}

			if (allShaders == null || (allShaders.Count == 0))
				return;

			// Option
			bool showInBuild = GUILayout.Toggle(inBuildOnly, "Show Build files only");
			if ((filteredShaders == null) || (showInBuild != inBuildOnly))
			{
				if (showInBuild)
				{
					filteredShaders = allShaders.Keys.Where( // Could not figure out the errors I got when trying to work with the kvp
						sh => (AssetInfoForShader(sh).inBuild)
					).ToList();
				}
				else
				{
					filteredShaders = allShaders.Keys.ToList();
				}
			}

			filteredShaders.Sort(
				(r1, r2) => r1.name.CompareTo(r2.name)
			);
			inBuildOnly = showInBuild;

			// Main disp..
			if (bigFatNope == null)
			{
				bigFatNope = PackageAsset.bigFatNope;
			}

			Rect window = this.position;
			float svHeight = window.height;
			float svWidth = window.width;

			float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			float widthLeft = svWidth / 4;

			if ( (selectedShader == null) && ( filteredShaders.Count>0) )
				selectedShader = filteredShaders[0];

			GUIStyle textButtonStyle = ToolStyles.textButtonWithStyleState(Palette.whiteText);
			textButtonStyle.alignment = TextAnchor.MiddleLeft;
			textButtonStyle.stretchWidth = true;

			using (new EditorGUILayout.HorizontalScope())
			{
				// LEFT
				using (new EditorGUILayout.VerticalScope(GUILayout.Width(widthLeft)))
				{
					using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPositionLeft, false, true))
					{
						scrollPositionLeft = scrollView.scrollPosition;

						foreach (var sh in filteredShaders)
						{
							//GUILayout.Label( sh.name );
							bool clicked = GUILayout.Button(sh.name, textButtonStyle, GUILayout.ExpandWidth(true));
							if (clicked)
							{
								Selection.activeObject = sh;
								selectedShader = sh;
								selectedMaterial = null; // Too confusing otherwise
							}
						}
					}

					if (GUILayout.Button("Rescan"))
					{
						DoScan();
					}
				}
				
				// RIGHT
				using (new EditorGUILayout.VerticalScope())
				{
					if (selectedShader == null)
					{
						EditorGUILayout.HelpBox( "No Valid Shaders were scanned, this is very suspicous", MessageType.Warning );
					}
					else
					{
						// Refactor this for goodness sake! What a bloody mess!
						int pcount = ShaderUtil.GetPropertyCount(selectedShader);

						// Look for Switches
						List<string> switchProps = new List<string>();
						List<string> switchNames = new List<string>();

						for (int p = 0; p < pcount; ++p)
						{
							string pname = ShaderUtil.GetPropertyName(selectedShader, p);
							if (pname.EndsWith("Switch", System.StringComparison.Ordinal))
							{
								switchProps.Add(pname);

								string tname = pname.Substring(0, pname.Length - ("Switch".Length));
								switchNames.Add(tname);
							}
						}

						List<string> viewProps = new List<string>();
						List<int> viewPropsIds = new List<int>();
						List<int> viewSwitchIndex = new List<int>();

						List<ShaderUtil.ShaderPropertyType> viewPropType = new List<ShaderUtil.ShaderPropertyType>();

						// Conf props
						// only looking for materials with unused texture slots
						for (int p = 0; p < pcount; ++p)
						{
							if (ShaderUtil.IsShaderPropertyHidden(selectedShader, p)) // We ignore hidden properties
								continue;

							if (ShaderUtil.GetPropertyType(selectedShader, p) == ShaderUtil.ShaderPropertyType.TexEnv)
							{
								string pname = ShaderUtil.GetPropertyName(selectedShader, p);

								viewProps.Add(pname);
								viewPropsIds.Add(Shader.PropertyToID(pname));
								viewPropType.Add(ShaderUtil.GetPropertyType(selectedShader, p));

								int switchIndex = -1;
								for (int sw = 0; sw < switchNames.Count; ++sw)
								{
									if (pname.StartsWith(switchNames[sw]) || (pname == switchNames[sw]))
									{
										switchIndex = sw;
										break;
									}
								}

								viewSwitchIndex.Add(switchIndex);
							}
						}

						bool sortNeeded = false;

						// Selected Shader
	//					GUIStyle headerBox = new GUIStyle(GUISkinEx.GetCurrentSkin().box);
						var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
						GUIStyle headerBox = new GUIStyle( skin.box );
						headerBox.normal.textColor = Color.white;
						GUILayout.Label(selectedShader.name, headerBox, GUILayout.ExpandWidth(true)); // todo, not use GUISKinEx

						float matFwidth = 220f; // Width pf Mat field
						float widthRight = svWidth;

						SortColumnButton.ColumnState[] sortCols = new SortColumnButton.ColumnState[viewProps.Count];

						using (new EditorGUILayout.HorizontalScope())
						{
							GUILayout.Label("", GUILayout.Width(matFwidth));

							widthRight = svWidth - widthLeft - matFwidth - EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).verticalScrollbar.fixedWidth;

							// Could lazy init if we remember to mark it dirty on shader change...
							for (int sc = 0; sc < sortCols.Length; ++sc)
							{
								sortCols[sc] = new SortColumnButton.ColumnState();

								sortCols[sc].enabled = false;

								float fieldWidth = 0;
								switch (viewPropType[sc])
								{
									case ShaderUtil.ShaderPropertyType.Color:
										fieldWidth = 90f;
										break;
									case ShaderUtil.ShaderPropertyType.Vector:
										fieldWidth = 90f;
										break;
									case ShaderUtil.ShaderPropertyType.Float:
										fieldWidth = 90f;
										break;
									case ShaderUtil.ShaderPropertyType.Range:
										fieldWidth = 90f;
										break;
									case ShaderUtil.ShaderPropertyType.TexEnv:

										//fieldWidth = 90f;
										fieldWidth = widthRight / (sortCols.Length + 0.5f);
										break;
								}

								// todo: sorting
								sortNeeded = SortColumnButton.Button(sortCols[sc], sortCols, viewProps[sc], GUILayout.Width(fieldWidth)) | sortNeeded;

							}
						}

						float texFWidth = widthRight / (sortCols.Length + 0.5f); // todo: using same size for all fields right now

						using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPositionRight, false, true))
						{
							scrollPositionRight = scrollView.scrollPosition;

							foreach (var m in allShaders[selectedShader])
							{
	#if USING_DEPS
								if (inBuildOnly)
								{
									AssetFileEntry afe = AssetInfoForMaterial(m);
									if (!afe.inBuild)
										continue;
								}
	#endif

								using (new EditorGUILayout.HorizontalScope())
								{
									using (new EditorGUILayout.VerticalScope(GUILayout.Width(matFwidth)))
									{
										//GUILayout.Label( m.name, GUILayout.Width( matFwidth ) );
										bool clicked = GUILayout.Button(m.name, textButtonStyle, GUILayout.Width(matFwidth));
										if (clicked)
										{
											Selection.activeObject = m;
											selectedMaterial = m;
										}

										GUI.enabled = false;
										for (int sw = 0; sw < switchNames.Count; ++sw)
										{
											// todo: tick cross
											GUILayout.Label(switchNames[sw] + "  " + m.GetFloat(switchProps[sw]).ToString());
										}

										GUI.enabled = true;
									}

									Color restoreCol = GUI.color;

									int vp = 0;
									foreach (string pname in viewProps) // show dependency info. texture slots
									{
										bool enabledSlot = (
											(viewSwitchIndex[vp] == -1) ||
											(m.GetFloat(switchProps[viewSwitchIndex[vp]]) != 0)
										);
										if (!enabledSlot)
											GUI.color = Color.red;

										//if ( m.GetTexture( pname ) == null )
										if (viewPropType[vp] == ShaderUtil.ShaderPropertyType.TexEnv)
										{
											if (m.GetTexture(pname) == null)
											{
												if (bigFatNope == null)
												{
													GUILayout.Label("NULL", GUILayout.Width(texFWidth)); //?
												}
												else
												{
													GUIContent customIcon = new GUIContent(EditorGUIUtility.whiteTexture);
													flexibleIconRect_.fixedWidth = flexibleIconRect_.fixedHeight = texFWidth + 4f;
													Rect rr = GUILayoutUtility.GetRect(customIcon, flexibleIconRect_);
													EditorGUI.DrawPreviewTexture(rr, bigFatNope);
												}
											}
											else
											{
												GUIContent customIcon = new GUIContent(EditorGUIUtility.whiteTexture);
												flexibleIconRect_.fixedWidth = flexibleIconRect_.fixedHeight = texFWidth + 4f;
												Rect rr = GUILayoutUtility.GetRect(customIcon, flexibleIconRect_);
												EditorGUI.DrawPreviewTexture(rr, m.GetTexture(pname));
											}

											//GUILayout.Label( "  " + pname + " = " + m.GetTexture( pname ) );
										}

										GUI.color = restoreCol;

										++vp;
									}
								}
							} // foreach material
						}
					}

#if USING_DEPS
					if (selectedMaterial != null)
					{
						using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPositionRightBottom, false, true, GUILayout.MinHeight(100f), GUILayout.MaxHeight(200f)))
						{
							scrollPositionRightBottom = scrollView.scrollPosition;

							AssetFileEntry afe = AssetInfoForMaterial(selectedMaterial);
							if (afe != null)
							{
								foreach (var dep in afe.dependents)
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
					}
#endif
				}
			}
		}

		protected override void DrawGui()
		{
			ShowDialog();
		}
	}

}

// todo: expanded version - with ALL params - or maybe just colours?

