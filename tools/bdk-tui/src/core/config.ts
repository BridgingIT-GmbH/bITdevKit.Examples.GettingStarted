// Configuration loader - reads config/bdk.env at runtime

import { readFileSync, existsSync } from 'fs';
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

export function loadConfig(): BdkConfig {
  const configPath = findConfigPath();
  const settings = parseEnvFile(configPath);
  
  // Root path should be the repo root, not where the binary is
  // When running from tools/bdk-tui, we need to go up two levels
  let rootPath = process.cwd();
  
  // If we're running from within tools/bdk-tui, go up to repo root
  if (rootPath.endsWith('tools/bdk-tui') || rootPath.endsWith('tools\\bdk-tui')) {
    rootPath = join(rootPath, '..', '..');
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
  };
}

export function getConfigPath(): string {
  return findConfigPath();
}
