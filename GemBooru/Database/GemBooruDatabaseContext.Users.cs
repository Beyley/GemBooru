namespace GemBooru.Database;

public partial class GemBooruDatabaseContext // Users
{
    public DbUser CreateOrGetUser(string certificateHash, string name)
    {
        var user = Users.FirstOrDefault(u => u.CertificateHash == certificateHash);
        if (user == null)
        {
            var newUser = Users.Add(new DbUser
            {
                CertificateHash = certificateHash,
                Name = name,
                Bio = "This user hasn't introduced themselves.",
            });

            SaveChanges();

            // Reload to load the user id of the new user
            newUser.Reload();

            return newUser.Entity;
        }

        return user;
    }

    public DbUser? GetUserById(int userId) => Users.FirstOrDefault(u => u.UserId == userId);
}