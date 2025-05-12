using HarmonyLib;
using pworld.Scripts.Extensions;
using System.Collections;
using System.Reflection;
using UnityEngine;
using static Mono.Security.X509.X520;

namespace HourGlassSounds;

public class Config
{
	public Config(HastySetting cfg)
	{
		EffectVolume = new(cfg, "Effect Volume", "Changed how loud the effects are.", new(0, 1, 0.5f));

		/// <summary>
		/// This creates an option that contains each type in the <seealso cref="SoundsSelection"/> enum.
		/// </summary>
		HastyEnum<SoundsSelection> soundsSelection = new(cfg, "Gravity Control", "Choose the pool of sounds for the Hourglass", new()
		{
			DefaultValue = SoundsSelection.Natural,
			Choices = Enum.GetNames(typeof(SoundsSelection)).AsEnumerable(),
			OnLoad = ChooseSounds,
			OnApplied = (SoundsSelection sel) => { ChooseSounds(sel); UnityEngine.Debug.LogWarning("Sounds selected: " + string.Join(", ", Main.Sounds)); },
		});
	}

	public enum SoundsSelection
	{
		None,
		Natural,
		All,
		MegaloStrip,
		BotanicalKingdom,
		MeteortechPremises,
		AquaticCapital,
		GiganRocks,
		CrimsonCrater,
		EightiesBoulevard
	}

	public static HastyFloat EffectVolume { get; set; } = null!;

	private void ChooseSounds(SoundsSelection sel)
	{
		// Clears the list for our new sounds
		Main.Sounds.Clear();

		// Basically a better if statement
		switch (sel)
		{
			case SoundsSelection.None:
				break;  // Just breaks / returns / exits it, otherwise it chooses the 'default' one.

			case SoundsSelection.All:
				Main.TryLoadClips(["Energy Booster", "Energy Booster 2", "Energy Booster 3", "Energy Booster 4", "Energy Booster 5", "Energy Booster 6", "Energy Booster 7"]);
				break;

			case SoundsSelection.Natural:
			default:    // This means if the SoundsSelection is somehow invalid, it defaults to this one. Additionally, we can combine it with the Natural option.
				Main.TryLoadClips(["Energy Booster", "Energy Booster 2", "Energy Booster 3", "Energy Booster 6"]);
				break;

			case SoundsSelection.MegaloStrip:
				Main.TryLoadClips(["Energy Booster"]);
				break;

			case SoundsSelection.BotanicalKingdom:
				Main.TryLoadClips(["Energy Booster 2"]);
				break;

			case SoundsSelection.MeteortechPremises:
				Main.TryLoadClips(["Energy Booster 3"]);
				break;

			case SoundsSelection.AquaticCapital:
				Main.TryLoadClips(["Energy Booster 4"]);
				break;

			case SoundsSelection.GiganRocks:
				Main.TryLoadClips(["Energy Booster 5"]);
				break;

			case SoundsSelection.CrimsonCrater:
				Main.TryLoadClips(["Energy Booster 6"]);
				break;

			case SoundsSelection.EightiesBoulevard:
				Main.TryLoadClips(["Energy Booster 7"]);
				break;
		}

		// Add the Swoosh sound effect last if it's not a None type.
		if (sel != SoundsSelection.None)
		{ Main.TryLoadClips(["SRZG Swoosh"]); }
	}
}

[Landfall.Modding.LandfallPlugin]
public class Main
{
	static Main()
	{
		Harmony = new Harmony(ModID);
		Harmony.PatchAll(typeof(Patches));
		if (LoadBundle()) throw new Exception("[ZGHourglass]: Failed to load asset bundle");
		var menu = new HastySetting("ZGHourglass");
		HastySetting.OnConfig += () => new Config(menu);
	}

	public static GameObject bellObject { get; set; } = null!;
	public static Harmony Harmony { get; private set; } = null!;
	public static string ModID { get; private set; } = "bam.haste.ZGHourglass";
	public static List<AudioClip> Sounds { get; private set; } = new();
	private static AssetBundle assetBundle { get; set; } = null!;

