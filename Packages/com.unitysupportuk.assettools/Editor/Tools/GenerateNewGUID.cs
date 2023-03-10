using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;

// Right click Menu Item "Assets/Generate New GUIDs"
//
// Multiple selected objects can have new guids generated in one action.
// New guid is assigned to selected assets and all references in project
// to the old guid are edited to point to the new guid.
//
// NOTE. If 'recurseFolders' is set 'true' folders will be recrused and contents processed
// NOTE. If 'reguidFolders' is set 'true' folders will get new guids too, otherwise they will keep their old guid
//
// NOTE. Best to close any open scenes that might reference a remapped object before running.

public class GenerateNewGUID
{
	private static bool recurseFolders = true;
	private static bool reguidFolders = true;

	[MenuItem("Assets/Generate New GUIDs")]
	private static void DoGenerateNewGUID()
	{
		// Let's handle all the selections
		var targetObjects = Selection.objects;
		if ( recurseFolders )
			targetObjects = Selection.GetFiltered<Object>( SelectionMode.DeepAssets );

		var assetsToProcess = targetObjects.Select(
			aa =>
			{
				return AssetDatabase.GetAssetPath(aa);
			}
			).ToArray();

		ReCreate( assetsToProcess );
	}

	[MenuItem("Assets/Generate New GUIDs", true)]
	private static bool DoGenerateNewGUIDValidate()
	{
		return true;
	}

	public class ReferenceInfo
	{
		public HashSet<string> referencingAssets = new HashSet<string>();
	}

	public static void CollectReferencingAssets( bool showProgress, Dictionary<string,ReferenceInfo> targetAssets )
	{
		if (showProgress)
			EditorUtility.DisplayProgressBar("Finding references... ", "...", 0.0f);

		List<string> allGuids = new List<string>();
		allGuids.AddRange(AssetDatabase.FindAssets("t:Object"));

		int index = 0;
		foreach (var guidAsset in allGuids)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guidAsset);

			if (assetPath.StartsWith("Assets/StreamingAssets")) // LOL nope!
				continue;

			if (System.IO.Directory.Exists(assetPath))
			{
				// This is a directory not an asset
			}
			else
			{
				// Check what this asset depends on
				string[] dependsOn = AssetDatabase.GetDependencies(new string[] { assetPath });

				foreach (var dep in dependsOn)
				{
					var guidDep = AssetDatabase.AssetPathToGUID(dep);
					if ( targetAssets.ContainsKey(guidDep))
					{
						// One of our tracked assets
						if (guidDep != guidAsset) // You can't depend on yourself
						{
							// Debug.Log( $"Ref {guidAsset} depends on {guidDep}" );
							targetAssets[guidDep].referencingAssets.Add(guidAsset);
						}
					}
				}
			}

			if (showProgress)
			{
				if (index % 10 == 0)
				{
					float prog = (float)index / (float)allGuids.Count;
					EditorUtility.DisplayProgressBar("Finding references... ", assetPath, prog);
				}
			}

