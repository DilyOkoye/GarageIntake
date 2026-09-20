using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using GarageIntake.Web.Pages;
using GarageIntake.Web.Services;
using Microsoft.Extensions.Configuration;

namespace GarageIntake.Tests;
public sealed class IntakeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("         ")]
    [InlineData("noise")]
    public void RejectsInsufficientInput(string? input) => Assert.Throws<ValidationException>(() => IntakeInput.Validate(input));
    [Fact] public void RejectsLongInput() => Assert.Throws<ValidationException>(() => IntakeInput.Validate(new string('x', 2001)));
    [Fact] public void TrimsInput() => Assert.Equal("A grinding noise", IntakeInput.Validate("  A grinding noise  "));
    [Fact]
    public async Task DemoPreservesReportAndSuggestsQuestions()
    {
        const string input = "My Ford Focus makes a grinding noise when braking.";
        var result = await new DemoIntakeAnalyzer().AnalyzeAsync(input);
        Assert.Equal(input, result.Summary); Assert.Equal("Brakes", result.Category);
        Assert.Contains(input, result.JobCard); Assert.NotEmpty(result.Questions);
    }
    [Theory]
    [InlineData("The engine is overheating and my brakes make a noise.")]
    [InlineData("Something feels strange when I drive.")]
    [InlineData("The starter is making noise near the brakewater.")]
    public async Task DemoDoesNotForceAmbiguousCategory(string input) =>
        Assert.Equal("Other / unclear", (await new DemoIntakeAnalyzer().AnalyzeAsync(input)).Category);
    [Fact]
    public async Task MarkupRemainsData() => Assert.Equal("<script>alert('x')</script>",
        (await new DemoIntakeAnalyzer().AnalyzeAsync("<script>alert('x')</script>")).Summary);
    [Fact] public void InvalidResultRejected() => Assert.Throws<AnalysisException>(() => new IntakeResult("", "Brakes", [], "").Validate());
    [Fact] public void UnknownCategoryRejected() => Assert.Throws<AnalysisException>(() => (Valid with { Category = "Guaranteed safe" }).Validate());
    private static readonly IntakeResult Valid = new("Customer reports a noise.", "Other / unclear", ["When does it occur?"], "Adviser to review customer report.");
    private static string Envelope(string content, string finish = "stop", string? refusal = null) => JsonSerializer.Serialize(new
    {
        choices = new[] { new { finish_reason = finish, message = new { content, refusal } } }
    });
    private static OpenAiIntakeAnalyzer Analyzer(HttpMessageHandler handler, string? key = "test-key") =>
        new(new HttpClient(handler), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenAI:ApiKey"] = key }).Build());

    [Fact]
    public async Task AcceptsStructuredResponseAndSendsStrictSchema()
    {
        var handler = new FakeHandler(Envelope(JsonSerializer.Serialize(Valid, new JsonSerializerOptions(JsonSerializerDefaults.Web))));
        Assert.Equal(Valid.Summary, (await Analyzer(handler).AnalyzeAsync("There is an unusual noise.")).Summary);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.True(body.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("strict").GetBoolean());
        Assert.False(body.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal("There is an unusual noise.", body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString());
        Assert.Equal("https://api.openai.com/v1/chat/completions", handler.Uri);
    }
    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"choices\":[]}")]
    [InlineData("{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"content\":\"{}\"}}]}")]
    public async Task RejectsMalformedProviderResponse(string body) =>
        await Assert.ThrowsAsync<AnalysisException>(() => Analyzer(new FakeHandler(body)).AnalyzeAsync("There is an unusual noise."));
    [Fact]
    public async Task RejectsRefusal() => await Assert.ThrowsAsync<AnalysisException>(() =>
        Analyzer(new FakeHandler(Envelope("{}", refusal: "Cannot assist"))).AnalyzeAsync("There is an unusual noise."));
    [Fact]
    public async Task RejectsTruncation() => await Assert.ThrowsAsync<AnalysisException>(() =>
        Analyzer(new FakeHandler(Envelope("{}", "length"))).AnalyzeAsync("There is an unusual noise."));
    [Fact]
    public async Task ProviderErrorDoesNotLeakBody()
    {
        var error = await Assert.ThrowsAsync<AnalysisException>(() => Analyzer(new FakeHandler("sensitive-provider-details", HttpStatusCode.Unauthorized)).AnalyzeAsync("There is an unusual noise."));
        Assert.DoesNotContain("sensitive", error.Message);
    }
    [Fact]
    public async Task MissingKeyMakesNoRequest()
    {
        var handler = new FakeHandler("{}");
        await Assert.ThrowsAsync<AnalysisException>(() => Analyzer(handler, null).AnalyzeAsync("There is an unusual noise."));
        Assert.Null(handler.Body);
    }
    [Fact]
    public async Task NetworkFailureIsFriendly() => await Assert.ThrowsAsync<AnalysisException>(() =>
        Analyzer(new ThrowingHandler(new HttpRequestException())).AnalyzeAsync("There is an unusual noise."));
    [Fact]
    public async Task PageRetainsDescriptionAfterTimeout()
    {
        var page = new IndexModel(Analyzer(new ThrowingHandler(new TaskCanceledException()))) { Description = "There is an unusual noise." };
        await page.OnPostAsync(default);
        Assert.False(page.ModelState.IsValid); Assert.Null(page.Result); Assert.Equal("There is an unusual noise.", page.Description);
    }
    [Fact]
    public async Task InvalidPageInputDoesNotProduceResult()
    {
        var page = new IndexModel(new DemoIntakeAnalyzer()) { Description = "short" };
        await page.OnPostAsync(default);
        Assert.False(page.ModelState.IsValid); Assert.Null(page.Result);
    }
    private sealed class FakeHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? Uri { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri!.ToString(); Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
    private sealed class ThrowingHandler(Exception error) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw error;
    }
}
