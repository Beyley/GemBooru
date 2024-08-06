using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GemBooru.Database;

[PrimaryKey(nameof(TagRelationId))]
public class DbTagRelation
{
    public const int MaxTagLength = 128;
    
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TagRelationId { get; set; }
    
    [ForeignKey(nameof(Post))]
    public int PostId { get; set; }
    public DbPost Post { get; set; }
    
    [MaxLength(MaxTagLength)]
    public string Tag { get; set; }
}