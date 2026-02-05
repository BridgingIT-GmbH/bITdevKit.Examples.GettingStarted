// BDK CLI - Utilities Module
/// <summary>
/// Common utility methods for file operations, process management, etc.
/// </summary>

using System.Diagnostics;
using Spectre.Console;

public static class Utils
{
    /// <summary>
    /// Find files matching pattern in directory
    /// </summary>
    public static List<string> FindFiles(string directory, string pattern, bool recursive = false)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        
        if (!Directory.Exists(directory))
            return new List<string>();
        
        return Directory.GetFiles(directory, pattern, searchOption)
            .Select(f => Path.GetRelativePath(Directory.GetCurrentDirectory(), f)
                .Replace("\\", "/"))
            .ToList();
    }
    
    /// <summary>
    /// Open file in default application
    /// </summary>
    public static void OpenFile(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;
            
            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                Verb = "open"
            };
            
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Failed to open file: {Markup.Escape(ex.Message)}[/]");
        }
    }
    
    /// <summary>
    /// Open folder in default file manager
    /// </summary>
    public static void OpenFolder(string folderPath)
    {
        try
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;
            
            var startInfo = new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true,
                Verb = "open"
            };
            
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Failed to open folder: {Markup.Escape(ex.Message)}[/]");
        }
    }
    
    /// <summary>
    /// Open URL in default browser
    /// </summary>
    public static void OpenUrl(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url))
                return;
            
            var startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Failed to open URL: {Markup.Escape(ex.Message)}[/]");
        }
    }
    
    /// <summary>
    /// Get all .NET processes
    /// </summary>
    public static List<ProcessInfo> GetDotnetProcesses()
    {
        var processes = new List<ProcessInfo>();
        
        try
        {
            var allProcesses = Process.GetProcesses();
            
            foreach (var process in allProcesses)
            {
                try
                {
                    var processName = process.ProcessName;
                    var displayName = $"{processName} (PID: {process.Id})";
                    
                    if (process.MainWindowHandle != IntPtr.Zero || processName.Contains("dotnet"))
                    {
                        processes.Add(new ProcessInfo
                        {
                            Id = process.Id,
                            Name = processName,
                            DisplayName = displayName
                        });
                    }
                }
                catch
                {
                    // Skip processes that can't be accessed
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Failed to get processes: {Markup.Escape(ex.Message)}[/]");
        }
        
        return processes.OrderBy(p => p.Name).ToList();
    }
}

/// <summary>
/// Process information for user selection
/// </summary>
public class ProcessInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
