using System;

/// <summary>
/// Ascon-128 AEAD — Pure C# implementation adapted from CSAscon v1.3.
///
/// Parameters:
///   Key:    16 bytes (128-bit)
///   Nonce:  16 bytes (128-bit)
///   Tag:    16 bytes (128-bit)
///   Rate:   16 bytes (128-bit blocks)
///   Init/Final rounds: 12
///   Intermediate rounds: 8
/// </summary>
public static class AsconCipher
{
    // ──────────────── Parameters ────────────────

    public const int KEY_BYTES = 16;
    public const int NONCE_BYTES = 16;
    public const int TAG_BYTES = 16;
    public const int AEAD_RATE = 16;

    private const int ASCON_KEYWORDS = 2;

    // Round constants
    private const byte RC0  = 0xf0;
    private const byte RC1  = 0xe1;
    private const byte RC2  = 0xd2;
    private const byte RC3  = 0xc3;
    private const byte RC4  = 0xb4;
    private const byte RC5  = 0xa5;
    private const byte RC6  = 0x96;
    private const byte RC7  = 0x87;
    private const byte RC8  = 0x78;
    private const byte RC9  = 0x69;
    private const byte RCa  = 0x5a;
    private const byte RCb  = 0x4b;

    // IV for Ascon-128
    private const ulong ASCON_128A_IV = 0x00001000808c0001UL;

    // ──────────────── Public API ────────────────

    /// <summary>
    /// Ascon-128 AEAD encryption.
    /// Returns (ciphertext, 16-byte tag).
    /// </summary>
    public static (byte[] ciphertext, byte[] tag) AEAD_Encrypt(
        byte[] key,
        byte[] nonce,
        byte[] plaintext,
        byte[] associatedData = null)
    {
        ValidateKey(key);
        ValidateNonce(nonce);

        ulong[] s = new ulong[5];
        ulong[] k = LoadKey(key);

        // ── Initialize ──
        s[0] = ASCON_128A_IV;
        s[1] = k[0];
        s[2] = k[1];
        s[3] = BytesToULongLE(nonce, 0);
        s[4] = BytesToULongLE(nonce, 8);
        P12(s);
        s[3] ^= k[0];
        s[4] ^= k[1];

        // ── Associated data ──
        if (associatedData != null && associatedData.Length > 0)
        {
            ProcessAD(s, associatedData);
        }

        // Domain separation
        s[4] ^= 0x80UL << 56;

        // ── Encrypt ──
        byte[] ciphertext = new byte[plaintext.Length];
        int cOffset = 0;
        int mOffset = 0;

        while (plaintext.Length - mOffset >= AEAD_RATE)
        {
            s[0] ^= BytesToULongLE(plaintext, mOffset);
            ULongToBytesLE(s[0], ciphertext, cOffset);

            s[1] ^= BytesToULongLE(plaintext, mOffset + 8);
            ULongToBytesLE(s[1], ciphertext, cOffset + 8);

            P8(s);
            mOffset += AEAD_RATE;
            cOffset += AEAD_RATE;
        }

        // Final partial block
        int pxIndex = 0;
        if (plaintext.Length - mOffset >= 8)
        {
            s[0] ^= BytesToULongLE(plaintext, mOffset);
            ULongToBytesLE(s[0], ciphertext, cOffset);
            pxIndex = 1;
            mOffset += 8;
            cOffset += 8;
        }

        int remaining = plaintext.Length - mOffset;
        s[pxIndex] ^= PAD(remaining);
        if (remaining > 0)
        {
            ulong lastBlock = BytesToULongLEPartial(plaintext, mOffset, remaining);
            s[pxIndex] ^= lastBlock;
            ULongToBytesLEPartial(s[pxIndex], ciphertext, cOffset, remaining);
        }

        // ── Finalize ──
        s[2] ^= k[0];
        s[3] ^= k[1];
        P12(s);
        s[3] ^= k[0];
        s[4] ^= k[1];

        byte[] tag = new byte[16];
        ULongToBytesLE(s[3], tag, 0);
        ULongToBytesLE(s[4], tag, 8);

        return (ciphertext, tag);
    }

