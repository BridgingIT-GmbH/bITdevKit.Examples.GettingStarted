// Configuration loader - reads config/bdk.env at runtime

import { readFileSync, existsSync, readdirSync, writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { homedir } from 'os';
import type { BdkConfig } from '../types/config';

function findConfigPath(): string {
  // Get directory where binary/script is located
  const scriptDir = dirname(import.meta.path);
  
  const searchPaths = [
    // 1. Next to binary (production)
    join(scriptDir, '..', 'config', 'bdk.env'),
    
    // 2. Development mode (running from src)
    join(scriptDir, '..', '..', 'config', 'bdk.env'),
    
    // 3. Current working directory
    join(process.cwd(), 'tools', 'bdk-tui', 'config', 'bdk.env'),
    
    // 4. User override
    join(homedir(), '.bdk-tui', 'config.env'),
  ];
  
  for (const path of searchPaths) {
    if (existsSync(path)) {
      console.log(`[config] Using: ${path}`);
      return path;
    }
  }
  
  throw new Error(
    'Config file not found. Expected locations:\n' +
    searchPaths.map(p => `  - ${p}`).join('\n')
  );
}

function parseEnvFile(filePath: string): Record<string, string> {
  const content = readFileSync(filePath, 'utf-8');
  const settings: Record<string, string> = {};
  
  for (const line of content.split('\n')) {
    const trimmed = line.trim();
    
    // Skip empty lines and comments
    if (!trimmed || trimmed.startsWith('#')) continue;
    
    const eqIndex = trimmed.indexOf('=');
    if (eqIndex === -1) continue;
    
    const key = trimmed.substring(0, eqIndex).trim();
    let value = trimmed.substring(eqIndex + 1).trim();
    
    // Remove surrounding quotes
    if ((value.startsWith('"') && value.endsWith('"')) ||
        (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }
    
    settings[key] = value;
  }
  
  return settings;
}

function getCachePath(rootPath: string): string {
  return join(rootPath, '.bdk', '.solution-cache');
}

function loadCachedSolution(rootPath: string): string | null {
  const cachePath = getCachePath(rootPath);
  if (!existsSync(cachePath)) {
    return null;
  }
  
  try {
    return readFileSync(cachePath, 'utf-8').trim();
  } catch {
    return null;
  }
}

function saveCachedSolution(rootPath: string, solutionFile: string): void {
  const cacheDir = join(rootPath, '.bdk');
  if (!existsSync(cacheDir)) {
    mkdirSync(cacheDir, { recursive: true });
  }
  
  writeFileSync(getCachePath(rootPath), solutionFile, 'utf-8');
}

function findSolutionFiles(rootPath: string): string[] {
  const files = readdirSync(rootPath);
  
  // Look for .slnx first (new format), then .sln
  const solutions: string[] = [];
  
  for (const file of files) {
    if (file.endsWith('.slnx') || file.endsWith('.sln')) {
      solutions.push(file);
    }
  }
  
  return solutions;
}

export async function selectSolutionFile(rootPath: string, onOutput?: (line: string) => void): Promise<string> {
  const solutions = findSolutionFiles(rootPath);
  
  // No solutions found - this shouldn't happen in a proper .NET solution
  if (solutions.length === 0) {
    throw new Error('No solution files (.slnx or .sln) found in repository root');
  }
  
  // Single solution - use it
  if (solutions.length === 1) {
    const selected = solutions[0];
    if (onOutput) {
      onOutput(`Using solution: ${selected}`);
    }
    return selected;
  }
  
  // Multiple solutions - show selection dialog
  if (onOutput) {
    onOutput(`Found ${solutions.length} solution files:`);
  }
  
  // Import dialog dynamically to avoid circular dependency
  const { createInterface } = await import('readline');
  const rl = createInterface({
    input: process.stdin,
    output: process.stdout
  });
  
  return new Promise((resolve) => {
    console.log('\nAvailable solutions:');
    solutions.forEach((sol, idx) => {
      console.log(`  ${idx + 1}) ${sol}`);
    });
    
    rl.question(`Select solution (1-${solutions.length}): `, (answer) => {
      rl.close();
      
      const selection = parseInt(answer, 10);
      if (isNaN(selection) || selection < 1 || selection > solutions.length) {
        console.log('\nInvalid selection, using first solution...');
        resolve(solutions[0]);
      } else {
        const selected = solutions[selection - 1];
        console.log(`\nSelected: ${selected}\n`);
        saveCachedSolution(rootPath, selected);
        resolve(selected);
      }
    });
  });
}

export function loadConfig(solutionFile?: string): BdkConfig {
  const configPath = findConfigPath();
  const settings = parseEnvFile(configPath);
  
  // Root path should be the repo root, not where the binary is
  // When running from tools/bdk-tui, we need to go up two levels
  let rootPath = process.cwd();
  
  // If we're running from within tools/bdk-tui, go up to repo root
  if (rootPath.endsWith('tools/bdk-tui') || rootPath.endsWith('tools\\bdk-tui')) {
    rootPath = join(rootPath, '..', '..');
  }
  
  // Load cached solution if no explicit solution provided
  if (!solutionFile) {
    solutionFile = loadCachedSolution(rootPath);
  }
  
  // If still no solution, auto-detect
  if (!solutionFile) {
    const solutions = findSolutionFiles(rootPath);
    if (solutions.length === 1) {
      solutionFile = solutions[0];
      saveCachedSolution(rootPath, solutionFile);
    } else if (solutions.length === 0) {
      console.warn('[config] Warning: No solution file found');
      solutionFile = '';
    }
    // If multiple solutions, let selectSolutionFile handle it
  }
  
  return {
    rootPath,
    outputDirectory: settings.OUTPUT_DIRECTORY || '.tmp',
    artifactsDirectory: settings.ARTIFACTS_DIRECTORY || '.artifacts',
    dockerFilePath: settings.DOCKER_FILE_PATH || 'src/Presentation.Web.Server/Dockerfile',
    dockerComposePath: settings.DOCKER_COMPOSE_PATH || 'docker-compose.yml',
    sourcesDirectory: settings.SOURCES_DIRECTORY || 'src',
    modulesDirectory: settings.MODULES_DIRECTORY || 'src/Modules',
    testsDirectory: settings.TESTS_DIRECTORY || 'tests',
    dotnetPublishProject: settings.DOTNET_PUBLISH_PROJECT || 'src/Presentation.Web.Server/Presentation.Web.Server.csproj',
    efStartupProject: settings.EF_STARTUP_PROJECT || 'src/Presentation.Web.Server/Presentation.Web.Server.csproj',
    dockerDbConnectionString: settings.DOCKER_DB_CONNECTIONSTRING || '',
    containerPrefix: settings.CONTAINER_PREFIX || 'bit-devkit-gettingstarted',
    registryHost: settings.REGISTRY_HOST || 'localhost:5000',
    networkName: settings.NETWORK_NAME || 'bit-devkit-network',
    solutionFile: solutionFile || '',
  };
}

export function getConfigPath(): string {
  return findConfigPath();
}

export function selectSolution(rootPath: string, onOutput?: (line: string) => void): Promise<string> {
  return selectSolutionFile(rootPath, onOutput);
}
