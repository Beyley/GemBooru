using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GemBooru.Database;

[PrimaryKey(nameof(UserId))]
public class DbUser
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }
    
    [Key]
    [MaxLength(64)]
    public string CertificateHash { get; set; }
    
    [MaxLength(64)]
    public string Name { get; set; }
}