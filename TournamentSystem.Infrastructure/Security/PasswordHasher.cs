using NSec.Cryptography;
using System.Security.Cryptography;

namespace TournamentSystem.Infrastructure.Security
{
    public static class PasswordHasher
    {
        private static readonly Argon2id argon2Id = new(new Argon2Parameters
        {
            MemorySize = 19 * 1024,  // 19 MiB en KiB
            NumberOfPasses = 2,
            DegreeOfParallelism = 1
        });

        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = argon2Id.DeriveBytes(password, salt, HashSize);

            return $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedPassword)
        {
            var parts = storedPassword.Split('$');
            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);
            var inputHash = argon2Id.DeriveBytes(password, salt, HashSize);

            return storedHash.SequenceEqual(inputHash);
        }
    }
}
