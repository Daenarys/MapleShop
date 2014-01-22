using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapleShop
{
    public enum ParseResult
    {
        Success,
        NotFound,
        Failed
    }

    public static class Parser
    {
        public static List<Packet> ShopPackets = null;
        private static ushort mLocalPort = 0;
        private static ushort mRemotePort = 0;
        private static ushort mBuild = 0;
        private static byte mLocale = 0;

        private static string mRemoteEndpoint = "???";
        private static string mLocalEndpoint = "???";

        public static List<string> ShopQueries = null;

        public static ParseResult ParseShops(string pPath)
        {
            ShopQueries = new List<string>();
            ShopPackets = new List<Packet>();

            using (FileStream Stream = new FileStream(pPath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader Reader = new BinaryReader(Stream);
                mBuild = Reader.ReadUInt16();
                ushort version = mBuild;

                if (mBuild == 0x2012)
                {
                    mLocale = (byte)Reader.ReadUInt16();
                    mBuild = Reader.ReadUInt16();
                    mLocalPort = Reader.ReadUInt16();
                }
                else if (mBuild == 0x2014)
                {
                    mLocalEndpoint = Reader.ReadString();
                    mLocalPort = Reader.ReadUInt16();
                    mRemoteEndpoint = Reader.ReadString();
                    mRemotePort = Reader.ReadUInt16();

                    mLocale = (byte)Reader.ReadUInt16();
                    mBuild = Reader.ReadUInt16();
                }
                else if (mBuild == 0x2015 || mBuild == 0x2020)
                {
                    mLocalEndpoint = Reader.ReadString();
                    mLocalPort = Reader.ReadUInt16();
                    mRemoteEndpoint = Reader.ReadString();
                    mRemotePort = Reader.ReadUInt16();

                    mLocale = Reader.ReadByte();
                    mBuild = Reader.ReadUInt16();
                }
                else
                {
                    mLocalPort = Reader.ReadUInt16();
                }

                while (Stream.Position < Stream.Length)
                {
                    long Timestamp = Reader.ReadInt64();
                    ushort Size = Reader.ReadUInt16();
                    ushort Opcode = Reader.ReadUInt16();
                    bool Outbound;
                    if (version >= 0x2020)
                    {
                        Outbound = Reader.ReadBoolean();
                    }
                    else
                    {
                        Outbound = (Size & 0x8000) != 0;
                        Size = (ushort)(Size & 0x7FFF);
                    }

                    //                    string OpcodeName = "0x" + Opcode.ToString("X4");

                    byte[] Buffer = Reader.ReadBytes(Size);

                    if (Opcode == 0x0338)
                    {
                        Packet p = new Packet(Buffer);
                        if (!ShopPackets.Contains(p))
                            ShopPackets.Add(p);
                    }
                }
            }

            if (ShopPackets.Count == 0)
                return ParseResult.NotFound;
            else
            {
                foreach (Packet pPacket in ShopPackets)
                {
                    string shopQuery = "";

                    pPacket.ReadInt(); // Unknown
                    int shopID = pPacket.ReadInt(); // Also counts as NPC id in gms..
                    bool ranks = pPacket.ReadBool();

                    int rankSize = 0;
                    int rank = 0;
                    string rankMessage = string.Empty;

                    if (ranks)
                    {
                        rankSize = pPacket.ReadByte();
                        for (int r = 0; r < rankSize; r++)
                        {
                            rank = pPacket.ReadInt();
                            rankMessage = pPacket.ReadString();
                        }
                    }
                    short itemCount = pPacket.ReadShort();

                    shopQuery += "INSERT INTO shops(NpcID) VALUES(" + shopID + ");";

                    ShopQueries.Add(shopQuery);

                    for (int i = 1; i < itemCount; i++)
                    {
                        string query = "";

                        int itemID = pPacket.ReadInt();
                        int price = pPacket.ReadInt();
                        byte discount = pPacket.ReadByte();

                        int requiredItem = pPacket.ReadInt();
                        int requiredItemQuantity = pPacket.ReadInt();
                        int expirationTime = pPacket.ReadInt();
                        int requiredLevel = pPacket.ReadInt();
                        pPacket.ReadInt(); // ?

                        pPacket.ReadLong(); // 2079
                        pPacket.ReadLong(); // 1970
                        int category = pPacket.ReadInt();
                        pPacket.ReadBool(); // potential?

                        int expiration = pPacket.ReadInt();
                        bool unk = pPacket.ReadBool();

                        if (unk)
                        {
                            pPacket.ReadInt(); //?
                            pPacket.ReadLong();//?
                        }

                        if (itemID / 10000 == 207 || itemID / 10000 == 233)
                            pPacket.Skip(6);

                        short quantity = pPacket.ReadShort();
                        short buyable = pPacket.ReadShort();

                        pPacket.Skip(49); // red leaf and rank shit.

                        query += "INSERT INTO shop_items(ShopID, ItemID, Price, Sort, RequiredItem, RequiredItemQuantity, Rank, Quantity, Buyable, Category, MinimumLevel, Expiration) VALUES(";
                        query += shopID + ", ";
                        query += itemID + ", ";
                        query += price + ", ";
                        query += i + ", ";
                        query += requiredItem + ", ";
                        query += requiredItemQuantity + ", ";
                        query += "0, ";
                        query += quantity + ", ";
                        query += buyable + ", ";
                        query += category + ", ";
                        query += requiredLevel + ", ";
                        query += expiration + ");";

                        ShopQueries.Add(query);
                    }
                }

                return ParseResult.Success;
            }
        }
    }
}
