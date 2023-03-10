using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Linq;


namespace ElfDev
{
    public class Formatting
    {
        static public string ByteClumpedString(long sz) 
        {
            float sizeKB = ((float)sz / 1024f);
            float sizeMB = (sizeKB / 1024f);
            return (sizeMB > 1f) ? sizeMB.ToString("0.00") + "MB" : sizeKB.ToString("0.0") + "KB";
        }
    }
}


