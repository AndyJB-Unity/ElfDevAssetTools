using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ElfDev
{
	public partial class TextureDeDuplicator : ElfDevEditorWindow<TextureDeDuplicator>
	{
		Vector2 scrollPosition = new Vector2(0, 0);

		[MenuItem("ElfDev Asset Insights/Texture DeDuplicator Tool")]
		static void OpenWindow()
		{
			ShowWindow(true);
		}

		public  TextureDeDuplicator(): base("Texture De-Duplicator")
		{
			minSize = new Vector2(400f, 300f);
		}

		string[] FindAllTexturesInProject()
		{
			// Array of GUIDs
			return AssetDatabase.FindAssets("t:Texture2D");
		}

		class TextureEntry : TextureEntryBase
		{
			public FileHash md5hash;

			public bool unFolded = false;

			public bool markedForRemap = false; // When true remap button will remap references to this texture to the master
			public bool markedAsMaster = false; // When true remap button will change references to marked textures to reference to this one

			public TextureEntry(string guid_)
				: base(guid_)
			{
				md5hash = new FileHash(assetPath);
			}

			public override Texture InitAsset()
			{
				return base.InitAsset();
			}
		}

		Dictionary<string, TextureEntry> texturesByGuid = null;

		void MarkBuildFiles()
		{
			TextureEntryBase.MarkBuildFiles<TextureEntry>(true, ref texturesByGuid);
		}

		void CollectDependencies(bool showProgress)
		{
			TextureEntryBase.CollectDependencies<TextureEntry>(showProgress, ref texturesByGuid);
		}

		class HashBucket
		{
			public FileHash hash;
			public bool unFolded = false;
			public List<TextureEntry> textures = new List<TextureEntry>();

			public Texture asset; // Now only loading single texture for each active bucket

			public HashBucket(FileHash fh)
			{
				hash = fh;
			}
		}

		Dictionary<string, HashBucket> texturesByHash = null;

		void ResetProject()
		{
			texturesByGuid = null;
			texturesByHash = null;
		}

		void ScanProject()
		{
			EditorUtility.DisplayProgressBar("Scanning... ", "Scanning Textures", 0.0f);

			string[] guids = FindAllTexturesInProject();

			texturesByGuid = new Dictionary<string, TextureEntry>();
			texturesByHash = new Dictionary<string, HashBucket>();

			// Gather Textures
			int index = 0;
			foreach (var guid in guids)
			{
				TextureEntry te = new TextureEntry(guid);
				texturesByGuid[guid] = te;

				EditorUtility.DisplayProgressBar("Scanning... ", te.assetPath, ((float)index / guids.Length));
				++index;

				string hashString = te.md5hash.ToString();

				if (!texturesByHash.ContainsKey(hashString))
				{
					texturesByHash[hashString] = new HashBucket(te.md5hash);
				}

				texturesByHash[hashString].textures.Add(te);

			}

			// End progress bar
			EditorUtility.ClearProgressBar();

			// Load Textures for active buckets ( todo: should I further trim out unreferenced textures? - though always keep those in Resources folders? )
			EditorUtility.DisplayProgressBar("Loading... ", "Loading Textures", 0.0f);
			index = 0;
			foreach (var bucket in texturesByHash.Values)
			{
				if (bucket.textures.Count <= 1) // Only interested in duplicates
					continue;

				foreach (var te in bucket.textures)
				{
					EditorUtility.DisplayProgressBar("Loading... ", te.assetPath, ((float)index / texturesByHash.Count));

					Texture asset = te.InitAsset();
					if (asset != null)
					{
						bucket.asset = asset; // point to a texture, doesn't matter which one
					}
				}

				++index;
			}

			// End progress bar
			EditorUtility.ClearProgressBar();

			// Get things that depend on these textures
			CollectDependencies(true);

			// Setup the Build FIlter
			MarkBuildFiles();
		}

		class CustomFoldout
		{
			Texture iconOpen = null;
			Texture iconClosed = null;

			GUIContent contentFoldoutOpen = null;
			GUIContent contentFoldoutClosed = null;

			GUIStyle contentFoldoutStyle = null;

			public CustomFoldout() { }

			public void Init()
			{
				if (iconOpen != null)
					return;

				iconOpen = PackageAsset.iconOpen;
				iconClosed = PackageAsset.iconClosed;

				contentFoldoutOpen = new GUIContent(iconOpen);
				contentFoldoutClosed = new GUIContent(iconClosed);

				contentFoldoutStyle = new GUIStyle(EditorStyles.foldout);
				contentFoldoutStyle.fixedWidth = 16f;
				contentFoldoutStyle.fixedHeight = 16f;
			}

			public bool Foldout(bool unFolded) // Content free control!
			{
				Init();
				bool clicked = GUILayout.Button(unFolded ? contentFoldoutOpen : contentFoldoutClosed, contentFoldoutStyle, GUILayout.Width(16f));
				if (clicked)
					unFolded = !unFolded;
				return unFolded;
			}
		}

		CustomFoldout myFoldout = new CustomFoldout();

		class Palette : StylePalette { }

		void ShowDialog()
		{
			GUILayout.Label("DeDuplicate Textures");

			if (texturesByGuid == null)
			{
				EditorGUILayout.HelpBox("Compile a List of Texture Dupilcates in your project/build. You can remap textures to help reduce final build sizes", MessageType.Info, true );
				EditorGUILayout.HelpBox("Currently only files with the same bit equivalent source file, and import settings are considered equal. (Pixel equality is not considered yet)", MessageType.Warning, true );

				//! todo: pixel equality
				
				if ( GUILayout.Button( "Scan" ) )
					ScanProject();
				if (texturesByGuid == null)
					return;
			}

			GUILayout.Label("Textures found = " + texturesByGuid.Count);

			GUIStyle textButtonStyle = ToolStyles.textButtonWithStyleState(Palette.cyanText);
			textButtonStyle.alignment = TextAnchor.MiddleLeft;
			textButtonStyle.stretchWidth = true;

			GUIStyle depButtonStyle = ToolStyles.textButtonWithStyleState(Palette.yellowText);
			depButtonStyle.alignment = TextAnchor.MiddleLeft;
			depButtonStyle.stretchWidth = true;

			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

			foreach (var bucket in texturesByHash.Values)
			{
				if (bucket.textures.Count <= 1) // Only interested in duplicates
					continue;

				int dependedItemCount = 0; // Dim buckets with one or less referenced textures
				foreach (var te in bucket.textures)
				{
					dependedItemCount += (te.dependents.Count > 0) ? 1 : 0;
				}

				if (dependedItemCount <= 1)
				{
					GUI.color = Color.grey;
				}

				GUILayout.BeginHorizontal();
				GUILayout.Space(5f);

				GUILayout.Label(bucket.textures.Count.ToString(), GUILayout.Width(20f));

				// ...apparently the only way to actually draw the texture at the size we want!
				GUIContent customIcon = new GUIContent(EditorGUIUtility.whiteTexture);
				Rect rr = GUILayoutUtility.GetRect(customIcon, ToolStyles.fixedIconRect);

				//GUI.DrawTexture( rr, bucket.asset );
				EditorGUI.DrawPreviewTexture(rr, bucket.asset);
				bucket.unFolded = EditorGUILayout.Foldout(bucket.unFolded, bucket.hash.ToString());
				GUILayout.EndHorizontal();

				if (bucket.unFolded)
				{
					foreach (var te in bucket.textures)
					{
						using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
						{
							GUILayout.Space(54f);

							if (te.dependents.Count < 1)
								GUI.color = Color.grey;

							GUILayout.Label(te.dependents.Count.ToString(), GUILayout.Width(24f));

							using (new GUILayout.HorizontalScope(GUILayout.MaxWidth(16f)))
							{
								if (te.dependents.Count > 0)
								{
									//te.unFolded = EditorGUILayout.Foldout( te.unFolded, GUIContent.none );
									te.unFolded = myFoldout.Foldout(te.unFolded);
								}
								else
								{
									GUILayout.Space(16f);
								}
							}

							bool markedAsMaster = EditorGUILayout.Toggle(te.markedAsMaster, GUILayout.Width(24f));
							if (markedAsMaster && (markedAsMaster != te.markedAsMaster))
							{
								// There can only be one master
								foreach (var te2 in bucket.textures)
								{
									te2.markedAsMaster = false;
								}

								te.markedAsMaster = true;
							}

							te.markedForRemap = EditorGUILayout.Toggle(te.markedForRemap, GUILayout.Width(24f));

							bool clicked = GUILayout.Button(te.assetPath, textButtonStyle, GUILayout.ExpandWidth(true));
							GUILayout.FlexibleSpace();
							if (clicked)
							{
								Selection.activeObject = te.asset;
							}

							GUILayout.Label(te.asset.dimension.ToString(), GUILayout.Width(60f));
							GUILayout.Label(te.asset.width.ToString(), GUILayout.Width(40f));
							GUILayout.Label(te.asset.height.ToString(), GUILayout.Width(40f));
							GUILayout.Label(te.asset.filterMode.ToString(), GUILayout.Width(60f));
							GUILayout.Label(te.asset.wrapMode.ToString(), GUILayout.Width(60f));

							Texture2D te2d = te.asset as Texture2D;
							if (te2d)
							{
								GUILayout.Label(te2d.alphaIsTransparency ? "alphaIsT" : " ", GUILayout.Width(60f));
								GUILayout.Label(te2d.mipmapCount.ToString(), GUILayout.Width(60f));
								GUILayout.Label(te2d.format.ToString(), GUILayout.Width(60f));

								TextureImporter ti = TextureImporter.GetAtPath(te.assetPath) as TextureImporter;
								if (ti != null)
								{
									GUILayout.Label(ti.sRGBTexture ? "sRGB" : "Linear", GUILayout.Width(60f));
								}
								else
								{
									GUILayout.Label("ERROR", GUILayout.Width(60f));
								}
							}
							else
							{
								GUILayout.Label("", GUILayout.Width(60f));
								GUILayout.Label("", GUILayout.Width(60f));
								GUILayout.Label("", GUILayout.Width(60f));
								GUILayout.Label("", GUILayout.Width(60f));
							}

							float sizeKB = ((float)te.estimatedSizeBytes / 1024f);
							float sizeMB = (sizeKB / 1024f);
							string estimatedSize = sizeMB > 1f ? sizeMB.ToString("0.00") + "MB" : sizeKB.ToString("0.0") + "KB";
							GUILayout.Label(estimatedSize, GUILayout.Width(60f));

							//GUILayout.Label( string.Format( "{0:F2}Mb/{1:F2}Mb", sizeStorageMB, sizeRuntimeMB ), GUILayout.Width( 100f ) );

							if (te.dependents.Count < 1)
							{
								GUI.color = Color.white;
							}
						}

						if (te.unFolded)
						{
							foreach (var dep in te.dependents)
							{
								GUILayout.BeginHorizontal();
								GUILayout.Space(172f);

								//GUILayout.Label( dep );

								bool clicked = GUILayout.Button(dep, depButtonStyle, GUILayout.ExpandWidth(true));
								GUILayout.FlexibleSpace();
								if (clicked)
								{
									Selection.activeObject = AssetDatabase.LoadAssetAtPath(dep, typeof(Object));
								}

								GUILayout.EndHorizontal();
							}
						}
					}
				}

				GUI.color = Color.white;

				bool hasMaster = false;
				bool hasRemaps = false;
				foreach (var te3 in bucket.textures)
				{
					if (te3.markedAsMaster)
						hasMaster = true;
					if (te3.markedForRemap && !te3.markedAsMaster && (te3.dependents.Count > 0))
						hasRemaps = true;
				}

				if (hasMaster && hasRemaps)
				{
					// Remap becomes available!
					if (GUILayout.Button("Remap Texture", GUILayout.ExpandWidth(true)))
					{
						// Do the remap!
						RemapBucket(bucket);

						// Do a full rescan
						ResetProject();
					}
				}
			}

			GUILayout.EndScrollView();

			GUILayout.Space(1f);
		}

		protected override void DrawGui()
		{
			ShowDialog();
		}

		//	void CondenseReferences( Texture[] from, Texture to );

		void RemapBucket(HashBucket bucket)
		{
			// Work out the entries involved
			TextureEntry teMaster = null;
			List<TextureEntry> teRemaps = new List<TextureEntry>();

			foreach (var te in bucket.textures)
			{
				if (te.markedAsMaster)
					teMaster = te;
				if (te.markedForRemap && !te.markedAsMaster && (te.dependents.Count > 0))
					teRemaps.Add(te);
			}

			// Remap!
			foreach (var rem in teRemaps)
			{
				Debug.Log("Remapping references from " + rem.assetPath + " to " + teMaster.assetPath);
				Object target = rem.asset;
				foreach (var dep in rem.dependents)
				{
					Debug.Log("  Editing file " + dep);

					Object obj = AssetDatabase.LoadAssetAtPath(dep, typeof(Object));

					bool updated = false;

					SerializedObject serializedObject = new UnityEditor.SerializedObject(obj);
					SerializedProperty prop = serializedObject.GetIterator();
					while (prop.Next(true) == true)
					{
						if (SerializedPropertyType.ObjectReference == prop.propertyType)
						{
							if (prop.objectReferenceValue == target)
							{
								Debug.Log("    SerObj: " + serializedObject.targetObject.name + ", Property: " + prop.displayName + ", Type: " + prop.propertyType.ToString());
								prop.objectReferenceValue = teMaster.asset;
								updated = true;
							}
						}
					}

					if (updated)
					{
						serializedObject.ApplyModifiedProperties();
						serializedObject.Update();
					}

					// todo - scene remaps? 
				}
			}

			// Required?
			AssetDatabase.SaveAssets();
		}

		/*
		private void Debug_DeepScanObject( Object obj )
		{
			const System.Reflection.BindingFlags flags = 
				System.Reflection.BindingFlags.NonPublic | 
				System.Reflection.BindingFlags.Public | 
				System.Reflection.BindingFlags.Instance | 
				System.Reflection.BindingFlags.Static;
	
			System.Reflection.FieldInfo[] fields = obj.GetType().GetFields( flags );
	
			foreach ( System.Reflection.FieldInfo fieldInfo in fields )
			{
				Debug.Log( "Obj: " + obj.name + ", Field: " + fieldInfo.Name + ", Type: " + fieldInfo.FieldType.FullName );
			}
			System.Reflection.PropertyInfo[] properties = obj.GetType().GetProperties( flags );
			foreach ( System.Reflection.PropertyInfo propertyInfo in properties )
			{
				Debug.Log( "Obj: " + obj.name + ", Property: " + propertyInfo.Name + ", Type: " + propertyInfo.PropertyType.FullName );
			}
		}
	
		private void Debug_DeepScanSerializedObject( Object obj, Object target )
		{
			SerializedObject serializedObject = new UnityEditor.SerializedObject( obj );
			SerializedProperty prop = serializedObject.GetIterator();
			while ( prop.Next( true ) == true )
			{
				if ( SerializedPropertyType.ObjectReference == prop.propertyType )
				{
					if ( prop.objectReferenceValue == target )
					{
						Debug.Log( "SerObj: " + serializedObject.targetObject.name + ", Property: " + prop.displayName + ", Type: " + prop.propertyType.ToString() );
					}
				}
			}
		} */

		// Fin
	}


}