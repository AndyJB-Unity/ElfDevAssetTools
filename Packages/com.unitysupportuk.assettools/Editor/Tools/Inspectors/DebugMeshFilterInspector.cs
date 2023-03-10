using UnityEngine;
using UnityEditor;

namespace ElfDev
{
	// Show helpful Mesh info
	[CustomEditor(typeof(MeshFilter))]
	public class DebugMeshFilterInspector : UnityEditor.Editor
	{
		MeshInspectorShared mis = new MeshInspectorShared();

		public void InspectMeshFilter( MeshFilter mf )
		{
			Mesh m = mf.sharedMesh;
			if (m == null) return;

			mis.MeshInspectorGUI(m, true);
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			MeshFilter mf = target as MeshFilter;

			InspectMeshFilter( mf );
		}
	}

}
