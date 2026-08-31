using System;
using CsNetwork.Compression;
using CsNetwork.Framing;
using Xunit;

namespace CsNetwork.Tests.Framing;

public class BatchFuzzingTests
{
    [Fact]
    public void Random_Byte_Mutations_Never_Throw_Exceptions()
    {
        var rng = new Random(1337);
        byte[] fuzzBuffer = new byte[1024];

        for (int i = 0; i < 10000; i++)
        {
            int len = rng.Next(0, fuzzBuffer.Length);
            rng.NextBytes(fuzzBuffer.AsSpan(0, len));

            if (len > 0 && (i % 2 == 0))
            {
                fuzzBuffer[0] = BatchDecoder.BatchHeader;
            }

            bool success = BatchDecoder.TryDecodePooled(fuzzBuffer.AsMemory(0, len), out var result);
            if (success)
            {
                using (result)
                {
                    Assert.True(result.Count <= BatchDecoder.MaximumInBatch);
                }
            }
        }
    }

    [Fact]
    public void Bit_Flipping_On_Valid_Batch_Is_Handled_Safely()
    {
        var rng = new Random(999);

        SubPacketSlice[] packets = [
            new SubPacketSlice(new SubPacketHeader(5), new byte[100]),
            new SubPacketSlice(new SubPacketHeader(10), new byte[200])
        ];

        byte[] originalBatch = new byte[2048];
        Assert.True(BatchEncoder.TryEncodeBatch(packets, originalBatch, out int batchBytes, FlateCompressionCodec.Instance, 0));

        byte[] mutated = new byte[batchBytes];

        for (int i = 0; i < 5000; i++)
        {
            originalBatch.AsSpan(0, batchBytes).CopyTo(mutated);

            int flips = rng.Next(1, 6);
            for (int f = 0; f < flips; f++)
            {
                int index = rng.Next(0, batchBytes);
                mutated[index] ^= (byte)rng.Next(1, 256);
            }

            bool success = BatchDecoder.TryDecodePooled(mutated.AsMemory(0, batchBytes), out var result);
            if (success)
            {
                using (result)
                {
                    Assert.True(result.Count <= BatchDecoder.MaximumInBatch);
                }
            }
        }
    }
}
