using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public override string Version => "1.2.2";
        public override string Link => "https://github.com/NepuShiro/ResoniteFixContactIcons/";

        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly Dictionary<string, Contact> ContactCache = new Dictionary<string, Contact>();

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.NepuShiro.ResoniteFixContactIcons");
            harmony.PatchAll();

            Engine.Current.OnReady += () =>
            {
                // We could probably find a Method to patch for refreshing all Contacts, but this is fine for now
                Engine.Current.Cloud.Contacts.ForeachContact(async void (c) =>
                {
                    try
                    {
                        await EnsureProfile(c);
                    }
                    catch (Exception e)
                    {
                        Error(e);
                    }
                });

                Engine.Current.Cloud.Contacts.ContactRemoved += EnsureProfile;
                Engine.Current.Cloud.Contacts.ContactAdded += EnsureProfile;
                Engine.Current.Cloud.Contacts.ContactUpdated += EnsureProfile;
            };
        }

        [HarmonyPatch(typeof(NotificationPanel), "AddNotification")]
        [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(Uri), typeof(colorX), typeof(NotificationType), typeof(string), typeof(Uri), typeof(IAssetProvider<AudioClip>) })]
        public class AddNotificationPatch
        {
            [HarmonyPrefix]
            private static void Prefix(string userId, ref Uri overrideProfile)
            {
                if (string.IsNullOrEmpty(userId) || overrideProfile != null) return;
                if (!ContactCache.TryGetValue(userId, out Contact contact) || string.IsNullOrEmpty(contact?.Profile?.IconUrl)) return;

                overrideProfile = new Uri(contact.Profile.IconUrl);
            }
        }

        private static async void EnsureProfile(ContactData cd)
        {
            try
            {
                User newUser = await EnsureProfile(cd.Contact);
                if (newUser == null) return;

                List<ContactItem> contacts = Userspace.UserspaceWorld?.RootSlot?.GetComponentsInChildren<ContactItem>(x => x.Contact == cd.Contact || x.Data == cd).ToList();
                if (contacts == null || contacts.Count <= 0) return;
                foreach (ContactItem contact in contacts)
                {
                    contact.Update(newUser);
                }
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        private static async Task<User> EnsureProfile(Contact contact)
        {
            try
            {
                ContactCache[contact.ContactUserId] = contact;

                if (contact.Profile != null) return null;

                User user = await GetUser(contact.ContactUserId);
                if (user?.Profile == null) return null;

                contact.Profile = user.Profile;

                return user;
            }
            catch (Exception e)
            {
                Error(e);
                return null;
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