using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// todo: restore to prefab editor if it was active when scan started?

namespace ElfDev
{
	public class ComponentManifest : ElfDevEditorWindow<ComponentManifest>
	{
		Vector2 scrollPositionUpper = new Vector2(0, 0);
		Vector2 scrollPositionLower = new Vector2(0, 0);

		public  ComponentManifest(): base("Component Manifest")
		{
			minSize = new Vector2(400f, 300f);
		}
		
		public class Palette : StylePalette
		{
			public static Color selectedComponent()
			{
				return yellow;
			}

			public static Color sceneFile()
			{
				//return red;
				return yellow;
			}

			public static Color prefabFile()
			{
				//return blue;
				return cyan;
			}

			public static Color gameObject()
			{
				return green;
			}
		}

		enum ComponetTypeFilter
		{
			None,
			HasOnRenderImage,
			HasOnGUI
		}

		ScanScope currentScanScope = ScanScope.Everything;


		[System.Serializable]
		public class ComponentDatabase
		{
			public class ComponentRef
			{
				public HashSet<string> gameObjectNodePaths;
			}

			public class ComponentInfo
			{
				public string componentName;
				public TextAsset asset;
				public Dictionary<string, ComponentRef> sceneRefs;
				public Dictionary<string, ComponentRef> prefabRefs;
				public bool isImageEffect;
				public bool hasOnGUI;
			}

			Dictionary<string, ComponentInfo> db = new Dictionary<string, ComponentInfo>();

			[System.Serializable]
			public class PackedComponentRef
			{
				public string entityName; // Scene or Prefab
				public List<string> gameObjectNodePaths;
			}

			[System.Serializable]
			public class PackedComponentInfo
			{
				public string componentName;
				public TextAsset asset;
				public List<PackedComponentRef> sceneRefs;
				public List<PackedComponentRef> prefabRefs;
				public int componentRefCount;
				public bool isImageEffect;
				public bool hasOnGUI;
			}

			[System.Serializable]
			public class PackedComponentDictionary
			{
				public List<PackedComponentInfo> pdb;
			}

			//[SerializeField]
			public PackedComponentDictionary packedDictionary = null;

			public void Pack()
			{
				if (db == null)
					return;
				packedDictionary = new PackedComponentDictionary();
				packedDictionary.pdb = new List<PackedComponentInfo>();
				foreach (var ci in db.Values.OrderBy(civ => civ.componentName))
				{
					PackedComponentInfo pci = new PackedComponentInfo();
					pci.componentName = ci.componentName;
					pci.isImageEffect = ci.isImageEffect;
					pci.hasOnGUI = ci.hasOnGUI;
					pci.asset = ci.asset;
					pci.componentRefCount = 0;
					pci.sceneRefs = new List<PackedComponentRef>();
					foreach (var cr in ci.sceneRefs)
					{
						PackedComponentRef pcr = new PackedComponentRef();
						pcr.entityName = cr.Key;
						pcr.gameObjectNodePaths = new List<string>(cr.Value.gameObjectNodePaths);
						pci.sceneRefs.Add(pcr);
						pci.componentRefCount += pcr.gameObjectNodePaths.Count;
					}

					pci.prefabRefs = new List<PackedComponentRef>();
					foreach (var cr in ci.prefabRefs)
					{
						PackedComponentRef pcr = new PackedComponentRef();
						pcr.entityName = cr.Key;
						pcr.gameObjectNodePaths = new List<string>(cr.Value.gameObjectNodePaths);
						pci.prefabRefs.Add(pcr);
						pci.componentRefCount += pcr.gameObjectNodePaths.Count;
					}

					packedDictionary.pdb.Add(pci);
				}
			}

			void UnPack()
			{
				// Don't bother? Just keep the packed data
			}

			public bool SetAssetForComponent(string componentName, TextAsset loadedAsset)
			{
				if (!db.ContainsKey(componentName))
					return false;

				ComponentInfo ci = db[componentName];

				ci.asset = loadedAsset;

				return true;
			}

