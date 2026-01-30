// Confirmation dialog for yes/no questions
import enquirer from 'enquirer';
import { colors, bold, center, box } from '../theme.js';

const { Confirm } = enquirer;

export interface ConfirmDialogOptions {
  title?: string;
  message: string;
  initial?: boolean;
}

/**
 * Show confirmation dialog (Yes/No)
 * Returns true for Yes, false for No, null if cancelled
 */
export async function showConfirmDialog(options: ConfirmDialogOptions): Promise<boolean | null> {
  const {
    title = 'Confirmation',
    message,
    initial = false,
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
    const prompt = new Confirm({
      name: 'confirm',
      message: message,
      initial: initial,
    });
    
    const result = await prompt.run();
    return result;
  } catch (error) {
    // User cancelled (Ctrl+C)
    return null;
  }
}
