# dingoConfig Specification

# dingoConfig Specification

## Project Overview

Build a desktop application using ASP.NET Core for reading/displaying CAN or USB CDC data from multiple devices simultaneously. The application operates locally without internet connectivity and provides a REST API foundation for future frontend development.

### Key Requirements
- Multi-device simultaneous communication support
- Real-time data streaming via SignalR
- Generic device handling through JSON catalog system
- Configuration persistence and management
- Simulation capabilities for testing
- Single executable deployment

## Implementation Milestones

### Milestone 1: Device Catalog Handling
**Goal**: Implement the foundation for generic device support through JSON catalog files

**Deliverables**:
- Create `dingoConfig.contracts` project with catalog DTOs
- Create `dingoConfig.persistence` project with catalog loading
- Implement device catalog JSON schema validation
- Create sample device catalog files for testing
- Implement catalog loading service with error handling
- Create basic REST endpoints for catalog management

**Key Components**:
```csharp
// Core catalog structures
public class DeviceCatalog { }
public class DeviceParameter { }
public class CyclicDataDefinition { }

// Services
public interface IDeviceCatalogService { }
public class DeviceCatalogLoader { }
public class CatalogValidator { }
```

**API Endpoints**:
- `GET /api/catalogs` - List available catalogs
- `GET /api/catalogs/{type}` - Get specific catalog
- `POST /api/catalogs/validate` - Validate catalog file

**Acceptance Criteria**:
- Load multiple catalog files from directory
- Validate catalog JSON structure and content
- Handle missing or corrupted catalog files gracefully
- Provide detailed validation error messages
- Support hot-reloading of catalog changes

---

### Milestone 2: Communication Methods Implementation
**Goal**: Establish working communication with devices via SLCAN, PCAN, and USB CDC

**Deliverables**:
- Create `dingoConfig.application` project with communication managers
- Implement SLCAN serial communication (cross-platform)
- Implement PCAN USB adapter communication (with platform-specific native libraries)
- Implement direct USB CDC communication (cross-platform)
- Create device connection state management
- Implement message framing and parsing for each protocol
- Add cross-platform serial port detection and enumeration

**Key Components**:
```csharp
// Communication interfaces
public interface IDeviceManager { }
public interface ICommunicationChannel { }

// Protocol implementations
public class SlcanManager { }
public class PcanManager { }
public class UsbCdcManager { }

// Message handling
public class MessageParser { }
public class MessageBuilder { }
```

**API Endpoints**:
- `POST /api/devices/{id}/connect` - Connect to device
- `POST /api/devices/{id}/disconnect` - Disconnect from device
- `GET /api/devices/{id}/status` - Get connection status

**Acceptance Criteria**:
- Successfully connect/disconnect from devices via all three protocols on Windows, Linux, and macOS
- Handle connection failures and timeouts gracefully across platforms
- Maintain connection state for multiple devices simultaneously
- Parse and validate incoming messages according to protocol specifications
- Support device enumeration and discovery with platform-appropriate methods
- Load platform-specific native libraries (PCAN) automatically

---

### Milestone 3: Configuration File Management
**Goal**: Implement project configuration saving, loading, and validation

**Deliverables**:
- Design configuration file JSON schema
- Implement configuration serialization/deserialization
- Create configuration validation service
- Implement project creation, save, and load functionality
- Add configuration change tracking and dirty state management

**Key Components**:
```csharp
// Configuration structures
public class ProjectConfiguration { }
public class DeviceConfiguration { }

// Services
public interface IConfigurationService { }
public class ConfigurationValidator { }
public class ConfigurationRepository { }
```

**API Endpoints**:
- `GET /api/configuration` - Get current configuration
- `POST /api/configuration/load` - Load configuration file
- `POST /api/configuration/save` - Save configuration file
- `POST /api/configuration/validate` - Validate configuration

**Acceptance Criteria**:
- Create new project configurations from templates
- Save/load configuration files with full device settings
- Validate configuration against catalog definitions
- Handle configuration versioning and migration
- Support configuration merging and conflict resolution

---

### Milestone 4: Cyclic Data Handling
**Goal**: Implement real-time cyclic data collection and streaming

**Deliverables**:
- Create `dingoConfig.api` project with SignalR hubs
- Implement cyclic data scheduler and manager
- Create data parsing using catalog signal definitions
- Implement SignalR real-time data streaming
- Add data buffering and rate limiting

