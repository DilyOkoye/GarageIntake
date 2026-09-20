# Garage Intake Assistant

A small C# application that helps a service adviser turn a customer's fault description into a reviewable workshop intake draft.

**Works offline out of the box.** Demo mode uses transparent keyword rules, preserves the customer report verbatim and suggests questions. Optional OpenAI mode generates structured summaries and questions. Both require adviser review before downloading a job card.

## Run

Install a .NET 10 SDK. From this repository directory:

```sh
dotnet restore
dotnet build --no-restore
dotnet run --project GarageIntake.Web --urls http://localhost:5000
```

Open http://localhost:5000. No API key or database is required for demo mode. The first restore of the test project requires NuGet access; the web application has no third-party package dependencies. Open `GarageIntake.sln` in Visual Studio or Rider if preferred.

```sh
dotnet test
```


## Try the workflow

1. Select **Braking noise** or type your own description (10–2,000 characters).
2. Select **Prepare intake draft**.
3. Review the report, suggested category and follow-up questions.
4. Edit the job card and tick the review checkbox.
5. Download a plain-text card. The export retains the original report as well as your edited notes.

Editing the card clears the review checkbox. Notes are not persisted by the application; a new request or refresh can discard edits. Download is browser-side and needs JavaScript. Analysis and results work without JavaScript.

## Enable OpenAI mode (optional)

The implementation uses OpenAI Chat Completions with a strict JSON schema, server-side credentials, a 30-second timeout and application-level response validation. It makes no automatic retries and never silently switches to demo mode after an AI failure.

Keep the key out of source code. In a terminal, set these environment variables before running:

```sh
# macOS / Linux; enter your own key locally, never commit it
export Analysis__Mode=OpenAI
export OpenAI__ApiKey='YOUR_API_KEY'
export OpenAI__Model=gpt-4o-mini
dotnet run --project src/GarageIntake.Web --urls http://localhost:5000
```

or
# enter your own key locally, never commit it
``` via user secret locally

dotnet user-secrets init --project GarageIntake.Web

dotnet user-secrets set "OpenAI:ApiKey" "YOUR_ACTUAL_KEY" --project GarageIntake.Web
```

PowerShell equivalent:

```powershell
$env:Analysis__Mode = "OpenAI"
$env:OpenAI__ApiKey = "YOUR_API_KEY"
$env:OpenAI__Model = "gpt-4o-mini"
dotnet run --project src/GarageIntake.Web --urls http://localhost:5000
```

Use a model available to your account that supports Chat Completions and strict structured outputs. The default model is configurable, not a claim that it is the latest model. API calls may incur charges. `store: false` disables response storage through that API option; it is not a promise of zero provider retention.

To return to demo, set `Analysis__Mode=Demo` and restart. An unrecognised mode fails at startup rather than silently choosing a provider.

## Structure

- `Program.cs`: application setup and dependency injection.
- `Pages/Index.cshtml`: server-rendered interface; Razor encodes displayed customer/model text.
- `Pages/Index.cshtml.cs`: form handling and friendly validation/errors.
- `Services/IIntakeAnalyzer.cs`: the contract shared by both providers.
- `Services/DemoIntakeAnalyzer.cs`: simple offline rules.
- `Services/OpenAiIntakeAnalyzer.cs`: HTTP integration, prompt, schema, parsing and provider errors.
- `Services/IntakeResult.cs`: input and output validation.
- `wwwroot`: local CSS and small JavaScript enhancements, no CDN or build pipeline.
- `GarageIntake.Tests`: xUnit tests with fake HTTP responses; no paid API calls.

## Verification and boundaries

25 automated tests cover input validation, ambiguous demo reports, preservation of original text, request schema, malformed outputs, refusals, truncation, provider failures, missing keys and page timeout behaviour. See [verification notes](docs/VERIFICATION.md) for execution evidence and browser checks.

The real OpenAI endpoint has been exercised with a paid key in this session which I am happy to walk through during the interview run through. Mocked contract tests verify our HTTP request and handling, not real model quality or account access. Try the evaluation cases in [development notes](DEVELOPMENT.md) before using AI output.

This is a local prototype, not a production garage system: no authentication, rate limiting, database, diagnostic rules, audit trail or validated safety triage. Keyword classification cannot reliably handle negation or multiple meanings. Every output needs adviser review. The review checkbox is a workflow aid, not an auditable sign-off.

Form submissions use ASP.NET antiforgery protection. Data-protection keys are ephemeral, so refresh the page after restarting the server. A deployment would need persistent protected keys, HTTPS, authentication, cost controls and a considered data-retention policy.
