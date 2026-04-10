#!/usr/bin/env dotnet-script
// bdk-mcp
// A small MCP server that routes bITdevKit documentation requests through the
// live INDEX.md file published in the main bITdevKit GitHub repository.

#r "nuget: Microsoft.Extensions.Hosting, 10.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 10.0.0"
#r "nuget: ModelContextProtocol, 1.0.0"

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(Args.ToArray());

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddSingleton(CreateHttpClient());
builder.Services.AddSingleton<BdkDocumentationService>();
builder.Services.AddSingleton<BdkRepositoryGuidanceService>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "bdk-mcp",
            Title = "bITdevKit MCP",
            Version = "0.1.0",
            Description = "Live bdk documentation and repo-aware development guidance routed through the GitHub INDEX.md file."
        };
        options.ServerInstructions = "For bdk requests, route through the live GitHub INDEX.md before reading documentation pages.";
    })
    .WithStdioServerTransport()
    .WithResourcesFromAssembly()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

static HttpClient CreateHttpClient()
{
    var client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    client.DefaultRequestHeaders.UserAgent.ParseAdd("bdk-mcp/0.1.0");
    return client;
}

[McpServerResourceType]
public static class BdkDocumentationResources
{
    [McpServerResource(
        Name = "bITdevKit Documentation Index",
        Title = "bITdevKit Documentation Index",
        UriTemplate = "bdk://docs/index",
        MimeType = "text/markdown")]
    [Description("Returns the live bITdevKit documentation index from GitHub. Use this index as the routing table for other documentation lookups.")]
    public static async Task<string> GetIndex(IServiceProvider services, CancellationToken cancellationToken)
    {
        var documentation = services.GetRequiredService<BdkDocumentationService>();
        return await documentation.GetIndexMarkdownAsync(cancellationToken);
    }
}

[McpServerToolType]
public static class BdkDocumentationTools
{
    [McpServerTool(
        Name = "get_bdk_docs",
        Title = "Get bITdevKit Documentation",
        ReadOnly = true,
        Idempotent = true,
        UseStructuredContent = true)]
    [Description("Routes a bITdevKit documentation query through the live GitHub INDEX.md file and returns the best matching documentation page.")]
    public static async Task<BdkDocumentationResponse> GetBdkDocs(
        [Description("The documentation topic or question to look up, for example 'presentation endpoints' or 'requester notifier'.")]
        string query,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var documentation = services.GetRequiredService<BdkDocumentationService>();
        return await documentation.GetDocumentationAsync(query, cancellationToken);
    }

    [McpServerTool(
        Name = "get_bdk_proj",
        Title = "Get bdk Project Help",
        ReadOnly = true,
        Idempotent = true,
        UseStructuredContent = true)]
    [Description("Combines live bdk docs routing with project-aware guidance, including suggested files based on module structure and topic.")]
    public static async Task<BdkRepositoryHelpResponse> GetBdkRepoHelp(
        [Description("The development task or topic, for example 'add a module', 'requester behavior', or 'presentation endpoints'.")]
        string query,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var guidance = services.GetRequiredService<BdkRepositoryGuidanceService>();
        return await guidance.GetHelpAsync(query, cancellationToken);
    }
}

public sealed class BdkDocumentationService(HttpClient httpClient, ILogger<BdkDocumentationService> logger)
{
    private const string DocumentationBaseUrl = "https://raw.githubusercontent.com/BridgingIT-GmbH/bITdevKit/main/docs/";
    private const string IndexFileName = "INDEX.md";
    private static readonly TimeSpan IndexCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DocumentCacheDuration = TimeSpan.FromMinutes(10);