**Key Components**:
```csharp
// Cyclic data management
public interface ICyclicDataManager { }
public class CyclicScheduler { }
public class DataBuffer { }

// SignalR integration
public class DeviceDataHub : Hub { }
public class CyclicDataService { }
```

**API Endpoints**:
- `POST /api/devices/{id}/cyclic/start` - Start cyclic data collection
- `POST /api/devices/{id}/cyclic/stop` - Stop cyclic data collection
- `GET /api/devices/{id}/cyclic/status` - Get cyclic data status

**SignalR Events**:
- `OnCyclicDataReceived(deviceId, data)` - Real-time data updates
- `OnDataStreamStarted(deviceId)` - Data collection started
- `OnDataStreamStopped(deviceId)` - Data collection stopped

**Acceptance Criteria**:
- Collect cyclic data from multiple devices simultaneously
- Parse raw data using catalog signal definitions
- Stream data to clients via SignalR with configurable intervals
- Handle data buffer overflow and memory management
- Support different data collection rates per device

---

### Milestone 5: Device Settings Read/Write
**Goal**: Implement bidirectional settings communication with devices

**Deliverables**:
- Implement settings read operations from devices
- Implement settings write operations to devices
- Add settings validation against catalog parameters
- Implement burn (NVRAM storage) functionality
- Add bootloader entry capability
- Create settings comparison and change detection

**Key Components**:
```csharp
// Settings management
public interface IDeviceCommunication { }
public class SettingsManager { }
public class ParameterValidator { }

// Device operations
public class DeviceSettings { }
public class SettingsComparer { }
```

**API Endpoints**:
- `GET /api/devices/{id}/settings` - Read settings from device
- `PUT /api/devices/{id}/settings` - Write settings to device
- `POST /api/devices/{id}/burn` - Burn settings to NVRAM
- `POST /api/devices/{id}/bootloader` - Enter bootloader mode

**Acceptance Criteria**:
- Read all device parameters as defined in catalog
- Write individual or bulk parameter changes to device
- Validate parameter values against catalog constraints
- Detect and highlight settings differences between device and configuration
- Handle read/write errors and provide meaningful feedback
- Support atomic settings operations (all-or-nothing writes)

---

### Milestone 6: Simulation Engine
**Goal**: Implement device simulation for testing and development

**Deliverables**:
- Create simulation engine with virtual device support
- Implement simulated responses for all device operations
- Add configurable simulation parameters and behaviors
- Create simulation state management and persistence
- Implement realistic data generation with configurable patterns

**Key Components**:
```csharp
// Simulation framework
public interface ISimulationService { }
public class SimulationEngine { }
public class VirtualDevice { }

// Simulation behaviors
public class SimulationConfig { }
public class DataGenerator { }
```

**API Endpoints**:
- `POST /api/devices/{id}/simulation/start` - Start device simulation
- `POST /api/devices/{id}/simulation/stop` - Stop device simulation
- `PUT /api/devices/{id}/simulation/config` - Update simulation settings
- `POST /api/devices/{id}/simulation/scenario` - Load simulation scenario

**Acceptance Criteria**:
- Simulate all communication protocols (SLCAN, PCAN, USB CDC)
- Generate realistic cyclic data with configurable patterns
- Respond to settings read/write operations with simulated delays
- Support multiple simulation scenarios and test cases
- Allow real-time modification of simulated parameter values
- Provide simulation state persistence across application restarts

---

## Cross-Milestone Requirements

### Testing Strategy (Per Milestone)
- **Unit Tests**: Core functionality for each milestone component
- **Integration Tests**: End-to-end functionality testing
- **Manual Testing**: Verify user-facing features work correctly

### Documentation (Per Milestone)
- **API Documentation**: OpenAPI/Swagger documentation for new endpoints
- **Code Documentation**: XML documentation for public interfaces
- **Usage Examples**: Sample configurations and catalog files

### Error Handling (All Milestones)
- Comprehensive exception handling with meaningful error messages
- Structured logging using Serilog for debugging and monitoring
- Graceful degradation when optional features fail

### Performance Considerations (All Milestones)
- Async/await patterns for all I/O operations
- Efficient memory usage for data buffering and streaming
- Concurrent device communication without blocking

## Milestone Dependencies

```
Milestone 1 (Device Catalogs)
    ↓
Milestone 2 (Communication) ← Milestone 3 (Configuration)
    ↓                              ↓
Milestone 4 (Cyclic Data) ← Milestone 5 (Settings)
    ↓                              ↓
Milestone 6 (Simulation)
```

