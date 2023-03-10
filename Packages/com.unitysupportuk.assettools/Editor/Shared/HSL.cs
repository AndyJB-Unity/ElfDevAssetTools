using UnityEngine;

namespace ElfDev
{
    public class HSVColour
    {
        public static Color ColorFromHSV(Vector4 hsv)
        {
            return ColorFromHSV(hsv.x, hsv.y, hsv.z, hsv.w);
        }

        // http://pastebin.com/683Gk9xZ
        public static Color ColorFromHSV(float h, float s, float v, float a = 1)
        {
            // no saturation, we can return the value across the board (grayscale)
            if (s == 0)
                return new Color(v, v, v, a);

            // which chunk of the rainbow are we in?
            float sector = h / 60;

            // split across the decimal (ie 3.87 into 3 and 0.87)
            int i = (int)sector;
            float f = sector - i;

            float p = v * (1 - s);
            float q = v * (1 - s * f);
            float t = v * (1 - s * (1 - f));

            // build our rgb color
            Color color = new Color(0, 0, 0, a);

            switch (i)
            {
                case 0:
                    color.r = v;
                    color.g = t;
                    color.b = p;
                    break;

                case 1:
                    color.r = q;
                    color.g = v;
                    color.b = p;
                    break;

                case 2:
                    color.r = p;
                    color.g = v;
                    color.b = t;
                    break;

                case 3:
                    color.r = p;
                    color.g = q;
                    color.b = v;
                    break;

                case 4:
                    color.r = t;
                    color.g = p;
                    color.b = v;
                    break;

                default:
                    color.r = v;
                    color.g = p;
                    color.b = q;
                    break;
            }

            return color;
        }

        public static Color HSVLerp(Color colorA, Color colorB, float alpha)
        {
            return ColorFromHSV(Vector4.Lerp(ColorToHSV(colorA), ColorToHSV(colorB), alpha));
        }

        public static Vector4 ColorToHSV(Color color)
        {
            float h, s, v;
            ColorToHSV(color, out h, out s, out v);
            return new Vector4(h, s, v, color.a);
        }

        public static void ColorToHSV(Color color, out float h, out float s, out float v)
        {
            float min = Mathf.Min(Mathf.Min(color.r, color.g), color.b);
            float max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
            float delta = max - min;

            // value is our max color
            v = max;

            // saturation is percent of max
            if (!Mathf.Approximately(max, 0))
            {
                s = delta / max;
            }
            else
            {
                // all colors are zero, no saturation and hue is undefined
                s = 0;
                h = -1;
                return;
            }

            // grayscale image if min and max are the same
            if (Mathf.Approximately(min, max))
            {
                v = max;
                s = 0;
                h = -1;
                return;
            }

            // hue depends which color is max (this creates a rainbow effect)
            if (color.r == max)
                h = (color.g - color.b) / delta;            // between yellow & magenta
            else if (color.g == max)
                h = 2 + (color.b - color.r) / delta;        // between cyan & yellow
            else
                h = 4 + (color.r - color.g) / delta;        // between magenta & cyan

            // turn hue into 0-360 degrees
            h *= 60;
            if (h < 0)
                h += 360;
        }
    }
}

/*
public class HSLColour
{
	public float H = 0.0f;
	public float S = 0.0f;
	public float L = 0.0f;

	public HSLColour( System.Drawing.Color rgb )
	{
		// This conversion is easy thanks to .net 
		H = rgb.GetHue() / 360.0f;
		S = rgb.GetSaturation();
		L = rgb.GetBrightness();
	}

	public System.Drawing.Color ToRGB()
	{
		// This conversion is harder, no .net support 
		// See Foley et Van Damme for details of conversion
		float[] rgb = { 0.0f, 0.0f, 0.0f };

		if ( L == 0.0f )
		{
			// No luminence, colour must be black!
		}
		else if ( S == 0.0f )
		{
			rgb[ 0 ] = rgb[ 1 ] = rgb[ 2 ] = L;	// No saturation, colour is grey
		}
		else
		{
			float temp2 = ( L < 0.5f ) ? ( L * ( 1.0f + S ) ) : ( ( L + S ) - ( L * S ) );
			float temp1 = ( 2.0f * L ) - temp2;
			float[] temp3 = new float[] { H + ( 1.0f / 3.0f ), H, H - ( 1.0f / 3.0f ) };
			for ( int i = 0; i < 3; ++i )
			{
				if ( temp3[ i ] < 0.0f ) temp3[ i ] += 1.0f;
				if ( temp3[ i ] > 1.0f ) temp3[ i ] -= 1.0f;
				if ( ( 6.0f * temp3[ i ] ) < 1.0f )
					rgb[ i ] = temp1 + ( temp2 - temp1 ) * 6.0f * temp3[ i ];
				else if ( ( 2.0f * temp3[ i ] ) < 1.0f )
					rgb[ i ] = temp2;
				else if ( ( 3.0f * temp3[ i ] ) < 2.0f )
					rgb[ i ] = temp1 + ( temp2 - temp1 ) * ( ( 2.0f / 3.0f ) - temp3[ i ] ) * 6.0f;
				else
					rgb[ i ] = temp1;
			}
		}

		return System.Drawing.Color.FromArgb( (int)( 255 * rgb[ 0 ] ), (int)( 255 * rgb[ 1 ] ), (int)( 255 * rgb[ 2 ] ) );
	}
}
*/
