namespace StoreProgram.Models;

public class ExpiryAlertSetting
{
    // "Mendekati kadaluarsa" = sisa <= 14 hari
    public int DaysBeforeExpiryWarning { get; set; } = 0;
    public int DaysBeforeExpiryMax { get; set; } = 14;
}
