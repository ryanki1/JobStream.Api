namespace JobStream.Api.Models;

/// <summary>
/// Payment structure for job postings with various payment types
/// </summary>
public class PaymentStructure
{
    /// <summary>
    /// Payment amount per completed Jira ticket
    /// </summary>
    public decimal PaymentPerTicket { get; set; }

    /// <summary>
    /// Fixed payment amount per completed sprint
    /// </summary>
    public decimal PaymentPerSprint { get; set; }

    /// <summary>
    /// Payment amount per completed milestone
    /// </summary>
    public decimal PaymentPerMilestone { get; set; }

    /// <summary>
    /// Percentage of payment released during sprint (via Zebec)
    /// Remaining percentage paid after validation
    /// Range: 0-100
    /// </summary>
    public byte PartPaymentPercentage { get; set; }
}
