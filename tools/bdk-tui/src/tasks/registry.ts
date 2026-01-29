// Task registry - defines all available tasks

import type { TaskDefinition, TaskContext } from '../types/task';
import { DotnetCli } from '../lib/dotnet';

const dotnetCli = new DotnetCli();

export const TASK_REGISTRY: TaskDefinition[] = [
  // Build & Maintenance
  {
    key: 'build',
    label: 'Build',
    description: 'Build the solution',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      
      try {
        const result = await dotnetCli.build({
          cwd: ctx.config.rootPath,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });
        
        return {
          success: result.success,
          exitCode: result.exitCode,
          output: result.stdout,
          error: result.stderr,
          duration: Date.now() - startTime
        };
      } catch (error) {
        return {
          success: false,
          exitCode: 1,
          error: String(error),
          duration: Date.now() - startTime
        };
      }
    }
  },
  
  {
    key: 'clean',
    label: 'Clean',
    description: 'Clean the solution',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      
      try {
        const result = await dotnetCli.clean({
          cwd: ctx.config.rootPath,
          onOutput: ctx.onOutput
        });
        
        return {
          success: result.success,
          exitCode: result.exitCode,
          output: result.stdout,
          error: result.stderr,
          duration: Date.now() - startTime
        };
      } catch (error) {
        return {
          success: false,
          exitCode: 1,
          error: String(error),
          duration: Date.now() - startTime
        };
      }
    }
  },
  
  {
    key: 'restore',
    label: 'Restore',
    description: 'Restore NuGet packages',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      
      try {
        const result = await dotnetCli.restore({
          cwd: ctx.config.rootPath,
          onOutput: ctx.onOutput
        });
        
        return {
          success: result.success,
          exitCode: result.exitCode,
          output: result.stdout,
          error: result.stderr,
          duration: Date.now() - startTime
        };
      } catch (error) {
        return {
          success: false,
          exitCode: 1,
          error: String(error),
          duration: Date.now() - startTime
        };
      }
    }
  },
  
  {
    key: 'version',
    label: 'Show .NET Version',
    description: 'Display installed .NET SDK version',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      
      try {
        const result = await dotnetCli.version({
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });
        
        return {
          success: result.success,
          exitCode: result.exitCode,
          output: result.stdout,
          error: result.stderr,
          duration: Date.now() - startTime
        };
      } catch (error) {
        return {
          success: false,
          exitCode: 1,
          error: String(error),
          duration: Date.now() - startTime
        };
      }
    }
  },
];

// Group tasks by category
export function getCategories(): string[] {
  const categories = new Set(TASK_REGISTRY.map(t => t.category));
  return Array.from(categories).sort();
}

export function getTasksByCategory(category: string): TaskDefinition[] {
  return TASK_REGISTRY.filter(t => t.category === category);
}

export function getTaskByKey(key: string): TaskDefinition | undefined {
  return TASK_REGISTRY.find(t => t.key === key);
}
