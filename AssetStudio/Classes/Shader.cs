using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Lz4;

namespace AssetStudio
{
    public struct ShaderData
    {
        public bool isSuccess;
        public ShaderPlatform[] platforms;
        public int[] offset;
        public int[] compressedLengths;
        public int[] decompressedLengths;
        public byte[] compressedBlob;
        public byte[] m_Script;
    }

    public enum ShaderPlatform
    {
        NotSupport = 0,
        OpenglES2 = 5,
        OpenglES3 = 9,
        Opengl = 15
    }

    public sealed class Shader : NamedObject
    {
        public byte[] m_Script{ get { return shaderData.m_Script; } set { shaderData.m_Script = value; } }
        public ShaderData shaderData = new ShaderData();
        public List<KeyValuePair<ShaderPlatform, byte[]>> ShaderText = new List<KeyValuePair<ShaderPlatform, byte[]>>();

        public Shader(AssetPreloadData preloadData,bool export) : base(preloadData)
        {
            if (sourceFile.version[0] == 5 && sourceFile.version[1] >= 5 || sourceFile.version[0] > 5) //5.5.0 and up
            {
                if (export)
                {
                    shaderData = preloadData.DumpShader();
                    if (shaderData.isSuccess == false) shaderData.m_Script = System.Text.Encoding.ASCII.GetBytes("Can't read Shader!");
                }
                else
                {
                    string str = preloadData.Dump();
                    m_Script = Encoding.Default.GetBytes(str ?? "can't read shader!");
                }
                
            }
            else
            {
                m_Script = reader.ReadBytes(reader.ReadInt32());
                if (sourceFile.version[0] == 5 && sourceFile.version[1] >= 3) //5.3 - 5.4
                {
                    reader.AlignStream(4);
                    var m_PathName = reader.ReadAlignedString();
                    var decompressedSize = reader.ReadUInt32();
                    var m_SubProgramBlob = reader.ReadBytes(reader.ReadInt32());
                    var decompressedBytes = new byte[decompressedSize];
                    using (var decoder = new Lz4DecoderStream(new MemoryStream(m_SubProgramBlob)))
                    {
                        decoder.Read(decompressedBytes, 0, (int)decompressedSize);
                    }

                    m_Script = m_Script.Concat(decompressedBytes.ToArray()).ToArray();
                }
            }
            if(export)processShaderText();
        }

        private void processShaderText()
        {
            if(shaderData.isSuccess)
            {

                for (int index = 0; index < shaderData.platforms.Length; index++)
                {
                    if (shaderData.platforms[index] == ShaderPlatform.NotSupport) continue;
                    //解压数据
                    byte[] decompresseddata = new byte[shaderData.decompressedLengths[index]];
                    byte[] cdata = shaderData.compressedBlob.Skip(shaderData.offset[index]).Take(shaderData.compressedLengths[index]).ToArray();
                    Lz4DecoderStream decoder = new Lz4DecoderStream(new MemoryStream(cdata));
                    byte[] dedata = new byte[shaderData.decompressedLengths[index]];
                    decoder.Read(dedata, 0, dedata.Length);
                    //整理文本
                    MemoryStream ms = new MemoryStream(dedata);
                    BinaryReader br = new BinaryReader(ms);
                    int programNum = br.ReadInt32();
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < programNum; i++)
                    {

                        int offset = br.ReadInt32();
                        int length = br.ReadInt32();
                        long tp = br.BaseStream.Position;
                        br.BaseStream.Position = offset + 24;
                        int strnum = br.ReadInt32();
                        sb.Append("\n[" + i.ToString() + "]\n\t");

                        for (int j = 0; j < strnum; j++)
                        {
                            int strlen = br.ReadInt32();
                            sb.Append(Encoding.Default.GetString(br.ReadBytes(strlen)) + "  ");
                            br.AlignStream(4);
                        }

                        int shaderlen = br.ReadInt32();
                        if (shaderlen != 0)
                        {
                            sb.Append("\n\n" + Encoding.Default.GetString(br.ReadBytes(shaderlen)) + "\n\n");
                            br.AlignStream(4);
                        }

                        br.BaseStream.Position = tp;

                    }
                    ShaderText.Add(new KeyValuePair<ShaderPlatform, byte[]>(shaderData.platforms[index], Encoding.Default.GetBytes(sb.ToString())));

                }
            }
        }
    }
}
