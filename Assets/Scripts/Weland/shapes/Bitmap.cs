/*
 * This file is a modified version of Bitmap from Weland.
 * Modifications made by huriettic on 2026-03-15.
 * Original code licensed under the GPL.
 */

using System.IO;

namespace Weland
{
    enum BitmapFlags : ushort
    {
        ColumnOrder = 0x8000
    }

    class Bitmap
    {
        public short Width;
        public short Height;
        short bytesPerRow;

        BitmapFlags flags;
        public bool ColumnOrder
        {
            get { return (flags & BitmapFlags.ColumnOrder) != 0; }
        }

        public short BitDepth;

        byte[] data;
        public byte[] Data { get { return data; } }

        public void Load(BinaryReaderBE reader)
        {
            Width = reader.ReadInt16();
            Height = reader.ReadInt16();
            bytesPerRow = reader.ReadInt16();
            flags = (BitmapFlags)reader.ReadUInt16();
            BitDepth = reader.ReadInt16();

            int scanlines = ColumnOrder ? Width : Height;
            reader.BaseStream.Seek(20 + scanlines * 4, SeekOrigin.Current);

            data = new byte[Width * Height];

            if (bytesPerRow > -1)
            {
                if (!ColumnOrder)
                {
                    reader.Read(data, 0, Width * Height);
                }
                else
                {
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = Height - 1; y >= 0; y--)
                        {
                            data[y * Width + x] = reader.ReadByte();
                        }
                    }
                }
            }
            else
            {
                for (int x = 0; x < Width; x++)
                {
                    short start = reader.ReadInt16();
                    short end = reader.ReadInt16();

                    for (int y = start; y < end; y++)
                    {
                        data[y * Width + x] = reader.ReadByte();
                    }
                }
            }
        }
    }
}