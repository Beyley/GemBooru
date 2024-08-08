using Microsoft.EntityFrameworkCore;

namespace GemBooru.Database;

public partial class GemBooruDatabaseContext // Posts
{
    public int TotalPostCount() => Posts.Count();
    public int TotalUserCount() => Users.Count();

    public IQueryable<DbPost> GetRecentPosts() => Posts
        .OrderByDescending(p => p.UploadDate)
        .Where(p => p.Processed)
        .Include(p => p.Uploader);

    public IQueryable<DbPost> GetPostsByTag(string tag) => TagRelations
        .Where(t => t.Tag == tag)
        .Include(t => t.Post)
        .Include(t => t.Post.Uploader)
        .Select(t => t.Post)
        .Where(p => p.Processed)
        .OrderByDescending(p => p.UploadDate);
    
    public IQueryable<DbPost> GetPostsByUser(int userId) => Posts
        .Where(p => p.UploaderId == userId)
        .OrderByDescending(p => p.UploadDate)
        .Include(p => p.Uploader);

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
}