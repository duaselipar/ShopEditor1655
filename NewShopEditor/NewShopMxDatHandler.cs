using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewShopEditor
{
    /// <summary>
    /// Reader/Writer untuk NewShopMx.dat (Eudemons).
    /// File disusun sebagai berulang: [int32 length][bytes XOR key].
    /// Key XOR: "dhhjiami".
    /// </summary>
    internal static class NewShopMxDatHandler
    {
        private static readonly byte[] key = Encoding.ASCII.GetBytes("dhhjiami");

        /// <summary>
        /// Decrypt & load semua line dari NewShopMx.dat
        /// </summary>
        public static List<string> Read(string path)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return lines;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    if (br.BaseStream.Length - br.BaseStream.Position < 4)
                        break;

                    int length = br.ReadInt32();
                    if (length <= 0) continue;

                    long remain = br.BaseStream.Length - br.BaseStream.Position;
                    if (length > remain) length = (int)remain;

                    byte[] buffer = br.ReadBytes(length);
                    for (int i = 0; i < buffer.Length; i++)
                        buffer[i] ^= key[i % key.Length];

                    string line = Encoding.UTF8.GetString(buffer).TrimEnd('\0', '\r', '\n');
                    if (line.Length > 0)
                        lines.Add(line);
                }
            }
            return lines;
        }

        /// <summary>
        /// Encrypt & save senarai line ke NewShopMx.dat
        /// </summary>
        public static void Write(string path, List<string> lines)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Invalid path", nameof(path));
            if (lines == null) lines = new List<string>();

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                foreach (var rawLine in lines)
                {
                    var line = rawLine ?? string.Empty;
                    byte[] buffer = Encoding.UTF8.GetBytes(line);
                    for (int i = 0; i < buffer.Length; i++)
                        buffer[i] ^= key[i % key.Length];
                    bw.Write(buffer.Length);
                    if (buffer.Length > 0)
                        bw.Write(buffer);
                }
            }
        }

        /// <summary>
        /// Encrypt & save dari struktur Dictionary → NewShopMx.dat
        /// </summary>
        public static void Write(string path, Dictionary<string, Dictionary<string, string>> data)
        {
            var lines = new List<string>();

            foreach (var section in data)
            {
                if (string.IsNullOrWhiteSpace(section.Key)) continue;
                lines.Add("[" + section.Key + "]");

                foreach (var kv in section.Value)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key))
                        lines.Add($"{kv.Key}={kv.Value}");
                }

                lines.Add(""); // kosong antara section
            }

            Write(path, lines);
        }
    }
}
