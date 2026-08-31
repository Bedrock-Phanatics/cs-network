using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace CsNetwork.Cryptography;

public sealed class EcdhKeyPair : IDisposable
{
    public const int UncompressedPublicKeyLength = 97;
    public const int CoordinateLength = 48;
    public const int SharedSecretLength = 48;

    private readonly ECDiffieHellman _ecdh;
    private bool _disposed;

    private EcdhKeyPair(ECDiffieHellman ecdh)
    {
        _ecdh = ecdh;
    }

    public static EcdhKeyPair Create()
    {
        var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
        return new EcdhKeyPair(ecdh);
    }

    public static bool TryImportPrivateKey(ReadOnlySpan<byte> privateKeyD, ReadOnlySpan<byte> uncompressedPublicKey, [NotNullWhen(true)] out EcdhKeyPair? keyPair)
    {
        keyPair = null;
        if (privateKeyD.Length != CoordinateLength || uncompressedPublicKey.Length != UncompressedPublicKeyLength || uncompressedPublicKey[0] != 0x04)
        {
            return false;
        }

        try
        {
            var ecParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP384,
                D = privateKeyD.ToArray(),
                Q = new ECPoint
                {
                    X = uncompressedPublicKey.Slice(1, CoordinateLength).ToArray(),
                    Y = uncompressedPublicKey.Slice(1 + CoordinateLength, CoordinateLength).ToArray()
                }
            };

            var ecdh = ECDiffieHellman.Create(ecParams);
            keyPair = new EcdhKeyPair(ecdh);
            return true;
        }
        catch
        {
            keyPair = null;
            return false;
        }
    }

    public void ExportUncompressedPublicKey(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (destination.Length < UncompressedPublicKeyLength)
        {
            throw new ArgumentException("Destination span is too small for uncompressed P-384 public key.", nameof(destination));
        }

        var parameters = _ecdh.ExportParameters(includePrivateParameters: false);
        byte[]? x = parameters.Q.X;
        byte[]? y = parameters.Q.Y;

        if (x == null || y == null)
        {
            throw new CryptographicException("Failed to export ECDH public key coordinates.");
        }

        destination[0] = 0x04;

        Span<byte> xDest = destination.Slice(1, CoordinateLength);
        xDest.Clear();
        int xOffset = CoordinateLength - x.Length;
        if (xOffset >= 0)
        {
            x.CopyTo(xDest[xOffset..]);
        }
        else
        {
            x.AsSpan(-xOffset).CopyTo(xDest);
        }

        Span<byte> yDest = destination.Slice(1 + CoordinateLength, CoordinateLength);
        yDest.Clear();
        int yOffset = CoordinateLength - y.Length;
        if (yOffset >= 0)
        {
            y.CopyTo(yDest[yOffset..]);
        }
        else
        {
            y.AsSpan(-yOffset).CopyTo(yDest);
        }
    }

    public byte[] GetUncompressedPublicKey()
    {
        byte[] buffer = new byte[UncompressedPublicKeyLength];
        ExportUncompressedPublicKey(buffer);
        return buffer;
    }

    public bool TryDeriveSharedSecret(ReadOnlySpan<byte> peerPublicKeyBytes, Span<byte> destination, out int bytesWritten)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        bytesWritten = 0;

        if (peerPublicKeyBytes.Length != UncompressedPublicKeyLength || peerPublicKeyBytes[0] != 0x04 || destination.Length < SharedSecretLength)
        {
            return false;
        }

        try
        {
            var ecParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP384,
                Q = new ECPoint
                {
                    X = peerPublicKeyBytes.Slice(1, CoordinateLength).ToArray(),
                    Y = peerPublicKeyBytes.Slice(1 + CoordinateLength, CoordinateLength).ToArray()
                }
            };

            using var remoteEcdh = ECDiffieHellman.Create(ecParams);
            byte[] secret = _ecdh.DeriveRawSecretAgreement(remoteEcdh.PublicKey);

            if (secret.Length != SharedSecretLength)
            {
                Span<byte> aligned = stackalloc byte[SharedSecretLength];
                aligned.Clear();
                int offset = SharedSecretLength - secret.Length;
                if (offset >= 0)
                {
                    secret.CopyTo(aligned[offset..]);
                }
                else
                {
                    secret.AsSpan(-offset).CopyTo(aligned);
                }

                aligned.CopyTo(destination);
                CryptographicOperations.ZeroMemory(aligned);
            }
            else
            {
                secret.CopyTo(destination);
            }

            CryptographicOperations.ZeroMemory(secret);
            bytesWritten = SharedSecretLength;
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _ecdh.Dispose();
            _disposed = true;
        }
    }
}
