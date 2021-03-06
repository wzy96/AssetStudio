﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public static class BinaryWriterExtensions
    {
        private static void WriteArray<T>(Action<T> del, T[] array)
        {
            foreach (var item in array)
            {
                del(item);
            }
        }

        public static void Write(this BinaryWriter writer, uint[] array)
        {
            WriteArray(writer.Write, array);
        }

        public static void AlignStream(this BinaryWriter writer, int alignment)
        {
            var pos = writer.BaseStream.Position;
            var mod = pos % alignment;
            if (mod != 0)
            {
                writer.Write(new byte[alignment - mod]);
            }
        }

        public static void WriteAlignedString(this BinaryWriter writer, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            writer.AlignStream(4);
        }
    }
}
