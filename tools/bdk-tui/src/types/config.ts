// Configuration type definitions

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
  solutionFile: string;
}
