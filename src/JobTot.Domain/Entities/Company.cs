namespace JobTot.Domain.Entities;

public sealed partial class Company
{
    private Company() { }

    public Company(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            throw new ArgumentException("Company name must contain 1-200 characters.");
        if (description.Length > 4000)
            throw new ArgumentException("Description exceeds 4000 characters.");
        Name = name.Trim();
        Description = description;
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
}
