using HarmonyLib;
using UnityEngine;
using HasteEffects;
using System.Reflection;
using System.Collections;
using pworld.Scripts.Extensions;

namespace HourGlassSounds;

public class Config
{
	public Config()
	{
		HastySetting cfg = new("ZG Hourglass", Main.ModID);
		EffectVolume = new HastyFloat(cfg, "Effect volume", "Changes how loud the effects are", 0, 1, 0.5f);
	}

	public static HastyFloat EffectVolume { get; set; }
}

[Landfall.Modding.LandfallPlugin]
public class Main
{
	static Main()
	{
		Harmony = new Harmony(ModID);
		Harmony.PatchAll(typeof(Patches));
		LoadBundle();
	}

	public static GameObject bellObject { get; set; }
	public static Harmony Harmony { get; private set; }
	public static string ModID { get; private set; } = "bam.haste.ZGHourglass";
	public static Dictionary<string, AudioClip> Sounds { get; private set; } = new();

	public static void LoadBundle()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();

		string resourceName = "HourGlassSounds.Assets.hourglassgravity";

		using (Stream stream = assembly.GetManifestResourceStream(resourceName))
		{
			byte[] bundleData = new byte[stream.Length];
			stream.Read(bundleData, 0, bundleData.Length);

			AssetBundle myBundle = AssetBundle.LoadFromMemory(bundleData);

			AudioClip[] audioClips = myBundle.LoadAllAssets<AudioClip>();

			Sounds.Add("EnergyBoost1", audioClips.First(audioClip => audioClip.name == "Energy Booster"));
			Sounds.Add("EnergyBoost2", audioClips.First(audioClip => audioClip.name == "Energy Booster 2"));
			Sounds.Add("EnergyBoost3", audioClips.First(audioClip => audioClip.name == "Energy Booster 3"));
			Sounds.Add("EnergyBoost4", audioClips.First(audioClip => audioClip.name == "Energy Booster 6"));
			Sounds.Add("Swoosh", audioClips.First(audioClip => audioClip.name == "SRZG Swoosh"));
		}
	}
}

public class Patches
{
	public static event Action OnSlowmoUpdate;

	[HarmonyLib.HarmonyPatch(typeof(Zorro.Localization.LocalizeUIText), "OnStringChanged")]
	[HarmonyLib.HarmonyPostfix]
	private static void OnStringChangedPostfix(Zorro.Localization.LocalizeUIText __instance)
	{
		// Ensure the string's table reference matches the mod's GUID
		if (__instance.String?.TableReference.TableCollectionName != Main.ModID) return;
		__instance.Text.text = __instance.String.TableEntryReference.Key;
	}

	[HarmonyPatch(typeof(Ability_Slomo), "Update")]
	[HarmonyPostfix]
	private static void OnUpdatePostFix() => OnSlowmoUpdate?.Invoke();

	[HarmonyPatch(typeof(Ability_Slomo), "OnEnable")]
	[HarmonyPostfix]
	private static void Postfix(Ability_Slomo __instance)
	{
		Main.bellObject = __instance.gameObject;

		// We either add the component or skip it.
		// Easier if we just use the GetOrAddComponent method
		Main.bellObject.GetOrAddComponent<BellHandler>();
	}

	[HarmonyLib.HarmonyPatch(typeof(HasteSettingsHandler), "RegisterPage")]
	[HarmonyLib.HarmonyPrefix]
	private static void RegisterPagePrefix(HasteSettingsHandler __instance) => new Config();
}

public class BellHandler : MonoBehaviour
{
	private bool hasPressed = false;
	private AudioSource MainEffect { get; set; }
	private AudioSource Music { get; set; }
	private AudioLowPassFilter MusicFilter { get; set; }
	private AudioSource Swoosh { get; set; }

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

	private void OnDestroy() => Patches.OnSlowmoUpdate -= OnUpdate;

	private void OnUpdate()
	{
		if (PlayerCharacter.localPlayer.input.abilityIsPressed && Player.localPlayer.data.energy > 0f)
		{
			if (!hasPressed)
			{
				hasPressed = true;

				AudioClip randomSound = Main.Sounds.Take(Main.Sounds.Count - 1).OrderBy(x => UnityEngine.Random.value).FirstOrDefault().Value;

				MainEffect.clip = randomSound;
				MainEffect.volume = Config.EffectVolume.Value;
				Swoosh.volume = Config.EffectVolume.Value;
				MainEffect.Play();

				MusicFilter.cutoffFrequency = 800;
			}
		}
		else
		{
			if (hasPressed)
			{
				StartCoroutine(FadeOutEffect(0.4f));
				MusicFilter.cutoffFrequency = 5000;
				Swoosh.PlayOneShot(Main.Sounds["Swoosh"]);
			}
			hasPressed = false;
		}
	}

	private void Start()
	{
		Patches.OnSlowmoUpdate += OnUpdate;

		MainEffect = gameObject.AddComponent<AudioSource>();
		Swoosh = gameObject.AddComponent<AudioSource>();

		Music = GameObject.Find("PersistentObjects/Handlers/Music").GetComponent<AudioSource>();

		// Same problem here, we kept adding the component expecting it to just vanish
		// So we either add or just skip it using the GetOrAddComponent method.
		MusicFilter = Music.gameObject.GetOrAddComponent<AudioLowPassFilter>();

		MusicFilter.cutoffFrequency = 5000;
	}
}
