using System.ComponentModel.DataAnnotations;

namespace Longhl104.ShelterHub.Models.PostgreSql
{
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

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0, 10000)]
        public decimal AdoptionFee { get; set; } = 0;

        public bool IsSpayedNeutered { get; set; }

        public bool IsVaccinated { get; set; }

        public bool IsGoodWithKids { get; set; }

        public bool IsGoodWithPets { get; set; }

        public bool IsHouseTrained { get; set; }

        public bool IsMicrochipped { get; set; }

        public PetStatus Status { get; set; } = PetStatus.Available;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? MainImageFileExtension { get; set; }

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
