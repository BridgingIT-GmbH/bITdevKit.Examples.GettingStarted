// Docker CLI wrapper
import { CrossPlatformExecutor } from '../core/executor.js';

export interface DockerBuildOptions {
  dockerfile: string;
  tag: string;
  context?: string;
  noCache?: boolean;
  buildArgs?: Record<string, string>;
  onOutput?: (line: string) => void;
  onError?: (line: string) => void;
}

export interface DockerRunOptions {
  image: string;
  containerName: string;
  hostPort?: number;
  containerPort?: number;
  network?: string;
  env?: Record<string, string>;
  volumes?: string[];
  detached?: boolean;
  onOutput?: (line: string) => void;
  onError?: (line: string) => void;
}

export interface DockerComposeOptions {
  composeFile: string;
  project?: string;
  detached?: boolean;
  onOutput?: (line: string) => void;
  onError?: (line: string) => void;
}

export class DockerCli {
  private executor = new CrossPlatformExecutor();
  
  /**
   * Build Docker image
   */
  async build(options: DockerBuildOptions) {
    const args = ['build'];
    
    if (options.noCache) {
      args.push('--no-cache');
    }
    
    args.push('-f', options.dockerfile);
    args.push('-t', options.tag);
    
    if (options.buildArgs) {
      for (const [key, value] of Object.entries(options.buildArgs)) {
        args.push('--build-arg', `${key}=${value}`);
      }
    }
    
    args.push(options.context || '.');
    
    return this.executor.execute('docker', args, {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Run Docker container
   */
  async run(options: DockerRunOptions) {
    const args = ['run'];
    
    if (options.detached) {
      args.push('-d');
    }
    
    args.push('--name', options.containerName);
    
    if (options.hostPort && options.containerPort) {
      args.push('-p', `${options.hostPort}:${options.containerPort}`);
    }
    
    if (options.network) {
      args.push('--network', options.network);
    }
    
    if (options.env) {
      for (const [key, value] of Object.entries(options.env)) {
        args.push('-e', `${key}=${value}`);
      }
    }
    
    if (options.volumes) {
      for (const volume of options.volumes) {
        args.push('-v', volume);
      }
    }
    
    args.push(options.image);
    
    return this.executor.execute('docker', args, {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Stop container
   */
  async stop(options: {
    containerName: string;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  }) {
    return this.executor.execute('docker', ['stop', options.containerName], {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Remove container
   */
  async remove(options: {
    containerName: string;
    force?: boolean;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  }) {
    const args = ['rm'];
    
    if (options.force) {
      args.push('-f');
    }
    
    args.push(options.containerName);
    
    return this.executor.execute('docker', args, {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Remove image
   */
  async removeImage(options: {
    imageTag: string;
    force?: boolean;
    onOutput?: (line: string) => void;
    onError?: (line: string) => void;
  }) {
    const args = ['rmi'];
    
    if (options.force) {
      args.push('-f');
    }
    
    args.push(options.imageTag);
    
    return this.executor.execute('docker', args, {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Docker Compose up
   */
  async composeUp(options: DockerComposeOptions) {
    const args = ['compose'];
    
    if (options.composeFile) {
      args.push('-f', options.composeFile);
    }
    
    if (options.project) {
      args.push('-p', options.project);
    }
    
    args.push('up');
    
    if (options.detached) {
      args.push('-d');
    }
    
    return this.executor.execute('docker', args, {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Docker Compose down
   */
  async composeDown(options: DockerComposeOptions & { removeVolumes?: boolean }) {
    const args = ['compose'];
    
    if (options.composeFile) {
      args.push('-f', options.composeFile);
    }
    
    if (options.project) {
      args.push('-p', options.project);
    }
    
    args.push('down');
    
    if (options.removeVolumes) {
      args.push('-v');
    }
    
    return this.executor.execute('docker', args, {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Docker Compose pull
   */
  async composePull(options: DockerComposeOptions) {
    const args = ['compose'];
    
    if (options.composeFile) {
      args.push('-f', options.composeFile);
    }
    
    if (options.project) {
      args.push('-p', options.project);
    }
    
    args.push('pull');
    
    return this.executor.execute('docker', args, {
      onStdout: options.onOutput,
      onStderr: options.onError
    });
  }
  
  /**
   * Check if Docker daemon is running
   */
  async isDockerRunning(): Promise<boolean> {
    try {
      const result = await this.executor.execute('docker', ['info']);
      return result.success;
    } catch {
      return false;
    }
  }
}
