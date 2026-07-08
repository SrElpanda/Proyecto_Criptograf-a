using UnityEngine;
using System;
using System.Text;

public class XORStrategy : IMessageStrategy
{
    private static readonly string Key = "LWSimKey";

    public string Serialize(SensorMessage message)
    {
        string json = JsonUtility.ToJson(message);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        byte[] encrypted = XORBytes(jsonBytes, Key);
        return Convert.ToBase64String(encrypted);
    }

    public SensorMessage Deserialize(string data)
    {
        byte[] encrypted = Convert.FromBase64String(data);
        byte[] decrypted = XORBytes(encrypted, Key);
        string json = Encoding.UTF8.GetString(decrypted);
        return JsonUtility.FromJson<SensorMessage>(json);
    }

    private byte[] XORBytes(byte[] input, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] output = new byte[input.Length];

        for (int i = 0; i < input.Length; i++)
        {
            output[i] = (byte)(input[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return output;
    }
}