			bool CheckImageEffect(Component blob)
			{
				System.Reflection.BindingFlags methFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
				var renderMeth = blob.GetType().GetMethod("OnRenderImage", methFlags);
				return (renderMeth != null);
			}

			bool CheckOnGUI(Component blob)
			{
				System.Reflection.BindingFlags methFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
				var ogMeth = blob.GetType().GetMethod("OnGUI", methFlags);
				return (ogMeth != null);
			}

			public void AddComponent(Component blob, string entityPath, bool isPrefab)
			{
				string componentName = blob.GetType().Name;
				string componentFullName = blob.GetType().FullName;

				if (componentName == "Transform") // Skip this one!
					return;

				ComponentInfo ci = null;
				if (db.ContainsKey(componentName))
				{
					ci = db[componentName];
				}
				else
				{
					ci = new ComponentInfo();

					ci.componentName = componentName;

					ci.isImageEffect = CheckImageEffect(blob);

					ci.hasOnGUI = CheckOnGUI(blob);

//				if ( ci.isImageEffect )
//					Debug.Log( componentName + " is an image effect " );
//				if ( ci.hasOnGUI )
//					Debug.Log( componentName + " has an OnGUI() method " );

					ci.sceneRefs = new Dictionary<string, ComponentRef>();
					ci.prefabRefs = new Dictionary<string, ComponentRef>();

					db[componentName] = ci;
				}

				ComponentRef cref = null;
				if (isPrefab)
				{
					if (ci.prefabRefs.ContainsKey(entityPath))
					{
						cref = ci.prefabRefs[entityPath];
					}
					else
					{
						cref = new ComponentRef();
						cref.gameObjectNodePaths = new HashSet<string>();
						ci.prefabRefs[entityPath] = cref;
					}
				}
				else
				{
					if (ci.sceneRefs.ContainsKey(entityPath))
					{
						cref = ci.sceneRefs[entityPath];
					}
					else
					{
						cref = new ComponentRef();
						cref.gameObjectNodePaths = new HashSet<string>();
						ci.sceneRefs[entityPath] = cref;
					}
				}

				cref.gameObjectNodePaths.Add(getNodePath(blob.gameObject));
			}

			string getNodePath(GameObject go)
			{
				List<string> breadcrumbs = new List<string>();

				while (go)
				{
					breadcrumbs.Insert(0, go.name);
					go = go.transform.parent?.gameObject;
				}

				return string.Join("/", breadcrumbs);
			}
		}


		[SerializeField]
		ComponentDatabase componentDatabase = null;

		HashSet<string> scenes = null;
		HashSet<string> prefabs = null;

		public static IEnumerable<GameObject> SceneRoots()
		{
			var prop = new HierarchyProperty(HierarchyType.GameObjects);
			var expanded = new int[0];
			while (prop.Next(expanded))
			{
				yield return prop.pptrValue as GameObject;
			}
		}

		ComponentDatabase.PackedComponentInfo selectedComponentEntry = null;

		HashSet<object> ComponentRefFoldoutState = null;

		public enum ColumnTag : int
		{
			Component = 0,
			SceneCount,
			PrefabCount,
			InstanceCount,
			ColummTagMax_
		}

		SortColumnButton.ColumnState[] sortCols = new SortColumnButton.ColumnState[(int)ColumnTag.ColummTagMax_];

