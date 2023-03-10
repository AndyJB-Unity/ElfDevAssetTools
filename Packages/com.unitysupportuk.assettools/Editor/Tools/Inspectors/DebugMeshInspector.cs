using UnityEngine;
using UnityEditor;

namespace ElfDev
{
	[CustomEditor(typeof(Mesh))]
	public class DebugMeshInspector : UnityEditor.Editor
	{
		MeshInspectorShared mis = new MeshInspectorShared();

		bool dumpBoneWeights = false;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			Mesh m = target as Mesh;
			if (m == null) return;

			GUI.enabled = true;

			mis.MeshInspectorGUI(m, false);

			dumpBoneWeights = GUILayout.Toggle( dumpBoneWeights, "Dump Bone Weights" );
			
			mis.MeshInspectorGUI_SkinningInfo(m, false);

			if ( GUILayout.Button( "Dump Info to file" ) )
			{
				mis.DebugDumpSkinningInfo( m, null, dumpBoneWeights );
			}
		}
	}
}
