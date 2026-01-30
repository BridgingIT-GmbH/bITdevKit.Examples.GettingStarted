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
  
  async pack(options: {
    cwd?: string;
    configuration?: string;
    outputPath?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['pack'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    if (options.configuration) {
      args.push('-c', options.configuration);
    }
    
    if (options.outputPath) {
      args.push('-o', options.outputPath);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async format(options: {
    cwd?: string;
    verifyOnly?: boolean;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['format'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    if (options.verifyOnly) {
      args.push('--verify-no-changes');
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async toolRestore(options: {
    cwd?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    return this.executor.execute('dotnet', ['tool', 'restore'], {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async listPackages(options: {
    cwd?: string;
    vulnerable?: boolean;
    includeTransitive?: boolean;
    outdated?: boolean;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['list'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    args.push('package');
    
    if (options.vulnerable) {
      args.push('--vulnerable');
    }
    
    if (options.includeTransitive) {
      args.push('--include-transitive');
    }
    
    if (options.outdated) {
      args.push('--outdated');
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async runAnalyzers(options: {
    cwd?: string;
    errorLogPath?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['build'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    args.push(
      '-warnaserror',
      '/p:RunAnalyzers=true',
      '/p:EnableNETAnalyzers=true',
      '/p:AnalysisLevel=latest'
    );
    
    if (options.errorLogPath) {
      args.push(`/p:ErrorLog=${options.errorLogPath}`);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async outdated(options: {
    cwd?: string;
    upgrade?: boolean;
    include?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['outdated'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    if (options.upgrade) {
      args.push('--upgrade');
    }
    
    if (options.include) {
      args.push('-inc', options.include);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async toolRun(options: {
    cwd?: string;
    toolName: string;
    args?: string[];
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  }) {
    const args = ['tool', 'run', options.toolName];
    
    if (options.args) {
      args.push(...options.args);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async coverage(options: {
    cwd?: string;
    outputPath?: string;
    format?: 'html' | 'cobertura' | 'opencover';
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['test'];
    
    // Add solution file if found
    const solutionFile = this.findSolutionFile(options.cwd);
    if (solutionFile) {
      args.push(solutionFile);
    }
    
    args.push('--collect:XPlat Code Coverage');
    
    if (options.outputPath) {
      args.push('--results-directory', options.outputPath);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async toolList(options: {
    cwd?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    return this.executor.execute('dotnet', ['tool', 'list'], {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async toolUpdate(options: {
    cwd?: string;
    toolName?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['tool', 'update'];
    
    if (options.toolName) {
      args.push(options.toolName);
    } else {
      // Update all tools - we need to read manifest and update each
      return this.executor.execute('dotnet', ['tool', 'restore'], {
        cwd: options.cwd,
        onStdout: options.onOutput,
        onStderr: options.onError
      });
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Ensure tools are restored before running tool commands
   * This is a helper that should be called before any toolRun
   */
  async ensureToolsRestored(options: {
     cwd?: string;
     onOutput?: (line: string) => void;
     onError?: (line: string) => void;
   } = {}): Promise<boolean> {
     const result = await this.toolRestore({
       cwd: options.cwd,
       onOutput: options.onOutput,
       onError: options.onError
     });
     return result.success;
   }
  
  async run(options: {
    cwd?: string;
    projectPath?: string;
    configuration?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['run'];
    
    if (options.projectPath) {
      args.push('--project', options.projectPath);
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
  
  async watch(options: {
    cwd?: string;
    projectPath?: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['watch', 'run'];
    
    if (options.projectPath) {
      args.push('--project', options.projectPath);
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  async publish(options: {
    cwd?: string;
    projectPath?: string;
    configuration?: string;
    singleFile?: boolean;
    selfContained?: boolean;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  } = {}) {
    const args = ['publish'];
    
    if (options.projectPath) {
      args.push(options.projectPath);
    }
    
    if (options.configuration) {
      args.push('-c', options.configuration);
    }
    
    if (options.singleFile) {
      args.push('-p:PublishSingleFile=true');
    }
    
    if (options.selfContained) {
      args.push('--self-contained');
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.cwd,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
}
