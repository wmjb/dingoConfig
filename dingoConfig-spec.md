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

## Use Cases

- User opens the application, nothing is loaded
- User can either load a configuration file or add devices from the catalog to the current configuration
- When adding a device from the catalog, a unique base CAN ID needs to be set
- The CAN IDs in the device catalogs should be offsets to the base ID as multiple devices of the same catalog type can be added to the same configuration
- Each device claims a 32-ID range (BaseId to BaseId+31) matching the reference project pattern
- After adding devices or loading an existing configuration, the configuration can be saved
- With devices added to the configuration, a single comms type can be selected and connected
- The comms types are SLCAN, PCAN or USB CDC. Only one comms can be connected at once. 
- When messages are received by the comms, a comms manager will decide which device the message belongs to
- The comms and comms manager shouldn't parse or modify the data beyond getting it in a generic CAN message format
- When the devices receive the message from the comms manager they should parse it according to their catalog DBC format

## Device ID Management

Device IDs are generated at runtime using array index position for simplicity:

```csharp
public class DeviceConfiguration
{
    public string Id { get; set; } // Generated as "device_{index}" at runtime
    public string CatalogType { get; set; }
    public uint BaseCanId { get; set; }
    public string Name { get; set; }
    // ... other properties
}

// Runtime ID generation
public string GenerateDeviceId(int arrayIndex) => $"device_{arrayIndex}";
```

Configuration files store only meaningful data without IDs:

```json
{
  "devices": [
    {
      "catalogType": "ExampleDevice",
      "baseCanId": "0x100",
      "name": "Motor Controller 1"
    },
    {
      "catalogType": "ExampleDevice", 
      "baseCanId": "0x200",
      "name": "Motor Controller 2"
    }
  ]
}
```

## Implementation Milestones

### Milestone 1: Device Catalog Handling
**Goal**: Implement the foundation for generic device support through JSON catalog files with offset-based CAN ID system

**Deliverables**:
- Create `dingoConfig.contracts` project with catalog DTOs
- Create `dingoConfig.persistence` project with catalog loading
- Implement device catalog JSON schema validation with offset-based CAN IDs
- Create sample device catalog files for testing
- Implement catalog loading service with error handling
- Create basic REST endpoints for catalog management

**Key Components**:
```csharp
// Core catalog structures
public class DeviceCatalog 
{
    public string DeviceType { get; set; }
    public string DisplayName { get; set; }
    public uint BaseCanIdOffset { get; set; } = 0; // Always 0 - user sets the base ID
    public uint CanIdRange { get; set; } = 32; // Each device claims 32 IDs (0-31 offsets)
    public Dictionary<string, uint> MessageOffsets { get; set; } // Message-specific offsets (0-31)
    public List<DeviceParameter> Parameters { get; set; }
    public List<CyclicDataDefinition> CyclicMessages { get; set; }
    public Dictionary<string, MessageDefinition> Messages { get; set; } // DBC-like message definitions
}

public class DeviceParameter 
{
    public string Name { get; set; }
    public uint CanIdOffset { get; set; } // Offset from device base ID (typically 30 for settings)
    public MessagePrefix MessagePrefix { get; set; } // Category for parameter grouping
    public int SubIndex { get; set; }
    public ParameterType Type { get; set; }
    public object DefaultValue { get; set; }
    public object MinValue { get; set; }
    public object MaxValue { get; set; }
    public string Units { get; set; }
    public string ValidationRule { get; set; } // Reference to validation logic
    // ... other parameter properties
}

// MessagePrefix system from reference project
public enum MessagePrefix
{
    Can = 1,           // CAN bus settings
    Inputs = 5,        // Digital input configuration  
    Outputs = 10,      // Output channel settings
    VirtualInputs = 15, // Software-defined inputs
    CanInputs = 35,    // CAN-based input mapping
    Keypad = 50,       // Keypad/button configuration
    Version = 120,     // Firmware version
    BurnSettings = 127 // Non-volatile save
}

public class CyclicDataDefinition 
{
    public string Name { get; set; }
    public uint CanIdOffset { get; set; } // Offset from device base ID (0-31)
    public int IntervalMs { get; set; }
    public List<SignalDefinition> Signals { get; set; }
    public bool IsRealTimeData { get; set; } = true;
}

// Signal definition for DBC-like parsing
public class SignalDefinition
{
    public string Name { get; set; }
    public int StartBit { get; set; }
    public int Length { get; set; }
    public string ByteOrder { get; set; } = "littleEndian";
    public string DataType { get; set; }
    public ScalingInfo Scaling { get; set; }
    public bool IsSigned { get; set; }
}

public class ScalingInfo
{
    public double Factor { get; set; } = 1.0;
    public double Offset { get; set; } = 0.0;
    public string Units { get; set; }
}

// Services
public interface IDeviceCatalogService 
{
    Task LoadCatalogsAsync(string catalogDirectory);
    DeviceCatalog GetCatalog(string deviceType);
    IEnumerable<string> GetAvailableDeviceTypes();
    Task<ValidationResult> ValidateCatalogAsync(string catalogPath);
}

public class DeviceCatalogLoader { }
public class CatalogValidator 
{
    // Validation rules from reference project:
    public ValidationResult ValidateCurrentLimit(float value); // 0-30 amps
    public ValidationResult ValidateResetTime(float value); // 0-25.5 seconds
    public ValidationResult ValidateFrequency(int value); // 1-400 Hz
    public ValidationResult ValidateDutyCycle(int value); // 1-100%
    public ValidationResult ValidateSoftStartTime(int value); // 1-2000ms
    public ValidationResult ValidateBaseId(uint value); // 1-2046 (11-bit CAN ID)
    public ValidationResult ValidateCanIdRangeConflicts(List<DeviceConfiguration> devices);
}
```