    /// <summary>
    /// Ascon-128 AEAD decryption.
    /// Returns plaintext or throws if tag doesn't match.
    /// </summary>
    public static byte[] AEAD_Decrypt(
        byte[] key,
        byte[] nonce,
        byte[] ciphertext,
        byte[] tag)
    {
        ValidateKey(key);
        ValidateNonce(nonce);
        if (tag == null || tag.Length != TAG_BYTES)
            throw new ArgumentException("Tag must be 16 bytes.");

        ulong[] s = new ulong[5];
        ulong[] k = LoadKey(key);

        // ── Initialize ──
        s[0] = ASCON_128A_IV;
        s[1] = k[0];
        s[2] = k[1];
        s[3] = BytesToULongLE(nonce, 0);
        s[4] = BytesToULongLE(nonce, 8);
        P12(s);
        s[3] ^= k[0];
        s[4] ^= k[1];

        // ── Associated data (empty) ──
        // Domain separation
        s[4] ^= 0x80UL << 56;

        // ── Decrypt ──
        byte[] plaintext = new byte[ciphertext.Length];
        int cOffset = 0;
        int pOffset = 0;

        while (ciphertext.Length - cOffset >= AEAD_RATE)
        {
            ulong cx = BytesToULongLE(ciphertext, cOffset);
            ulong px = s[0] ^ cx;
            ULongToBytesLE(px, plaintext, pOffset);
            s[0] = cx;

            cx = BytesToULongLE(ciphertext, cOffset + 8);
            px = s[1] ^ cx;
            ULongToBytesLE(px, plaintext, pOffset + 8);
            s[1] = cx;

            P8(s);
            cOffset += AEAD_RATE;
            pOffset += AEAD_RATE;
        }

        // Final partial block
        int pxIndex = 0;
        if (ciphertext.Length - cOffset >= 8)
        {
            ulong cx = BytesToULongLE(ciphertext, cOffset);
            ulong px = s[0] ^ cx;
            ULongToBytesLE(px, plaintext, pOffset);
            s[0] = cx;
            pxIndex = 1;
            cOffset += 8;
            pOffset += 8;
        }

        int remaining = ciphertext.Length - cOffset;
        s[pxIndex] ^= PAD(remaining);
        if (remaining > 0)
        {
            ulong cx = BytesToULongLEPartial(ciphertext, cOffset, remaining);
            s[pxIndex] ^= cx;
            ULongToBytesLEPartial(s[pxIndex], plaintext, pOffset, remaining);
            s[pxIndex] = CLEAR(s[pxIndex], remaining);
            s[pxIndex] ^= cx;
        }

        // ── Finalize ──
        s[2] ^= k[0];
        s[3] ^= k[1];
        P12(s);
        s[3] ^= k[0];
        s[4] ^= k[1];

        // Verify tag
        ulong t0 = s[3] ^ BytesToULongLE(tag, 0);
        ulong t1 = s[4] ^ BytesToULongLE(tag, 8);

        if (NOTZERO(t0, t1))
            throw new InvalidOperationException(
                "Ascon AEAD tag verification failed — data may be tampered.");

        return plaintext;
    }

    // ──────────────── Nonce Utility ────────────────

    public static byte[] NonceFromCounter(ulong counter)
    {
        byte[] nonce = new byte[16];
        ULongToBytesLE(counter, nonce, 8);
        return nonce;
    }

    // ──────────────── Internal: AD ────────────────

    private static void ProcessAD(ulong[] s, byte[] ad)
    {
        int offset = 0;

        while (ad.Length - offset >= AEAD_RATE)
        {
            s[0] ^= BytesToULongLE(ad, offset);
            s[1] ^= BytesToULongLE(ad, offset + 8);
            P8(s);
            offset += AEAD_RATE;
        }

        int pxIndex = 0;
        if (ad.Length - offset >= 8)
        {
            s[0] ^= BytesToULongLE(ad, offset);
            pxIndex = 1;
            offset += 8;
        }

        int remaining = ad.Length - offset;
        s[pxIndex] ^= PAD(remaining);
        if (remaining > 0)
        {
            s[pxIndex] ^= BytesToULongLEPartial(ad, offset, remaining);
        }
        P8(s);
    }

    // ──────────────── Internal: Key ────────────────

    private static ulong[] LoadKey(byte[] key)
    {
        return new ulong[]
        {
            BytesToULongLE(key, 0),
            BytesToULongLE(key, 8)
        };
    }

    // ──────────────── Internal: Permutation ────────────────

    private static void P12(ulong[] s)
    {
        Round(s, RC0);  Round(s, RC1);  Round(s, RC2);
        Round(s, RC3);  Round(s, RC4);  Round(s, RC5);
        Round(s, RC6);  Round(s, RC7);  Round(s, RC8);
        Round(s, RC9);  Round(s, RCa);  Round(s, RCb);
    }

