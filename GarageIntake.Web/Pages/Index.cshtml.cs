using System.ComponentModel.DataAnnotations;
using GarageIntake.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace GarageIntake.Web.Pages;
[RequestSizeLimit(32768)]
public sealed class IndexModel(IIntakeAnalyzer analyzer) : PageModel
{
    [BindProperty] public string Description { get; set; } = "";
    public string Mode => analyzer.Mode;
    public IntakeResult? Result { get; private set; }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            Description = IntakeInput.Validate(Description);
            Result = await analyzer.AnalyzeAsync(Description, cancellationToken);
        }
        catch (ValidationException ex) { ModelState.AddModelError(nameof(Description), ex.Message); }
        catch (AnalysisException ex) { ModelState.AddModelError("", ex.Message); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ModelState.AddModelError("", "Analysis took too long. Please try again; your description is still here.");
        }
        return Page();
    }
}