**Key Dependencies**:
- Milestones 2 & 3 can be developed in parallel after Milestone 1
- Milestone 4 requires completion of Milestones 1 & 2
- Milestone 5 requires completion of Milestones 1, 2 & 3
- Milestone 6 requires completion of all previous milestones

## Success Criteria for Each Milestone

Each milestone should be considered complete when:
1. All deliverables are implemented and tested
2. API endpoints return appropriate responses
3. Error handling covers common failure scenarios
4. Basic unit tests pass for core functionality
5. Integration with previous milestones works correctly
6. Documentation is updated for new features

## Reference Project

- Existing implementation: `DingoConfigurator/`
- Use as reference for functionality, data structures, and communication patterns
- Extract device property definitions from `JsonPropertyName` attributes

## Architecture & Folder Structure

### dingoConfig.api
**Purpose**: REST API layer and SignalR hubs

**Responsibilities**:
- Device management endpoints (CRUD operations)
- Configuration file management (save/load/validate)
- Device communication control (connect/disconnect/read/write)
- Real-time data streaming via SignalR
- Device catalog management

**Key Components**:
- Controllers for device operations
- SignalR hubs for real-time communication
- Middleware for error handling and logging
- API versioning support

### dingoConfig.application
**Purpose**: Business logic and orchestration

**Responsibilities**:
- Device communication protocols (SLCAN, PCAN, USB CDC)
- Message parsing and building using DBC-like definitions
- Cyclic data handling and scheduling
- Device state management
- Simulation engine
- Configuration validation and transformation

**Key Components**:
- Device managers for each communication type
- Message parsers and builders
- Scheduler for cyclic operations
- State machines for device lifecycle
- Simulation services

### dingoConfig.contracts
**Purpose**: Shared data contracts and definitions

**Responsibilities**:
- DTOs for API communication
- Device catalog schema definitions
- Configuration file schema
- Enums for device states, communication types
- Event definitions for SignalR

**Key Components**:
- Device DTOs (DeviceInfo, DeviceSettings, DeviceData)
- Configuration DTOs (ProjectConfiguration, DeviceConfiguration)
- Communication DTOs (MessageFrame, DataPoint)
- Enums (DeviceState, CommunicationType, MessageType)

### dingoConfig.persistence
**Purpose**: Data persistence and file management

**Responsibilities**:
- JSON configuration file I/O
- Device catalog loading and validation
- Settings persistence
- File system operations
- Data serialization/deserialization

**Key Components**:
- Configuration repository
- Catalog loader and validator
- File system abstraction
- JSON serialization services

## Technology Stack

### Core Framework
- **ASP.NET Core 8.0+**: Web API and dependency injection
- **SignalR**: Real-time communication
- **System.Text.Json**: JSON serialization
- **Microsoft.Extensions.Hosting**: Background services

### Communication Libraries
- **System.IO.Ports**: Serial communication for SLCAN (cross-platform)
- **PCAN-Basic .NET**: Cross-platform PCAN support via P/Invoke to native libraries
- **LibUsbDotNet**: Cross-platform USB CDC communication
- **SerialPortStream**: Alternative cross-platform serial library (if needed)

### Additional Dependencies
- **Serilog**: Structured logging
- **FluentValidation**: Input validation
- **AutoMapper**: Object mapping
- **Microsoft.Extensions.Configuration**: Configuration management
- **Octokit**: GitHub API integration for update checking
- **System.IO.Compression**: Archive extraction for updates

## Communication Protocols

### SLCAN (Serial CAN)
```csharp
// Configuration structure
public class SlcanConfiguration
{
    public string PortName { get; set; }
    public int BaudRate { get; set; } = 115200;
    public string CanBitrate { get; set; } = "500K";
}

// Message format: Tiiildddddddddddddddd
// T = Extended frame, t = Standard frame
// iii/iiiiiiii = CAN ID (hex)
// l = Data length (0-8)
// dd = Data bytes (hex)
```

### PCAN USB
```csharp
public class PcanConfiguration
{
    public PcanChannel Channel { get; set; }
    public PcanBaudrate Baudrate { get; set; } = PcanBaudrate.PCAN_BAUD_500K;
    public bool UseExtendedFrames { get; set; } = true;
    public string LibraryPath { get; set; } // Platform-specific PCAN library path
}

// Cross-platform PCAN implementation using P/Invoke
public class CrossPlatformPcanManager
{
    // Load appropriate native library based on platform
    // Windows: PCANBasic.dll
    // Linux: libpcanbasic.so
    // macOS: libpcanbasic.dylib
}
```

