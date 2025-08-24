using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewShopEditor
{
    internal static class FdbItemtypePatcher
    {
        // Update only uPrice (Gold) & uEPrice (Emoney) in-place (keep structure/text pool)
        public static void UpdatePrices(string fdbPath, Dictionary<uint, (int Gold, int Emoney)> updates)
        {
            if (string.IsNullOrEmpty(fdbPath) || !File.Exists(fdbPath)) throw new FileNotFoundException("itemtype.fdb not found", fdbPath);
            if (updates == null || updates.Count == 0) return;

            byte[] data = File.ReadAllBytes(fdbPath);
            const int HEADER = 0x20;

            int fieldCount = BitConverter.ToInt32(data, 0x14);
            int rowCount = BitConverter.ToInt32(data, 0x18);
            int textLen = BitConverter.ToInt32(data, 0x1C);
            int textBase = data.Length - textLen;

            // read labels (GBK)
            var gbk = Encoding.GetEncoding("GBK");
            string[] labels = new string[fieldCount];
            int p = textBase;
            for (int i = 0; i < fieldCount; i++)
            {
                int s = p; while (p < data.Length && data[p] != 0) p++;
                labels[i] = gbk.GetString(data, s, p - s);
                p++;
            }

            // field types
            byte[] types = new byte[fieldCount];
            for (int i = 0; i < fieldCount; i++)
                types[i] = data[HEADER + i * 5];

            // find exact columns
            int cId = IndexOf(labels, "uID");
            int cGold = IndexOf(labels, "uPrice");
            int cEP = IndexOf(labels, "uEPrice");
            if (cId < 0 || cGold < 0 || cEP < 0)
                throw new Exception("itemtype.fdb must contain uID, uPrice, uEPrice.");

            // quick size table per type
            int SizeOf(byte t) => t switch
            {
                1 => 1,              // byte
                2 or 3 => 2,         // short/ushort
                4 or 5 or 6 or 10 => 4, // int/uint/float/ptr
                7 or 8 or 9 => 8,    // double/int64/uint64
                _ => 4
            };

            int ptrTable = HEADER + fieldCount * 5;

            // iterate rows
            for (int r = 0; r < rowCount; r++)
            {
                int recPtr = BitConverter.ToInt32(data, ptrTable + r * 8 + 4);
                if (recPtr <= 0) continue; // empty row

                int pos = recPtr;
                int posId = -1, posGold = -1, posEP = -1;

                for (int f = 0; f < fieldCount; f++)
                {
                    byte t = types[f];

                    if (f == cId) posId = pos;
                    if (f == cGold) posGold = pos;
                    if (f == cEP) posEP = pos;

                    // advance
                    if (t == 10)
                        pos += 4;              // text pointer
                    else
                        pos += SizeOf(t);
                }

                if (posId < 0) continue;

                uint id = (uint)BitConverter.ToInt32(data, posId);
                if (!updates.TryGetValue(id, out var val)) continue;

                // write Gold
                if (posGold >= 0)
                {
                    byte[] g = BitConverter.GetBytes(val.Gold);
                    Buffer.BlockCopy(g, 0, data, posGold, 4);
                }
                // write Emoney
                if (posEP >= 0)
                {
                    byte[] e = BitConverter.GetBytes(val.Emoney);
                    Buffer.BlockCopy(e, 0, data, posEP, 4);
                }
            }

            // backup once
            string bak = fdbPath + ".bak";
            try { if (!File.Exists(bak)) File.Copy(fdbPath, bak, false); } catch { /* ignore */ }

            // write back
            File.WriteAllBytes(fdbPath, data);
        }

        private static int IndexOf(string[] labels, string name)
        {
            for (int i = 0; i < labels.Length; i++)
                if (string.Equals(labels[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
    }
}
