using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DDSLoader {

    // to ease up memory usage and GC, we reuse datare
    static byte[] datare;

    // RGBA Masks
    static uint[] A1R5G5B5_MASKS = { 0x7C00, 0x03E0, 0x001F, 0x8000 };
    static uint[] X1R5G5B5_MASKS = { 0x7C00, 0x03E0, 0x001F, 0x0000 };
    static uint[] A4R4G4B4_MASKS = { 0x0F00, 0x00F0, 0x000F, 0xF000 };
    static uint[] X4R4G4B4_MASKS = { 0x0F00, 0x00F0, 0x000F, 0x0000 };
    static uint[] R5G6B5_MASKS = { 0xF800, 0x07E0, 0x001F, 0x0000 };
    static uint[] R8G8B8_MASKS = { 0xFF0000, 0x00FF00, 0x0000FF, 0x000000 };
    static uint[] A8B8G8R8_MASKS = { 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000 };
    static uint[] X8B8G8R8_MASKS = { 0x000000FF, 0x0000FF00, 0x00FF0000, 0x00000000 };
    static uint[] A8R8G8B8_MASKS = { 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000 };
    static uint[] X8R8G8B8_MASKS = { 0x00FF0000, 0x0000FF00, 0x000000FF, 0x00000000 };

    const int DXT1 = 0x31545844;
    const int DXT2 = 0x32545844;
    const int DXT3 = 0x33545844;
    const int DXT4 = 0x34545844;
    const int DXT5 = 0x35545844;
    const int A1R5G5B5 = 1;
    const int X1R5G5B5 = 2;
    const int A4R4G4B4 = 3;
    const int X4R4G4B4 = 4;
    const int R5G6B5 = 5;
    const int R8G8B8 = 6;
    const int A8B8G8R8 = 7;
    const int X8B8G8R8 = 8;
    const int A8R8G8B8 = 9;
    const int X8R8G8B8 = 10;

    static int GetWidth(byte[] data)
    {
        return System.BitConverter.ToInt32(data, 16);
    }

    static int GetHeight(byte[] data)
    {
        return System.BitConverter.ToInt32(data, 12);
    }

    static int GetFlags(byte[] data)
    {
        return System.BitConverter.ToInt32(data, 80);
    }

    static int GetType(byte[] data)
    {
        int flags = GetFlags(data);
        if ((flags & 0x04) != 0)
        {
            // FourCC
            return System.BitConverter.ToInt32(data, 84);
        }
        // otherwise...
        int bitCount = System.BitConverter.ToInt32(data, 88);
        int redMask = System.BitConverter.ToInt32(data, 92);
        int greenMask = System.BitConverter.ToInt32(data, 96);
        int blueMask = System.BitConverter.ToInt32(data, 100);
        // check for 0x01 alpha flag
        int alphaMask = (flags & 0x01) != 0 ? System.BitConverter.ToInt32(data, 104) : 0;
        switch (bitCount)
        {
            case 16:
                {
                    if (redMask == A1R5G5B5_MASKS[0] && greenMask == A1R5G5B5_MASKS[1] && blueMask == A1R5G5B5_MASKS[2] && alphaMask == A1R5G5B5_MASKS[3])
                    {
                        // A1R5G5B5
                        return A1R5G5B5;
                    }
                    else if (redMask == X1R5G5B5_MASKS[0] && greenMask == X1R5G5B5_MASKS[1] && blueMask == X1R5G5B5_MASKS[2] && alphaMask == X1R5G5B5_MASKS[3])
                    {
                        // X1R5G5B5
                        return X1R5G5B5;
                    }
                    else if (redMask == A4R4G4B4_MASKS[0] && greenMask == A4R4G4B4_MASKS[1] && blueMask == A4R4G4B4_MASKS[2] && alphaMask == A4R4G4B4_MASKS[3])
                    {
                        // A4R4G4B4
                        return A4R4G4B4;
                    }
                    else if (redMask == X4R4G4B4_MASKS[0] && greenMask == X4R4G4B4_MASKS[1] && blueMask == X4R4G4B4_MASKS[2] && alphaMask == X4R4G4B4_MASKS[3])
                    {
                        // X4R4G4B4
                        return X4R4G4B4;
                    }
                    else if (redMask == R5G6B5_MASKS[0] && greenMask == R5G6B5_MASKS[1] && blueMask == R5G6B5_MASKS[2] && alphaMask == R5G6B5_MASKS[3])
                    {
                        // R5G6B5
                        return R5G6B5;
                    }
                    else
                    {
                        return -1;
                        // bad
                    }
                }
            case 24:
                {
                    if (redMask == R8G8B8_MASKS[0] && greenMask == R8G8B8_MASKS[1] && blueMask == R8G8B8_MASKS[2] && alphaMask == R8G8B8_MASKS[3])
                    {
                        // R8G8B8
                        return R8G8B8;
                    }
                    else
                    {
                        return -1;
                        // bad
                    }
                }
            case 32:
                {
                    if (redMask == A8B8G8R8_MASKS[0] && greenMask == A8B8G8R8_MASKS[1] && blueMask == A8B8G8R8_MASKS[2] && alphaMask == A8B8G8R8_MASKS[3])
                    {
                        // A8B8G8R8
                        return A8B8G8R8;
                    }
                    else if (redMask == X8B8G8R8_MASKS[0] && greenMask == X8B8G8R8_MASKS[1] && blueMask == X8B8G8R8_MASKS[2] && alphaMask == X8B8G8R8_MASKS[3])
                    {
                        // X8B8G8R8
                        return X8B8G8R8;
                    }
                    else if (redMask == A8R8G8B8_MASKS[0] && greenMask == A8R8G8B8_MASKS[1] && blueMask == A8R8G8B8_MASKS[2] && alphaMask == A8R8G8B8_MASKS[3])
                    {
                        // A8R8G8B8
                        return A8R8G8B8;
                    }
                    else if (redMask == X8R8G8B8_MASKS[0] && greenMask == X8R8G8B8_MASKS[1] && blueMask == X8R8G8B8_MASKS[2] && alphaMask == X8R8G8B8_MASKS[3])
                    {
                        // X8R8G8B8
                        return X8R8G8B8;
                    }
                    else
                    {
                        return -1;
                        // bad
                    }
                }
        }
        return -1;
    }

    static int Bit5Convert(int input)
    {
        float col = input;
        col = Mathf.Min(col / 31, 1) * 255;
        return (int)col;
    }

    static int Bit6Convert(int input)
    {
        float col = input;
        col = Mathf.Min(col / 63, 1) * 255;
        return (int)col;
    }

    // converts 8 bit color to 4 bit color
    static int Color4bit(int input)
    {
        return input >> 4;
    }

    static Color32 GetDXTColor1(int c0, int a)
    {
        Color32 col = new Color32();
        col.a = (byte)a;
        col.r = (byte)Bit5Convert((c0 & 0xFC00) >> 11);
        col.g = (byte)Bit6Convert((c0 & 0x07E0) >> 5);
        col.b = (byte)Bit5Convert(c0 & 0x001F);
        return col;
    }

    static Color32 GetDXTColor2(int c0, int c1, int a)
    {
        Color32 col = new Color32();
        col.r = (byte)((2 * Bit5Convert((c0 & 0xFC00) >> 11) + Bit5Convert((c1 & 0xFC00) >> 11)) / 3);
        col.g = (byte)((2 * Bit6Convert((c0 & 0x07E0) >> 5) + Bit5Convert((c1 & 0x07E0) >> 5)) / 3);
        col.b = (byte)((2 * Bit5Convert(c0 & 0x001F) + Bit5Convert(c1 & 0x001F)) / 3);
        col.a = (byte)a;
        return col;
    }

    static Color32 GetDXTColor3(int c0, int c1, int a)
    {
        Color32 col = new Color32();
        col.r = (byte)((Bit5Convert((c0 & 0xFC00) >> 11) + Bit5Convert((c1 & 0xFC00) >> 11)) / 2);
        col.g = (byte)((Bit6Convert((c0 & 0x07E0) >> 5) + Bit5Convert((c1 & 0x07E0) >> 5)) / 2);
        col.b = (byte)((Bit5Convert(c0 & 0x001F) + Bit5Convert(c1 & 0x001F)) / 2);
        col.a = (byte)a;
        return col;
    }

    static Color32 GetDXTColor(int c0, int c1, int a, int t)
    {
        switch (t)
        {
            case 0:
                {
                    return GetDXTColor1(c0, a);
                }
            case 1:
                {
                    return GetDXTColor1(c1, a);
                }
            case 2:
                {
                    return c0 > c1 ? GetDXTColor2(c0, c1, a) : GetDXTColor3(c0, c1, a);
                }
            case 3:
                {
                    return c0 > c1 ? GetDXTColor2(c1, c0, a) : new Color32(0, 0, 0, 0);
                }
        }
        return new Color32(0, 0, 0, 0);
    }

    static void DecodeDXT1(int width, int height, Texture2D tex, byte[] data)
    {
        Color32[] pixels = new Color32[width * height];
        int index = 128;
        // calculate number of rows and columns, aligned to 4. mandatory for dds format
        int w = (width + 3) / 4;
        int h = (height + 3) / 4;
        for (int i = 0; i < h; i++)
        {
            for (int i2 = 0; i2 < w; i2++)
            {
                // read color0 and color1, which are both 16 bit (r5, g6, b5) colors
                int c0 = data[index] | (data[index + 1] << 8);
                index += 2;
                int c1 = data[index] | (data[index + 1] << 8);
                index += 2;

                for (int i3 = 0; i3 < 4; i3++)
                {
                    // 4 more bytes telling us what color mixing method to use for each pixel
                    if (4 * i + i3 >= height) break;
                    int t0 = (data[index] & 0x03);
                    int t1 = (data[index] & 0x0C) >> 2;
                    int t2 = (data[index] & 0x30) >> 4;
                    int t3 = (data[index++] & 0xC0) >> 6;
                    // generate colors from the functions
                    pixels[4 * width * i + 4 * i2 + width * i3] = GetDXTColor(c0, c1, 255, t0);
                    if (4 * i2 + 1 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 1] = GetDXTColor(c0, c1, 255, t1);
                    if (4 * i2 + 2 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 2] = GetDXTColor(c0, c1, 255, t2);
                    if (4 * i2 + 3 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 3] = GetDXTColor(c0, c1, 255, t3);
                }
            }
        }
        tex.SetPixels32(pixels);
        // we now have our colors, let's convert them to rgba4444
        /*ushort[] bit16col = new ushort[width * height];
        for (int i = 0; i < width * height; i++)
        {
            bit16col[i] = (ushort)((Color4bit(pixels[i].r) << 12) | (Color4bit(pixels[i].g) << 8) | (Color4bit(pixels[i].b) << 4) | (Color4bit(pixels[i].a)));
        }
        byte[] pntr = new byte[width * height * 2];
        System.Buffer.BlockCopy(bit16col, 0, pntr, 0, width * height * 2);
        tex.LoadRawTextureData(pntr);*/
    }

    static void DecodeDXT3(int width, int height, Texture2D tex, byte[] data)
    {
        Color32[] pixels = new Color32[width * height];
        int index = 128;
        // calculate number of rows and columns, aligned to 4. mandatory for dds format
        int w = (width + 3) / 4;
        int h = (height + 3) / 4;
        int[] alphaTable = new int[16];
        for (int i = 0; i < h; i++)
        {
            for (int i2 = 0; i2 < w; i2++)
            {
                // create alpha table
                for (int i3 = 0; i3 < 4; i3++)
                {
                    int a0 = data[index++];
                    int a1 = data[index++];
                    // 4 bit to 8 bit conversion
                    alphaTable[4 * i3 + 0] = 17 * ((a0 & 0xF0) >> 4);
                    alphaTable[4 * i3 + 1] = 17 * (a0 & 0x0F);
                    alphaTable[4 * i3 + 2] = 17 * ((a1 & 0xF0) >> 4);
                    alphaTable[4 * i3 + 3] = 17 * (a1 & 0x0F);
                }
                // read color0 and color1, which are both 16 bit (r5, g6, b5) colors
                int c0 = data[index] | (data[index + 1] << 8);
                index += 2;
                int c1 = data[index] | (data[index + 1] << 8);
                index += 2;

                for (int i3 = 0; i3 < 4; i3++)
                {
                    // 4 more bytes telling us what color mixing method to use for each pixel
                    if (4 * i + i3 >= height) break;
                    int t0 = (data[index] & 0x03);
                    int t1 = (data[index] & 0x0C) >> 2;
                    int t2 = (data[index] & 0x30) >> 4;
                    int t3 = (data[index++] & 0xC0) >> 6;
                    // generate colors from the functions
                    pixels[4 * width * i + 4 * i2 + width * i3] = GetDXTColor(c0, c1, alphaTable[4 * i3], t0);
                    if (4 * i2 + 1 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 1] = GetDXTColor(c0, c1, alphaTable[4 * i3 + 1], t1);
                    if (4 * i2 + 2 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 2] = GetDXTColor(c0, c1, alphaTable[4 * i3 + 2], t2);
                    if (4 * i2 + 3 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 3] = GetDXTColor(c0, c1, alphaTable[4 * i3 + 3], t3);
                }
            }
        }
        tex.SetPixels32(pixels);
        // we now have our colors, let's convert them to rgba4444
        /*ushort[] bit16col = new ushort[width * height];
        for (int i = 0; i < width * height; i++)
        {
            bit16col[i] = (ushort)((Color4bit(pixels[i].r) << 12) | (Color4bit(pixels[i].g) << 8) | (Color4bit(pixels[i].b) << 4) | (Color4bit(pixels[i].a)));
        }
        byte[] pntr = new byte[width * height * 2];
        System.Buffer.BlockCopy(bit16col, 0, pntr, 0, width * height * 2);
        tex.LoadRawTextureData(pntr);*/
    }

    static int GetAlpha(int a0, int a1, int b)
    {
        if (a0 > a1) switch (b)
            {
                case 0: return a0;
                case 1: return a1;
                case 2: return (6 * a0 + a1) / 7;
                case 3: return (5 * a0 + 2 * a1) / 7;
                case 4: return (4 * a0 + 3 * a1) / 7;
                case 5: return (3 * a0 + 4 * a1) / 7;
                case 6: return (2 * a0 + 5 * a1) / 7;
                case 7: return (a0 + 6 * a1) / 7;
            }
        else switch (b)
            {
                case 0: return a0;
                case 1: return a1;
                case 2: return (4 * a0 + a1) / 5;
                case 3: return (3 * a0 + 2 * a1) / 5;
                case 4: return (2 * a0 + 3 * a1) / 5;
                case 5: return (a0 + 4 * a1) / 5;
                case 6: return 0;
                case 7: return 255;
            }
        return 0;
    }

    static void DecodeDXT5(int width, int height, Texture2D tex, byte[] data)
    {
        Color32[] pixels = new Color32[width * height];
        int index = 128;
        // calculate number of rows and columns, aligned to 4. mandatory for dds format
        int w = (width + 3) / 4;
        int h = (height + 3) / 4;
        int[] alphaTable = new int[16];
        for (int i = 0; i < h; i++)
        {
            for (int i2 = 0; i2 < w; i2++)
            {
                // create alpha table
                int a0 = data[index++];
                int a1 = data[index++];
                int b0 = data[index] | (data[index + 1] << 8) | (data[index + 2] << 16);
                index += 3;
                int b1 = data[index] | (data[index + 1] << 8) | (data[index + 2] << 16);
                index += 3;
                alphaTable[0] = b0 & 0x07;
                alphaTable[1] = (b0 >> 3) & 0x07;
                alphaTable[2] = (b0 >> 6) & 0x07;
                alphaTable[3] = (b0 >> 9) & 0x07;
                alphaTable[4] = (b0 >> 12) & 0x07;
                alphaTable[5] = (b0 >> 15) & 0x07;
                alphaTable[6] = (b0 >> 18) & 0x07;
                alphaTable[7] = (b0 >> 21) & 0x07;
                alphaTable[8] = b1 & 0x07;
                alphaTable[9] = (b1 >> 3) & 0x07;
                alphaTable[10] = (b1 >> 6) & 0x07;
                alphaTable[11] = (b1 >> 9) & 0x07;
                alphaTable[12] = (b1 >> 12) & 0x07;
                alphaTable[13] = (b1 >> 15) & 0x07;
                alphaTable[14] = (b1 >> 18) & 0x07;
                alphaTable[15] = (b1 >> 21) & 0x07;
                // read color0 and color1, which are both 16 bit (r5, g6, b5) colors
                int c0 = data[index] | (data[index + 1] << 8);
                index += 2;
                int c1 = data[index] | (data[index + 1] << 8);
                index += 2;

                for (int i3 = 0; i3 < 4; i3++)
                {
                    // 4 more bytes telling us what color mixing method to use for each pixel
                    if (4 * i + i3 >= height) break;
                    int t0 = (data[index] & 0x03);
                    int t1 = (data[index] & 0x0C) >> 2;
                    int t2 = (data[index] & 0x30) >> 4;
                    int t3 = (data[index++] & 0xC0) >> 6;
                    // generate colors from the functions
                    pixels[4 * width * i + 4 * i2 + width * i3] = GetDXTColor(c0, c1, GetAlpha(a0, a1, alphaTable[4 * i3]), t0);
                    if (4 * i2 + 1 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 1] = GetDXTColor(c0, c1, GetAlpha(a0, a1, alphaTable[4 * i3]), t1);
                    if (4 * i2 + 2 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 2] = GetDXTColor(c0, c1, GetAlpha(a0, a1, alphaTable[4 * i3]), t2);
                    if (4 * i2 + 3 >= width) continue;
                    pixels[4 * width * i + 4 * i2 + width * i3 + 3] = GetDXTColor(c0, c1, GetAlpha(a0, a1, alphaTable[4 * i3]), t3);
                }
            }
        }
        tex.SetPixels32(pixels);
        // we now have our colors, let's convert them to rgba4444
        /*ushort[] bit16col = new ushort[width * height];
        for (int i = 0; i < width * height; i++)
        {
            bit16col[i] = (ushort)((Color4bit(pixels[i].r) << 12) | (Color4bit(pixels[i].g) << 8) | (Color4bit(pixels[i].b) << 4) | (Color4bit(pixels[i].a)));
        }
        byte[] pntr = new byte[width * height * 2];
        System.Buffer.BlockCopy(bit16col, 0, pntr, 0, width * height * 2);
        tex.LoadRawTextureData(pntr);*/
    }


    public static Texture2D Load(string filename)
    {
        // open file, return null if failed to open
        FileStream fs = Utilj.FileOpen(filename, FileMode.Open, FileAccess.Read);
        if (fs == null)
        {
            return null;
        }
        fs.Seek(0, SeekOrigin.End);
        datare = new byte[fs.Position];
        fs.Seek(0, SeekOrigin.Begin);
        fs.Read(datare, 0, datare.Length);
        fs.Close();
        Texture2D tex = new Texture2D(GetWidth(datare), GetHeight(datare));
        int type = GetType(datare);
        switch (type)
        {
            case DXT1:
                {
                    DecodeDXT1(GetWidth(datare), GetHeight(datare), tex, datare);
                }
                break;
            case DXT2:
            case DXT3:
                {
                    DecodeDXT3(GetWidth(datare), GetHeight(datare), tex, datare);
                }
                break;
            case DXT4:
            case DXT5:
                {
                    DecodeDXT5(GetWidth(datare), GetHeight(datare), tex, datare);
                }
                break;
                // TODO: uncompressed formats
        }
        datare = null;
        tex.Apply();
        return tex;
    }
}