### USB CDC Direct
```csharp
public class UsbCdcConfiguration
{
    public string DeviceId { get; set; }
    public int VendorId { get; set; }
    public int ProductId { get; set; }
    public string SerialNumber { get; set; }
    public int BaudRate { get; set; } = 115200;
}
```

## Device Control Operations

### Connection Management
```csharp
public interface IDeviceManager
{
    Task<bool> ConnectAsync(string deviceId);
    Task<bool> DisconnectAsync(string deviceId);
    Task<DeviceStatus> GetStatusAsync(string deviceId);
    event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;
}
```

### Acyclic Communication
```csharp
public interface IDeviceCommunication
{
    // Read settings from device to application
    Task<DeviceSettings> ReadSettingsAsync(string deviceId);
    
    // Write settings from application to device RAM
    Task<bool> WriteSettingsAsync(string deviceId, DeviceSettings settings);
    
    // Store settings in device non-volatile memory
    Task<bool> BurnSettingsAsync(string deviceId);
    
    // Enter firmware update mode
    Task<bool> EnterBootloaderAsync(string deviceId);
}
```

### Cyclic Communication
```csharp
public interface ICyclicDataManager
{
    // Receive cyclic data from device
    void StartReceiving(string deviceId, TimeSpan interval);
    void StopReceiving(string deviceId);
    event EventHandler<CyclicDataReceivedEventArgs> DataReceived;
    
    // Send cyclic data to device (simulation)
    void StartSending(string deviceId, IEnumerable<CyclicMessage> messages);
    void StopSending(string deviceId);
}
```

## Device Settings Schema

### Settings Structure
```csharp
public class DeviceSettings
{
    public string DeviceId { get; set; }
    public string DeviceType { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsModified { get; set; }
}

public class SettingParameter
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public ParameterType Type { get; set; }
    public object Value { get; set; }
    public object DefaultValue { get; set; }
    public object MinValue { get; set; }
    public object MaxValue { get; set; }
    public string Units { get; set; }
    public string Description { get; set; }
    public bool IsReadOnly { get; set; }
    public string Category { get; set; }
}
```

## Device Catalog System

### Catalog File Structure
```json
{
  "deviceType": "ExampleDevice",
  "version": "1.0.0",
  "manufacturer": "Manufacturer Name",
  "description": "Device description",
  "communication": {
    "type": "CAN",
    "baseId": "0x600",
    "extendedFrames": true
  },
  "parameters": [
    {
      "name": "motorSpeed",
      "displayName": "Motor Speed",
      "type": "uint16",
      "address": "0x601",
      "subIndex": 1,
      "byteOrder": "littleEndian",
      "scaling": {
        "factor": 0.1,
        "offset": 0,
        "units": "RPM"
      },
      "limits": {
        "min": 0,
        "max": 10000
      },
      "defaultValue": 1000,
      "category": "Motor Control",
      "description": "Target motor speed in RPM"
    }
  ],
  "cyclicData": [
    {
      "name": "statusFrame",
      "id": "0x580",
      "interval": 100,
      "signals": [
        {
          "name": "currentSpeed",
          "startBit": 0,
          "length": 16,
          "byteOrder": "littleEndian",
          "type": "uint16",
          "scaling": {
            "factor": 0.1,
            "units": "RPM"
          }
        }
      ]
    }
  ]
}
```

### Catalog Loading
```csharp
public interface IDeviceCatalogService
{
    Task LoadCatalogsAsync(string catalogDirectory);
    DeviceCatalog GetCatalog(string deviceType);
    IEnumerable<string> GetAvailableDeviceTypes();
    Task<ValidationResult> ValidateCatalogAsync(string catalogPath);
}
```

## Configuration File Management

### Configuration Schema
```json
{
  "projectInfo": {
    "name": "Example Project",
    "version": "1.0.0",
    "created": "2025-06-30T00:00:00Z",
    "lastModified": "2025-06-30T00:00:00Z",
    "description": "Project description"
  },
  "devices": [
    {
      "id": "device_001",
      "name": "Motor Controller 1",
      "type": "ExampleDevice",
      "communication": {
        "type": "SLCAN",
        "config": {
          "portName": "COM3",
          "baudRate": 115200,
          "canBitrate": "500K"
        }
      },
      "settings": {
        "motorSpeed": 1500,
        "enableMotor": true
      },
      "cyclicMessages": [
        {
          "name": "statusRequest",
          "interval": 100,
          "enabled": true
        }
      ],
      "simulation": {
        "enabled": false,
        "responseDelay": 10
      }
    }
  ]
}
```

