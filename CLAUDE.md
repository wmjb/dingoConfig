# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

dingoConfig is a .NET 9.0 web application for managing dingo CAN devices (dingoPDM, dingoPDM-Max, CANBoard) through various communication adapters (USB, SLCAN, PCAN, Simulated). The system handles real-time CAN data at 1,000-3,000 messages/second, provides device configuration management (~100 parameters per device), and offers web-based monitoring with SignalR real-time updates.

## Build and Development Commands

### Build the solution
```bash
dotnet build dingoConfig.sln
```

### Run the API (web application)
```bash
dotnet run --project api/api.csproj
```

### Build specific projects
```bash
dotnet build api/api.csproj
dotnet build domain/domain.csproj
dotnet build infrastructure/infrastructure.csproj
dotnet build application/application.csproj
dotnet build contracts/contracts.csproj
```

### Run tests (when test projects are added)
```bash
dotnet test
```

### Clean build artifacts
```bash
dotnet clean
```

## Architecture

This project follows **Clean Architecture** with clear separation of concerns across five layers:

### Layer Structure and Dependencies

```
api (Presentation)
├── depends on: application, contracts, infrastructure
├── Controllers for device-specific endpoints
├── Realtime/Hubs/ for SignalR real-time updates
├── Middleware and health checks
└── Web UI (wwwroot/)

application (Business Logic)
├── depends on: domain, contracts
├── MediatR commands/queries (when implemented)
├── Business services (DeviceManager, ConfigurationService)
├── FluentValidation validators (when implemented)
└── AutoMapper profiles (when implemented)

contracts (DTOs)
├── no dependencies
└── Pure data structures for API requests/responses

domain (Core Domain)
├── no dependencies
├── Core interfaces (ICommsAdapter, IDevice, ICommsAdapterManager)
├── Domain models (CanData, DeviceResponse)
├── Device implementations (when added)
├── Domain events and exceptions
└── Enums (CanBitRate, etc.)

infrastructure (Implementation)
├── depends on: domain, application
├── Communication adapter implementations (USB, SLCAN, PCAN, Simulated)
├── CommsAdapterManager (runtime adapter selection)
├── CommsDataPipeline (bidirectional TX/RX with Channels)
├── JSON configuration persistence (when implemented)
├── CSV logging (when implemented)
└── Background services
```

**Key Rule**: Domain layer has NO dependencies. It defines interfaces that infrastructure implements.

## Core Components

### 1. CommsAdapterManager (Runtime Adapter Selection)

**Location**: `infrastructure/Comms/CommsAdapterManager.cs`

The CommsAdapterManager allows users to select and connect to communication adapters at runtime (not compile-time). It manages the active adapter lifecycle, forwards DataReceived events, and enables hot-swapping between different adapter types.

**Key Methods**:
- `ConnectAsync(ICommsAdapter, string port, CanBitRate, CancellationToken)`: Initialize and start an adapter
- `DisconnectAsync()`: Stop and clean up the current adapter
- `DataReceived` event: Fired when data arrives from the active adapter

**Usage Pattern**:
1. User selects adapter type in UI
2. Frontend calls API endpoint with adapter type
3. Controller resolves specific adapter from DI (UsbAdapter, SlcanAdapter, etc.)
4. Controller calls `CommsAdapterManager.ConnectAsync()` with the adapter instance
5. CommsDataPipeline receives data via manager's DataReceived event

### 2. CommsDataPipeline (Bidirectional TX/RX)

**Location**: `infrastructure/BackgroundServices/CommsDataPipeline.cs`

Processes all CAN bus communication using System.Threading.Channels for high-performance async message processing at 3000 msg/s.

**Architecture**:
- **RX Channel**: 50,000 capacity, drops oldest on full, routes incoming CAN data to devices
- **TX Channel**: 10,000 capacity, queues outgoing CAN data for transmission
- Background service that runs both pipelines concurrently

**Key Features**:
- Non-blocking channel-based architecture
- Subscribes to CommsAdapterManager.DataReceived (not directly to adapter)
- Separate RX and TX processing pipelines
- `QueueTransmit(CanData)` method for fire-and-forget transmission

**Important**: The pipeline subscribes to `ICommsAdapterManager.DataReceived`, NOT directly to individual adapters, because adapters are selected at runtime.

### 3. Communication Adapters

