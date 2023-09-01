using System;
using System.Collections.Generic;
using System.IO;

namespace A1.Tools
{
    public class RabinFingerprint
    {
        private int blockSize;
        private int prime;
        private int modulus;
        // fingerprints that had sent to cache
        List<uint> fingerprints;
        string folderPath;

        
        public RabinFingerprint()
        {
            blockSize = 2048;
            prime = 257;
            modulus = 12;
            fingerprints = new List<uint>();
            folderPath = @"UploadedFiles\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public List<byte[]> GetFingerprint(string fileName)
        {
            List<byte[]> response = new List<byte[]>();
            
            string filePath = Path.Combine(folderPath, fileName);
            if(!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{fileName} not found");
            }
            int hashSize = 32;
            int bytesToRead = blockSize - hashSize;
            byte[] buffer = new byte[blockSize];
            using (FileStream fs = File.OpenRead(filePath))
            {
                int bytesRead;
                while((bytesRead = fs.Read(buffer,0,buffer.Length)) > 0)
                {
                    uint hash = Fingerprint(buffer, 0, bytesToRead);
                   
                    if (fingerprints.Contains(hash))
                    {
                        // cache has the datablock
                        byte command = 0;
                        byte[] data = BitConverter.GetBytes(hash);
                        byte[] newData = new byte[data.Length + 1];
                        newData[0] = command;
                        Array.Copy(data, 0, newData, 1, data.Length);
                        response.Add(newData);
   
                    }
                    else
                    {
                        // send datablock;
                        byte command = 1;
                        byte[] newData = new byte[buffer.Length + 1];
                        newData[0] = command;
                        Array.Copy(buffer, 0, newData, 1, buffer.Length);
                        response.Add(newData);
                        fingerprints.Add(hash);

                    }
                }
            }
            return response;
        }

        public void AddFingerprint(uint fingerprint)
        {
            fingerprints.Add(fingerprint);
        }
        public uint Fingerprint(byte[] data, int offset, int length)
        {
            uint hash = 0;
            for(int i = offset; i < offset + length;i++)
            {
                hash = (uint)(hash * prime + data[i] % modulus);
            }
            return hash;
        }

        public void ClearFingerprint()
        {
            fingerprints.Clear();
            return;
        }
    }
}
