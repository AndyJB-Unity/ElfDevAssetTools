//#define USE_TEXTUREUTIL_API				// Requires a UnityEditor build with that API exposed.
//#define USE_PROFILER_API					// Editor Only API
//#define USE_HALF_PROFILER_API				// Editor Only API. Reports double 'real' size the idiot!
#define FUDGE_IT							// 

using UnityEngine;

namespace ElfDev
{
    public class TextureInfo
	{
		public static int RuntimeTextureSizeBytes(Texture tTexture)
		{
#if FUDGE_IT

			// Since Profiler sometime reports double the texture size and sometime not
			// I'm going to use my rough estimate to guess which one to use

			int sizeEstimateBytes = CalculateTextureSizeBytes(tTexture);

			int sizeRuntimeBytes = (int)UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tTexture);
			int sizeRuntimeBytes_Half = sizeRuntimeBytes / 2;

			if (Mathf.Abs(sizeRuntimeBytes - sizeEstimateBytes) < Mathf.Abs(sizeRuntimeBytes_Half - sizeEstimateBytes))
				return sizeRuntimeBytes;
			else
				return sizeRuntimeBytes_Half;

#elif USE_TEXTUREUTIL_API
			int sizeRuntimeBytes = UnityEditor.TextureUtil.GetRuntimeMemorySize( tTexture );
			return sizeRuntimeBytes;
			
#elif USE_PROFILER_API
			int sizeRuntimeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySize( tTexture );
			return sizeRuntimeBytes;
			
#elif USE_HALF_PROFILER_API
			int sizeRuntimeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySize( tTexture );
			return sizeRuntimeBytes / 2;
#else

			return CalculateTextureSizeBytes(tTexture);
#endif
        }

        static int CalculateTextureSizeBytes(Texture tTexture)
		{
			int tSize = 0;

			int tWidth = tTexture.width;
			int tHeight = tTexture.height;
			if (tTexture is Texture2D)
			{
				Texture2D tTex2D = tTexture as Texture2D;
				float bitsPerPixel = GetBitsPerPixel(tTex2D.format);
				int mipMapCount = tTex2D.mipmapCount;
				int mipLevel = 1;
				while (mipLevel <= mipMapCount)
				{
					tSize += (int)((tWidth * tHeight * bitsPerPixel) / 8f);
					tWidth = tWidth / 2;
					tHeight = tHeight / 2;
					mipLevel++;
				}
			}
			else if (tTexture is Cubemap)
			{
				Cubemap tCubemap = tTexture as Cubemap;
				float bitsPerPixel = GetBitsPerPixel(tCubemap.format);

				//Debug.LogWarning( "It's a cube! " + tTexture.name + " > " + tCubemap.format.ToString() + " : " + bitsPerPixel );
				return (int)((tWidth * tHeight * 6f * bitsPerPixel) / 8f);
			}
			else
			{
				// ?
			}

			return tSize;
		}

