namespace GarageIntake.Web.Services;
public interface IIntakeAnalyzer
{
    string Mode { get; }
    Task<IntakeResult> AnalyzeAsync(string description, CancellationToken cancellationToken = default);
}
public sealed class AnalysisException(string message) : Exception(message);
