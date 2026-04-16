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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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
builder.Services.AddSingleton<BdkPackageDocumentationService>();
builder.Services.AddSingleton<BdkRepositoryInspectorService>();
builder.Services.AddSingleton<BdkDocumentationService>();
builder.Services.AddSingleton<BdkRepositoryGuidanceService>();
builder.Services.AddSingleton<BdkRecipeService>();
builder.Services.AddSingleton<BdkConventionReviewService>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "bdk-mcp",
            Title = "bITdevKit MCP",
            Version = "0.2.0",
            Description = "Live bdk documentation, package-doc enrichment, and repo-aware development guidance routed through the GitHub INDEX.md file."
        };
        options.ServerInstructions = "For bdk requests, route through the live GitHub INDEX.md before reading documentation pages. Prefer repo-aware recipes, snippets, and review tools for implementation guidance in this repository.";
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

    client.DefaultRequestHeaders.UserAgent.ParseAdd("bdk-mcp/0.2.0");
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
    [Description("Returns the bITdevKit documentation index from GitHub. Use this index as the routing map for specific documentation lookups.")]
    public static async Task<string> GetIndex(IServiceProvider services, CancellationToken cancellationToken)
    {
        var documentation = services.GetRequiredService<BdkDocumentationService>();
        return await documentation.GetIndexMarkdownAsync(cancellationToken);
    }

    [McpServerResource(
        Name = "bITdevKit Repo Patterns",
        Title = "bITdevKit Repo Patterns",
        UriTemplate = "bdk://repo/patterns",
        MimeType = "text/markdown")]
    [Description("Returns supported bITdevKit topic families, sample prompts, current modules, source precedence, and recommended bdk-mcp tools for this repository.")]
    public static Task<string> GetPatterns(IServiceProvider services, CancellationToken cancellationToken)
    {
        var inspector = services.GetRequiredService<BdkRepositoryInspectorService>();
        return Task.FromResult(inspector.GetPatternsMarkdown());
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
    [Description("Finds the best matching bITdevKit documentation and supplements it with matching package XML documentation when available.")]
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
    [Description("Finds lightweight project-aware bITdevKit guidance, including suggested files based on the local module structure and topic.")]
    public static async Task<BdkRepositoryHelpResponse> GetBdkRepoHelp(
        [Description("The development task or topic, for example 'add a module', 'requester behavior', or 'presentation endpoints'.")]
        string query,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var guidance = services.GetRequiredService<BdkRepositoryGuidanceService>();
        return await guidance.GetHelpAsync(query, cancellationToken);
    }

    [McpServerTool(
        Name = "get_bdk_recipe",
        Title = "Get bdk Implementation Recipe",
        ReadOnly = true,
        Idempotent = true,
        UseStructuredContent = true)]
    [Description("Builds a repo-aware implementation recipe using routed bITdevKit docs, local examples, and repository conventions.")]
    public static async Task<BdkRecipeResponse> GetBdkRecipe(
        [Description("The development task or topic, for example 'presentation endpoints', 'commands and queries', or 'startup tasks'.")]
        string query,
        [Description("Optional module name to prefer. When omitted, the first available module is used.")]
        string? module,
        [Description("Optional layer hint such as 'Application', 'Presentation', or 'Infrastructure'.")]
        string? layer,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var recipes = services.GetRequiredService<BdkRecipeService>();
        return await recipes.GetRecipeAsync(query, module, layer, cancellationToken);
    }

    [McpServerTool(
        Name = "get_bdk_snippets",
        Title = "Get bdk Local Snippets",
        ReadOnly = true,
        Idempotent = true,
        UseStructuredContent = true)]
    [Description("Returns exact local code snippets with file paths and line numbers for common bITdevKit patterns in this repository.")]
    public static Task<BdkSnippetsResponse> GetBdkSnippets(
        [Description("The development task or topic, for example 'requester behaviors', 'presentation endpoints', or 'mapping'.")]
        string query,
        [Description("Optional module name to prefer. When omitted, the first available module is used.")]
        string? module,
        [Description("Maximum number of snippets to return. Default is 3.")]
        int? maxResults,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var inspector = services.GetRequiredService<BdkRepositoryInspectorService>();
        return Task.FromResult(inspector.GetSnippetsResponse(query, module, maxResults ?? 3));
    }

    [McpServerTool(
        Name = "review_bdk_usage",
        Title = "Review bdk Usage",
        ReadOnly = true,
        Idempotent = true,
        UseStructuredContent = true)]
    [Description("Reviews the repository for high-value bITdevKit convention issues such as Application-to-Infrastructure leakage, endpoint placement, missing validator signals, and missing nearby tests.")]
    public static Task<BdkReviewResponse> ReviewBdkUsage(
        [Description("Optional topic filter, for example 'presentation endpoints', 'commands and queries', or 'jobs'.")]
        string? query,
        [Description("Optional module name to scope the review. When omitted, all modules are reviewed.")]
        string? module,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var review = services.GetRequiredService<BdkConventionReviewService>();
        return Task.FromResult(review.Review(query, module));
    }
}

