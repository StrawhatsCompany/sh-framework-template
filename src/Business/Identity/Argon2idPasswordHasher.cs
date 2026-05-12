using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Business.Identity;

/// <summary>
/// Argon2id password hashing. Hash format:
///     $argon2id$v=19$m=&lt;memKiB&gt;,t=&lt;iterations&gt;,p=&lt;parallelism&gt;$&lt;saltB64&gt;$&lt;hashB64&gt;
/// Parameters are persisted in the hash so we can change defaults later without breaking
/// existing passwords (Verify reads the stored parameters; new hashes use the current defaults).
/// </summary>
internal sealed class Argon2idPasswordHasher : IPasswordHasher
{
    // Sensible 2026 defaults. RFC 9106 recommends m=2 GiB / t=1 / p=4 for high security,
    // but server-side login concurrency benefits from lower memory + more iterations.
    private const int DefaultMemoryKiB = 65_536;   // 64 MiB
    private const int DefaultIterations = 3;
    private const int DefaultParallelism = 4;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Compute(password, salt, DefaultMemoryKiB, DefaultIterations, DefaultParallelism);

        return $"$argon2id$v=19$m={DefaultMemoryKiB},t={DefaultIterations},p={DefaultParallelism}$" +
               $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;

        var parts = hash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5) return false;
        if (parts[0] != "argon2id") return false;
        if (parts[1] != "v=19") return false;

        if (!TryParseParameters(parts[2], out var m, out var t, out var p)) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException) { return false; }

        var actual = Compute(password, salt, m, t, p);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Compute(string password, byte[] salt, int memoryKiB, int iterations, int parallelism)
    {
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKiB,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        return argon.GetBytes(HashLength);
    }

    private static bool TryParseParameters(string segment, out int memoryKiB, out int iterations, out int parallelism)
    {
        memoryKiB = iterations = parallelism = 0;
        foreach (var kv in segment.Split(','))
        {
            var pair = kv.Split('=');
            if (pair.Length != 2) return false;
            if (!int.TryParse(pair[1], out var value)) return false;
            switch (pair[0])
            {
                case "m": memoryKiB = value; break;
                case "t": iterations = value; break;
                case "p": parallelism = value; break;
                default: return false;
            }
        }
        return memoryKiB > 0 && iterations > 0 && parallelism > 0;
    }
}