**Location**: `infrastructure/Comms/Adapters/`

All adapters implement `ICommsAdapter`:
```csharp
public delegate void DataReceivedHandler(object sender, CanDataEventArgs e);

public interface ICommsAdapter
{
    Task<(bool success, string? error)> InitAsync(string port, CanBitRate bitRate, CancellationToken ct);
    Task<(bool success, string? error)> StartAsync(CancellationToken ct);
    Task<(bool success, string? error)> StopAsync();
    Task<(bool success, string? error)> WriteAsync(CanData data, CancellationToken ct);
    DataReceivedHandler DataReceived { get; set; }
    TimeSpan RxTimeDelta();
    bool IsConnected { get; }
}
```

Available adapters: `UsbAdapter`, `SlcanAdapter`, `PcanAdapter`, `SimAdapter`

### 4. DeviceManager (Device Lifecycle & Request/Response Tracking)

**Location**: `application/Services/DeviceManager.cs`

Central service for managing all devices and coordinating CAN communication. Handles device creation, request/response tracking with timeouts and retries, and device operations.

**Key Responsibilities**:
- **Device Registry**: `Dictionary<Guid, IDevice>` - all devices keyed by Guid
- **Request Queue**: `ConcurrentDictionary<(BaseId, Prefix, Index), DeviceCanFrame>` - tracks pending messages
- **Timeout Management**: Automatically retries failed messages (max 3 attempts, 500ms timeout)
- **Polymorphic Operations**: Calls interface methods on all device types uniformly

**Key Methods**:
- `AddDevice(string deviceType, string name, int baseId)`: Create and register a new device
- `GetDevice(Guid id)` / `GetDevice<T>(Guid id)`: Retrieve devices
- `OnCanDataReceived(CanFrame frame)`: Called by CommsDataPipeline - routes data to devices
- `SetTransmitCallback(Action<CanFrame> callback)`: Sets up TX channel connection
- Device operations: `UploadDeviceConfig()`, `DownloadDeviceConfig()`, `BurnDeviceSettings()`, `SleepDevice()`, `RequestDeviceVersion()`, `DownloadUpdatedConfig()`

**Design Pattern**:
- All devices implement `IDevice` interface with all methods (even if no-op)
- DeviceManager calls interface methods without type checking
- Device-specific functionality handled uniformly through polymorphism

### 5. Dependency Injection Setup

**Location**: `api/Program.cs`

Adapters are registered as Transient (new instance per request):
```csharp
builder.Services.AddTransient<UsbAdapter>();
builder.Services.AddTransient<SlcanAdapter>();
builder.Services.AddTransient<PcanAdapter>();
builder.Services.AddTransient<SimAdapter>();
```

DeviceManager is Singleton (one instance for the lifetime of the app):
```csharp
builder.Services.AddSingleton<DeviceManager>();
```

CommsAdapterManager is Singleton (one instance for the lifetime of the app):
```csharp
builder.Services.AddSingleton<ICommsAdapterManager, CommsAdapterManager>();
```

CommsDataPipeline runs as a hosted background service:
```csharp
builder.Services.AddHostedService<CommsDataPipeline>();
```

## Data Flow Patterns

### RX Pipeline (Receive Cyclic Data)
```
CAN Adapter (3000 msg/s)
    ↓
CommsAdapterManager.DataReceived event
    ↓
CommsDataPipeline.OnDataReceived() writes to RX Channel
    ↓
ProcessRxPipelineAsync() reads from channel
    ↓
DeviceManager.OnCanDataReceived(frame) routes to devices
    ↓
Device.Read() parses data and updates properties
    ↓
If response message: removes from _requestQueue
    ↓
Device updates state/config properties and circular buffers
    ↓
SignalR broadcasts to web clients at 20 Hz
```

### TX Pipeline (Request/Response Tracking)
```
Controller receives request (e.g., /api/pdm-devices/{id}/burn)
    ↓
Controller calls DeviceManager.BurnDeviceSettings(deviceId)
    ↓
DeviceManager.QueueMessage(DeviceCanFrame)
    ↓
Message added to _requestQueue with key (BaseId, Prefix, Index)
    ↓
Timer starts (500ms timeout)
    ↓
Message queued to TX Channel via SetTransmitCallback()
    ↓
CommsDataPipeline.ProcessTxPipelineAsync() sends to adapter
    ↓
Adapter.WriteAsync() transmits over CAN bus
    ↓
Device responds with matching Prefix
    ↓
Response arrives → RX Pipeline → Device.Read() removes from queue
    ↓
Timer cancelled, message complete
    ↓
[If timeout] HandleMessageTimeout() → retries (max 3 attempts) or logs error
```

