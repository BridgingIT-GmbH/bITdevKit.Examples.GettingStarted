// Test script for dialogs
import { showModuleDialog } from './ui/dialogs/ModuleDialog.js';
import { showDbContextDialog } from './ui/dialogs/DbContextDialog.js';
import { showTextInputDialog, validators } from './ui/dialogs/TextInputDialog.js';
import { showConfirmDialog } from './ui/dialogs/ConfirmDialog.js';
import { getDotnetProcesses, showProcessSelectDialog } from './ui/dialogs/ProcessSelectDialog.js';
import { colors, bold } from './ui/theme.js';
import { join } from 'path';

// Navigate up from tools/bdk-tui to project root
const projectRoot = join(process.cwd(), '..', '..');

async function testDialogs() {
  console.log(`\n${colors.cyan}${bold('=== Dialog Testing Suite ===')}${colors.reset}\n`);
  
  // Test 1: Module Dialog
  console.log(`${colors.yellow}Test 1: Module Selection${colors.reset}`);
  const moduleResult = await showModuleDialog({
    projectRoot,
    allowAll: true,
  });
  
  if (!moduleResult) {
    console.log(`${colors.red}Module selection cancelled${colors.reset}`);
    return;
  }
  
  console.log(`${colors.green}Selected: ${moduleResult.moduleName} (isAll: ${moduleResult.isAll})${colors.reset}\n`);
  
  // Test 2: DbContext Dialog (only if single module selected)
  if (!moduleResult.isAll) {
    console.log(`${colors.yellow}Test 2: DbContext Selection${colors.reset}`);
    const dbContextResult = await showDbContextDialog({
      projectRoot,
      moduleName: moduleResult.moduleName,
    });
    
    if (!dbContextResult) {
      console.log(`${colors.red}DbContext selection cancelled${colors.reset}`);
    } else {
      console.log(`${colors.green}Selected: ${dbContextResult}${colors.reset}\n`);
    }
  }
  
  // Test 3: Text Input Dialog
  console.log(`${colors.yellow}Test 3: Text Input${colors.reset}`);
  const textResult = await showTextInputDialog({
    message: 'Enter migration name:',
    validate: validators.migrationName,
  });
  
  if (!textResult) {
    console.log(`${colors.red}Text input cancelled${colors.reset}`);
  } else {
    console.log(`${colors.green}Entered: ${textResult}${colors.reset}\n`);
  }
  
  // Test 4: Confirm Dialog
  console.log(`${colors.yellow}Test 4: Confirmation${colors.reset}`);
  const confirmResult = await showConfirmDialog({
    message: 'Do you want to proceed?',
    initial: true,
  });
  
  if (confirmResult === null) {
    console.log(`${colors.red}Confirmation cancelled${colors.reset}`);
  } else {
    console.log(`${colors.green}Confirmed: ${confirmResult ? 'Yes' : 'No'}${colors.reset}\n`);
  }
  
  // Test 5: Process Selection Dialog
  console.log(`${colors.yellow}Test 5: Process Selection${colors.reset}`);
  console.log('Fetching dotnet processes...');
  const processes = await getDotnetProcesses();
  
  if (processes.length > 0) {
    const processResult = await showProcessSelectDialog({
      processes,
    });
    
    if (!processResult) {
      console.log(`${colors.red}Process selection cancelled${colors.reset}`);
    } else {
      console.log(`${colors.green}Selected PID: ${processResult}${colors.reset}\n`);
    }
  } else {
    console.log(`${colors.yellow}No dotnet processes running${colors.reset}\n`);
  }
  
  console.log(`${colors.cyan}${bold('=== All Tests Complete ===')}${colors.reset}\n`);
}

testDialogs().catch(error => {
  console.error(`${colors.red}Error:${colors.reset}`, error);
  process.exit(1);
});
