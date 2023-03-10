using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
	[System.Serializable]
	public class ProjectPrefEntry
	{
		public enum AtomType { ABool, AnInt, AFloat, AString };
		public string key;
		public AtomType type;
		public bool valueBool;
		public float valueFloat;
		public int valueInt;
		public string valueString;
	}

	[System.Serializable]
	public class JsonProjectPrefs
	{
		public List<ProjectPrefEntry> entries;
	}

	public class ProjectPrefStore
	{
		public List<ProjectPrefEntry> entries = new List<ProjectPrefEntry>();

		public string assetPath_ = "Assets/ProjectPrefs.json.txt";

		private const string default_path_ = "Assets/ProjectPrefs.json.txt";

		public void CloneFrom( ProjectPrefStore from )
		{
			entries = new List<ProjectPrefEntry>( from.entries.ToArray() );
		}

		public bool cannotCheckOut = false;

		bool CheckoutJSONAssetJU( string jsonPath )
		{
			try
			{
				if ( UnityEditor.VersionControl.Provider.onlineState == UnityEditor.VersionControl.OnlineState.Online )
				{
					// Note, it has to be a loaded asset for the APIs to work, the other APIs are stuffed
					var loadedAsset = AssetDatabase.LoadAssetAtPath<TextAsset>( jsonPath );
					//Debug.Log( "Asset pre check for " + jsonPath + " = " + (loadedAsset!=null ? "OK" : "Not OK") );

					if ( loadedAsset != null )
					{
						UnityEditor.VersionControl.Task task = UnityEditor.VersionControl.Provider.Checkout( loadedAsset, UnityEditor.VersionControl.CheckoutMode.Asset );
						task.Wait();

						if ( task.success )
						{
							Debug.LogWarning( "Checked out " + jsonPath );

							// File does not seem to be readable immediately, fudge it
							System.IO.File.SetAttributes(jsonPath, System.IO.File.GetAttributes(jsonPath) & ~System.IO.FileAttributes.ReadOnly);
						}
						else
						{
							Debug.LogWarning( "Checkout failed on " + jsonPath );
							foreach ( var m in task.messages )
								m.Show();

							// Oh well, fudge it for now!
							if ( System.IO.File.Exists( jsonPath ) )
							{
								Debug.LogWarning( "Forced RW on" + jsonPath );
								System.IO.File.SetAttributes( jsonPath, System.IO.File.GetAttributes(jsonPath) & ~System.IO.FileAttributes.ReadOnly );
							}

							cannotCheckOut = true;
						}

						Resources.UnloadAsset( loadedAsset );
					}

				}
				else
				{
					// No source control
					Debug.LogWarning( jsonPath + " NOT UNDER SOURCE CONTROL!" );

					// Oh well, fudge it for now!
					if ( System.IO.File.Exists( jsonPath ) )
					{
						Debug.LogWarning( "Forced RW on" + jsonPath );
						System.IO.File.SetAttributes( jsonPath, System.IO.File.GetAttributes(jsonPath) & ~System.IO.FileAttributes.ReadOnly );
					}

					cannotCheckOut = true;
				}
			}
			catch
			{
				Debug.LogError( "Failed to checkout or attrib " + jsonPath );
				return false;
			}
			return true;
		}

		void SaveJSONAssetJU( ProjectPrefStore store, string jsonPath )
		{
			CheckoutJSONAssetJU( jsonPath );

			JsonProjectPrefs jpp = new JsonProjectPrefs();
			jpp.entries = store.entries;

			string jsonStream = JsonUtility.ToJson( jpp, true );
			System.IO.File.WriteAllText( jsonPath, jsonStream, System.Text.Encoding.UTF8 );
		}

		static ProjectPrefStore LoadJSONAssetJU( string jsonPath )
		{
			string jsonText = System.IO.File.ReadAllText( jsonPath, System.Text.Encoding.UTF8 );
			JsonProjectPrefs jpp = JsonUtility.FromJson<JsonProjectPrefs>( jsonText );
			ProjectPrefStore pps = new ProjectPrefStore();
			pps.assetPath_ = jsonPath;
			pps.entries = jpp.entries;
			return pps;
		}

		public static ProjectPrefStore CreateAsset()
		{
			return CreateAsset( default_path_ );
		}

		public static ProjectPrefStore CreateAsset( string jsonPath )
		{
			ProjectPrefStore asset = new ProjectPrefStore();
			asset.assetPath_ = jsonPath;
			asset.Flush();
			return asset;
		}

		public static ProjectPrefStore LoadAsset()
		{
			return LoadAsset( default_path_ );
		}

		public static ProjectPrefStore LoadAsset( string jsonPath )
		{
			if ( System.IO.File.Exists( jsonPath ) )
			{
				return LoadJSONAssetJU( jsonPath );
			}
			else
			{
				return null;
			}
		}

		public static ProjectPrefStore LoadOrCreateAsset()
		{
			ProjectPrefStore asset = LoadAsset();
			if ( asset == null )
			{
				asset = CreateAsset();
			}
			return asset;
		}

		public ProjectPrefStore LoadOrCreateAsset( string assetPath )
		{
			ProjectPrefStore asset = LoadAsset( assetPath );
			if ( asset == null )
			{
				asset = CreateAsset( assetPath );
			}
			return asset;
		}

		public void Flush()
		{
			SaveJSONAssetJU( this, this.assetPath_ );
		}

		public void Set( Dictionary<string,ProjectPrefEntry> values )
		{
			entries = new List<ProjectPrefEntry>();
			foreach ( ProjectPrefEntry entry in values.Values )
			{
				entries.Add( entry );
			}
		}

		public void Get( ref Dictionary<string,ProjectPrefEntry> values )
		{
			values.Clear();
			foreach ( ProjectPrefEntry entry in entries )
			{
				values[ entry.key ] = entry;
			}
		}
	}

}

