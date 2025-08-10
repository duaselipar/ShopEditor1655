using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewShopEditor
{
    public class ShopInfo
    {
        public uint ShopID;
        public string Name;
        public uint Type;
        public List<uint> ItemIDs = new List<uint>();
        public List<uint> Reserved1 = new List<uint>();
        public List<uint> Reserved2 = new List<uint>();
    }

    public static class ShopDatHandler
    {
        public static List<byte[]> rawVIPShopBytes = new List<byte[]>();
        public static List<uint> hiddenShopIDs = new List<uint>();

        // ==================== LOAD ====================
        public static List<ShopInfo> Read(string path)
        {
            var shops = new List<ShopInfo>();
            rawVIPShopBytes.Clear();
            hiddenShopIDs.Clear();

            if (!File.Exists(path))
                return shops;

            byte[] data = File.ReadAllBytes(path);
            int pos = 0;
            if (data.Length < 4) return shops;
            int npcCount = BitConverter.ToInt32(data, pos); pos += 4;
            for (int n = 0; n < npcCount; n++)
            {
                if (pos + 28 > data.Length) break;
                int curPos = pos;
                uint shopId = BitConverter.ToUInt32(data, pos);
                uint type = BitConverter.ToUInt32(data, pos + 20);

                // VIP = ShopID 1207 atau Type 31~199
                bool isVIP = (shopId == 1207) || (type > 30 && type < 200);

                if (isVIP)
                {
                    hiddenShopIDs.Add(shopId);

                    pos += 4;  // ShopID
                    pos += 16; // Name
                    pos += 4;  // Type
                    uint itemCount = BitConverter.ToUInt32(data, pos); pos += 4;
                    for (int i = 0; i < itemCount; i++)
                    {
                        pos += 4;   // ItemID
                        pos += 456; // reserved
                    }
                    byte[] vipBytes = new byte[pos - curPos];
                    Array.Copy(data, curPos, vipBytes, 0, pos - curPos);
                    rawVIPShopBytes.Add(vipBytes);
                    continue;
                }
                // --- Normal shop ---
                var shop = new ShopInfo();
                shop.ShopID = shopId;
                pos += 4;
                string rawName = Encoding.GetEncoding("GB2312").GetString(data, pos, 16);
                shop.Name = rawName.Split('\0')[0];
                pos += 16;
                shop.Type = BitConverter.ToUInt32(data, pos); pos += 4;
                uint itemCount2 = BitConverter.ToUInt32(data, pos); pos += 4;
                shop.ItemIDs = new List<uint>();
                shop.Reserved1 = new List<uint>();
                shop.Reserved2 = new List<uint>();
                for (int i = 0; i < itemCount2; i++)
                {
                    if (pos + 4 > data.Length) break;
                    uint itemId = BitConverter.ToUInt32(data, pos); pos += 4;
                    shop.ItemIDs.Add(itemId);
                }
                for (int i = 0; i < itemCount2; i++)
                {
                    if (pos + 4 > data.Length) break;
                    shop.Reserved1.Add(BitConverter.ToUInt32(data, pos)); pos += 4;
                }
                for (int i = 0; i < itemCount2; i++)
                {
                    if (pos + 4 > data.Length) break;
                    shop.Reserved2.Add(BitConverter.ToUInt32(data, pos)); pos += 4;
                }
                shops.Add(shop);
            }
            return shops;
        }

        // ==================== SAVE ====================
        public static void Save(string path, List<ShopInfo> shops)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // npcCount = normal + VIP shop
                bw.Write((int)(shops.Count + rawVIPShopBytes.Count));

                // VIP shop (untouched, original byte)
                foreach (var vipBytes in rawVIPShopBytes)
                    bw.Write(vipBytes);

                // Normal shop
                foreach (var shop in shops)
                {
                    bw.Write(shop.ShopID);
                    byte[] nameBytes = new byte[16];
                    var raw = Encoding.GetEncoding("GB2312").GetBytes(shop.Name ?? "");
                    Array.Copy(raw, nameBytes, Math.Min(raw.Length, 16));
                    bw.Write(nameBytes);
                    bw.Write(shop.Type);
                    bw.Write((uint)shop.ItemIDs.Count);
                    for (int i = 0; i < shop.ItemIDs.Count; i++)
                        bw.Write(shop.ItemIDs[i]);
                    for (int i = 0; i < shop.ItemIDs.Count; i++)
                        bw.Write((shop.Reserved1 != null && shop.Reserved1.Count > i) ? shop.Reserved1[i] : 0U);
                    for (int i = 0; i < shop.ItemIDs.Count; i++)
                        bw.Write((shop.Reserved2 != null && shop.Reserved2.Count > i) ? shop.Reserved2[i] : 0U);
                }
                File.WriteAllBytes(path, ms.ToArray());
            }
        }
    }
}
