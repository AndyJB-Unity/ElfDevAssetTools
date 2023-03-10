using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// todo: More texture options, compression etc...
// todo: generate actual normal map!
// todo: param handling to specifiy ranges for random generation
// todo: more interesting shapes/materials

namespace ElfDev
{
    public class MakeTestSceneAndAssets : ElfDevEditorWindow<MakeTestSceneAndAssets>
    {
        static Mesh CreateCubeMesh( float prescale=1f )
        {
            Vector3[] vertices =
            {
                new Vector3( +0.5f, +0.5f, -0.5f ),
                new Vector3( +0.5f, -0.5f, -0.5f ),
                new Vector3( -0.5f, -0.5f, -0.5f ),
                new Vector3( -0.5f, +0.5f, -0.5f ),
                new Vector3( +0.5f, +0.5f, +0.5f ),
                new Vector3( -0.5f, +0.5f, +0.5f ),
                new Vector3( -0.5f, -0.5f, +0.5f ),
                new Vector3( +0.5f, -0.5f, +0.5f ),
                new Vector3( +0.5f, +0.5f, -0.5f ),
                new Vector3( +0.5f, +0.5f, +0.5f ),
                new Vector3( +0.5f, -0.5f, +0.5f ),
                new Vector3( +0.5f, -0.5f, -0.5f ),
                new Vector3( +0.5f, -0.5f, -0.5f ),
                new Vector3( +0.5f, -0.5f, +0.5f ),
                new Vector3( -0.5f, -0.5f, +0.5f ),
                new Vector3( -0.5f, -0.5f, -0.5f ),
                new Vector3( -0.5f, -0.5f, -0.5f ),
                new Vector3( -0.5f, -0.5f, +0.5f ),
                new Vector3( -0.5f, +0.5f, +0.5f ),
                new Vector3( -0.5f, +0.5f, -0.5f ),
                new Vector3( +0.5f, +0.5f, +0.5f ),
                new Vector3( +0.5f, +0.5f, -0.5f ),
                new Vector3( -0.5f, +0.5f, -0.5f ),
                new Vector3( -0.5f, +0.5f, +0.5f ),
            };

            Vector2[] uvs =
            {
                new Vector2( +0.0f, +0.0f ),
                new Vector2( +1.0f, +0.0f ),
                new Vector2( +1.0f, +1.0f ),
                new Vector2( +0.0f, +1.0f ),
                new Vector2( +0.0f, +0.0f ),
                new Vector2( +1.0f, +0.0f ),
                new Vector2( +1.0f, +1.0f ),
                new Vector2( +0.0f, +1.0f ),
                new Vector2( +0.0f, +0.0f ),
                new Vector2( +1.0f, +0.0f ),
                new Vector2( +1.0f, +1.0f ),
                new Vector2( +0.0f, +1.0f ),
                new Vector2( +0.0f, +0.0f ),
                new Vector2( +1.0f, +0.0f ),
                new Vector2( +1.0f, +1.0f ),
                new Vector2( +0.0f, +1.0f ),
                new Vector2( +0.0f, +0.0f ),
                new Vector2( +1.0f, +0.0f ),
                new Vector2( +1.0f, +1.0f ),
                new Vector2( +0.0f, +1.0f ),
                new Vector2( +0.0f, +0.0f ),
                new Vector2( +1.0f, +0.0f ),
                new Vector2( +1.0f, +1.0f ),
                new Vector2( +0.0f, +1.0f ),
            };

            int[] triangles =
            {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                8, 10, 11,

                12, 13, 14,
                12, 14, 15,

                16, 17, 18,
                16, 18, 19,

                20, 21, 22,
                20, 22, 23,
            };

            for ( int v=0; v<vertices.Length; ++v )
            {
                vertices[v] *= prescale;
            }

            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }
        
        static Texture2D GenerateTextureAsset( int width, int height, string texpath, bool compress=false, bool normalMap=false )
        {
            float ra = Random.Range(-1.2f, 1.2f);
            float ga = Random.Range(-1.2f, 1.2f);
            float ba = Random.Range(-1.2f, 1.2f);

            float rw = Random.Range(0.05f, 4f);
            float gw = Random.Range(0.05f, 4f);
            float bw = Random.Range(0.05f, 4f);

            float rx = Random.Range(-Mathf.PI, Mathf.PI);
            float gx = Random.Range(-Mathf.PI, Mathf.PI);
            float bx = Random.Range(-Mathf.PI, Mathf.PI);

            Texture2D replacement = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] cols = new Color[width * height];
            for (int i = 0; i < cols.Length; ++i)
            {
                float r = Mathf.Sin(rx + ((float)i / rw)) * ra;
                float g = Mathf.Sin(gx + ((float)i / gw)) * ga;
                float b = Mathf.Sin(bx + ((float)i / bw)) * ba;
                cols[i] = new Color(r, g, b, 1f);
            }
            replacement.SetPixels(cols);
            replacement.Apply();

