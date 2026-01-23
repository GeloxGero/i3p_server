using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using i3p_server.Models.Enums;

namespace i3p_server.Models;

[Table("users")]
public class Users
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public required string Name { get; set; }

    [Required]
    [EmailAddress]
    [Column("email")]
    public required string Email { get; set; }

    [Required]
    [Column("password_hash")]
    public required string PasswordHash { get; set; } // Use 'Hash' to remind yourself not to store plain text

    [Column("authority")]
    public UserAuthority Authority { get; set; } = UserAuthority.NORMAL;
    
    [Column("photo_url")]
    public string? Photo { get; set; } // Stores path/URL to the image file

    [Column("date_created")]
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    [Column("date_updated")]
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}