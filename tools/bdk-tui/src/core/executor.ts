// Cross-platform command executor

import { spawn } from 'bun';
import { platform } from 'os';

export interface CommandOptions {
  cwd?: string;
  env?: Record<string, string>;
  onStdout?: (line: string) => void;
  onStderr?: (line: string) => void;
}

export interface CommandResult {
  success: boolean;
  exitCode: number;
  stdout: string;
  stderr: string;
  duration: number;
}

export class CrossPlatformExecutor {
  private isWindows = platform() === 'win32';
  
  /**
   * Execute a cross-platform command (dotnet, docker, git, etc.)
   */
  async execute(
    command: string,
    args: string[],
    options: CommandOptions = {}
  ): Promise<CommandResult> {
    const startTime = Date.now();
    
    console.log(`[exec] ${command} ${args.join(' ')}`);
    
    // Commands like 'dotnet', 'docker', 'git' work the same on all platforms
    const proc = spawn([command, ...args], {
      cwd: options.cwd || process.cwd(),
      env: { ...process.env, ...options.env },
      stdout: 'pipe',
      stderr: 'pipe',
      windowsHide: true, // Hide console window on Windows
    });
    
    let stdout = '';
    let stderr = '';
    
    // Stream stdout
    if (proc.stdout) {
      const decoder = new TextDecoder();
      for await (const chunk of proc.stdout) {
        const text = decoder.decode(chunk);
        stdout += text;
        
        if (options.onStdout) {
          const lines = text.split('\n');
          lines.forEach((line, index) => {
            if (line.trim()) {
              options.onStdout!(line);
            }
            // Handle last line without newline (only if it's not already covered by a split line)
            else if (index === lines.length - 1 && lines.length === 1 && text.trim()) {
              options.onStdout!(text.trim());
            }
          });
        }
      }
    }
    
    // Stream stderr
    if (proc.stderr) {
      const decoder = new TextDecoder();
      for await (const chunk of proc.stderr) {
        const text = decoder.decode(chunk);
        stderr += text;
        
        if (options.onStderr) {
          const lines = text.split('\n');
          lines.forEach((line, index) => {
            if (line.trim()) {
              options.onStderr!(line);
            }
            // Handle last line without newline
            else if (index === lines.length - 1 && lines.length === 1 && text.trim()) {
              options.onStderr!(text.trim());
            }
          });
        }
      }
    }
    
    const exitCode = await proc.exited;
    const duration = Date.now() - startTime;
    
    return {
      success: exitCode === 0,
      exitCode,
      stdout,
      stderr,
      duration
    };
  }
  
  /**
   * Normalize paths for the current platform
   */
  normalizePath(path: string): string {
    // Forward slashes work on all platforms
    return path.replace(/\\/g, '/');
  }
  
  /**
   * Get platform-specific executable name
   */
  getExecutable(baseName: string): string {
    return this.isWindows && !baseName.endsWith('.exe') 
      ? `${baseName}.exe` 
      : baseName;
  }
}
