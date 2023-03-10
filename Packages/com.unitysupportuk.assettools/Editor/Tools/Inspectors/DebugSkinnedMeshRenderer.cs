using UnityEngine;
using UnityEditor;

namespace ElfDev
{
	// Show helpful Mesh info
	[CustomEditor(typeof(SkinnedMeshRenderer))]
	public class DebugSkinnedMeshRenderer : UnityEditor.Editor
	{
		MeshInspectorShared mis = new MeshInspectorShared();

		bool dumpBoneWeights = false;
		bool showBones = false;

		public void InspectSkinnedMeshRender( SkinnedMeshRenderer smr )
		{
			Mesh m = smr.sharedMesh;
			if (m == null) return;

			mis.MeshInspectorGUI(m, true);
			mis.MeshInspectorGUI_SkinningInfo(m, false);

			if (smr.bones.Length > 0)
			{
				showBones = EditorGUILayout.Foldout(showBones, $"Bones ({smr.bones.Length })");
				if (showBones)
				{
					for (int bs = 0; bs < smr.bones.Length; ++bs)
						GUILayout.Label(bs + ": " + smr.bones[bs].name );
				}
			}
			
			dumpBoneWeights = GUILayout.Toggle( dumpBoneWeights, "Dump Bone Weights" );
			
			if (GUILayout.Button("Dump Info to file"))
			{
				mis.DebugDumpSkinningInfo(m, smr, dumpBoneWeights);
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			SkinnedMeshRenderer smr = target as SkinnedMeshRenderer;
			InspectSkinnedMeshRender( smr );
		}
	}
}

