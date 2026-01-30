// Text input dialog for user input
import enquirer from 'enquirer';
import { colors, bold, center, box } from '../theme.js';

const { Input } = enquirer;

export interface TextInputDialogOptions {
  title?: string;
  message: string;
  initial?: string;
  validate?: (value: string) => boolean | string;
}

/**
 * Show text input dialog
 * Returns the entered text or null if cancelled
 */
export async function showTextInputDialog(options: TextInputDialogOptions): Promise<string | null> {
  const {
    title = 'Input Required',
    message,
    initial = '',
    validate,
  } = options;
  
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
    const prompt = new Input({
      name: 'input',
      message: message,
      initial: initial,
      validate: validate,
    });
    
    const result = await prompt.run();
    return result.trim() || null;
  } catch (error) {
    // User cancelled (Ctrl+C)
    return null;
  }
}

/**
 * Predefined validators
 */
export const validators = {
  notEmpty: (value: string) => {
    return value.trim().length > 0 || 'Value cannot be empty';
  },
  
  alphanumeric: (value: string) => {
    return /^[a-zA-Z0-9_-]+$/.test(value) || 'Only alphanumeric characters, underscores, and hyphens allowed';
  },
  
  migrationName: (value: string) => {
    if (!value.trim()) return 'Migration name cannot be empty';
    if (!/^[a-zA-Z]/.test(value)) return 'Migration name must start with a letter';
    if (!/^[a-zA-Z0-9_]+$/.test(value)) return 'Only alphanumeric characters and underscores allowed';
    return true;
  },
  
  integer: (value: string) => {
    return /^\d+$/.test(value) || 'Must be a valid integer';
  },
  
  url: (value: string) => {
    try {
      new URL(value);
      return true;
    } catch {
      return 'Must be a valid URL';
    }
  },
};
