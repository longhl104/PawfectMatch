using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

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

    // PostGIS spatial data for location-based queries
    /// <summary>
    /// Geographic location point (longitude, latitude) using PostGIS geometry
    /// SRID 4326 (WGS84) for GPS coordinates
    /// </summary>
    public Point? Location { get; set; }

    /// <summary>
    /// Latitude in decimal degrees (for easier API usage)
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude in decimal degrees (for easier API usage)
    /// </summary>
    public double? Longitude { get; set; }
}