**API Endpoints**:
- `GET /api/catalogs` - List available catalogs
- `GET /api/catalogs/{type}` - Get specific catalog
- `POST /api/catalogs/validate` - Validate catalog file

**Acceptance Criteria**:
- Load multiple catalog files from directory
- Validate catalog JSON structure with offset-based CAN ID definitions
- Handle missing or corrupted catalog files gracefully
- Provide detailed validation error messages
- Support hot-reloading of catalog changes

---

### Milestone 2: Communication Methods Implementation
**Goal**: Establish working communication with single comms selection and message routing

**Deliverables**:
- Create `dingoConfig.application` project with a comms manager
- Implement SLCAN serial communication (USB serial) (cross-platform)
- Implement PCAN USB adapter communication (with platform-specific native libraries)
- Implement direct USB CDC communication (USB serial) (cross-platform)
- Create comms connection state management with single active connection
- Implement comms manager for message routing to appropriate devices
- Add cross-platform serial port detection and enumeration
- Only one comms should be active at a time

**Key Components**:
```csharp
// Individual comms interfaces
public interface ICommsInterface : IDisposable
{
    bool Init(string port, CanInterfaceBaudRate baud);
    bool Start();
    bool Stop();
    bool Write(CanInterfaceData canData);
    DataReceivedHandler DataReceived { get; set; }
    int RxTimeDelta { get; }
}

// Main comms manager - routes messages between single comms and multiple devices
public interface ICommsManager
{
    // Only one comms can be active
    Task<bool> SetActiveCommsAsync(CommunicationType type, object config);
    Task<bool> DisconnectAllAsync();
    CommunicationType? ActiveCommsType { get; }
    bool IsConnected { get; }
    
    // Message routing
    void RouteIncomingMessage(CanMessage message);
    Task<bool> SendMessageFromDeviceAsync(string deviceId, CanMessage message);
    
    // Device registration for message routing (32-ID range per device)
    void RegisterDevice(string deviceId, uint baseCanId, uint canIdRange = 32);
    void UnregisterDevice(string deviceId);
    
    event EventHandler<MessageReceivedEventArgs> MessageReceived;
    event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
}

public class MessageReceivedEventArgs
{
    public string DeviceId { get; set; } // Determined by comms manager
    public CanMessage Message { get; set; } // Raw CAN message
}

public enum CommunicationType
{
    SLCAN,
    PCAN,
    USBCDC
}
```

**API Endpoints**:
- `GET /api/comms/types` - List available comms types
- `POST /api/comms/connect` - Connect single comms type
- `POST /api/comms/disconnect` - Disconnect current comms
- `GET /api/comms/status` - Get current comms status
- `GET /api/comms/ports` - Get available ports for selected comms type

**Acceptance Criteria**:
- Successfully connect/disconnect from single comms interface via all three protocols on Windows, Linux, and macOS
- Handle connection failures and timeouts gracefully across platforms
- Route incoming messages to correct devices based on CAN ID ranges
- Parse and validate incoming messages according to protocol specifications
- Support device enumeration and discovery with platform-appropriate methods
- Load platform-specific native libraries (PCAN) automatically
- Enforce single active comms connection at a time

---

### Milestone 3: Configuration File Management
**Goal**: Implement project configuration saving, loading, and validation with array-index device IDs

**Deliverables**:
- Design configuration file JSON schema without stored device IDs
- Implement configuration serialization/deserialization with runtime ID generation
- Create configuration validation service
- Implement project creation, save, and load functionality
- Add configuration change tracking and dirty state management
- Implement device addition from catalog with base CAN ID assignment

