using UnityEngine;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using UnityEditor;

// todo: add facilities to make this show/hidable
// todo: add gizmos to show info in scene ( normals etc.. ) or do some shader replacment stuff
// todo: check what facilities are available...

namespace ElfDev
{
	public class MeshInspectorShared
	{
		class PerObjectInfo
		{
			public int[] indices = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
		}

		static Dictionary<int, PerObjectInfo> editTempInfo = new Dictionary<int, PerObjectInfo>();

		private void swap(ref int a, ref int b)
		{
			int t = a;
			a = b;
			b = t;
		}

		public void DumpMeshInfo(Mesh m, StringBuilder sb)
		{
			sb.AppendLine( $"Name: {m.name}" );

			sb.AppendLine( string.Format("Triangles : {0} ({1} indices)", m.triangles.Length / 3, m.triangles.Length) );
			sb.AppendLine(string.Format("Submeshes : {0}", m.subMeshCount));

			for (int sub = 0; sub < m.subMeshCount; ++sub)
			{
				sb.AppendLine(string.Format("  {0}: Start:{1} Count:{2} ({3} tris)", sub, m.GetIndexStart(sub), m.GetIndexCount(sub), m.GetIndexCount(sub) / 3));
			}

			sb.AppendLine(string.Format("Vertices : {0}", m.vertexCount));

			sb.AppendLine(string.Format("Colours : {0}", m.colors != null ? (m.colors.Length > 0) : false));
			sb.AppendLine(string.Format("Normals : {0}", m.normals != null ? (m.normals.Length > 0) : false));
			sb.AppendLine(string.Format("Tangents : {0}", m.tangents != null ? (m.tangents.Length > 0) : false));

			bool activeChannel1 = (m.uv != null ? (m.uv.Length > 0) : false); // uv1
			bool activeChannel2 = (m.uv2 != null ? (m.uv2.Length > 0) : false);
			bool activeChannel3 = (m.uv3 != null ? (m.uv3.Length > 0) : false);
			bool activeChannel4 = (m.uv4 != null ? (m.uv4.Length > 0) : false);
			bool activeChannel5 = (m.uv5 != null ? (m.uv5.Length > 0) : false);
			bool activeChannel6 = (m.uv6 != null ? (m.uv6.Length > 0) : false);
			bool activeChannel7 = (m.uv7 != null ? (m.uv7.Length > 0) : false);
			bool activeChannel8 = (m.uv8 != null ? (m.uv8.Length > 0) : false);

			sb.AppendLine($"uv: {activeChannel1}");
			sb.AppendLine($"uv2: {activeChannel2}");
			sb.AppendLine($"uv3: {activeChannel3}");
			sb.AppendLine($"uv4: {activeChannel4}");
			sb.AppendLine($"uv5: {activeChannel5}");
			sb.AppendLine($"uv6: {activeChannel6}");
			sb.AppendLine($"uv7: {activeChannel7}");
			sb.AppendLine($"uv8: {activeChannel8}");
		}
		
		public void DebugDumpSkinningInfo(Mesh m, SkinnedMeshRenderer smr, bool showVertexData )
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			DumpMeshInfo(m, sb);
			
			sb.AppendLine("Bind Pose Count: " + m.bindposes.Length);
			sb.AppendLine("Bone Weight Count: " + m.boneWeights.Length);
			
			int[] boneBoundVerts = null;

			if (smr != null)
			{
				sb.AppendLine("Bone Count: " + smr.bones.Length);

				Transform[] allNodes = smr.rootBone.transform.GetComponentsInChildren<Transform>();
				sb.AppendLine("Driver Transform Count: " + allNodes.Length);
				
//				allNodes = allNodes.Where(a => a.name.StartsWith("chunk_")).ToArray();
//				sb.AppendLine("Chunk Driver Transform Count: " + allNodes.Length);
				
				boneBoundVerts = new int[smr.bones.Length];
			}

