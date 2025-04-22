namespace HasteEffects;

// Very bare bones and old version. Dont worry.

public class HastyFloat : Zorro.Settings.FloatSetting, IExposedSetting
{
	private readonly HastySetting _config;
	private readonly float _defaultValue;
	private readonly UnityEngine.Localization.LocalizedString _displayName;
	private readonly Unity.Mathematics.float2 _minMax;

	public HastyFloat(HastySetting config, string name, string description, float min, float max, float defaultValue)
	{
		_config = config;
		_defaultValue = defaultValue;
		_minMax = new Unity.Mathematics.float2(min, max);
		_displayName = _config.CreateDisplayName(name, description);
		_config.Add(this);
	}

	public event Action<float>? Applied;

	public override void ApplyValue() => Applied?.Invoke(Value);

	public string GetCategory() => _config.ModName;

	public UnityEngine.Localization.LocalizedString GetDisplayName() => _displayName;

	UnityEngine.Localization.LocalizedString IExposedSetting.GetDisplayName() => _displayName;

	public void Reset() => Value = _defaultValue;

	protected override float GetDefaultValue() => _defaultValue;

	protected override Unity.Mathematics.float2 GetMinMaxValue() => _minMax;
}

public class HastySetting
{
	private static HarmonyLib.AccessTools.FieldRef<HasteSettingsHandler, List<Zorro.Settings.Setting>> settingsRef =
		HarmonyLib.AccessTools.FieldRefAccess<HasteSettingsHandler, List<Zorro.Settings.Setting>>("settings");

	private static HarmonyLib.AccessTools.FieldRef<HasteSettingsHandler, Zorro.Settings.ISettingsSaveLoad> settingsSaveLoadRef =
		HarmonyLib.AccessTools.FieldRefAccess<HasteSettingsHandler, Zorro.Settings.ISettingsSaveLoad>("_settingsSaveLoad");

	public HastySetting(string modName, string modGUID)
	{
		ModName = modName;
		SettingsUIPage.LocalizedTitles.Add(ModName, new(HourGlassSounds.Main.ModID, ModName));
	}

	public string ModName { get; private set; }

	public void Add<T>(T setting) where T : Zorro.Settings.Setting
	{
		var handler = GameHandler.Instance.SettingsHandler;
		settingsRef(handler).Add(setting);
		setting.Load(settingsSaveLoadRef(handler));
		setting.ApplyValue();
	}

	internal UnityEngine.Localization.LocalizedString CreateDisplayName(string name, string description = "") =>
		new(HourGlassSounds.Main.ModID, $"{name}\n<size=60%><alpha=#50>{description}");
}
