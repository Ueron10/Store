namespace StoreProgram.Models;

public class ExpiryAlertSetting
{
    public int DaysBeforeExpiryWarning { get; set; } = 7;
    public int DaysBeforeExpiryMax { get; set; } = 30;
}
