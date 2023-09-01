using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace A1.Tools
{
    public class RabinFingerprintCache
    {
        List<uint> fingerprints;
        string folderPath;

        public RabinFingerprintCache()
        {
            fingerprints = new List<uint>();
            folderPath = @"cache\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            RetrieveFingerprints();
        }

        public void RetrieveFingerprints()
        {
            if (Directory.Exists(folderPath))
            {
                DirectoryInfo directory = new DirectoryInfo(folderPath);

                foreach (FileInfo file in directory.GetFiles())
                {
                    string fileName = Path.GetFileNameWithoutExtension(file.Name);
                    uint fingerprint;;

                    if (uint.TryParse(fileName, out fingerprint))
                    {
                        fingerprints.Add(fingerprint);
                    }
                }
            }
        }
        public void AddDataBlock(uint fingerprint, byte[] dataBlock)
        {
            string fileName = fingerprint.ToString();
            string filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                return;
            }
            Task.Run(() =>
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    fileStream.Write(dataBlock, 0, dataBlock.Length);
                }
            });
            fingerprints.Add(fingerprint);
        }
        public List<uint> GetFingerprints()
        {
            return fingerprints;
        }
        public byte[] GetDataBlock(uint fingerprint)
        {

            string fileName = fingerprint.ToString();
            string filePath = Path.Combine(folderPath, fileName);
            if (File.Exists(filePath))
            {
                
                byte[] dataBlock = File.ReadAllBytes(filePath);
                return dataBlock;
            }
            return null;
        }

        public void ClearCache()
        {
            List<uint> fileToDelete = fingerprints;
            if (!Directory.Exists(folderPath))
            {
                return;
            }
            foreach (string file in Directory.GetFiles(folderPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (uint.TryParse(fileName, out uint fingerprint) && fileToDelete.Contains(fingerprint))
                {
                    try
                    {
                        File.Delete(file);
                        fingerprints.Remove(fingerprint);
                    } catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return;
        }

    }
}
