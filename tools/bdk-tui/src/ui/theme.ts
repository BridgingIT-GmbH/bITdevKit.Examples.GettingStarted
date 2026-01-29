// Styling utilities - Colors, box drawing, and visual elements

export const colors = {
  // ANSI Color codes
  reset: '\x1b[0m',
  
  // Text colors
  black: '\x1b[30m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  magenta: '\x1b[35m',
  cyan: '\x1b[36m',
  white: '\x1b[37m',
  
  // Bright colors
  brightRed: '\x1b[91m',
  brightGreen: '\x1b[92m',
  brightYellow: '\x1b[93m',
  brightBlue: '\x1b[94m',
  brightMagenta: '\x1b[95m',
  brightCyan: '\x1b[96m',
  brightWhite: '\x1b[97m',
  
  // Background colors
  bgBlack: '\x1b[40m',
  bgRed: '\x1b[41m',
  bgGreen: '\x1b[42m',
  bgYellow: '\x1b[43m',
  bgBlue: '\x1b[44m',
  bgMagenta: '\x1b[45m',
  bgCyan: '\x1b[46m',
  bgWhite: '\x1b[47m',
  
  // Styles
  bold: '\x1b[1m',
  dim: '\x1b[2m',
  italic: '\x1b[3m',
  underline: '\x1b[4m',
  blink: '\x1b[5m',
  inverse: '\x1b[7m',
  hidden: '\x1b[8m',
  strikethrough: '\x1b[9m',
};

export const box = {
  // Single line
  single: {
    horizontal: '─',
    vertical: '│',
    topLeft: '┌',
    topRight: '┐',
    bottomLeft: '└',
    bottomRight: '┘',
    leftT: '├',
    rightT: '┤',
    topT: '┬',
    bottomT: '┴',
    cross: '┼',
  },
  
  // Double line
  double: {
    horizontal: '═',
    vertical: '║',
    topLeft: '╔',
    topRight: '╗',
    bottomLeft: '╚',
    bottomRight: '╝',
    leftT: '╠',
    rightT: '╣',
    topT: '╦',
    bottomT: '╩',
    cross: '╬',
  },
  
  // Rounded corners
  rounded: {
    horizontal: '─',
    vertical: '│',
    topLeft: '╭',
    topRight: '╮',
    bottomLeft: '╰',
    bottomRight: '╯',
    leftT: '├',
    rightT: '┤',
    topT: '┬',
    bottomT: '┴',
    cross: '┼',
  },
};

export const symbols = {
  // Checkmarks
  success: '✓',
  error: '✗',
  info: 'ℹ',
  warn: '⚠',
  
  // Arrows
  arrowLeft: '←',
  arrowRight: '→',
  arrowDown: '↓',
  arrowUp: '↑',
  
  // Dots
  dot: '•',
  bullet: '●',
  circle: '○',
  
  // Spinners
  spinnerFrames: ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'],
};

// Spinner class for animated loading
export class Spinner {
  private frameIndex = 0;
  private interval: NodeJS.Timeout | null = null;
  
  start(callback: (frame: string) => void, intervalMs = 100) {
    this.stop();
    this.interval = setInterval(() => {
      callback(symbols.spinnerFrames[this.frameIndex]);
      this.frameIndex = (this.frameIndex + 1) % symbols.spinnerFrames.length;
    }, intervalMs);
  }
  
  stop() {
    if (this.interval) {
      clearInterval(this.interval);
      this.interval = null;
    }
  }
}

// Color utility functions
export function colorize(text: string, colorCode: string): string {
  return `${colorCode}${text}${colors.reset}`;
}

export function success(text: string): string {
  return colorize(text, colors.green);
}

export function error(text: string): string {
  return colorize(text, colors.red);
}

export function warn(text: string): string {
  return colorize(text, colors.yellow);
}

export function info(text: string): string {
  return colorize(text, colors.cyan);
}

export function bold(text: string): string {
  return colorize(text, colors.bold);
}

export function dim(text: string): string {
  return colorize(text, colors.dim);
}

// Box drawing functions
export function drawBox(
  content: string[],
  options: {
    title?: string;
    titleColor?: string;
    borderColor?: keyof typeof box;
    padding?: number;
    width?: number;
  } = {}
): string {
  const {
    title,
    titleColor = colors.cyan,
    borderColor = 'double',
    padding = 1,
  } = options;
  
  const b = box[borderColor];
  const maxContentWidth = Math.max(...content.map(line => stripAnsi(line).length));
  const boxWidth = options.width || maxContentWidth + (padding * 2) + 2;
  
  let result = '';
  
  // Top border with title
  if (title) {
    const titleText = colorize(` ${title} `, titleColor);
    const titleWidth = stripAnsi(title).length;
    const remainingWidth = boxWidth - titleWidth;
    const leftWidth = Math.floor(remainingWidth / 2);
    const rightWidth = remainingWidth - leftWidth;
    result += `${b.topLeft}${b.horizontal.repeat(leftWidth)}${titleText}${b.horizontal.repeat(rightWidth)}${b.topRight}\n`;
  } else {
    result += `${b.topLeft}${b.horizontal.repeat(boxWidth)}${b.topRight}\n`;
  }
  
  // Content lines
  for (const line of content) {
    const plainLine = stripAnsi(line);
    const paddingWidth = boxWidth - plainLine.length - 2;
    const leftPad = padding;
    const rightPad = paddingWidth - padding;
    result += `${b.vertical}${' '.repeat(leftPad)}${line}${' '.repeat(rightPad)}${b.vertical}\n`;
  }
  
  // Bottom border
  result += `${b.bottomLeft}${b.horizontal.repeat(boxWidth)}${b.bottomRight}`;
  
  return result;
}

export function drawSeparator(char: string = '─', width: number = 60): string {
  return char.repeat(width);
}

export function drawSection(title: string): string {
  const line = drawSeparator('─', 60);
  const centered = title.padStart(30 + Math.floor(title.length / 2)).padEnd(60);
  return `${colors.cyan}${line}${colors.reset}\n${colors.bold}${colors.cyan}${centered}${colors.reset}\n${colors.cyan}${line}${colors.reset}`;
}

// Utility to strip ANSI codes
function stripAnsi(text: string): string {
  return text.replace(/\x1b\[[0-9;]*m/g, '');
}

// Width calculation (accounts for ANSI codes)
export function getDisplayWidth(text: string): number {
  return stripAnsi(text).length;
}

// Center text within width
export function center(text: string, width: number): string {
  const textWidth = getDisplayWidth(text);
  const padding = Math.max(0, width - textWidth);
  const leftPad = Math.floor(padding / 2);
  const rightPad = padding - leftPad;
  return ' '.repeat(leftPad) + text + ' '.repeat(rightPad);
}

// Pad text to width
export function pad(text: string, width: number, align: 'left' | 'center' | 'right' = 'left'): string {
  const textWidth = getDisplayWidth(text);
  if (textWidth >= width) return text;
  
  const padding = width - textWidth;
  
  switch (align) {
    case 'center':
      return center(text, width);
    case 'right':
      return ' '.repeat(padding) + text;
    default:
      return text + ' '.repeat(padding);
  }
}
