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
        
        private static readonly HttpClient HttpClient = new HttpClient();

        public override void OnEngineInit()
        {
            // Harmony harmony = new Harmony("net.NepuShiro.ResoniteFixContactIcons");
            // harmony.PatchAll();
            
            Engine.Current.OnReady += () =>
            {
                // We could probably find a Method to patch for refreshing all Contacts, but this is fine for now
                Engine.Current.Cloud.Contacts.ForeachContact(EnsureProfile);
                
                Engine.Current.Cloud.Contacts.ContactRemoved += EnsureProfile;
                Engine.Current.Cloud.Contacts.ContactAdded += EnsureProfile;
                Engine.Current.Cloud.Contacts.ContactUpdated += EnsureProfile;
            };
        }
        
        // I don't think this is needed, as I can't find a good way to patch this without halting the main thread..
        // [HarmonyPatch(typeof(NotificationPanel), "AddNotification")]
        // [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(Uri), typeof(colorX), typeof(NotificationType), typeof(string), typeof(Uri), typeof(IAssetProvider<AudioClip>) })]
        // public class AddNotificationPatch
        // {
        //     [HarmonyPrefix]
        //     private static void Prefix(string userId, ref Uri overrideProfile)
        //     {
        //         if (string.IsNullOrEmpty(userId)) return;
        //         if (overrideProfile != null) return;
        //         
        //         UserProfile profile = EnsureProfile(null, userId).GetAwaiter().GetResult();
        //         overrideProfile = new Uri(profile.IconUrl);
        //     }
        // }

        // There could be a better Method to patch instead of the Contacts Panel to update the Profiles
        // We might not even need this
        // [HarmonyPatch(typeof(ContactItem), "Update")]
        // [HarmonyPatch(new Type[] { typeof(Contact), typeof(ContactData) })]
        // public static class ContactsPagePatch
        // {
        //     [HarmonyPrefix]
        //     public static void Prefix(ContactItem __instance)
        //     {
        //         Contact contact = __instance?.Contact ?? __instance?.Data?.Contact;
        //         
        //         if (contact != null)
        //             EnsureProfile(contact);
        //     }
        // }
        
        private static void EnsureProfile(ContactData cd)
        {
            EnsureProfile(cd.Contact);
        }
        
        private static async void EnsureProfile(Contact contact)
        {
            try
            {
                if (contact.Profile != null) return;

                User user = await GetUser(contact.ContactUserId);
                if (user?.Profile == null) return;

                contact.Profile = user.Profile;
            }
            catch (Exception e)
            {
                Error(e);
            }
        }
        private static async Task<User> GetUser(string userId)
        {
            HttpResponseMessage response = await HttpClient.GetAsync($"{Engine.Current.Cloud.ApiEndpoint}/users/{userId}").ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<User>(await response.Content.ReadAsStringAsync())
                : null;
        }
    }
}
