// Process selection dialog for selecting running dotnet processes
import enquirer from 'enquirer';
import { colors, bold, center, box, dim } from '../theme.js';

const { Select } = enquirer;

export interface ProcessInfo {
  pid: number;
  name: string;
  command?: string;
}

export interface ProcessSelectDialogOptions {
  processes: ProcessInfo[];
  title?: string;
  message?: string;
}

/**
 * Show process selection dialog
 * Returns selected PID or null if cancelled
 */
export async function showProcessSelectDialog(options: ProcessSelectDialogOptions): Promise<number | null> {
  const {
    processes,
    title = 'Select Process',
    message = 'Choose a process:',
  } = options;
  
  if (processes.length === 0) {
    console.log(`${colors.yellow}${bold('Warning:')} No processes found${colors.reset}`);
    return null;
  }
  
  // Build choices
  const choices = processes.map(p => ({
    name: p.pid.toString(),
    message: formatProcessChoice(p),
    value: p.pid,
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
      name: 'process',
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

function formatProcessChoice(process: ProcessInfo): string {
  let text = `${colors.bold}PID ${process.pid}${colors.reset}`;
  text += ` - ${process.name}`;
  
  if (process.command) {
    const shortCmd = process.command.length > 50 
      ? process.command.substring(0, 47) + '...' 
      : process.command;
    text += ` ${dim(`(${shortCmd})`)}`;
  }
  
  return text;
}

/**
 * Get running dotnet processes
 * Platform-specific implementation
 */
export async function getDotnetProcesses(): Promise<ProcessInfo[]> {
  const { spawn } = await import('child_process');
  const { promisify } = await import('util');
  const execAsync = promisify(spawn);
  
  const isWindows = process.platform === 'win32';
  
  try {
    let command: string;
    let args: string[];
    
    if (isWindows) {
      // Windows: Use tasklist to find dotnet processes
      command = 'tasklist';
      args = ['/FO', 'CSV', '/NH', '/FI', 'IMAGENAME eq dotnet.exe'];
    } else {
      // Unix: Use ps to find dotnet processes
      command = 'ps';
      args = ['-eo', 'pid,comm,args'];
    }
    
    const result = await new Promise<string>((resolve, reject) => {
      const proc = spawn(command, args);
      let output = '';
      
      proc.stdout.on('data', (data) => {
        output += data.toString();
      });
      
      proc.on('close', (code) => {
        if (code === 0) {
          resolve(output);
        } else {
          reject(new Error(`Process exited with code ${code}`));
        }
      });
      
      proc.on('error', reject);
    });
    
    return parseProcessList(result, isWindows);
  } catch (error) {
    console.error(`${colors.red}Error getting process list:${colors.reset}`, error);
    return [];
  }
}

function parseProcessList(output: string, isWindows: boolean): ProcessInfo[] {
  const processes: ProcessInfo[] = [];
  const lines = output.trim().split('\n');
  
  for (const line of lines) {
    if (!line.trim()) continue;
    
    try {
      if (isWindows) {
        // Windows CSV format: "ImageName","PID","SessionName","Session#","MemUsage"
        const matches = line.match(/"([^"]+)","(\d+)"/);
        if (matches) {
          const [, name, pidStr] = matches;
          const pid = parseInt(pidStr, 10);
          if (name.toLowerCase().includes('dotnet')) {
            processes.push({ pid, name });
          }
        }
      } else {
        // Unix format: PID COMMAND ARGS
        const parts = line.trim().split(/\s+/);
        if (parts.length >= 2) {
          const pid = parseInt(parts[0], 10);
          const command = parts.slice(1).join(' ');
          if (command.toLowerCase().includes('dotnet')) {
            processes.push({
              pid,
              name: 'dotnet',
              command,
            });
          }
        }
      }
    } catch {
      // Ignore parse errors
    }
  }
  
  return processes;
}