			for (int i = 0; i < m.boneWeights.Length; ++i)
			{
				BoneWeight bw = m.boneWeights[i];
				if (showVertexData)
				{
					if ((bw.weight0 != 1) && (bw.weight1 != 0) && (bw.weight2 != 0) && (bw.weight3 != 0))
					{
						//sb.AppendLine("  Vertex " + i + " is not single bone skinned!");
						sb.Append("  ");
						sb.Append(i.ToString() + " > ");
						sb.Append(bw.boneIndex0 + ":" + bw.weight0 + " ");
						sb.Append(bw.boneIndex1 + ":" + bw.weight1 + " ");
						sb.Append(bw.boneIndex2 + ":" + bw.weight2 + " ");
						sb.Append(bw.boneIndex3 + ":" + bw.weight3);
						sb.Append("\n");
					}
					else
					{
						sb.AppendLine("Vertex " + i + " is single bone skinned to bone " + bw.boneIndex0 );
					}
				}
				
				if ( (boneBoundVerts != null) && (bw.weight0 != 0) && (bw.boneIndex0 >= 0))
					boneBoundVerts[bw.boneIndex0]++;
				if ( (boneBoundVerts != null) && (bw.weight1 != 0) && (bw.boneIndex1 >= 0))
					boneBoundVerts[bw.boneIndex0]++;
				if ( (boneBoundVerts != null) && (bw.weight2 != 0) && (bw.boneIndex2 >= 0))
					boneBoundVerts[bw.boneIndex0]++;
				if ( (boneBoundVerts != null) && (bw.weight3 != 0) && (bw.boneIndex3 >= 0))
					boneBoundVerts[bw.boneIndex0]++;
			}
			
			sb.AppendLine();

			if (boneBoundVerts != null)
			{
				for (int bi = 0; bi < boneBoundVerts.Length; ++bi)
				{
					if (boneBoundVerts[bi] == 0)
					{
						sb.AppendLine("  Bone " + bi + ": '" + smr.bones[bi].name + "' IS NOT BOUND TO ANY VERTS!");
					}
					else
					{
						sb.AppendLine("  Bone " + bi + ": '" + smr.bones[bi].name + "' bound to " + boneBoundVerts[bi] + " verts");
					}
				}
			}
			else
			{
//				sb.AppendLine("Bind Pose Count: " + m.bindposes.Length);
			}

			sb.AppendLine();

			System.IO.File.WriteAllText("Assets/MeshInfo_" + m.name + ".txt", sb.ToString());
		}

		public Vector2 scrollPosition = Vector2.zero;

		bool showShapes = false;

		public void MeshInspectorGUI_SkinningInfo(Mesh m, bool detailed)
		{
			if (m.blendShapeCount > 0)
			{
				showShapes = EditorGUILayout.Foldout(showShapes, $"Blend Shapes ({m.blendShapeCount})");
				if (showShapes)
				{
					for (int bs = 0; bs < m.blendShapeCount; ++bs)
						GUILayout.Label(bs + ": " + m.GetBlendShapeName(bs));
				}
			}

			GUILayout.Label("Bind Pose Count: " + m.bindposes.Length);
			GUILayout.Label("Bone Weight Count: " + m.boneWeights.Length);
			
			if (detailed)
			{
				if (m.boneWeights != null)
				{
					using (var scrollScope = new GUILayout.ScrollViewScope(scrollPosition, "box"))
					{
						scrollPosition = scrollScope.scrollPosition;

						for (int ibw = 0; ibw < m.boneWeights.Length; ++ibw)
						{
							BoneWeight bw = m.boneWeights[ibw];

							using (var horizontalScope = new GUILayout.HorizontalScope("box"))
							{
								GUILayout.Label(ibw.ToString());
								GUILayout.Label(bw.boneIndex0 + ":" + bw.weight0);
								GUILayout.Label(bw.boneIndex1 + ":" + bw.weight1);
								GUILayout.Label(bw.boneIndex2 + ":" + bw.weight2);
								GUILayout.Label(bw.boneIndex3 + ":" + bw.weight3);
							}
						}
					}
				}

				//	public BoneWeight[] boneWeights { get; set; }
				//	public Matrix4x4[] bindposes { get; set; }

			}
		}

		void DebugShowStreamUV(Vector2[] uv)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			foreach (var val in uv)
			{
				sb.AppendLine( val.ToString() );
			}

