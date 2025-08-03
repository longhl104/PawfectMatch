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

        public int? AgeInMonths { get; set; }

        [MaxLength(20)]
        public string? Size { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        public bool IsNeutered { get; set; }

        public bool IsVaccinated { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public DateTime? DateAdopted { get; set; }

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

        [MaxLength(200)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
        public virtual ICollection<PetBreed> Breeds { get; set; } = new List<PetBreed>();
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

        [MaxLength(200)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual PetSpecies Species { get; set; } = null!;
        public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
    }
}
