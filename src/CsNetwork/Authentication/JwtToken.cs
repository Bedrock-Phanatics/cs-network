using System;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace CsNetwork.Authentication;

public static class JwtToken
{
    public const int Es384SignatureLength = 96;

    public static bool TryParse(
        ReadOnlySpan<char> jwt,
        out ReadOnlySpan<char> headerBase64,
        out ReadOnlySpan<char> payloadBase64,
        out ReadOnlySpan<char> signatureBase64)
    {
        headerBase64 = default;
        payloadBase64 = default;
        signatureBase64 = default;

        int firstDot = jwt.IndexOf('.');
        if (firstDot <= 0)
            return false;

        ReadOnlySpan<char> remaining = jwt[(firstDot + 1)..];
        int secondDot = remaining.IndexOf('.');
        if (secondDot <= 0)
            return false;

        headerBase64 = jwt[..firstDot];
        payloadBase64 = remaining[..secondDot];
        signatureBase64 = remaining[(secondDot + 1)..];

        return !signatureBase64.IsEmpty;
    }

    public static bool TryVerifyEs384(
        ReadOnlySpan<char> jwt,
        ECDsa publicKey,
        [NotNullWhen(true)] out string? payloadJson)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        payloadJson = null;

        if (!TryParse(jwt, out ReadOnlySpan<char> headerSpan, out ReadOnlySpan<char> payloadSpan, out ReadOnlySpan<char> signatureSpan))
            return false;

        try
        {
            int signingCharCount = headerSpan.Length + 1 + payloadSpan.Length;
            ReadOnlySpan<char> signingInputChars = jwt[..signingCharCount];

            int utf8ByteCount = Encoding.UTF8.GetByteCount(signingInputChars);
            Span<byte> signingInputBytes = utf8ByteCount <= 512 ? stackalloc byte[utf8ByteCount] : new byte[utf8ByteCount];
            Encoding.UTF8.GetBytes(signingInputChars, signingInputBytes);

            Span<byte> signatureBytes = stackalloc byte[Es384SignatureLength];
            if (!Base64Url.TryDecodeFromChars(signatureSpan, signatureBytes, out int sigBytesWritten) || sigBytesWritten != Es384SignatureLength)
                return false;

            // verify es384 signature (ieee p1363: 48-byte r + 48-byte s)
            if (!publicKey.VerifyData(signingInputBytes, signatureBytes, HashAlgorithmName.SHA384, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                return false;

            int payloadByteCount = Base64Url.GetMaxDecodedLength(payloadSpan.Length);
            Span<byte> payloadBytes = payloadByteCount <= 2048 ? stackalloc byte[payloadByteCount] : new byte[payloadByteCount];

            if (!Base64Url.TryDecodeFromChars(payloadSpan, payloadBytes, out int payloadBytesWritten))
                return false;

            payloadJson = Encoding.UTF8.GetString(payloadBytes[..payloadBytesWritten]);
            return true;
        }
        catch
        {
            payloadJson = null;
            return false;
        }
    }

    public static string SignEs384(string headerJson, string payloadJson, ECDsa privateKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);

        string headerB64 = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(headerJson));
        string payloadB64 = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payloadJson));

        string signingInput = $"{headerB64}.{payloadB64}";
        byte[] signingBytes = Encoding.UTF8.GetBytes(signingInput);

        byte[] signature = privateKey.SignData(signingBytes, HashAlgorithmName.SHA384, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        string sigB64 = Base64Url.EncodeToString(signature);

        return $"{signingInput}.{sigB64}";
    }

    public static ECDsa ImportPublicKey(string base64Spki)
    {
        byte[] spki = Convert.FromBase64String(base64Spki);
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        ecdsa.ImportSubjectPublicKeyInfo(spki, out _);

        var ecParams = ecdsa.ExportParameters(false);
        if (ecParams.Q.X == null || ecParams.Q.X.Length != 48 || ecParams.Q.Y == null || ecParams.Q.Y.Length != 48)
        {
            ecdsa.Dispose();
            throw new CryptographicException("Imported key must be a valid NIST P-384 public key.");
        }

        return ecdsa;
    }

    public static string ExportPublicKey(ECDsa ecdsa)
    {
        ArgumentNullException.ThrowIfNull(ecdsa);
        byte[] spki = ecdsa.ExportSubjectPublicKeyInfo();
        return Convert.ToBase64String(spki);
    }
}
