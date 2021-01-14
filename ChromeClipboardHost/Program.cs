using System;
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

    [DllImport("user32.dll", SetLastError=true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll", SetLastError=true)]
    private static extern bool EmptyClipboard();
    [DllImport("user32.dll", SetLastError=true)]
    private static extern uint RegisterClipboardFormat(string format);
    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    [DllImport("user32.dll", SetLastError=true)]
    private static extern bool CloseClipboard();
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    private static void WriteClipboard(string format, byte[] data)
    {
      OpenClipboard(IntPtr.Zero);
      EmptyClipboard();
      uint cformat = RegisterClipboardFormat(format);
      IntPtr hglobal = Marshal.AllocHGlobal(data.Length);
      Marshal.Copy(data, 0, hglobal, data.Length);
      SetClipboardData(cformat, hglobal);
      CloseClipboard();
      Marshal.FreeHGlobal(hglobal);
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
            try
            {
              WriteClipboard(data.Format, Convert.FromBase64String(data.Base64));
              Write(output, true);
            }
            catch (Exception ex)
            {
              Write(output, ex.Message);
              throw;
            }
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
