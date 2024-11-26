using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Threading.Tasks;
using System;
using SkyFrost.Base;
using System.Collections.Generic;
using FrooxEngine.UIX;
using System.Linq;
using System.Reflection.Emit;

namespace ResoniteFixContactIcons
{
    public class ResoniteFixContactIcons : ResoniteMod
    {
        public override string Name => "ResoniteFixContactIcons";
        public override string Author => "NepuShiro";
        public override string Version => "1.0.1";
        public override string Link => "https://github.com/NepuShiro/ResoniteFixContactIcons/";

        public override void OnEngineInit()
        {
            Harmony harmony = new("net.NepuShiro.ResoniteFixContactIcons");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(NotificationPanel), "AddNotification")]
        [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(Uri), typeof(colorX), typeof(NotificationType), typeof(string), typeof(Uri), typeof(IAssetProvider<AudioClip>) })]
        public class AddNotificationPatch
        {
            [HarmonyPrefix]
            private static void Prefix(NotificationPanel __instance, ref string userId, ref Uri overrideProfile)
            {
                Uri newOverrideProfile = null;
                string inUserId = userId;
                
                __instance.StartTask(async delegate
                {
                    if (Uri.TryCreate(((await Engine.Current.Cloud.Users.GetUser(inUserId))?.Entity?.Profile)?.IconUrl, UriKind.Absolute, out var result))
                    {
                        newOverrideProfile = result;
                    }
                }).GetAwaiter().GetResult();
                
                if (newOverrideProfile != null)
                {
                    overrideProfile = newOverrideProfile;
                }
            }
        }

        [HarmonyPatch(typeof(ContactItem), "Update")]
        [HarmonyPatch(new Type[] { typeof(Contact), typeof(ContactData) })]
        public static class ContactsPagePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ContactItem __instance, Contact contact, SyncRef<StaticTexture2D> ____thumbnailTexture, SyncRef<Image> ____thumbnail)
            {
                __instance.StartTask(async delegate
                {
                    if (____thumbnailTexture.Target.URL.Value == null)
                    {
                        if (Uri.TryCreate(((await Engine.Current.Cloud.Users.GetUser(contact.ContactUserId))?.Entity?.Profile)?.IconUrl, UriKind.Absolute, out var result))
                        {
                            ____thumbnailTexture.Target.URL.Value = result;
                            ____thumbnail.Target.Tint.Value = colorX.White;
                        }
                        
                        if (____thumbnailTexture.Target.URL.Value == null)
                        {
                            ____thumbnailTexture.Target.URL.Value = OfficialAssets.Graphics.Thumbnails.AnonymousHeadset;
                            ____thumbnail.Target.Tint.Value = LegacyUIStyle.NameTint(contact.ContactUserId);
                        }
                    }
                });
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    // I know this is cursed, but it works :)
                    if (codes[i].opcode == OpCodes.Ldarg_0 &&
                        codes[i + 1].opcode == OpCodes.Ldfld &&
                        codes[i + 2].opcode == OpCodes.Callvirt &&
                        codes[i + 3].opcode == OpCodes.Ldfld &&
                        codes[i + 4].opcode == OpCodes.Call &&
                        codes[i + 5].opcode == OpCodes.Callvirt &&
                        codes[i + 6].opcode == OpCodes.Ldarg_0 &&
                        codes[i + 7].opcode == OpCodes.Ldfld &&
                        codes[i + 8].opcode == OpCodes.Callvirt &&
                        codes[i + 9].opcode == OpCodes.Ldfld &&
                        codes[i + 10].opcode == OpCodes.Ldarg_1 &&
                        codes[i + 11].opcode == OpCodes.Callvirt &&
                        codes[i + 12].opcode == OpCodes.Ldc_R4 &&
                        codes[i + 13].opcode == OpCodes.Call &&
                        codes[i + 14].opcode == OpCodes.Callvirt)
                    {
                        for (int j = 0; j <= 14; j++)
                        {
                            codes[i + j].opcode = OpCodes.Nop;
                        }
                        Msg($"Replaced the No-Bueno Icon Code at [{i}] {codes[i]} - [{i + 14}] {codes[i + 14]}");
                    }
                }
                
                return codes.AsEnumerable();
            }
        }
    }
}
