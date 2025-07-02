# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

.NET 9.0 is required. Use the full path to dotnet.exe on Windows WSL:

```bash
# Build the entire solution
"/mnt/c/Program Files/dotnet/dotnet.exe" build

# Build specific project
"/mnt/c/Program Files/dotnet/dotnet.exe" build dingoConfig.api

# Build in Release configuration
"/mnt/c/Program Files/dotnet/dotnet.exe" build -c Release

# Run the API project
"/mnt/c/Program Files/dotnet/dotnet.exe" run --project dingoConfig.api

# Test (when test projects are added)
"/mnt/c/Program Files/dotnet/dotnet.exe" test
```

## Architecture

This is a .NET 9.0 solution with Clean Architecture implementing a device configuration system for CAN and USB CDC communication. The project is a rewrite of the existing DingoConfigurator WPF application as an ASP.NET Core web API.

### Core Projects

- **dingoConfig.api**: ASP.NET Core Web API with controllers and SignalR hubs for real-time communication
- **dingoConfig.application**: Business logic layer handling device communication protocols (SLCAN, PCAN, USB CDC), message parsing, and simulation
- **dingoConfig.contracts**: Shared DTOs, enums, and data contracts between layers
- **dingoConfig.persistence**: Data persistence layer for JSON configuration files and device catalogs

### Legacy Reference

The `DingoConfigurator/` directory contains the original WPF application that serves as a reference for:
- Device communication patterns and data structures
- CAN message formats and device property definitions (via JsonPropertyName attributes)
- UI layout and user workflows
- Device types: CanBoard, DingoDash, DingoPdm, SoftButtonBox, etc.

## Key Features

- Multi-device simultaneous CAN/USB communication
- Real-time data streaming via SignalR
- JSON-based device catalog system for generic device handling
- Configuration file management (save/load/validate)
- Device simulation capabilities
- Single executable deployment with embedded catalogs

## Project Specification

**IMPORTANT**: Always reference `dingoConfig-spec.md` when working on this project. It contains:
- Implementation milestones with specific deliverables and acceptance criteria
- REST API endpoints for device management
- SignalR hub definitions for real-time updates
- Device catalog schema and communication protocols
- Configuration file structure
- Cross-platform considerations and deployment requirements
- Application update system via GitHub releases

The specification defines 6 implementation milestones:
1. Device Catalog Handling
2. Communication Methods Implementation (SLCAN, PCAN, USB CDC)
3. Configuration File Management
4. Cyclic Data Handling
5. Device Settings Read/Write
6. Simulation Engine