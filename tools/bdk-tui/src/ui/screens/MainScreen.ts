// Main screen - Category selection with arrow key navigation

import type { BdkConfig } from '../../types/config';
import { getCategories } from '../../tasks/registry';
import { TaskScreen } from './TaskScreen';
import { SelectMenu } from '../components/SelectMenu';
import { symbols, info, bold } from '../theme';

export class MainScreen {
  private config: BdkConfig;
  private selectMenu: SelectMenu;
  
  constructor(config: BdkConfig) {
    this.config = config;
    this.selectMenu = new SelectMenu();
  }
  
  async show(): Promise<void> {
    const categories = getCategories();
    
    while (true) {
      const result = await this.selectMenu.show({
        title: 'Select a category:',
        items: categories.map(cat => ({
          label: cat,
          value: cat,
          description: ''
        })),
        allowBack: false,
        allowExit: true
      });
      
      // Handle exit
      if (result === '__EXIT__' || result === null) {
        console.clear();
        console.log(`${bold(info(symbols.success))} Goodbye!`);
        break;
      }
      
      // Show task screen for selected category
      const taskScreen = new TaskScreen(this.config, result);
      const taskResult = await taskScreen.show();
      
      // If task screen returned exit, propagate it
      if (taskResult === '__EXIT__') {
        console.clear();
        console.log(`${bold(info(symbols.success))} Goodbye!`);
        break;
      }
      
      // Loop back to category selection
    }
  }
  
  destroy(): void {
    // Cleanup if needed
  }
}
