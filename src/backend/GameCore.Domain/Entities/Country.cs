namespace GameCore.Domain.Entities;

public sealed class Country
{
    public int CountryId { get; set; }
    public string Iso2 { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Distribution> Distributions { get; set; } = new List<Distribution>();
}