		void ComponentSummary(float svUpperPanel)
		{
			// Name, Num referencing scenes & prefabs, number of references
			if (componentDatabase == null)
				return;
			if (componentDatabase.packedDictionary == null)
				return;

			Color saved = GUI.color;
			Color savedBK = GUI.backgroundColor;

			GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
			headerStyle.normal.background = Texture2D.whiteTexture;
			headerStyle.normal.textColor = Palette.black;

			// Lazy Init
			for (int sc = 0; sc < sortCols.Length; ++sc)
			{
				if (sortCols[sc] == null)
					sortCols[sc] = new SortColumnButton.ColumnState();
			}

			bool sortNeeded = true; //false;

			using (new EditorGUILayout.HorizontalScope())
			{
				sortNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.Component], sortCols, "Component", GUILayout.ExpandWidth(true)) | sortNeeded;
				sortNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.SceneCount], sortCols, "Sc#", GUILayout.Width(50f)) | sortNeeded;
				sortNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.PrefabCount], sortCols, "Pf#", GUILayout.Width(50f)) | sortNeeded;
				sortNeeded = SortColumnButton.Button(sortCols[(int)ColumnTag.InstanceCount], sortCols, "All", GUILayout.Width(50f)) | sortNeeded;

				GUILayout.Label("", headerStyle, GUILayout.Width(10f));
			}

			using (var ScrollView = new EditorGUILayout.ScrollViewScope(scrollPositionUpper, false, true, GUILayout.Height(svUpperPanel)))
			{
				scrollPositionUpper = ScrollView.scrollPosition;

				Regex filterRegex = null;
				try
				{
					filterRegex = new Regex(filterString, RegexOptions.IgnoreCase);
				}
				catch { }

				ColumnTag active = ColumnTag.Component;
				for (int t = 0; t < sortCols.Length; ++t)
				{
					if (sortCols[t].active)
					{
						active = (ColumnTag)t;
						break;
					}
				}

				bool sortUp = sortCols[(int)active].sortUp;


				List<ComponentDatabase.PackedComponentInfo> unfilteredComponentList = componentDatabase.packedDictionary.pdb;
				switch (typeFilter)
				{
					case ComponetTypeFilter.None:
						break;
					case ComponetTypeFilter.HasOnRenderImage:
						unfilteredComponentList = unfilteredComponentList.Where<ComponentDatabase.PackedComponentInfo>(
							a => (a.isImageEffect == true)
						).ToList();
						break;
					case ComponetTypeFilter.HasOnGUI:
						unfilteredComponentList = unfilteredComponentList.Where<ComponentDatabase.PackedComponentInfo>(
							a => (a.hasOnGUI == true)
						).ToList();
						break;
					default:
						break;
				}

				List<ComponentDatabase.PackedComponentInfo> filteredComponentList = unfilteredComponentList.Where<ComponentDatabase.PackedComponentInfo>(
					a => filterString == "" ? true : filterRegex != null ? filterRegex.IsMatch(a.componentName) : true
				).ToList();
				if (sortNeeded)
					filteredComponentList.Sort(
						(r1, r2) =>
						{
							int result = 0;
							switch (active)
							{
								case ColumnTag.Component:
									result = r1.componentName.CompareTo(r2.componentName);
									break;
								case ColumnTag.SceneCount:
									result = r1.sceneRefs.Count.CompareTo(r2.sceneRefs.Count);
									break;
								case ColumnTag.PrefabCount:
									result = r1.prefabRefs.Count.CompareTo(r2.prefabRefs.Count);
									break;
								case ColumnTag.InstanceCount:
									result = r1.componentRefCount.CompareTo(r2.componentRefCount);
									break;
							}

							return sortUp ? result : -result;
						}
					);

				foreach (var pci in filteredComponentList)
				{
					int numSceneRefs = pci.sceneRefs.Count;
					int numPrefabRefs = pci.prefabRefs.Count;
					int componentRefCount = pci.componentRefCount;

					bool clicked = false;

					using (new EditorGUILayout.HorizontalScope())
					{
						if (pci.asset == null)
						{
							GUI.color = Palette.cyan; // Visually showing components we have no source for, either built-in ot somewhere in DLLs
						}
						else
						{
							GUI.color = saved;

							string path = AssetDatabase.GetAssetPath( pci.asset );
							if (path.StartsWith("Packages/"))
								GUI.color = Palette.orange;
						}

						if (pci == selectedComponentEntry)
							GUI.color = Palette.selectedComponent();

						clicked = GUILayout.Button(pci.componentName, GUI.skin.label, GUILayout.ExpandWidth(true)) || clicked;
						clicked = GUILayout.Button(numSceneRefs.ToString(), GUI.skin.label, GUILayout.Width(40f)) || clicked;
						clicked = GUILayout.Button(numPrefabRefs.ToString(), GUI.skin.label, GUILayout.Width(40f)) || clicked;
						clicked = GUILayout.Button(componentRefCount.ToString(), GUI.skin.label, GUILayout.Width(40f)) || clicked;

						if (pci == selectedComponentEntry)
						{
							GUI.color = saved;
							GUI.backgroundColor = savedBK;
						}

						if (clicked)
						{
							//Debug.LogError( "SELECTED => " + pci.componentName );
							selectedComponentEntry = pci;

							if (pci.asset)
							{
								Selection.activeObject = pci.asset;
							}
						}
					}
				}
			}

			GUI.backgroundColor = savedBK;
			GUI.color = saved;
		}

		void ComponentDetails()
		{
			if (selectedComponentEntry == null)
				return;

			if (ComponentRefFoldoutState == null)
			{
				ComponentRefFoldoutState = new HashSet<object>();
			}

			var pci = selectedComponentEntry;

			if (pci.asset!=null)
			{
				var ap = AssetDatabase.GetAssetPath( pci.asset );
				GUILayout.Label( $"[{ap}]" );			// todo: would be cool to make this clickable to focus the script
			}
			else
			{
				GUILayout.Label( $"[Internal]" );
			}

			if (pci.isImageEffect) GUILayout.Label("Component is an ImageEffect");
			if (pci.hasOnGUI) GUILayout.Label("Component implements OnGUI");

			foreach (var pcr in pci.sceneRefs)
			{
				bool unfolded = ComponentRefFoldoutState.Contains(pcr);

				GUI.color = Palette.sceneFile();

				//unfolded = EditorGUILayout.Foldout( unfolded, pcr.entityName + " (" + pcr.gameObjectNodePaths.Count + ")");
				bool clicked;
				ClickableFoldout.Foldout(ref unfolded, out clicked, (pcr.entityName + " (" + pcr.gameObjectNodePaths.Count + ")"));

				if (unfolded)
				{
					GUI.color = Palette.gameObject();
					foreach (string gop in pcr.gameObjectNodePaths)
					{
						GUILayout.Label("     " + gop);
					}
				}

				if (unfolded)
					ComponentRefFoldoutState.Add(pcr);
				else
					ComponentRefFoldoutState.Remove(pcr);

				if (clicked)
				{
					//Debug.LogError( pcr.entityName + " CLICKED!" );
					EditorUtility.FocusProjectWindow();
					Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(pcr.entityName);
				}
			}

			foreach (var pcr in pci.prefabRefs)
			{
				bool unfolded = ComponentRefFoldoutState.Contains(pcr);

				GUI.color = Palette.prefabFile();

				//unfolded = EditorGUILayout.Foldout( unfolded, pcr.entityName + " (" + pcr.gameObjectNodePaths.Count + ")" );
				bool clicked;
				ClickableFoldout.Foldout(ref unfolded, out clicked, (pcr.entityName + " (" + pcr.gameObjectNodePaths.Count + ")"));

				if (unfolded)
				{
					GUI.color = Palette.gameObject();
					foreach (string gop in pcr.gameObjectNodePaths)
					{
						GUILayout.Label("     " + gop);
					}
				}

				if (unfolded)
					ComponentRefFoldoutState.Add(pcr);
				else
					ComponentRefFoldoutState.Remove(pcr);

				if (clicked)
				{
					//Debug.LogError( pcr.entityName + " CLICKED!" );
					EditorUtility.FocusProjectWindow();
					Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(pcr.entityName);
				}
			}
		}

		void GatherComponentsInPrefab(string prefabPath)
		{
			if (componentDatabase == null)
			{
				componentDatabase = new ComponentDatabase();
			}

			/*UnityEngine.SceneManagement.Scene createdScene =*/
			EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

			GameObject codeInstantiatedPrefab = GameObject.Instantiate(
				AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject))) as GameObject;

			Component[] allComponents = codeInstantiatedPrefab.GetComponentsInChildren<Component>(true);
			foreach (var blob in allComponents)
			{
				if (blob == null)
					continue;

				componentDatabase.AddComponent(blob, prefabPath, true);
			}

			//EditorSceneManager.CloseScene( createdScene, true );
		}

		void GatherComponentsInScene(string scenePath)
		{
			if (componentDatabase == null)
			{
				componentDatabase = new ComponentDatabase();
			}

			//Debug.Log( "========= LOADING " + scenePath );

			UnityEngine.SceneManagement.Scene loadedScene;
			try
			{
				loadedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			}
			catch
			{
				//Debug.LogWarning( "============>  KABOOM! " );
				return;
			}

			GameObject[] sceneRoots = loadedScene.GetRootGameObjects();

			try
			{
				foreach (var root in sceneRoots)
				{
					Component[] allComponents = root.GetComponentsInChildren<Component>(true);
					foreach (var blob in allComponents)
					{
						if (blob == null)
							continue;

						componentDatabase.AddComponent(blob, scenePath, false);
					}
				}
			}
			catch
			{
				//Debug.LogWarning( "============>  OOPS! " );
				return;
			}
		}

		void GatherScenesAndPrefabs( ScanScope scope )
		{
			var currentScenePath = EditorSceneManager.GetActiveScene().path;

			if (EditorSceneManager.GetActiveScene().isDirty)
			{
				if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				{
					currentScenePath = "";
				}
			}

			EditorUtility.DisplayProgressBar("Gathering Scenes..", "", 0);

			// Scan
			scenes = new HashSet<string>();
			prefabs = new HashSet<string>();

			string[] allAssets = AssetDatabase.GetAllAssetPaths();

			if (( scope == ScanScope.Everything) || (scope == ScanScope.EverythingIncludingPackages ) )
			{
				var guids = AssetDatabase.FindAssets("t:SceneAsset", null);
				foreach (var guid in guids)
				{
					scenes.Add(AssetDatabase.GUIDToAssetPath(guid));
				}

				foreach (string s in allAssets)
				{
					if (s.EndsWith(".prefab"))
					{
						prefabs.Add(s);
					}
				}
			}
			else
			{
				scenes.UnionWith(
					( scope == ScanScope.ScenesEnabledInBuildListOnly
						? EditorBuildSettings.scenes.Where(a => a.enabled).Select((b) => b.path).ToArray()
						: // Only Enabled
						EditorBuildSettings.scenes.Select((b) => b.path).ToArray() 
					).Where( c => c!=null && c!="" )
				);
			}			

			// Filter for Packages
			{
				int beforeCount = scenes.Count;

				if ( scope != ScanScope.EverythingIncludingPackages )
				{
					scenes = new HashSet<string>( scenes.Where( scp =>
						ProjectHelpers.SceneIsInPackage( scp ) == false
					) );
				}
				else
				{
					scenes = new HashSet<string>( scenes.Where( scp =>
						ProjectHelpers.SceneCanBeLoaded( scp ) == true
					) );
				}

				Debug.Log( $"Package Filtering removed {beforeCount-scenes.Count} scenes from consideration" );
			}

			{
				int i = 0;
				foreach (var scp in scenes)
				{
					EditorUtility.DisplayProgressBar("Examining Scene:", scp, ((float)i) / ((float)scenes.Count));
					GatherComponentsInScene(scp);
					++i;
				}
			}

			{
				int i = 0;
				foreach (var pf in prefabs)
				{
					EditorUtility.DisplayProgressBar("Examining Prefab:", pf, ((float)i) / ((float)prefabs.Count));
					GatherComponentsInPrefab(pf);
					++i;
				}
			}

			// Identify non-dll scripts (hopefully
			foreach (string s in allAssets)
			{
				if (s.EndsWith(".cs"))
				{
					// Load script and check for MonoBehaviour
					TextAsset loadedAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(s);
					if (loadedAsset == null)
					{
						Debug.LogError("Why U no load? " + s);
						continue;
					}

					//if ( -1 == loadedAsset.text.IndexOf( "MonoBehaviour" ) )
					//	continue;

					string scriptname = System.IO.Path.GetFileNameWithoutExtension(s);

					//Debug.Log( "loaded script: "+scriptname+" ("+s+")" );

					componentDatabase.SetAssetForComponent(scriptname, loadedAsset);
				}
			}



			EditorUtility.ClearProgressBar();

			// Pack up!
			componentDatabase.Pack();

			// Restore
			if (currentScenePath != "")
			{
				/*var loadedScene =*/
				EditorSceneManager.OpenScene(currentScenePath); // Reopen scene we started with
				Debug.Log("GatherScenesAndPrefabs PREVIOUS SCENE RESTORED");
			}
			else
			{
				/*var baseScene =*/
				EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
				Debug.Log("GatherScenesAndPrefabs NEW SCENE OPENED");
			}

		}

		string filterString = "";
		ComponetTypeFilter typeFilter = ComponetTypeFilter.None;

		void ShowDialog()
		{
			bool alreadyScanned = !((componentDatabase == null) || (componentDatabase.packedDictionary == null));
			if ( !alreadyScanned )
				EditorGUILayout.HelpBox("Compile a List of all Monobehaviours used in your project", MessageType.Info, true );

			using (new EditorGUILayout.HorizontalScope())
			{
				currentScanScope = (ScanScope)EditorGUILayout.EnumPopup("Scope:", currentScanScope, GUILayout.ExpandWidth(true) );

				string buttonText = alreadyScanned ? "Rescan" : "Scan";

				if (GUILayout.Button(buttonText))
				{
					componentDatabase = null;
					GatherScenesAndPrefabs(currentScanScope);
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Filter(Regex)", GUILayout.Width(80f));
				filterString = EditorGUILayout.TextField(filterString, GUILayout.ExpandWidth(true));

				bool radioAll = GUILayout.Toggle(typeFilter == ComponetTypeFilter.None, "All");
				bool radioImgFx = GUILayout.Toggle(typeFilter == ComponetTypeFilter.HasOnRenderImage, "ImgFx");
				bool radioGUI = GUILayout.Toggle(typeFilter == ComponetTypeFilter.HasOnGUI, "GUI");
				if (radioAll && typeFilter != ComponetTypeFilter.None)
				{
					typeFilter = ComponetTypeFilter.None;
				}
				else if (radioImgFx && typeFilter != ComponetTypeFilter.HasOnRenderImage)
				{
					typeFilter = ComponetTypeFilter.HasOnRenderImage;
				}
				else if (radioGUI && typeFilter != ComponetTypeFilter.HasOnGUI)
				{
					typeFilter = ComponetTypeFilter.HasOnGUI;
				}
			}

			Rect window = this.position;
			float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			window.height += itemHeight * 2f;

			float svUpperPanel = Mathf.Floor((window.height / 2f) / itemHeight) * itemHeight;

//		float svLowerPanel = window.height - svUpperPanel;

			ComponentSummary(svUpperPanel); // todo: selection

			//	GUILayout.Box( "", GUILayout.ExpandWidth(true) );
			EditorGUILayout.Separator();

			// Lower Panel
			using (var ScrollView = new EditorGUILayout.ScrollViewScope(scrollPositionLower, false, true))
			{
				scrollPositionLower = ScrollView.scrollPosition;

				// details
				ComponentDetails();
			}
		}

		protected override Color BackgroundColour()
		{
			return DefaultBackgroundColor * 1.5f;
		}
		
		protected override void DrawGui()
		{
			ShowDialog();
		}

		[MenuItem("ElfDev Asset Insights/Component Manifest")]
		static void OpenWindow()
		{
			ShowWindow(true);
		}
	}

}
