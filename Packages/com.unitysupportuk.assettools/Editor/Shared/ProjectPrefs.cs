//#define ENABLE_BACKUP

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
	public class ProjectPrefs
	{
		private static ProjectPrefStore prefStore = null;
		private static Dictionary<string, ProjectPrefEntry> prefs = null;

		private static bool CheckStore()
		{
			if ( prefStore == null )
			{
				prefStore = ProjectPrefStore.LoadOrCreateAsset();

				if ( prefStore == null )
					return false;

				prefs = new Dictionary<string,ProjectPrefEntry>();
				prefStore.Get( ref prefs );
			}

			return true;
		}

		public static bool CannotCheckOut()
		{
			CheckStore();
			return prefStore.cannotCheckOut;
		}

		public static void DeleteAll()
		{
			CheckStore();
			prefs.Clear();
			prefStore.Set( prefs );
			prefStore.Flush();
		}

		public static void DeleteKey( string key )
		{
			CheckStore();
			prefs.Remove( key );
			prefStore.Set( prefs );
			prefStore.Flush();
		}

		private static bool GetEntry( string key, out ProjectPrefEntry entry )
		{
			CheckStore();
			if ( prefs.ContainsKey( key ) )
			{
				entry = prefs[ key ];
				return true;
			}
			entry = new ProjectPrefEntry();
			return false;
		}

		private static bool HasEntry( string key )
		{
			CheckStore();
			if ( prefs.ContainsKey( key ) )
			{
				return true;
			}
			return false;
		}

		private static void SetEntry( string key, ProjectPrefEntry entry )
		{
			CheckStore();
			entry.key = key;
			prefs[ key ] = entry;
			prefStore.Set( prefs );
			prefStore.Flush();
		}

//		public static bool GetBool( string key )
//		{
//			bool defaultValue = false;
//			return EditorPrefs.GetBool( key, defaultValue );
//		}

		public static bool HasBool( string key )
		{
			return HasEntry( key );
		}

		public static bool GetBool( string key, bool defaultValue )
		{
			ProjectPrefEntry entry = new ProjectPrefEntry();
			if ( !GetEntry( key, out entry ) )
				return defaultValue;
			else
				return entry.valueBool;
		}
		
		public static float GetFloat( string key )
		{
			float defaultValue = 0f;
			return EditorPrefs.GetFloat( key, defaultValue );
		}
		
		public static float GetFloat( string key, float defaultValue )
		{
			ProjectPrefEntry entry = new ProjectPrefEntry();
			if ( !GetEntry( key, out entry ) )
				return defaultValue;
			else
				return entry.valueFloat;
		}
		
		public static int GetInt(string key)
		{
			int defaultValue = 0;
			return EditorPrefs.GetInt( key, defaultValue );
		}
		
		public static int GetInt( string key, int defaultValue )
		{
			ProjectPrefEntry entry = new ProjectPrefEntry();
			if ( !GetEntry( key, out entry ) )
				return defaultValue;
			else
				return entry.valueInt;
		}
		
		public static string GetString( string key )
		{
			string empty = string.Empty;
			return EditorPrefs.GetString( key, empty );
		}
		
		public static string GetString( string key, string defaultValue )
		{
			ProjectPrefEntry entry = new ProjectPrefEntry();
			if ( !GetEntry( key, out entry ) )
				return defaultValue;
			else
				return entry.valueString;
		}
		
		public static bool HasKey( string key )
		{
			ProjectPrefEntry entry = new ProjectPrefEntry();
			if ( !GetEntry( key, out entry ) )
				return false;
			else
				return true;
		}
		
		public static void SetBool( string key, bool value )
		{
			ProjectPrefEntry entry = new ProjectPrefEntry();
			entry.type = ProjectPrefEntry.AtomType.ABool;
			entry.valueBool = value;
			entry.valueInt = value ? 1 : 0;
			entry.valueFloat = value ? 1.0f : 0.0f;
			entry.valueString = value ? "true" : "false";
			SetEntry( key, entry );
		}
		
		public static void SetFloat( string key, float value )
		{
			ProjectPrefEntry entry = new ProjectPrefEntry();
			entry.type = ProjectPrefEntry.AtomType.AFloat;
			entry.valueBool = ( value != 0.0f );
			entry.valueInt = (int)value;
			entry.valueFloat = value;
			entry.valueString = value.ToString();
			SetEntry( key, entry );
		}
		
		public static void SetInt( string key, int value )
		{
			ProjectPrefEntry entry = new ProjectPrefEntry();
			entry.type = ProjectPrefEntry.AtomType.AnInt;
			entry.valueBool = ( value != 0 );
			entry.valueInt = value;
			entry.valueFloat = value;
			entry.valueString = value.ToString();
			SetEntry( key, entry );
		}
		
		public static void SetString( string key, string value )
		{
			if ( value == null )
				value = "";
			ProjectPrefEntry entry = new ProjectPrefEntry();
			entry.type = ProjectPrefEntry.AtomType.AString;
			entry.valueBool = ( value.Length > 0 );
			entry.valueInt = entry.valueBool ? 1 : 0;
			entry.valueFloat = entry.valueBool ? 1.0f : 0.0f;
			entry.valueString = value;
			SetEntry( key, entry );
		}

		public static void Flush()
		{
			CheckStore();
			prefStore.Flush();
		}
	}
}

