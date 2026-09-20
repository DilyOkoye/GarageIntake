using System.Text.RegularExpressions;
namespace GarageIntake.Web.Services;

// Intentionally transparent, deterministic demo. This is not a diagnostic engine.
public sealed class DemoIntakeAnalyzer : IIntakeAnalyzer
{
    public string Mode => "Demo";
    public Task<IntakeResult> AnalyzeAsync(string description, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = IntakeInput.Validate(description);
        var matches = new List<string>();
        if (Regex.IsMatch(text, @"\b(brakes?|braking)\b", RegexOptions.IgnoreCase)) matches.Add("Brakes");
        if (Regex.IsMatch(text, @"\b(engine|overheating|smoke)\b", RegexOptions.IgnoreCase)) matches.Add("Engine");
        if (Regex.IsMatch(text, @"\b(start|starting|battery)\b", RegexOptions.IgnoreCase)) matches.Add("Starting");
        if (Regex.IsMatch(text, @"\b(tyres?|tires?|steering)\b", RegexOptions.IgnoreCase)) matches.Add("Tyres / steering");
        var category = matches.Count == 1 ? matches[0] : "Other / unclear";
        string[] questions = category switch
        {
            "Brakes" => ["Have you noticed a change in braking performance?", "When does the noise or symptom occur?", "Are any dashboard warning lights showing?"],
            "Engine" => ["Which warning lights or messages are showing?", "When does the symptom occur?", "When did you first notice it?"],
            "Starting" => ["Does the engine turn over when you try to start it?", "Do dashboard lights come on?", "Is this intermittent or does it happen every time?"],
            "Tyres / steering" => ["When do you notice the symptom?", "Have you noticed visible tyre damage?", "Has steering feel changed?"],
            _ => ["What happens, and when does it happen?", "When did you first notice it?", "Are any warning lights or changes in vehicle behaviour present?"]
        };
        var result = new IntakeResult(text, category, questions,
            $"CUSTOMER REPORT\n{text}\n\nSUGGESTED INTAKE CATEGORY\n{category} (confirm with adviser)\n\nFOLLOW-UP QUESTIONS\n" +
            string.Join("\n", questions.Select(q => "• " + q)) +
            "\n\nADVISER REVIEW\nConfirm details with the customer and assess next steps. No diagnosis or assurance of roadworthiness has been made.");
        return Task.FromResult(result.Validate());
    }
}
