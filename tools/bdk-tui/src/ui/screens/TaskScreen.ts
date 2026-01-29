// Task screen - Task selection with arrow key navigation

import type { BdkConfig } from '../../types/config';
import { getTasksByCategory } from '../../tasks/registry';
import { ExecutionScreen } from './ExecutionScreen';
import { SelectMenu } from '../components/SelectMenu';

export class TaskScreen {
  private config: BdkConfig;
  private category: string;
  private selectMenu: SelectMenu;
  
  constructor(config: BdkConfig, category: string) {
    this.config = config;
    this.category = category;
    this.selectMenu = new SelectMenu();
  }
  
  async show(): Promise<string | null> {
    const tasks = getTasksByCategory(this.category);
    
    while (true) {
      const result = await this.selectMenu.show({
        title: `Category: ${this.category}\n\nSelect a task:`,
        items: tasks.map(task => ({
          label: task.label,
          value: task.key,
          description: task.description
        })),
        allowBack: true,
        allowExit: true
      });
      
      // Handle back
      if (result === '__BACK__') {
        return '__BACK__';
      }
      
      // Handle exit
      if (result === '__EXIT__' || result === null) {
        return '__EXIT__';
      }
      
      // Find and execute the selected task
      const selectedTask = tasks.find(t => t.key === result);
      if (!selectedTask) {
        continue;
      }
      
      // Execute task
      const execScreen = new ExecutionScreen(this.config, selectedTask);
      const execResult = await execScreen.show();
      
      // If execution screen returned exit, propagate it
      if (execResult === '__EXIT__') {
        return '__EXIT__';
      }
      
      // Menu renders below the output
    }
  }
}
