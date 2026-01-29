// Task type definitions

export interface TaskDefinition {
  key: string;
  label: string;
  description: string;
  category: string;
  execute: (context: TaskContext) => Promise<TaskResult>;
}

export interface TaskContext {
  config: BdkConfig;
  onOutput?: (line: string) => void;
  onError?: (line: string) => void;
}

export interface TaskResult {
  success: boolean;
  exitCode: number;
  output?: string;
  error?: string;
  duration: number;
}

export interface BdkConfig {
  rootPath: string;
  outputDirectory: string;
  artifactsDirectory: string;
  dockerFilePath: string;
  dockerComposePath: string;
  sourcesDirectory: string;
  modulesDirectory: string;
  testsDirectory: string;
  dotnetPublishProject: string;
  efStartupProject: string;
  dockerDbConnectionString: string;
  containerPrefix: string;
  registryHost: string;
  networkName: string;
}