### Configuration Service
```csharp
public interface IConfigurationService
{
    Task<ProjectConfiguration> LoadAsync(string filePath);
    Task SaveAsync(ProjectConfiguration config, string filePath);
    Task<ValidationResult> ValidateAsync(ProjectConfiguration config);
    ProjectConfiguration CreateNew(string projectName);
}
```

## REST API Endpoints

### Device Management
```
GET    /api/devices                    - List all configured devices
GET    /api/devices/{id}               - Get device details
POST   /api/devices                    - Add new device
PUT    /api/devices/{id}               - Update device configuration
DELETE /api/devices/{id}               - Remove device

POST   /api/devices/{id}/connect       - Connect to device
POST   /api/devices/{id}/disconnect    - Disconnect from device
GET    /api/devices/{id}/status        - Get connection status
```

### Device Communication
```
GET    /api/devices/{id}/settings      - Read settings from device
PUT    /api/devices/{id}/settings      - Write settings to device
POST   /api/devices/{id}/burn          - Burn settings to NVRAM
POST   /api/devices/{id}/bootloader    - Enter bootloader mode

POST   /api/devices/{id}/cyclic/start  - Start cyclic communication
POST   /api/devices/{id}/cyclic/stop   - Stop cyclic communication
```

### Configuration Management
```
GET    /api/configuration              - Get current configuration
POST   /api/configuration/load         - Load configuration file
POST   /api/configuration/save         - Save configuration file
POST   /api/configuration/validate     - Validate configuration

### Application Management
```
GET    /api/app/version                - Get current application version
GET    /api/app/updates/check          - Check for available updates
GET    /api/app/updates/download       - Download available update
POST   /api/app/updates/install        - Install downloaded update
GET    /api/app/updates/status         - Get update process status
```

## SignalR Hubs

### Real-time Communication
```csharp
public class DeviceDataHub : Hub
{
    // Client methods to implement
    // - OnDeviceConnected(deviceId, status)
    // - OnDeviceDisconnected(deviceId)
    // - OnCyclicDataReceived(deviceId, data)
    // - OnDeviceError(deviceId, error)
    // - OnSettingsChanged(deviceId, settings)
}
```

### Hub Usage
```javascript
// Client-side SignalR connection
connection.on("OnCyclicDataReceived", (deviceId, data) => {
    updateDeviceDisplay(deviceId, data);
});

connection.on("OnDeviceConnected", (deviceId, status) => {
    updateDeviceStatus(deviceId, "Connected");
});

connection.on("OnUpdateProgress", (progress) => {
    updateDownloadProgress(progress);
});

connection.on("OnUpdateAvailable", (updateInfo) => {
    showUpdateNotification(updateInfo);
});
```

## Application Update System

### Update Configuration
```csharp
public class UpdateConfiguration
{
    public string GitHubRepository { get; set; } = "owner/dingoConfig";
    public string GitHubToken { get; set; } // Optional for private repos
    public bool CheckForPreReleases { get; set; } = false;
    public TimeSpan UpdateCheckInterval { get; set; } = TimeSpan.FromDays(1);
    public bool AutoCheckEnabled { get; set; } = true;
    public string UpdateDirectory { get; set; } = "updates";
}
```

### Update Service Interface
```csharp
public interface IUpdateService
{
    Task<UpdateInfo> CheckForUpdatesAsync();
    Task<DownloadResult> DownloadUpdateAsync(string version, IProgress<DownloadProgress> progress = null);
    Task<bool> InstallUpdateAsync(string downloadPath);
    Task<string> GetCurrentVersionAsync();
    Task<bool> IsUpdateAvailableAsync();
    event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;
}

public class UpdateInfo
{
    public string Version { get; set; }
    public string ReleaseNotes { get; set; }
    public DateTime PublishedAt { get; set; }
    public string DownloadUrl { get; set; }
    public long FileSize { get; set; }
    public bool IsPreRelease { get; set; }
    public bool IsCurrentVersion { get; set; }
}