**Key Components**:
```csharp
// Configuration structures (runtime objects)
public class ProjectConfiguration 
{
    public ProjectInfo ProjectInfo { get; set; }
    public CommunicationSettings CommunicationSettings { get; set; }
    public List<DeviceConfiguration> Devices { get; set; }
}

public class DeviceConfiguration
{
    public string Id { get; set; } // Generated at runtime as "device_{index}"
    public string CatalogType { get; set; }
    public uint BaseCanId { get; set; } // User-defined base ID
    public string Name { get; set; }
    public Dictionary<string, object> Settings { get; set; }
    public List<CyclicMessageConfig> CyclicMessages { get; set; }
    public SimulationConfig Simulation { get; set; }
    
    // Calculated properties
    public uint CalculatedCanId(uint offset) => BaseCanId + offset;
}

// Serialization structures (for JSON files)
public class ProjectConfigurationData
{
    public ProjectInfo ProjectInfo { get; set; }
    public CommunicationSettings CommunicationSettings { get; set; }
    public List<DeviceConfigurationData> Devices { get; set; } // No IDs stored
}

public class DeviceConfigurationData
{
    public string CatalogType { get; set; }
    public uint BaseCanId { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object> Settings { get; set; }
    public List<CyclicMessageConfig> CyclicMessages { get; set; }
    public SimulationConfig Simulation { get; set; }
}

public class CommunicationSettings
{
    public CommunicationType? ActiveType { get; set; }
    public SlcanConfiguration SlcanConfig { get; set; }
    public PcanConfiguration PcanConfig { get; set; }
    public UsbCdcConfiguration UsbCdcConfig { get; set; }
}

// Services
public interface IConfigurationService 
{
    Task<ProjectConfiguration> LoadAsync(string filePath);
    Task SaveAsync(ProjectConfiguration config, string filePath);
    Task<ValidationResult> ValidateAsync(ProjectConfiguration config);
    ProjectConfiguration CreateNew(string projectName);
    DeviceConfiguration AddDeviceFromCatalog(string catalogType, uint baseCanId, string customName = null);
}

public class ConfigurationValidator 
{
    // Comprehensive validation matching reference project patterns
    public ValidationResult ValidateDeviceConfiguration(DeviceConfiguration device, DeviceCatalog catalog);
    public ValidationResult ValidateParameterValue(string parameterName, object value, DeviceParameter parameterDef);
    public ValidationResult ValidateCanIdConflicts(List<DeviceConfiguration> devices);
    public ValidationResult ValidateVarMapReferences(DeviceConfiguration device); // 327-value enum validation
    public ValidationResult ValidateOutputConfiguration(OutputConfiguration output);
    public ValidationResult ValidateInputConfiguration(InputConfiguration input);
    
    // Device-specific validation rules
    public ValidationResult ValidateDingoPdmConfiguration(DeviceConfiguration device);
    public ValidationResult ValidateCanBoardConfiguration(DeviceConfiguration device);
}
public class ConfigurationRepository { }
```

**API Endpoints**:
- `GET /api/configuration` - Get current configuration
- `GET /api/configuration/new` - Create new empty configuration
- `POST /api/configuration/load` - Load configuration file
- `POST /api/configuration/save` - Save configuration file
- `POST /api/configuration/validate` - Validate configuration
- `POST /api/devices/add-from-catalog` - Add device from catalog with base CAN ID
- `DELETE /api/devices/{deviceId}` - Remove device from configuration
- `PUT /api/devices/{deviceId}` - Update device configuration

**Acceptance Criteria**:
- Create new project configurations from templates
- Save/load configuration files with clean JSON structure (no stored device IDs)
- Generate runtime device IDs using array index
- Validate configuration against catalog definitions including CAN ID conflicts
- Handle configuration versioning and migration
- Support adding devices from catalog with user-specified base CAN IDs
- Detect and prevent CAN ID range conflicts between devices

---

### Milestone 4: Cyclic Data Handling
**Goal**: Implement real-time cyclic data collection and streaming

**Deliverables**:
- Create `dingoConfig.api` project with SignalR hubs
- Implement cyclic data scheduler and manager
- Create data parsing using catalog signal definitions with offset-based CAN IDs
- Implement SignalR real-time data streaming
- Add data buffering and rate limiting

**Key Components**:
```csharp
// Cyclic data management
public interface ICyclicDataManager 
{
    Task StartCyclicDataAsync(string deviceId);
    Task StopCyclicDataAsync(string deviceId);
    bool IsCyclicDataActive(string deviceId);
    event EventHandler<CyclicDataReceivedEventArgs> CyclicDataReceived;
}

public class CyclicScheduler 
{
    // Manages timing for multiple devices
    public void ScheduleDevice(string deviceId, int intervalMs);
    public void UnscheduleDevice(string deviceId);
}

public class DataBuffer 
{
    // Efficient buffering for real-time streaming
    public void AddData(string deviceId, CyclicDataPoint data);
    public IEnumerable<CyclicDataPoint> GetData(string deviceId, DateTime since);
}

// Device-specific data parsing
public interface IDeviceDataParser
{
    CyclicDataPoint ParseMessage(CanMessage message, CyclicDataDefinition definition);
    bool CanParseMessage(CanMessage message, uint deviceBaseCanId);
}

// SignalR integration matching reference project real-time patterns
public class DeviceDataHub : Hub 
{
    // Real-time data streaming (100ms intervals like reference)
    public async Task SubscribeToDevice(string deviceId)
    public async Task UnsubscribeFromDevice(string deviceId)
    public async Task SubscribeToOutputs(string deviceId, int[] outputIndices)
    public async Task SubscribeToInputs(string deviceId, int[] inputIndices)
    
    // Client methods to implement:
    // - OnCyclicDataReceived(deviceId, data) - Device status, currents, voltage, temperature
    // - OnOutputStateChanged(deviceId, outputIndex, state, current, power)
    // - OnInputStateChanged(deviceId, inputIndex, state)
    // - OnCanInputReceived(deviceId, canInputIndex, value)
    // - OnVirtualInputChanged(deviceId, virtualInputIndex, state)
    // - OnDeviceConnected(deviceId, deviceInfo)
    // - OnDeviceDisconnected(deviceId, reason)
    // - OnDeviceError(deviceId, errorCode, message)
    // - OnParameterChanged(deviceId, category, parameterName, oldValue, newValue)
    // - OnDeviceStateChanged(deviceId, oldState, newState) // Run/Sleep/Error/OverTemp
    // - OnCommsConnected(commsType, portInfo)
    // - OnCommsDisconnected(reason)
    // - OnMessageTransmitted(deviceId, canId, data) // For debugging
    // - OnMessageReceived(deviceId, canId, data) // For debugging
}

public class CyclicDataService 
{
    // Coordinates between comms manager, data parsing, and SignalR
}
```

