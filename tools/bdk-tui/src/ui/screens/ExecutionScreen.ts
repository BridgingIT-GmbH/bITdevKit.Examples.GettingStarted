// Execution screen - Shows task execution with live output

import type { BdkConfig } from '../../types/config';
import type { TaskDefinition } from '../../types/task';
import { colors, symbols, box, drawSeparator, success, error, bold, info, dim } from '../theme';

export class ExecutionScreen {
  private config: BdkConfig;
  private task: TaskDefinition;
  private outputLines: string[] = [];

  constructor(config: BdkConfig, task: TaskDefinition) {
    this.config = config;
    this.task = task;
  }

  async show(): Promise<string | null> {
    // Small delay to let terminal mode switch settle
    await new Promise(resolve => setTimeout(resolve, 100));

    // Show task header with improved styling
    console.log('');
    console.log(drawSeparator('═', 60));
    console.log(`${bold(info('Task:'))} ${this.task.label}`);
    console.log(`${dim(this.task.description)}`);
    console.log(drawSeparator('═', 60));
    console.log('');

    const startTime = Date.now();

    // Show initial message
    console.log(`${colors.cyan}${symbols.arrowRight} Executing task...${colors.reset}`);

    try {
      const result = await this.task.execute({
        config: this.config,
        onOutput: (line) => {
          // Print output in real-time with styling
          console.log(`  ${line}`);
          this.outputLines.push(line);
        },
        onError: (line) => {
          // Stop spinner
          // if (spinnerActive) {
          //   spinner.stop();
          //   spinnerActive = false;
          // }

          // Print errors with red styling
          console.error(`  ${error(line)}`);
          this.outputLines.push(`ERROR: ${line}`);
        }
      });

      const duration = Date.now() - startTime;

      console.log('');
      console.log(drawSeparator('═', 60));

      if (result.success) {
        console.log(`${colors.bold}${success(symbols.success)} Task completed successfully (${duration}ms)${colors.reset}`);
      } else {
        console.log(`${colors.bold}${error(symbols.error)} Task failed with exit code ${result.exitCode} (${duration}ms)${colors.reset}`);
        if (result.error) {
          console.error(`  ${error(result.error)}`);
        }
      }

      console.log(drawSeparator('═', 60));
      console.log('');
    } catch (err) {
      // Stop spinner
      // if (spinnerActive) {
      //   spinner.stop();
      // }

      console.log('');
      console.log(drawSeparator('═', 60));
      console.error(`${colors.bold}${error(symbols.error)} Task failed with error:${colors.reset} ${String(err)}`);
      console.log(drawSeparator('═', 60));
      console.log('');
    }

    // Wait for keypress before returning to menu
    await this.waitForKeypress();

    return null;
  }

  private async waitForKeypress(): Promise<void> {
    console.log(`${dim(bold(info(symbols.arrowRight)))} ${dim('Press any key to continue...')}`);

    // Use readline for cross-platform compatibility
    const { createInterface } = await import('readline');
    const rl = createInterface({
      input: process.stdin,
      output: process.stdout
    });

    const promise = new Promise<void>((resolve) => {
      rl.question('', () => {
        rl.close();
        console.log('');
        resolve();
      });
    });

    await promise;
  }
}
