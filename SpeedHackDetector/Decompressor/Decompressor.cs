using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedHackDetector.Decompressor
{
    public static class Decompressor
    {
        public static byte[] Decompress(byte[] input, out int length)
        {

            Huffman h = new Huffman();
            return h.Decompress(input, out length);
        }
    }
}