**API Endpoints**:
- `POST /api/devices/{deviceId}/cyclic/start` - Start cyclic data collection
- `POST /api/devices/{deviceId}/cyclic/stop` - Stop cyclic data collection
- `GET /api/devices/{deviceId}/cyclic/status` - Get cyclic data status
- `GET /api/devices/{deviceId}/cyclic/data` - Get recent cyclic data

**SignalR Events** (matching reference project real-time patterns):
- `OnCyclicDataReceived(deviceId, data)` - Real-time data updates (100ms intervals)
- `OnDeviceStatusChanged(deviceId, status)` - Device state changes (Run/Sleep/Error)
- `OnOutputStateChanged(deviceId, outputIndex, state, current)` - Output state and current updates
- `OnInputStateChanged(deviceId, inputIndex, state)` - Digital input state changes
- `OnCanInputReceived(deviceId, canInputIndex, value)` - CAN input value updates
- `OnDeviceConnected(deviceId, connectionInfo)` - Device connection established
- `OnDeviceDisconnected(deviceId, reason)` - Device disconnection
- `OnDeviceError(deviceId, errorCode, message)` - Device error notifications
- `OnDataStreamStarted(deviceId)` - Data collection started
- `OnDataStreamStopped(deviceId)` - Data collection stopped
- `OnParameterChanged(deviceId, category, parameterName, value)` - Parameter value changes

**Acceptance Criteria**:
- Collect cyclic data from multiple devices simultaneously
- Parse raw CAN messages using catalog signal definitions and offset calculations
- Stream data to clients via SignalR with configurable intervals
- Handle data buffer overflow and memory management
- Support different data collection rates per device
- Correctly route messages to devices based on calculated CAN IDs (base + offset)

---

### Milestone 5: Device Settings Read/Write
**Goal**: Implement bidirectional settings communication with devices using offset-based addressing

**Deliverables**:
- Implement settings read operations from devices
- Implement settings write operations to devices
- Add settings validation against catalog parameters
- Implement burn (NVRAM storage) functionality
- Add bootloader entry capability
- Create settings comparison and change detection
- Handle offset-based CAN ID calculations for parameter access

**Key Components**:
```csharp
// Settings management
public interface IDeviceCommunication 
{
    Task<DeviceSettings> ReadSettingsAsync(string deviceId);
    Task<bool> WriteSettingsAsync(string deviceId, DeviceSettings settings);
    Task<bool> WriteParameterAsync(string deviceId, string parameterName, object value);
    Task<bool> BurnSettingsAsync(string deviceId);
    Task<bool> EnterBootloaderAsync(string deviceId);
}

public class SettingsManager 
{
    // Coordinates settings operations with comms manager
    // Handles CAN ID offset calculations for parameter access
}

public class ParameterValidator 
{
    // Validates parameter values against catalog constraints and reference project rules
    public ValidationResult ValidateParameter(object value, DeviceParameter definition);
    public ValidationResult ValidateNumericRange(object value, object min, object max);
    public ValidationResult ValidateEnumValue(object value, string[] allowedValues);
    public ValidationResult ValidateBooleanValue(object value);
    public ValidationResult ValidateComplexParameter(Dictionary<string, object> values, ComplexParameterDefinition definition);
    public ValidationResult ValidateArrayParameter(object[] values, ArrayParameterDefinition definition, int expectedSize);
    
    // Reference project validation patterns:
    public bool IsValidCurrentLimit(float amps) => amps >= 0 && amps <= 30;
    public bool IsValidPwmFrequency(int hz) => hz >= 1 && hz <= 400;
    public bool IsValidDutyCycle(int percent) => percent >= 1 && percent <= 100;
    public bool IsValidResetTime(float seconds) => seconds >= 0 && seconds <= 25.5f;
    public bool IsValidDebounceTime(int ms) => ms >= 0 && ms <= 1000;
    public bool IsValidBaseCanId(uint id) => id >= 1 && id <= 2046; // 11-bit CAN ID range
}

// Device operations
public class DeviceSettings 
{
    public string DeviceId { get; set; }
    public string DeviceType { get; set; }
    public Dictionary<string, ParameterValue> Parameters { get; set; }
    public DateTime LastRead { get; set; }
    public bool IsModified { get; set; }
}

public class ParameterValue
{
    public string Name { get; set; }
    public object Value { get; set; }
    public object OriginalValue { get; set; } // Value from device
    public bool IsModified => !Equals(Value, OriginalValue);
}

public class SettingsComparer 
{
    // Detects differences between device and configuration
}
```