    private static readonly Regex IndexEntryRegex = new(
        @"^- \[(?<title>[^\]]+)\]\(\./(?<path>[^)]+)\): (?<description>.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Dictionary<string, string[]> QuerySynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bdk"] = ["bitdevkit", "devkit"],
        ["bitdevkit"] = ["bdk", "devkit"],
        ["module"] = ["modules", "modular"],
        ["modules"] = ["module", "modular"],
        ["jobs"] = ["job", "background processing", "jobscheduling", "background processing"],
        ["jobscheduling"] = ["jobs", "background processing", "background processing", "jobs"],
        ["endpoint"] = ["endpoints", "api", "minimal"],
        ["endpoints"] = ["endpoint", "api", "minimal"],
        ["requester"] = ["notifier", "pipeline", "handlers", "commands", "queries", "events"],
        ["notifier"] = ["requester", "pipeline", "handlers", "commands", "queries", "events"],
        ["pipeline"] = ["behavior", "behaviors"],
        ["behavior"] = ["pipeline", "behaviors"]
    };

    private readonly HttpClient httpClient = httpClient;
    private readonly ILogger<BdkDocumentationService> logger = logger;
    private readonly object indexLock = new();
    private CacheEntry? indexCache;
    private readonly ConcurrentDictionary<string, CacheEntry> documentCache = new(StringComparer.OrdinalIgnoreCase);

    public Task<string> GetIndexMarkdownAsync(CancellationToken cancellationToken)
    {
        return this.GetMarkdownAsync(IndexFileName, cancellationToken);
    }

    public async Task<BdkDocumentationResponse> GetDocumentationAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("The query must not be empty.", nameof(query));
        }

        var indexMarkdown = await this.GetIndexMarkdownAsync(cancellationToken);
        var routes = ParseIndex(indexMarkdown);
        var matches = RankRoutes(routes, query).Take(3).ToList();

        if (matches.Count == 0)
        {
            return new BdkDocumentationResponse
            {
                Query = query,
                RoutedViaIndex = true,
                IndexUri = BuildRawUrl(IndexFileName),
                Message = "No matching documentation page was found in the live bITdevKit INDEX.md routing table.",
                Summary = "No relevant route found. Try a more specific bdk topic, such as 'modules', 'requester notifier', or 'presentation endpoints'.",
                Matches = Array.Empty<BdkDocumentationMatch>(),
                Document = null
            };
        }

        var selected = matches[0];
        var documentMarkdown = await this.GetMarkdownAsync(selected.Path, cancellationToken);
        var confidence = CalculateConfidence(matches);
        var routeReason = BuildRouteReason(selected, query);
        var summary = BuildSummary(documentMarkdown, selected.Title);

        if (confidence < 0.40)
        {
            this.logger.LogWarning("Low-confidence bdk docs route for query '{Query}' to '{Path}' (confidence {Confidence:0.00}).", query, selected.Path, confidence);
        }

        this.logger.LogInformation("Routed bdk docs query '{Query}' to '{Path}' with confidence {Confidence:0.00}.", query, selected.Path, confidence);

        return new BdkDocumentationResponse
        {
            Query = query,
            RoutedViaIndex = true,
            IndexUri = BuildRawUrl(IndexFileName),
            Source = "live-github",
            Message = $"Matched '{selected.Title}' from the live GitHub INDEX.md routing table.",
            Summary = summary,
            Confidence = confidence,
            RouteReason = routeReason,
            Matches = matches
                .Select(match => new BdkDocumentationMatch
                {
                    Title = match.Title,
                    Path = match.Path,
                    Description = match.Description,
                    RawUrl = BuildRawUrl(match.Path),
                    Score = match.Score,
                    MatchReason = match.MatchReason
                })
                .ToArray(),
            Document = new BdkDocumentationDocument
            {
                Title = selected.Title,
                Path = selected.Path,
                RawUrl = BuildRawUrl(selected.Path),
                Markdown = documentMarkdown
            }
        };
    }

    private async Task<string> GetMarkdownAsync(string relativePath, CancellationToken cancellationToken)
    {
        var isIndex = string.Equals(relativePath, IndexFileName, StringComparison.OrdinalIgnoreCase);
        var cacheDuration = isIndex ? IndexCacheDuration : DocumentCacheDuration;

        if (TryGetFresh(relativePath, cacheDuration, out var cached))
        {
            return cached;
        }

        try
        {
            using var response = await this.httpClient.GetAsync(BuildRawUrl(relativePath), cancellationToken);
            response.EnsureSuccessStatusCode();
            var markdown = await response.Content.ReadAsStringAsync(cancellationToken);
            SetCache(relativePath, markdown);
            return markdown;
        }
        catch when (TryGetAny(relativePath, out var stale))
        {
            this.logger.LogWarning("Using stale cached bdk docs for '{Path}' due to live fetch failure.", relativePath);
            return stale;
        }
    }

    private static string BuildRawUrl(string relativePath)
    {
        return DocumentationBaseUrl + relativePath.TrimStart('.');
    }

    private static IReadOnlyList<IndexRoute> ParseIndex(string indexMarkdown)
    {
        return IndexEntryRegex
            .Matches(indexMarkdown)
            .Select(match => new IndexRoute(
                match.Groups["title"].Value.Trim(),
                match.Groups["path"].Value.Trim(),
                match.Groups["description"].Value.Trim()))
            .ToList();
    }

    private static IEnumerable<IndexRouteMatch> RankRoutes(IEnumerable<IndexRoute> routes, string query)
    {
        var normalizedQuery = Normalize(query);
        var queryTerms = ExpandQueryTerms(normalizedQuery).ToArray();

        return routes
            .Select(route =>
            {
                var score = 0;
                var reasons = new List<string>();
                var title = Normalize(route.Title);
                var path = Normalize(route.Path);
                var description = Normalize(route.Description);

                if (title.Contains(normalizedQuery, StringComparison.Ordinal))
                {
                    score += 140;
                    reasons.Add("exact title phrase");
                }
                else if (path.Contains(normalizedQuery, StringComparison.Ordinal))
                {
                    score += 110;
                    reasons.Add("exact path phrase");
                }
                else if (description.Contains(normalizedQuery, StringComparison.Ordinal))
                {
                    score += 60;
                    reasons.Add("exact description phrase");
                }

                foreach (var term in queryTerms)
                {
                    if (title.Contains(term, StringComparison.Ordinal))
                    {
                        score += 35;
                    }
                    else if (path.Contains(term, StringComparison.Ordinal))
                    {
                        score += 25;
                    }
                    else if (description.Contains(term, StringComparison.Ordinal))
                    {
                        score += 10;
                    }
                }

                if (score > 0 && reasons.Count == 0)
                {
                    reasons.Add("term overlap");
                }

                return new IndexRouteMatch(route.Title, route.Path, route.Description, score, string.Join(", ", reasons));
            })
            .Where(route => route.Score > 0)
            .OrderByDescending(route => route.Score)
            .ThenBy(route => route.Title, StringComparer.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(value.Trim().ToLowerInvariant(), "\\s+", " ");
    }

    private static IEnumerable<string> ExpandQueryTerms(string normalizedQuery)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var tokens = Tokenize(normalizedQuery);

        foreach (var token in tokens)
        {
            if (seen.Add(token))
            {
                yield return token;
            }

            if (QuerySynonyms.TryGetValue(token, out var synonyms))
            {
                foreach (var synonym in synonyms.Select(Normalize))
                {
                    if (seen.Add(synonym))
                    {
                        yield return synonym;
                    }
                }
            }
        }

        if (seen.Add("bdk"))
        {
            yield return "bdk";
        }

        if (seen.Add("bitdevkit"))
        {
            yield return "bitdevkit";
        }
    }

    private static IEnumerable<string> Tokenize(string value)
    {
        return Regex.Matches(value, "[a-z0-9]+")
            .Select(m => m.Value)
            .Where(v => v.Length > 1);
    }

    private static double CalculateConfidence(IReadOnlyList<IndexRouteMatch> matches)
    {
        if (matches.Count == 0)
        {
            return 0.0;
        }

        var top = matches[0].Score;
        var second = matches.Count > 1 ? matches[1].Score : 0;
        var separation = Math.Max(0.0, (top - second) / 100.0);
        var baseScore = Math.Min(0.95, top / 220.0);
        var confidence = Math.Min(0.99, baseScore * 0.75 + separation * 0.25);
        return Math.Round(confidence, 2, MidpointRounding.AwayFromZero);
    }

    private static string BuildRouteReason(IndexRouteMatch selected, string query)
    {
        var reason = string.IsNullOrWhiteSpace(selected.MatchReason) ? "term overlap" : selected.MatchReason;
        return $"Routed query '{query}' to '{selected.Title}' due to {reason}.";
    }

    private static string BuildSummary(string markdown, string title)
    {
        var lines = markdown
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !l.StartsWith("#", StringComparison.Ordinal))
            .Where(l => !l.StartsWith("[TOC]", StringComparison.OrdinalIgnoreCase))
            .Where(l => !l.StartsWith("```", StringComparison.Ordinal))
            .ToList();

        var first = lines.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
        {
            return $"Matched bdk page '{title}'.";
        }

        return first.Length <= 220 ? first : first[..220] + "...";
    }

    private bool TryGetFresh(string path, TimeSpan maxAge, out string markdown)
    {
        if (TryGetCache(path, out var entry) && DateTimeOffset.UtcNow - entry.FetchedAtUtc <= maxAge)
        {
            markdown = entry.Markdown;
            return true;
        }

        markdown = string.Empty;
        return false;
    }

    private bool TryGetAny(string path, out string markdown)
    {
        if (TryGetCache(path, out var entry))
        {
            markdown = entry.Markdown;
            return true;
        }

        markdown = string.Empty;
        return false;
    }

    private bool TryGetCache(string path, out CacheEntry entry)
    {
        if (string.Equals(path, IndexFileName, StringComparison.OrdinalIgnoreCase))
        {
            lock (this.indexLock)
            {
                if (this.indexCache is not null)
                {
                    entry = this.indexCache.Value;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        return this.documentCache.TryGetValue(path, out entry);
    }

    private void SetCache(string path, string markdown)
    {
        var entry = new CacheEntry(markdown, DateTimeOffset.UtcNow);

        if (string.Equals(path, IndexFileName, StringComparison.OrdinalIgnoreCase))
        {
            lock (this.indexLock)
            {
                this.indexCache = entry;
            }

            return;
        }

        this.documentCache[path] = entry;
    }

    private sealed record IndexRoute(string Title, string Path, string Description);

    private sealed record IndexRouteMatch(string Title, string Path, string Description, int Score, string MatchReason);

    private readonly record struct CacheEntry(string Markdown, DateTimeOffset FetchedAtUtc);
}

public sealed class BdkRepositoryGuidanceService(BdkDocumentationService documentationService)
{
    private readonly BdkDocumentationService documentationService = documentationService;
    private readonly string repositoryRoot = Directory.GetCurrentDirectory();

    public async Task<BdkRepositoryHelpResponse> GetHelpAsync(string query, CancellationToken cancellationToken)
    {
        var docs = await this.documentationService.GetDocumentationAsync(query, cancellationToken);
        var topic = InferTopic(docs.Document?.Path, query);
        var files = this.GetFiles(topic);

        return new BdkRepositoryHelpResponse
        {
            Query = query,
            Topic = topic,
            Documentation = docs,
            SuggestedFiles = files
        };
    }

    private IReadOnlyList<BdkRepositoryFileHint> GetFiles(string topic)
    {
        var hints = new List<BdkRepositoryFileHint>();
        var moduleNames = this.GetModuleNames();

        AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host wiring for modules, requester/notifier, and endpoint setup.");
        AddIfExists(hints, "src/Presentation.Web.Server/ProgramExtensions.cs", "Default requester/notifier behaviors and app-level helpers.");

        if (topic == "modules")
        {
            foreach (var moduleName in moduleNames)
            {
                AddFirstMatch(hints, $"src/Modules/{moduleName}", "*Module.cs", $"{moduleName} module registration entrypoint.");
                AddFirstMatch(hints, $"src/Modules/{moduleName}", "*Configuration.cs", $"{moduleName} module configuration pattern.");
                AddFirstMatch(hints, $"src/Modules/{moduleName}", "*Endpoints.cs", $"{moduleName} endpoint mapping pattern.");
                AddIfExists(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/ArchitectureTests.cs", $"{moduleName} architecture boundary tests.");
                AddFirstMatch(hints, $"tests/Modules/{moduleName}", "*EndpointTests.cs", $"{moduleName} integration endpoint coverage.");
            }
        }
        else if (topic == "requester-notifier")
        {
            foreach (var moduleName in moduleNames)
            {
                var commandsFolder = $"src/Modules/{moduleName}/{moduleName}.Application/Commands";
                var queriesFolder = $"src/Modules/{moduleName}/{moduleName}.Application/Queries";
                var applicationFolder = $"src/Modules/{moduleName}/{moduleName}.Application";

                var commandAdded =
                    AddFirstFileContainingAny(hints, commandsFolder, "*.cs", ["[Command]", "[Handle]", "RequestBase<"], $"{moduleName} command implementation for requester pattern (co-located or generated).") ||
                    AddFirstFileContainingAny(hints, applicationFolder, "*.cs", ["[Command]", "[Handle]", "RequestBase<"], $"{moduleName} command implementation for requester pattern (co-located or generated).");

                var queryAdded =
                    AddFirstFileContainingAny(hints, queriesFolder, "*.cs", ["[Query]", "[Handle]", "RequestBase<"], $"{moduleName} query implementation for requester pattern (co-located or generated).") ||
                    AddFirstFileContainingAny(hints, applicationFolder, "*.cs", ["[Query]", "[Handle]", "RequestBase<"], $"{moduleName} query implementation for requester pattern (co-located or generated).");

                if (!commandAdded)
                {
                    AddFirstMatch(hints, applicationFolder, "*Command*.cs", $"{moduleName} command implementation example.");
                }

                if (!queryAdded)
                {
                    AddFirstMatch(hints, applicationFolder, "*Query*.cs", $"{moduleName} query implementation example.");
                }

                AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Commands", "*HandlerTests.cs", $"{moduleName} command handler unit tests.");
                AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Queries", "*HandlerTests.cs", $"{moduleName} query handler unit tests.");
            }

            AddIfExists(hints, "src/Presentation.Web.Server/ProgramExtensions.cs", "Requester/Notifier pipeline behavior wiring (module scope, validation, retry, timeout).");
        }
        else if (topic == "presentation-endpoints")
        {
            foreach (var moduleName in moduleNames)
            {
                AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/Web/Endpoints", "*Endpoints.cs", $"{moduleName} presentation endpoint conventions.");
                AddFirstMatch(hints, $"tests/Modules/{moduleName}", "*EndpointTests.cs", $"{moduleName} endpoint integration test coverage.");
            }
        }

        return hints
            .GroupBy(h => h.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(12)
            .ToList();
    }

    private IReadOnlyList<string> GetModuleNames()
    {
        var modulesRoot = Path.Combine(this.repositoryRoot, "src", "Modules");
        if (!Directory.Exists(modulesRoot))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(modulesRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }

    private static string InferTopic(string? path, string query)
    {
        var source = $"{path ?? string.Empty} {query}".ToLowerInvariant();
        if (source.Contains("modules", StringComparison.Ordinal))
        {
            return "modules";
        }

        if (source.Contains("requester", StringComparison.Ordinal) ||
            source.Contains("notifier", StringComparison.Ordinal) ||
            source.Contains("command", StringComparison.Ordinal) ||
            source.Contains("query", StringComparison.Ordinal) ||
            source.Contains("commands", StringComparison.Ordinal) ||
            source.Contains("queries", StringComparison.Ordinal))
        {
            return "requester-notifier";
        }

        if (source.Contains("endpoint", StringComparison.Ordinal) || source.Contains("presentation", StringComparison.Ordinal))
        {
            return "presentation-endpoints";
        }

        return "general";
    }

    private void AddIfExists(List<BdkRepositoryFileHint> hints, string relativePath, string reason)
    {
        var fullPath = Path.Combine(this.repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            hints.Add(new BdkRepositoryFileHint
            {
                Path = relativePath,
                Reason = reason
            });
        }
    }

    private void AddFirstMatch(List<BdkRepositoryFileHint> hints, string relativeFolder, string pattern, string reason)
    {
        var fullFolder = Path.Combine(this.repositoryRoot, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(fullFolder))
        {
            return;
        }

        var match = Directory.GetFiles(fullFolder, pattern, SearchOption.AllDirectories)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (match is null)
        {
            return;
        }

        var relative = Path.GetRelativePath(this.repositoryRoot, match)
            .Replace('\\', '/');

        hints.Add(new BdkRepositoryFileHint
        {
            Path = relative,
            Reason = reason
        });
    }

    private bool AddFirstFileContainingAny(List<BdkRepositoryFileHint> hints, string relativeFolder, string pattern, IReadOnlyList<string> markers, string reason)
    {
        var fullFolder = Path.Combine(this.repositoryRoot, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(fullFolder))
        {
            return false;
        }

        var files = Directory.GetFiles(fullFolder, pattern, SearchOption.AllDirectories)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in files)
        {
            string content;

            try
            {
                content = File.ReadAllText(file);
            }
            catch
            {
                continue;
            }

            if (!markers.Any(marker => content.Contains(marker, StringComparison.Ordinal)))
            {
                continue;
            }

            var relative = Path.GetRelativePath(this.repositoryRoot, file)
                .Replace('\\', '/');

            hints.Add(new BdkRepositoryFileHint
            {
                Path = relative,
                Reason = reason
            });

            return true;
        }

        return false;
    }
}

public sealed class BdkDocumentationResponse
{
    public required string Query { get; init; }

    public required bool RoutedViaIndex { get; init; }

    public required string IndexUri { get; init; }

    public string Source { get; init; } = "live-github";

    public required string Message { get; init; }

    public string Summary { get; init; } = string.Empty;

    public double Confidence { get; init; }

    public string RouteReason { get; init; } = string.Empty;

    public required IReadOnlyList<BdkDocumentationMatch> Matches { get; init; }

    public BdkDocumentationDocument? Document { get; init; }
}

public sealed class BdkDocumentationMatch
{
    public required string Title { get; init; }

    public required string Path { get; init; }

    public required string Description { get; init; }

    public required string RawUrl { get; init; }

    public required int Score { get; init; }

    public string MatchReason { get; init; } = string.Empty;
}

public sealed class BdkDocumentationDocument
{
    public required string Title { get; init; }

    public required string Path { get; init; }

    public required string RawUrl { get; init; }

    public required string Markdown { get; init; }
}

public sealed class BdkRepositoryHelpResponse
{
    public required string Query { get; init; }

    public required string Topic { get; init; }

    public required BdkDocumentationResponse Documentation { get; init; }

    public required IReadOnlyList<BdkRepositoryFileHint> SuggestedFiles { get; init; }
}

public sealed class BdkRepositoryFileHint
{
    public required string Path { get; init; }

    public required string Reason { get; init; }
}
