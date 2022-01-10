using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SampleExchangeApi.Console.ListRequester;

public static class Sha256
{

    public static byte[] Hash(string value, IEnumerable<byte> salt)
    {
        return Hash(Encoding.UTF8.GetBytes(value), salt);
    }

    private static byte[] Hash(IEnumerable<byte> value, IEnumerable<byte> salt)
    {
        var saltedValue = value.Concat(salt).ToArray();

        return SHA256.Create().ComputeHash(saltedValue);
    }

    public static string ByteArrayToString(byte[] byteArray)
    {
        var hex = new StringBuilder(byteArray.Length * 2);
        foreach (var b in byteArray)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    public static byte[] StringToByteArray(string hexString)
    {
        return Enumerable.Range(0, hexString.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
            .ToArray();
    }
}
