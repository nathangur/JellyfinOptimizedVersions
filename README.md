# OptimizedVersions Plugin for Jellyfin

A Jellyfin plugin that creates and manages optimized versions of media files for improved streaming performance.

## Overview

The OptimizedVersions plugin allows Jellyfin users to create optimized versions of their media files. It handles transcoding operations asynchronously and maintains a database of optimization jobs and their statuses.

## Features

- Asynchronous media transcoding
- Job management system (create, cancel, monitor progress)
- Configurable output settings
- Web interface for management
- SQLite database for persistent job tracking

## Components

### Plugin Structure

- **OptimizedVersionsPlugin**: Main plugin class that handles initialization and configuration
- **OptimizedVersionsController**: API controller for handling optimization requests
- **TranscodingService**: Core service managing media transcoding operations
- **OptimizedVersionsDbService**: Database service for job tracking

### Services

#### TranscodingService
Handles all media transcoding operations:
- Manages transcoding queue
- Processes media files using FFmpeg
- Updates job status and progress
- Implements background service pattern

#### OptimizedVersionsDbService
Manages the SQLite database:
- Tracks job statuses
- Stores file paths and metadata
- Maintains job history

### API Endpoints

- `POST /OptimizedVersions/request/{itemId}`: Request new optimization
- `GET /OptimizedVersions/status/{jobId}`: Check job status
- `GET /OptimizedVersions/jobs`: List all jobs
- `DELETE /OptimizedVersions/cancel/{jobId}`: Cancel job

### Major Issues

Error when requesting from an endpoint. I've gone over this repeatitly without any luck so I'm hoping someone else can take a look. 

```System.InvalidOperationException: Unable to resolve service for type 'Nathan.Plugin.OptimizedVersions.Services.ITranscodingService' while attempting to activate 'Nathan.Plugin.OptimizedVersions.Api.OptimizedVersionsController'.```

A lot of little tweaks and changes are needed. The project is pretty messy as well and needs some cleaning up.

## Build:

1. Clone this repository
2. Ensure you have the DotNET SDk installed
3. Build the plugin with: `dotnet build`
4. Copy the build files into a plugin folder called `Nathan.Plugin.OptimizedVersions` in your Jellyfin plugins directory and the .xml file into the configurations folder in the same directory. (I tried making this automated but my computer is mangled. If someone could do that, that would be great)
