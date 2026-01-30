// DbContext selection dialog
import enquirer from 'enquirer';
import { discoverDbContexts, resolveDbContextName, type DbContextInfo } from '../../lib/discovery.js';
import { colors, bold, info, center, box } from '../theme.js';

const { Select } = enquirer;

export interface DbContextDialogOptions {
  projectRoot: string;
  moduleName: string;
  title?: string;
  message?: string;
}

/**
 * Show DbContext selection dialog
 * Auto-selects if only one DbContext exists in the module
 * Always prompts if multiple - never caches (per user requirement)
 */
export async function showDbContextDialog(options: DbContextDialogOptions): Promise<string | null> {
  const {
    projectRoot,
    moduleName,
    title = 'Select DbContext',
    message = 'Choose a DbContext:',
  } = options;
  
  // Discover DbContexts in module
  const contexts = discoverDbContexts(projectRoot, moduleName);
  
  if (contexts.length === 0) {
    console.log(`${colors.red}${bold('Error:')} No DbContext found in module ${moduleName}${colors.reset}`);
    return null;
  }
  
  // Auto-select if only one
  if (contexts.length === 1) {
    console.log(info(`${bold('DbContext:')} ${contexts[0].name} (auto-selected)`));
    return contexts[0].name;
  }
  
  // Build choices
  const choices = contexts.map(c => ({
    name: c.name,
    message: formatDbContextChoice(c),
    value: c.name,
  }));
  
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
      name: 'dbcontext',
      message: message,
      choices: choices,
    });
    
    const selected = await prompt.run();
    return selected;
  } catch (error) {
    // User cancelled (Ctrl+C)
    return null;
  }
}

function formatDbContextChoice(context: DbContextInfo): string {
  let text = `${colors.bold}${context.name}${colors.reset}`;
  text += ` ${colors.dim}(${context.module})${colors.reset}`;
  return text;
}
