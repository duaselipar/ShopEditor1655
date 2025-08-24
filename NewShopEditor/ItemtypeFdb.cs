using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace NewShopEditor
{
    // ---- low-level FDB types/loader (read-only) ----
    internal class FdbField { public byte Type; public string Name = ""; }

    internal static class FdbLoaderEPLStyle
    {
        public static (List<FdbField> fields, List<List<object>> rows) Load(string path)
        {
            var data = File.ReadAllBytes(path);
            const int HEADER = 0x20;

            int fieldCount = BitConverter.ToInt32(data, 0x14);
            int rowCount = BitConverter.ToInt32(data, 0x18);
            int textLen = BitConverter.ToInt32(data, 0x1C);
            int textBase = data.Length - textLen;

            var gbk = Encoding.GetEncoding("GBK");

            // labels
            var labels = new List<string>(fieldCount);
            int p = textBase;
            for (int i = 0; i < fieldCount; i++)
            { int s = p; while (p < data.Length && data[p] != 0) p++; labels.Add(gbk.GetString(data, s, p - s)); p++; }

            // fields
            var fields = new List<FdbField>(fieldCount);
            for (int i = 0; i < fieldCount; i++)
            {
                int off = HEADER + i * 5;
                fields.Add(new FdbField { Type = data[off], Name = labels[i] ?? "" });
            }

            // rows
            int ptrTable = HEADER + fieldCount * 5;
            var rows = new List<List<object>>(rowCount);
            for (int r = 0; r < rowCount; r++)
            {
                int recPtr = BitConverter.ToInt32(data, ptrTable + r * 8 + 4);
                if (recPtr <= 0) { rows.Add(new List<object>(new object[fieldCount])); continue; }

                int pos = recPtr;
                var vals = new List<object>(fieldCount);
                for (int f = 0; f < fieldCount; f++)
                {
                    object v; byte t = fields[f].Type;
                    switch (t)
                    {
                        case 1: v = data[pos]; pos += 1; break;
                        case 2: v = BitConverter.ToInt16(data, pos); pos += 2; break;
                        case 3: v = (ushort)BitConverter.ToInt16(data, pos); pos += 2; break;
                        case 4: v = BitConverter.ToInt32(data, pos); pos += 4; break;
                        case 5: v = (uint)BitConverter.ToInt32(data, pos); pos += 4; break;
                        case 6: v = BitConverter.ToSingle(data, pos); pos += 4; break;
                        case 7: v = BitConverter.ToDouble(data, pos); pos += 8; break;
                        case 8: v = BitConverter.ToInt64(data, pos); pos += 8; break;
                        case 9: v = (ulong)BitConverter.ToInt64(data, pos); pos += 8; break;
                        case 10:
                            int strPtr = BitConverter.ToInt32(data, pos);
                            int addr = textBase + strPtr; v = "";
                            if (addr >= 0 && addr < data.Length)
                            { int e = addr; while (e < data.Length && data[e] != 0) e++; v = gbk.GetString(data, addr, e - addr); }
                            pos += 4; break;
                        default: v = ""; break;
                    }
                    vals.Add(v);
                }
                rows.Add(vals);
            }
            return (fields, rows);
        }
    }

    // ---- single shared cache for whole app ----
    public sealed class ItemtypeCache
    {
        public Dictionary<uint, (string Name, int Gold, int Emoney)> ById { get; private set; } = new();
        public Dictionary<uint, (string Name, int Gold, int Emoney)> ByType { get; private set; } = new(); // key=Type if exists, else uID
        public DataTable GridTable { get; private set; } = new(); // ID/Name/Gold/Emoney for NewShopItemForm

        public static ItemtypeCache Load(string fdbPath)
        {
            var cache = new ItemtypeCache();

            List<FdbField> fields; List<List<object>> rows;
            (fields, rows) = FdbLoaderEPLStyle.Load(fdbPath);

            // strict columns we use
            int idx_uID = FindExact(fields, "uID");
            int idx_szName = FindExact(fields, "szName");
            int idx_uPrice = FindExact(fields, "uPrice");
            int idx_uEPrice = FindExact(fields, "uEPrice");
            if (idx_uID < 0 || idx_szName < 0 || idx_uPrice < 0 || idx_uEPrice < 0)
                throw new Exception("itemtype.fdb missing required fields (uID/szName/uPrice/uEPrice).");

            int idx_Type = FindExact(fields, "Type"); // optional

            // build dictionaries + datatable once
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(uint));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Gold", typeof(int));
            dt.Columns.Add("Emoney", typeof(int));

            for (int r = 0; r < rows.Count; r++)
            {
                uint id = ToUInt(rows[r][idx_uID]);
                if (id == 0) continue;

                string name = rows[r][idx_szName]?.ToString() ?? "";
                int gold = ToInt(rows[r][idx_uPrice]);
                int ep = ToInt(rows[r][idx_uEPrice]);

                cache.ById[id] = (name, gold, ep);
                uint typeKey = (idx_Type >= 0) ? ToUInt(rows[r][idx_Type]) : id;
                if (typeKey != 0) cache.ByType[typeKey] = (name, gold, ep);

                dt.Rows.Add(id, name, gold, ep);
            }

            cache.GridTable = dt;
            return cache;
        }

        private static int FindExact(List<FdbField> f, string name)
        {
            for (int i = 0; i < f.Count; i++)
                if (string.Equals(f[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static uint ToUInt(object v)
        { try { return Convert.ToUInt32(v); } catch { uint.TryParse(v?.ToString() ?? "0", out var o); return o; } }
        private static int ToInt(object v)
        { try { return Convert.ToInt32(v); } catch { int.TryParse(v?.ToString() ?? "0", out var o); return o; } }
    }
}
