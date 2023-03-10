#define EXCLUDE_STREAMING_ASSETS		// They have no dependencies anyway, no need to waste time or space on them
#define USING_DEPS

using UnityEngine;
using UnityEditor;

namespace ElfDev
{
	public partial class TextureList : ElfDevEditorWindow<TextureList>
	{
		class TextureEntry : TextureEntryBase
		{
			public TextureEntry(string guid_)
				: base(guid_) { }

			public override Texture InitAsset()
			{
				if (base.InitAsset() == null)
					return null;

				DisplayPrep();

				return asset;
			}

			// Display memos
			public string d_dimension;

			public string d_width;
			public string d_height;
			public string d_filterMode;
			public string d_wrapMode;

			public bool isTexture2d = false;
			public bool isReadable = false;

			public string d_alphaIsTransparency;
			public string d_mipmapCount;
			public string d_format;

			public string d_sRGBTexture;

			public string d_estimatedSize;

			public string d_importerPreset;

			public void DisplayPrep()
			{
				d_dimension = displayFieldMemoizer.get(asset.dimension.ToString());

				d_width = displayFieldMemoizer.get(asset.width.ToString());
				d_height = displayFieldMemoizer.get(asset.height.ToString());
				d_filterMode = displayFieldMemoizer.get(asset.filterMode.ToString());
				d_wrapMode = displayFieldMemoizer.get(asset.wrapMode.ToString());

				Texture2D te2d = asset as Texture2D;
				if (te2d != null)
				{
					isTexture2d = true;

					d_alphaIsTransparency = displayFieldMemoizer.get(te2d.alphaIsTransparency ? "alphaIsT" : " ");
					d_mipmapCount = displayFieldMemoizer.get(te2d.mipmapCount.ToString());
					d_format = displayFieldMemoizer.get(te2d.format.ToString());

					TextureImporter ti = TextureImporter.GetAtPath(assetPath) as TextureImporter;
					if (ti != null)
					{
						d_sRGBTexture = displayFieldMemoizer.get(ti.sRGBTexture ? "sRGB" : "Linear");

						isReadable = ti.isReadable;

						if (isReadable)
							d_format += " R";
					}
				}
				else
				{
					d_alphaIsTransparency = displayFieldMemoizer.get("");
					d_mipmapCount = displayFieldMemoizer.get("");
					d_format = displayFieldMemoizer.get("");
					d_sRGBTexture = displayFieldMemoizer.get("");
				}

				float sizeKB = ((float)estimatedSizeBytes / 1024f);
				float sizeMB = (sizeKB / 1024f);
				d_estimatedSize = sizeMB > 1f ? sizeMB.ToString("0.00") + "MB" : sizeKB.ToString("0.0") + "KB";

				d_importerPreset = displayFieldMemoizer.get(importerPreset);

			}
		}

		// Fin
	}


}