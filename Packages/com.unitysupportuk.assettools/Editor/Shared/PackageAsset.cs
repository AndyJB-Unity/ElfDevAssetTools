using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PackageAsset : MonoBehaviour
{
    private static Texture bigFatNope_;
    public static Texture bigFatNope => bigFatNope_ ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unitysupportuk.assettools/Editor/Icons/BigFatNope.psd") as Texture;

    private static Texture iconUp_;
    public static Texture iconUp => iconUp_ ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unitysupportuk.assettools/Editor/Icons/sort_up.png") as Texture;

    private static Texture iconDown_;
    public static Texture iconDown => iconDown_ ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unitysupportuk.assettools/Editor/Icons/sort_down.png") as Texture;

    private static Texture iconOpen_;
    public static Texture iconOpen => iconOpen_ ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unitysupportuk.assettools/Editor/Icons/foldout_open.png") as Texture;
    
    private static Texture iconClosed_;
    public static Texture iconClosed => iconClosed_ ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unitysupportuk.assettools/Editor/Icons/foldout_closed.png") as Texture;
    
    private static GUISkin elfSkin_;
    public static GUISkin elfSkin => elfSkin_ ??= AssetDatabase.LoadAssetAtPath<GUISkin>("Packages/com.unitysupportuk.assettools/Editor/Elf.guiskin") as GUISkin;
}