            if (texpath.EndsWith(".tga"))
            {
                byte[] bs = replacement.EncodeToTGA();

                System.IO.File.WriteAllBytes(Application.dataPath + "/../" + texpath, bs);

                Debug.Log($"Wrote: {Application.dataPath}/../{texpath}");

                DestroyImmediate(replacement);
            }
            else if (texpath.EndsWith(".png"))
            {
                byte[] bs = replacement.EncodeToPNG();

                System.IO.File.WriteAllBytes(Application.dataPath + "/../" + texpath, bs);

                Debug.Log($"Wrote: {Application.dataPath}/../{texpath}");

                DestroyImmediate(replacement);
            }
            else
            {
                Debug.LogError($"Don't recognise File Format for Texture Generate! ({texpath})");
            }
            // else..

            // Get the importer and adjust the importer settings
            AssetImporter imp = AssetImporter.GetAtPath(texpath);
            if ( imp == null )
            {
                // todo: surely I don't need to import twice. Ask AndyM...
                AssetDatabase.ImportAsset(texpath, ImportAssetOptions.ForceUpdate);
                imp = AssetImporter.GetAtPath(texpath);
            }

            TextureImporter timp = imp as TextureImporter;
            if (compress)
            {
                timp.textureCompression = TextureImporterCompression.Compressed;
            }
            else
            {
                timp.textureCompression = TextureImporterCompression.Uncompressed;
            }
            if (normalMap)
            {
                timp.textureType = TextureImporterType.NormalMap;
            }

            AssetDatabase.ImportAsset(texpath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texpath);
        }

        // Parameters
        string folderPrefix = "";

        int numScenes = 5;
        int numObjects = 20;
        bool useRootObject = false;

        float XZextent = 10f;
        float meshPreScaleFactor = 2.5f;

        int texDim = 256;
        bool compressTextures = true;
        private string imageFormat = "png";    // "tga"

        bool prefabs = false;
        bool embeddedMeshes = false;

        void ShowDialog()
        {
            EditorGUILayout.HelpBox("Generate Test Scenes with random unique meshes, materials and textures.", MessageType.Info, true );

            folderPrefix = EditorGUILayout.TextField("Folder Prefix", folderPrefix);

            numScenes = EditorGUILayout.IntField("Num Scenes",numScenes);
            numObjects = EditorGUILayout.IntField("Num Objects",numObjects);
            meshPreScaleFactor = EditorGUILayout.FloatField("Mesh Prescale", meshPreScaleFactor);

            useRootObject = EditorGUILayout.Toggle("Use Root Object", useRootObject);
            if ( useRootObject )
			{
                prefabs = EditorGUILayout.Toggle("Make Root a Prefab", prefabs);
            }
            else
			{
                prefabs = EditorGUILayout.Toggle("Make SceneObjs Prefabs", prefabs);
            }
            //var embedText = prefabs ? "Embed Meshes in Prefabs" : "Embed Meshes in Scenes";
            var embedText = "Embed Meshes in Scenes";
            embeddedMeshes = EditorGUILayout.Toggle($" {embedText}", embeddedMeshes );

            // NOTE. At present meshes can only be embedded in the Scenes. I thought embedding in prefabs should be possible
            // but so far I haven't figured out how.

            texDim = EditorGUILayout.IntField("Texture Dimension", texDim);                 // Pop up Power of 2 sizes?
            compressTextures = EditorGUILayout.Toggle("Compress Textures", compressTextures);

            if ( GUILayout.Button( "Generate!" ) )
            {
                MakeTestScenes();
            }
        }

        protected override void DrawGui()
        {
            ShowDialog();
        }
        
        public  MakeTestSceneAndAssets(): base("Generate Test Scene")
        {
            minSize = new Vector2(400f, 300f);
        }
        

        [MenuItem("ElfDev Asset Insights/Generate Test Scene")]
        static void OpenWindow()
        {
            ShowWindow(true);
        }

        string assetPath;
        string scenePath;

        private int assetCount = 0;
        
