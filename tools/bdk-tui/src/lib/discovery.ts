// Discovery utilities for modules, DbContexts, and other project elements
import { readdirSync, existsSync, readFileSync } from 'fs';
import { join } from 'path';

export interface ModuleInfo {
  name: string;
  path: string;
  hasInfrastructure: boolean;
  hasDbContext: boolean;
}

export interface DbContextInfo {
  name: string;
  module: string;
  filePath: string;
}

/**
 * Discover all modules in the project
 * Modules are located in src/Modules/[ModuleName]
 */
export function discoverModules(projectRoot: string): ModuleInfo[] {
  const modulesPath = join(projectRoot, 'src', 'Modules');
  
  if (!existsSync(modulesPath)) {
    return [];
  }
  
  const modules: ModuleInfo[] = [];
  const entries = readdirSync(modulesPath, { withFileTypes: true });
  
  for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    
    const moduleName = entry.name;
    const modulePath = join(modulesPath, moduleName);
    const infraPath = join(modulePath, `${moduleName}.Infrastructure`);
    
    const hasInfrastructure = existsSync(infraPath);
    const hasDbContext = hasInfrastructure && hasDbContextFiles(infraPath);
    
    modules.push({
      name: moduleName,
      path: modulePath,
      hasInfrastructure,
      hasDbContext,
    });
  }
  
  return modules.sort((a, b) => a.name.localeCompare(b.name));
}

/**
 * Discover all DbContext classes in a module
 */
export function discoverDbContexts(projectRoot: string, moduleName?: string): DbContextInfo[] {
  const contexts: DbContextInfo[] = [];
  const modulesPath = join(projectRoot, 'src', 'Modules');
  
  if (!existsSync(modulesPath)) {
    return contexts;
  }
  
  const modules = moduleName 
    ? [moduleName] 
    : readdirSync(modulesPath, { withFileTypes: true })
        .filter(e => e.isDirectory())
        .map(e => e.name);
  
  for (const module of modules) {
    const infraPath = join(modulesPath, module, `${module}.Infrastructure`);
    
    if (!existsSync(infraPath)) continue;
    
    const dbContexts = findDbContextFiles(infraPath, module);
    contexts.push(...dbContexts);
  }
  
  return contexts.sort((a, b) => a.name.localeCompare(b.name));
}

/**
 * Check if infrastructure directory has DbContext files
 */
function hasDbContextFiles(infraPath: string): boolean {
  try {
    return findDbContextFilesRecursive(infraPath).length > 0;
  } catch {
    return false;
  }
}

/**
 * Find all DbContext files in infrastructure directory
 */
function findDbContextFiles(infraPath: string, moduleName: string): DbContextInfo[] {
  const contextFiles = findDbContextFilesRecursive(infraPath);
  
  return contextFiles.map(filePath => {
    const fileName = filePath.split(/[\\/]/).pop() || '';
    const className = fileName.replace('.cs', '');
    
    return {
      name: className,
      module: moduleName,
      filePath,
    };
  });
}

/**
 * Recursively find all files that look like DbContext classes
 */
function findDbContextFilesRecursive(dir: string): string[] {
  const results: string[] = [];
  
  try {
    const entries = readdirSync(dir, { withFileTypes: true });
    
    for (const entry of entries) {
      const fullPath = join(dir, entry.name);
      
      if (entry.isDirectory()) {
        results.push(...findDbContextFilesRecursive(fullPath));
      } else if (entry.name.endsWith('DbContext.cs')) {
        // Verify it actually contains DbContext class (or inherits from one)
        try {
          const content = readFileSync(fullPath, 'utf8');
          // Check for direct DbContext inheritance or base class that ends with DbContext
          if (content.includes(': DbContext') || 
              content.includes(':DbContext') ||
              content.match(/:\s*\w*DbContext\w*/)) {
            results.push(fullPath);
          }
        } catch {
          // Ignore read errors
        }
      }
    }
  } catch {
    // Ignore directory read errors
  }
  
  return results;
}

/**
 * Get module name from environment variable or auto-detect
 */
export function getModuleNameFromEnv(env: Record<string, string | undefined>): string | undefined {
  return env.MODULE_NAME || env.MODULE;
}

/**
 * Resolve module name - auto-select if single module, otherwise return undefined
 */
export function resolveModuleName(projectRoot: string, envModuleName?: string): string | undefined {
  if (envModuleName) {
    return envModuleName;
  }
  
  const modules = discoverModules(projectRoot);
  
  if (modules.length === 1) {
    return modules[0].name;
  }
  
  return undefined;
}

/**
 * Resolve DbContext name - auto-select if single context in module, otherwise return undefined
 */
export function resolveDbContextName(projectRoot: string, moduleName: string): string | undefined {
  const contexts = discoverDbContexts(projectRoot, moduleName);
  
  if (contexts.length === 1) {
    return contexts[0].name;
  }
  
  return undefined;
}
