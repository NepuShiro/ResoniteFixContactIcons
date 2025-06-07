using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Threading.Tasks;
using System;
using SkyFrost.Base;
using System.Net.Http;
using System.Text.Json;
using User = SkyFrost.Base.User;

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
            Harmony harmony = new Harmony("net.NepuShiro.ResoniteFixContactIcons");
            harmony.PatchAll();
            
            Engine.Current.OnReady += () =>
            {
                Engine.Current.Cloud.Contacts.ForeachContact(c =>
                {
                    EnsureProfile(c);
                });
            };
        }

        [HarmonyPatch(typeof(NotificationPanel), "AddNotification")]
        [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(Uri), typeof(colorX), typeof(NotificationType), typeof(string), typeof(Uri), typeof(IAssetProvider<AudioClip>) })]
        public class AddNotificationPatch
        {
            [HarmonyPrefix]
            private static void Prefix(string userId, ref Uri overrideProfile)
            {
                if (string.IsNullOrEmpty(userId)) return;
                if (overrideProfile != null) return;
                
                UserProfile profile = EnsureProfile(null, userId).GetAwaiter().GetResult();
                overrideProfile = new Uri(profile.IconUrl);
            }
        }

        [HarmonyPatch(typeof(ContactItem), "Update")]
        [HarmonyPatch(new Type[] { typeof(Contact), typeof(ContactData) })]
        public static class ContactsPagePatch
        {
            [HarmonyPrefix]
            public static void Prefix(ContactItem __instance)
            {
                Contact contact = __instance?.Contact ?? __instance?.Data?.Contact;
                
                if (contact != null)
                    EnsureProfile(contact);
            }
        }
        
        private static async Task<UserProfile> EnsureProfile(Contact contact = null, string userId = "")
        {
            try
            {
                if (contact?.Profile != null)
                    return null;

                if (contact == null && !string.IsNullOrEmpty(userId))
                    contact = Engine.Current.Cloud.Contacts.GetContact(userId);

                if (contact == null)
                    return null;

                User user = await GetUser(contact.ContactUserId);
                if (user?.Profile == null)
                    return null;

                contact.Profile = user.Profile;

                return contact.Profile;
            }
            catch (Exception e)
            {
                Error(e);
                return null;
            }
        }
        
        private static async Task<User> GetUser(string userId)
        {
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync($"https://api.resonite.com/users/{userId}").ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<User>(await response.Content.ReadAsStringAsync())
                : null;
        }
    }
}
