using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChromeClipboardHost
{
  class Program
  {
    class Data
    {
      [JsonPropertyName("format")]
      public string Format { get; set; }
      [JsonPropertyName("base64")]
      public string Base64 { get; set; }
    }

    static private T Read<T>(BinaryReader input)
    {
      int length = input.ReadInt32();
      byte[] jsonBuffer = input.ReadBytes(length);
      return JsonSerializer.Deserialize<T>(jsonBuffer);
    }

    static private void Write<T>(BinaryWriter output, T value)
    {
      byte[] jsonBuffer = JsonSerializer.SerializeToUtf8Bytes<T>(value);
      output.Write(jsonBuffer.Length);
      output.Write(jsonBuffer);
      output.Flush();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint RegisterClipboardFormat(string format);

    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int memcpy(IntPtr dst, IntPtr src, uint count);

    enum BitmapCompressionMode : uint
    {
      BI_RGB = 0,
      BI_RLE8 = 1,
      BI_RLE4 = 2,
      BI_BITFIELDS = 3,
      BI_JPEG = 4,
      BI_PNG = 5,
    }

    enum ClipboardType : uint
    {
      CF_BITMAP = 2,
      CF_DIB = 8,
      CF_DIBV5 = 17,
    }

    enum LogicalColorSpace
    {
      LCS_CALIBRATED_RGB = 0x00000000,
      LCS_sRGB = 0x73524742,
      LCS_WINDOWS_COLOR_SPACE = 0x57696E20
    }

    enum GamutMappingIntent
    {
      LCS_GM_ABS_COLORIMETRIC = 0x00000008,
      LCS_GM_BUSINESS = 0x00000001,
      LCS_GM_GRAPHICS = 0x00000002,
      LCS_GM_IMAGES = 0x00000004
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CIEXYZ
    {
      public int ciexyzX; // 16.16 fixed point
      public int ciexyzY;
      public int ciexyzZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CIEXYZTRIPLE
    {
      public CIEXYZ ciexyzRed;
      public CIEXYZ ciexyzGreen;
      public CIEXYZ ciexyzBlue;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPV5HEADER
    {
      public int bV5Size;
      public int bV5Width;
      public int bV5Height;
      public short bV5Planes;
      public short bV5BitCount;
      public int bV5Compression;
      public int bV5SizeImage;
      public int bV5XPelsPerMeter;
      public int bV5YPelsPerMeter;
      public int bV5ClrUsed;
      public int bV5ClrImportant;
      public uint bV5RedMask;
      public uint bV5GreenMask;
      public uint bV5BlueMask;
      public uint bV5AlphaMask;
      public uint bV5CSType;
      public CIEXYZTRIPLE bV5Endpoints;
      public int bV5GammaRed;
      public int bV5GammaGreen;
      public int bV5GammaBlue;
      public uint bV5Intent;
      public int bV5ProfileData;
      public int bV5ProfileSize;
      public int bV5Reserved;
    }

    private static void SetClipboardData(uint uFormat, byte[] data)
    {
      IntPtr hGlobal = Marshal.AllocHGlobal(data.Length);
      Marshal.Copy(data, 0, hGlobal, data.Length);
      SetClipboardData(uFormat, hGlobal);
      Marshal.FreeHGlobal(hGlobal);
    }

    private static void SetClipboardBitmap(Bitmap bitmap, byte[] pngData)
    {
      // https://stackoverflow.com/questions/15689541/win32-clipboard-and-alpha-channel-images
#if !USE_V5_BITMAP_HEADER
      bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY); // GDI+ bitmap data is flipped vertically, unflip before accessing bytes
      BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

      BITMAPINFOHEADER header = new BITMAPINFOHEADER();
      header.biSize = Marshal.SizeOf(header);
      header.biWidth = bitmap.Width;
      header.biHeight = bitmap.Height;
      header.biPlanes = 1;
      header.biBitCount = 32;
      header.biCompression = (int)BitmapCompressionMode.BI_RGB;
      header.biSizeImage = bitmap.Width * bitmap.Height * 4;

      IntPtr hGlobal = Marshal.AllocHGlobal(Marshal.SizeOf(header) + header.biSizeImage);
      Marshal.StructureToPtr(header, hGlobal, false);
      memcpy(hGlobal + Marshal.SizeOf(header), data.Scan0, (uint)header.biSizeImage);
      SetClipboardData((uint)ClipboardType.CF_DIB, hGlobal);
      Marshal.FreeHGlobal(hGlobal);

      bitmap.UnlockBits(data);
#else
      bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY); // GDI+ bitmap data is flipped vertically, unflip before accessing bytes
      BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

      BITMAPV5HEADER header = new BITMAPV5HEADER();
      header.bV5Size = Marshal.SizeOf(header);
      header.bV5Width = bitmap.Width;
      header.bV5Height = bitmap.Height;
      header.bV5Planes = 1;
      header.bV5BitCount = 32;
      header.bV5Compression = (int)BitmapCompressionMode.BI_BITFIELDS;
      header.bV5SizeImage = bitmap.Width * bitmap.Height * 4;
      header.bV5RedMask   = 0x00FF0000;
      header.bV5GreenMask = 0x0000FF00;
      header.bV5BlueMask  = 0x000000FF;
      header.bV5AlphaMask = 0xFF000000;
      header.bV5CSType = (uint)LogicalColorSpace.LCS_WINDOWS_COLOR_SPACE;
      header.bV5Intent = (uint)GamutMappingIntent.LCS_GM_IMAGES;

      IntPtr hGlobal = Marshal.AllocHGlobal(Marshal.SizeOf(header) + header.bV5SizeImage);
      Marshal.StructureToPtr(header, hGlobal, false);
      //Marshal.Copy(pngData, 0, hGlobal + Marshal.SizeOf(header), pngData.Length);
      memcpy(hGlobal + Marshal.SizeOf(header), data.Scan0, (uint)header.bV5SizeImage);
      SetClipboardData((uint)ClipboardType.CF_DIBV5, hGlobal);
      Marshal.FreeHGlobal(hGlobal);

      bitmap.UnlockBits(data);
#endif
    }

    private static void WriteClipboard(string format, byte[] data)
    {
      OpenClipboard(IntPtr.Zero);
      EmptyClipboard();
      SetClipboardData(RegisterClipboardFormat(format), data);
      if (format == "PNG")
      {
        using (MemoryStream pngMemoryStream = new MemoryStream(data))
        using (Bitmap bitmap = new Bitmap(pngMemoryStream))
        using (Bitmap bitmapWithoutBackground = new Bitmap(bitmap.Width, bitmap.Height))
        {
          // clear transparent background with white color
          using (Graphics graphics = Graphics.FromImage(bitmapWithoutBackground))
          {
            graphics.Clear(Color.White);
            graphics.DrawImage(bitmap, Point.Empty);
          }
          SetClipboardBitmap(bitmapWithoutBackground, data);
        }
      }
      CloseClipboard();
    }

    static void Main(string[] args)
    {
      try
      {
        using (Stream inputStream = Console.OpenStandardInput(), outputStream = Console.OpenStandardOutput())
        using (BinaryReader input = new BinaryReader(inputStream))
        using (BinaryWriter output = new BinaryWriter(outputStream))
        {
          while(true)
          {
            Data data = Read<Data>(input);
            WriteClipboard(data.Format, Convert.FromBase64String(data.Base64));
            Write(output, true);
          }
        }
      }
      catch (EndOfStreamException)
      {
        // do nothing this is fine
      }
      catch (Exception ex)
      {
        MessageBox(IntPtr.Zero, ex.Message, "ChromeClipboardHost failed", 0x00000010);
      }
    }
  }
}
