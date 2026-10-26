namespace BrokerPortal.Domain.Entities;

public sealed class Agency
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DuckCreekAgencyId { get; set; }

    /// <summary>Comma-separated accepted email domains, e.g. "brokeragency.com,brokeragency.co.uk".</summary>
    public string AcceptedDomains { get; set; } = string.Empty;

    /// <summary>Comma-separated product codes the agency writes.</summary>
    public string Products { get; set; } = string.Empty;

    /// <summary>Maximum invited + active users. Null means unlimited (open question Q2).</summary>
    public int? MaxActiveUsers { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<string> GetDomains() =>
        AcceptedDomains.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(domain => domain.TrimStart('@').ToLowerInvariant())
            .ToList();
}
