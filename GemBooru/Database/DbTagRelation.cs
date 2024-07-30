using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GemBooru.Database;

[PrimaryKey(nameof(PostId))]
public class DbTagRelation
{
    [Key]
    public int PostId { get; set; }
    [Key]
    public string Tag { get; set; }
}