### Runtime Adapter Selection Flow
```
User selects "PCAN" in UI
    ↓
Frontend: POST /api/adapter/connect { adapterType: "PCAN" }
    ↓
AdapterController.Connect()
    ↓
Resolve PcanAdapter from DI
    ↓
CommsAdapterManager.ConnectAsync(pcanAdapter, port, bitRate)
    ↓
Manager subscribes to adapter.DataReceived
    ↓
Manager calls adapter.InitAsync() then adapter.StartAsync()
    ↓
CommsDataPipeline starts receiving data via manager
```

## Implementation Approach

### Current State (Phase 3)
- ✅ Foundation and folder structure complete
- ✅ Core interfaces defined in domain/ (ICommsAdapter, IDevice, ICommsAdapterManager)
- ✅ CommsAdapterManager fully implemented in infrastructure/Comms/
- ✅ CommsDataPipeline with bidirectional channels and RX/TX pipelines implemented
- ✅ Adapter stubs created (UsbAdapter, SlcanAdapter, PcanAdapter, SimAdapter)
- ✅ DeviceManager fully implemented with:
  - Device creation and registry
  - Request/response tracking with timeout/retry logic
  - Polymorphic device operations (no type checking needed)
  - TX callback integration with CommsDataPipeline
- ✅ Device implementations for PdmDevice and PdmMaxDevice (with all IDevice methods)
- ✅ CreateDeviceRequest DTO in contracts/
- ✅ DI setup in api/Program.cs
- ⚠️ Controllers for device operations not yet created
- ⚠️ SignalR real-time updates not yet implemented
- ⚠️ AutoMapper profiles for DTO mapping not yet created

### Next Steps
1. **Build Controllers**: Device-specific API endpoints (DeviceController, PdmDeviceController, etc.)
2. **Add AutoMapper Profiles**: DTO mapping for State/Config DTOs
3. **Add SignalR**: Real-time web updates to clients
4. **Implement CSV Logging**: Message logging with rotation
5. **Add MediatR**: Commands/queries/handlers for business operations (optional optimization)
6. **Unit Tests**: Test DeviceManager, device operations, timeout/retry logic
7. **Integration Tests**: End-to-end CAN communication testing

### When Adding New Features

**Device Classes**:
- Place concrete implementations in `domain/Devices/`
- Implement the complete `IDevice` interface (provide no-op implementations for unused methods)
- Include cyclic properties (real-time values), configuration properties, and circular buffers for charting
- Parse incoming CAN data and update device state via `Read()` method
- Remove completed messages from request queue via the ref `queue` parameter

**Controllers**:
- Create device-specific controllers in `api/Controllers/`
- Call `DeviceManager` methods for device operations (upload, download, burn, etc.)
- Map DTOs to device properties before calling `DownloadUpdatedConfig()`
- Use AutoMapper for DTO ↔ domain model mapping
- Return DTOs from `contracts/` layer

**DTOs** (State vs Config):
- **State DTOs**: Real-time data, sent via SignalR, updated frequently from device cyclic messages
  - Properties: Connected, LastRxTime, Version, BatteryVoltage, TotalCurrent, temperature, input/output states, etc.
  - Location: `contracts/Devices/{DeviceType}/[DeviceName]StateDto.cs`
  - Base: `DeviceStateDto` (contains Guid, Name, BaseId, Connected, LastRxTime, Version, voltage, current, temps)

- **Config DTOs**: Persistent settings, modified via REST API, downloaded to device via CAN
  - Properties: SleepEnabled, CanFiltersEnabled, BitRate, input/output configurations, etc.
  - Location: `contracts/Devices/{DeviceType}/[DeviceName]ConfigDto.cs`
  - Base: `DeviceConfigDto` (contains Guid, Name, BaseId, SleepEnabled, CanFiltersEnabled, BitRate)