public class DownloadProgress
{
    public long BytesReceived { get; set; }
    public long TotalBytes { get; set; }
    public double ProgressPercentage => TotalBytes > 0 ? (double)BytesReceived / TotalBytes * 100 : 0;
    public TimeSpan Elapsed { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
}

public class DownloadResult
{
    public bool Success { get; set; }
    public string LocalPath { get; set; }
    public string Error { get; set; }
    public string Version { get; set; }
}
```

### GitHub Integration
```csharp
public class GitHubUpdateProvider
{
    private readonly GitHubClient _githubClient;
    private readonly UpdateConfiguration _config;

    public async Task<UpdateInfo> GetLatestReleaseAsync()
    {
        var release = await _githubClient.Repository.Release
            .GetLatest(_config.GitHubRepository.Split('/')[0], 
                      _config.GitHubRepository.Split('/')[1]);
        
        return new UpdateInfo
        {
            Version = release.TagName,
            ReleaseNotes = release.Body,
            PublishedAt = release.PublishedAt.DateTime,
            DownloadUrl = release.Assets.FirstOrDefault()?.BrowserDownloadUrl,
            FileSize = release.Assets.FirstOrDefault()?.Size ?? 0,
            IsPreRelease = release.Prerelease
        };
    }

    public async Task<Stream> DownloadReleaseAsync(string downloadUrl, 
        IProgress<DownloadProgress> progress = null)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var buffer = new byte[8192];
        var bytesReceived = 0L;
        var stopwatch = Stopwatch.StartNew();

        using var contentStream = await response.Content.ReadAsStreamAsync();
        var memoryStream = new MemoryStream();

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await memoryStream.WriteAsync(buffer, 0, bytesRead);
            bytesReceived += bytesRead;

            progress?.Report(new DownloadProgress
            {
                BytesReceived = bytesReceived,
                TotalBytes = totalBytes,
                Elapsed = stopwatch.Elapsed,
                EstimatedTimeRemaining = CalculateETA(bytesReceived, totalBytes, stopwatch.Elapsed)
            });
        }

        memoryStream.Position = 0;
        return memoryStream;
    }
}
```

### Update Installation Process
```csharp
public class UpdateInstaller
{
    public async Task<bool> InstallUpdateAsync(string updateFilePath, string currentExecutablePath)
    {
        try
        {
            // 1. Verify update file integrity
            if (!await ValidateUpdateFileAsync(updateFilePath))
                return false;

            // 2. Create backup of current version
            var backupPath = CreateBackup(currentExecutablePath);

            // 3. Extract update to temporary location
            var tempDir = Path.Combine(Path.GetTempPath(), "dingoConfig_update");
            await ExtractUpdateAsync(updateFilePath, tempDir);

            // 4. Create update script
            var updateScript = CreateUpdateScript(tempDir, currentExecutablePath, backupPath);

            // 5. Launch update script and exit current application
            Process.Start(new ProcessStartInfo
            {
                FileName = updateScript,
                UseShellExecute = true,
                CreateNoWindow = true
            });

            // Signal application to shutdown
            return true;
        }
        catch (Exception ex)
        {
            // Log error and cleanup
            return false;
        }
    }

