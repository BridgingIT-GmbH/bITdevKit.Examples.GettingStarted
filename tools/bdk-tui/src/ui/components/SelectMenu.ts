// SelectMenu component using Enquirer for arrow key navigation
// Enquirer provides interactive prompts without interfering with console output

import Enquirer from 'enquirer';
import { colors, box, info, dim, symbols, center, bold } from '../theme';

export interface MenuItem {
  label: string;
  value: string;
  description?: string;
}

export interface SelectMenuOptions {
  title: string;
  items: MenuItem[];
  allowBack?: boolean;
  allowExit?: boolean;
}

export class SelectMenu {
  async show(options: SelectMenuOptions): Promise<string | null> {
    // Prepare choices for Enquirer
    const choices = options.items.map(item => ({
      name: item.label,
      value: item.value,
      message: item.label,
      hint: item.description || ''
    }));

    // Add navigation options
    if (options.allowBack) {
      choices.push({
        name: `${symbols.arrowLeft} Back`,
        value: '__BACK__',
        message: `${symbols.arrowLeft} Back`,
        hint: 'Return to previous menu'
      });
    }
    if (options.allowExit) {
      choices.push({
        name: `${symbols.error} Exit`,
        value: '__EXIT__',
        message: `${symbols.error} Exit`,
        hint: 'Exit to application'
      });
    }

    // Print title with improved styling
    console.clear();
    const headerWidth = 60;
    console.log(`╔${box.double.horizontal.repeat(headerWidth)}╗`);
    console.log(`║${info(center('bITdevKit BDK Tool', headerWidth))}║`);
    console.log(`╚${box.double.horizontal.repeat(headerWidth)}╝`);
    console.log('');
    console.log(`${bold(info('›'))} ${options.title}`);
    console.log('');
    console.log(`${dim(`Use ${info('↑/↓')} to navigate, ${info('Enter')} to select`)}`);
    console.log('');

    try {
      // Create Enquirer prompt
      const enquirer = new Enquirer();

      const result = await enquirer.prompt({
        type: 'select',
        name: 'selection',
        message: '',
        choices: choices.map(c => ({
          name: c.value,
          message: c.message,
          hint: c.hint
        })),
        initial: 0
      });

      // @ts-ignore
      return result.selection as string;
    } catch (error) {
      // Handle cancel (Ctrl+C)
      if (options.allowBack) {
        return '__BACK__';
      }
      return '__EXIT__';
    }
  }
}
