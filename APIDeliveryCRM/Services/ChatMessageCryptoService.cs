using System.Security.Cryptography;
using System.Text;
using APIDeliveryCRM.Interfaces;

namespace APIDeliveryCRM.Services;

public sealed class ChatMessageCryptoService : IChatMessageCryptoService
{
    private const string PrefixV1 = "enc:v1:";
    private const string PrefixV2 = "enc:v2:";
    private readonly Dictionary<string, byte[]> _keyRing = new(StringComparer.Ordinal);
    private readonly byte[] _legacyV1Key;
    private readonly string _activeKeyId;

    public ChatMessageCryptoService(IConfiguration configuration)
    {
        var keysSection = configuration.GetSection("ChatEncryption:Keys");
        foreach (var child in keysSection.GetChildren())
        {
            if (string.IsNullOrWhiteSpace(child.Key) || string.IsNullOrWhiteSpace(child.Value))
                continue;
            var keyBytes = Convert.FromBase64String(child.Value);
            if (keyBytes.Length != 32)
                throw new InvalidOperationException($"ChatEncryption:Keys:{child.Key} must be base64 for 32 bytes.");
            _keyRing[child.Key] = keyBytes;
        }

        var legacyV1Base64 = configuration["ChatEncryption:LegacyV1Key"];
        if (!string.IsNullOrWhiteSpace(legacyV1Base64))
        {
            var parsed = Convert.FromBase64String(legacyV1Base64);
            if (parsed.Length != 32)
                throw new InvalidOperationException("ChatEncryption:LegacyV1Key must be base64 for 32 bytes.");
            _legacyV1Key = parsed;
            return;
        }

        var fallback = configuration["Jwt:Key"] ?? "deliverycrm-chat-encryption-fallback";
        _legacyV1Key = SHA256.HashData(Encoding.UTF8.GetBytes(fallback));

        _activeKeyId = configuration["ChatEncryption:ActiveKeyId"] ?? string.Empty;
        if (_keyRing.Count == 0)
        {
            const string fallbackKeyId = "fallback";
            _keyRing[fallbackKeyId] = _legacyV1Key;
            _activeKeyId = fallbackKeyId;
            return;
        }

        if (string.IsNullOrWhiteSpace(_activeKeyId) || !_keyRing.ContainsKey(_activeKeyId))
            _activeKeyId = _keyRing.Keys.First();
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;
        if (plaintext.StartsWith(PrefixV2, StringComparison.Ordinal) || plaintext.StartsWith(PrefixV1, StringComparison.Ordinal))
            return plaintext;

        var iv = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_keyRing[_activeKeyId], 16);
        aes.Encrypt(iv, plainBytes, cipherBytes, tag);

        var ivB64 = Convert.ToBase64String(iv);
        var cipherAndTag = new byte[cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(cipherBytes, 0, cipherAndTag, 0, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, cipherAndTag, cipherBytes.Length, tag.Length);
        var cipherB64 = Convert.ToBase64String(cipherAndTag);
        return $"{PrefixV2}{_activeKeyId}:{ivB64}:{cipherB64}";
    }

    public string Decrypt(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return payload;

        try
        {
            if (payload.StartsWith(PrefixV2, StringComparison.Ordinal))
            {
                var content = payload[PrefixV2.Length..];
                var split = content.Split(':', 3);
                if (split.Length != 3)
                    return payload;
                var keyId = split[0];
                if (!_keyRing.TryGetValue(keyId, out var key))
                    return payload;
                return DecryptCore(split[1], split[2], key, payload);
            }

            if (payload.StartsWith(PrefixV1, StringComparison.Ordinal))
            {
                var content = payload[PrefixV1.Length..];
                var split = content.Split(':', 2);
                if (split.Length != 2)
                    return payload;
                return DecryptCore(split[0], split[1], _legacyV1Key, payload);
            }

            return payload;
        }
        catch
        {
            return payload;
        }
    }

    private static string DecryptCore(string ivB64, string cipherB64, byte[] key, string fallbackPayload)
    {
        try
        {
            var iv = Convert.FromBase64String(ivB64);
            var cipherAndTag = Convert.FromBase64String(cipherB64);
            if (iv.Length != 12 || cipherAndTag.Length < 17)
                return fallbackPayload;

            var cipherLen = cipherAndTag.Length - 16;
            var cipher = new byte[cipherLen];
            var tag = new byte[16];
            Buffer.BlockCopy(cipherAndTag, 0, cipher, 0, cipherLen);
            Buffer.BlockCopy(cipherAndTag, cipherLen, tag, 0, 16);

            var plain = new byte[cipherLen];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(iv, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return fallbackPayload;
        }
    }
}
