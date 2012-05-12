namespace LCD.Emulator
{
    public enum Command : byte
    {
        Firmware            =   1, // Causes device to reset into a firmware / bootloader mode
        //ResetDevice         =   2,
        ReadDisplay         =   3, // Returns 1 byte, with the 3 LSB being the current display, cursor and blink
        ReadDisplayMin      =   4, // Returns 1 byte, the current remaining time till display off, or 0
        ReadContrast        =   5, // Returns 1 byte, the current contrast level
        ReadBacklight       =   6, // Returns 1 byte, the current backlight level
        ReadCustom          =   7, // [char:0-7] Returns 8 bytes, the 8 bytes of the current char
        ReadMessage         =   8, // Returns 80 bytes, the current message
        ReadGPO             =   9, // [1-5] Returns 1 byte, the current state of the GPO
        ReadGPOpwm          =  10, // [1-5] Returns 1 byte, the current pwm of the GPO
        ReadSavedDisplay    =  13, // Returns 1 byte, with the 3 LSB being the saved display, cursor and blink
        ReadSavedDisplayMin =  14, // Returns 1 byte, the saved time till display off, or 0
        ReadSavedContrast   =  15, // Returns 1 byte, the saved contrast level
        ReadSavedBacklight  =  16, // Returns 1 byte, the saved backlight level
        ReadSavedCustom     =  17, // [char:0-7] Returns 8 bytes, the 8 bytes of the saved char
        ReadSavedMessage    =  18, // Returns 160 bytes, the saved startup message
        ReadSavedGPO        =  19, // [1-5] Returns 1 byte, the current state of the GPO
        ReadSavedGPOpwm     =  20, // [1-5] Returns 1 byte, the current pwm of the GPO
        SetLargeDisplay     =  21, // [0-1] 1 for a display that is large (>80 characters); this setting is always remembered
        IsLargeDisplay      =  22, // Return 1 byte, 1 for large display, 0 otherwise
        SetSerialNum        =  52, // [2 bytes], can be called any number of times
        ReadSerialNum       =  53, // Returns 2 bytes
        ReadVersion         =  54, // Returns 1 byte, the version of the firmware (major version in high nibble, minor version in low nibble)
        ReadModuleType      =  55, // Returns 1 byte, exactly 0x5B to identify this software
        SaveStartup         =  64, // [160 chars] (spec says 40, but we want to be able to use 40x4)
        DisplayOn           =  66, // [mins:0-100]
        DisplayOff          =  70,
        Position            =  71, // [col][row]
        Home                =  72,
        CursorOn            =  74,
        CursorOff           =  75,
        CursorLeft          =  76,
        CursorRight         =  77,
        DefineCustom        =  78, // [char:0-7][8 bytes]
        Contrast            =  80, // [0-255]
        BlinkOn             =  83,
        BlinkOff            =  84,
        GPOoff              =  86, // [1-5]
        GPOon               =  87, // [1-5]
        ClearDisplay        =  88,
        Backlight_          =  89, // [0-255], duplicate of 152
        GPOpwm              = 102, // [1-5][0-255]
        SaveBacklight       = 145, // [0-255]
        Remember            = 147, // [0-1]
        Backlight           = 152, // [0-255]
        GPOpwm_             = 192, // [1-5][0-255], duplicate of 102
        ReadButton          = 193, // [1-5], returns one character A-E (this is originally for reading fan RPM) or X if not pressed
        RememberCustom      = 194, // [char:0-7][8 bytes]
        RememberGPOpwm      = 195, // [1-5][0-255]
        RememberGPO         = 196, // [1-5][0-1]
        Char254             = 254,
    }
}
