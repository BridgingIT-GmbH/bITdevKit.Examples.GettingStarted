// Task registry - defines all available tasks

import type { TaskDefinition, TaskContext } from '../types/task';
import { DotnetCli } from '../lib/dotnet';
import { EntityFrameworkCli } from '../lib/ef';
import { DockerCli } from '../lib/docker';
import { showModuleDialog } from '../ui/dialogs/ModuleDialog';
import { showDbContextDialog } from '../ui/dialogs/DbContextDialog';
import { showTextInputDialog, validators } from '../ui/dialogs/TextInputDialog';
import { showConfirmDialog } from '../ui/dialogs/ConfirmDialog';
import { join } from 'path';
import { mkdirSync, existsSync, readdirSync } from 'fs';

const dotnetCli = new DotnetCli();
const efCli = new EntityFrameworkCli();
const dockerCli = new DockerCli();

// Helper to ensure tools are restored before running tool-dependent tasks
async function ensureToolsRestored(ctx: any): Promise<boolean> {
  try {
    const result = await dotnetCli.ensureToolsRestored({
      cwd: ctx.config.rootPath,
      onOutput: (line: string) => {
        if (ctx.onOutput && line.includes('Restored')) {
          ctx.onOutput('Restoring .NET tools...');
        }
      }
    });
    return result;
  } catch {
    return false;
  }
}

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
    key: 'build-release',
    label: 'Build (Release)',
    description: 'Build the solution in Release configuration',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.build({
          cwd: ctx.config.rootPath,
          configuration: 'Release',
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
    key: 'build-nr',
    label: 'Build (No Restore)',
    description: 'Build without restoring packages (fast)',
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
    key: 'pack',
    label: 'Pack',
    description: 'Create NuGet packages (Release)',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.pack({
          cwd: ctx.config.rootPath,
          configuration: 'Release',
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
    key: 'format-check',
    label: 'Format Check',
    description: 'Verify code formatting (no changes)',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.format({
          cwd: ctx.config.rootPath,
          verifyOnly: true,
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
    key: 'format-apply',
    label: 'Format Apply',
    description: 'Auto-format code files',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.format({
          cwd: ctx.config.rootPath,
          verifyOnly: false,
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
    key: 'tool-restore',
    label: 'Tool Restore',
    description: 'Restore .NET local tools',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.toolRestore({
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
    key: 'tool-list',
    label: 'Tool List',
    description: 'List installed .NET local tools',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.toolList({
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
    key: 'tool-update',
    label: 'Tool Update',
    description: 'Update all .NET tools in manifest',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        if (ctx.onOutput) {
          ctx.onOutput('Reading tool manifest...');
        }

        // Read the manifest to get all tools
        const manifestPath = join(ctx.config.rootPath, '.config', 'dotnet-tools.json');
        if (!existsSync(manifestPath)) {
          return {
            success: false,
            exitCode: 1,
            error: 'Tool manifest not found at .config/dotnet-tools.json',
            duration: Date.now() - startTime
          };
        }

        const manifestContent = await import('fs').then(fs => fs.promises.readFile(manifestPath, 'utf8'));
        const manifest = JSON.parse(manifestContent);
        const tools = Object.keys(manifest.tools || {});

        if (tools.length === 0) {
          return {
            success: true,
            exitCode: 0,
            output: 'No tools in manifest',
            error: '',
            duration: Date.now() - startTime
          };
        }

        if (ctx.onOutput) {
          ctx.onOutput(`Found ${tools.length} tools to update:\n${tools.map(t => '  - ' + t).join('\n')}\n`);
        }

        let allSuccess = true;
        let combinedOutput = '';

        for (const tool of tools) {
          if (ctx.onOutput) {
            ctx.onOutput(`\nUpdating ${tool}...`);
          }

          const result = await dotnetCli.toolUpdate({
            cwd: ctx.config.rootPath,
            toolName: tool,
            onOutput: ctx.onOutput,
            onError: ctx.onError
          });

          if (!result.success) {
            allSuccess = false;
          }

          combinedOutput += result.stdout + '\n';
        }

        if (ctx.onOutput) {
          ctx.onOutput(`\nTool update complete. ${allSuccess ? 'All tools updated successfully.' : 'Some tools failed to update.'}`);
        }

        return {
          success: allSuccess,
          exitCode: allSuccess ? 0 : 1,
          output: combinedOutput,
          error: '',
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
    key: 'vulnerabilities',
    label: 'Check Vulnerabilities',
    description: 'List packages with known vulnerabilities',
    category: 'Security & Compliance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.listPackages({
          cwd: ctx.config.rootPath,
          vulnerable: true,
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
    key: 'outdated',
    label: 'Check Outdated Packages',
    description: 'List outdated NuGet packages',
    category: 'Security & Compliance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.listPackages({
          cwd: ctx.config.rootPath,
          outdated: true,
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
    key: 'outdated-json',
    label: 'Export Outdated Packages (JSON)',
    description: 'Export outdated packages to JSON file',
    category: 'Security & Compliance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        // Create output directory
        const outputDir = join(ctx.config.rootPath, '.tmp', 'compliance');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').replace('T', '_').split('T')[0];
        const jsonPath = join(outputDir, `outdated_${timestamp}.json`);

        // Find solution file
        const solutionFile = readdirSync(ctx.config.rootPath).find(f => f.endsWith('.slnx') || f.endsWith('.sln'));

        if (!solutionFile) {
          return {
            success: false,
            exitCode: 1,
            error: 'No solution file found',
            duration: Date.now() - startTime
          };
        }

        // Run dotnet list outdated
        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();

        const result = await executor.execute('dotnet', ['list', solutionFile, 'package', '--outdated'], {
          onStdout: ctx.onOutput,
          onStderr: ctx.onError
        });

        if (!result.success) {
          return {
            success: false,
            exitCode: result.exitCode,
            error: result.stderr,
            duration: Date.now() - startTime
          };
        }

        // Parse the output and convert to JSON
        const lines = result.stdout.split('\n');
        const packages: any[] = [];

        // Parse the table output (skipping header lines)
        for (const line of lines) {
          // Match pattern: >PackageName CurrentVersion LatestVersion
          const match = line.match(/^>(?:\s+)([^\s]+)\s+([^\s]+)\s+([^\s]+)\s+([^\s]+)/);
          if (match) {
            packages.push({
              name: match[1],
              current: match[2],
              wanted: match[3],
              latest: match[4]
            });
          }
        }

        // Write JSON to file
        const { writeFileSync } = await import('fs');
        writeFileSync(jsonPath, JSON.stringify(packages, null, 2), 'utf8');

        if (ctx.onOutput) {
          ctx.onOutput(`Exported ${packages.length} outdated packages to: ${jsonPath}`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Exported ${packages.length} packages to ${jsonPath}`,
          error: '',
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
    key: 'analyzers',
    label: 'Run Analyzers',
    description: 'Run .NET code analyzers',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.runAnalyzers({
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
    key: 'vulnerabilities-deep',
    label: 'Check Vulnerabilities (Deep)',
    description: 'List vulnerable packages including transitive dependencies',
    category: 'Security & Compliance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.listPackages({
          cwd: ctx.config.rootPath,
          vulnerable: true,
          includeTransitive: true,
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
    key: 'update-packages',
    label: 'Update Packages',
    description: 'Update all packages to latest compatible versions',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.outdated({
          cwd: ctx.config.rootPath,
          upgrade: true,
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
    key: 'update-packages-devkit',
    label: 'Update DevKit Packages',
    description: 'Update BridgingIT.DevKit packages only',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const result = await dotnetCli.outdated({
          cwd: ctx.config.rootPath,
          upgrade: true,
          include: 'BridgingIT.DevKit',
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
    key: 'analyzers-export',
    label: 'Analyzers Export',
    description: 'Run analyzers and export SARIF report',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        // Create output directory
        const outputDir = join(ctx.config.rootPath, '.tmp', 'analyzers');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').split('T')[0];
        const sarifPath = join(outputDir, `analyzers_${timestamp}.sarif`);

        const result = await dotnetCli.runAnalyzers({
          cwd: ctx.config.rootPath,
          errorLogPath: sarifPath,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`SARIF report written to: ${sarifPath}`);
        }

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
    key: 'licenses',
    label: 'Generate License Report',
    description: 'Generate package license report (JSON)',
    category: 'Security & Compliance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        // Tool restore first
        await dotnetCli.toolRestore({
          cwd: ctx.config.rootPath,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        // Create output directory
        const outputDir = join(ctx.config.rootPath, '.tmp', 'compliance');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').split('T')[0];
        const jsonPath = join(outputDir, `licenses_${timestamp}.json`);

        // Use solution file from config if available
        if (!ctx.config.solutionFile) {
          return {
            success: false,
            exitCode: 1,
            error: 'No solution file selected. Use solution selection dialog.',
            duration: Date.now() - startTime
          };
        }

        const result = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'nuget-license',
          args: ['-i', ctx.config.solutionFile, '-t', '-o', 'JsonPretty'],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`License report written to: ${jsonPath}`);
        }

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
    key: 'coverage',
    label: 'Run Tests with Coverage',
    description: 'Run all tests and collect code coverage',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const outputDir = join(ctx.config.rootPath, '.tmp', 'coverage');

        const result = await dotnetCli.coverage({
          cwd: ctx.config.rootPath,
          outputPath: outputDir,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`Coverage results written to: ${outputDir}`);
        }

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
    key: 'coverage-html',
    label: 'Coverage Report (HTML)',
    description: 'Generate HTML coverage report using ReportGenerator',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        // Ensure tools are restored
        await ensureToolsRestored(ctx);

        // Run coverage first
        const coverageDir = join(ctx.config.rootPath, '.tmp', 'coverage');

        await dotnetCli.coverage({
          cwd: ctx.config.rootPath,
          outputPath: coverageDir,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        // Generate HTML report with reportgenerator tool
        const htmlDir = join(ctx.config.rootPath, '.tmp', 'coverage-html');

        const result = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'reportgenerator',
          args: [
            `-reports:${coverageDir}/**/coverage.cobertura.xml`,
            `-targetdir:${htmlDir}`,
            '-reporttypes:Html'
          ],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`HTML coverage report written to: ${htmlDir}`);
          ctx.onOutput(`Open: ${htmlDir}/index.html`);
        }

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
    key: 'test-unit',
    label: 'Run Unit Tests',
    description: 'Run unit tests for selected module(s)',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'Unit Tests',
          allowAll: true,
        });

        if (!moduleResult) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const modules = moduleResult.isAll
          ? (await import('../lib/discovery.js')).discoverModules(ctx.config.rootPath).map(m => m.name)
          : [moduleResult.moduleName];

        let allSuccess = true;
        let combinedOutput = '';
        let combinedError = '';

        for (const moduleName of modules) {
          const testProject = join(
            ctx.config.rootPath,
            'tests',
            'Modules',
            moduleName,
            `${moduleName}.UnitTests`,
            `${moduleName}.UnitTests.csproj`
          );

          if (!existsSync(testProject)) {
            if (ctx.onOutput) {
              ctx.onOutput(`Skipping ${moduleName}: Unit test project not found`);
            }
            continue;
          }

          if (ctx.onOutput) {
            ctx.onOutput(`\n=== Running unit tests for ${moduleName} ===`);
          }

          const result = await dotnetCli.test({
            cwd: join(ctx.config.rootPath, 'tests', 'Modules', moduleName, `${moduleName}.UnitTests`),
            onOutput: ctx.onOutput,
            onError: ctx.onError
          });

          if (!result.success) {
            allSuccess = false;
          }

          combinedOutput += result.stdout + '\n';
          combinedError += result.stderr + '\n';
        }

        return {
          success: allSuccess,
          exitCode: allSuccess ? 0 : 1,
          output: combinedOutput,
          error: combinedError,
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
    key: 'test-integration',
    label: 'Run Integration Tests',
    description: 'Run integration tests for selected module(s)',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'Integration Tests',
          allowAll: true,
        });

        if (!moduleResult) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const modules = moduleResult.isAll
          ? (await import('../lib/discovery.js')).discoverModules(ctx.config.rootPath).map(m => m.name)
          : [moduleResult.moduleName];

        let allSuccess = true;
        let combinedOutput = '';
        let combinedError = '';

        for (const moduleName of modules) {
          const testProject = join(
            ctx.config.rootPath,
            'tests',
            'Modules',
            moduleName,
            `${moduleName}.IntegrationTests`,
            `${moduleName}.IntegrationTests.csproj`
          );

          if (!existsSync(testProject)) {
            if (ctx.onOutput) {
              ctx.onOutput(`Skipping ${moduleName}: Integration test project not found`);
            }
            continue;
          }

          if (ctx.onOutput) {
            ctx.onOutput(`\n=== Running integration tests for ${moduleName} ===`);
          }

          const result = await dotnetCli.test({
            cwd: join(ctx.config.rootPath, 'tests', 'Modules', moduleName, `${moduleName}.IntegrationTests`),
            onOutput: ctx.onOutput,
            onError: ctx.onError
          });

          if (!result.success) {
            allSuccess = false;
          }

          combinedOutput += result.stdout + '\n';
          combinedError += result.stderr + '\n';
        }

        return {
          success: allSuccess,
          exitCode: allSuccess ? 0 : 1,
          output: combinedOutput,
          error: combinedError,
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
    key: 'test-all',
    label: 'Run All Tests',
    description: 'Run both unit and integration tests',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'All Tests',
          allowAll: true,
        });

        if (!moduleResult) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const modules = moduleResult.isAll
          ? (await import('../lib/discovery.js')).discoverModules(ctx.config.rootPath).map(m => m.name)
          : [moduleResult.moduleName];

        let allSuccess = true;
        let combinedOutput = '';
        let combinedError = '';

        for (const moduleName of modules) {
          // Unit tests
          const unitTestProject = join(
            ctx.config.rootPath,
            'tests',
            'Modules',
            moduleName,
            `${moduleName}.UnitTests`,
            `${moduleName}.UnitTests.csproj`
          );

          if (existsSync(unitTestProject)) {
            if (ctx.onOutput) {
              ctx.onOutput(`\n=== Running unit tests for ${moduleName} ===`);
            }

            const unitResult = await dotnetCli.test({
              cwd: join(ctx.config.rootPath, 'tests', 'Modules', moduleName, `${moduleName}.UnitTests`),
              onOutput: ctx.onOutput,
              onError: ctx.onError
            });

            if (!unitResult.success) {
              allSuccess = false;
            }

            combinedOutput += unitResult.stdout + '\n';
            combinedError += unitResult.stderr + '\n';
          }

          // Integration tests
          const integrationTestProject = join(
            ctx.config.rootPath,
            'tests',
            'Modules',
            moduleName,
            `${moduleName}.IntegrationTests`,
            `${moduleName}.IntegrationTests.csproj`
          );

          if (existsSync(integrationTestProject)) {
            if (ctx.onOutput) {
              ctx.onOutput(`\n=== Running integration tests for ${moduleName} ===`);
            }

            const integrationResult = await dotnetCli.test({
              cwd: join(ctx.config.rootPath, 'tests', 'Modules', moduleName, `${moduleName}.IntegrationTests`),
              onOutput: ctx.onOutput,
              onError: ctx.onError
            });

            if (!integrationResult.success) {
              allSuccess = false;
            }

            combinedOutput += integrationResult.stdout + '\n';
            combinedError += integrationResult.stderr + '\n';
          }
        }

        return {
          success: allSuccess,
          exitCode: allSuccess ? 0 : 1,
          output: combinedOutput,
          error: combinedError,
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

  // Docker Tasks
  {
    key: 'docker-build-run',
    label: 'Docker: Build & Run',
    description: 'Build Docker image and run container',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const dockerfile = ctx.config.dockerFilePath || 'Dockerfile';
        const containerName = `${ctx.config.containerPrefix || 'app'}-web`;
        const imageTag = `${ctx.config.registryHost || 'localhost'}/${containerName}:latest`;

        // Build
        if (ctx.onOutput) {
          ctx.onOutput('Building Docker image...');
        }

        const buildResult = await dockerCli.build({
          dockerfile,
          tag: imageTag,
          context: ctx.config.rootPath,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (!buildResult.success) {
          return {
            success: false,
            exitCode: buildResult.exitCode,
            output: buildResult.stdout,
            error: buildResult.stderr,
            duration: Date.now() - startTime
          };
        }

        // Run
        if (ctx.onOutput) {
          ctx.onOutput('Running Docker container...');
        }

        const runResult = await dockerCli.run({
          image: imageTag,
          containerName,
          hostPort: 8080,
          containerPort: 8080,
          network: ctx.config.networkName || 'bridge',
          detached: true,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        return {
          success: runResult.success,
          exitCode: runResult.exitCode,
          output: runResult.stdout,
          error: runResult.stderr,
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
    key: 'docker-build-debug',
    label: 'Docker: Build (Debug)',
    description: 'Build Docker image in Debug configuration',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const dockerfile = ctx.config.dockerFilePath || 'Dockerfile';
        const containerName = `${ctx.config.containerPrefix || 'app'}-web`;
        const imageTag = `${ctx.config.registryHost || 'localhost'}/${containerName}:debug`;

        const result = await dockerCli.build({
          dockerfile,
          tag: imageTag,
          context: ctx.config.rootPath,
          buildArgs: { 'CONFIGURATION': 'Debug' },
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
    key: 'docker-build-release',
    label: 'Docker: Build (Release)',
    description: 'Build Docker image in Release configuration',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const dockerfile = ctx.config.dockerFilePath || 'Dockerfile';
        const containerName = `${ctx.config.containerPrefix || 'app'}-web`;
        const imageTag = `${ctx.config.registryHost || 'localhost'}/${containerName}:latest`;

        const result = await dockerCli.build({
          dockerfile,
          tag: imageTag,
          context: ctx.config.rootPath,
          buildArgs: { 'CONFIGURATION': 'Release' },
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
    key: 'docker-run',
    label: 'Docker: Run Container',
    description: 'Run Docker container from existing image',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const containerName = `${ctx.config.containerPrefix || 'app'}-web`;
        const imageTag = `${ctx.config.registryHost || 'localhost'}/${containerName}:latest`;

        const result = await dockerCli.run({
          image: imageTag,
          containerName,
          hostPort: 8080,
          containerPort: 8080,
          network: ctx.config.networkName || 'bridge',
          detached: true,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`Container running at http://localhost:8080`);
        }

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
    key: 'docker-stop',
    label: 'Docker: Stop Container',
    description: 'Stop running Docker container',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const containerName = `${ctx.config.containerPrefix || 'app'}-web`;

        const result = await dockerCli.stop({
          containerName,
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
    key: 'docker-remove',
    label: 'Docker: Remove Container',
    description: 'Remove Docker container',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const containerName = `${ctx.config.containerPrefix || 'app'}-web`;

        const result = await dockerCli.remove({
          containerName,
          force: true,
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
    key: 'docker-remove-image',
    label: 'Docker: Remove Image',
    description: 'Remove Docker image',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const containerName = `${ctx.config.containerPrefix || 'app'}-web`;
        const imageTag = `${ctx.config.registryHost || 'localhost'}/${containerName}:latest`;

        const result = await dockerCli.removeImage({
          imageTag,
          force: true,
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
    key: 'compose-up',
    label: 'Docker Compose: Up',
    description: 'Start services with Docker Compose',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const composeFile = ctx.config.dockerComposePath || 'docker-compose.yml';

        const result = await dockerCli.composeUp({
          composeFile,
          detached: true,
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
    key: 'compose-down',
    label: 'Docker Compose: Down',
    description: 'Stop and remove Docker Compose services',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const composeFile = ctx.config.dockerComposePath || 'docker-compose.yml';

        const result = await dockerCli.composeDown({
          composeFile,
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
    key: 'compose-down-clean',
    label: 'Docker Compose: Down (Clean)',
    description: 'Stop services and remove volumes',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const composeFile = ctx.config.dockerComposePath || 'docker-compose.yml';

        const result = await dockerCli.composeDown({
          composeFile,
          removeVolumes: true,
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
    key: 'compose-recreate',
    label: 'Docker Compose: Recreate',
    description: 'Recreate all Docker Compose services',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const composeFile = ctx.config.dockerComposePath || 'docker-compose.yml';

        // Down first
        await dockerCli.composeDown({
          composeFile,
          removeVolumes: true,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        // Then up
        const result = await dockerCli.composeUp({
          composeFile,
          detached: true,
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

  // OpenAPI Tasks
  {
    key: 'openapi-lint',
    label: 'OpenAPI: Lint Specification',
    description: 'Lint OpenAPI spec with Spectral (Docker)',
    category: 'API & Spec',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const specPath = 'src/Presentation.Web.Server/wwwroot/openapi.json';
        const fullSpecPath = join(ctx.config.rootPath, specPath);

        if (!existsSync(fullSpecPath)) {
          return {
            success: false,
            exitCode: 1,
            error: `OpenAPI spec not found: ${specPath}. Build the project first.`,
            duration: Date.now() - startTime
          };
        }

        // Check if Docker is running
        const isDockerRunning = await dockerCli.isDockerRunning();
        if (!isDockerRunning) {
          return {
            success: false,
            exitCode: 1,
            error: 'Docker daemon not running. Spectral lint requires Docker.',
            duration: Date.now() - startTime
          };
        }

        // Clean up any existing container
        await dockerCli.remove({
          containerName: 'spectral-lint',
          force: true,
        }).catch(() => {}); // Ignore if container doesn't exist

        // Run Spectral lint via Docker
        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();

        // Check if ruleset exists
        const rulesetPath = '.spectral.yaml';
        const fullRulesetPath = join(ctx.config.rootPath, rulesetPath);
        const hasRuleset = existsSync(fullRulesetPath);

        const dockerArgs = [
          'run',
          '--rm',
          '--name', 'spectral-lint',
          '-v', `${ctx.config.rootPath}:/work`,
          'stoplight/spectral:latest',
          'lint',
          `/work/${specPath}`,
          '--format', 'stylish',
          '--fail-severity', 'error'
        ];

        // Add ruleset if it exists
        if (hasRuleset) {
          dockerArgs.push('-r', `/work/${rulesetPath}`);
        }

        const result = await executor.execute('docker', dockerArgs, {
          onStdout: ctx.onOutput,
          onStderr: ctx.onError
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
    key: 'openapi-client-dotnet',
    label: 'OpenAPI: Generate C# Client',
    description: 'Generate C# API client with Kiota',
    category: 'API & Spec',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        // Ensure tools are restored
        await ensureToolsRestored(ctx);

        const specPath = join(ctx.config.rootPath, 'src/Presentation.Web.Server/wwwroot/openapi.json');

        if (!existsSync(specPath)) {
          return {
            success: false,
            exitCode: 1,
            error: 'OpenAPI spec not found. Build the project first.',
            duration: Date.now() - startTime
          };
        }

        const outputDir = join(ctx.config.rootPath, '.tmp', 'openapi', 'dotnet');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        const result = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'kiota',
          args: [
            'generate',
            '-d', specPath,
            '-l', 'CSharp',
            '-o', outputDir,
            '-c', 'ApiClient',
            '-n', 'OpenApi.Client',
            '--log-level', 'Error'
          ],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`C# client generated in: ${outputDir}`);
        }

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
    key: 'openapi-client-typescript',
    label: 'OpenAPI: Generate TypeScript Client',
    description: 'Generate TypeScript API client with Kiota',
    category: 'API & Spec',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        // Ensure tools are restored
        await ensureToolsRestored(ctx);

        const specPath = join(ctx.config.rootPath, 'src/Presentation.Web.Server/wwwroot/openapi.json');

        if (!existsSync(specPath)) {
          return {
            success: false,
            exitCode: 1,
            error: 'OpenAPI spec not found. Build the project first.',
            duration: Date.now() - startTime
          };
        }

        const outputDir = join(ctx.config.rootPath, '.tmp', 'openapi', 'typescript');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        const result = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'kiota',
          args: [
            'generate',
            '-d', specPath,
            '-l', 'TypeScript',
            '-o', outputDir,
            '-c', 'ApiClient',
            '--log-level', 'Error'
          ],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`TypeScript client generated in: ${outputDir}`);
        }

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
    key: 'openapi-http-requests',
    label: 'OpenAPI: Generate HTTP Requests',
    description: 'Generate .http request files',
    category: 'API & Spec',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const specPath = join(ctx.config.rootPath, 'src/Presentation.Web.Server/wwwroot/openapi.json');

        if (!existsSync(specPath)) {
          return {
            success: false,
            exitCode: 1,
            error: 'OpenAPI spec not found. Build the project first.',
            duration: Date.now() - startTime
          };
        }

        const outputDir = join(ctx.config.rootPath, '.tmp', 'openapi', 'http');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        const result = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'httpgenerator',
          args: [
            specPath,
            '--base-url', 'https://localhost:5001',
            '--output', outputDir,
            '--authorization-header', 'Bearer TOKEN',
            '--output-type', 'OneFilePerTag',
            '--skip-validation'
          ],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`HTTP request files generated in: ${outputDir}`);
        }

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

  // Entity Framework Tasks
  {
    key: 'ef-info',
    label: 'EF: Show DbContext Info',
    description: 'Display DbContext information',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        // Select module and DbContext
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: DbContext Info',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.info({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
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
    key: 'ef-list',
    label: 'EF: List Migrations',
    description: 'List all migrations for a DbContext',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: List Migrations',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.listMigrations({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
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
    key: 'ef-add',
    label: 'EF: Add Migration',
    description: 'Create a new migration',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Add Migration',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const migrationName = await showTextInputDialog({
          title: 'Migration Name',
          message: 'Enter migration name:',
          validate: validators.migrationName,
        });

        if (!migrationName) {
          return {
            success: false,
            exitCode: 1,
            error: 'Migration name is required',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.addMigration({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
          migrationName,
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
    key: 'ef-remove',
    label: 'EF: Remove Last Migration',
    description: 'Remove the most recent migration (not applied)',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Remove Migration',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.removeMigration({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
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
    key: 'ef-removeall',
    label: 'EF: Remove All Migrations',
    description: 'Delete all migration files (filesystem only)',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Remove All Migrations',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const confirmed = await showConfirmDialog({
          title: 'Confirm: Remove All Migrations',
          message: `Delete all migration files for ${moduleResult.moduleName}/${dbContextName}?`,
          initial: false,
        });

        if (!confirmed) {
          return {
            success: false,
            exitCode: 1,
            error: 'Operation cancelled by user',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.removeAllMigrations({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (ctx.onOutput && result.success) {
          ctx.onOutput(`Successfully removed ${result.removedCount} migration files`);
        }

        return {
          success: result.success,
          exitCode: result.success ? 0 : 1,
          output: `Removed ${result.removedCount} files`,
          error: result.error,
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
    key: 'ef-apply',
    label: 'EF: Apply Migrations',
    description: 'Update database with pending migrations',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Apply Migrations',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.applyMigrations({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
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
    key: 'ef-status',
    label: 'EF: Migration Status',
    description: 'Show applied vs filesystem migrations',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Migration Status',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.showStatus({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput('\n=== Filesystem Migrations ===');
          result.filesystem.forEach(m => ctx.onOutput(`  ${m}`));
          ctx.onOutput('\n=== Applied Migrations ===');
          result.applied.forEach(m => ctx.onOutput(`  ${m}`));
          ctx.onOutput('\n=== Pending Migrations ===');
          if (result.pending.length > 0) {
            result.pending.forEach(m => ctx.onOutput(`  ${m}`));
          } else {
            ctx.onOutput('  (none)');
          }
        }

        return {
          success: result.success,
          exitCode: result.success ? 0 : 1,
          output: `Filesystem: ${result.filesystem.length}, Applied: ${result.applied.length}, Pending: ${result.pending.length}`,
          error: result.error,
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
    key: 'ef-recreate',
    label: 'EF: Recreate Database',
    description: 'Drop and recreate database with migrations',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Recreate Database',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const confirmed = await showConfirmDialog({
          title: 'Confirm: Recreate Database',
          message: `Drop and recreate database for ${moduleResult.moduleName}/${dbContextName}? ALL DATA WILL BE LOST!`,
          initial: false,
        });

        if (!confirmed) {
          return {
            success: false,
            exitCode: 1,
            error: 'Operation cancelled by user',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.recreateDatabase({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
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
    key: 'ef-undo',
    label: 'EF: Undo Last Migration',
    description: 'Revert database to previous migration',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Undo Migration',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.undoMigration({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
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
    key: 'ef-reset',
    label: 'EF: Reset Migrations',
    description: 'Squash all migrations into Initial baseline',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Reset Migrations',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        const confirmed = await showConfirmDialog({
          title: 'Confirm: Reset Migrations',
          message: `Delete all migrations and create new Initial baseline for ${moduleResult.moduleName}/${dbContextName}?`,
          initial: false,
        });

        if (!confirmed) {
          return {
            success: false,
            exitCode: 1,
            error: 'Operation cancelled by user',
            duration: Date.now() - startTime
          };
        }

        const result = await efCli.resetMigrations({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
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
    key: 'ef-script',
    label: 'EF: Generate SQL Script',
    description: 'Export idempotent SQL migration script',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Generate SQL Script',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        // Create output directory
        const outputDir = join(ctx.config.rootPath, '.tmp', 'db');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').split('T')[0];
        const outputPath = join(outputDir, `efscript_${moduleResult.moduleName}_${timestamp}.sql`);

        const result = await efCli.generateScript({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
          outputPath,
          idempotent: true,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`SQL script written to: ${outputPath}`);
        }

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
    key: 'ef-bundle',
    label: 'EF: Generate Migration Bundle',
    description: 'Create self-contained migration executable',
    category: 'EF & Persistence',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const moduleResult = await showModuleDialog({
          projectRoot: ctx.config.rootPath,
          title: 'EF: Generate Bundle',
        });

        if (!moduleResult || moduleResult.isAll) {
          return {
            success: false,
            exitCode: 1,
            error: 'Module selection cancelled or invalid',
            duration: Date.now() - startTime
          };
        }

        const dbContextName = await showDbContextDialog({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
        });

        if (!dbContextName) {
          return {
            success: false,
            exitCode: 1,
            error: 'DbContext selection cancelled',
            duration: Date.now() - startTime
          };
        }

        // Create output directory
        const outputDir = join(ctx.config.rootPath, '.tmp', 'db');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').split('T')[0];
        const outputPath = join(outputDir, `efbundle_${moduleResult.moduleName}_${timestamp}.exe`);

        const result = await efCli.generateBundle({
          projectRoot: ctx.config.rootPath,
          moduleName: moduleResult.moduleName,
          dbContextName,
          outputPath,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`Migration bundle written to: ${outputPath}`);
        }

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

  {
    key: 'clean',
    label: 'Clean Workspace',
    description: 'Remove bin/obj/node_modules directories',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const { readdirSync, rmSync, statSync } = await import('fs');
        const { join } = await import('path');

        const dirsToRemove = ['bin', 'obj', 'node_modules', '.tmp', 'Debug', 'Release'];
        let removedCount = 0;

        function cleanDirectory(dir: string) {
          try {
            const entries = readdirSync(dir, { withFileTypes: true });

            for (const entry of entries) {
              const fullPath = join(dir, entry.name);

              if (entry.isDirectory()) {
                if (dirsToRemove.includes(entry.name)) {
                  if (ctx.onOutput) {
                    ctx.onOutput(`Removing: ${fullPath}`);
                  }
                  rmSync(fullPath, { recursive: true, force: true });
                  removedCount++;
                } else if (entry.name !== '.git' && entry.name !== 'node_modules') {
                  cleanDirectory(fullPath);
                }
              }
            }
          } catch (error) {
            // Ignore errors for inaccessible directories
          }
        }

        cleanDirectory(ctx.config.rootPath);

        if (ctx.onOutput) {
          ctx.onOutput(`Cleaned ${removedCount} directories`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Removed ${removedCount} directories`,
          error: '',
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
    key: 'repl',
    label: 'C# REPL',
    description: 'Launch interactive C# REPL',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        // Tool restore first
        await dotnetCli.toolRestore({
          cwd: ctx.config.rootPath,
          onOutput: ctx.onOutput
        });

        const result = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'csharprepl',
          args: [],
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
    key: 'kill-dotnet',
    label: 'Kill .NET Process',
    description: 'Terminate a dotnet process',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const { getDotnetProcesses, showProcessSelectDialog } = await import('../ui/dialogs/ProcessSelectDialog.js');

        const processes = await getDotnetProcesses();

        if (processes.length === 0) {
          return {
            success: false,
            exitCode: 1,
            error: 'No dotnet processes found',
            duration: Date.now() - startTime
          };
        }

        const pid = await showProcessSelectDialog({
          processes,
          title: 'Kill .NET Process',
          message: 'Select process to terminate:'
        });

        if (!pid) {
          return {
            success: false,
            exitCode: 1,
            error: 'Process selection cancelled',
            duration: Date.now() - startTime
          };
        }

        // Kill the process
        const isWindows = process.platform === 'win32';
        const killCommand = isWindows ? 'taskkill' : 'kill';
        const killArgs = isWindows ? ['/F', '/PID', pid.toString()] : ['-9', pid.toString()];

        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();

        const result = await executor.execute(killCommand, killArgs, {
          onStdout: ctx.onOutput,
          onStderr: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`Process ${pid} terminated`);
        }

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
    key: 'browser-devkit-docs',
    label: 'Browser: DevKit Docs',
    description: 'Open BridgingIT DevKit documentation',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const url = 'https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs';

        const { spawn } = await import('child_process');
        const isWindows = process.platform === 'win32';
        const isMac = process.platform === 'darwin';

        const command = isWindows ? 'start' : isMac ? 'open' : 'xdg-open';
        const args = isWindows ? ['', url] : [url];

        spawn(command, args, { shell: true, detached: true });

        if (ctx.onOutput) {
          ctx.onOutput(`Opening: ${url}`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Opened ${url}`,
          error: '',
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
    key: 'browser-seq',
    label: 'Browser: Seq Dashboard',
    description: 'Open Seq logging dashboard',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const url = 'http://localhost:15349';

        const { spawn } = await import('child_process');
        const isWindows = process.platform === 'win32';
        const isMac = process.platform === 'darwin';

        const command = isWindows ? 'start' : isMac ? 'open' : 'xdg-open';
        const args = isWindows ? ['', url] : [url];

        spawn(command, args, { shell: true, detached: true });

        if (ctx.onOutput) {
          ctx.onOutput(`Opening: ${url}`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Opened ${url}`,
          error: '',
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
    key: 'browser-adminneo',
    label: 'Browser: AdminNeo Dashboard',
    description: 'Open AdminNeo dashboard',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const url = 'http://localhost:18089';

        const { spawn } = await import('child_process');
        const isWindows = process.platform === 'win32';
        const isMac = process.platform === 'darwin';

        const command = isWindows ? 'start' : isMac ? 'open' : 'xdg-open';
        const args = isWindows ? ['', url] : [url];

        spawn(command, args, { shell: true, detached: true });

        if (ctx.onOutput) {
          ctx.onOutput(`Opening: ${url}`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Opened ${url}`,
          error: '',
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
    key: 'browser-server-kestrel',
    label: 'Browser: Server (Kestrel)',
    description: 'Open server running on Kestrel',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const url = 'https://localhost:5001/scalar';

        const { spawn } = await import('child_process');
        const isWindows = process.platform === 'win32';
        const isMac = process.platform === 'darwin';

        const command = isWindows ? 'start' : isMac ? 'open' : 'xdg-open';
        const args = isWindows ? ['', url] : [url];

        spawn(command, args, { shell: true, detached: true });

        if (ctx.onOutput) {
          ctx.onOutput(`Opening: ${url}`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Opened ${url}`,
          error: '',
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
    key: 'browser-server-docker',
    label: 'Browser: Server (Docker)',
    description: 'Open server running in Docker',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const url = 'http://localhost:8080/scalar';

        const { spawn } = await import('child_process');
        const isWindows = process.platform === 'win32';
        const isMac = process.platform === 'darwin';

        const command = isWindows ? 'start' : isMac ? 'open' : 'xdg-open';
        const args = isWindows ? ['', url] : [url];

        spawn(command, args, { shell: true, detached: true });

        if (ctx.onOutput) {
          ctx.onOutput(`Opening: ${url}`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Opened ${url}`,
          error: '',
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

  // Diagnostics Tasks
  {
    key: 'trace-flame',
    label: 'Diagnostics: Flame Trace',
    description: 'Collect CPU flame graph trace',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const { getDotnetProcesses, showProcessSelectDialog } = await import('../ui/dialogs/ProcessSelectDialog.js');

        const processes = await getDotnetProcesses();
        if (processes.length === 0) {
          return { success: false, exitCode: 1, error: 'No dotnet processes found', duration: Date.now() - startTime };
        }

        const pid = await showProcessSelectDialog({ processes, title: 'Flame Trace', message: 'Select process to trace:' });
        if (!pid) {
          return { success: false, exitCode: 1, error: 'Process selection cancelled', duration: Date.now() - startTime };
        }

        const outputDir = join(ctx.config.rootPath, '.tmp', 'diagnostics');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });

        const fileBase = `trace_${pid}_${new Date().toISOString().replace(/[:.]/g, '-')}`;
        const traceFile = join(outputDir, `${fileBase}.nettrace`);

        const result = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'dotnet-trace',
          args: ['collect', '--process-id', pid.toString(), '--providers', 'Microsoft-DotNETCore-SampleProfiler', '--duration', '00:00:10', '-o', traceFile],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`Trace saved to: ${traceFile}`);
        }

        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'trace-cpu',
    label: 'Diagnostics: CPU Trace',
    description: 'Collect CPU usage trace',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { getDotnetProcesses, showProcessSelectDialog } = await import('../ui/dialogs/ProcessSelectDialog.js');
        const processes = await getDotnetProcesses();
        if (processes.length === 0) return { success: false, exitCode: 1, error: 'No dotnet processes found', duration: Date.now() - startTime };
        const pid = await showProcessSelectDialog({ processes, title: 'CPU Trace' });
        if (!pid) return { success: false, exitCode: 1, error: 'Cancelled', duration: Date.now() - startTime };
        const outputDir = join(ctx.config.rootPath, '.tmp', 'diagnostics');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });
        const traceFile = join(outputDir, `cpu_${pid}_${Date.now()}.nettrace`);
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'dotnet-trace', args: ['collect', '--process-id', pid.toString(), '--profile', 'cpu-sampling', '--duration', '00:00:10', '-o', traceFile], onOutput: ctx.onOutput, onError: ctx.onError });
        if (result.success && ctx.onOutput) ctx.onOutput(`CPU trace: ${traceFile}`);
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'trace-gc',
    label: 'Diagnostics: GC Trace',
    description: 'Collect garbage collection trace',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { getDotnetProcesses, showProcessSelectDialog } = await import('../ui/dialogs/ProcessSelectDialog.js');
        const processes = await getDotnetProcesses();
        if (processes.length === 0) return { success: false, exitCode: 1, error: 'No dotnet processes found', duration: Date.now() - startTime };
        const pid = await showProcessSelectDialog({ processes, title: 'GC Trace' });
        if (!pid) return { success: false, exitCode: 1, error: 'Cancelled', duration: Date.now() - startTime };
        const outputDir = join(ctx.config.rootPath, '.tmp', 'diagnostics');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });
        const traceFile = join(outputDir, `gc_${pid}_${Date.now()}.nettrace`);
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'dotnet-trace', args: ['collect', '--process-id', pid.toString(), '--profile', 'gc-collect', '--duration', '00:00:10', '-o', traceFile], onOutput: ctx.onOutput, onError: ctx.onError });
        if (result.success && ctx.onOutput) ctx.onOutput(`GC trace: ${traceFile}`);
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'dump-heap',
    label: 'Diagnostics: Heap Dump',
    description: 'Create memory heap dump',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { getDotnetProcesses, showProcessSelectDialog } = await import('../ui/dialogs/ProcessSelectDialog.js');
        const processes = await getDotnetProcesses();
        if (processes.length === 0) return { success: false, exitCode: 1, error: 'No dotnet processes found', duration: Date.now() - startTime };
        const pid = await showProcessSelectDialog({ processes, title: 'Heap Dump' });
        if (!pid) return { success: false, exitCode: 1, error: 'Cancelled', duration: Date.now() - startTime };
        const outputDir = join(ctx.config.rootPath, '.tmp', 'diagnostics');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });
        const dumpFile = join(outputDir, `heap_${pid}_${Date.now()}.dmp`);
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'dotnet-dump', args: ['collect', '--process-id', pid.toString(), '--type', 'Heap', '-o', dumpFile], onOutput: ctx.onOutput, onError: ctx.onError });
        if (result.success && ctx.onOutput) ctx.onOutput(`Heap dump: ${dumpFile}`);
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'gc-stats',
    label: 'Diagnostics: GC Stats',
    description: 'Monitor GC statistics in real-time',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { getDotnetProcesses, showProcessSelectDialog } = await import('../ui/dialogs/ProcessSelectDialog.js');
        const processes = await getDotnetProcesses();
        if (processes.length === 0) return { success: false, exitCode: 1, error: 'No dotnet processes found', duration: Date.now() - startTime };
        const pid = await showProcessSelectDialog({ processes, title: 'GC Stats' });
        if (!pid) return { success: false, exitCode: 1, error: 'Cancelled', duration: Date.now() - startTime };
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'dotnet-counters', args: ['monitor', '--process-id', pid.toString(), '--counters', 'System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate]'], onOutput: ctx.onOutput, onError: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'aspnet-metrics',
    label: 'Diagnostics: ASP.NET Metrics',
    description: 'Monitor ASP.NET Core metrics',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { getDotnetProcesses, showProcessSelectDialog } = await import('../ui/dialogs/ProcessSelectDialog.js');
        const processes = await getDotnetProcesses();
        if (processes.length === 0) return { success: false, exitCode: 1, error: 'No dotnet processes found', duration: Date.now() - startTime };
        const pid = await showProcessSelectDialog({ processes, title: 'ASP.NET Metrics' });
        if (!pid) return { success: false, exitCode: 1, error: 'Cancelled', duration: Date.now() - startTime };
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'dotnet-counters', args: ['monitor', '--process-id', pid.toString(), '--counters', 'Microsoft.AspNetCore.Hosting'], onOutput: ctx.onOutput, onError: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'bench',
    label: 'Diagnostics: Run Benchmarks',
    description: 'Run BenchmarkDotNet benchmarks',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const benchProj = readdirSync(ctx.config.rootPath, { recursive: true }).find((f: any) => f.endsWith('Benchmarks.csproj'));
        if (!benchProj) return { success: false, exitCode: 1, error: 'No benchmark project found', duration: Date.now() - startTime };
        const result = await dotnetCli.build({ cwd: join(ctx.config.rootPath, benchProj, '..'), configuration: 'Release', onOutput: ctx.onOutput, onError: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  // Roslynator Tasks
  {
    key: 'roslynator-analyze',
    label: 'Roslynator: Analyze',
    description: 'Run Roslynator code analysis',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const args = ['analyze'];
        if (ctx.config.solutionFile) {
          args.push(ctx.config.solutionFile);
        }
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'roslynator', args, onOutput: ctx.onOutput, onError: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'roslynator-loc',
    label: 'Roslynator: Count Lines',
    description: 'Count physical lines of code',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const args = ['loc'];
        if (ctx.config.solutionFile) {
          args.push(ctx.config.solutionFile);
        }
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'roslynator', args, onOutput: ctx.onOutput, onError: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'roslynator-lloc',
    label: 'Roslynator: Count Logical Lines',
    description: 'Count logical lines of code',
    category: 'Testing & Quality',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const args = ['lloc'];
        if (ctx.config.solutionFile) {
          args.push(ctx.config.solutionFile);
        }
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'roslynator', args, onOutput: ctx.onOutput, onError: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  // Project Tasks
  {
    key: 'pack-projects',
    label: 'Pack Module Projects',
    description: 'Create NuGet packages for module projects',
    category: 'Publishing & Packaging',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const moduleResult = await showModuleDialog({ projectRoot: ctx.config.rootPath, title: 'Pack Projects' });
        if (!moduleResult || moduleResult.isAll) return { success: false, exitCode: 1, error: 'Module selection required', duration: Date.now() - startTime };
        const outputDir = join(ctx.config.rootPath, '.tmp', 'packages');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });
        const result = await dotnetCli.pack({ cwd: ctx.config.rootPath, configuration: 'Release', outputPath: outputDir, onOutput: ctx.onOutput, onError: ctx.onError });
        if (result.success && ctx.onOutput) ctx.onOutput(`Packages created in: ${outputDir}`);
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'project-build',
    label: 'Project: Build',
    description: 'Build specific project',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const projectPath = await showTextInputDialog({ title: 'Project Build', message: 'Enter project path:', validate: validators.notEmpty });
        if (!projectPath) return { success: false, exitCode: 1, error: 'Project path required', duration: Date.now() - startTime };
        const result = await dotnetCli.build({ cwd: join(ctx.config.rootPath, projectPath), onOutput: ctx.onOutput, onError: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'project-run',
    label: 'Project: Run',
    description: 'Run specific project',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const projectPath = await showTextInputDialog({ title: 'Project Run', message: 'Enter project path (.csproj):', validate: validators.notEmpty });
        if (!projectPath) return { success: false, exitCode: 1, error: 'Project path required', duration: Date.now() - startTime };
        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('dotnet', ['run', '--project', projectPath], { cwd: ctx.config.rootPath, onStdout: ctx.onOutput, onStderr: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'project-watch',
    label: 'Project: Watch',
    description: 'Run project with hot reload',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const projectPath = await showTextInputDialog({ title: 'Project Watch', message: 'Enter project path (.csproj):', validate: validators.notEmpty });
        if (!projectPath) return { success: false, exitCode: 1, error: 'Project path required', duration: Date.now() - startTime };
        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('dotnet', ['watch', 'run', '--project', projectPath, '--nologo'], { cwd: ctx.config.rootPath, onStdout: ctx.onOutput, onStderr: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'project-watch-fast',
    label: 'Project: Watch (Fast)',
    description: 'Run project with hot reload (no restore)',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const projectPath = await showTextInputDialog({ title: 'Project Watch Fast', message: 'Enter project path (.csproj):', validate: validators.notEmpty });
        if (!projectPath) return { success: false, exitCode: 1, error: 'Project path required', duration: Date.now() - startTime };
        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('dotnet', ['watch', 'run', '--project', projectPath, '--nologo', '--no-restore'], { cwd: ctx.config.rootPath, onStdout: ctx.onOutput, onStderr: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'project-publish',
    label: 'Project: Publish',
    description: 'Publish project as self-contained executable',
    category: 'Publishing & Packaging',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const projectPath = await showTextInputDialog({ title: 'Project Publish', message: 'Enter project path (.csproj):', initial: 'src/Presentation.Web.Server/Presentation.Web.Server.csproj', validate: validators.notEmpty });
        if (!projectPath) return { success: false, exitCode: 1, error: 'Project path required', duration: Date.now() - startTime };

        const { showRidDialog } = await import('../ui/dialogs/RidDialog.js');
        const rid = await showRidDialog({ title: 'Select Target Platform' });
        if (!rid) return { success: false, exitCode: 1, error: 'RID selection cancelled', duration: Date.now() - startTime };

        const outputDir = join(ctx.config.rootPath, '.tmp', 'publish');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });

        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('dotnet', ['publish', projectPath, '-r', rid, '--self-contained', 'true', '-o', outputDir], { cwd: ctx.config.rootPath, onStdout: ctx.onOutput, onStderr: ctx.onError });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`\nPublished to: ${outputDir}`);
        }

        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'project-publish-release',
    label: 'Project: Publish (Release)',
    description: 'Publish project in Release configuration',
    category: 'Publishing & Packaging',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const projectPath = await showTextInputDialog({ title: 'Project Publish Release', message: 'Enter project path (.csproj):', initial: 'src/Presentation.Web.Server/Presentation.Web.Server.csproj', validate: validators.notEmpty });
        if (!projectPath) return { success: false, exitCode: 1, error: 'Project path required', duration: Date.now() - startTime };

        const { showRidDialog } = await import('../ui/dialogs/RidDialog.js');
        const rid = await showRidDialog({ title: 'Select Target Platform' });
        if (!rid) return { success: false, exitCode: 1, error: 'RID selection cancelled', duration: Date.now() - startTime };

        const outputDir = join(ctx.config.rootPath, '.tmp', 'publish');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });

        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('dotnet', ['publish', projectPath, '-c', 'Release', '-r', rid, '--self-contained', 'true', '-o', outputDir], { cwd: ctx.config.rootPath, onStdout: ctx.onOutput, onStderr: ctx.onError });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`\nPublished to: ${outputDir}`);
        }

        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'project-publish-sc',
    label: 'Project: Publish (Single File)',
    description: 'Publish as single self-contained executable',
    category: 'Publishing & Packaging',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const projectPath = await showTextInputDialog({ title: 'Project Publish Single File', message: 'Enter project path (.csproj):', initial: 'src/Presentation.Web.Server/Presentation.Web.Server.csproj', validate: validators.notEmpty });
        if (!projectPath) return { success: false, exitCode: 1, error: 'Project path required', duration: Date.now() - startTime };

        const { showRidDialog } = await import('../ui/dialogs/RidDialog.js');
        const rid = await showRidDialog({ title: 'Select Target Platform' });
        if (!rid) return { success: false, exitCode: 1, error: 'RID selection cancelled', duration: Date.now() - startTime };

        const outputDir = join(ctx.config.rootPath, '.tmp', 'publish');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });

        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('dotnet', [
          'publish', projectPath,
          '-c', 'Release',
          '-r', rid,
          '--self-contained', 'true',
          '/p:PublishSingleFile=true',
          '/p:PublishTrimmed=false',
          '-o', outputDir
        ], { cwd: ctx.config.rootPath, onStdout: ctx.onOutput, onStderr: ctx.onError });

        if (result.success && ctx.onOutput) {
          ctx.onOutput(`\nSingle-file executable published to: ${outputDir}`);
        }

        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'show-minver',
    label: 'Show MinVer Version',
    description: 'Display semantic version from MinVer',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const result = await dotnetCli.toolRun({ cwd: ctx.config.rootPath, toolName: 'minver', args: [], onOutput: ctx.onOutput, onError: ctx.onError });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  // Documentation Tasks
  {
    key: 'digest-sources',
    label: 'Digest Sources',
    description: 'Combine source files into markdown documentation',
    category: 'Documentation',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const outputDir = join(ctx.config.rootPath, '.tmp', 'digest');
        if (!existsSync(outputDir)) {
          mkdirSync(outputDir, { recursive: true });
        }

        if (ctx.onOutput) {
          ctx.onOutput('Scanning for .csproj files...');
        }

        // Find all .csproj files
        const { readdirSync, readFileSync, writeFileSync } = await import('fs');
        const { join: pathJoin } = await import('path');

        function findProjects(dir: string, results: string[] = []): string[] {
          try {
            const entries = readdirSync(dir, { withFileTypes: true });
            for (const entry of entries) {
              const fullPath = pathJoin(dir, entry.name);
              if (entry.isDirectory() && !['bin', 'obj', 'node_modules', '.git'].includes(entry.name)) {
                findProjects(fullPath, results);
              } else if (entry.name.endsWith('.csproj')) {
                results.push(fullPath);
              }
            }
          } catch {}
          return results;
        }

        const projects = findProjects(ctx.config.rootPath);

        if (ctx.onOutput) {
          ctx.onOutput(`Found ${projects.length} projects to process`);
        }

        let totalFiles = 0;

        for (const projectPath of projects) {
          const projectDir = pathJoin(projectPath, '..');
          const projectName = projectPath.split(/[\\/]/).pop()?.replace('.csproj', '') || 'Unknown';

          if (ctx.onOutput) {
            ctx.onOutput(`Processing: ${projectName}`);
          }

          // Find all .cs files in project
          function findSourceFiles(dir: string, results: string[] = []): string[] {
            try {
              const entries = readdirSync(dir, { withFileTypes: true });
              for (const entry of entries) {
                const fullPath = pathJoin(dir, entry.name);
                if (entry.isDirectory() && !['bin', 'obj'].includes(entry.name)) {
                  findSourceFiles(fullPath, results);
                } else if (entry.name.endsWith('.cs') && !entry.name.includes('.g.') && !entry.name.includes('.Designer.')) {
                  results.push(fullPath);
                }
              }
            } catch {}
            return results;
          }

          const sourceFiles = findSourceFiles(projectDir);
          totalFiles += sourceFiles.length;

          // Combine into markdown
          let markdown = `# ${projectName}\n\n`;

          for (const sourceFile of sourceFiles) {
            const relativePath = sourceFile.replace(projectDir + '\\', '').replace(projectDir + '/', '');
            const content = readFileSync(sourceFile, 'utf8');

            // Strip license headers
            const cleanContent = content
              .replace(/\/\/ MIT-License[\s\S]*?found in the LICENSE file.*\n\n?/m, '')
              .trim();

            markdown += `## ${relativePath}\n\n\`\`\`csharp\n${cleanContent}\n\`\`\`\n\n`;
          }

          const outputFile = pathJoin(outputDir, `${projectName}.g.md`);
          writeFileSync(outputFile, markdown);
        }

        if (ctx.onOutput) {
          ctx.onOutput(`\nDigest complete: ${projects.length} projects, ${totalFiles} files`);
          ctx.onOutput(`Output directory: ${outputDir}`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Processed ${projects.length} projects, ${totalFiles} files`,
          error: '',
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
    key: 'remove-headers',
    label: 'Remove License Headers',
    description: 'Remove MIT license headers from C# files',
    category: 'Documentation',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const { readdirSync, readFileSync, writeFileSync } = await import('fs');
        const { join: pathJoin } = await import('path');

        function findCsFiles(dir: string, results: string[] = []): string[] {
          try {
            const entries = readdirSync(dir, { withFileTypes: true });
            for (const entry of entries) {
              const fullPath = pathJoin(dir, entry.name);
              if (entry.isDirectory() && !['bin', 'obj', 'node_modules', '.git'].includes(entry.name)) {
                findCsFiles(fullPath, results);
              } else if (entry.name.endsWith('.cs')) {
                results.push(fullPath);
              }
            }
          } catch {}
          return results;
        }

        const srcDir = pathJoin(ctx.config.rootPath, 'src');
        const testsDir = pathJoin(ctx.config.rootPath, 'tests');

        const files = [...findCsFiles(srcDir), ...findCsFiles(testsDir)];

        if (ctx.onOutput) {
          ctx.onOutput(`Found ${files.length} C# files to process`);
        }

        let modifiedCount = 0;

        for (const file of files) {
          const content = readFileSync(file, 'utf8');
          const lines = content.split('\n');

          // Check if file has MIT header
          if (lines.length > 5 && lines[0].includes('MIT-License') && lines[1].includes('Copyright BridgingIT')) {
            // Find where header ends (typically line 4-5)
            let headerEnd = 4;
            if (lines[headerEnd] && lines[headerEnd].trim() === '') {
              headerEnd++;
            }

            const newContent = lines.slice(headerEnd).join('\n').trimStart();
            writeFileSync(file, newContent, 'utf8');
            modifiedCount++;

            if (ctx.onOutput && modifiedCount % 10 === 0) {
              ctx.onOutput(`Processed ${modifiedCount} files...`);
            }
          }
        }

        if (ctx.onOutput) {
          ctx.onOutput(`\nRemoved headers from ${modifiedCount} files`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Modified ${modifiedCount} of ${files.length} files`,
          error: '',
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
    key: 'docs-update',
    label: 'Update DevKit Docs',
    description: 'Download latest DevKit documentation from GitHub',
    category: 'Documentation',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();

      try {
        const targetDir = join(ctx.config.rootPath, '.bdk', 'docs');
        if (!existsSync(targetDir)) {
          mkdirSync(targetDir, { recursive: true });
        }

        if (ctx.onOutput) {
          ctx.onOutput('Downloading DevKit docs from GitHub...');
        }

        const apiBase = 'https://api.github.com/repos/BridgingIT-GmbH/bITdevKit/contents/docs';
        const branchRef = 'main';

        // Fetch directory listing
        const listResponse = await fetch(`${apiBase}?ref=${branchRef}`, {
          headers: {
            'User-Agent': 'bdk-tui',
            'Accept': 'application/vnd.github.v3+json'
          }
        });

        if (!listResponse.ok) {
          return {
            success: false,
            exitCode: 1,
            error: `Failed to fetch docs: ${listResponse.statusText}`,
            duration: Date.now() - startTime
          };
        }

        const items = await listResponse.json();
        let downloadedCount = 0;

        for (const item of items) {
          if (item.type === 'file' && item.name.endsWith('.md')) {
            const { writeFileSync } = await import('fs');

            const contentResponse = await fetch(item.download_url);
            if (contentResponse.ok) {
              const content = await contentResponse.text();
              const localPath = join(targetDir, item.name);
              writeFileSync(localPath, content, 'utf8');
              downloadedCount++;

              if (ctx.onOutput) {
                ctx.onOutput(`Downloaded: ${item.name}`);
              }
            }
          }
        }

        if (ctx.onOutput) {
          ctx.onOutput(`\nDownloaded ${downloadedCount} markdown files to ${targetDir}`);
        }

        return {
          success: true,
          exitCode: 0,
          output: `Downloaded ${downloadedCount} files`,
          error: '',
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

  // Server Tasks
  {
    key: 'server-build',
    label: 'Server: Build',
    description: 'Build the web server',
    category: 'Build & Maintenance',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const serverProject = join(ctx.config.rootPath, 'src/Presentation.Web.Server/Presentation.Web.Server.csproj');
        const result = await dotnetCli.build({
          cwd: ctx.config.rootPath,
          configuration: 'Release',
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  // Additional Utilities
  {
    key: 'misc-digest',
    label: 'Utility: Generate Source Digest',
    description: 'Generate consolidated markdown documentation',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('pwsh', [
          '-NoProfile',
          '-File',
          join(ctx.config.rootPath, '.bdk/tasks-misc.ps1'),
          'digest'
        ], {
          cwd: ctx.config.rootPath,
          onStdout: ctx.onOutput,
          onStderr: ctx.onError
        });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'misc-remove-headers',
    label: 'Utility: Remove MIT Headers',
    description: 'Strip MIT license headers from C# files',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('pwsh', [
          '-NoProfile',
          '-File',
          join(ctx.config.rootPath, '.bdk/tasks-misc.ps1'),
          'remove-headers'
        ], {
          cwd: ctx.config.rootPath,
          onStdout: ctx.onOutput,
          onStderr: ctx.onError
        });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'misc-show-minver',
    label: 'Utility: Show MinVer Version',
    description: 'Display semantic version computed by MinVer',
    category: 'Utilities',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        await ensureToolsRestored(ctx);
        const result = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'minver',
          args: ['-v', 'd', '-p', 'preview.0'],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  // Additional Diagnostics
  {
    key: 'bench-select',
    label: 'Diagnostics: Run Benchmark (Select)',
    description: 'Run benchmarks on selected project',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { readdirSync } = await import('fs');
        const benchProjects = readdirSync(ctx.config.rootPath, { recursive: true })
          .filter((f: any) => typeof f === 'string' && f.endsWith('Benchmarks.csproj'));
        
        if (!benchProjects || benchProjects.length === 0) {
          return { success: false, exitCode: 1, error: 'No benchmark project found', duration: Date.now() - startTime };
        }

        // If multiple projects, show selection
        let selectedProject = benchProjects[0];
        if (benchProjects.length > 1) {
          // For simplicity, use first project (could add dialog later)
          selectedProject = benchProjects[0];
        }

        const result = await dotnetCli.run({
          cwd: ctx.config.rootPath,
          projectPath: selectedProject,
          configuration: 'Release',
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'diag-quick',
    label: 'Diagnostics: Quick Diagnostics',
    description: 'Run CPU trace, GC trace, and ASP.NET metrics',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { getDotnetProcesses, showProcessSelectDialog } = await import('../ui/dialogs/ProcessSelectDialog.js');
        const processes = await getDotnetProcesses();
        if (processes.length === 0) return { success: false, exitCode: 1, error: 'No dotnet processes found', duration: Date.now() - startTime };
        
        const pid = await showProcessSelectDialog({ processes, title: 'Quick Diagnostics' });
        if (!pid) return { success: false, exitCode: 1, error: 'Cancelled', duration: Date.now() - startTime };

        const outputDir = join(ctx.config.rootPath, '.tmp', 'diagnostics');
        if (!existsSync(outputDir)) mkdirSync(outputDir, { recursive: true });

        const fileBase = `quick_${pid}_${Date.now()}`;
        
        // CPU trace (5s)
        if (ctx.onOutput) ctx.onOutput('Running CPU trace (5s)...');
        const cpuFile = join(outputDir, `${fileBase}_cpu.nettrace`);
        const cpuResult = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'dotnet-trace',
          args: ['collect', '--process-id', pid.toString(), '--providers', 'Microsoft-DotNETCore-SampleProfiler:1', '--duration', '00:00:05', '-o', cpuFile],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        // GC trace (5s)
        if (ctx.onOutput) ctx.onOutput('Running GC trace (5s)...');
        const gcFile = join(outputDir, `${fileBase}_gc.nettrace`);
        const gcResult = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'dotnet-trace',
          args: ['collect', '--process-id', pid.toString(), '--providers', 'Microsoft-DotNETCore-SampleProfiler:1,System.Runtime:4', '--duration', '00:00:05', '-o', gcFile],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        // ASP.NET metrics (6s)
        if (ctx.onOutput) ctx.onOutput('Running ASP.NET metrics (6s)...');
        const metricsResult = await dotnetCli.toolRun({
          cwd: ctx.config.rootPath,
          toolName: 'dotnet-counters',
          args: ['monitor', '--process-id', pid.toString(), '--counters', 'Microsoft.AspNetCore.Hosting', '--refresh-interval', '1', '--duration', '6'],
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        const allSuccess = cpuResult.success && gcResult.success && metricsResult.success;
        if (ctx.onOutput) {
          ctx.onOutput(`Quick diagnostics complete. ${allSuccess ? 'All' : 'Some'} operations succeeded.`);
        }

        return { success: allSuccess, exitCode: allSuccess ? 0 : 1, output: 'Quick diagnostics complete', error: '', duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  {
    key: 'speedscope-view',
    label: 'Diagnostics: View Speedscope Profile',
    description: 'Open Speedscope profile viewer',
    category: 'Performance & Diagnostics',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { readdirSync, existsSync } = await import('fs');
        const { spawn } = await import('child_process');
        
        const diagDir = join(ctx.config.rootPath, '.tmp', 'diagnostics');
        if (!existsSync(diagDir)) {
          return { success: false, exitCode: 1, error: 'Diagnostics directory not found', duration: Date.now() - startTime };
        }

        const profiles = readdirSync(diagDir, { recursive: true })
          .filter((f: any) => typeof f === 'string' && f.endsWith('.speedscope.json'));

        if (!profiles || profiles.length === 0) {
          return { success: false, exitCode: 1, error: 'No speedscope profiles found', duration: Date.now() - startTime };
        }

        // Open speedscope.app and the folder
        if (ctx.onOutput) ctx.onOutput('Opening speedscope.app...');
        
        const isWindows = process.platform === 'win32';
        const isMac = process.platform === 'darwin';
        const command = isWindows ? 'start' : isMac ? 'open' : 'xdg-open';
        
        spawn(command, ['https://www.speedscope.app'], { shell: true, detached: true });
        spawn(command, [diagDir], { shell: true, detached: true });

        return { success: true, exitCode: 0, output: 'Opened speedscope.app', error: '', duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  // Additional Docker
  {
    key: 'compose-up-pull',
    label: 'Docker: Compose Up (with Pull)',
    description: 'Pull images and start services with Docker Compose',
    category: 'Docker & Containers',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const composeFile = ctx.config.dockerComposePath || 'docker-compose.yml';
        
        // First pull
        if (ctx.onOutput) ctx.onOutput('Pulling images...');
        const pullResult = await dockerCli.composePull({
          composeFile,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        // Then up
        if (ctx.onOutput) ctx.onOutput('Starting services...');
        const result = await dockerCli.composeUp({
          composeFile,
          detached: true,
          onOutput: ctx.onOutput,
          onError: ctx.onError
        });

        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
      }
    }
  },

  // Documentation
  {
    key: 'doc-update-devkit-docs',
    label: 'Documentation: Update DevKit Docs',
    description: 'Download latest DevKit docs from GitHub',
    category: 'Documentation',
    execute: async (ctx: TaskContext) => {
      const startTime = Date.now();
      try {
        const { CrossPlatformExecutor } = await import('../core/executor.js');
        const executor = new CrossPlatformExecutor();
        const result = await executor.execute('pwsh', [
          '-NoProfile',
          '-File',
          join(ctx.config.rootPath, '.bdk/tasks-misc.ps1'),
          'docs-update'
        ], {
          cwd: ctx.config.rootPath,
          onStdout: ctx.onOutput,
          onStderr: ctx.onError
        });
        return { success: result.success, exitCode: result.exitCode, output: result.stdout, error: result.stderr, duration: Date.now() - startTime };
      } catch (error) {
        return { success: false, exitCode: 1, error: String(error), duration: Date.now() - startTime };
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