**API Endpoints**:
- `GET /api/devices/{deviceId}/settings` - Read settings from device
- `PUT /api/devices/{deviceId}/settings` - Write settings to device
- `PUT /api/devices/{deviceId}/settings/{parameterName}` - Write single parameter
- `POST /api/devices/{deviceId}/burn` - Burn settings to NVRAM
- `POST /api/devices/{deviceId}/bootloader` - Enter bootloader mode
- `GET /api/devices/{deviceId}/settings/compare` - Compare device vs config settings

**Acceptance Criteria**:
- Read all device parameters as defined in catalog using calculated CAN IDs
- Write individual or bulk parameter changes to device
- Validate parameter values against catalog constraints
- Detect and highlight settings differences between device and configuration
- Handle read/write errors and provide meaningful feedback
- Support atomic settings operations (all-or-nothing writes)
- Correctly calculate CAN IDs for parameter access (device base ID + parameter offset)

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
public interface ISimulationService 
{
    Task StartSimulationAsync(string deviceId, SimulationConfig config);
    Task StopSimulationAsync(string deviceId);
    Task UpdateSimulatedValueAsync(string deviceId, string parameter, object value);
    bool IsSimulating(string deviceId);
    event EventHandler<SimulatedMessageEventArgs> MessageGenerated;
}

public class SimulationEngine 
{
    // Coordinates virtual devices and message generation
}

public class VirtualDevice 
{
    // Simulates a real device with configurable behavior
    public string DeviceId { get; set; }
    public string CatalogType { get; set; }
    public uint BaseCanId { get; set; }
    public Dictionary<string, object> SimulatedParameters { get; set; }
    public Dictionary<string, DataGenerator> CyclicGenerators { get; set; }
}

// Simulation behaviors
public class SimulationConfig 
{
    public bool Enabled { get; set; }
    public int ResponseDelayMs { get; set; } = 10;
    public Dictionary<string, object> InitialValues { get; set; }
    public Dictionary<string, GeneratorConfig> DataGenerators { get; set; }
    public bool RandomizeValues { get; set; } = false;
    public int RandomSeed { get; set; }
}

public class DataGenerator 
{
    // Generates realistic data patterns (sine waves, random walks, etc.)
}

public class GeneratorConfig
{
    public string Type { get; set; } // "constant", "sine", "random", "ramp"
    public Dictionary<string, object> Parameters { get; set; }
}
```

**API Endpoints**:
- `POST /api/devices/{deviceId}/simulation/start` - Start device simulation
- `POST /api/devices/{deviceId}/simulation/stop` - Stop device simulation
- `PUT /api/devices/{deviceId}/simulation/config` - Update simulation settings
- `POST /api/devices/{deviceId}/simulation/scenario` - Load simulation scenario
- `PUT /api/devices/{deviceId}/simulation/parameter/{name}` - Set simulated parameter value

**Acceptance Criteria**:
- Simulate all communication protocols (SLCAN, PCAN, USB CDC)
- Generate realistic cyclic data with configurable patterns
- Respond to settings read/write operations with simulated delays
- Support multiple simulation scenarios and test cases
- Allow real-time modification of simulated parameter values
- Provide simulation state persistence across application restarts
- Use correct offset-based CAN IDs for simulated message generation

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
- Communication interface management (single connection)

**Key Components**:
- Controllers for device operations
- SignalR hubs for real-time communication
- Middleware for error handling and logging
- API versioning support

### dingoConfig.application
**Purpose**: Business logic and orchestration

**Responsibilities**:
- Device communication protocols (SLCAN, PCAN, USB CDC)
- Communications manager for single connection and message routing
- Message parsing and building using DBC-like definitions with offset calculations
- Cyclic data handling and scheduling
- Device state management
- Simulation engine
- Configuration validation and transformation

**Key Components**:
- Communication interfaces for each protocol type
- CommsManager for routing messages between single comms and multiple devices
- Message parsers and builders with offset-based CAN ID handling
- Scheduler for cyclic operations
- State machines for device lifecycle
- Simulation services

### dingoConfig.contracts
**Purpose**: Shared data contracts and definitions

**Responsibilities**:
- DTOs for API communication
- Device catalog schema definitions with offset-based CAN IDs
- Configuration file schema (without stored device IDs)
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
- JSON configuration file I/O with runtime ID generation
- Device catalog loading and validation
- Settings persistence
- File system operations
- Data serialization/deserialization

**Key Components**:
- Configuration repository with array-index device ID handling
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

// Message format from reference: t[ID][DLC][DATA]\r
// t = Standard frame, T = Extended frame (not used in reference)
// ID = 3-digit hex CAN ID (e.g., 7D8)
// DLC = 1-digit data length (0-8)
// DATA = Data bytes in hex pairs
// Example: t7D81FF\r (ID=0x7D8, DLC=1, Data=0xFF)
// Initialization: C\r (close), S[bitrate]\r (set speed), O\r (open)
```

