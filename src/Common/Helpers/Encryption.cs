using Serilog;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Common.Helpers;

public static class Encryption
{
	private const string EncryptionKey = "395b6f3e-795f-11ed-a1eb-0242ac120002";

	public static string Encrypt(this string text) => Encrypt(text, EncryptionKey);
	public static string Encrypt(this string text, string keyString)
	{
		var key = Encoding.UTF8.GetBytes(keyString);

		using (var aesAlg = Aes.Create())
		{
			using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
			{
				using (var msEncrypt = new MemoryStream())
				{
					using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					using (var swEncrypt = new StreamWriter(csEncrypt))
					{
						swEncrypt.Write(text);
					}

					var iv = aesAlg.IV;

					var decryptedContent = msEncrypt.ToArray();

					var result = new byte[iv.Length + decryptedContent.Length];

					Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
					Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

					return Convert.ToBase64String(result);
				}
			}
		}
	}

	public static string Decrypt(this string cipherText) => DecryptString(cipherText, EncryptionKey);

	public static string DecryptString(this string cipherText, string keyString)
	{
		var fullCipher = Convert.FromBase64String(cipherText);

		var iv = new byte[16];
		var cipher = new byte[16];

		Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
		Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, iv.Length);
		var key = Encoding.UTF8.GetBytes(keyString);

		using (var aesAlg = Aes.Create())
		{
			using (var decryptor = aesAlg.CreateDecryptor(key, iv))
			{
				string result;
				using (var msDecrypt = new MemoryStream(cipher))
				{
					using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (var srDecrypt = new StreamReader(csDecrypt))
						{
							result = srDecrypt.ReadToEnd();
						}
					}
				}

				return result;
			}
		}
	}

	public static void Encrypt(this ICredentials credentials)
	{
		if (!string.IsNullOrWhiteSpace(credentials.Email))
			credentials.Email = credentials.Email.Encrypt();

		if (!string.IsNullOrWhiteSpace(credentials.Password))
			credentials.Password = credentials.Password.Encrypt();
	}

	public static void Decrypt(this ICredentials credentials)
	{
		if (!string.IsNullOrWhiteSpace(credentials.Email))
		{
			try
			{
				var decrypted = credentials.Email.Decrypt();
				credentials.Email = decrypted;
			} catch (Exception e)
			{
				Log.Verbose(e, "Failed to decrypt Email, returning as is.");
			}
		}


		if (!string.IsNullOrWhiteSpace(credentials.Password))
		{
			try
			{
				var decrypted = credentials.Password.Decrypt();
				credentials.Password = decrypted;
			}
			catch (Exception e)
			{
				Log.Verbose(e, "Failed to decrypt Password, returning as is.");
			}
		}
	}
}
