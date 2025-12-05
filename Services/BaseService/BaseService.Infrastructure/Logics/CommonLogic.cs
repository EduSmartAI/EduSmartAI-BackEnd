using System.Security.Cryptography;
using System.Text;
using BaseService.Application.Interfaces.Commons;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;

namespace BaseService.Infrastructure.Logics;

/// <summary>
/// Shared.Common logic
/// </summary>
public class CommonLogic : ICommonLogic
{
    /// <summary>
    /// Encrypt the text
    /// </summary>
    /// <param name="beforeEncrypt"></param>
    /// <returns></returns>
    public EncryptTextResponse EncryptText(string beforeEncrypt)
    {
        var response = new EncryptTextResponse {Success = false};
        
        // Get the key and IV from configuration
        EnvLoader.Load();
        
        var key = Environment.GetEnvironmentVariable(ConstEnv.EncryptionKey);
        var iv = Environment.GetEnvironmentVariable(ConstEnv.EncryptionIv);
        
        // Check for null
        if (key == null)
        {
            response.SetMessage(MessageId.E99999);
            return response;
        }
        // Encrypt the text
        using Aes aes = Aes.Create();
        
        // Set the key and IV
        aes.Key = Encoding.UTF8.GetBytes(key);
        if (iv != null) aes.IV = Encoding.UTF8.GetBytes(iv);

        // Encrypt
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(beforeEncrypt);
        }     
        
        response.Response = new EncryptTextResponseEntity
        {
            EncryptedKey = Convert.ToBase64String(ms.ToArray())
        };
        
        // True
        response.Success = true;
        return response;
    }

    /// <summary>
    /// Decrypt the text
    /// </summary>
    /// <param name="beforeDecrypt"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DecryptTextEmailAndIdResponse DecryptTextEmailAndId(string beforeDecrypt)
    {
        var response = new DecryptTextEmailAndIdResponse {Success = false};
        
        EnvLoader.Load();
        
        var key = Environment.GetEnvironmentVariable(ConstEnv.EncryptionKey);
        var iv = Environment.GetEnvironmentVariable(ConstEnv.EncryptionIv);
        
        // Check for null
        if (key == null)
        {
            response.SetMessage(MessageId.E99999);
            return response;
        }
        // Decrypt the text
        var aes = Aes.Create();
        
        // Set the key and IV
        aes.Key = Encoding.UTF8.GetBytes(key);
        if (iv != null) aes.IV = Encoding.UTF8.GetBytes(iv);

        // Decrypt
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(beforeDecrypt));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        var decrypted = sr.ReadToEnd();

        var parts = decrypted.Split('-', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out var id))
        {
            response.SetMessage(MessageId.E99999);
            return response;
        }

        // Read the decrypted text
        response.Response = new DecryptTextResponseEntity
        {
            Email = parts[0],
            Id = Guid.Parse(parts[1])
        };
        
        // True
        response.Success = true;
        return response;
    }
    
    /// <summary>
    /// Generates a random password that meets complexity requirements
    /// </summary>
    /// <param name="length">Password length (minimum 8)</param>
    /// <returns>A secure random password</returns>
    public string GenerateRandomPassword(int length = 12)
    {
        // Ensure minimum length of 8
        length = Math.Max(8, length);
        
        // Define character sets
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numberChars = "0123456789";
        const string specialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";
        
        // Create a random number generator
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        
        // Generate one character from each required set
        var password = new char[length];
        password[0] = GetRandomChar(lowerChars, rng, bytes);
        password[1] = GetRandomChar(upperChars, rng, bytes);
        password[2] = GetRandomChar(numberChars, rng, bytes);
        password[3] = GetRandomChar(specialChars, rng, bytes);
        
        // Combine all character sets for remaining positions
        string allChars = lowerChars + upperChars + numberChars + specialChars;
        
        // Fill remaining positions with random characters
        for (int i = 4; i < length; i++)
        {
            password[i] = GetRandomChar(allChars, rng, bytes);
        }
        
        // Shuffle the password to avoid predictable patterns
        ShuffleArray(password, rng, bytes);
        
        return new string(password);
    }
    
    /// <summary>
    /// Gets a random character from the provided character set
    /// </summary>
    private static char GetRandomChar(string charSet, RandomNumberGenerator rng, byte[] bytes)
    {
        rng.GetBytes(bytes);
        uint num = BitConverter.ToUInt32(bytes, 0);
        return charSet[(int)(num % charSet.Length)];
    }
    
    /// <summary>
    /// Fisher-Yates shuffle algorithm to randomize character positions
    /// </summary>
    private static void ShuffleArray(char[] array, RandomNumberGenerator rng, byte[] bytes)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            rng.GetBytes(bytes);
            uint num = BitConverter.ToUInt32(bytes, 0);
            int j = (int)(num % (i + 1));
            
            // Swap elements
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
    
    /// <summary>
    /// Generates a secure random 6-digit OTP (One-Time Password)
    /// </summary>
    /// <returns>A 6-digit numeric OTP code as string</returns>
    public string GenerateOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
    
        // Convert bytes to a positive integer and take modulo to get a 6-digit number
        int value = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
    
        // Format as a 6-digit string with leading zeros if needed
        return value.ToString("D6");
    }

    /// <summary>
    /// Decrypt the text with DateTime and Id
    /// </summary>
    /// <param name="beforeDecrypt"></param>
    /// <returns></returns>
    public DecryptTextIdAndDateTimeResponse DecryptTextDateTimeAndEmail(string beforeDecrypt)
    {
        var response = new DecryptTextIdAndDateTimeResponse { Success = false };

        EnvLoader.Load();

        var key = Environment.GetEnvironmentVariable(ConstEnv.EncryptionKey);
        var iv = Environment.GetEnvironmentVariable(ConstEnv.EncryptionIv);

        // Check for null
        if (key == null)
        {
            response.SetMessage(MessageId.E99999);
            return response;
        }
        // Decrypt the text
        var aes = Aes.Create();

        // Set the key and IV
        aes.Key = Encoding.UTF8.GetBytes(key);
        if (iv != null) aes.IV = Encoding.UTF8.GetBytes(iv);

        // Decrypt
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(beforeDecrypt));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        var decrypted = sr.ReadToEnd();

        var parts = decrypted.Split('-', 2);
        if (parts.Length != 2)
        {
            response.SetMessage(MessageId.E99999);
            return response;
        }

        // Read the decrypted text
        response.Response = new DecryptTextIdAndDateTimeResponseEntity
        {
            DateTimeValue = DateTime.Parse(parts[0]),
            Email = parts[1].ToString()
        };

        // True
        response.Success = true;
        return response;
    }
}