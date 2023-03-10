using UnityEngine;
using UnityEditor;

namespace ElfDev
{
	public class SortColumnButton
	{
		public class ColumnState
		{
			public bool enabled = true; // Only enabled columns can be sorted
			public bool active = false;
			public bool sortUp = true; // False means sort down
		}

		static Texture iconUp;
		static Texture iconDown;

		static GUIContent gcNone = null;

		static GUIContent gcUp;
		static GUIContent gcDown;

		static GUIStyleState buttonStyleState = null;
		static GUIStyle buttonStyle = null;

		// column is updated, as is colgroup if supplied, return value is true if there was a click
		public static bool Button(ColumnState column, ColumnState[] colgroup, string text, params GUILayoutOption[] options)
		{
			if (buttonStyleState == null) // todo: hijack button, but change only images
			{
				buttonStyleState = new GUIStyleState();
				buttonStyleState.background = Texture2D.whiteTexture;
				buttonStyleState.textColor = Color.black;
			}

			if (buttonStyle == null)
			{
				buttonStyle = new GUIStyle(GUI.skin.button);
				buttonStyle.alignment = TextAnchor.MiddleLeft;
				buttonStyle.stretchWidth = true;

				buttonStyle.active = buttonStyleState; // Rendering settings for when the control is pressed down.
				buttonStyle.focused = buttonStyleState; // Rendering settings for when the element has keyboard focus.
				buttonStyle.hover = buttonStyleState; // Rendering settings for when the mouse is hovering over the control.
				buttonStyle.normal = buttonStyleState; // Rendering settings for when the component is displayed normally.
				buttonStyle.onActive = buttonStyleState; // Rendering settings for when the element is turned on and pressed down.
				buttonStyle.onFocused = buttonStyleState; // Rendering settings for when the element has keyboard and is turned on.
				buttonStyle.onHover = buttonStyleState; // Rendering settings for when the control is turned on and the mouse is hovering it.
				buttonStyle.onNormal = buttonStyleState; // Rendering settings for when the control is turned on.
			}

			if (iconUp == null)
			{
				iconUp = PackageAsset.iconUp;
				gcUp = new GUIContent(iconUp);
			}

			if (iconDown == null)
			{
				iconDown = PackageAsset.iconDown;
				gcDown = new GUIContent(iconDown);
			}

			if (!column.enabled)
			{
				if (gcNone == null)
				{
					gcNone = new GUIContent();
				}

				gcNone.text = text;

				GUILayout.Button(gcNone, buttonStyle, options);
				return false;
			}

			if (column.sortUp)
			{
				gcUp.text = text;

				if (GUILayout.Button(gcUp, buttonStyle, options))
				{
					column.sortUp = false;

					if (colgroup != null)
					{
						foreach (var c in colgroup)
							c.active = false;
					}

					column.active = true;
					return true;
				}

				return false;
			}
			else
			{
				gcDown.text = text;

				if (GUILayout.Button(gcDown, buttonStyle, options))
				{
					column.sortUp = true;

					if (colgroup != null)
					{
						foreach (var c in colgroup)
							c.active = false;
					}

					column.active = true;
					return true;
				}

				return false;
			}
		}
	}
}



