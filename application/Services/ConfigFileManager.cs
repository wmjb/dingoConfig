using System.Text.Json;
using application.Models;
using domain.Devices.CanboardDevice;
using domain.Devices.dingoPdm;
using domain.Devices.dingoPdmMax;
using domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace application.Services;

public class ConfigFileManager(DeviceManager manager, ILogger<ConfigFileManager> logger)
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true};

    private string _workingDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "dingoConfig");
    private string? _currentFileName;
    private bool _hasUnsavedChanges;

    public event Action? OnStateChanged;

    public string WorkingDirectory
    {
        get => _workingDirectory;
        set
        {
            if (_workingDirectory != value)
            {
                _workingDirectory = value;
                EnsureWorkingDirectoryExists();
                OnStateChanged?.Invoke();
            }
        }
    }

    public string? CurrentFileName
    {
        get => _currentFileName;
        private set
        {
            if (_currentFileName != value)
            {
                _currentFileName = value;
                OnStateChanged?.Invoke();
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (_hasUnsavedChanges != value)
            {
                _hasUnsavedChanges = value;
                OnStateChanged?.Invoke();
            }
        }
    }

    public ConfigFileManager Initialize()
    {
        EnsureWorkingDirectoryExists();
        return this;
    }

    private void EnsureWorkingDirectoryExists()
    {
        if (!Directory.Exists(_workingDirectory))
        {
            Directory.CreateDirectory(_workingDirectory);
            logger.LogInformation($"Created working directory: {_workingDirectory}");
        }
    }

    public List<FileInfo> ListJsonFiles()
    {
        EnsureWorkingDirectoryExists();
        var directory = new DirectoryInfo(_workingDirectory);
        return directory.GetFiles("*.json")
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();
    }

    public bool FileExists(string fileName)
    {
        var fullPath = GetFullPath(fileName);
        return File.Exists(fullPath);
    }

    public async Task<T?> OpenFile<T>(string fileName)
    {
        var fullPath = GetFullPath(fileName);

        if (!File.Exists(fullPath))
        {
            logger.LogError($"File not found: {fullPath}");
            return default;
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(fullPath);
            var result = JsonSerializer.Deserialize<T>(jsonString, _options);

            CurrentFileName = fileName;
            HasUnsavedChanges = false;

            logger.LogInformation($"Opened file: {fileName}");
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error opening file: {fileName}");
            throw;
        }
    }

    public async Task SaveFile<T>(T data, string? fileName = null)
    {
        var targetFileName = fileName ?? CurrentFileName;

        if (string.IsNullOrWhiteSpace(targetFileName))
        {
            throw new InvalidOperationException("No filename specified");
        }

        var fullPath = GetFullPath(targetFileName);

        try
        {
            var jsonString = JsonSerializer.Serialize(data, _options);
            await File.WriteAllTextAsync(fullPath, jsonString);

            CurrentFileName = targetFileName;
            HasUnsavedChanges = false;

            logger.LogInformation($"Saved file: {targetFileName}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error saving file: {targetFileName}");
            throw;
        }
    }

    public void NewFile()
    {
        CurrentFileName = null;
        HasUnsavedChanges = false;
        logger.LogInformation("New file started");
    }

    /// <summary>
    /// Save devices to file, preserving all properties by grouping by concrete type
    /// </summary>
    public async Task SaveDevices(List<IDevice> devices, string? fileName = null)
    {
        var targetFileName = fileName ?? CurrentFileName;

        if (string.IsNullOrWhiteSpace(targetFileName))
        {
            throw new InvalidOperationException("No filename specified");
        }

        var fullPath = GetFullPath(targetFileName);

        try
        {
            var config = new ConfigFile
            {
                PdmDevices = devices.OfType<PdmDevice>().ToList(),
                PdmMaxDevices = devices.OfType<PdmMaxDevice>().ToList(),
                CanboardDevices = devices.OfType<CanboardDevice>().ToList()
            };

            var jsonString = JsonSerializer.Serialize(config, _options);
            await File.WriteAllTextAsync(fullPath, jsonString);

            CurrentFileName = targetFileName;
            HasUnsavedChanges = false;

            logger.LogInformation($"Saved {devices.Count} devices to {targetFileName}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error saving devices to {targetFileName}");
            throw;
        }
    }

    /// <summary>
    /// Load devices from file, returning all devices as a single list
    /// </summary>
    public async Task<List<IDevice>?> LoadDevices(string fileName)
    {
        var fullPath = GetFullPath(fileName);

        if (!File.Exists(fullPath))
        {
            logger.LogError($"File not found: {fullPath}");
            return null;
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(fullPath);
            var config = JsonSerializer.Deserialize<ConfigFile>(jsonString, _options);

            if (config == null)
            {
                return null;
            }

            CurrentFileName = fileName;
            HasUnsavedChanges = false;

            var allDevices = new List<IDevice>();
            allDevices.AddRange(config.PdmDevices);
            allDevices.AddRange(config.PdmMaxDevices);
            allDevices.AddRange(config.CanboardDevices);

            logger.LogInformation($"Loaded {allDevices.Count} devices from {fileName}");
            return allDevices;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error loading devices from {fileName}");
            throw;
        }
    }

    private string GetFullPath(string fileName)
    {
        // Ensure .json extension
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".json";
        }

        // If already a full path, return it
        if (Path.IsPathRooted(fileName))
        {
            return fileName;
        }

        // Otherwise, combine with working directory
        return Path.Combine(_workingDirectory, fileName);
    }

    // Legacy methods for backward compatibility with existing code
    public async Task Open(string filename)
    {
        var jsonString = await File.ReadAllTextAsync(filename);

        List<IDevice>? devices = JsonSerializer.Deserialize<List<IDevice>>(jsonString, _options);

        if (devices != null)
        {
            manager.AddDevices(devices);
            logger.LogInformation($"Added {devices.Count} devices");
        }
        else
        {
            logger.LogError($"No devices found in {filename}");
        }
    }

    public async Task Save(string filename)
    {
        await File.WriteAllTextAsync(filename, JsonSerializer.Serialize(manager.GetDevices(), _options));

        logger.LogInformation($"Config file saved to {filename}");
    }
}