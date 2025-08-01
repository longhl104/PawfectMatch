namespace Longhl104.ShelterHub.Models.PostgreSql;

public class Shelter
{
    public required int ShelterId { get; set; }
    public required string ShelterName { get; set; }
    public required string ShelterContactNumber { get; set; }
    public required string ShelterAddress { get; set; }
}
