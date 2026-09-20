using System.ComponentModel.DataAnnotations;
namespace GarageIntake.Web.Services;
public sealed record IntakeResult(string Summary, string Category, string[] Questions, string JobCard)
{
    public static readonly string[] Categories = ["Brakes", "Engine", "Starting", "Tyres / steering", "Other / unclear"];
    public IntakeResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Summary) || Summary.Length > 2000 ||
            !Categories.Contains(Category) || string.IsNullOrWhiteSpace(JobCard) || JobCard.Length > 5000 ||
            Questions is null || Questions.Length is < 1 or > 5 ||
            Questions.Any(q => string.IsNullOrWhiteSpace(q) || q.Length > 500))
            throw new AnalysisException("The analysis was incomplete. Please try again or prepare the job card manually.");
        return this;
    }
}
public static class IntakeInput
{
    public static string Validate(string? input)
    {
        var value = input?.Trim() ?? "";
        if (value.Length is < 10 or > 2000)
            throw new ValidationException("Please enter between 10 and 2,000 characters describing the fault.");
        return value;
    }
}
