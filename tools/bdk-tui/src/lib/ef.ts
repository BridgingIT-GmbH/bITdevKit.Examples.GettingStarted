// Entity Framework Core CLI wrapper
import { CrossPlatformExecutor } from '../core/executor.js';
import { join } from 'path';
import { existsSync, readdirSync, rmSync } from 'fs';

export interface EfOptions {
  projectRoot: string;
  moduleName: string;
  dbContextName: string;
  startupProject?: string;
  onOutput?: (line: string) => void;
  onError?: (line: string) => void;
}

export interface EfMigrationOptions extends EfOptions {
  migrationName: string;
}

export interface EfScriptOptions extends EfOptions {
  outputPath: string;
  idempotent?: boolean;
}

export interface EfBundleOptions extends EfOptions {
  outputPath: string;
}

export class EntityFrameworkCli {
  private executor = new CrossPlatformExecutor();
  
  /**
   * Resolve infrastructure project path for a module
   */
  private resolveInfrastructureProject(projectRoot: string, moduleName: string): string {
    const infraFolder = join(projectRoot, 'src', 'Modules', moduleName, `${moduleName}.Infrastructure`);
    const csproj = join(infraFolder, `${moduleName}.Infrastructure.csproj`);
    
    if (!existsSync(csproj)) {
      throw new Error(`Infrastructure project not found: ${csproj}`);
    }
    
    return csproj;
  }
  
  /**
   * Resolve startup project path
   */
  private resolveStartupProject(projectRoot: string, startupProject?: string): string {
    if (startupProject) return startupProject;
    
    // Default to Presentation.Web.Server
    const defaultStartup = join(projectRoot, 'src', 'Presentation.Web.Server', 'Presentation.Web.Server.csproj');
    
    if (!existsSync(defaultStartup)) {
      throw new Error(`Startup project not found: ${defaultStartup}`);
    }
    
    return defaultStartup;
  }
  
  /**
   * Build common EF command arguments
   */
  private buildEfArgs(options: EfOptions, command: string[]): string[] {
    const infraProject = this.resolveInfrastructureProject(options.projectRoot, options.moduleName);
    const startupProject = this.resolveStartupProject(options.projectRoot, options.startupProject);
    
    return [
      'ef',
      ...command,
      '--project', infraProject,
      '--startup-project', startupProject,
      '--context', options.dbContextName,
      '--no-build',
      '--verbose'
    ];
  }
  
