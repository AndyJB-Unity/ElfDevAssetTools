using UnityEngine;
using UnityEditor;

// Adapted from Unity.foldout

namespace ElfDev
{
	public class ClickableFoldout : MonoBehaviour
	{
		static GUIContent s_FoldoutContent = new GUIContent();

		static GUIContent TempContent(string t)
		{
			s_FoldoutContent.text = t;
			return s_FoldoutContent;
		}

		// inout foldout status
		// out	 label clicked
		public static void Foldout(ref bool foldout, out bool clicked, string textContent)
		{
			FoldoutInternal(ref foldout, out clicked, TempContent(textContent), EditorStyles.foldout);
		}

		public static void Foldout(ref bool foldout, out bool clicked, GUIContent content)
		{
			FoldoutInternal(ref foldout, out clicked, content, EditorStyles.foldout);
		}

		public static void Foldout(ref bool foldout, out bool clicked, GUIContent content, GUIStyle style)
		{
			FoldoutInternal(ref foldout, out clicked, content, style);
		}

		static Rect s_LastRect;

		static void FoldoutInternal(ref bool foldout, out bool clicked, GUIContent content, GUIStyle style)
		{
			float kSingleLineHeight = EditorGUIUtility.singleLineHeight; // EditorGUI.kSingleLineHeight
			Rect r = s_LastRect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUIUtility.fieldWidth, kSingleLineHeight, kSingleLineHeight, style);
			FoldoutInternal(r, ref foldout, out clicked, content, style);
		}

		static int s_FoldoutHash = "Foldout".GetHashCode();

		static GUIStyle foldoutSelected
		{
			get { return GUIStyle.none; }
		}

		static float indent
		{
			get
			{
//	        const float kIndentPerLevel = 15;					// From EditorGUI - not exposed!
//			return EditorGUI.indentLevel * kIndentPerLevel;		// But the level is, hmmm

				Rect r = EditorGUI.IndentedRect(Rect.zero);
				return r.x;
			}
		}

		private static int s_DragUpdatedOverID = 0;

		private static Color s_MixedValueContentColor = new Color(1, 1, 1, 0.5f);
		private static Color s_MixedValueContentColorTemp = Color.white;

		static void BeginHandleMixedValueContentColor()
		{
			s_MixedValueContentColorTemp = GUI.contentColor;
			GUI.contentColor = EditorGUI.showMixedValue ? (GUI.contentColor * s_MixedValueContentColor) : GUI.contentColor;
		}

		static void EndHandleMixedValueContentColor()
		{
			GUI.contentColor = s_MixedValueContentColorTemp;
		}

		//static GUIContent s_MixedValueContent = EditorGUIUtility.TextContent( "\u2014|Mixed Values" );
		static GUIContent s_MixedValueContent = new GUIContent("\u2014", "Mixed Values");

		static double s_FoldoutDestTime = 0f;
		const double kFoldoutExpandTimeout = 0.7;

		// Make a label with a foldout arrow to the left of it.
		static void FoldoutInternal(Rect position, ref bool foldout, out bool clicked, GUIContent content, GUIStyle style)
		{
			clicked = false;

			Rect origPosition = position;
			if (EditorGUIUtility.hierarchyMode)
			{
				int offset = (EditorStyles.foldout.padding.left - EditorStyles.label.padding.left);
				position.xMin -= offset;
			}

			int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
			EventType eventType = Event.current.type;

			// special case test, so we are able to receive mouse events when we are disabled. This allows the foldout to still be expanded/contracted when disabled.
/*		if ( !GUI.enabled && GUIClip.enabled && ( Event.current.rawType == EventType.MouseDown || Event.current.rawType == EventType.MouseDrag || Event.current.rawType == EventType.MouseUp ) )
		{
			eventType = Event.current.rawType;
		} */ // GUIClip not public, doh!

			switch (eventType)
			{
				case EventType.MouseDown:

					// If the mouse is inside the button, we say that we're the hot control
					if (position.Contains(Event.current.mousePosition) && Event.current.button == 0)
					{
						GUIUtility.keyboardControl = GUIUtility.hotControl = id;
						Event.current.Use();
					}

					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0;

						// If we got the mousedown, the mouseup is ours as well
						// (no matter if the click was in the button or not)
						Event.current.Use();

						// toggle the passed-in value if the mouse was over the button & return true
						Rect clickRect = position;

						bool clickedAnywhere = clickRect.Contains(Event.current.mousePosition);

						clickRect.width = style.padding.left;
						clickRect.x += indent;

						bool clickedOnArrow = clickRect.Contains(Event.current.mousePosition);

						if (clickedAnywhere)
						{
							if (clickedOnArrow)
							{
								GUI.changed = true;
								foldout = !foldout;
							}
							else
							{
								clicked = true;
							}
						}
					}

					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == id)
					{
						Event.current.Use();
					}

					break;
				case EventType.Repaint:
					/*EditorStyles.*/
					foldoutSelected.Draw(position, GUIContent.none, id, s_DragUpdatedOverID == id);

					Rect drawRect = new Rect(position.x + indent, position.y, EditorGUIUtility.labelWidth - indent, position.height);

					// If mixed values, indicate it in the collapsed foldout field so it's easy to see at a glance if anything
					// in the Inspector has different values. Don't show it when expanded, since the difference will be visible further down.
					if (EditorGUI.showMixedValue && !foldout)
					{
						style.Draw(drawRect, content, id, foldout);

						BeginHandleMixedValueContentColor();
						Rect fieldPosition = origPosition;
						fieldPosition.xMin += EditorGUIUtility.labelWidth;
						EditorStyles.label.Draw(fieldPosition, s_MixedValueContent, id, false);
						EndHandleMixedValueContentColor();
					}
					else
					{
						style.Draw(drawRect, content, id, foldout);
					}

					break;
				case EventType.KeyDown:
					if (GUIUtility.keyboardControl == id)
					{
						KeyCode kc = Event.current.keyCode;
						if ((kc == KeyCode.LeftArrow && foldout == true) || (kc == KeyCode.RightArrow && foldout == false))
						{
							foldout = !foldout;
							GUI.changed = true;
							Event.current.Use();
						}
					}

					break;
				case EventType.DragUpdated:
					if (s_DragUpdatedOverID == id)
					{
						if (position.Contains(Event.current.mousePosition))
						{
							if (Time.realtimeSinceStartup > s_FoldoutDestTime)
							{
								foldout = true;
								Event.current.Use();
							}
						}
						else
						{
							s_DragUpdatedOverID = 0;
						}
					}
					else
					{
						if (position.Contains(Event.current.mousePosition))
						{
							s_DragUpdatedOverID = id;
							s_FoldoutDestTime = Time.realtimeSinceStartup + kFoldoutExpandTimeout;
							Event.current.Use();
						}
					}

					break;
				case EventType.DragExited:
					if (s_DragUpdatedOverID == id)
					{
						s_DragUpdatedOverID = 0;
						Event.current.Use();
					}

					break;
			}

			return;
		}

	}

}