public sealed class BdkDocumentationService(
    HttpClient httpClient,
    ILogger<BdkDocumentationService> logger,
    BdkPackageDocumentationService packageDocumentationService)
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
        ["domain"] = ["aggregate", "aggregates", "value object", "value objects", "typed id", "typed ids", "enumeration", "enumerations"],
        ["aggregate"] = ["domain", "value object", "typed id", "change builder"],
        ["event"] = ["events", "domain events", "outbox"],
        ["events"] = ["event", "domain events", "outbox"],
        ["specification"] = ["specifications", "filtering", "criteria"],
        ["specifications"] = ["specification", "filtering", "criteria"],
        ["module"] = ["modules", "modular"],
        ["modules"] = ["module", "modular"],
        ["messaging"] = ["message", "messages", "broker", "outbox", "publish", "subscribe"],
        ["message"] = ["messaging", "broker", "publish", "subscribe"],
        ["queueing"] = ["queue", "queues", "enqueue", "work item"],
        ["queue"] = ["queueing", "enqueue", "work item"],
        ["notifications"] = ["notification", "email", "smtp", "outbox"],
        ["notification"] = ["notifications", "email", "smtp", "outbox"],
        ["jobs"] = ["job", "jobscheduling", "scheduler", "quartz", "background"],
        ["job"] = ["jobs", "jobscheduling", "scheduler", "quartz", "background"],
        ["jobscheduling"] = ["job", "jobs", "scheduler", "quartz", "background"],
        ["endpoint"] = ["endpoints", "api", "minimal", "presentation"],
        ["endpoints"] = ["endpoint", "api", "minimal", "presentation"],
        ["requester"] = ["notifier", "pipeline", "handlers", "commands", "queries", "events"],
        ["notifier"] = ["requester", "pipeline", "handlers", "commands", "queries", "events"],
        ["pipeline"] = ["behavior", "behaviors", "requester", "notifier"],
        ["behavior"] = ["pipeline", "behaviors", "requester", "notifier"],
        ["pipelines"] = ["pipeline", "workflow", "hook", "behavior", "steps"],
        ["workflow"] = ["pipelines", "pipeline", "steps", "hook", "behavior"],
        ["command"] = ["commands", "query", "queries", "requester"],
        ["commands"] = ["command", "query", "queries", "requester"],
        ["query"] = ["queries", "command", "commands", "requester"],
        ["queries"] = ["query", "command", "commands", "requester"],
        ["repository"] = ["repositories", "persistence", "findoptions"],
        ["repositories"] = ["repository", "persistence", "findoptions"],
        ["filter"] = ["filtering", "filters", "specification", "query"],
        ["filters"] = ["filter", "filtering", "specification", "query"],
        ["filtering"] = ["filter", "filters", "specification", "findoptions"],
        ["mapping"] = ["mapper", "mapster", "dto"],
        ["mapper"] = ["mapping", "mapster", "dto"],
        ["startup"] = ["startuptask", "startuptasks", "seed", "seeder"],
        ["task"] = ["startuptask", "startuptasks", "seed", "seeder"],
        ["result"] = ["results", "map", "bind", "ensure", "unless"],
        ["results"] = ["result", "map", "bind", "ensure", "unless"],
        ["rule"] = ["rules", "validation"],
        ["rules"] = ["rule", "validation"],
        ["storage"] = ["document", "documents", "file", "files"],
        ["document"] = ["documents", "storage", "document storage"],
        ["documents"] = ["document", "storage", "document storage"],
        ["file"] = ["files", "storage", "file storage"],
        ["files"] = ["file", "storage", "file storage"]
    };

    private readonly HttpClient httpClient = httpClient;
    private readonly ILogger<BdkDocumentationService> logger = logger;
    private readonly BdkPackageDocumentationService packageDocumentationService = packageDocumentationService;
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
        var xmlMatches = await this.packageDocumentationService.FindXmlDocumentationAsync(query, maxMatches: 3, cancellationToken);

        if (matches.Count == 0)
        {
            return new BdkDocumentationResponse
            {
                Query = query,
                RoutedViaIndex = true,
                IndexUri = BuildRawUrl(IndexFileName),
                Source = xmlMatches.Count > 0 ? "package-xml" : "live-github",
                Message = xmlMatches.Count > 0
                    ? "No matching page was found in the live bITdevKit INDEX.md routing table, but matching package XML documentation was found."
                    : "No matching documentation page was found in the live bITdevKit INDEX.md routing table.",
                Summary = xmlMatches.Count > 0
                    ? "No relevant GitHub route was found. Supplemental package XML documentation matches are available."
                    : "No relevant route found. Try a more specific bdk topic, such as 'modules', 'requester notifier', or 'presentation endpoints'.",
                Matches = Array.Empty<BdkDocumentationMatch>(),
                Document = null,
                XmlDocumentation = xmlMatches,
                Sources = BuildSources(null, null, xmlMatches)
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

        return new BdkDocumentationResponse
        {
            Query = query,
            RoutedViaIndex = true,
            IndexUri = BuildRawUrl(IndexFileName),
            Source = xmlMatches.Count > 0 ? "live-github+package-xml" : "live-github",
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
            },
            XmlDocumentation = xmlMatches,
            Sources = BuildSources(selected, summary, xmlMatches)
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

    private static IReadOnlyList<BdkDocumentationSourceAttribution> BuildSources(
        IndexRouteMatch? selected,
        string? selectedSummary,
        IReadOnlyList<BdkXmlDocumentationMatch> xmlMatches)
    {
        var sources = new List<BdkDocumentationSourceAttribution>
        {
            new()
            {
                Kind = "github-index",
                Name = "bITdevKit Documentation Index",
                Location = BuildRawUrl(IndexFileName),
                Summary = "Routing table used to select the best matching bITdevKit documentation page."
            }
        };

        if (selected is not null)
        {
            sources.Add(new BdkDocumentationSourceAttribution
            {
                Kind = "github-doc",
                Name = selected.Title,
                Location = BuildRawUrl(selected.Path),
                Summary = selectedSummary ?? selected.Description
            });
        }

        sources.AddRange(xmlMatches.Select(match => new BdkDocumentationSourceAttribution
        {
            Kind = "package-xml",
            Name = $"{match.PackageId}::{match.MemberName}",
            Location = match.XmlPath,
            Summary = match.Summary
        }));

        return sources;
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

public sealed class BdkPackageDocumentationService(ILogger<BdkPackageDocumentationService> logger)
{
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);
    private readonly ILogger<BdkPackageDocumentationService> logger = logger;
    private readonly object packagesLock = new();
    private string? globalPackagesPath;
    private IReadOnlyList<BdkPackageDescriptor>? packagesCache;
    private readonly ConcurrentDictionary<string, IReadOnlyList<BdkXmlDocumentationEntry>> xmlEntryCache = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<BdkXmlDocumentationMatch>> FindXmlDocumentationAsync(string query, int maxMatches, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<BdkXmlDocumentationMatch>>(Array.Empty<BdkXmlDocumentationMatch>());
        }

        var normalizedQuery = Normalize(query);
        var queryTerms = ExpandTerms(normalizedQuery).ToArray();
        var matches = this.GetPackages()
            .SelectMany(package => this.GetXmlEntries(package).Select(entry => Score(entry, normalizedQuery, queryTerms)))
            .Where(match => match is not null)
            .Select(match => match!)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.PackageId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.MemberName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(maxMatches, 1, 10))
            .ToList();

        return Task.FromResult<IReadOnlyList<BdkXmlDocumentationMatch>>(matches);
    }

    private IReadOnlyList<BdkPackageDescriptor> GetPackages()
    {
        if (this.packagesCache is not null)
        {
            return this.packagesCache;
        }

        lock (this.packagesLock)
        {
            if (this.packagesCache is not null)
            {
                return this.packagesCache;
            }

            var root = this.GetGlobalPackagesPath();
            if (!Directory.Exists(root))
            {
                this.logger.LogWarning("NuGet global packages path '{Path}' does not exist. Package XML docs will be unavailable.", root);
                this.packagesCache = Array.Empty<BdkPackageDescriptor>();
                return this.packagesCache;
            }

            this.packagesCache = Directory.GetDirectories(root, "bridgingit.devkit*")
                .Select(TryBuildDescriptor)
                .Where(descriptor => descriptor is not null)
                .Select(descriptor => descriptor!)
                .OrderBy(descriptor => descriptor.PackageId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return this.packagesCache;
        }
    }

    private IReadOnlyList<BdkXmlDocumentationEntry> GetXmlEntries(BdkPackageDescriptor package)
    {
        return this.xmlEntryCache.GetOrAdd(package.RootPath, _ =>
        {
            var entries = new List<BdkXmlDocumentationEntry>();

            foreach (var xmlPath in package.XmlPaths)
            {
                try
                {
                    var document = XDocument.Load(xmlPath, LoadOptions.None);
                    if (!string.Equals(document.Root?.Name.LocalName, "doc", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreach (var member in document.Descendants("member"))
                    {
                        var memberName = member.Attribute("name")?.Value?.Trim();
                        if (string.IsNullOrWhiteSpace(memberName))
                        {
                            continue;
                        }

                        var summary = CollapseWhitespace(member.Element("summary")?.Value);
                        var remarks = CollapseWhitespace(member.Element("remarks")?.Value);
                        var combined = string.Join(" ", new[] { summary, remarks }.Where(v => !string.IsNullOrWhiteSpace(v)));
                        if (string.IsNullOrWhiteSpace(combined))
                        {
                            continue;
                        }

                        entries.Add(new BdkXmlDocumentationEntry(
                            package.PackageId,
                            package.Version,
                            xmlPath,
                            memberName,
                            combined,
                            Normalize($"{package.PackageId} {memberName} {combined}")));
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogDebug(ex, "Failed to parse package XML documentation '{Path}'.", xmlPath);
                }
            }

            return entries;
        });
    }

    private string GetGlobalPackagesPath()
    {
        if (!string.IsNullOrWhiteSpace(this.globalPackagesPath))
        {
            return this.globalPackagesPath;
        }

        this.globalPackagesPath =
            Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
            TryResolveFromDotNet() ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        return this.globalPackagesPath;
    }

    private static BdkPackageDescriptor? TryBuildDescriptor(string packageDirectory)
    {
        var packageId = Path.GetFileName(packageDirectory);
        if (string.IsNullOrWhiteSpace(packageId))
        {
            return null;
        }

        var versionDirectory = Directory.GetDirectories(packageDirectory)
            .OrderByDescending(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (versionDirectory is null)
        {
            return null;
        }

        var xmlPaths = Directory.GetFiles(versionDirectory, "*.xml", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return new BdkPackageDescriptor(packageId, Path.GetFileName(versionDirectory), versionDirectory, xmlPaths);
    }

    private static string? TryResolveFromDotNet()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "nuget locals global-packages --list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            var line = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(l => l.StartsWith("global-packages:", StringComparison.OrdinalIgnoreCase));

            if (line is null)
            {
                return null;
            }

            var path = line[(line.IndexOf(':') + 1)..].Trim();
            return Directory.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    private static BdkXmlDocumentationMatch? Score(BdkXmlDocumentationEntry entry, string normalizedQuery, IReadOnlyList<string> queryTerms)
    {
        var score = 0;

        if (entry.SearchText.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            score += 90;
        }

        foreach (var term in queryTerms)
        {
            if (entry.MemberName.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 35;
            }
            else if (entry.Summary.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 18;
            }
            else if (entry.PackageId.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 12;
            }
        }

        if (score <= 0)
        {
            return null;
        }

        return new BdkXmlDocumentationMatch
        {
            PackageId = entry.PackageId,
            Version = entry.Version,
            XmlPath = entry.XmlPath,
            MemberName = entry.MemberName,
            Summary = entry.Summary.Length <= 260 ? entry.Summary : entry.Summary[..260] + "...",
            Score = score
        };
    }

    private static IEnumerable<string> ExpandTerms(string normalizedQuery)
    {
        var terms = Regex.Matches(normalizedQuery, "[a-z0-9]+")
            .Select(match => match.Value)
            .Where(value => value.Length > 1)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (!terms.Contains("bdk", StringComparer.Ordinal))
        {
            terms.Add("bdk");
        }

        if (!terms.Contains("bitdevkit", StringComparer.Ordinal))
        {
            terms.Add("bitdevkit");
        }

        return terms;
    }

    private static string Normalize(string value)
    {
        return WhitespaceRegex.Replace(value.Trim().ToLowerInvariant(), " ");
    }

    private static string CollapseWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return WhitespaceRegex.Replace(value.Trim(), " ");
    }

    private sealed record BdkPackageDescriptor(string PackageId, string Version, string RootPath, IReadOnlyList<string> XmlPaths);
    private sealed record BdkXmlDocumentationEntry(string PackageId, string Version, string XmlPath, string MemberName, string Summary, string SearchText);
}

public sealed class BdkRepositoryGuidanceService(
    BdkDocumentationService documentationService,
    BdkRepositoryInspectorService inspector)
{
    private readonly BdkDocumentationService documentationService = documentationService;
    private readonly BdkRepositoryInspectorService inspector = inspector;

    public async Task<BdkRepositoryHelpResponse> GetHelpAsync(string query, CancellationToken cancellationToken)
    {
        var docs = await this.documentationService.GetDocumentationAsync(query, cancellationToken);
        var topic = this.inspector.InferTopic(docs.Document?.Path, query);
        var files = this.inspector.GetFiles(topic);

        return new BdkRepositoryHelpResponse
        {
            Query = query,
            Topic = topic,
            Documentation = docs,
            SuggestedFiles = files
        };
    }
}

public sealed class BdkRecipeService(
    BdkDocumentationService documentationService,
    BdkRepositoryInspectorService inspector)
{
    private readonly BdkDocumentationService documentationService = documentationService;
    private readonly BdkRepositoryInspectorService inspector = inspector;

    public async Task<BdkRecipeResponse> GetRecipeAsync(string query, string? module, string? layer, CancellationToken cancellationToken)
    {
        var docs = await this.documentationService.GetDocumentationAsync(query, cancellationToken);
        var topic = this.inspector.InferTopic(docs.Document?.Path, query);
        var resolvedModule = this.inspector.ResolveModule(module);
        var resolvedLayer = string.IsNullOrWhiteSpace(layer) ? this.inspector.GetDefaultLayer(topic) : layer.Trim();
        var exampleFiles = this.inspector.GetFiles(topic, resolvedModule)
            .Where(hint => !hint.Reason.Contains("test", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();
        var relatedTests = this.inspector.GetRelatedTests(topic, resolvedModule).Take(3).ToList();
        var snippets = this.inspector.GetSnippets(topic, resolvedModule, maxResults: 3);

        var sources = docs.Sources
            .Concat(exampleFiles.Select(file => new BdkDocumentationSourceAttribution
            {
                Kind = "local-repo",
                Name = file.Path,
                Location = file.Path,
                Summary = file.Reason
            }))
            .GroupBy(source => $"{source.Kind}:{source.Location}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        return new BdkRecipeResponse
        {
            Query = query,
            Topic = topic,
            Module = resolvedModule,
            Layer = resolvedLayer,
            Summary = this.inspector.BuildRecipeSummary(topic, docs.Summary, resolvedModule, resolvedLayer),
            Documentation = docs,
            ImplementationSteps = this.inspector.GetRecipeSteps(topic, resolvedModule, resolvedLayer),
            Constraints = this.inspector.GetConstraints(topic, resolvedLayer),
            ExampleFiles = exampleFiles,
            RelatedTests = relatedTests,
            Snippets = snippets,
            Sources = sources
        };
    }
}

public sealed class BdkConventionReviewService(BdkRepositoryInspectorService inspector)
{
    private static readonly Regex CommentRegex = new(
        @"//.*?$|/\*.*?\*/",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly BdkRepositoryInspectorService inspector = inspector;

    public BdkReviewResponse Review(string? query, string? module)
    {
        var topic = this.inspector.InferTopic(null, query ?? string.Empty);
        var modules = this.inspector.ResolveModules(module);
        var findings = new List<BdkConventionReviewFinding>();

        foreach (var moduleName in modules)
        {
            findings.AddRange(this.FindApplicationInfrastructureViolations(moduleName));
            findings.AddRange(this.FindEndpointPlacementViolations(moduleName));
            findings.AddRange(this.FindMissingValidatorSignals(moduleName));
            findings.AddRange(this.FindMissingTests(topic, moduleName));
        }

        return new BdkReviewResponse
        {
            Query = query ?? string.Empty,
            Topic = topic,
            Module = string.IsNullOrWhiteSpace(module) ? string.Join(", ", modules) : module!,
            Summary = findings.Count == 0
                ? "No convention issues were found for the requested bITdevKit checks."
                : $"Found {findings.Count} convention issue(s) across Application, Presentation, validation, or nearby tests.",
            Findings = findings,
            SuggestedFiles = this.inspector.GetFiles(topic, module).Take(6).ToList()
        };
    }

    private IReadOnlyList<BdkConventionReviewFinding> FindApplicationInfrastructureViolations(string module)
    {
        var findings = new List<BdkConventionReviewFinding>();
        var applicationRoot = this.inspector.Combine($"src/Modules/{module}/{module}.Application");
        if (!Directory.Exists(applicationRoot))
        {
            return findings;
        }

        foreach (var file in Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var content = ReadForReview(file);
            var checks = new[]
            {
                $"using BridgingIT.DevKit.Examples.GettingStarted.Modules.{module}.Infrastructure",
                ".Infrastructure.EntityFramework",
                "Microsoft.EntityFrameworkCore",
                "DbContext",
                "DbSet<"
            };

            var matched = checks.FirstOrDefault(token => content.Contains(token, StringComparison.Ordinal));
            if (matched is null)
            {
                continue;
            }

            findings.Add(new BdkConventionReviewFinding
            {
                Severity = "high",
                Rule = "application-layer-dependency",
                Message = "Application code should depend on bITdevKit abstractions such as repositories, not Infrastructure or EF Core types.",
                Path = this.inspector.ToRelative(file),
                Line = FindLine(file, matched),
                Evidence = matched,
                Suggestion = "Inject IGenericRepository<T>, IMapper, or domain/application abstractions instead of Infrastructure or DbContext types."
            });
        }

        return findings;
    }

    private IReadOnlyList<BdkConventionReviewFinding> FindMissingValidatorSignals(string module)
    {
        var findings = new List<BdkConventionReviewFinding>();
        var applicationRoot = this.inspector.Combine($"src/Modules/{module}/{module}.Application");
        if (!Directory.Exists(applicationRoot))
        {
            return findings;
        }

        var candidateFiles = Directory.GetFiles(applicationRoot, "*Command*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(applicationRoot, "*Query*.cs", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in candidateFiles)
        {
            var content = ReadForReview(file);
            var fileName = Path.GetFileNameWithoutExtension(file);
            var hasValidationSignal =
                content.Contains("[Validate", StringComparison.Ordinal) ||
                content.Contains("InlineValidator<", StringComparison.Ordinal) ||
                content.Contains("AbstractValidator<", StringComparison.Ordinal);

            var hasLikelyRequiredInput =
                Regex.IsMatch(content, @"public\s+\w*Model\s+\w+\s*\{", RegexOptions.Compiled) ||
                Regex.IsMatch(content, @"public\s+\w+\s+\w*Id\s*\{", RegexOptions.Compiled) ||
                Regex.IsMatch(content, @"public\s+" + Regex.Escape(fileName) + @"\s*\((?<args>[^)]*(string|Guid|int|long|DateTime|[A-Za-z]+Model)[^)]*)\)", RegexOptions.Compiled);

            if (!hasLikelyRequiredInput || hasValidationSignal)
            {
                continue;
            }

            var validatorTest = this.inspector.Combine($"tests/Modules/{module}/{module}.UnitTests");
            var hasNearbyValidatorTest = Directory.Exists(validatorTest) &&
                Directory.GetFiles(validatorTest, $"{fileName}ValidatorTests.cs", SearchOption.AllDirectories).Any();

            if (hasNearbyValidatorTest)
            {
                continue;
            }

            findings.Add(new BdkConventionReviewFinding
            {
                Severity = "medium",
                Rule = "validator-signal",
                Message = "Command/query has likely input data but no obvious validation marker or nearby validator test.",
                Path = this.inspector.ToRelative(file),
                Line = FindLine(file, fileName),
                Evidence = fileName,
                Suggestion = "Add property validation attributes or a [Validate] method, and add a matching validator test."
            });
        }

        return findings;
    }

    private IReadOnlyList<BdkConventionReviewFinding> FindEndpointPlacementViolations(string module)
    {
        var findings = new List<BdkConventionReviewFinding>();
        var moduleRoot = this.inspector.Combine($"src/Modules/{module}");
        if (!Directory.Exists(moduleRoot))
        {
            return findings;
        }

        var expectedSegment = $"{Path.DirectorySeparatorChar}{module}.Presentation{Path.DirectorySeparatorChar}";
        foreach (var file in Directory.GetFiles(moduleRoot, "*Endpoints.cs", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var content = ReadForReview(file);
            var looksLikeEndpoint = content.Contains("EndpointsBase", StringComparison.Ordinal) || content.Contains("MapGroup(", StringComparison.Ordinal);
            if (!looksLikeEndpoint || file.Contains(expectedSegment, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new BdkConventionReviewFinding
            {
                Severity = "medium",
                Rule = "endpoint-placement",
                Message = "Endpoint classes should live in the module Presentation layer.",
                Path = this.inspector.ToRelative(file),
                Line = FindLine(file, "EndpointsBase"),
                Evidence = "Endpoint class outside *.Presentation",
                Suggestion = $"Move the endpoint into src/Modules/{module}/{module}.Presentation/Web/Endpoints."
            });
        }

        return findings;
    }

    private IReadOnlyList<BdkConventionReviewFinding> FindMissingTests(string topic, string module)
    {
        var findings = new List<BdkConventionReviewFinding>();
        var relatedTests = this.inspector.GetRelatedTests(topic, module);
        if (relatedTests.Count > 0)
        {
            return findings;
        }

        findings.Add(new BdkConventionReviewFinding
        {
            Severity = "low",
            Rule = "nearby-tests",
            Message = $"No nearby test example was found for topic '{topic}' in module '{module}'.",
            Path = string.Empty,
            Line = 0,
            Evidence = topic,
            Suggestion = "Add or locate a unit/integration test near the same pattern so future changes have a local example."
        });

        return findings;
    }

    private static string ReadForReview(string path)
    {
        var raw = File.ReadAllText(path);
        return CommentRegex.Replace(raw, string.Empty);
    }

    private static int FindLine(string path, string needle)
    {
        var lines = File.ReadAllLines(path);
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(needle, StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return 0;
    }
}

public sealed class BdkRepositoryInspectorService
{
    private readonly string repositoryRoot = Directory.GetCurrentDirectory();

    private static readonly IReadOnlyList<BdkTopicDefinition> Topics =
    [
        new(
            "domain-modeling",
            "Domain",
            "features-domain",
            "Domain",
            ["domain", "aggregate", "aggregates", "value object", "value objects", "typed id", "typed ids", "enumeration", "enumerations", "change builder"],
            [
                "Use get_bdk_recipe for domain aggregates and value objects.",
                "Use get_bdk_snippets for domain model examples."
            ]),
        new(
            "domain-events",
            "Domain Events",
            "features-domain-events",
            "Domain+Infrastructure",
            ["domain event", "domain events", "outbox event", "aggregate event", "event publishing"],
            [
                "Use get_bdk_recipe for aggregate domain events.",
                "Use get_bdk_snippets for domain event handlers and outbox examples."
            ]),
        new(
            "domain-specifications",
            "Domain Specifications",
            "features-domain-specifications",
            "Domain+Application",
            ["specification", "specifications", "criteria", "named specification", "unique specification"],
            [
                "Use get_bdk_recipe for reusable specifications.",
                "Use get_bdk_docs for filtering-to-specification guidance."
            ]),
        new(
            "modules",
            "Modules",
            "features-modules",
            "Presentation+Infrastructure",
            ["module", "modules", "modular", "webmodule"],
            [
                "Use get_bdk_recipe for adding a module.",
                "Use get_bdk_proj for module registration entrypoints."
            ]),
        new(
            "commands-queries",
            "Application Commands and Queries",
            "features-application-commands-queries",
            "Application",
            ["command", "commands", "query", "queries", "cqs", "cqrs"],
            [
                "Use get_bdk_recipe for commands and queries.",
                "Use get_bdk_snippets for command and query examples."
            ]),
        new(
            "requester-notifier",
            "Requester and Notifier",
            "features-requester-notifier",
            "Host+Application",
            ["requester", "notifier", "behavior", "behaviors", "handler policy", "request pipeline"],
            [
                "Use get_bdk_docs for requester pipeline behaviors.",
                "Use get_bdk_snippets for default requester/notifier wiring."
            ]),
        new(
            "messaging",
            "Messaging",
            "features-messaging",
            "Host+Infrastructure",
            ["messaging", "message", "messages", "broker", "publish", "subscribe", "outbox messaging"],
            [
                "Use get_bdk_recipe for broker-backed messaging.",
                "Use get_bdk_docs for broker and outbox setup details."
            ]),
        new(
            "queueing",
            "Queueing",
            "features-queueing",
            "Host+Infrastructure",
            ["queueing", "queue", "queues", "enqueue", "work item", "single consumer"],
            [
                "Use get_bdk_recipe for queue-backed background work.",
                "Use get_bdk_docs for queue broker and operational endpoint setup."
            ]),
        new(
            "notifications",
            "Notifications",
            "features-notifications",
            "Application+Infrastructure",
            ["notifications", "notification", "email", "smtp", "mail", "queued notification"],
            [
                "Use get_bdk_recipe for notification delivery flows.",
                "Use get_bdk_docs for SMTP, storage provider, and outbox setup."
            ]),
        new(
            "presentation-endpoints",
            "Presentation Endpoints",
            "features-presentation-endpoints",
            "Presentation",
            ["endpoint", "endpoints", "presentation", "minimal api", "route", "http"],
            [
                "Use get_bdk_recipe for adding an endpoint.",
                "Use get_bdk_snippets for endpoint mapping examples."
            ]),
        new(
            "jobs",
            "Job Scheduling",
            "features-jobscheduling",
            "Application+Host",
            ["job", "jobs", "jobscheduling", "scheduler", "quartz", "background"],
            [
                "Use get_bdk_recipe for adding a job.",
                "Use get_bdk_snippets for job registration and job class examples."
            ]),
        new(
            "startup-tasks",
            "Startup Tasks",
            "features-startuptasks",
            "Application",
            ["startup task", "startup tasks", "startuptask", "startuptasks", "seeder", "seed"],
            [
                "Use get_bdk_recipe for adding a startup task.",
                "Use get_bdk_snippets for startup task registration examples."
            ]),
        new(
            "pipelines",
            "Pipelines",
            "features-pipelines",
            "Application",
            ["pipelines", "pipeline", "workflow", "workflow pipeline", "hooks", "behaviors", "pipeline step"],
            [
                "Use get_bdk_recipe for multi-step workflows.",
                "Use get_bdk_docs for pipeline hooks, behaviors, and execution options."
            ]),
        new(
            "filtering",
            "Filtering",
            "features-filtering",
            "Presentation+Application",
            ["filtering", "filter", "filters", "filter model", "search filter", "query filter"],
            [
                "Use get_bdk_recipe for filterable endpoints and queries.",
                "Use get_bdk_snippets for local FilterModel usage."
            ]),
        new(
            "results",
            "Results",
            "features-results",
            "Application+Domain",
            ["results", "result", "map", "bind", "ensure", "unless", "operation scope"],
            [
                "Use get_bdk_recipe for result-based flows.",
                "Use get_bdk_snippets for local Result pipelines."
            ]),
        new(
            "rules",
            "Rules",
            "features-rules",
            "Application+Domain",
            ["rule", "rules", "validation", "business rule"],
            [
                "Use get_bdk_docs for Rules feature guidance.",
                "Use get_bdk_snippets for local rule usage examples."
            ]),
        new(
            "repositories",
            "Domain Repositories",
            "features-domain-repositories",
            "Application+Infrastructure",
            ["repository", "repositories", "persistence", "findoptions"],
            [
                "Use get_bdk_recipe for repository-backed handlers.",
                "Use get_bdk_snippets for repository registration and usage."
            ]),
        new(
            "document-storage",
            "DocumentStorage",
            "features-storage-documents",
            "Application+Infrastructure",
            ["document storage", "documents", "document", "document store", "document key"],
            [
                "Use get_bdk_recipe for document storage clients.",
                "Use get_bdk_docs for provider and behavior setup."
            ]),
        new(
            "file-storage",
            "FileStorage",
            "features-storage-files",
            "Application+Infrastructure",
            ["file storage", "files", "file", "file monitoring"],
            [
                "Use get_bdk_recipe for file storage providers.",
                "Use get_bdk_docs for provider registration and monitoring guidance."
            ]),
        new(
            "mappings",
            "Common Mapping",
            "common-mapping",
            "Presentation",
            ["mapping", "mapster", "mapper", "dto"],
            [
                "Use get_bdk_recipe for mapper registrations.",
                "Use get_bdk_snippets for Mapster registration examples."
            ]),
        new(
            "general",
            "General",
            string.Empty,
            "Host",
            ["bdk", "bitdevkit"],
            [
                "Open the bdk://repo/patterns resource.",
                "Use get_bdk_proj for lightweight file hints."
            ])
    ];

    public string ResolveModule(string? module)
    {
        var modules = this.GetModuleNames();
        if (!string.IsNullOrWhiteSpace(module))
        {
            var exact = modules.FirstOrDefault(name => string.Equals(name, module, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(exact))
            {
                return exact;
            }
        }

        return modules.FirstOrDefault() ?? string.Empty;
    }

    public IReadOnlyList<string> ResolveModules(string? module)
    {
        if (!string.IsNullOrWhiteSpace(module))
        {
            var resolved = this.ResolveModule(module);
            return !string.IsNullOrWhiteSpace(resolved) ? [resolved] : Array.Empty<string>();
        }

        return this.GetModuleNames();
    }

    public string InferTopic(string? path, string query)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            var normalizedPath = path.ToLowerInvariant();
            var matchedByPath = Topics.FirstOrDefault(topic =>
                !string.IsNullOrWhiteSpace(topic.DocsPathHint) &&
                normalizedPath.Contains(topic.DocsPathHint, StringComparison.Ordinal));

            if (matchedByPath is not null)
            {
                return matchedByPath.Key;
            }
        }

        var normalizedQuery = Normalize(query);
        var best = Topics
            .Select(topic => new
            {
                Topic = topic,
                Score = topic.Aliases.Sum(alias => normalizedQuery.Contains(Normalize(alias), StringComparison.Ordinal) ? alias.Length : 0)
            })
            .OrderByDescending(match => match.Score)
            .FirstOrDefault();

        return best is not null && best.Score > 0 ? best.Topic.Key : "general";
    }

    public string GetDefaultLayer(string topic)
    {
        return Topics.FirstOrDefault(candidate => candidate.Key == topic)?.DefaultLayer ?? "Host";
    }

    public string BuildRecipeSummary(string topic, string documentationSummary, string module, string layer)
    {
        var display = Topics.FirstOrDefault(candidate => candidate.Key == topic)?.DisplayName ?? "bITdevKit";
        var moduleText = string.IsNullOrWhiteSpace(module) ? "the current repository" : module;
        return $"{display} recipe for {moduleText} in the {layer} layer. {documentationSummary}".Trim();
    }

    public IReadOnlyList<string> GetRecipeSteps(string topic, string module, string layer)
    {
        return topic switch
        {
            "domain-modeling" =>
            [
                $"Model the feature in `src/Modules/{module}/{module}.Domain` with aggregates, value objects, typed ids, and smart enumerations before touching outer layers.",
                "Prefer factory methods and aggregate methods that return `Result` instead of exposing mutable setters or leaking validation into handlers.",
                "Use the fluent change builder for state transitions that need ordered checks, event registration, or collection updates.",
                "Back new domain primitives with focused domain unit tests so invariants stay local to the model."
            ],
            "domain-events" =>
            [
                "Raise immutable domain events from aggregates when a business-significant change has already happened, not before persistence succeeds.",
                "Choose direct repository publication for local in-process reactions, or outbox-backed publication when side effects must survive restarts and retries.",
                "Keep handlers outside the aggregate in Application and let repository behaviors plus notifier infrastructure dispatch them.",
                "Make the DbContext expose the outbox set and register the outbox hosted service when durable event publication is required."
            ],
            "domain-specifications" =>
            [
                "Express reusable selection criteria as named `ISpecification<T>` implementations or focused `Specification<T>` compositions in the Domain layer.",
                "Use specifications for criteria that must work both in memory and in repository queries, especially uniqueness and reusable read-model constraints.",
                "Compose specifications with `And(...)`, `Or(...)`, and `Not()` instead of duplicating predicates in handlers or endpoints.",
                "Let Application queries pass specifications into repository APIs, while external filter payloads translate into specifications rather than replacing them."
            ],
            "modules" =>
            [
                $"Start from `src/Modules/{module}` and keep the module composition in `{module}Module.cs`.",
                "Bind configuration through the module configuration type and validator rather than scattering configuration reads.",
                "Register startup tasks, job scheduling, repositories, and endpoints from the module entrypoint.",
                "Keep the host limited to `AddModules(...).WithModule<...>()` and app-level behaviors."
            ],
            "commands-queries" =>
            [
                "Author the request as a `[Command]` or `[Query]` partial type in the Application layer.",
                "Keep business logic in one or more `[Handle]` methods and inject dependencies through handler parameters.",
                "Use `IGenericRepository<T>`, `IMapper`, rules, and result pipelines instead of direct DbContext access.",
                "Add validator coverage and handler/unit tests near the command or query."
            ],
            "requester-notifier" =>
            [
                "Wire requester/notifier once in host startup and keep the default behaviors centralized.",
                "Let commands, queries, and events stay focused on business logic while behaviors handle tracing, validation, retry, and timeout.",
                "Use module scope behavior when the request should respect module enablement.",
                "Prefer a recipe plus snippet lookup before introducing custom behaviors."
            ],
            "messaging" =>
            [
                "Register messaging once from the host or module composition, add subscriptions explicitly, and choose the broker that matches durability and delivery needs.",
                "Define thin `IMessage` payloads and `IMessageHandler<T>` implementations, keeping business side effects inside handlers rather than publishers.",
                "Use publisher and handler behaviors for retry, timeout, metrics, and module scoping instead of duplicating cross-cutting logic in every handler.",
                "Prefer outbox-backed publication and operational endpoints when message delivery must survive restarts, be retried, or be inspected by support tooling."
            ],
            "queueing" =>
            [
                "Model one logical work item per queue message type and register exactly one queue handler for it.",
                "Use the in-process broker for local and test scenarios, or the Entity Framework broker when work must be durable, leased, and inspectable across nodes.",
                "Keep queue producers limited to `Enqueue(...)` calls while the broker runtime owns retries, waiting-for-handler states, and pause or resume controls.",
                "Expose queueing operational endpoints separately when support staff need retained-message inspection, retry, archive, or queue-type pause controls."
            ],
            "notifications" =>
            [
                "Depend on `INotificationService<TMessage>` from Application code and keep transport details such as SMTP or storage providers in Infrastructure wiring.",
                "Use direct send for immediate flows and queue plus outbox processing only when a persistent storage provider is configured.",
                "Prefer Fake SMTP and in-memory storage for local verification and tests, then switch to a real SMTP client and persistent provider in deployed environments.",
                "Keep message construction in handlers or services, while delivery, retry, and queued status updates remain owned by the notification feature."
            ],
            "presentation-endpoints" =>
            [
                "Place endpoint classes in the module Presentation project under `Web/Endpoints`.",
                "Use `MapGroup(...)` and requester-based Minimal API handlers instead of embedding business logic in endpoint methods.",
                "Map `Result` values with the built-in HTTP helpers such as `MapHttpOk`, `MapHttpCreated`, or `MapHttpNoContent`.",
                "Back endpoint changes with integration tests in the module IntegrationTests project."
            ],
            "jobs" =>
            [
                "Define the job in the Application layer and inherit from the devkit job base when you need shared logging and scheduling behavior.",
                "Register the job from module or host scheduling setup with explicit schedule, name, and scope.",
                "Resolve repositories or other scoped services inside the job execution path rather than holding onto scoped instances.",
                "Add job-focused unit tests and keep job scheduling infrastructure in host/module wiring."
            ],
            "startup-tasks" =>
            [
                "Implement the startup task in the Application layer and keep the task focused on initialization work.",
                "Register it through `AddStartupTasks(...).WithTask<...>()` from the module entrypoint.",
                "Use configuration and environment checks in registration, not inside unrelated handlers.",
                "Treat startup tasks as observable initialization steps and add nearby tests or examples when they become important."
            ],
            "pipelines" =>
            [
                "Prefer packaged pipeline definitions for reusable workflows, with a dedicated context type that carries all shared execution state.",
                "Author steps to return `PipelineControl` or `Result` so continuation, retry, break, and termination stay explicit instead of being hidden in imperative code.",
                "Reserve hooks for lifecycle observation and behaviors for cross-cutting execution concerns such as tracing, timing, and ambient scopes.",
                "Use background execution and the pipeline tracker only when a caller needs fire-and-forget processing with queryable progress or completion state."
            ],
            "filtering" =>
            [
                "Accept a `FilterModel` at the Presentation boundary, then pass it into Application queries and repository result APIs rather than hand-building ad hoc query parameters.",
                "Use filtering for client-driven sorting, includes, and paging, while named specifications remain the reusable domain-side criteria building blocks.",
                "Prefer POST for large or sensitive filter payloads and keep GET for simple, URL-sized filter requests.",
                "Return paged results from repository-backed queries so paging metadata stays consistent with the filter contract."
            ],
            "results" =>
            [
                "Model expected success and failure with `Result`, `Result<T>`, or `ResultPaged<T>` instead of exceptions for normal control flow.",
                "Compose workflows with `Map`, `Bind`, `Tap`, `Ensure`, `Unless`, and related operations so failure short-circuits stay explicit and consistent.",
                "Use repository result extensions and HTTP mapping helpers to preserve result semantics from Infrastructure through Presentation.",
                "Reach for operation scopes only when a result chain needs explicit commit or rollback semantics around transactions, files, or other scoped resources."
            ],
            "rules" =>
            [
                "Keep rule composition explicit with `Rule.Add(...)` and `RuleSet` helpers.",
                "Use custom rules for business-specific checks that should stay reusable and testable.",
                "Invoke rules from handlers or aggregates at the boundary where the business decision is made.",
                "Prefer clear `Result` failures over exceptions for expected business-rule failures."
            ],
            "repositories" =>
            [
                "Register repositories from Infrastructure/module composition and consume them through abstractions in Application.",
                "Use repository `Result` APIs and query/filter options instead of leaking EF-specific access into handlers.",
                "Keep include/query complexity in repository options or specifications, not controller/endpoints.",
                "Use architecture tests to keep Application free from Infrastructure dependencies."
            ],
            "mappings" =>
            [
                "Register mapping once with `AddMapping().WithMapster<...>()` or the module mapper register.",
                "Keep mapping configuration in the module-specific mapper register rather than ad hoc handler code.",
                "Use the devkit `IMapper` abstraction in handlers and result pipelines.",
                "Add mapper registration tests when introducing non-trivial conversions."
            ],
            "document-storage" =>
            [
                "Register an `IDocumentStoreClient<T>` for each document type and choose the provider in composition code, not in handlers.",
                "Use `DocumentKey` plus `DocumentKeyFilter` to keep partition and row-key semantics explicit for exact, prefix, or suffix lookups.",
                "Add client behaviors such as logging, retry, timeout, or cache in the order you want them wrapped around the provider-backed client.",
                "Batch upserts when writing multiple documents and keep document payloads stable for provider serialization and cache invalidation."
            ],
            "file-storage" =>
            [
                "Register named file storage providers through `AddFileStorage(...)` and resolve them via `IFileStorageFactory` rather than newing providers ad hoc.",
                "Use `WriteFileAsync` when the caller already has a source stream, or `OpenWriteFileAsync` when bytes should be streamed directly into the destination provider.",
                "Lean on provider behaviors and extension methods for logging, retry, compression, and cross-provider copy or move operations instead of re-implementing them.",
                "Bring in file monitoring only when you need real-time or scheduled scan-based change detection on top of file storage providers."
            ],
            _ =>
            [
                "Start from routed docs with `get_bdk_docs`.",
                "Use `get_bdk_proj` for lightweight file hints or `get_bdk_snippets` for exact local examples.",
                "Promote repeated guidance into repo-aware recipes rather than guessing file locations.",
                "Use `review_bdk_usage` to sanity-check architecture and nearby tests."
            ]
        };
    }

    public IReadOnlyList<string> GetConstraints(string topic, string layer)
    {
        return topic switch
        {
            "domain-modeling" =>
            [
                "Keep aggregates, value objects, typed ids, and enumerations in the Domain layer and free from Infrastructure dependencies.",
                "Prefer aggregate methods and factories that enforce invariants through `Result` instead of mutable setters.",
                "Raise side effects through domain events rather than direct service calls from the domain model."
            ],
            "domain-events" =>
            [
                "Aggregates may register events, but publication belongs to repository behaviors and notifier infrastructure after persistence.",
                "Choose one publication mode per aggregate persistence path to avoid duplicate event delivery.",
                "Handlers should remain idempotent when outbox-backed retries are enabled."
            ],
            "domain-specifications" =>
            [
                "Use specifications for reusable selection criteria, not for imperative business workflows.",
                "Keep specification definitions in Domain and consume them from Application queries or repository calls.",
                "Prefer typed expressions over dynamic string specifications unless the criteria truly come from external metadata."
            ],
            "commands-queries" =>
            [
                "Application should depend on repositories and mapper abstractions, not DbContext or Infrastructure namespaces.",
                "Validation belongs with the request definition or a clearly paired validator.",
                "Keep HTTP concerns out of Application handlers."
            ],
            "messaging" =>
            [
                "Messaging is pub/sub fan-out; do not use it when exactly one logical consumer should own a work item.",
                "Keep message contracts serializable and handlers idempotent when durable brokers or outbox delivery are enabled.",
                "Operational broker endpoints should be protected behind authorization in production."
            ],
            "queueing" =>
            [
                "Queueing is single-consumer work dispatch; reject designs that assume fan-out to multiple handlers for one queue type.",
                "Durable queue brokers are at-least-once, so handlers must tolerate reprocessing.",
                "Keep queue and type pause or resume operations in operational surfaces, not in business handlers."
            ],
            "notifications" =>
            [
                "Keep transport setup in composition and do not let Application handlers depend on MailKit or provider details.",
                "Only rely on `QueueAsync(...)` when a persistent storage provider and outbox processing are configured.",
                "Treat large attachments deliberately because queued notifications may persist them."
            ],
            "presentation-endpoints" =>
            [
                "Endpoints belong in Presentation and should delegate to IRequester.",
                "Use HTTP result mapping helpers rather than hand-written status code branching.",
                "Keep DTO mapping at the boundary."
            ],
            "modules" =>
            [
                "Module wiring should stay in module composition classes and the host.",
                "Respect module boundaries and avoid direct cross-module internal references.",
                "Keep feature-specific registrations grouped by module."
            ],
            "repositories" =>
            [
                "Keep repository registration in Infrastructure or module composition.",
                "Prefer repository abstractions and result-based APIs in handlers.",
                "Avoid EF-specific query logic in Application."
            ],
            "pipelines" =>
            [
                "Use pipelines for explicit multi-step workflows, not for trivial one-method operations.",
                "Keep business step logic in steps and leave tracing, timing, and lifecycle observation to behaviors and hooks.",
                "Shared workflow state belongs in the pipeline context, not in static or ambient mutable state."
            ],
            "filtering" =>
            [
                "Treat filter payloads as boundary contracts and validate them server-side before repository execution.",
                "Use specifications and find options as the repository-facing model rather than leaking raw transport details deeper into the domain.",
                "Be deliberate about paging limits and includes to avoid expensive queries or excessive payloads."
            ],
            "results" =>
            [
                "Use exceptions only for exceptional conditions; normal business failures should stay in `Result` values.",
                "Preserve errors and messages through the chain instead of flattening everything into strings too early.",
                "Map results to HTTP or UI responses at the boundary rather than collapsing them inside Application logic."
            ],
            "mappings" =>
            [
                "Keep mapping explicit and testable at the boundary.",
                "Prefer module-local Mapster registrations over scattered inline mappings.",
                "Treat mapping failures as configuration problems worth testing."
            ],
            "document-storage" =>
            [
                "Keep document store clients in Application and provider selection in Infrastructure or composition.",
                "Use stable partition and row-key conventions because filtering, caching, and provider efficiency depend on them.",
                "Behavior order matters; register decorators intentionally from outermost to innermost."
            ],
            "file-storage" =>
            [
                "Resolve providers by name through the factory so file handling stays swappable and testable.",
                "Always inspect `Result` failures and use progress reporting for long-running copy, move, or compression operations.",
                "Only rely on real-time monitoring when the provider supports notifications; otherwise prefer scheduled scans."
            ],
            _ =>
            [
                $"Favor the {layer} layer as the primary home for this pattern.",
                "Use the current module as the canonical local example before inventing a new structure.",
                "Keep bITdevKit usage explicit, observable, and repository-aligned."
            ]
        };
    }

    public string GetPatternsMarkdown()
    {
        var builder = new StringBuilder();
        var modules = this.GetModuleNames();

        builder.AppendLine("# bITdevKit Repo Patterns");
        builder.AppendLine();
        builder.AppendLine("## Current Modules");
        if (modules.Count == 0)
        {
            builder.AppendLine("- No modules discovered.");
        }
        else
        {
            foreach (var module in modules)
            {
                builder.AppendLine($"- `{module}`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Source Precedence");
        builder.AppendLine("1. Live GitHub `INDEX.md` routing table");
        builder.AppendLine("2. Routed GitHub documentation page");
        builder.AppendLine("3. Matching local NuGet package XML docs when available");
        builder.AppendLine("4. Local repository examples and tests");
        builder.AppendLine();
        builder.AppendLine("## Available Tools");
        builder.AppendLine("- `get_bdk_docs`: Routed docs with source attribution.");
        builder.AppendLine("- `get_bdk_proj`: Lightweight file hints for the current repo.");
        builder.AppendLine("- `get_bdk_recipe`: Repo-aware implementation recipe.");
        builder.AppendLine("- `get_bdk_snippets`: Exact local snippets with line numbers.");
        builder.AppendLine("- `review_bdk_usage`: Convention-focused review of bITdevKit usage.");
        builder.AppendLine();
        builder.AppendLine("## Supported Topics");

        foreach (var topic in Topics.Where(topic => topic.Key != "general"))
        {
            builder.AppendLine($"### {topic.DisplayName}");
            builder.AppendLine($"- Key: `{topic.Key}`");
            builder.AppendLine($"- Default layer: `{topic.DefaultLayer}`");
            builder.AppendLine($"- Aliases: {string.Join(", ", topic.Aliases.Select(alias => $"`{alias}`"))}");
            foreach (var prompt in topic.ExamplePrompts)
            {
                builder.AppendLine($"- Prompt: `{prompt}`");
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    public IReadOnlyList<BdkRepositoryFileHint> GetFiles(string topic, string? module = null)
    {
        var hints = new List<BdkRepositoryFileHint>();
        var modules = this.ResolveModules(module);

        switch (topic)
        {
            case "domain-modeling":
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}-README.md", $"{moduleName} module walkthrough with domain modeling examples.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Domain/Model", "*Customer.cs", $"{moduleName} aggregate root example.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Domain/Model", "*EmailAddress.cs", $"{moduleName} value object example.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Domain", "*Tests.cs", $"{moduleName} domain unit tests.");
                }

                break;

            case "domain-events":
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} outbox domain event service and repository behavior wiring.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Domain/Events", "*.cs", $"{moduleName} domain event contract example.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Events", "*.cs", $"{moduleName} domain event handler example.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Events", "*Tests.cs", $"{moduleName} domain event handler tests.");
                }

                break;

            case "domain-specifications":
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}-README.md", $"{moduleName} module notes on query and repository patterns.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Queries", "*FindAllQuery.cs", $"{moduleName} closest local read-model query using repository filtering.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} repository composition point for specification-backed queries.");
                }

                break;

            case "modules":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host composition root where modules are added.");
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} module registration entrypoint.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Application/{moduleName}Configuration.cs", $"{moduleName} strongly typed module configuration.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/Web/Endpoints", "*Endpoints.cs", $"{moduleName} endpoint mapping example.");
                    AddIfExists(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/ArchitectureTests.cs", $"{moduleName} architecture boundary tests.");
                }

                break;

            case "commands-queries":
                AddIfExists(hints, "src/Presentation.Web.Server/ProgramExtensions.cs", "Default requester/notifier behaviors and pipeline wiring.");
                foreach (var moduleName in modules)
                {
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Commands", "*Command*.cs", $"{moduleName} command example.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Queries", "*Query*.cs", $"{moduleName} query example.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Commands", "*Command*Tests.cs", $"{moduleName} command tests.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Queries", "*Query*Tests.cs", $"{moduleName} query tests.");
                }

                break;

            case "requester-notifier":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host requester/notifier registration.");
                AddIfExists(hints, "src/Presentation.Web.Server/ProgramExtensions.cs", "Default requester/notifier behavior chain.");
                foreach (var moduleName in modules)
                {
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Commands", "*Command*.cs", $"{moduleName} requester command example.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Queries", "*Query*.cs", $"{moduleName} requester query example.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Events", "*.cs", $"{moduleName} notifier event handler example.");
                }

                break;

            case "messaging":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host composition root for future messaging registration.");
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} closest local durable async wiring via outbox domain events.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Events", "*.cs", $"{moduleName} closest local async side-effect handlers.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}-README.md", $"{moduleName} architecture notes for async side effects and pipelines.");
                }

                break;

            case "queueing":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host composition root for queue broker registration.");
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} composition point for queueing registration.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Jobs", "*.cs", $"{moduleName} closest local background work implementation example.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}-README.md", $"{moduleName} background processing walkthrough.");
                }

                break;

            case "notifications":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host composition root for SMTP and notification service registration.");
                foreach (var moduleName in modules)
                {
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Events", "*.cs", $"{moduleName} closest local side-effect handler for notification-style work.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Commands", "*DeleteCommand.cs", $"{moduleName} notifier usage in an application command.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}-README.md", $"{moduleName} module walkthrough with notifier and side-effect notes.");
                }

                break;

            case "presentation-endpoints":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host endpoint registration and mapping.");
                foreach (var moduleName in modules)
                {
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/Web/Endpoints", "*Endpoints.cs", $"{moduleName} endpoint definitions.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.IntegrationTests/Presentation/Web", "*EndpointTests.cs", $"{moduleName} endpoint integration tests.");
                }

                break;

            case "jobs":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "App-level job scheduling setup.");
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} module-level job registration.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Jobs", "*.cs", $"{moduleName} job implementation example.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Jobs", "*Tests.cs", $"{moduleName} job unit tests.");
                }

                break;

            case "startup-tasks":
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} startup task registration.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application", "*Task.cs", $"{moduleName} startup task implementation.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Application/{moduleName}Configuration.cs", $"{moduleName} configuration controlling startup behavior.");
                }

                break;

            case "pipelines":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host composition root for pipeline registration.");
                foreach (var moduleName in modules)
                {
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Commands", "*CreateCommand.cs", $"{moduleName} closest local multi-step result workflow.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}-README.md", $"{moduleName} workflow and pipeline-style handler walkthrough.");
                }

                break;

            case "filtering":
                foreach (var moduleName in modules)
                {
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Queries", "*FindAllQuery.cs", $"{moduleName} `FilterModel` query example.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/Web/Endpoints", "*Endpoints.cs", $"{moduleName} endpoint surface for read queries.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Queries", "*FindAllQuery*Tests.cs", $"{moduleName} filtering query tests.");
                }

                break;

            case "results":
                foreach (var moduleName in modules)
                {
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Commands", "*CreateCommand.cs", $"{moduleName} result-chain command example.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Domain/Model", "*Customer.cs", $"{moduleName} domain result usage example.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/Web/Endpoints", "*Endpoints.cs", $"{moduleName} result-to-HTTP mapping example.");
                }

                break;

            case "rules":
                foreach (var moduleName in modules)
                {
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Rules", "*.cs", $"{moduleName} custom rule implementation.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Commands", "*Command*.cs", $"{moduleName} command using rule composition.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Commands", "*ValidatorTests.cs", $"{moduleName} validation test example.");
                }

                break;

            case "repositories":
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} repository registration.");
                    AddFirstMatch(hints, $"src/Modules/{moduleName}/{moduleName}.Application/Commands", "*Command*.cs", $"{moduleName} handler depending on IGenericRepository.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Infrastructure/EntityFramework/{moduleName}DbContext.cs", $"{moduleName} DbContext implementation kept in Infrastructure.");
                    AddIfExists(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/ArchitectureTests.cs", $"{moduleName} clean architecture guardrails.");
                }

                break;

            case "mappings":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host mapping registration.");
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}MapperRegister.cs", $"{moduleName} Mapster registration.");
                    AddIfExists(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Presentation/{moduleName}MapperRegisterTests.cs", $"{moduleName} mapper registration tests.");
                }

                break;

            case "document-storage":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host composition root for document store client registration.");
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} module composition point for storage client registration.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}-README.md", $"{moduleName} overall architecture reference for adding new infrastructure services.");
                }

                break;

            case "file-storage":
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Host composition root for file storage provider registration.");
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} module composition point for file storage or monitoring setup.");
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}-README.md", $"{moduleName} architecture reference for background and infrastructure features.");
                }

                break;

            default:
                AddIfExists(hints, "src/Presentation.Web.Server/Program.cs", "Primary application composition root.");
                foreach (var moduleName in modules)
                {
                    AddIfExists(hints, $"src/Modules/{moduleName}/{moduleName}.Presentation/{moduleName}Module.cs", $"{moduleName} main module entrypoint.");
                }

                break;
        }

        return hints
            .GroupBy(hint => hint.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(12)
            .ToList();
    }

    public IReadOnlyList<BdkRepositoryFileHint> GetRelatedTests(string topic, string? module = null)
    {
        var hints = new List<BdkRepositoryFileHint>();
        var modules = this.ResolveModules(module);

        foreach (var moduleName in modules)
        {
            switch (topic)
            {
                case "domain-modeling":
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Domain/Model", "*Tests.cs", $"{moduleName} domain model tests.");
                    break;

                case "domain-events":
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Events", "*Tests.cs", $"{moduleName} domain event handler tests.");
                    break;

                case "domain-specifications":
                case "filtering":
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Queries", "*FindAllQuery*Tests.cs", $"{moduleName} query tests closest to specification/filtering usage.");
                    break;

                case "modules":
                    AddIfExists(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/ArchitectureTests.cs", $"{moduleName} architecture tests.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.IntegrationTests/Presentation/Web", "*EndpointTests.cs", $"{moduleName} integration endpoint tests.");
                    break;

                case "commands-queries":
                case "requester-notifier":
                case "results":
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Commands", "*Command*Tests.cs", $"{moduleName} command tests.");
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Queries", "*Query*Tests.cs", $"{moduleName} query tests.");
                    break;

                case "presentation-endpoints":
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.IntegrationTests/Presentation/Web", "*EndpointTests.cs", $"{moduleName} endpoint integration tests.");
                    break;

                case "jobs":
                    AddFirstMatch(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Application/Jobs", "*Tests.cs", $"{moduleName} job unit tests.");
                    break;

                case "mappings":
                    AddIfExists(hints, $"tests/Modules/{moduleName}/{moduleName}.UnitTests/Presentation/{moduleName}MapperRegisterTests.cs", $"{moduleName} mapper tests.");
                    break;
            }
        }

        return hints
            .GroupBy(hint => hint.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(8)
            .ToList();
    }

    public BdkSnippetsResponse GetSnippetsResponse(string query, string? module, int maxResults)
    {
        var topic = this.InferTopic(null, query);
        var resolvedModule = this.ResolveModule(module);
        var snippets = this.GetSnippets(topic, resolvedModule, maxResults);

        return new BdkSnippetsResponse
        {
            Query = query,
            Topic = topic,
            Module = resolvedModule,
            Message = snippets.Count > 0
                ? $"Returned {snippets.Count} local snippet(s) for topic '{topic}'."
                : $"No local snippets were found for topic '{topic}'.",
            Snippets = snippets
        };
    }

    public IReadOnlyList<BdkSnippetMatch> GetSnippets(string topic, string? module, int maxResults)
    {
        var resolvedModule = this.ResolveModule(module);
        var candidates = this.GetSnippetCandidates(topic, resolvedModule);
        var snippets = candidates
            .Select(candidate => this.TryBuildSnippet(candidate))
            .Where(snippet => snippet is not null)
            .Select(snippet => snippet!)
            .GroupBy(snippet => snippet.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(snippet => snippet.Score)
            .ThenBy(snippet => snippet.Path, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(maxResults, 1, 10))
            .ToList();

        return snippets;
    }

    public string Combine(string relativePath)
    {
        return Path.Combine(this.repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public string ToRelative(string fullPath)
    {
        return Path.GetRelativePath(this.repositoryRoot, fullPath).Replace('\\', '/');
    }

    private IReadOnlyList<SnippetCandidate> GetSnippetCandidates(string topic, string module)
    {
        var candidates = new List<SnippetCandidate>();

        switch (topic)
        {
            case "domain-modeling":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Domain/Model/CustomerAggregate/Customer.cs", "Aggregate root with typed id, result-based factory, and fluent change builder.", 100, ["[TypedEntityId<Guid>]", "public static Result<Customer> Create", "return this.Change()"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Domain/Model/EmailAddress.cs", "Value object example using Result and Rule composition.", 95, ["public static Result<EmailAddress> Create", "Rule", "RuleSet.IsValidEmail"]);
                AddCandidateIfExists(candidates, $"tests/Modules/{module}/{module}.UnitTests/Domain/Model/CustomerAggregate/CustomerTests.cs", "Domain aggregate unit tests.", 80, ["Customer", "Should", "Result"]);
                break;

            case "domain-events":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Domain/Events/CustomerCreatedDomainEvent.cs", "Domain event contract example.", 95, ["DomainEventBase", "CustomerCreatedDomainEvent"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Events/CustomerCreatedDomainEventHandler.cs", "Domain event handler example.", 100, ["DomainEventHandlerBase", "Process("]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Outbox domain event registration in module composition.", 90, ["WithOutboxDomainEventService", "RepositoryOutboxDomainEventBehavior"]);
                break;

            case "domain-specifications":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Queries/CustomerFindAllQuery.cs", "Closest local repository query that already consumes `FilterModel`.", 80, ["public FilterModel Filter", "FindAllResultAsync(Filter"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}-README.md", "Module walkthrough referencing repository and filter-driven reads.", 60, ["FindAllResultAsync", "Filter", "Result"]);
                break;

            case "modules":
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/Program.cs", "Host module composition.", 95, ["AddModules(", ".WithModule<"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Module registration entrypoint.", 100, ["public override IServiceCollection Register", "services.AddStartupTasks", "services.AddEntityFrameworkRepository"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/{module}Configuration.cs", "Module configuration model.", 80, ["public class", "ConnectionStrings"]);
                break;

            case "commands-queries":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Commands/CustomerCreateCommand.cs", "Source-generated command pattern.", 100, ["[Command]", "[Handle]", "[Validate]"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Queries/CustomerFindAllQuery.cs", "Query pattern using repository and mapper abstractions.", 95, ["[Query]", "[Handle]", "IGenericRepository"]);
                AddCandidateIfExists(candidates, $"tests/Modules/{module}/{module}.UnitTests/Application/Commands/CustomerCreateCommandValidatorTests.cs", "Command validator test example.", 75, ["new CustomerCreateCommand.Validator", "TestValidate"]);
                break;

            case "requester-notifier":
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/ProgramExtensions.cs", "Default requester/notifier behavior chain.", 100, ["WithDefaultBehaviors", "ValidationPipelineBehavior", "TimeoutPipelineBehavior"]);
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/Program.cs", "Host requester/notifier registration.", 90, ["builder.Services.AddRequester()", "builder.Services.AddNotifier()"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Events/CustomerCreatedDomainEventHandler.cs", "Notifier/event handler example.", 75, ["HandleAsync", "Result"]);
                break;

            case "messaging":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Closest local durable async delivery wiring via outbox domain events.", 85, ["WithOutboxDomainEventService", "RepositoryOutboxDomainEventBehavior"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Events/CustomerCreatedDomainEventHandler.cs", "Closest local async side-effect handler.", 75, ["DomainEventHandlerBase", "Process("]);
                break;

            case "queueing":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Jobs/CustomerExportJob.cs", "Closest local background work implementation for queueing-oriented flows.", 70, ["JobBase", "Process("]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Module composition point where a queue broker would be registered.", 60, ["services.AddJobScheduling", "RegisterScoped"]);
                break;

            case "notifications":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Commands/CustomerDeleteCommand.cs", "Application command using `INotifier` for follow-up side effects.", 80, ["INotifier notifier", "PublishAsync(notifier"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Events/CustomerCreatedDomainEventHandler.cs", "Closest local notification-style reaction point.", 70, ["LogInformation", "Process("]);
                break;

            case "presentation-endpoints":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/Web/Endpoints/CustomerEndpoints.cs", "Endpoint mapping using IRequester and HTTP result helpers.", 100, ["MapGroup(", "requester", "MapHttpCreated"]);
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/Program.cs", "Global endpoint registration and mapping.", 85, ["AddEndpoints<SystemEndpoints>", "MapEndpoints()"]);
                AddCandidateIfExists(candidates, $"tests/Modules/{module}/{module}.IntegrationTests/Presentation/Web/CustomerEndpointTests.cs", "Endpoint integration test example.", 80, ["GetAsync", "PostAsync", "Be200Ok"]);
                break;

            case "jobs":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Module-level job scheduling registration.", 100, ["services.AddJobScheduling", ".WithJob<CustomerExportJob>()"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Jobs/CustomerExportJob.cs", "Job implementation example.", 95, ["public class", "JobBase", "Process("]);
                AddCandidateIfExists(candidates, $"tests/Modules/{module}/{module}.UnitTests/Application/Jobs/CustomerExportJobTests.cs", "Job unit test example.", 80, ["CustomerExportJob", "Process("]);
                break;

            case "startup-tasks":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Startup task registration from the module.", 100, ["services.AddStartupTasks", "WithTask<"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/{module}DomainSeederTask.cs", "Startup task implementation example.", 95, ["IStartupTask", "ExecuteAsync"]);
                break;

            case "pipelines":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Commands/CustomerCreateCommand.cs", "Closest local multi-step workflow composed with a result pipeline.", 85, ["await Result<CustomerModel>", ".BindAsync(", ".UnlessAsync("]);
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/ProgramExtensions.cs", "Existing pipeline behavior composition for requester/notifier.", 75, ["WithDefaultBehaviors", "TracingBehavior", "TimeoutPipelineBehavior"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}-README.md", "Module walkthrough explaining the multi-step handler pipeline.", 65, ["Result pipeline", "Context Pattern", "short-circuits"]);
                break;

            case "filtering":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Queries/CustomerFindAllQuery.cs", "Local `FilterModel` repository query example.", 100, ["public FilterModel Filter", "FindAllResultAsync(Filter"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/Web/Endpoints/CustomerEndpoints.cs", "Read endpoint surface that can forward filters to queries.", 70, ["MapGet(", "CustomerFindAllQuery"]);
                break;

            case "results":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Commands/CustomerCreateCommand.cs", "Application result chain with validation, rules, persistence, and mapping.", 100, ["await Result<CustomerModel>", ".BindAsync(", ".MapResult<"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Domain/Model/CustomerAggregate/Customer.cs", "Domain result usage for factories and fluent updates.", 90, ["public static Result<Customer> Create", "return this.Change()"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/Web/Endpoints/CustomerEndpoints.cs", "Boundary mapping from Result to HTTP responses.", 75, ["MapHttp", "MapHttpOk", "MapHttpCreated"]);
                break;

            case "rules":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Rules/EmailShouldBeUniqueRule.cs", "Custom rule example.", 100, ["RuleBase", "ExecuteAsync", "Result"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Commands/CustomerCreateCommand.cs", "Rule composition inside a handler pipeline.", 85, ["Rule", "EmailShouldBeUniqueRule", "CheckAsync"]);
                break;

            case "repositories":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Repository registration in module composition.", 100, ["AddEntityFrameworkRepository", "RepositoryLoggingBehavior"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Application/Commands/CustomerCreateCommand.cs", "Application handler using IGenericRepository.", 90, ["IGenericRepository<Customer>", "InsertResultAsync"]);
                break;

            case "mappings":
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}MapperRegister.cs", "Module Mapster registration.", 100, ["public class", "IRegister", "config.ForType"]);
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/Program.cs", "Host mapping registration.", 80, ["AddMapping().WithMapster"]);
                break;

            case "document-storage":
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/Program.cs", "Composition root where a document store client would be registered.", 60, ["builder.Services", "AddModules"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Module composition point for storage client registration.", 55, ["public override IServiceCollection Register", "services.AddSqlServerDbContext"]);
                break;

            case "file-storage":
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/Program.cs", "Composition root where file storage providers would be registered.", 60, ["builder.Services", "AddModules"]);
                AddCandidateIfExists(candidates, $"src/Modules/{module}/{module}.Presentation/{module}Module.cs", "Module composition point for file storage or monitoring setup.", 55, ["public override IServiceCollection Register", "services.AddJobScheduling"]);
                break;

            default:
                AddCandidateIfExists(candidates, "src/Presentation.Web.Server/Program.cs", "Primary host composition example.", 80, ["builder.Services"]);
                break;
        }

        return candidates;
    }

    private BdkSnippetMatch? TryBuildSnippet(SnippetCandidate candidate)
    {
        var fullPath = this.Combine(candidate.RelativePath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var lines = File.ReadAllLines(fullPath);
        if (lines.Length == 0)
        {
            return null;
        }

        var lineIndex = FindBestLine(lines, candidate.Markers);
        var start = Math.Max(0, lineIndex - 5);
        var end = Math.Min(lines.Length - 1, lineIndex + 10);
        var excerpt = string.Join("\n", lines.Skip(start).Take(end - start + 1)).TrimEnd();
        if (string.IsNullOrWhiteSpace(excerpt))
        {
            return null;
        }

        return new BdkSnippetMatch
        {
            Path = candidate.RelativePath,
            Language = GetLanguage(candidate.RelativePath),
            LineStart = start + 1,
            LineEnd = end + 1,
            Reason = candidate.Reason,
            Score = candidate.Score,
            Excerpt = excerpt
        };
    }

    private static int FindBestLine(IReadOnlyList<string> lines, IReadOnlyList<string> markers)
    {
        foreach (var marker in markers)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(marker, StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            return i;
        }

        return 0;
    }

    private static string GetLanguage(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".csx" => "csharp",
            ".csproj" => "xml",
            ".props" => "xml",
            ".json" => "json",
            ".md" => "markdown",
            _ => "text"
        };
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

    private static string Normalize(string value)
    {
        return Regex.Replace(value.Trim().ToLowerInvariant(), "\\s+", " ");
    }

    private void AddCandidateIfExists(List<SnippetCandidate> candidates, string relativePath, string reason, int score, IReadOnlyList<string> markers)
    {
        if (File.Exists(this.Combine(relativePath)))
        {
            candidates.Add(new SnippetCandidate(relativePath, reason, score, markers));
        }
    }

    private void AddIfExists(List<BdkRepositoryFileHint> hints, string relativePath, string reason)
    {
        if (File.Exists(this.Combine(relativePath)))
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
        var fullFolder = this.Combine(relativeFolder);
        if (!Directory.Exists(fullFolder))
        {
            return;
        }

        var match = Directory.GetFiles(fullFolder, pattern, SearchOption.AllDirectories)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (match is null)
        {
            return;
        }

        hints.Add(new BdkRepositoryFileHint
        {
            Path = this.ToRelative(match),
            Reason = reason
        });
    }

    private sealed record SnippetCandidate(string RelativePath, string Reason, int Score, IReadOnlyList<string> Markers);
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

    public IReadOnlyList<BdkXmlDocumentationMatch> XmlDocumentation { get; init; } = Array.Empty<BdkXmlDocumentationMatch>();

    public required IReadOnlyList<BdkDocumentationSourceAttribution> Sources { get; init; }
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

public sealed class BdkDocumentationSourceAttribution
{
    public required string Kind { get; init; }

    public required string Name { get; init; }

    public string Location { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;
}

public sealed class BdkXmlDocumentationMatch
{
    public required string PackageId { get; init; }

    public required string Version { get; init; }

    public required string XmlPath { get; init; }

    public required string MemberName { get; init; }

    public required string Summary { get; init; }

    public required int Score { get; init; }
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

public sealed class BdkRecipeResponse
{
    public required string Query { get; init; }

    public required string Topic { get; init; }

    public required string Module { get; init; }

    public required string Layer { get; init; }

    public required string Summary { get; init; }

    public required BdkDocumentationResponse Documentation { get; init; }

    public required IReadOnlyList<string> ImplementationSteps { get; init; }

    public required IReadOnlyList<string> Constraints { get; init; }

    public required IReadOnlyList<BdkRepositoryFileHint> ExampleFiles { get; init; }

    public required IReadOnlyList<BdkRepositoryFileHint> RelatedTests { get; init; }

    public required IReadOnlyList<BdkSnippetMatch> Snippets { get; init; }

    public required IReadOnlyList<BdkDocumentationSourceAttribution> Sources { get; init; }
}

public sealed class BdkSnippetsResponse
{
    public required string Query { get; init; }

    public required string Topic { get; init; }

    public required string Module { get; init; }

    public required string Message { get; init; }

    public required IReadOnlyList<BdkSnippetMatch> Snippets { get; init; }
}

public sealed class BdkSnippetMatch
{
    public required string Path { get; init; }

    public required string Language { get; init; }

    public required int LineStart { get; init; }

    public required int LineEnd { get; init; }

    public required string Reason { get; init; }

    public required int Score { get; init; }

    public required string Excerpt { get; init; }
}

public sealed class BdkReviewResponse
{
    public required string Query { get; init; }

    public required string Topic { get; init; }

    public required string Module { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<BdkConventionReviewFinding> Findings { get; init; }

    public required IReadOnlyList<BdkRepositoryFileHint> SuggestedFiles { get; init; }
}

public sealed class BdkConventionReviewFinding
{
    public required string Severity { get; init; }

    public required string Rule { get; init; }

    public required string Message { get; init; }

    public required string Path { get; init; }

    public required int Line { get; init; }

    public string Evidence { get; init; } = string.Empty;

    public string Suggestion { get; init; } = string.Empty;
}

public sealed record BdkTopicDefinition(
    string Key,
    string DisplayName,
    string DocsPathHint,
    string DefaultLayer,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> ExamplePrompts);
