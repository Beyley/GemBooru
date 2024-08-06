using Bunkum.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace GemBooru.Database;

public class GemBooruDatabaseContext : DbContext, IDatabaseContext
{
    private readonly GemBooruConfig _config;

    public GemBooruDatabaseContext()
    {
        this._config = new GemBooruConfig();
    }

    public GemBooruDatabaseContext(GemBooruConfig config)
    {
        _config = config;
    }

    private DbSet<DbPost> Posts { get; set; }
    private DbSet<DbTagRelation> TagRelations { get; set; }
    private DbSet<DbUser> Users { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        => optionsBuilder.UseNpgsql(_config.PostgresSqlConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTagRelation>()
            .HasIndex(p => new {p.Tag , p.PostId}).IsUnique();
        
        modelBuilder.Entity<DbPost>()  
            .Property(b => b.Processed)  
            .HasDefaultValue(true);
    }
    
    public int TotalPostCount() => Posts.Count();
    public int TotalUserCount() => Users.Count();

    public IQueryable<DbPost> GetAllPosts(int skip, int count) => Posts
        .OrderByDescending(p => p.UploadDate)
        .Where(p => p.Processed)
        .Skip(skip)
        .Take(count)
        .Include(p => p.Uploader);

    public IQueryable<DbPost> GetPostsByTag(int skip, int count, string tag) => TagRelations
        .Where(t => t.Tag == tag)
        .Include(t => t.Post)
        .Include(t => t.Post.Uploader)
        .Select(t => t.Post)
        .Where(p => p.Processed)
        .OrderByDescending(p => p.UploadDate)
        .Skip(skip)
        .Take(count);

    public IQueryable<DbPost> GetAllPostsByUser(int userId) => Posts
        .Where(p => p.UploaderId == userId)
        .OrderByDescending(p => p.UploadDate)
        .Include(p => p.Uploader);
    
    public IQueryable<DbPost> GetPostsByUser(int skip, int count, int userId) => Posts
        .Where(p => p.UploaderId == userId)
        .OrderByDescending(p => p.UploadDate)
        .Include(p => p.Uploader)
        .Skip(skip)
        .Take(count);
    
    public int GetTotalPostCount() => Posts.Count();

    public DbPost CreatePost(int uploaderId)
    {
        var post = Posts.Add(new DbPost
        {
            Width = 0,
            Height = 0,
            FileSizeInBytes = 0,
            UploaderId = uploaderId,
            UploadDate = DateTimeOffset.UtcNow,
            Source = null,
            Processed = false,
        });

        SaveChanges();
        
        // Reload to load the post ID
        post.Reload();
        
        return post.Entity;
    }

    public bool TagPost(DbPost post, string tag)
    {
        // Normalize the tag
        var normalizedTag = tag.ToLower().Replace('-', '_').Replace(' ', '_');

        // Block any too long tags
        if (tag.Length > DbTagRelation.MaxTagLength)
            return false;
        
        // Prevent the same tag from being applied twice
        if (this.TagRelations.Any(t => t.PostId == post.PostId && t.Tag == normalizedTag))
            return false;

        // Add the tag relation to the database
        Add(new DbTagRelation
        {
            Post = post,
            Tag = normalizedTag,
        });

        return true;
    }

    public IQueryable<DbTagRelation> GetTagsForPost(int postId) => TagRelations.Where(t => t.PostId == postId);
    public int GetTagCountFromPost(int postId) => TagRelations.Count(t => t.PostId == postId);
    
    public DbPost? GetPostById(int id) => Posts.Include(p => p.Uploader).FirstOrDefault(p => p.PostId == id);

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
    
    public override void Dispose()
    {
        SaveChanges();
        base.Dispose();
    }
}