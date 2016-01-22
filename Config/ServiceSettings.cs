using CPService.Properties;
using log4net;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace CPService.Config
{
    public class ServiceSettings
    {
        private static string _password;


        /// <summary>
        /// Database connection string
        /// </summary>
        public static string SqlConnectionString { get; set; }

        /// <summary>
        /// Primary domain controller to use when communicating
        /// </summary>
        public static string PrimaryDC { get; set; }

        /// <summary>
        /// Connection to the Exhcange server
        /// </summary>
        public static string ExchangeConnection { get; set; }

        /// <summary>
        /// FQDN of the Exchange server or load balancer to use
        /// </summary>
        public static string ExchangeServer { get; set; }

        /// <summary>
        /// What version of Exchange server
        /// </summary>
        public static int ExchangeVersion { get; set; }

        /// <summary>
        /// Username to use when contacting Active Directory and Exchange
        /// </summary>
        public static string Username { get; set; }

        /// <summary>
        /// SaltKey to use when encrypting and decrypting the password
        /// </summary>
        public static string SaltKey { get; set; }

        /// <summary>
        /// Password that is stored unencrypted in memory but encrypted in the settings file
        /// </summary>
        public static string Password

        {
            get
            {
                return _password;
            }
            set
            {
                _password = Decrypt(value, SaltKey);
            }
        }

        /// <summary>
        /// Used to decrypt the password with the salt string
        /// </summary>
        /// <param name="cipherString"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string Decrypt(string cipherString, string key)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);

            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            hashmd5.Clear();

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            tdes.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        /// <summary>
        /// Reads the settings from the Xml file and stores in memory
        /// </summary>
        public static void ReadSettings()
        {
            try
            {                
                var xDoc = XDocument.Load(Settings.Default.SettingsPath);
                var x = from s in xDoc.Elements("cloudpanel") select s;

                // Get salt key first for decryption
                SaltKey = x.Descendants("Settings").Elements("SaltKey").FirstOrDefault().Value;
                Username = x.Descendants("Settings").Elements("Username").FirstOrDefault().Value;
                Password = x.Descendants("Settings").Elements("Password").FirstOrDefault().Value;
                PrimaryDC = x.Descendants("Settings").Elements("PrimaryDC").FirstOrDefault().Value;

                SqlConnectionString = x.Descendants("Settings").Elements("Database").FirstOrDefault().Value;

                ExchangeServer = x.Descendants("Exchange").Elements("ExchangeServer").FirstOrDefault().Value;
                ExchangeVersion = int.Parse(x.Descendants("Exchange").Elements("ExchangeVersion").FirstOrDefault().Value);
                ExchangeConnection = x.Descendants("Exchange").Elements("ExchangeConnection").FirstOrDefault().Value;
            }
            catch (Exception ex)
            {
                CPService.LogError("Error reading settings: " + ex.ToString());
            }
        }

    }
}
