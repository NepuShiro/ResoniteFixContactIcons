using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace ExampleMod;
public class ExampleMod : ResoniteMod
{
	internal const string VERSION_CONSTANT = "1.0.0";
	public override string Name => "ExampleMod";
	public override string Author => "ExampleAuthor";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/ExampleAuthor/ExampleMod/";

	[AutoRegisterConfigKey]
	private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabled", "Should ExampleMod be Enabled?", () => true);

	private static ModConfiguration config;

	public override void OnEngineInit()
	{
		config = GetConfiguration();

		Harmony harmony = new Harmony("com.example.ExampleMod");
		harmony.PatchAll();
	}

	[HarmonyPatch(typeof(ClassNameHere), "MethodNameHere")]
	class ClassNameHere_MethodNameHere_Patch
	{
		static void Postfix(ClassNameHere __instance)
		{
			Msg("Postfix from ExampleMod");
		}
	}
}
