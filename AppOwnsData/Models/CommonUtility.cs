using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AppOwnsData.Models
{
    public class CommonUtility
    {
        private static readonly string EncryptionKey = ConfigurationManager.AppSettings["EncryptionKey"].ToString();
        private static readonly string SqlConnectionString = ConfigurationManager.AppSettings["ConnectionString"].ToString();
        private static readonly string SmtpServer = ConfigurationManager.AppSettings["SmtpServer"].ToString();
        private static readonly int SmtpPort = Convert.ToInt16(ConfigurationManager.AppSettings["SmtpPort"].ToString());
        private static readonly string SmptUSer = ConfigurationManager.AppSettings["SmtpUsername"].ToString();
        private static readonly string SmptPass = ConfigurationManager.AppSettings["SmtpPass"].ToString();

        public string InitialPassGeneration()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "@$!%*?&";
            string allChars = upper + lower + digits + special;

            StringBuilder password = new StringBuilder();
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            password.Append(GetRandomChar(upper, rng));
            password.Append(GetRandomChar(lower, rng));
            password.Append(GetRandomChar(digits, rng));
            password.Append(GetRandomChar(special, rng));

            for (int i = password.Length; i < 8; i++)
            {
                password.Append(GetRandomChar(allChars, rng));
            }

            return ShuffleString(password.ToString(), rng);
        }
        private static char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            byte[] buffer = new byte[1];
            char result;

            do
            {
                rng.GetBytes(buffer);
                result = chars[buffer[0] % chars.Length];
            }
            while (!chars.Contains(result));

            return result;
        }
        private static string ShuffleString(string input, RandomNumberGenerator rng)
        {
            char[] array = input.ToCharArray();
            int n = array.Length;

            while (n > 1)
            {
                byte[] box = new byte[1];
                rng.GetBytes(box);
                int k = box[0] % n;
                n--;

                char temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }

            return new string(array);
        }
        public bool ResetPass(string ResetEmail, string InitialPass)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(SmptUSer);
                mail.To.Add(ResetEmail);
                mail.IsBodyHtml = true;
                mail.Subject = "PowerBI Password Reset – One Time Initial Password";
                mail.Body = $"<html>\r\n<body style=\"font-family: Arial, sans-serif; color: #333;\">\r\n\r\n    <h2>Password Reset – One Time Initial Password</h2>\r\n\r\n    " +
                    $"<p>Hello User,</p>\r\n\r\n    " +
                    $"<p>Your account has been created successfully. To log in for the first time, please use the one-time initial password given below:</p>\r\n\r\n    " +
                    $"<p style=\"padding: 10px; background: #f4f4f4; display: inline-block; border-radius: 5px;\">\r\n        " +
                    $"<b>Initial Password:</b> {InitialPass}\r\n    </p>\r\n\r\n    <br />\r\n\r\n    " +
                    $"<p>\r\n        " +
                    $"<p>If you did not request this or believe it is an error, please contact the support team immediately.</p>\r\n\r\n    " +
                    $"<p>Regards,<br />\r\n    " +
                    $"<b>Support Team</b></p>\r\n\r\n</body>\r\n</html>\r\n";

                SmtpClient client = new SmtpClient(SmtpServer, SmtpPort);

                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(SmptUSer, SmptPass);
                client.EnableSsl = false;
                client.Send(mail);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string Encrypt(string plainText)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey.Substring(0, 32));
            byte[] ivBytes = Encoding.UTF8.GetBytes(EncryptionKey.Substring(0, 16));

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                    cs.Write(bytes, 0, bytes.Length);
                    cs.Close();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        public string Decrypt(string encryptedText)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey.Substring(0, 32));
            byte[] ivBytes = Encoding.UTF8.GetBytes(EncryptionKey.Substring(0, 16));
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                    cs.Close();
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
        public bool IsDefaultPass(string UserEmail)
        {
            DataTable dt = OpenDataTable(SqlConnectionString, $"Select IsDefault From PB_Users Where UserEmail = '{UserEmail}' and IsDefault = 'Y'");
            if(dt.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }
        public int ExecuteNonQuery(string connectionString, string query)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected;
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
        }
        public int ExecuteNonQuery(SqlConnection SqlCon, string query, SqlTransaction transaction)
        {
            using (SqlCommand command = new SqlCommand(query, SqlCon, transaction))
            {
                try
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
                catch
                {
                    return 0;
                }
            }
        }
        public DataTable OpenDataTable(SqlConnection Con, string query, SqlTransaction transaction)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlCommand command = new SqlCommand(query, Con))
                {
                    command.Transaction = transaction;

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception)
            {
                transaction?.Rollback();
                throw;
            }

            return dataTable;
        }
        public DataTable OpenDataTable(string connectionString, string query)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return dataTable;
        }
        public bool IsPasswordValid(string password)
        {
            string pattern = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$";

            return Regex.IsMatch(password, pattern);
        }
    }
}