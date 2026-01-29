// .NET CLI wrapper - cross-platform

import { CrossPlatformExecutor, type CommandOptions } from '../core/executor';

export class DotnetCli {
  private executor = new CrossPlatformExecutor();
  
  /**
   * Find solution file in current directory (.sln or .slnx)
   */
  private findSolutionFile(cwd: string = process.cwd()): string | null {
    const fs = require('fs');
    const files = fs.readdirSync(cwd);
    
    // Look for .slnx first (new format), then .sln
    const slnx = files.find((f: string) => f.endsWith('.slnx'));
    if (slnx) return slnx;
    
    const sln = files.find((f: string) => f.endsWith('.sln'));
    if (sln) return sln;
    
    return null;
  }
  
  async build(options: {
    cwd?: string;
    configuration?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['build'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    if (options.configuration) {
      args.push('-c', options.configuration);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async restore(options: {
    cwd?: string;
    onOutput?: (line: string) => void;
  } = {}) {
    const args = ['restore'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput
    });
  }
  
  async clean(options: {
    cwd?: string;
    onOutput?: (line: string) => void;
  } = {}) {
    const args = ['clean'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput
    });
  }
  
  async test(options: {
    cwd?: string;
    filter?: string;
    noRestore?: boolean;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['test'];
    
    if (options.filter) {
      args.push('--filter', options.filter);
    }
    if (options.noRestore) {
      args.push('--no-restore');
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async version(options: {
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    return this.executor.execute('dotnet', ['--version'], {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
}