			System.IO.File.WriteAllText("Assets/TempMeshInfo.txt", sb.ToString());
			AssetDatabase.ImportAsset( "Assets/TempMeshInfo.txt" );
			AssetDatabase.OpenAsset( AssetDatabase.LoadAssetAtPath<TextAsset>( "Assets/TempMeshInfo.txt" ) );
		}
		
		public void MeshInspectorGUI(Mesh m, bool allowStreamSwaps)
		{
			int[] indices = null;
			if (editTempInfo.ContainsKey(m.GetInstanceID()))
			{
				indices = editTempInfo[m.GetInstanceID()].indices;
			}
			else
			{
				editTempInfo[m.GetInstanceID()] = new PerObjectInfo();
				indices = editTempInfo[m.GetInstanceID()].indices;
			}

			GUILayout.Label(string.Format("Triangles : {0} ({1} indices)", m.triangles.Length / 3, m.triangles.Length));
			GUILayout.Label(string.Format("Submeshes : {0}", m.subMeshCount));

			for (int sub = 0; sub < m.subMeshCount; ++sub)
			{
				GUILayout.Label(string.Format("  {0}: Start:{1} Count:{2} ({3} tris)", sub, m.GetIndexStart(sub), m.GetIndexCount(sub), m.GetIndexCount(sub) / 3));
			}

			GUILayout.Label(string.Format("Vertices : {0}", m.vertexCount));

			GUILayout.Label(string.Format("Colours : {0}", m.colors != null ? (m.colors.Length > 0) : false));
			GUILayout.Label(string.Format("Normals : {0}", m.normals != null ? (m.normals.Length > 0) : false));
			GUILayout.Label(string.Format("Tangents : {0}", m.tangents != null ? (m.tangents.Length > 0) : false));

			bool activeChannel1 = (m.uv != null ? (m.uv.Length > 0) : false); // uv1
			bool activeChannel2 = (m.uv2 != null ? (m.uv2.Length > 0) : false);
			bool activeChannel3 = (m.uv3 != null ? (m.uv3.Length > 0) : false);
			bool activeChannel4 = (m.uv4 != null ? (m.uv4.Length > 0) : false);
			bool activeChannel5 = (m.uv5 != null ? (m.uv5.Length > 0) : false);
			bool activeChannel6 = (m.uv6 != null ? (m.uv6.Length > 0) : false);
			bool activeChannel7 = (m.uv7 != null ? (m.uv7.Length > 0) : false);
			bool activeChannel8 = (m.uv8 != null ? (m.uv8.Length > 0) : false);

			GUILayout.BeginHorizontal();
			GUILayout.Label("uv  [" + indices[0] + "]", GUILayout.Width(60));
			activeChannel1 = (m.uv != null ? (m.uv.Length > 0) : false);
			if (allowStreamSwaps)
			{
				GUI.enabled = false;
				GUILayout.Button("Up", GUILayout.Width(45));
				GUI.enabled = activeChannel1;
				if (GUILayout.Button("Down", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv;
					m.uv = m.uv2;
					m.uv2 = uvTmp;
					swap(ref indices[0], ref indices[1]);
				}

				if (GUILayout.Button("Show", GUILayout.Width(45)))
					DebugShowStreamUV(m.uv);
				
				GUI.enabled = true;
			}

			GUILayout.Label("albedo");
			if (!activeChannel2 && !activeChannel3)
				GUILayout.Label("lightmap");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("uv2 [" + indices[1] + "]", GUILayout.Width(60));
			activeChannel2 = (m.uv2 != null ? (m.uv2.Length > 0) : false);
			if (allowStreamSwaps)
			{
				GUI.enabled = activeChannel2;
				if (GUILayout.Button("Up", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv;
					m.uv = m.uv2;
					m.uv2 = uvTmp;
					swap(ref indices[0], ref indices[1]);
				}

				GUI.enabled = activeChannel2;
				if (GUILayout.Button("Down", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv2;
					m.uv2 = m.uv3;
					m.uv3 = uvTmp;
					swap(ref indices[1], ref indices[2]);
				}

				if (GUILayout.Button("Show", GUILayout.Width(45)))
					DebugShowStreamUV(m.uv2);

				GUI.enabled = true;
			}

			if (activeChannel2 && !activeChannel3) GUILayout.Label("lightmap");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("uv3 [" + indices[2] + "]", GUILayout.Width(60));
			activeChannel3 = (m.uv3 != null ? (m.uv3.Length > 0) : false);
			if (allowStreamSwaps)
			{
				GUI.enabled = activeChannel3;
				if (GUILayout.Button("Up", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv2;
					m.uv2 = m.uv3;
					m.uv3 = uvTmp;
					swap(ref indices[1], ref indices[2]);
				}

				GUI.enabled = activeChannel3;
				if (GUILayout.Button("Down", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv3;
					m.uv3 = m.uv4;
					m.uv4 = uvTmp;
					swap(ref indices[2], ref indices[3]);
				}

				if (GUILayout.Button("Show", GUILayout.Width(45)))
					DebugShowStreamUV(m.uv3);

				GUI.enabled = true;
			}

			if (activeChannel3) GUILayout.Label("lightmap");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("uv4 [" + indices[3] + "]", GUILayout.Width(60));
			activeChannel4 = (m.uv4 != null ? (m.uv4.Length > 0) : false);
			if (allowStreamSwaps)
			{
				GUI.enabled = activeChannel4;
				if (GUILayout.Button("Up", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv3;
					m.uv3 = m.uv4;
					m.uv4 = uvTmp;
					swap(ref indices[2], ref indices[3]);
				}

				GUI.enabled = activeChannel4;
				if (GUILayout.Button("Down", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv5;
					m.uv5 = m.uv4;
					m.uv4 = uvTmp;
					swap(ref indices[3], ref indices[4]);
				}

				if (GUILayout.Button("Show", GUILayout.Width(45)))
					DebugShowStreamUV(m.uv4);

				GUI.enabled = true;
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("uv5 [" + indices[4] + "]", GUILayout.Width(60));
			activeChannel5 = (m.uv5 != null ? (m.uv5.Length > 0) : false);
			if (allowStreamSwaps)
			{
				GUI.enabled = activeChannel5;
				if (GUILayout.Button("Up", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv5;
					m.uv5 = m.uv4;
					m.uv4 = uvTmp;
					swap(ref indices[3], ref indices[4]);
				}

				GUI.enabled = activeChannel5;
				if (GUILayout.Button("Down", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv6;
					m.uv6 = m.uv5;
					m.uv5 = uvTmp;
					swap(ref indices[4], ref indices[5]);
				}

				if (GUILayout.Button("Show", GUILayout.Width(45)))
					DebugShowStreamUV(m.uv5);

				GUI.enabled = true;
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("uv6 [" + indices[5] + "]", GUILayout.Width(60));
			activeChannel6 = (m.uv6 != null ? (m.uv6.Length > 0) : false);
			if (allowStreamSwaps)
			{
				GUI.enabled = activeChannel6;
				if (GUILayout.Button("Up", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv6;
					m.uv6 = m.uv5;
					m.uv5 = uvTmp;
					swap(ref indices[4], ref indices[5]);
				}

				GUI.enabled = activeChannel6;
				if (GUILayout.Button("Down", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv7;
					m.uv7 = m.uv6;
					m.uv6 = uvTmp;
					swap(ref indices[5], ref indices[6]);
				}

				if (GUILayout.Button("Show", GUILayout.Width(45)))
					DebugShowStreamUV(m.uv6);

				GUI.enabled = true;
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("uv7 [" + indices[6] + "]", GUILayout.Width(60));
			activeChannel7 = (m.uv7 != null ? (m.uv7.Length > 0) : false);
			if (allowStreamSwaps)
			{
				GUI.enabled = activeChannel7;
				if (GUILayout.Button("Up", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv7;
					m.uv7 = m.uv6;
					m.uv6 = uvTmp;
					swap(ref indices[5], ref indices[6]);
				}

				GUI.enabled = activeChannel7;
				if (GUILayout.Button("Down", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv8;
					m.uv8 = m.uv7;
					m.uv7 = uvTmp;
					swap(ref indices[6], ref indices[7]);
				}

				if (GUILayout.Button("Show", GUILayout.Width(45)))
					DebugShowStreamUV(m.uv7);
				
				GUI.enabled = true;
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("uv8 [" + indices[7] + "]", GUILayout.Width(60));
			activeChannel8 = (m.uv8 != null ? (m.uv8.Length > 0) : false);
			if (allowStreamSwaps)
			{
				GUI.enabled = activeChannel8;
				if (GUILayout.Button("Up", GUILayout.Width(45)))
				{
					Vector2[] uvTmp = m.uv8;
					m.uv8 = m.uv7;
					m.uv7 = uvTmp;
					swap(ref indices[6], ref indices[7]);
				}

				GUI.enabled = false;
				GUILayout.Button("Down", GUILayout.Width(45));
				
				if (GUILayout.Button("Show", GUILayout.Width(45)))
					DebugShowStreamUV(m.uv8);
				
				GUI.enabled = true;
			}

			GUILayout.EndHorizontal();
		}
	}
}



