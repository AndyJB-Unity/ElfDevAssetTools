using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ElfDev
{
	public class StylePalette
	{
		static Color adjustV(Color c, float vScale)
		{
			float h, s, v;
			Color.RGBToHSV(c, out h, out s, out v);
			return Color.HSVToRGB(h, s, v * vScale);
		}

		public static Color red
		{
			get { return Color.red; }
		}

		public static Color green
		{
			get { return Color.green; }
		}

		public static Color blue
		{
			get { return Color.blue; }
		}

		public static Color midblue => new Color(0.3f, 0.4f, 1f, 1f);

		public static Color yellow
		{
			get { return Color.yellow; }
		}

		public static Color cyan
		{
			get { return Color.cyan; }
		}

		public static Color magenta
		{
			get { return Color.magenta; }
		}

		public static Color orange
		{
			get { return Color.Lerp(Color.red, Color.yellow, 0.5f); }
		}

		public static Color white
		{
			get { return Color.white; }
		}

		public static Color grey
		{
			get { return Color.grey; }
		}

		public static Color black
		{
			get { return Color.black; }
		}

		static readonly Color darkred_ = adjustV(Color.red, 0.7f);
		static readonly Color darkblue_ = adjustV(Color.blue, 0.7f);
		static readonly Color darkgreen_ = adjustV(Color.green, 0.7f);
		static readonly Color darkyellow_ = adjustV(Color.yellow, 0.7f);
		static readonly Color darkcyan_ = adjustV(Color.cyan, 0.7f);
		static readonly Color darkmagenta_ = adjustV(Color.magenta, 0.7f);
		static readonly Color darkorange_ = adjustV(orange, 0.7f);

		public static Color darkred
		{
			get { return darkred_; }
		}

		public static Color darkblue
		{
			get { return darkblue_; }
		}

		public static Color darkgreen
		{
			get { return darkgreen_; }
		}

		public static Color darkyellow
		{
			get { return darkyellow_; }
		}

		public static Color darkcyan
		{
			get { return darkcyan_; }
		}

		public static Color darkmagenta
		{
			get { return darkmagenta_; }
		}

		public static Color darkorange
		{
			get { return darkorange_; }
		}

		static GUIStyleState textStyleStateConstructor(Color c)
		{
			GUIStyleState gss = new GUIStyleState();
			gss.background = null;
			gss.textColor = c;
			return gss;
		}

		static readonly GUIStyleState whiteText_ = textStyleStateConstructor(white);
		static readonly GUIStyleState greyText_ = textStyleStateConstructor(grey);
		static readonly GUIStyleState redText_ = textStyleStateConstructor(red);
		static readonly GUIStyleState greenText_ = textStyleStateConstructor(green);
		static readonly GUIStyleState cyanText_ = textStyleStateConstructor(cyan);
		static readonly GUIStyleState yellowText_ = textStyleStateConstructor(yellow);
		static readonly GUIStyleState orangeText_ = textStyleStateConstructor(orange);

		static readonly GUIStyleState darkredText_ = textStyleStateConstructor(darkred);
		static readonly GUIStyleState darkcyanText_ = textStyleStateConstructor(darkcyan);
		static readonly GUIStyleState darkorangeText_ = textStyleStateConstructor(darkorange);

		public static GUIStyleState whiteText
		{
			get { return whiteText_; }
		}

		public static GUIStyleState greyText
		{
			get { return greyText_; }
		}

		public static GUIStyleState redText
		{
			get { return redText_; }
		}

		public static GUIStyleState cyanText
		{
			get { return cyanText_; }
		}

		public static GUIStyleState greenText
		{
			get { return greenText_; }
		}

		public static GUIStyleState yellowText
		{
			get { return yellowText_; }
		}

		public static GUIStyleState orangeText
		{
			get { return orangeText_; }
		}

		public static GUIStyleState darkredText
		{
			get { return darkredText_; }
		}

		public static GUIStyleState darkcyanText
		{
			get { return darkcyanText_; }
		}

		public static GUIStyleState darkorangeText
		{
			get { return darkorangeText_; }
		}

		private static Color _DefaultBackgroundColor;

		public static Color DefaultBackgroundColor
		{
			get
			{
				if (_DefaultBackgroundColor.a == 0)
				{
					var method = typeof(EditorGUIUtility)
						.GetMethod("GetDefaultBackgroundColor", BindingFlags.NonPublic | BindingFlags.Static);
					_DefaultBackgroundColor = (Color)method.Invoke(null, null);
				}
				return _DefaultBackgroundColor;
			}
		}
	}

	class ToolStyles
	{
		static GUIStyle textButton_ = null;

		public static GUIStyle textButton
		{
			get
			{
				if (textButton_ == null)
				{
					textButton_ = new GUIStyle(GUI.skin.button);
				}

				return textButton_;
			}
		}

		public static GUIStyle textButtonWithStyleState(GUIStyleState gss)
		{
			textButton.active = gss; // Rendering settings for when the control is pressed down.		(N/A)
			textButton.focused = gss; // Rendering settings for when the element has keyboard focus.  (N/A)
			textButton.hover = gss; // Rendering settings for when the mouse is hovering over the control. (N/A)
			textButton.normal = gss; // Rendering settings for when the component is displayed normally.
			textButton.onActive = gss; // Rendering settings for when the element is turned on and pressed down.(N/A)
			textButton.onFocused = gss; // Rendering settings for when the element has keyboard and is turned on. (N/A)
			textButton.onHover = gss; // Rendering settings for when the control is turned on and the mouse is hovering it. (N/A)
			textButton.onNormal = gss; // Rendering settings for when the control is turned on. (N/A)
			return textButton;
		}

		static GUIStyle labelButton_ = null;

		public static GUIStyle labelButton
		{
			get
			{
				if (labelButton_ == null)
				{
					labelButton_ = new GUIStyle(GUI.skin.label);
				}

				return labelButton_;
			}
		}
		
		public static GUIStyle labelButtonWithStyleState(GUIStyleState gss)
		{
			labelButton.active = gss; // Rendering settings for when the control is pressed down.		(N/A)
			labelButton.focused = gss; // Rendering settings for when the element has keyboard focus.  (N/A)
			labelButton.hover = gss; // Rendering settings for when the mouse is hovering over the control. (N/A)
			labelButton.normal = gss; // Rendering settings for when the component is displayed normally.
			labelButton.onActive = gss; // Rendering settings for when the element is turned on and pressed down.(N/A)
			labelButton.onFocused = gss; // Rendering settings for when the element has keyboard and is turned on. (N/A)
			labelButton.onHover = gss; // Rendering settings for when the control is turned on and the mouse is hovering it. (N/A)
			labelButton.onNormal = gss; // Rendering settings for when the control is turned on. (N/A)
			return labelButton;
		}
		
		static GUIContent s_tempContent = new GUIContent();

		public static GUIContent TempContent(string t)
		{
			s_tempContent.text = t;
			return s_tempContent;
		}

		static GUIStyle fixedIconRect_ = null;

		public static GUIStyle fixedIconRect
		{
			get
			{
				if (fixedIconRect_ == null)
				{
					fixedIconRect_ = new GUIStyle(GUIStyle.none);
					fixedIconRect_.fixedWidth = 32f;
					fixedIconRect_.fixedHeight = 32f;
				}

				return fixedIconRect_;
			}
		}

		//
	}
}