			++index;
		}

		// End progress bar
		if (showProgress)
			EditorUtility.ClearProgressBar();
	}

	static void RemapReferences(Dictionary<string,string> oldToNewGuidMap, Dictionary<string,ReferenceInfo> targetAssets )
	{
		AssetDatabase.StartAssetEditing();

		//foreach (var (oldGuid,newGuid) in oldToNewGuidMap)
		foreach (var kvGuid in oldToNewGuidMap)
		{
			var oldGuid = kvGuid.Key;
			var newGuid = kvGuid.Value;

			Debug.Log( $"Remapping {oldGuid} => {newGuid}" );

			var assetsToRemap = targetAssets[ oldGuid ];

			foreach (var atr in assetsToRemap.referencingAssets )
			{
				var candidateGuid = oldToNewGuidMap.ContainsKey(atr) ? oldToNewGuidMap[ atr ] : atr ;

				var assetPath = AssetDatabase.GUIDToAssetPath(candidateGuid);

				// Debug.Log($"Editing file {candidateGuid} - {assetPath}");

				WriteNewReferences( assetPath, oldGuid, newGuid) ;
			}
		}

		AssetDatabase.StopAssetEditing();
	}

	private static void WriteNewReferences(string assetPath, string oldGuid, string newGuid)
	{
		string[] lines = System.IO.File.ReadAllLines(assetPath);

		// Debug.Log( $"Checking:{assetPath} ({lines.Length} lines)" );

		int lineNo = 0;
		bool updated = false;
		foreach (var lineIn in lines)
		{
			if (lineIn.Contains(oldGuid))
			{
				Debug.Log($"Fixing {assetPath}#{lineNo + 1}");
				string lineOut = lineIn.Replace(oldGuid, newGuid);
				lines[lineNo] = lineOut;
				updated = true;
			}

			++lineNo;
		}

		if (updated)
		{
			// Debug.Log( "Saving and reimporting " + assetPath );
			System.IO.File.WriteAllLines(assetPath, lines);
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
		}
	}

	public static void ReCreate( string[] sourceAssetPaths )
	{
		Dictionary<string,string> oldToNewGuidMap = new Dictionary<string, string>();
		Dictionary<string,ReferenceInfo> targetAssets = new Dictionary<string, ReferenceInfo>();

		// Gather references
		foreach (var sourceAssetPath in sourceAssetPaths)
		{
			string sourceGuid = AssetDatabase.AssetPathToGUID(sourceAssetPath);
			targetAssets[ sourceGuid ] = new ReferenceInfo();
		}
		CollectReferencingAssets(true, targetAssets);

		var foldersToReguid = new Dictionary<string, string>();

		foreach (var sourceAssetPath in sourceAssetPaths)
		{
			if (reguidFolders && AssetDatabase.IsValidFolder(sourceAssetPath))
			{
				// Handle folders after assets are done
 				string folderGuid = AssetDatabase.AssetPathToGUID(sourceAssetPath);
				foldersToReguid[ folderGuid ] = sourceAssetPath;
				continue;
			}

			if (AssetDatabase.IsValidFolder(sourceAssetPath)) // If folder not handled above skip it now
				continue;

			string sourceAssetName = Path.GetFileName(sourceAssetPath);
			string destinationAssetPath = Path.Combine(Path.GetDirectoryName(sourceAssetPath),
				$"{Path.GetFileNameWithoutExtension(sourceAssetPath)}-copy{Path.GetExtension(sourceAssetPath)}" );

			string sourceGuid = AssetDatabase.AssetPathToGUID(sourceAssetPath);

			bool bRes = AssetDatabase.CopyAsset(sourceAssetPath, destinationAssetPath);
			if ( !bRes )
				Debug.LogWarning( $"Failed to copy {sourceAssetPath} to {destinationAssetPath}" );

			oldToNewGuidMap[ sourceGuid ] = AssetDatabase.AssetPathToGUID( destinationAssetPath );

			bRes = AssetDatabase.DeleteAsset(sourceAssetPath);
			if ( !bRes )
				Debug.LogWarning( $"Failed to delete {sourceAssetPath}" );

			string res = AssetDatabase.RenameAsset(destinationAssetPath, sourceAssetName);
			if ( res.Length > 0 )
				Debug.LogWarning( res );
		}

		// Remap references
		RemapReferences( oldToNewGuidMap, targetAssets );

		// Flush any remaining edits
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

		// Handle Folders if required
		foreach (var folderAsset in foldersToReguid)
		{
			string folderAssetGuid = folderAsset.Key;
			string folderAssetPath = AssetDatabase.GUIDToAssetPath(folderAssetGuid);

			string sourceFolderName = Path.GetFileName(folderAssetPath);
			string parentFolder = folderAssetPath.Substring( 0,folderAssetPath.Length - sourceFolderName.Length - 1 );
			string tempFolderName = sourceFolderName + "_copy";
			string tempAssetPath = Path.Combine(parentFolder, tempFolderName);

			// Rename
			string renamed = AssetDatabase.RenameAsset(folderAssetPath, tempFolderName);
			if ( renamed.Length > 0 )
				Debug.LogWarning( renamed );

			// New guid for path
			string newFolderGuid = AssetDatabase.CreateFolder( parentFolder, sourceFolderName );
			if (newFolderGuid.Length == 0)
			{
				Debug.Log($"Failed to make new folder {tempFolderName}");
				continue;
			}

			// Filesystem shenanigans. ( NOTE we will lose any tags/etc on the original folder. todo: fix this )
			Directory.Delete( folderAssetPath );
			File.Delete( tempAssetPath + ".meta" );
			Directory.Move( tempAssetPath,folderAssetPath );

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		}
	}
}
