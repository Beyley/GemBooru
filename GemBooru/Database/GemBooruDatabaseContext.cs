using Bunkum.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GemBooru.Database;

public class GemBooruDatabaseContext(GemBooruConfig config) : DbContext, IDatabaseContext
{
    private DbSet<DbPost> Posts { get; set; }
    private DbSet<DbTagRelation> TagRelations { get; set; }
    private DbSet<DbUser> Users { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        => optionsBuilder.UseNpgsql(config.PostgresSqlConnectionString);

    public int TotalPostCount() => Posts.Count();
    public int TotalUserCount() => Users.Count();

    public IQueryable<DbPost> GetPosts(int skip, int count) => Posts.Skip(skip).Take(count).Include(p => p.Uploader);

    public DbPost CreatePost(int uploaderId)
    {
        var post = Posts.Add(new DbPost
        {
            Width = 0,
            Height = 0,
            FileSizeInBytes = 0,
            UploaderId = uploaderId,
            UploadDate = DateTime.UtcNow,
            Source = null,
        });

        SaveChanges();
        
        // Reload to load the post ID
        post.Reload();
        
        return post.Entity;
    }

    public void DeletePost(int postId)
    {
        // Remove all the tag relations
        TagRelations.RemoveRange(TagRelations.Where(t => t.PostId == postId));
        
        // Remove the post from the database
        Posts.Remove(Posts.Find(postId)!);
    }

    public DbUser CreateOrGetUser(string certificateHash, string name)
    {
        var user = Users.FirstOrDefault(u => u.CertificateHash == certificateHash);
        if (user == null)
        {
            var newUser = Users.Add(new DbUser
            {
                CertificateHash = certificateHash,
                Name = name,
            });

            SaveChanges();
            
            // Reload to load the user id of the new user
            newUser.Reload();
            
            return newUser.Entity;
        }

        return user;
    }

    public DbUser? GetUserById(int userId) => Users.FirstOrDefault(u => u.UserId == userId);
    
    public override void Dispose()
    {
        SaveChanges();
        base.Dispose();
    }
}