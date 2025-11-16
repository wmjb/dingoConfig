namespace domain.Devices.dingoPdm.Enums;

public enum MessageSrc
{
    StatePowerOn = 1,
    StateStarting,
    StateRun,
    StateOvertemp,
    StateError,
    StateSleep,
    StateWake,
    OverCurrent,
    BatteryVoltage,
    CAN,
    USB,
    OverTemp,
    Config,
    FRAM,
    ADC,
    I2C,
    TempSensor,
    USBConnected
}