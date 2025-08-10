using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewShopEditor
{
    internal static class NewShopDatHandler
    {
        private static readonly byte[] key = Encoding.ASCII.GetBytes("zxcvbnmq");

        /// <summary>
        /// Baca dan decrypt NewShop.dat → senarai line text (untuk parse ke NewShopEntry)
        /// </summary>
        public static List<string> Read(string path)
        {
            var lines = new List<string>();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    int length = br.ReadInt32();
                    byte[] buffer = br.ReadBytes(length);
                    for (int i = 0; i < buffer.Length; i++)
                        buffer[i] ^= key[i % key.Length];
                    string line = Encoding.UTF8.GetString(buffer);
                    lines.Add(line.TrimEnd('\0', '\r', '\n'));
                }
            }

            return lines;
        }

        /// <summary>
        /// Simpan balik senarai line text → NewShop.dat (encrypt)
        /// </summary>
        public static void Write(string path, List<string> lines)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                foreach (var line in lines)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(line);
                    for (int i = 0; i < buffer.Length; i++)
                        buffer[i] ^= key[i % key.Length];
                    bw.Write(buffer.Length);
                    bw.Write(buffer);
                }
            }
        }
    }
}
