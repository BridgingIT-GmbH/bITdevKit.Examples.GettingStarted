// Runtime Identifier (RID) selection dialog
import enquirer from 'enquirer';
import { colors, bold, center, box } from '../theme.js';

const { Select } = enquirer;

export interface RidDialogOptions {
  title?: string;
  message?: string;
}

/**
 * Show RID (Runtime Identifier) selection dialog
 * Returns selected RID or null if cancelled
 */
export async function showRidDialog(options: RidDialogOptions = {}): Promise<string | null> {
  const {
    title = 'Select Runtime Identifier',
    message = 'Choose target platform:',
  } = options;
  
  const commonRids = [
    { name: 'win-x64', message: `${colors.bold}Windows x64${colors.reset}`, value: 'win-x64' },
    { name: 'win-x86', message: `Windows x86 ${colors.dim}(32-bit)${colors.reset}`, value: 'win-x86' },
    { name: 'win-arm64', message: `Windows ARM64 ${colors.dim}(ARM)${colors.reset}`, value: 'win-arm64' },
    { name: 'linux-x64', message: `${colors.bold}Linux x64${colors.reset}`, value: 'linux-x64' },
    { name: 'linux-arm64', message: `Linux ARM64 ${colors.dim}(ARM)${colors.reset}`, value: 'linux-arm64' },
    { name: 'linux-musl-x64', message: `Linux x64 ${colors.dim}(musl/Alpine)${colors.reset}`, value: 'linux-musl-x64' },
    { name: 'osx-x64', message: `${colors.bold}macOS x64${colors.reset} ${colors.dim}(Intel)${colors.reset}`, value: 'osx-x64' },
    { name: 'osx-arm64', message: `${colors.bold}macOS ARM64${colors.reset} ${colors.dim}(Apple Silicon)${colors.reset}`, value: 'osx-arm64' },
  ];
  
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
      name: 'rid',
      message: message,
      choices: commonRids,
    });
    
    const selected = await prompt.run();
    return selected;
  } catch (error) {
    // User cancelled (Ctrl+C)
    return null;
  }
}
