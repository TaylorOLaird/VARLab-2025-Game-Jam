public static class WaterSceneState
{
    private static bool _isWaterEnabled;

    public static event System.Action<bool> OnWaterEnabledChanged;

    public static bool IsWaterEnabled
    {
        get => _isWaterEnabled;
        set
        {
            if (_isWaterEnabled != value) // only trigger if actually changed
            {
                _isWaterEnabled = value;
                OnWaterEnabledChanged?.Invoke(_isWaterEnabled);
            }
        }
    }
}