    private string CreateUpdateScript(string sourceDir, string targetPath, string backupPath)
    {
        string scriptPath, scriptContent;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            scriptPath = Path.Combine(Path.GetTempPath(), "dingoConfig_updater.bat");
            scriptContent = $@"
@echo off
echo Updating dingoConfig...
timeout /t 3 /nobreak > nul

echo Copying new files...
copy ""{Path.Combine(sourceDir, "dingoConfig.exe")}"" ""{targetPath}"" /Y

if %ERRORLEVEL% NEQ 0 (
    echo Update failed, restoring backup...
    copy ""{backupPath}"" ""{targetPath}"" /Y
    echo Update failed. Original version restored.
    pause
    exit /b 1
)

echo Cleaning up...
rmdir /s /q ""{sourceDir}""
del ""{backupPath}""
del ""{scriptPath}""

echo Update completed successfully!
echo Restarting application...
start """" ""{targetPath}""
exit /b 0
";
        }
        else
        {
            scriptPath = Path.Combine(Path.GetTempPath(), "dingoConfig_updater.sh");
            var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "dingoConfig" : "dingoConfig";
            var sourceFile = Path.Combine(sourceDir, executableName);
            
            scriptContent = $@"#!/bin/bash
echo ""Updating dingoConfig...""
sleep 3

echo ""Copying new files...""
cp ""{sourceFile}"" ""{targetPath}""

if [ $? -ne 0 ]; then
    echo ""Update failed, restoring backup...""
    cp ""{backupPath}"" ""{targetPath}""
    echo ""Update failed. Original version restored.""
    read -p ""Press any key to continue...""
    exit 1
fi

echo ""Setting permissions...""
chmod +x ""{targetPath}""

echo ""Cleaning up...""
rm -rf ""{sourceDir}""
rm ""{backupPath}""
rm ""{scriptPath}""

echo ""Update completed successfully!""
echo ""Restarting application...""
""{targetPath}"" &
exit 0
";
        }

        File.WriteAllText(scriptPath, scriptContent);
        
        // Make script executable on Unix systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var chmod = Process.Start("chmod", $"+x {scriptPath}");
            chmod.WaitForExit();
        }
        
        return scriptPath;
    }
}
```

### Version Management
```csharp
public class VersionService
{
    public string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                     ?? assembly.GetName().Version?.ToString()
                     ?? "1.0.0";
        
        return version;
    }

    public bool IsNewerVersion(string currentVersion, string availableVersion)
    {
        if (Version.TryParse(currentVersion.Split('-')[0], out var current) &&
            Version.TryParse(availableVersion.Split('-')[0], out var available))
        {
            return available > current;
        }
        
        return string.Compare(availableVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
    }
}
```

### Update UI Integration
```csharp
// SignalR Hub additions
public class DeviceDataHub : Hub
{
    // Existing methods...
    
    // Update-related client methods
    // - OnUpdateAvailable(updateInfo)
    // - OnUpdateDownloadProgress(progress)
    // - OnUpdateDownloadComplete(result)
    // - OnUpdateInstallStarted()
    // - OnUpdateError(error)
}

// Controller for update management
[ApiController]
[Route("api/app")]
public class ApplicationController : ControllerBase
{
    private readonly IUpdateService _updateService;
    private readonly IHubContext<DeviceDataHub> _hubContext;

    [HttpGet("version")]
    public async Task<IActionResult> GetVersion()
    {
        var version = await _updateService.GetCurrentVersionAsync();
        return Ok(new { version, buildDate = GetBuildDate() });
    }

    [HttpGet("updates/check")]
    public async Task<IActionResult> CheckForUpdates()
    {
        var updateInfo = await _updateService.CheckForUpdatesAsync();
        return Ok(updateInfo);
    }

    [HttpPost("updates/download")]
    public async Task<IActionResult> DownloadUpdate([FromBody] string version)
    {
        var progress = new Progress<DownloadProgress>(p =>
        {
            _hubContext.Clients.All.SendAsync("OnUpdateDownloadProgress", p);
        });

        var result = await _updateService.DownloadUpdateAsync(version, progress);
        
        if (result.Success)
        {
            await _hubContext.Clients.All.SendAsync("OnUpdateDownloadComplete", result);
        }
        
        return Ok(result);
    }

    [HttpPost("updates/install")]
    public async Task<IActionResult> InstallUpdate([FromBody] string downloadPath)
    {
        await _hubContext.Clients.All.SendAsync("OnUpdateInstallStarted");
        
        var success = await _updateService.InstallUpdateAsync(downloadPath);
        
        if (success)
        {
            // Application will exit for update installation
            return Ok(new { message = "Update installation started. Application will restart." });
        }
        
        return BadRequest(new { message = "Update installation failed." });
    }
}
```

## Simulation Engine

### Simulation Configuration
```csharp
public class SimulationConfig
{
    public bool Enabled { get; set; }
    public int ResponseDelayMs { get; set; } = 10;
    public Dictionary<string, object> SimulatedValues { get; set; }
    public bool RandomizeValues { get; set; } = false;
    public int RandomSeed { get; set; }
}
```

### Simulation Service
```csharp
public interface ISimulationService
{
    Task StartSimulation(string deviceId, SimulationConfig config);
    Task StopSimulation(string deviceId);
    Task UpdateSimulatedValue(string deviceId, string parameter, object value);
    bool IsSimulating(string deviceId);
}
```

## Error Handling & Logging

### Error Response Format
```csharp
public class ApiErrorResponse
{
    public string Error { get; set; }
    public string Message { get; set; }
    public string TraceId { get; set; }
    public Dictionary<string, string[]> ValidationErrors { get; set; }
}
```

### Logging Categories
- `dingoConfig.Communication` - Device communication events
- `dingoConfig.Configuration` - Configuration loading/saving
- `dingoConfig.Simulation` - Simulation engine events
- `dingoConfig.Api` - API request/response logging
- `dingoConfig.Updates` - Update checking and installation events

## Testing Strategy

### Unit Tests
- Device communication protocols
- Message parsing and building
- Configuration validation
- Catalog loading
- Update checking and version comparison
- GitHub API integration

### Integration Tests
- End-to-end device communication
- SignalR hub functionality
- File I/O operations
- API endpoint testing
- Update download and installation process
- GitHub release API integration

### Simulation Tests
- All device types should be testable via simulation
- Automated test scenarios for common operations
- Performance testing with multiple simulated devices

## Deployment Requirements

### Single Executable
- Self-contained deployment
- Include all dependencies
- Embedded device catalogs
- Configuration templates

### System Requirements
- Windows 10/11 (primary target)
- .NET 8.0 Runtime (self-contained)
- USB device drivers (PCAN, CDC)
- No internet connectivity required

### Installation
- Single executable file per platform
- Portable application (no installer required)
- Configuration directory auto-creation
- Default catalog files included
- Update directory for downloaded updates
- Backup directory for rollback capability
- Platform-specific executable names and permissions

## Performance Considerations

### Concurrent Operations
- Support 10+ simultaneous device connections
- Non-blocking async operations
- Proper resource disposal
- Thread-safe collections

### Memory Management
- Efficient message buffering
- Cyclic data streaming optimization
- Large configuration file handling
- Device catalog caching

### Real-time Requirements
- Sub-100ms cyclic message processing
- Responsive UI updates via SignalR
- Minimal latency for device commands
- Efficient data serialization

## Security Considerations

### Input Validation
- Strict validation of all API inputs
- Configuration file schema validation
- Device parameter bounds checking
- File path sanitization

### Device Communication
- Timeout handling for all operations
- Rate limiting for device commands
- Error recovery mechanisms
- Connection state management

### Update Security
- Cryptographic signature verification for updates
- HTTPS-only download connections
- Backup creation before updates
- Rollback capability on failed updates
- User confirmation required for installations

## Release and Update Process

### GitHub Release Structure
```
dingoConfig-v1.2.3/
├── win-x64/
│   ├── dingoConfig.exe              # Windows executable
│   └── PCANBasic.dll               # Windows PCAN library
├── linux-x64/
│   ├── dingoConfig                 # Linux executable
│   └── libpcanbasic.so            # Linux PCAN library
├── osx-x64/
│   ├── dingoConfig                 # macOS executable
│   └── libpcanbasic.dylib         # macOS PCAN library
├── catalogs/                       # Shared device catalogs
│   ├── ExampleDevice.json
│   └── MotorController.json
├── CHANGELOG.md                    # Release notes
└── README.md                       # Installation instructions
```

### Release Naming Convention
- **Stable releases**: `v1.2.3`
- **Pre-releases**: `v1.2.3-beta.1`, `v1.2.3-alpha.2`
- **GitHub release tags**: Same as version numbers
- **Asset naming**: 
  - `dingoConfig-v1.2.3-win-x64.zip`
  - `dingoConfig-v1.2.3-linux-x64.tar.gz`
  - `dingoConfig-v1.2.3-osx-x64.tar.gz`

### Update Deployment Process
1. **Version Detection**: Compare current version with GitHub releases
2. **User Notification**: Display update availability with release notes
3. **Manual Download**: User-initiated download with progress tracking
4. **Backup Creation**: Automatic backup of current version
5. **Installation**: Replace executable with new version
6. **Verification**: Confirm successful update and cleanup
7. **Rollback**: Restore backup if update fails

### Configuration for Updates
```json
{
  "updateSettings": {
    "githubRepository": "owner/dingoConfig",
    "checkForPreReleases": false,
    "autoCheckEnabled": true,
    "checkInterval": "1.00:00:00",
    "downloadDirectory": "./updates",
    "backupDirectory": "./backups",
    "maxBackupsToKeep": 3,
    "platformSpecific": {
      "currentPlatform": "auto-detect",
      "supportedPlatforms": ["win-x64", "linux-x64", "osx-x64"]
    }
  }
}
```

### Update Process Flow
1. **Background Check**: Periodic check for new releases (configurable interval)
2. **User Notification**: Non-intrusive notification of available updates
3. **Manual Trigger**: User can manually check for updates via API/UI
4. **Download Phase**: Progress-tracked download with pause/resume capability
5. **Installation Phase**: Graceful shutdown → update → restart
6. **Verification**: Post-update verification and cleanup
