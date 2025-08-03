using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.ComponentModel;

namespace Longhl104.ShelterHub.Models.PostgreSql
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PetStatus
    {
        [Description("Available")]
        Available,
        [Description("Pending")]
        Pending,
        [Description("Adopted")]
        Adopted,
        [Description("MedicalHold")]
        MedicalHold
    }

    public class Pet
    {
        [Key]
        public int PetId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        public int SpeciesId { get; set; }

        public int? BreedId { get; set; }

        [Required]
        public int ShelterId { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsNeutered { get; set; }

        public bool IsVaccinated { get; set; }

        public bool IsGoodWithKids { get; set; }

        public bool IsGoodWithPets { get; set; }

        public PetStatus Status { get; set; } = PetStatus.Available;

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual PetSpecies Species { get; set; } = null!;
        public virtual PetBreed? Breed { get; set; }
        public virtual Shelter Shelter { get; set; } = null!;
    }

    public class PetSpecies
    {
        [Key]
        public int SpeciesId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Pet> Pets { get; set; } = [];
        public virtual ICollection<PetBreed> Breeds { get; set; } = [];
    }

    public class PetBreed
    {
        [Key]
        public int BreedId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int SpeciesId { get; set; }

        // Navigation properties
        public virtual PetSpecies Species { get; set; } = null!;
        public virtual ICollection<Pet> Pets { get; set; } = [];
    }
}
