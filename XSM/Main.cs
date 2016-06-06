using System;
using System.IO;

namespace XSM
{
    public class XuseManager
    {
        public string[] Packgets { get; private set; } = new string[0];
        public XuseManager(string Directory) {
            if (!System.IO.Directory.Exists(Directory))
                throw new Exception("Invalid Directory");
            string[] Files = System.IO.Directory.GetFiles(Directory, "*.dll");
            foreach (string file in Files) {
                string Packget = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file);
                if (System.IO.File.Exists(Packget + ".gd")) {
                    string[] tmp = new string[Packgets.Length + 1];
                    Packgets.CopyTo(tmp, 0);
                    tmp[Packgets.Length] = Packget;
                    Packgets = tmp;
                }
            }
        }

        public void Extract(int ID) {
            //Initialize Variables
            Stream stream = new FileStream(Packgets[ID] + ".gd", FileMode.Open);
            byte[] OffsetTable = File.ReadAllBytes(Packgets[ID] + ".dll");
            uint FileCount = (((uint)OffsetTable.Length / 4u) / 2u) - 1u;
            string BaseDir = Packgets[ID] + "_Dump\\";

            //Start Extract
            Directory.CreateDirectory(BaseDir);
            for (int i = 0; i < FileCount; i++) {
                uint offpos = ((uint)i * 8u) + 4u;
                byte[] DW = GetRange(offpos, 4, OffsetTable);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(DW, 0, DW.Length);
                uint Offset = BitConverter.ToUInt32(DW, 0);

                DW = GetRange(offpos + 4, 4, OffsetTable);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(DW, 0, DW.Length);
                uint EndPos = BitConverter.ToUInt32(DW, 0) + Offset;

                stream.Position = Offset;
                string FileName = BaseDir + GenHex(i);
                if (File.Exists(FileName))
                    File.Delete(FileName);
                Stream OutFile = new StreamWriter(FileName).BaseStream;
                while (stream.Position < EndPos) {
                    uint ReamingLen = EndPos - (uint)stream.Position;
                    uint BuffLen = 1024 * 2;
                    byte[] Buffer = new byte[ReamingLen < BuffLen ? ReamingLen : BuffLen];
                    stream.Read(Buffer, 0, Buffer.Length);
                    OutFile.Write(Buffer, 0, Buffer.Length);
                }
                OutFile.Close();
            }
            stream.Close();
            GiveExtension(BaseDir);
        }

        private void GiveExtension(string Dir) {
            string[] Files = Directory.GetFiles(Dir);
            Format[] Formats = new Format[] {
                new Format() {
                    Extension = "jpg",
                    Signature = new int[] { 0xFF, 0xD8, 0xFF }
                }, new Format() {
                    Extension = "dll",
                    Signature = new int[] { 0x4D, 0x5A }
                }, new Format() {
                    Extension = "zip",
                    Signature = new int[] { 0x50, 0x5B }
                }, new Format() {
                    Extension = "png",
                    Signature = new int[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
                }, new Format() {
                    Extension = "wmv",
                    Signature = new int[] { 0x30, 0x26, 0xB2, 0x75 }
                }, new Format() {
                    Extension = "ogg",
                    Signature = new int[] { 0x4F, 0x67, 0x67, 0x53 }
                }, new Format() {
                    Extension = "wav",
                    Signature = new int[] { 0x52, 0x49, 0x46, 0x46 -1, -1, -1, -1, 0x57, 0x51, 0x56, 0x45 }
                }, new Format() {
                    Extension = "avi",
                    Signature = new int[] { 0x52, 0x49, 0x46, 0x46, -1, -1, -1, -1, 0x41, 0x56, 0x49, 0x20 }
                }, new Format() {
                    Extension = "mp3",
                    Signature = new int[] { 0xFF, 0xFB}
                }, new Format() {
                    Extension = "mp3",
                    Signature = new int[] {0x49, 0x44, 0x33}
                }, new Format() {
                    Extension = "bmp",
                    Signature = new int[] { 0x42, 0x4D }
                }, new Format() {
                    Extension = "midi",
                    Signature = new int[] { 0x4D, 0x54, 0x68, 0x64 }
                }
            };
            
            foreach (string file in Files) {
                Stream Reader = new StreamReader(file).BaseStream;
                string Extension = ".";
                foreach (Format extension in Formats) {
                    Reader.Position = 0;
                    byte[] Header = new byte[extension.Signature.Length];
                    Reader.Read(Header, 0, Header.Length);
                    bool isvalid = true;
                    for (int i = 0; i < Header.Length; i++)
                        if (extension.Signature[i] != -1)
                            if (Header[i] != extension.Signature[i])
                                isvalid = false;
                    if (isvalid)
                        Extension += extension.Extension;
                }
                if (Extension == ".") {
                    byte[] Header = new byte[3];
                    Reader.Position = 0;
                    Reader.Read(Header, 0, Header.Length);
                    string ForceExt = System.Text.Encoding.ASCII.GetString(Header);
                    bool CanUse = true;
                    foreach (char letter in ForceExt)
                        if (!((letter >= 'a' && letter <= 'z') || (letter >= 'A' && letter <= 'Z')))
                            CanUse = false;
                    if (CanUse)
                        Extension += ForceExt.ToLower();
                    else
                        Extension += "bin";
                }
                Reader.Close();
                string NewName = file + Extension;
                if (File.Exists(NewName))
                    File.Delete(NewName);
                File.Move(Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file), NewName);                
            }
        }

        private struct Format {
            public string Extension;
            public int[] Signature;
        }
        private string GenHex(int i) {
            string hex = i.ToString("X");
            while (hex.Length < 8)
                hex = "0" + hex;
            return "0x" + hex;
        }

        public byte[] GetRange(uint pos, uint Length, byte[] data) {
            byte[] content = new byte[Length];
            for (uint i = 0; i < Length; i++)
                content[i] = data[i + pos];
            return content;
        }
    }
}
