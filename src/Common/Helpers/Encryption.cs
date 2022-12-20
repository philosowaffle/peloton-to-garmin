using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Common.Helpers;

public static class Encryption
{
	private static readonly byte[] Key_V1 = new byte[] { 93, 200, 159, 57, 99, 83, 228, 185, 232, 146, 99, 32, 196, 228, 250, 171, 51, 127, 166, 88, 155, 7, 123, 92, 166, 216, 37, 103, 41, 240, 204, 70 };
	private static readonly byte[] IV_V1 = new byte[] { 170, 245, 63, 175, 180, 206, 91, 224, 69, 186, 51, 196, 193, 166, 245, 50 };

	public static string Encrypt(this string text) => Encrypt(text, Key_V1, IV_V1);
	public static string Encrypt(this string text, byte[] key, byte[] iv)
	{
		using (var aesAlg = Aes.Create())
		{
			aesAlg.Key = key;
			aesAlg.IV = iv;

			using (var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
			{
				using (var msEncrypt = new MemoryStream())
				{
					using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					using (var swEncrypt = new StreamWriter(csEncrypt))
					{
						swEncrypt.Write(text);
					}

					var encrypted = msEncrypt.ToArray();
					return Convert.ToBase64String(encrypted);
				}
			}
		}
	}

	public static string Decrypt(this string cipherText) => DecryptString(cipherText, Key_V1, IV_V1);

	public static string DecryptString(this string cipherText, byte[] key, byte[] iv)
	{
		var encrypted = Convert.FromBase64String(cipherText);
		var encryptedByteArray = encrypted.ToArray();

		using (var aesAlg = Aes.Create())
		{
			aesAlg.Key = key;
			aesAlg.IV = iv;

			using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
			{
				string result;
				using (var msDecrypt = new MemoryStream(encryptedByteArray))
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
		if (string.IsNullOrWhiteSpace(credentials.Password)
			|| string.IsNullOrWhiteSpace(credentials.Email))
		{
			credentials.EncryptionVersion = EncryptionVersion.None;
			return;
		}

		var originalPassword = credentials.Password;
		var originalEmail = credentials.Email;

		try
		{
			credentials.Email = credentials.Email.Encrypt();
			credentials.Password = credentials.Password.Encrypt();
			credentials.EncryptionVersion = EncryptionVersion.V1;

		} catch (Exception e)
		{
			Log.Error(e, "Failed to encrypt Email or Password.");

			credentials.Email = originalEmail;
			credentials.Password = originalPassword;
			credentials.EncryptionVersion = EncryptionVersion.None;
			return;
		}
	}

	public static void Decrypt(this ICredentials credentials)
	{
		if (credentials.EncryptionVersion != EncryptionVersion.V1) return;

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