		static float GetBitsPerPixel(TextureFormat format)
		{
			switch (format)
			{
				case TextureFormat.Alpha8: // Alpha-only texture format.
					return 8;
				case TextureFormat.ARGB4444: // A 16 bits/pixel texture format. Texture stores color with an alpha channel.
					return 16;
				case TextureFormat.RGBA4444: // A 16 bits/pixel texture format.
					return 16;
				case TextureFormat.RGB24: // A color texture format.
					return 24;
				case TextureFormat.RGBA32: // Color with an alpha channel texture format.
					return 32;
				case TextureFormat.ARGB32: // Color with an alpha channel texture format.
					return 32;
				case TextureFormat.RGB565: // A 16 bit color texture format.
					return 16;

				case TextureFormat.DXT1: // Compressed color texture format.
					return 4;
				case TextureFormat.DXT5: // Compressed color with alpha channel texture format.
					return 8;
				case TextureFormat.DXT1Crunched: // Compressed color texture format.
					return 4;
				case TextureFormat.DXT5Crunched: // Compressed color with alpha channel texture format.
					return 8;

				case TextureFormat.PVRTC_RGB2: // PowerVR (iOS) 2 bits/pixel compressed color texture format.
					return 2;
				case TextureFormat.PVRTC_RGBA2: // PowerVR (iOS) 2 bits/pixel compressed with alpha channel texture format
					return 2;
				case TextureFormat.PVRTC_RGB4: // PowerVR (iOS) 4 bits/pixel compressed color texture format.
					return 4;
				case TextureFormat.PVRTC_RGBA4: // PowerVR (iOS) 4 bits/pixel compressed with alpha channel texture format
					return 4;
				case TextureFormat.ETC_RGB4: // ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
					return 4;
				case TextureFormat.BGRA32: // Format returned by iPhone camera
					return 32;

				//AJB Added
				case TextureFormat.R16: // A 16 bit color texture format that only has a red channel.
					return 16;
				case TextureFormat.RHalf: // Scalar (R) texture format, 16 bit floating point.
					return 16;
				case TextureFormat.RGHalf: // Two color (RG) texture format, 16 bit floating point per channel.
					return 32;
				case TextureFormat.RGBAHalf: // RGB color and alpha texture format, 16 bit floating point per channel.
					return 64;
				case TextureFormat.RFloat: // Scalar (R) texture format, 32 bit floating point.
					return 32;
				case TextureFormat.RGFloat: // Two color (RG) texture format, 32 bit floating point per channel.
					return 64;
				case TextureFormat.RGBAFloat: // RGB color and alpha texture format, 32-bit floats per channel.
					return 128;

				case TextureFormat.YUY2: // Unsupported
					Debug.LogWarning("Unsupported Texture Format : " + format.ToString());
					return 0;

				case TextureFormat.BC6H: // Unsupported
					return 16 * 3;

				case TextureFormat.BC7: // Unsupported
					Debug.LogWarning("Unsupported Texture Format : " + format.ToString());
					return 0;
				case TextureFormat.BC4: // Unsupported
					Debug.LogWarning("Unsupported Texture Format : " + format.ToString());
					return 0;
				case TextureFormat.BC5: // Unsupported
					Debug.LogWarning("Unsupported Texture Format : " + format.ToString());
					return 0;

				case TextureFormat.EAC_R: // ETC2 / EAC (GL ES 3.0) 4 bits/pixel compressed unsigned single-channel texture format.
					return 4;
				case TextureFormat.EAC_R_SIGNED: // ETC2 / EAC (GL ES 3.0) 4 bits/pixel compressed signed single-channel texture format.
					return 4;
				case TextureFormat.EAC_RG: // ETC2 / EAC (GL ES 3.0) 8 bits/pixel compressed unsigned dual-channel (RG) texture format.
					return 8;
				case TextureFormat.EAC_RG_SIGNED: // ETC2 / EAC (GL ES 3.0) 8 bits/pixel compressed signed dual-channel (RG) texture format.
					return 8;

				case TextureFormat.ETC2_RGB: // ETC2 (GL ES 3.0) 4 bits/pixel compressed RGB texture format.
					return 4;
				case TextureFormat.ETC2_RGBA1: // ETC2 (GL ES 3.0) 4 bits/pixel RGB+1-bit alpha texture format.
					return 5;
				case TextureFormat.ETC2_RGBA8: // ETC2 (GL ES 3.0) 8 bits/pixel compressed RGBA texture format.
					return 8;

				case TextureFormat.ASTC_4x4: // ASTC (4x4 pixel block in 128 bits) compressed RGB texture format.
					return (128) / (4 * 4);
				case TextureFormat.ASTC_5x5: // ASTC (5x5 pixel block in 128 bits) compressed RGB texture format.
					return (128) / (5 * 5);
				case TextureFormat.ASTC_6x6: // ASTC (6x6 pixel block in 128 bits) compressed RGB texture format.
					return (128) / (6 * 6);
				case TextureFormat.ASTC_8x8: // ASTC (8x8 pixel block in 128 bits) compressed RGB texture format.
					return (128) / (8 * 8);
				case TextureFormat.ASTC_10x10: // ASTC (10x10 pixel block in 128 bits) compressed RGB texture format.
					return (128) / (10 * 10);
				case TextureFormat.ASTC_12x12: // ASTC (12x12 pixel block in 128 bits) compressed RGB texture format.
					return (128) / (12 * 12);
				
			}

			Debug.LogWarning("Unrecognized Texture Format " + format.ToString());
			return 0;
		}
	}

}

