// Module selection dialog
import enquirer from 'enquirer';
import { discoverModules, resolveModuleName, type ModuleInfo } from '../../lib/discovery.js';
import { colors, bold, info, center, box } from '../theme.js';

const { Select } = enquirer;

export interface ModuleDialogOptions {
  projectRoot: string;
  envModuleName?: string;
  title?: string;
  message?: string;
  allowAll?: boolean;
}

export interface ModuleDialogResult {
  moduleName: string;
  isAll: boolean;
}

/**
 * Show module selection dialog
 * Auto-selects if only one module exists or env variable is set
 */
export async function showModuleDialog(options: ModuleDialogOptions): Promise<ModuleDialogResult | null> {
  const {
    projectRoot,
    envModuleName,
    title = 'Select Module',
    message = 'Choose a module:',
    allowAll = false,
  } = options;
  
  // Try to resolve automatically
  const resolved = resolveModuleName(projectRoot, envModuleName);
  if (resolved) {
    console.log(info(`${bold('Module:')} ${resolved} (auto-selected)`));
    return { moduleName: resolved, isAll: false };
  }
  
  // Discover available modules
  const modules = discoverModules(projectRoot);
  
  if (modules.length === 0) {
    console.log(`${colors.red}${bold('Error:')} No modules found in ${projectRoot}/src/Modules${colors.reset}`);
    return null;
  }
  
  // Build choices
  const choices = modules.map(m => ({
    name: m.name,
    message: formatModuleChoice(m),
    value: m.name,
  }));
  
  if (allowAll) {
    choices.unshift({
      name: 'ALL',
      message: `${colors.cyan}${bold('All Modules')}${colors.reset}`,
      value: 'ALL',
    });
  }
  
  // Show header
  console.log('');
  const b = box.double;
  const headerWidth = 60;
  const titleText = bold(title);
  console.log(`${colors.cyan}${b.topLeft}${b.horizontal.repeat(headerWidth)}${b.topRight}${colors.reset}`);
  console.log(`${colors.cyan}${b.vertical}${center(titleText, headerWidth)}${b.vertical}${colors.reset}`);
  console.log(`${colors.cyan}${b.bottomLeft}${b.horizontal.repeat(headerWidth)}${b.bottomRight}${colors.reset}`);
  console.log('');
  
  try {
    const prompt = new Select({
      name: 'module',
      message: message,
      choices: choices,
    });
    
    const selected = await prompt.run();
    
    if (selected === 'ALL') {
      return { moduleName: 'ALL', isAll: true };
    }
    
    return { moduleName: selected, isAll: false };
  } catch (error) {
    // User cancelled (Ctrl+C)
    return null;
  }
}

function formatModuleChoice(module: ModuleInfo): string {
  let text = `${colors.bold}${module.name}${colors.reset}`;
  
  const tags: string[] = [];
  if (module.hasDbContext) {
    tags.push(`${colors.green}DB${colors.reset}`);
  }
  if (module.hasInfrastructure) {
    tags.push(`${colors.cyan}Infra${colors.reset}`);
  }
  
  if (tags.length > 0) {
    text += ` ${colors.dim}[${tags.join(' ')}]${colors.reset}`;
  }
  
  return text;
}