        void MakeTestScenes()
        {
            assetCount = 0;

            string sceneBasePath = "Assets/GeneratedScenes";
            string assetBasePath = "Assets/GeneratedAssets";
            if ( folderPrefix != "" )
            {
                sceneBasePath = $"Assets/{folderPrefix}_GeneratedScenes";
                assetBasePath = $"Assets/{folderPrefix}_GeneratedAssets";
            }

            System.IO.Directory.CreateDirectory(sceneBasePath);
            System.IO.Directory.CreateDirectory(sceneBasePath);
            
            for (int i = 0; i < numScenes; ++i)
            {
                scenePath = $"{sceneBasePath}/Scene_{i}.unity";
                assetPath = $"{assetBasePath}/Scene_{i}/";
                MakeTestScene( i );
            }
            
            EditorUtility.ClearProgressBar();
            
            Debug.Log( $"Created {assetCount} assets in total" );
        }

        
        void MakeTestScene( int j )
        {
            float range = 1f / numScenes;
            float baseval = (float) j * range;

            EditorUtility.DisplayProgressBar("Generating Test Scene and Assets:", scenePath, baseval );
            
            Scene createdScene =
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            System.IO.Directory.CreateDirectory(assetPath);
            System.IO.Directory.CreateDirectory(assetPath + "Textures");
            System.IO.Directory.CreateDirectory(assetPath + "Materials");

            if (!embeddedMeshes)
            {
                System.IO.Directory.CreateDirectory(assetPath + "Meshes");
            }

            if ( prefabs )
            {
                System.IO.Directory.CreateDirectory(assetPath + "Prefabs");
            }

            GameObject rootObject = null;
            if (useRootObject)
			{
                rootObject = new GameObject( $"SceneRoot{j}" );
            }

            for (int i = 0; i < numObjects; ++i)
            {
                int assetBaseIndex = (j * numObjects) + i;

                float loopval = ((float) i / (float) numObjects) * range;
                EditorUtility.DisplayProgressBar("Generating Test Scene and Assets:", $"{assetCount}", baseval+loopval );
                
                var go = new GameObject();

                go.name = $"Test Object {assetBaseIndex}";      // {i}

                go.transform.position = new Vector3(Random.Range(-XZextent, XZextent), 0, Random.Range(-XZextent, XZextent));

                if ( useRootObject )
				{
                    go.transform.parent = rootObject.transform;
				}

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                var co = go.AddComponent<BoxCollider>();

                var mesh = CreateCubeMesh( meshPreScaleFactor );
                mesh.name = $"GenMesh{assetBaseIndex}";

                if ( !embeddedMeshes )
                {
                    AssetDatabase.CreateAsset(mesh, $"{assetPath}Meshes/GenMesh{assetBaseIndex}.mesh");
                    assetCount++;
                }

                mf.sharedMesh = mesh;

                var mat = new Material(Shader.Find("Standard"));
                mat.name = $"StandardMat{assetBaseIndex}";
                
                Texture2D albedo = GenerateTextureAsset(texDim, texDim, $"{assetPath}Textures/Tex{assetBaseIndex}_A.{imageFormat}", compressTextures );
                Texture2D normal = GenerateTextureAsset(texDim, texDim, $"{assetPath}Textures/Tex{assetBaseIndex}_N.{imageFormat}", compressTextures, true );
                Texture2D spec = GenerateTextureAsset(texDim, texDim, $"{assetPath}Textures/Tex{assetBaseIndex}_S.{imageFormat}", compressTextures );
                assetCount += 3;
                
                mat.SetTexture("_MainTex", albedo);
                mat.SetTexture("_BumpMap", normal);
                mat.SetTexture("_MetallicGlossMap", spec);

                AssetDatabase.CreateAsset(mat, $"{assetPath}Materials/StandardMat{assetBaseIndex}.mat");
                assetCount++;

                mr.material = mat;

                if ( prefabs && !useRootObject )
                {
                    var prefabInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{assetPath}Prefabs/Prefab{assetBaseIndex}.prefab", InteractionMode.AutomatedAction);
                }
            }

            if (prefabs && useRootObject)
            {
                var prefabInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(rootObject, $"{assetPath}Prefabs/RootPrefab{j}.prefab", InteractionMode.AutomatedAction);
            }

            EditorUtility.DisplayProgressBar("Generating Test Scene and Assets:", scenePath, baseval+range );
            
            AssetDatabase.SaveAssets();

            // todo: prefab-ize some assets?
            
            EditorSceneManager.SaveScene(createdScene, scenePath);
            assetCount++;
        }
    }

}
