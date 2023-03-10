using UnityEngine;
#if UNITY_EDITOR

#endif

namespace ElfDev
{
	public class ComponentLookup
	{
		public delegate bool ComponentVisitor<T>(T component) where T : Component;

		static public void VisitChildComponentRecursive<T>( GameObject _go, ComponentVisitor<T> _v ) where T : Component
		{
			if ( _go != null )
				VisitChildComponentRecursive<T>( _go.transform, _v );
		}

		static public void VisitChildComponentRecursive<T>( Transform _t, ComponentVisitor<T> _v ) where T : Component
		{
			T com = _t.gameObject.GetComponent<T>();
			if ( com != null )
			{
				if ( false == _v(com) )
					return;
			}

			foreach ( Transform t in _t )
			{
				VisitChildComponentRecursive<T>( t, _v );
			}
		}

		static public T FindChildComponentRecursive<T>( GameObject _go ) where T : Component
		{
			if ( _go == null ) return null;
			return FindChildComponentRecursive<T>( _go.transform );
		}

		static public T FindChildComponentRecursive<T>( Transform _t ) where T : Component
		{
			if ( _t == null ) return null;

			T com = _t.gameObject.GetComponent<T>();
			if ( com != null ) return com;

			foreach ( Transform t in _t )
			{
				com = FindChildComponentRecursive<T>( t );
				if ( com != null ) return com;
			}

			return null;
		}

		static public Object FindChildComponentRecursive( GameObject _go, System.Type _y )
		{
			if ( _go == null ) return null;
			return FindChildComponentRecursive( _go.transform, _y );
		}

		static public Object FindChildComponentRecursive( Transform _t, System.Type _y )
		{
			if ( _t == null ) return null;

			Object o = _t.gameObject.GetComponent( _y );
			if ( o != null ) return o;

			foreach ( Transform t in _t )
			{
				o = FindChildComponentRecursive( t, _y );
				if ( o != null ) return o;
			}

			return null;
		}

		static public T FindParentComponent<T>( Transform _t ) where T : Component
		{
			T com = null;
			while( _t != null )
			{
				com = _t.GetComponent<T>();
				if( com != null )
					return com;
				_t = _t.parent;
			}

			return com;
		}

		static public Transform FindChildRecursive( GameObject _go, string name )
		{
			if ( _go == null ) return null;
			return FindChildRecursive( _go.transform, name );
		}
		static public GameObject FindChildGameObjectRecursive( GameObject _go, string name )
		{
			Transform _t = FindChildRecursive( _go, name );
			if ( _t != null )
				return _t.gameObject;
			else
				return null;
		}
		static public Transform FindChildRecursive( Transform _t, string name )
		{
			if ( _t == null ) return null;

			if ( _t.name == name ) return _t;

			Transform _f = _t.Find( name );			// Allow matching of node paths are well as single names
			if ( _f != null ) return _f;

			foreach ( Transform t in _t )
			{
				_f = FindChildRecursive( t, name );
				if ( _f != null ) return _f;
			}

			return null;
		}

		static public string getNodePath( Transform t, string path )
		{
			if ( t == null )
				return path;

			if ( t.parent == null )
			{
				return "/" + t.name + "/" + path;
			}
			else
			{
				return getNodePath( t.parent, t.name + "/" + path );
			}
		}

		static public string getNodePath( Transform t )
		{
			return getNodePath( t.parent, t.name );
		}

		static public string getNodePath( GameObject go )
		{
			return getNodePath( go.transform.parent, go.transform.name );
		}

		/// <summary>
		/// Get all Transforms in the scene, even for GameObjects which are inactive, AS LONG AS at least one GameObject is active. Returns null if all GameObjects are inactive.
		/// </summary>
		static public Transform[] GetTransformsIncludingInactive()
		{
			var firstObj = Object.FindObjectOfType<GameObject>() as GameObject; //Find the first ACTIVE GameObject
			if (firstObj != null)
			{
				return firstObj.transform.root.GetComponentsInChildren<Transform>(true); //true: include inactive
			}
			return new Transform[0];
		}

		/// <summary>
		/// Finds the named transform, even if its GameObject is inactive, AS LONG AS at least one GameObject in the scene is active.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		static public Transform Find(string name)
		{
			var allTransforms = GetTransformsIncludingInactive();
			for (int idx = 0; idx < allTransforms.Length; ++idx)
			{
				if (allTransforms[idx].name == name)
				{
					return allTransforms[idx];
				}
			}
			return null;
		}

		/// <summary>
		/// Get all components of type T in the scene, even for GameObjects which are inactive, AS LONG AS at least one GameObject is active. Returns null if all GameObjects are inactive.
		/// </summary>
		static public T[] GetComponentsIncludingInactive<T>()
			where T : Component
		{
			var firstObj = Object.FindObjectOfType<GameObject>() as GameObject; //Find the first ACTIVE GameObject
			if (firstObj != null)
			{
				return firstObj.transform.root.GetComponentsInChildren<T>(true); //true: include inactive
			}
			return new T[0];
		}
	}
}