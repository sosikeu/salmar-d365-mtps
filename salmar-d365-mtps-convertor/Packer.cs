namespace salmar_d365_mtps_convertor
{
    using global::neu_d365mtinteg_convertor;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

    internal class Packer
    {
        public MemoryStream PackerZIP(PackFile[] FilesToPack)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    int counter = 0;
                    foreach (PackFile f in FilesToPack)
                    {
                        counter++;
                        string file = f.FileContentBase64String.ToString();
                        byte[] fileinbytes = Convert.FromBase64String(file);

                        var demoFile = archive.CreateEntry(f.FileName);
                        using (var entryStream = demoFile.Open())
                        {
                            using (var binaryWriter = new BinaryWriter(entryStream))
                            {
                                binaryWriter.Write(fileinbytes);
                            }
                        }
                    }
                }
                return memoryStream;
            }
        }

        public List<PackFile> PackerUNZIP(byte[] zipBuffer)
        {
            List<PackFile> RES = new List<PackFile>();
            using (var archive = new ZipArchive(new MemoryStream(zipBuffer, false), ZipArchiveMode.Read, false))
            {
                foreach (ZipArchiveEntry zipArchiveEntry in archive.Entries)
                {
                    PackFile f = new PackFile();
                    f.FileName = zipArchiveEntry.Name;
                    using (StreamReader sr = new StreamReader(zipArchiveEntry.Open()))
                    {
                        f.FileContentBase64String = sr.ReadToEnd();
                    }
                    RES.Add(f);
                }
            }
            return RES;
        }
    }

}