	/// <summary>
	/// This tries to get every sound that is in the array and adds them to the <seealso cref="Main.Sounds"/> list!
	/// </summary>
	/// <param name="AssetNames"></param>
	public static void TryLoadClips(string[] AssetNames)
	{
		if (assetBundle == null)
		{ Debug.LogError("AssetBundle is not loaded."); return; }

		foreach (string assetName in AssetNames)
		{
			AudioClip? asset = assetBundle.LoadAsset<AudioClip>(assetName);
			if (asset == null)
			{ Debug.LogWarning($"Asset '{assetName}' not found in bundle."); return; }
			Sounds.Add(asset);
		}
	}

	private static bool LoadBundle()
	{
		try
		{
			// If the bundle has already loaded, we just skip ig
			if (assetBundle) return true;

			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HourGlassSounds.Assets.hourglassgravity"))
			{
				if (stream == null)
				{ Debug.LogError("AssetBundle resource not found!"); return false; }

				byte[] bundleData = new byte[stream.Length];
				stream.Read(bundleData, 0, bundleData.Length);

				// Load the AssetBundle from memory
				if ((assetBundle = AssetBundle.LoadFromMemory(bundleData)) == null)
				{ Debug.LogError("Failed to load AssetBundle from memory."); return false; }

				Debug.Log("AssetBundle loaded from embedded resource!");
			}
		}
		catch (System.Exception e) { UnityEngine.Debug.LogError(e); }
		return false;
	}
}

public class Patches
{
	[HarmonyPatch(typeof(Ability_Slomo), "OnEnable")]
	[HarmonyPostfix]
	private static void Postfix(Ability_Slomo __instance)
	{
		Main.bellObject = __instance.gameObject;

		// We either add the component or skip it.
		// Easier if we just use the GetOrAddComponent method
		Main.bellObject.GetOrAddComponent<BellHandler>();
	}
}

public class BellHandler : MonoBehaviour
{
	private bool hasPressed { get; set; } = false;
	private AudioSource MainEffect { get; set; } = null!;
	private AudioSource Music { get; set; } = null!;
	private AudioLowPassFilter MusicFilter { get; set; } = null!;
	private AudioSource Swoosh { get; set; } = null!;

	private IEnumerator FadeOutEffect(float duration)
	{
		float startVolume = MainEffect.volume;

		while (MainEffect.volume > 0)
		{
			MainEffect.volume -= startVolume * Time.deltaTime / duration;
			yield return null;
		}

		MainEffect.volume = 0;
	}

	private void Start()
	{
		MainEffect = gameObject.AddComponent<AudioSource>();
		Swoosh = gameObject.AddComponent<AudioSource>();

		Music = GameObject.Find("PersistentObjects/Handlers/Music").GetComponent<AudioSource>();
		MusicFilter = Music.gameObject.GetOrAddComponent<AudioLowPassFilter>();
		MusicFilter.cutoffFrequency = 5000;
	}

	private void Update()
	{
		if (PlayerCharacter.localPlayer.input.abilityIsPressed && Player.localPlayer.data.energy > 0f)
		{
			if (!hasPressed)
			{
				hasPressed = true;

				AudioClip randomSound = Main.Sounds.Take(Main.Sounds.Count - 1).OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
				MainEffect.clip = randomSound;

				MainEffect.volume = Config.EffectVolume.Value;
				MainEffect.Play();

				Swoosh.volume = Config.EffectVolume.Value;
				MusicFilter.cutoffFrequency = 800;
			}
		}
		else
		{
			if (hasPressed)
			{
				StartCoroutine(FadeOutEffect(0.4f));
				MusicFilter.cutoffFrequency = 5000;
				Swoosh.PlayOneShot(Main.Sounds.Last());
			}
			hasPressed = false;
		}
	}
}
