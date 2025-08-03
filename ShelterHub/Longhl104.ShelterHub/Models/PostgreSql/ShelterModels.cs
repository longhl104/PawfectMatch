using System.ComponentModel.DataAnnotations;

namespace Longhl104.ShelterHub.Models.PostgreSql;

public class Shelter
{
    [Key]
    public int ShelterId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string ShelterName { get; set; }

    [Required]
    [MaxLength(15)]
    public required string ShelterContactNumber { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ShelterAddress { get; set; }
}