- **Request DTOs**: Data sent TO the server for creating/updating resources
  - `CreateDeviceRequest`: Device creation (DeviceType, Name, BaseId)
  - `Update[Device]ConfigRequest`: Config updates (same fields as ConfigDto minus Guid/BaseId which are immutable)
  - Location: `contracts/Devices/{DeviceType}/Requests/`

**Background Services**:
- Implement as IHostedService
- Register in `api/Program.cs` with `AddHostedService<T>()`
- For startup tasks, register as factory service if dependency on other singletons needed

## Key Design Decisions

### Why Channel-based Pipeline?
System.Threading.Channels provides high-performance async message processing needed for 3000 msg/s without blocking. The bounded channels with DropOldest policy ensure the system never blocks the CAN adapter.

### Why Runtime Adapter Selection?
Users need to choose their CAN adapter type at runtime based on available hardware. The CommsAdapterManager abstraction allows hot-swapping adapters without recompiling or restarting the application.

### Why No Database for MVP?
The MVP uses JSON for configuration persistence, in-memory circular buffers for charting (last 1 hour), and CSV files for message logging. This is sufficient for real-time monitoring and avoids database complexity.

### Why Clean Architecture?
Clear separation of concerns makes the codebase maintainable and testable. The domain layer defines interfaces without dependencies, while infrastructure provides implementations. This allows swapping implementations (e.g., different adapters) without changing business logic.

## Configuration

Key settings that will be needed in `appsettings.json` (to be added during implementation):

```json
{
  "CanPipeline": {
    "RxChannelCapacity": 50000,
    "TxChannelCapacity": 10000,
    "DefaultTimeoutMs": 500,
    "DefaultMaxRetries": 3
  },
  "SignalR": {
    "BroadcastIntervalMs": 50,
    "KeepAliveIntervalSeconds": 15
  },
  "MessageLogging": {
    "Directory": "./logs",
    "RetentionDays": 7,
    "BatchSize": 1000
  }
}
```

## Important Notes

1. **CommsDataPipeline subscribes to ICommsAdapterManager, not individual adapters**, because adapters are selected at runtime.

2. **CanFrame Model**: The codebase uses `CanFrame` as the core model (with Id, Len, Payload properties) for all CAN bus communication. `DeviceCanFrame` wraps CanFrame with metadata for request/response tracking.

3. **Device Polymorphism**: All devices implement the complete `IDevice` interface. DeviceManager calls interface methods uniformly without type checking. Devices that don't need certain functionality can provide no-op implementations (e.g., returning empty DeviceCanFrame lists).

4. **Request/Response Tracking**:
   - **Key**: Tuple of `(BaseId, Prefix, Index)` uniquely identifies each pending message
   - **Timeout**: 500ms by default, configurable via constants in DeviceManager
   - **Retries**: Maximum 3 attempts (configurable), automatic backoff on timeout
   - **Response Handling**: Devices remove completed messages from queue in their `Read()` method via ref parameter
   - **Concurrency**: Uses ConcurrentDictionary for thread-safe access from RX and TX paths

5. **Circular Buffer Throttling**: When implemented, devices should throttle circular buffer updates to ~10 Hz (not every CAN message) to save memory.

6. **SignalR Throttling**: SignalR broadcasts should be throttled to 20 Hz to avoid overwhelming browsers while internal processing runs at full speed.

7. **No MediatR in RX Path**: Don't use MediatR for cyclic data processing (too slow for 3000 msg/s). Use events and direct method calls instead.

8. **Channel Capacity Tuning**: The 50K RX and 10K TX capacities are starting points. Monitor BoundedChannelFullMode.DropOldest behavior and adjust if messages are being dropped.

9. **Realtime Folder**: The actual codebase uses `api/Realtime/` for SignalR components, not `api/SignalR/` as might be expected.

10. **SetTransmitCallback**: DeviceManager requires CommsDataPipeline to call `SetTransmitCallback()` during startup to establish the TX path. This decouples DeviceManager from the pipeline infrastructure.

11. **Device Identity**: Each device has a unique `Guid` assigned at creation time. Use this for all API endpoints and SignalR subscriptions. `BaseId` is the CAN identifier range, not the unique device key.

## Reference Documentation

For detailed technical specification including:
- Complete CAN protocol details
- Request/response tracking implementation
- Device-specific parameter mappings
- Phase-by-phase implementation guide
- Success criteria and testing strategy

See: `dingoconfig-spec.md` in the repository root.
