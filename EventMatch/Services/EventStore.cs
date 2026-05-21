using EventMatch.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace EventMatch.Services
{
    public class EventStore
    {
        private const string EventsKey = "StoredEvents";

        public List<Event> LoadAll()
        {
            var json = Preferences.Get(EventsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new List<Event>();

            try
            {
                var all = JsonSerializer.Deserialize<List<Event>>(json) ?? new List<Event>();
                return all;
            }
            catch
            {
                return new List<Event>();
            }
        }

        public void SaveAll(List<Event> events)
        {
            var json = JsonSerializer.Serialize(events);
            Preferences.Set(EventsKey, json);
        }

        public void Add(Event e)
        {
            var all = LoadAll();
            all.Add(e);
            SaveAll(all);
        }

        public void DeleteAll()
        {
            Preferences.Remove(EventsKey);
        }
    }
}
