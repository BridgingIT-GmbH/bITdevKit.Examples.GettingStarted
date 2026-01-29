#!/usr/bin/env bun
// BDK TUI - Terminal User Interface for bITdevKit tasks

import { loadConfig } from './core/config';
import { getCategories, getTasksByCategory, getTaskByKey } from './tasks/registry';
import { colors, box, success, info, bold, dim, center, symbols } from './ui/theme';

// Draw header with improved styling
const titleText = bold(info('bITdevKit BDK Tool (TUI)'));
const centeredTitle = center(titleText, 38);

const headerLines = [
  '',
  `╔${box.double.horizontal.repeat(40)}╗`,
  `║${centeredTitle}║`,
  `╚${box.double.horizontal.repeat(40)}╝`,
  '',
];

headerLines.forEach(line => console.log(line));

// Load configuration
let config;
try {
  config = loadConfig();
  console.log(`${success(symbols.success)} Config loaded`);
  console.log(`${success(symbols.success)} Root: ${dim(config.rootPath)}`);
  console.log('');
} catch (error) {
  console.error(`${error(symbols.error)} Failed to load config: ${error}`);
  process.exit(1);
}

// Check for direct task execution
const args = process.argv.slice(2);

if (args.length > 0) {
  // Direct task execution: ./bdk-tui build
  const taskKey = args[0];
  
  if (taskKey === '--help' || taskKey === '-h') {
    console.log('Usage:');
    console.log('  bdk-tui              Interactive mode');
    console.log('  bdk-tui <task>       Execute task directly');
    console.log('  bdk-tui --help       Show this help');
    console.log('');
    console.log('Available tasks:');
    const categories = getCategories();
    for (const category of categories) {
      console.log(`\n${category}:`);
      const tasks = getTasksByCategory(category);
      for (const task of tasks) {
        console.log(`  ${task.key.padEnd(20)} ${task.description}`);
      }
    }
    process.exit(0);
  }
  
  const task = getTaskByKey(taskKey);
  if (!task) {
    console.error(`✗ Unknown task: ${taskKey}`);
    console.error('  Run with --help to see available tasks');
    process.exit(1);
  }
  
  console.log(`${bold(info('Task:'))} ${task.label}`);
  console.log(`${dim(task.description)}`);
  console.log(`${colors.cyan}${'═'.repeat(50)}${colors.reset}`);
  console.log('');
  
  const result = await task.execute({
    config,
    onOutput: (line) => console.log(line),
    onError: (line) => console.error(line)
  });
  
  console.log('');
  console.log(`${colors.cyan}${'═'.repeat(50)}${colors.reset}`);
  
  if (result.success) {
    console.log(`${bold(success(symbols.success))} Task completed successfully (${result.duration}ms)`);
  } else {
    console.error(`${bold(error(symbols.error))} Task failed with exit code ${result.exitCode} (${result.duration}ms)`);
    if (result.error) {
      console.error(`  ${error(result.error)}`);
    }
  }
  
  console.log('');
  await waitForKeypress();
  
  process.exit(result.success ? 0 : result.exitCode);
}

async function waitForKeypress(): Promise<void> {
  console.log(`${dim(bold(info(symbols.arrowRight)))} ${dim('Press any key to exit...')}`);
  
  // Use readline for cross-platform compatibility (works in both Node and Bun)
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

// Interactive mode - Fancy TUI
const { MainScreen } = await import('./ui/screens/MainScreen');
const mainScreen = new MainScreen(config);

// Handle graceful shutdown
process.on('SIGINT', () => {
  console.log('\n\nExiting...');
  mainScreen.destroy();
  process.exit(0);
});

await mainScreen.show();
