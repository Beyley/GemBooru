using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GemBooru.Database;

[PrimaryKey(nameof(PostId))]
public class DbPost
{
     [Key]
     [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
     public int PostId { get; set; }
     
     public int Width { get; set; }
     public int Height { get; set; }
     
     public int FileSizeInBytes { get; set; }
     
     [MaxLength(256)]    
     public string? Source { get; set; }
     
     [ForeignKey(nameof(Uploader))]
     public int UploaderId { get; set; }
     public DbUser Uploader { get; set; }
     
     public DateTime UploadDate { get; set; }
     
     public PostType PostType { get; set; }
}