### PCAN USB
```csharp
public class PcanConfiguration
{
    public PcanChannel Channel { get; set; }
    public PcanBaudrate Baudrate { get; set; } = PcanBaudrate.PCAN_BAUD_500K;
    public bool UseExtendedFrames { get; set; } = false; // Reference uses standard frames
    public string LibraryPath { get; set; } // Platform-specific PCAN library path
}

// Reference uses Peak.Can.Basic library with:
// - Standard CAN frames (11-bit IDs)
// - Baudrates: 125K, 250K, 500K, 1000K
// - Real-time message processing with timing analysis
// - Worker thread pattern for continuous message handling
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
    public string PortName { get; set; } // Reference uses serial port communication
    public int BaudRate { get; set; } = 115200;
    // Reference implementation uses serial port at 115200 baud
    // Similar ASCII protocol to SLCAN but simplified
    // Raw byte transmission for direct device communication
}
```

## Device Control Operations

### Connection Management
```csharp
public interface IDeviceManager
{
    Task<bool> ConnectAsync(CommunicationType commsType, object config);
    Task<bool> DisconnectAsync();
    Task<DeviceStatus> GetStatusAsync(string deviceId);
    Task RegisterDeviceAsync(string deviceId, uint baseCanId);
    Task UnregisterDeviceAsync(string deviceId);
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
    public string DeviceId { get; set; } // Runtime-generated "device_{index}"
    public string DeviceType { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsModified { get; set; }
    
    // Complex parameter collections from reference project
    public List<OutputConfiguration> Outputs { get; set; }
    public List<InputConfiguration> DigitalInputs { get; set; }
    public List<CanInputConfiguration> CanInputs { get; set; }
    public List<VirtualInputConfiguration> VirtualInputs { get; set; }
    public List<FlasherConfiguration> Flashers { get; set; }
    public List<CounterConfiguration> Counters { get; set; }
    public List<ConditionConfiguration> Conditions { get; set; }
    public List<KeypadConfiguration> Keypads { get; set; }
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
    public uint CanIdOffset { get; set; } // Offset from device base CAN ID
}
```

## Device Catalog System