    private static void P8(ulong[] s)
    {
        Round(s, RC4);  Round(s, RC5);  Round(s, RC6);
        Round(s, RC7);  Round(s, RC8);  Round(s, RC9);
        Round(s, RCa);  Round(s, RCb);
    }

    private static void Round(ulong[] s, byte C)
    {
        ulong[] t = new ulong[5];

        s[2] ^= C;

        // S-box
        s[0] ^= s[4]; s[4] ^= s[3]; s[2] ^= s[1];

        t[0] = s[0] ^ (~s[1] & s[2]);
        t[2] = s[2] ^ (~s[3] & s[4]);
        t[4] = s[4] ^ (~s[0] & s[1]);
        t[1] = s[1] ^ (~s[2] & s[3]);
        t[3] = s[3] ^ (~s[4] & s[0]);

        t[1] ^= t[0]; t[3] ^= t[2]; t[0] ^= t[4];

        // Linear layer
        s[2] = t[2] ^ Rot64(t[2], 6 - 1);
        s[3] = t[3] ^ Rot64(t[3], 17 - 10);
        s[4] = t[4] ^ Rot64(t[4], 41 - 7);
        s[0] = t[0] ^ Rot64(t[0], 28 - 19);
        s[1] = t[1] ^ Rot64(t[1], 61 - 39);

        s[2] = t[2] ^ Rot64(s[2], 1);
        s[3] = t[3] ^ Rot64(s[3], 10);
        s[4] = t[4] ^ Rot64(s[4], 7);
        s[0] = t[0] ^ Rot64(s[0], 19);
        s[1] = t[1] ^ Rot64(s[1], 39);

        s[2] = ~s[2];
    }

    // ──────────────── Helpers ────────────────

    private static ulong Rot64(ulong x, int n)
    {
        return (x << n) | (x >> (64 - n));
    }

    private static ulong PAD(int i)
    {
        return 0x01UL << (8 * i);
    }

    private static ulong CLEAR(ulong w, int n)
    {
        ulong mask = ~0UL << (8 * n);
        return w & mask;
    }

    private static bool NOTZERO(ulong a, ulong b)
    {
        ulong result = a | b;
        result |= result >> 32;
        result |= result >> 16;
        result |= result >> 8;
        int r = (byte)(result & 0xff);
        return r != 0;
    }

    // ──────────────── Byte Helpers (Little-Endian) ────────────────

    private static ulong BytesToULongLE(byte[] bytes, int offset)
    {
        return (ulong)bytes[offset] |
               ((ulong)bytes[offset + 1] << 8) |
               ((ulong)bytes[offset + 2] << 16) |
               ((ulong)bytes[offset + 3] << 24) |
               ((ulong)bytes[offset + 4] << 32) |
               ((ulong)bytes[offset + 5] << 40) |
               ((ulong)bytes[offset + 6] << 48) |
               ((ulong)bytes[offset + 7] << 56);
    }

    private static ulong BytesToULongLEPartial(byte[] bytes, int offset, int n)
    {
        ulong result = 0;
        for (int i = 0; i < n; i++)
        {
            result |= (ulong)bytes[offset + i] << (8 * i);
        }
        return result;
    }

    private static void ULongToBytesLE(ulong value, byte[] bytes, int offset)
    {
        bytes[offset]     = (byte)(value);
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
        bytes[offset + 4] = (byte)(value >> 32);
        bytes[offset + 5] = (byte)(value >> 40);
        bytes[offset + 6] = (byte)(value >> 48);
        bytes[offset + 7] = (byte)(value >> 56);
    }

    private static void ULongToBytesLEPartial(ulong value, byte[] bytes, int offset, int n)
    {
        for (int i = 0; i < n; i++)
        {
            bytes[offset + i] = (byte)(value >> (8 * i));
        }
    }

    // ──────────────── Validation ────────────────

    private static void ValidateKey(byte[] key)
    {
        if (key == null || key.Length != KEY_BYTES)
            throw new ArgumentException("Key must be 16 bytes (128-bit).");
    }

    private static void ValidateNonce(byte[] nonce)
    {
        if (nonce == null || nonce.Length != NONCE_BYTES)
            throw new ArgumentException("Nonce must be 16 bytes (128-bit).");
    }
}
