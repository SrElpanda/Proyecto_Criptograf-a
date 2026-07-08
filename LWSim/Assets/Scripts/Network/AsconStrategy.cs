using UnityEngine;
using System;

/// <summary>
/// IMessageStrategy implementation using Ascon-128 AEAD.
/// 
/// Wire format: Base64( 16-byte nonce || ciphertext || 16-byte tag )
/// 
/// Key is configurable in the Inspector (16 bytes).
/// Nonce auto-increments on each encryption.
/// </summary>
[System.Serializable]
public class AsconStrategy : IMessageStrategy
{
    [Tooltip("16-byte encryption key (128-bit)")]
    public byte[] key = new byte[16]
    {
        0x00, 0x01, 0x02, 0x03,
        0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B,
        0x0C, 0x0D, 0x0E, 0x0F
    };

    [NonSerialized] private ulong nonceCounter = 0;

    public string Serialize(SensorMessage message)
    {
        string json = JsonUtility.ToJson(message);
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes(json);

        // Generate unique nonce
        byte[] nonce = AsconCipher.NonceFromCounter(nonceCounter++);

        // Encrypt
        var (ciphertext, tag) = AsconCipher.AEAD_Encrypt(
            key, nonce, plaintext);

        // Pack: nonce || ciphertext || tag
        byte[] wire = new byte[16 + ciphertext.Length + 16];
        Array.Copy(nonce, 0, wire, 0, 16);
        Array.Copy(ciphertext, 0, wire, 16, ciphertext.Length);
        Array.Copy(tag, 0, wire, 16 + ciphertext.Length, 16);

        return Convert.ToBase64String(wire);
    }

    public SensorMessage Deserialize(string data)
    {
        byte[] wire = Convert.FromBase64String(data);

        if (wire.Length < 32)
            throw new FormatException(
                "Ascon wire format too short (need nonce + tag + data).");

        // Unpack
        int ciphertextLen = wire.Length - 16 - 16;

        byte[] nonce = new byte[16];
        byte[] ciphertext = new byte[ciphertextLen];
        byte[] tag = new byte[16];

        Array.Copy(wire, 0, nonce, 0, 16);
        Array.Copy(wire, 16, ciphertext, 0, ciphertextLen);
        Array.Copy(wire, 16 + ciphertextLen, tag, 0, 16);

        // Decrypt
        byte[] plaintext = AsconCipher.AEAD_Decrypt(
            key, nonce, ciphertext, tag);

        string json = System.Text.Encoding.UTF8.GetString(plaintext);
        return JsonUtility.FromJson<SensorMessage>(json);
    }

    /// <summary>
    /// Set the encryption key at runtime.
    /// </summary>
    public void SetKey(byte[] newKey)
    {
        if (newKey == null || newKey.Length != 16)
            throw new ArgumentException("Key must be 16 bytes.");
        key = newKey;
    }
}