### Catalog File Structure
```json
{
  "deviceType": "DingoPdm",
  "version": "1.0.0",
  "manufacturer": "Dingo",
  "description": "Power Distribution Module with 8 outputs",
  "communication": {
    "type": "CAN",
    "baseIdOffset": "0x000",
    "extendedFrames": false,
    "canIdRange": "0x020",
    "baudRates": ["125K", "250K", "500K", "1000K"]
  },
  "deviceLimits": {
    "numOutputs": 8,
    "numDigitalInputs": 8,
    "numCanInputs": 32,
    "numVirtualInputs": 16,
    "numFlashers": 4,
    "numCounters": 4,
    "numConditions": 32,
    "numKeypads": 2,
    "numKeypadButtons": 20
  },
  "messageDefinitions": {
    "deviceStatus": {
      "canIdOffset": "0x00",
      "description": "Device state, current, voltage, temperature",
      "signals": [
        {"name": "digitalInputs", "startBit": 0, "length": 8, "type": "uint8"},
        {"name": "deviceState", "startBit": 8, "length": 8, "type": "enum", "values": ["Run", "Sleep", "OverTemp", "Error"]},
        {"name": "totalCurrent", "startBit": 16, "length": 16, "type": "uint16", "scaling": {"factor": 0.1, "units": "A"}},
        {"name": "batteryVoltage", "startBit": 32, "length": 16, "type": "uint16", "scaling": {"factor": 0.01, "units": "V"}},
        {"name": "mcuTemperature", "startBit": 48, "length": 16, "type": "int16", "scaling": {"factor": 0.1, "units": "°C"}}
      ]
    },
    "outputCurrents1": {
      "canIdOffset": "0x01",
      "description": "Output current readings 1-4",
      "signals": [
        {"name": "output1Current", "startBit": 0, "length": 16, "type": "uint16", "scaling": {"factor": 0.01, "units": "A"}},
        {"name": "output2Current", "startBit": 16, "length": 16, "type": "uint16", "scaling": {"factor": 0.01, "units": "A"}},
        {"name": "output3Current", "startBit": 32, "length": 16, "type": "uint16", "scaling": {"factor": 0.01, "units": "A"}},
        {"name": "output4Current", "startBit": 48, "length": 16, "type": "uint16", "scaling": {"factor": 0.01, "units": "A"}}
      ]
    },
    "outputStates": {
      "canIdOffset": "0x03",
      "description": "Output states and control flags",
      "signals": [
        {"name": "outputStates", "startBit": 0, "length": 8, "type": "uint8", "description": "Bit field of output states"},
        {"name": "outputFaults", "startBit": 8, "length": 8, "type": "uint8", "description": "Bit field of output faults"}
      ]
    }
  },
  "parameters": [
    {
      "name": "sleepEnabled",
      "displayName": "Sleep Mode Enabled",
      "type": "bool",
      "canIdOffset": "0x1E",
      "messagePrefix": "Can",
      "subIndex": 1,
      "defaultValue": true,
      "category": "Power Management",
      "description": "Enable sleep mode when no CAN activity"
    },
    {
      "name": "baudRate",
      "displayName": "CAN Baud Rate",
      "type": "enum",
      "canIdOffset": "0x1E",
      "messagePrefix": "Can",
      "subIndex": 2,
      "values": ["BAUD_125K", "BAUD_250K", "BAUD_500K", "BAUD_1000K"],
      "defaultValue": "BAUD_500K",
      "category": "Communication",
      "description": "CAN bus communication speed"
    }
  ],
  "complexParameters": {
    "outputs": {
      "arraySize": 8,
      "parameters": [
        {
          "name": "enabled",
          "type": "bool",
          "canIdOffset": "0x1E",
          "messagePrefix": "Outputs",
          "defaultValue": false
        },
        {
          "name": "currentLimit",
          "type": "float",
          "canIdOffset": "0x1E",
          "messagePrefix": "Outputs",
          "limits": {"min": 0, "max": 30},
          "units": "A",
          "defaultValue": 15.0,
          "validation": "CurrentLimitValidationRule"
        },
        {
          "name": "resetMode",
          "type": "enum",
          "values": ["None", "Count", "Endless"],
          "defaultValue": "None"
        }
      ]
    },
    "digitalInputs": {
      "arraySize": 8,
      "parameters": [
        {
          "name": "enabled",
          "type": "bool",
          "defaultValue": false
        },
        {
          "name": "mode",
          "type": "enum",
          "values": ["Momentary", "Latching"],
          "defaultValue": "Momentary"
        },
        {
          "name": "pull",
          "type": "enum",
          "values": ["NoPull", "PullUp", "PullDown"],
          "defaultValue": "NoPull"
        }
      ]
    },
    "canInputs": {
      "arraySize": 32,
      "parameters": [
        {
          "name": "id",
          "type": "uint32",
          "description": "CAN message ID"
        },
        {
          "name": "startingByte",
          "type": "uint8",
          "limits": {"min": 0, "max": 7}
        },
        {
          "name": "operator",
          "type": "enum",
          "values": ["Equals", "NotEquals", "GreaterThan", "LessThan", "GreaterThanOrEqual", "LessThanOrEqual"]
        }
      ]
    }
  },
  "enums": {
    "VarMap": {
      "description": "Variable mapping for inputs and outputs",
      "values": [
        "Off",
        "DigitalInput1", "DigitalInput2", "DigitalInput3", "DigitalInput4",
        "CanInput1", "CanInput2", "CanInput3", "CanInput4",
        "VirtualInput1", "VirtualInput2", "VirtualInput3", "VirtualInput4",
        "Output1", "Output2", "Output3", "Output4"
      ]
    },
    "MessagePrefix": {
      "description": "Message categories for device communication",
      "values": [
        {"name": "Can", "value": 1},
        {"name": "Inputs", "value": 5},
        {"name": "Outputs", "value": 10},
        {"name": "VirtualInputs", "value": 15},
        {"name": "CanInputs", "value": 35},
        {"name": "Keypad", "value": 50},
        {"name": "Version", "value": 120},
        {"name": "BurnSettings", "value": 127}
      ]
    }
  },
  "cyclicData": [
    {
      "name": "deviceStatus",
      "canIdOffset": "0x00",
      "interval": 100,
      "description": "Real-time device status data"
    },
    {
      "name": "outputCurrents",
      "canIdOffset": "0x01",
      "interval": 100,
      "description": "Output current measurements"
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
    uint CalculateCanId(string deviceId, uint baseCanId, uint offset);
    bool ValidateCanIdRanges(List<DeviceConfiguration> devices);
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
  "communicationSettings": {
    "activeType": "SLCAN",
    "slcanConfig": {
      "portName": "COM3",
      "baudRate": 115200,
      "canBitrate": "500K"
    },
    "pcanConfig": {
      "channel": "PCAN_USBBUS1",
      "baudrate": "PCAN_BAUD_500K"
    },
    "usbCdcConfig": {
      "deviceId": "USB\\VID_1234&PID_5678",
      "baudRate": 115200
    }
  },
  "devices": [
    {
      "catalogType": "ExampleDevice",
      "baseCanId": "0x100",
      "name": "Motor Controller 1",
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
    },
    {
      "catalogType": "ExampleDevice",
      "baseCanId": "0x200", 
      "name": "Motor Controller 2",
      "settings": {
        "motorSpeed": 2000,
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
    DeviceConfiguration AddDeviceFromCatalog(string catalogType, uint baseCanId, string customName = null);
    bool RemoveDevice(int deviceIndex);
    bool ValidateCanIdConflicts(List<DeviceConfiguration> devices);
}

public class ConfigurationLoader
{
    public ProjectConfiguration LoadFromFile(string filePath)
    {
        var jsonData = File.ReadAllText(filePath);
        var configData = JsonSerializer.Deserialize<ProjectConfigurationData>(jsonData);
        
        // Convert to runtime objects with generated IDs
        return new ProjectConfiguration
        {
            ProjectInfo = configData.ProjectInfo,
            CommunicationSettings = configData.CommunicationSettings,
            Devices = configData.Devices.Select((deviceData, index) => 
                new DeviceConfiguration
                {
                    Id = $"device_{index}", // Generated runtime ID
                    CatalogType = deviceData.CatalogType,
                    BaseCanId = deviceData.BaseCanId,
                    Name = deviceData.Name,
                    Settings = deviceData.Settings,
                    CyclicMessages = deviceData.CyclicMessages,
                    Simulation = deviceData.Simulation
                }).ToList()
        };
    }
    
    public void SaveToFile(ProjectConfiguration config, string filePath)
    {
        // Convert to serialization format (no IDs)
        var configData = new ProjectConfigurationData
        {
            ProjectInfo = config.ProjectInfo,
            CommunicationSettings = config.CommunicationSettings,
            Devices = config.Devices.Select(device => new DeviceConfigurationData
            {
                // ID not stored - will be regenerated on load
                CatalogType = device.CatalogType,
                BaseCanId = device.BaseCanId,
                Name = device.Name,
                Settings = device.Settings,
                CyclicMessages = device.CyclicMessages,
                Simulation = device.Simulation
            }).ToList()
        };
        
        var jsonData = JsonSerializer.Serialize(configData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(filePath, jsonData);
    }
}
```

