﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;

namespace AssetStudio
{
    public class AssetPreloadData : ListViewItem
    {
        public long m_PathID;
        public uint Offset;
        public int Size;
        public ClassIDReference Type;
        public int Type1;
        public int Type2;

        public string TypeString;
        public int fullSize;
        public string InfoText;

        public AssetsFile sourceFile;
        public GameObject gameObject;
        public string uniqueID;

        public EndianBinaryReader InitReader()
        {
            var reader = sourceFile.reader;
            reader.Position = Offset;
            return reader;
        }

        public string Dump()
        {
            var reader = InitReader();

            if (sourceFile.m_Type.TryGetValue(Type1, out var typeTreeList))
            {
                var sb = new StringBuilder();
                TypeTreeHelper.ReadTypeString(sb, typeTreeList, reader);
                return sb.ToString();
            }
            return null;
        }

        public ShaderData DumpShader()
        {
            var reader = InitReader();
            ShaderData sd = new ShaderData();
            sd.isSuccess = false;
            try
            {
                //check & load typetree
                if (Studio.typeTreeList.Count == 0)
                {
                    var fs = new FileStream(Application.StartupPath + @"\typetreelist.dat", FileMode.Open);
                    BinaryFormatter binary = new BinaryFormatter();
                    Studio.typeTreeList = binary.Deserialize(fs) as SortedDictionary<int, List<TypeTree>>;
                }
            }catch
            {}
            if (sourceFile.m_Type.TryGetValue(Type1, out var typeTreeList)||Studio.typeTreeList.TryGetValue(Type1,out typeTreeList))
            {
                var sb = new StringBuilder();
                sd = TypeTreeHelper.ReadShaderTypeString(sb, typeTreeList, reader);
                sd.m_Script = Encoding.Default.GetBytes(sb.ToString());
                if (sb.Length > 1) sd.isSuccess = true; else sd.isSuccess = false;
                return sd;
            }
            return sd;
        }

        public bool HasStructMember(string name)
        {
            return sourceFile.m_Type.TryGetValue(Type1, out var typeTreeList) && typeTreeList.Any(x => x.m_Name == name);
        }
    }
}