  /**
   * Show DbContext information
   */
  async info(options: EfOptions) {
    const args = this.buildEfArgs(options, ['dbcontext', 'info']);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * List all migrations
   */
  async listMigrations(options: EfOptions) {
    const args = this.buildEfArgs(options, ['migrations', 'list']);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Add a new migration
   */
  async addMigration(options: EfMigrationOptions) {
    const args = this.buildEfArgs(options, [
      'migrations',
      'add',
      options.migrationName,
      '--output-dir', 'EntityFramework/Migrations'
    ]);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Remove the last migration
   */
  async removeMigration(options: EfOptions) {
    const args = this.buildEfArgs(options, ['migrations', 'remove']);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Remove all migration files (filesystem only)
   */
  async removeAllMigrations(options: EfOptions): Promise<{ success: boolean; removedCount: number; error?: string }> {
    try {
      const infraProject = this.resolveInfrastructureProject(options.projectRoot, options.moduleName);
      const infraFolder = join(options.projectRoot, 'src', 'Modules', options.moduleName, `${options.moduleName}.Infrastructure`);
      const migDir = join(infraFolder, 'EntityFramework', 'Migrations');
      
      if (!existsSync(migDir)) {
        return { success: true, removedCount: 0, error: 'Migrations directory does not exist' };
      }
      
      const files = readdirSync(migDir).filter(f => f.endsWith('.cs') || f.endsWith('.Designer.cs'));
      const count = files.length;
      
      // Remove all migration files
      for (const file of files) {
        rmSync(join(migDir, file), { force: true });
      }
      
      if (options.onOutput) {
        options.onOutput(`Removed ${count} migration files from ${migDir}`);
      }
      
      return { success: true, removedCount: count };
    } catch (error) {
      return { success: false, removedCount: 0, error: String(error) };
    }
  }
  
  /**
   * Apply migrations (update database)
   */
  async applyMigrations(options: EfOptions) {
    const args = this.buildEfArgs(options, ['database', 'update']);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Update database to specific migration
   */
  async updateToMigration(options: EfMigrationOptions) {
    const args = this.buildEfArgs(options, ['database', 'update', options.migrationName]);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Drop database
   */
  async dropDatabase(options: EfOptions) {
    const args = this.buildEfArgs(options, ['database', 'drop', '--force']);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Recreate database (drop + apply migrations)
   */
  async recreateDatabase(options: EfOptions) {
    // Drop first
    const dropResult = await this.dropDatabase(options);
    if (!dropResult.success) {
      return dropResult;
    }
    
    // Then apply
    return this.applyMigrations(options);
  }
  
  /**
   * Undo last migration (revert to previous)
   */
  async undoMigration(options: EfOptions) {
    // Get list of migrations
    const listArgs = this.buildEfArgs(options, ['migrations', 'list']);
    const listResult = await this.executor.execute('dotnet', listArgs, {
      cwd: options.projectRoot
    });
    
    if (!listResult.success) {
      return listResult;
    }
    
    // Parse migrations (format: "20231201_120000_MigrationName")
    const migrations = listResult.stdout
      .split('\n')
      .map(line => line.trim())
      .filter(line => /^[0-9]{14}_/.test(line));
    
    if (migrations.length < 2) {
      return {
        success: false,
        exitCode: 1,
        stdout: '',
        stderr: 'Not enough migrations to undo (need at least 2)',
      };
    }
    
    // Get second-to-last migration
    const targetMigration = migrations[migrations.length - 2].split(' ')[0];
    
    if (options.onOutput) {
      options.onOutput(`Reverting to migration: ${targetMigration}`);
    }
    
    // Update to that migration
    const args = this.buildEfArgs(options, ['database', 'update', targetMigration]);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Show migration status (applied vs filesystem)
   */
  async showStatus(options: EfOptions): Promise<{
    success: boolean;
    filesystem: string[];
    applied: string[];
    pending: string[];
    error?: string;
  }> {
    try {
      // Get filesystem migrations
      const infraFolder = join(options.projectRoot, 'src', 'Modules', options.moduleName, `${options.moduleName}.Infrastructure`);
      const migDir = join(infraFolder, 'EntityFramework', 'Migrations');
      
      let fsMigrations: string[] = [];
      if (existsSync(migDir)) {
        fsMigrations = readdirSync(migDir)
          .filter(f => f.endsWith('.cs') && /^[0-9]{14}_/.test(f))
          .map(f => f.replace('.cs', ''))
          .sort();
      }
      
      // Get applied migrations from EF
      const listArgs = this.buildEfArgs(options, ['migrations', 'list']);
      const listResult = await this.executor.execute('dotnet', listArgs, {
        cwd: options.projectRoot
      });
      
      if (!listResult.success) {
        return {
          success: false,
          filesystem: fsMigrations,
          applied: [],
          pending: [],
          error: listResult.stderr
        };
      }
      
      const appliedMigrations = listResult.stdout
        .split('\n')
        .map(line => line.trim())
        .filter(line => /^[0-9]{14}_/.test(line))
        .map(line => line.split(' ')[0]);
      
      const pending = fsMigrations.filter(m => !appliedMigrations.includes(m));
      
      return {
        success: true,
        filesystem: fsMigrations,
        applied: appliedMigrations,
        pending
      };
    } catch (error) {
      return {
        success: false,
        filesystem: [],
        applied: [],
        pending: [],
        error: String(error)
      };
    }
  }
  
  /**
   * Reset migrations (remove all + create Initial baseline)
   */
  async resetMigrations(options: EfOptions) {
    // Remove all migrations
    const removeResult = await this.removeAllMigrations(options);
    if (!removeResult.success) {
      return {
        success: false,
        exitCode: 1,
        stdout: '',
        stderr: removeResult.error || 'Failed to remove migrations'
      };
    }
    
    if (options.onOutput) {
      options.onOutput('Creating baseline migration (Initial)...');
    }
    
    // Add Initial migration
    const args = this.buildEfArgs(options, [
      'migrations',
      'add',
      'Initial',
      '--output-dir', 'EntityFramework/Migrations'
    ]);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Generate SQL script
   */
  async generateScript(options: EfScriptOptions) {
    const args = this.buildEfArgs(options, [
      'migrations',
      'script',
      '--output', options.outputPath
    ]);
    
    if (options.idempotent) {
      args.push('--idempotent');
    }
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Generate migration bundle
   */
  async generateBundle(options: EfBundleOptions) {
    const args = this.buildEfArgs(options, [
      'migrations',
      'bundle',
      '--output', options.outputPath
    ]);
    
    return this.executor.execute('dotnet', args, {
      cwd: options.projectRoot,
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
}
