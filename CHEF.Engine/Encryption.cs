using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace CHEFEngine
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    public class ConnectionEncryption
    {
        private const string END_MARKER = "@@##";

        private string m_sKeyName = "";

        public ConnectionEncryption()
        {
        }

        public ConnectionEncryption(string NameValue)
        {
            m_sKeyName = NameValue;
        }

        /// <summary>
        /// Public method called to encrypt the passed in string and return the 
        /// encrypted string.
        /// </summary>
        /// <param name="InitialString">String value to encrypt</param>
        /// <returns>Encrypted string value</returns>
        public string EncryptString(string InitialString)
        {

            InitialString += END_MARKER;
            string sName = "";

            if (m_sKeyName == "")
                sName = System.Environment.MachineName;
            else
                sName = m_sKeyName;

            TripleDESCryptoServiceProvider provider = FormatProvider(sName);

            if (InitialString.Length % 4 != 0)
            {
                int iDif = InitialString.Length % 4;
                InitialString = InitialString.PadRight(InitialString.Length + iDif, ' ');
            }

            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, provider.CreateEncryptor(), CryptoStreamMode.Write);

            byte[] baUnEnc = new byte[Encoding.Unicode.GetMaxByteCount(InitialString.Length)];
            baUnEnc = Encoding.Unicode.GetBytes(InitialString);

            ms.Position = 0;
            cs.Write(baUnEnc, 0, baUnEnc.Length);
            cs.Flush();
            cs.Close();
            ms.Close();
            byte[] baEnc = ms.GetBuffer();

            string sEncrypted = Convert.ToBase64String(baEnc);
            return sEncrypted;
        }


        /// <summary>
        /// Public method called to decrypt the passed in string.  This string is 
        /// assumed to have been encrypted using the encrypt string method provided
        /// by this module.
        /// </summary>
        /// <param name="InitialString">String value to decrypt</param>
        /// <returns>Decrypted string value</returns>
        public string DecryptString(string InitialString)
        {

            string sName = "";

            if (m_sKeyName == "")
                sName = System.Environment.MachineName;
            else
                sName = m_sKeyName;

            TripleDESCryptoServiceProvider provider = FormatProvider(sName);

            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, provider.CreateDecryptor(), CryptoStreamMode.Write);

            byte[] baIn = Convert.FromBase64String(InitialString);

            ms.Position = 0;
            cs.Write(baIn, 0, baIn.Length);
            cs.Flush();
            ms.Close();

            byte[] baOut = ms.GetBuffer();

            string sUnencrypted = Encoding.Unicode.GetString(baOut);
            // trim off everything after the end marker
            int iPos = sUnencrypted.IndexOf(END_MARKER);
            if (iPos > -1)
                sUnencrypted = sUnencrypted.Substring(0, iPos);

            return sUnencrypted;
        }


        /// <summary>
        /// Private method called to format the TripleDESCryptoServiceProvider
        /// object and initialize the key and iv values.
        /// </summary>
        /// <param name="Name">
        /// String value to used in formatting the key and iv for the encryption/
        /// decryption routines.
        /// </param>
        /// <returns></returns>
        private TripleDESCryptoServiceProvider FormatProvider(string Name)
        {

            TripleDESCryptoServiceProvider tdp = new TripleDESCryptoServiceProvider();
            tdp.Key = FormatKey(Name);
            tdp.IV = FormatIV(Name);

            return tdp;
        }


        /// <summary>
        /// Private method called to format the key used by the TripleDES encryption 
        /// algorythym.  This key is based on the passed in name value.
        /// </summary>
        /// <param name="Name">String value to use for formatting the encryption key</param>
        /// <returns>Byte array containing the encryption key</returns>
        private byte[] FormatKey(string Name)
        {

            string sKey = "";

            // reverse it
            for (int i = Name.Length - 1; i >= 0; i--)
            {
                sKey += Name.Substring(i, 1);
            }

            // pad it
            if (sKey.Length < 24)
            {
                for (int i = sKey.Length; i < 24; i++)
                {
                    int iValue = (i % 8);
                    sKey += iValue.ToString();
                }
            }

            byte[] baKey = Encoding.Unicode.GetBytes(sKey.ToCharArray(), 0, 12);
            // return it
            return baKey;
        }



        /// <summary>
        /// Private method called to format the initialization vector used by the 
        /// triple des encription algorythym.
        /// </summary>
        /// <param name="Name">String value to use for initializing the IV key</param>
        /// <returns>Byte array used for the IV parameter of the TripleDES encryptor</returns>
        private byte[] FormatIV(string Name)
        {
            string sIV = Name.Length.ToString();
            sIV += Name.GetHashCode().ToString();
            sIV += sIV.PadRight(10, '\n');

            byte[] baIV = Encoding.Unicode.GetBytes(sIV.ToCharArray(), 0, 4);
            return baIV;
        }

    }
}
