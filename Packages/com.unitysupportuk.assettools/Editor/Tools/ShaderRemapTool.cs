using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ElfDev
{
	public class ShaderRemapTool : ElfDevEditorWindow<ShaderRemapTool>
	{
		static bool manuallyCheckOut = true;

		static bool showProgressBar = true;

		Vector2 scrollPosition = new Vector2(0, 0);

		public  ShaderRemapTool(): base("Shader Remap Tool")
		{
			minSize = new Vector2(400f, 300f);
		}
		
		// Scans all Materials in project for specified shader
		// and remaps to another shader (optionally)
		[MenuItem("ElfDev Asset Insights/Shader Remap Tool")]
		static void OpenWindow()
		{
			ShowWindow(true);
		}

		string[] FindAllMaterialInProject()
		{
			// Array of GUIDs
			return AssetDatabase.FindAssets("t:Material");
		}

		Material[] FindAllMaterialInProject(Shader withShader)
		{
			if (withShader == null)
				return null;

			if (showProgressBar)
				EditorUtility.DisplayProgressBar("Scanning.. ", "Checking Materials ", 0f);

			List<Material> acceptedMats = new List<Material>();

			// Get possible matches
			string[] allMaterialGUIDS = FindAllMaterialInProject();

			totalProjectMaterials = allMaterialGUIDS.Length;

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
					if (candidateMaterial.shader == withShader)
					{
						// Match!
						acceptedMats.Add(candidateMaterial);
					}
					else
					{
						// No Match!
						// ( Don't destroy! )
					}
				}
			}

			if (showProgressBar)
				EditorUtility.ClearProgressBar();

			return acceptedMats.ToArray();
		}

		int totalProjectMaterials = 0;
		Material[] filteredMaterials = null;
		Shader shaderToReplace = null;
		Shader shaderReplacement = null;

		Dictionary<int, bool> shaderReplaceToggles = null;

		void ShowDialog()
		{
			EditorGUILayout.HelpBox("Replace all instances in project of one shader with another", MessageType.Info, true );
			
			if (shaderReplaceToggles == null)
			{
				shaderReplaceToggles = new Dictionary<int, bool>();
			}

			GUILayout.Label("Remap Material Shaders");

			GUILayout.Label("Locate all materials that use a shader and optionally change the shader they use");

			shaderToReplace = EditorGUILayout.ObjectField("Shader to remap", shaderToReplace, typeof(Shader), true) as Shader;

			bool doScan = false;
			if (shaderToReplace != null)
			{
				doScan = GUILayout.Button("Scan for shader " + shaderToReplace.name);
			}
			else
			{
				GUI.enabled = false;
				GUILayout.Button("Scan for shader");
				GUI.enabled = true;
			}

			if (doScan)
			{
				// get all materials with this shader
				filteredMaterials = FindAllMaterialInProject(withShader: shaderToReplace);
			}

			if ((filteredMaterials != null) && (filteredMaterials.Length > 0))
			{
				scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

				int totalMaterialCount = filteredMaterials.Length;
				int remappingMaterialCount = 0;

				foreach (var m in filteredMaterials)
				{
					GUILayout.BeginHorizontal();

					if (!shaderReplaceToggles.ContainsKey(m.GetInstanceID()))
						shaderReplaceToggles[m.GetInstanceID()] = true;

					shaderReplaceToggles[m.GetInstanceID()] = GUILayout.Toggle(shaderReplaceToggles[m.GetInstanceID()], "");

					remappingMaterialCount += (shaderReplaceToggles[m.GetInstanceID()] != false) ? 1 : 0;

					//GUILayout.Label( m.name );
					if (GUILayout.Button(m.name))
					{
						Selection.objects = new Object[1] { m };
					}

					GUILayout.EndHorizontal();
				}

				GUILayout.EndScrollView();

				shaderReplacement = EditorGUILayout.ObjectField("Remap to ", shaderReplacement, typeof(Shader), true) as Shader;
				if (shaderReplacement == null)
					GUI.enabled = false;
				if (GUILayout.Button("Do Remap (" + remappingMaterialCount + " of " + totalMaterialCount + " materials ) [ " + totalProjectMaterials + " total Materials ]"))
				{
					if (showProgressBar)
						EditorUtility.DisplayProgressBar("Replacing.. ", "Updating Materials ", (0.0f / 3.0f));

					foreach (var m in filteredMaterials)
					{
						m.shader = shaderReplacement;
						EditorUtility.SetDirty(m);
					}

					if (showProgressBar)
						EditorUtility.DisplayProgressBar("Replacing.. ", "Checking Out Materials ", (1.0f / 3.0f));

					if (manuallyCheckOut)
						UnityEditor.VersionControl.Provider.Checkout(filteredMaterials, UnityEditor.VersionControl.CheckoutMode.Both).Wait();

					if (showProgressBar)
						EditorUtility.DisplayProgressBar("Replacing.. ", "Flushing Material Assets to Disk ", (2.0f / 3.0f));

					AssetDatabase.SaveAssets();

					filteredMaterials = null; // Require a rescan

					if (showProgressBar)
						EditorUtility.ClearProgressBar();
				}

				GUI.enabled = true;

			}

			GUILayout.Label(""); // Spacer
		}

		protected override void DrawGui()
		{
			ShowDialog();
		}
	}

// If the ObjectField is part of a custom Editor for a script component, use EditorUtility.IsPersistent() to check if the component is on an asset or a scene object.
// 		AssetDatabase.GUIDToAssetPath(string guid);
// 		AssetDatabase.LoadAssetAtPath();

//		var dependencies = EditorUtility.CollectDependencies( new UnityEngine.Object[] { withShader } );
// returns things this asset depends on, NOT things that depend on this asset! grr!

}
