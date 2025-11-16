namespace domain.Devices.dingoPdm.Enums;

public enum MessagePrefix
{
    Null = 0,
    Can = 1,
    Inputs = 5,
    Outputs = 10,
    OutputsPwm = 11,
    VirtualInputs = 15,
    Wiper = 20,
    WiperSpeed = 21,
    WiperDelays = 22,
    Flashers = 25,
    StarterDisable = 30,
    CanInputs = 35,
    CanInputsId = 36,
    Counter = 40,
    Conditions = 45,
    Version = 120,
    Sleep = 121,
    Bootloader = 125,
    BurnSettings = 127
}