using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace FsFilter1UI
{
    static class MyEncryption
    {
        public static void EncryptFolder(string path, string key, int keySize, int saltSize)
        {
            string[] files = Directory.GetFiles(path, "", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (!file.EndsWith("aes")) AES_Encrypt(file, key, keySize, saltSize);
            }
        }

        public static void DecryptFolder(string path, string key, int keySize, int saltSize)
        {
            string[] files = Directory.GetFiles(path, "", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (file.EndsWith("aes")) AES_Decrypt(file, key, keySize, saltSize);
            }
        }

        public static string replaceExtension(string path, string ext)
        {
            int i = path.LastIndexOf(".");
            return path.Substring(0, i + 1) + ext;
        }

        public static string HashPasswordWithSalt(string password, int saltSize)
        {
            string salt = BitConverter.ToString(GenerateRandomSalt(saltSize)).Replace("-", string.Empty);
            string key = String.Concat(password, salt);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] hash = MD5.Create().ComputeHash(keyBytes);
            return salt + BitConverter.ToString(hash).Replace("-", string.Empty);
        }
        public static string HashPasswordWithSalt(string password, string salt)
        {
            string key = String.Concat(password, salt);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] hash = MD5.Create().ComputeHash(keyBytes);
            return salt + BitConverter.ToString(hash).Replace("-", string.Empty);
        }
        public static byte[] GenerateRandomSalt(int size)
        {
            byte[] data = new byte[size];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < size; i++)
                {
                    // Fill the buffer with the generated data
                    rng.GetBytes(data);
                }
            }

            return data;
        }

        // encrypt file to output file and deletes the input file.
        // the first few letters of the output file are the extension. to mark the end of the extension writes ! at the end
        // the IV is a hash of the salt
        static void AES_Encrypt(string inputFile, string key, int keySize, int saltSize)
        {
            string cryptFile = replaceExtension(inputFile, "aes");
            FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create);
            int extensionIndex = inputFile.LastIndexOf(".");
            string inputFileExtension = inputFile.Substring(extensionIndex + 1, inputFile.Length - 1 - extensionIndex) + "!";
            byte[] inputFileExtBytes = Encoding.UTF8.GetBytes(inputFileExtension);
            fsCrypt.Write(inputFileExtBytes);
            RijndaelManaged AES = new RijndaelManaged();

            AES.KeySize = keySize * 8;
            AES.BlockSize = 128;

            string regIV = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "IV", null);
            if (regIV == null)
            {
                AES.GenerateIV();
                var GeneratedIV = AES.IV;
                AES.IV = XorByteArrays(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(key, 0, saltSize * 2)), AES.IV);
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "IV", BitConverter.ToString(AES.IV).Replace("-", string.Empty));
                AES.Key = XorByteArrays(MD5.Create().ComputeHash(GeneratedIV), Encoding.UTF8.GetBytes(key, saltSize * 2, keySize / 2));
                AES.Padding = PaddingMode.Zeros;
            }
            else
            {
                AES.IV = ConvertHexStringToByteArray(regIV);
                byte[] generatedIV = XorByteArrays(AES.IV, MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(key, 0, saltSize * 2)));
                AES.Key = XorByteArrays(MD5.Create().ComputeHash(generatedIV), Encoding.UTF8.GetBytes(key, saltSize * 2, keySize / 2));
                AES.Padding = PaddingMode.Zeros;
                AES.Mode = CipherMode.CBC;
            }
            AES.Mode = CipherMode.CBC;

            CryptoStream cs = new CryptoStream(fsCrypt,
                 AES.CreateEncryptor(),
                CryptoStreamMode.Write);

            FileStream fsIn = new FileStream(inputFile, FileMode.Open);

            int data;
            while ((data = fsIn.ReadByte()) != -1)
                cs.WriteByte((byte)data);


            fsIn.Close();
            cs.Close();
            fsCrypt.Close();
            File.Delete(inputFile);
        }

        private static byte[] XorByteArrays(byte[] vs, byte[] iV)
        {
            if (vs.Length != iV.Length)
            {
                throw new Exception("can't xor byte arrays who are not the same size");
            }
            byte[] result = new byte[vs.Length];
            for (int i = 0; i < vs.Length; i++)
            {
                result[i] = (byte)(vs[i] ^ iV[i]);
            }
            return result;
        }

        private static void AES_Decrypt(string inputFile, string key, int keySize, int saltSize)
        {

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);

            //read the extension and replace it
            int info;
            string extension = "";
            char infoc;
            while (true)
            {
                info = fsCrypt.ReadByte();
                infoc = Convert.ToChar(info);
                if (infoc == '!') break;
                extension += infoc;
            }
            string outputFile = replaceExtension(inputFile, extension);
            //

            RijndaelManaged AES = new RijndaelManaged();

            AES.KeySize = keySize * 8;
            AES.BlockSize = 128;

            string regIV = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "IV", null);
            if(regIV == null)
            {
                throw new Exception("failed to get IV from registry");
            }
            AES.IV = ConvertHexStringToByteArray(regIV);
            byte[] generatedIV = XorByteArrays(AES.IV, MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(key, 0, saltSize * 2)));
            AES.Key = XorByteArrays(MD5.Create().ComputeHash(generatedIV), Encoding.UTF8.GetBytes(key, saltSize * 2, keySize / 2));
            AES.Padding = PaddingMode.Zeros;
            AES.Mode = CipherMode.CBC;

            CryptoStream cs = new CryptoStream(fsCrypt,
                AES.CreateDecryptor(),
                CryptoStreamMode.Read);

            FileStream fsOut = new FileStream(outputFile, FileMode.Create);

            int data;
            while ((data = cs.ReadByte()) != -1)
                fsOut.WriteByte((byte)data);

            fsOut.Close();
            cs.Close();
            fsCrypt.Close();
            File.Delete(inputFile);
        }
        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }
    }
}

