using System;
using System.IO;

namespace XSM
{
    public class XuseManager
    {
        private bool RepackMode = false;
        private string RepackDir;
        public string[] Packgets { get; private set; } = new string[0];
        public XuseManager(string Directory, bool CreateMode) {
            if (!System.IO.Directory.Exists(Directory))
                throw new Exception("Invalid Directory");
            RepackMode = CreateMode;
            RepackDir = Directory;
            if (RepackMode)
                return;
            string[] Files = System.IO.Directory.GetFiles(Directory, "*.dll");
            foreach (string file in Files) {
                string Packget = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file);
                if (System.IO.File.Exists(Packget + ".gd") && System.IO.File.Exists(Packget + ".dll")) {
                    string[] tmp = new string[Packgets.Length + 1];
                    Packgets.CopyTo(tmp, 0);
                    tmp[Packgets.Length] = Packget;
                    Packgets = tmp;
                }
            }
        }

        public void Extract(int ID, bool Log) {
            if (RepackMode)
                return;
            //Initialize Variables
            Stream stream = new FileStream(Packgets[ID] + ".gd", FileMode.Open);

            byte[] OffsetTable = File.ReadAllBytes(Packgets[ID] + ".dll");
            uint FileCount = (((uint)OffsetTable.Length / 4u) / 2u) - 1u;
            string BaseDir = Packgets[ID] + "_Dump\\";

            //Start Extract
            Directory.CreateDirectory(BaseDir);
            for (uint i = 0; i < FileCount; i++) {
                uint offpos = (i * 8u) + 4u;
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
                if (Log)
                    Console.WriteLine("Extracting file at " + GenHex(Offset) + "...");
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

        public void CreatePackget(bool Log) {
            string Dir = RepackDir;
            if (!RepackMode)
                return;
            //Prepare Paths
            string BaseDir = Path.GetDirectoryName(Dir) + "\\";
            string PackgetName = Path.GetFileName(Dir).Replace("_Dump", "");

            //Prepare Files
            if (File.Exists(BaseDir + PackgetName + ".gd"))
                if (File.Exists(BaseDir + PackgetName + "-backup.gd"))
                    File.Delete(BaseDir + PackgetName + ".gd");
                else
                    File.Move(BaseDir + PackgetName + ".gd", BaseDir + PackgetName + "-backup.gd");

            if (File.Exists(BaseDir + PackgetName + ".dll"))
                if (File.Exists(BaseDir + PackgetName + "-backup.dll"))
                    File.Delete(BaseDir + PackgetName + ".dll");
                else
                    File.Move(BaseDir + PackgetName + ".dll", BaseDir + PackgetName + "-backup.dll");

            string[] files = Directory.GetFiles(Dir);
            uint Count = 0;
            string[] Extensions = new string[0];
            foreach (string file in files)
                try {
                    string ext = Path.GetExtension(file);
                    bool found = false;
                    for (int i = 0; i < Extensions.Length; i++) {
                        if (Extensions[i] == ext) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        string[] tmp = new string[Extensions.Length + 1];
                        Extensions.CopyTo(tmp, 0);
                        tmp[Extensions.Length] = ext;
                        Extensions = tmp;
                    }
                    uint ID = uint.Parse(Path.GetFileNameWithoutExtension(file).Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                    if (ID > Count)
                        Count = ID;
                }
                catch {
                    throw new Exception("Invalid File Name");
                }

            byte[] OffsetTable = new byte[4 + (8 * Count)];
            if (File.Exists(BaseDir + PackgetName + "-backup.dll")) {
                byte[] temp = File.ReadAllBytes(BaseDir + PackgetName + "-backup.dll");
                for (int i = 0; i < 4; i++)
                    OffsetTable[i] = temp[i]; 
            } else
                BitConverter.GetBytes(Count).CopyTo(OffsetTable, 0);

            Stream OutPackget = new StreamWriter(BaseDir + PackgetName + ".gd").BaseStream;
            for (uint id = 0; id < Count; id++) {
                bool found = false;
                foreach (string file in files)
                    if (uint.Parse(Path.GetFileNameWithoutExtension(file).Replace("0x", ""), System.Globalization.NumberStyles.HexNumber) == id) {
                        found = true;
                        break;
                    }
                if (!found)
                    throw new Exception("Missing " + GenHex(id));

                string FName = string.Empty;
                foreach (string Ext in Extensions) {
                    string fn = Dir + "\\" + GenHex(id) + Ext;
                    if (File.Exists(fn)) {
                        FName = fn;
                        break;
                    }
                }

                Stream Reader = new StreamReader(FName).BaseStream;

                uint OffPos = (id * 8) + 4;
                BitConverter.GetBytes((uint)OutPackget.Position).CopyTo(OffsetTable, OffPos);
                BitConverter.GetBytes((uint)Reader.Length).CopyTo(OffsetTable, OffPos + 4);
                if (Log)
                    Console.WriteLine("Writing File " + Path.GetFileNameWithoutExtension(FName) + " At " + GenHex((uint)OutPackget.Position));
                while (Reader.Position < Reader.Length) {
                    long ReamingLen = Reader.Length - Reader.Position;
                    long BuffLen = 1024 * 2;
                    byte[] Buffer = new byte[ReamingLen < BuffLen ? ReamingLen : BuffLen];
                    Reader.Read(Buffer, 0, Buffer.Length);
                    OutPackget.Write(Buffer, 0, Buffer.Length);
                }
                Reader.Close();
            }
            OutPackget.Close();
            File.WriteAllBytes(BaseDir + PackgetName + ".dll", OffsetTable);
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
        private string GenHex(uint i) {
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
