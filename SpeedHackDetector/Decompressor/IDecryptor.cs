using System;

namespace SpeedHackDetector.Decompressor
{
    public interface IDecryptor
    {
        byte[] Decrypt(byte[] data);
        byte[] Decrypt(byte[] data, int len);
        string Description { get; }
    }
}