## REST API Endpoints

### Device Management
```
GET    /api/devices                    - List all configured devices
GET    /api/devices/{deviceId}         - Get device details (e.g., device_0, device_1)
POST   /api/devices/add-from-catalog   - Add new device from catalog
PUT    /api/devices/{deviceId}         - Update device configuration
DELETE /api/devices/{deviceId}         - Remove device

POST   /api/devices/{deviceId}/connect       - Connect to device (deprecated - use comms endpoints)
POST   /api/devices/{deviceId}/disconnect    - Disconnect from device (deprecated - use comms endpoints)
GET    /api/devices/{deviceId}/status        - Get device status
```

### Communication Management
```
GET    /api/comms/types                - List available comms types
GET    /api/comms/ports               - Get available ports for current comms type
POST   /api/comms/connect             - Connect single comms interface
POST   /api/comms/disconnect          - Disconnect current comms interface
GET    /api/comms/status              - Get current comms connection status
```

### Device Communication
```
# Settings Operations (MessagePrefix-based like reference project)
GET    /api/devices/{deviceId}/settings      - Read all settings from device
GET    /api/devices/{deviceId}/settings/{category} - Read specific category (Can, Inputs, Outputs, etc.)
PUT    /api/devices/{deviceId}/settings      - Write settings to device RAM
PUT    /api/devices/{deviceId}/settings/{category} - Write specific category
PUT    /api/devices/{deviceId}/settings/{category}/{index} - Write single parameter
POST   /api/devices/{deviceId}/burn          - Burn settings to NVRAM
POST   /api/devices/{deviceId}/bootloader    - Enter bootloader mode
GET    /api/devices/{deviceId}/version       - Get firmware version

# Complex Parameter Management (from reference project)
GET    /api/devices/{deviceId}/outputs       - Get all output configurations
PUT    /api/devices/{deviceId}/outputs/{index} - Update specific output
GET    /api/devices/{deviceId}/inputs        - Get all input configurations  
PUT    /api/devices/{deviceId}/inputs/{index} - Update specific input
GET    /api/devices/{deviceId}/can-inputs    - Get CAN input configurations
PUT    /api/devices/{deviceId}/can-inputs/{index} - Update specific CAN input
GET    /api/devices/{deviceId}/virtual-inputs - Get virtual input configurations
PUT    /api/devices/{deviceId}/virtual-inputs/{index} - Update specific virtual input

# Real-time Data (matching reference project message structure)
POST   /api/devices/{deviceId}/cyclic/start  - Start cyclic communication
POST   /api/devices/{deviceId}/cyclic/stop   - Stop cyclic communication
GET    /api/devices/{deviceId}/cyclic/status - Get cyclic data status
GET    /api/devices/{deviceId}/cyclic/data   - Get recent cyclic data
GET    /api/devices/{deviceId}/runtime-data  - Get current device state (outputs, currents, temperature)

# Device Control (from reference project patterns)
POST   /api/devices/{deviceId}/sleep         - Put device to sleep
POST   /api/devices/{deviceId}/wake          - Wake device from sleep
POST   /api/devices/{deviceId}/reset         - Reset device
GET    /api/devices/{deviceId}/diagnostics   - Get device diagnostic info
```

### Configuration Management
```
GET    /api/configuration              - Get current configuration
GET    /api/configuration/new          - Create new empty configuration
POST   /api/configuration/load         - Load configuration file
POST   /api/configuration/save         - Save configuration file
POST   /api/configuration/validate     - Validate configuration
```

### Device Catalog Management
```
GET    /api/catalogs                   - List available device catalogs
GET    /api/catalogs/{deviceType}      - Get specific catalog details
POST   /api/catalogs/validate          - Validate catalog file
POST   /api/catalogs/reload            - Reload catalogs from directory
```

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
    // - OnCommsConnected(commsType)
    // - OnCommsDisconnected()
    // - OnCommsError(error)
    // - OnUpdateAvailable(updateInfo)
    // - OnUpdateProgress(progress)
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

connection.on("OnCommsConnected", (commsType) => {
    updateCommsStatus("Connected", commsType);
});

connection.on("OnCommsDisconnected", () => {
    updateCommsStatus("Disconnected");
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
- Message parsing and building with offset calculations
- Configuration validation
- Catalog loading
- Update checking and version comparison
- GitHub API integration
- Runtime device ID generation

### Integration Tests
- End-to-end device communication
- SignalR hub functionality
- File I/O operations with array-index device IDs
- API endpoint testing
- Update download and installation process
- GitHub release API integration
- CAN ID conflict detection

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