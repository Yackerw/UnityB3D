using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BMPLoader {
	// BM, little endian
	const short header = 0x4D42;

	static private int ReadShort(FileStream fs)
	{
		byte[] bts = new byte[2];
		fs.Read(bts, 0, 2);
		int sh = bts[0];
		sh |= bts[1] << 8;
		return sh;
	}

	static private int ReadInt(FileStream fs)
	{
		byte[] bts = new byte[4];
		fs.Read(bts, 0, 4);
		int it = bts[0];
		it |= bts[1] << 8;
		it |= bts[2] << 16;
		it |= bts[3] << 24;

		return it;
	}

	static private uint ReadUInt(FileStream fs)
	{
		byte[] bts = new byte[4];
		fs.Read(bts, 0, 4);
		uint it = bts[0];
		it |= (uint)bts[1] << 8;
		it |= (uint)bts[2] << 16;
		it |= (uint)bts[3] << 24;

		return it;
	}

	private static int GetShift(uint mask)
	{
		for (int i = 0; i < 32; ++i)
		{
			if ((mask & 0x01) > 0)
				return i;
			mask >>= 1;
		}
		return 0;
	}

	static public Texture2D Load(string dir)
	{
		FileStream fs = Utilj.FileOpen(dir, FileMode.Open, FileAccess.Read);
		if (fs == null) return null;
		if (fs.Length < 54)
		{
			fs.Close();
			return null;
		}
		// check header for BM
		int hd = ReadShort(fs);
		if (hd != header)
		{
			fs.Close();
			return null;
		}

		// check buffer size from file; if it doesn't match the actual file size, then we can it
		int fSize = ReadInt(fs);
		if (fSize > fs.Length)
		{
			fs.Close();
			return null;
		}

		// unused reserved bytes
		fs.Seek(4, SeekOrigin.Current);

		// offset to pixel data
		//int pixOffs = ReadInt(fs);
		fs.Seek(4, SeekOrigin.Current);

		// header size; if this isn't 40, then again, can it
		int headerSize = ReadInt(fs);
		if (headerSize != 40)
		{
			fs.Close();
			return null;
		}
		// finally, useful stuff
		int width = ReadInt(fs);
		int height = ReadInt(fs);
		// "number of image planes" always 1
		fs.Seek(2, SeekOrigin.Current);
		int bitCount = ReadShort(fs);
		if (bitCount != 1 && bitCount != 4 && bitCount != 8 && bitCount != 16 && bitCount != 24 && bitCount != 32)
		{
			fs.Close();
			return null;
		}
		// todo: implement these?
		//int compression = ReadInt(fs);
		//int compSize = ReadInt(fs);
		fs.Seek(8, SeekOrigin.Current);
		// worthless printing information
		fs.Seek(8, SeekOrigin.Current);
		int colorsUsed = ReadInt(fs);
		// not important
		fs.Seek(4, SeekOrigin.Current);

		// now we load color data
		if (bitCount <= 8)
		{
			int colorTableSize;
			if (colorsUsed != 0)
			{
				colorTableSize = colorsUsed;
			} else
			{
				colorTableSize = (int)Mathf.Pow(2, bitCount);
			}
			//fs.Seek(pixOffs, SeekOrigin.Begin);
			Color32[] colors = LoadColors(fs, colorTableSize);
			int textSize = width * height;
			Color32[] textColors = new Color32[textSize];
			//int scanSize = (width * 8) / bitCount;
			// determine how many dead bytes we have at the end of each scanline, as bmps are stored to align each scanline to 4 bytes
			int dead = (4 - ((int)Mathf.Ceil((float)width / (float)(8 / bitCount)) % 4)) % 4;
			switch (bitCount)
			{
				case 1:
					{
						int colorPos = 0;
						for (int i = 0; i < height; ++i)
						{
							byte indexes = 0;
							// read the 1 bit indexes by just bit shifting them over
							for (int i2 = 0; i2 < width; ++i2)
							{
								if (i2 % 8 == 0) indexes = (byte)fs.ReadByte();
								textColors[colorPos] = colors[(indexes >> i2 % 8) & 1];
								++colorPos;
							}
							fs.Seek(dead, SeekOrigin.Current);
						}
					}
					break;
				case 4:
					{
						int colorPos = 0;
						for (int i = 0; i < height; ++i)
						{
							byte indexes = 0;
							// read the 4 bit indexes similarly to the 1 bit ones
							for (int i2 = 0; i2 < width; ++i2)
							{
								if (i2 % 2 == 0) indexes = (byte)fs.ReadByte();
								textColors[colorPos] = colors[(indexes >> ((i2 % 8) * 4)) & 15];
								++colorPos;
							}
							fs.Seek(dead, SeekOrigin.Current);
						}
					}
					break;
				case 8:
					{
						int colorPos = 0;
						for (int i = 0; i < height; ++i)
						{
							// 8 bit depth, so we're just reading straight bytes
							for (int i2 = 0; i2 < width; ++i2)
							{
								textColors[colorPos] = colors[fs.ReadByte()];
								++colorPos;
							}
							fs.Seek(dead, SeekOrigin.Current);
						}
					}
					break;
			}
			fs.Close();
			Texture2D texture = new Texture2D(width, height);
			texture.SetPixels32(textColors);
			texture.Apply();
			return texture;
		}
		// now check for other colors
		switch (bitCount)
		{
			// todo
			case 16:
				{
					uint rMask = ReadUInt(fs);
					uint gMask = ReadUInt(fs);
					uint bMask = ReadUInt(fs);
					int rShift = GetShift(rMask);
					int gShift = GetShift(gMask);
					int bShift = GetShift(bMask);
					Color32[] color = new Color32[width * height];
					int dead = width * height * 2;
					dead = (4 - (dead % 4)) % 4;
					int colorPos = 0;
					for (int i = 0; i < height; ++i)
					{
						for (int i2 = 0; i2 < width; ++i2)
						{
							int bts = ReadShort(fs);
							color[colorPos] = new Color32((byte)((bts & rMask) >> rShift), (byte)((bts & gMask) >> gShift), (byte)((bts & bMask) >> bShift), 255);
							++colorPos;
						}
						fs.Seek(dead, SeekOrigin.Current);
					}
					Texture2D texture = new Texture2D(width, height);
					texture.SetPixels32(color);
					texture.Apply();
					fs.Close();
					return texture;
				}
			case 24:
				{
					// simple RGB
					Color32[] color = new Color32[width * height];
					int dead = width * height * 3;
					dead = (4 - (dead % 4)) % 4;
					int colorPos = 0;
					for (int i = 0; i < height; ++i)
					{
						for (int i2 = 0; i2 < width; ++i2)
						{
							byte[] bts = new byte[3];
							fs.Read(bts, 0, 3);
							color[colorPos] = new Color32(bts[2], bts[1], bts[0], 255);
							++colorPos;
						}
						fs.Seek(dead, SeekOrigin.Current);
					}
					Texture2D texture = new Texture2D(width, height);
					texture.SetPixels32(color);
					texture.Apply();
					fs.Close();
					return texture;
				}
				// todo
			case 32:
				{
					uint rMask = ReadUInt(fs);
					uint gMask = ReadUInt(fs);
					uint bMask = ReadUInt(fs);
					int rShift = GetShift(rMask);
					int gShift = GetShift(gMask);
					int bShift = GetShift(bMask);
					Color32[] color = new Color32[width * height];
					int colorPos = 0;
					for (int i = 0; i < height; ++i)
					{
						for (int i2 = 0; i2 < width; ++i2)
						{
							uint bts = ReadUInt(fs);
							color[colorPos] = new Color((float)((bts & rMask) >> rShift) / (float)(rMask >> rShift), (float)((bts & gMask) >> gShift) / (float)(gMask >> gShift), (float)((bts & bMask) >> bShift) / (float)(bMask >> bShift), 255);
							++colorPos;
						}
					}
					Texture2D texture = new Texture2D(width, height);
					texture.SetPixels32(color);
					texture.Apply();
					fs.Close();
					return texture;
				}
		}
		return null;
	}

	static private Color32[] LoadColors(FileStream fs, int count)
	{
		Color32[] colors = new Color32[count];
		for (int i = 0; i < count; ++i)
		{
			byte[] bts = new byte[3];
			fs.Read(bts, 0, 3);
			colors[i] = new Color32(bts[2], bts[1], bts[0], 255);
			fs.Seek(1, SeekOrigin.Current);
		}
		return colors;
	}
}
