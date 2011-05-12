using System;

namespace SpeedHackDetector.Decompressor
{
    public interface IEncryptor
    {
        byte[] Encrypt(byte[] data);
        byte[] Encrypt(byte[] data, int len);
        string Description { get; }
    }
}
