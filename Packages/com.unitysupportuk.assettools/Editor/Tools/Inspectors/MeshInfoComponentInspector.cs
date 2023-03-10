using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace ElfDev
{
    [CustomEditor(typeof(MeshInfoComponent))]
    public class MeshInfoComponentInspector : Editor
    {
		MeshInspectorShared mis = new MeshInspectorShared();

		public void InspectMeshFilter( MeshFilter mf )
		{
			Mesh m = mf.sharedMesh;
			if (m == null) return;

			mis.MeshInspectorGUI(m, false);
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			MeshInfoComponent mic = target as MeshInfoComponent;

            GameObject go = mic.gameObject;

            MeshFilter mf = go.GetComponent<MeshFilter>();
            if ( mf != null )
            {
        		InspectMeshFilter( mf );
            }
            else
            {
                // No Idea...
                GUILayout.Label( "No Mesh Located" );
            }
		}
    }
}

