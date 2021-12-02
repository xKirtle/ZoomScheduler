using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ZoomScheduler
{
    public class ZoomMeeting
    {
        public string Name { get; set; } = "";
        public ulong ID { get; set; }
        public string Password { get; set; } = "";
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int[] Days { get; set; } = new int[7];
        public string Prefix { get; set; } = "";

        public ZoomMeeting()
        {
            
        }

        public bool setName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Name = name;
                return true;
            }

            return false;
        }

        public bool setId(string id)
        {
            if (ulong.TryParse(id, out ulong idLong))
            {
                ID = idLong;
                return true;
            }

            return false;
        }

        public bool setPassword(string password)
        {
            Password = password == null ? "" : password;
            return true;
        }

        public bool setStartTime(TimeSpan? time)
        {
            if (time != null)
            {
                StartTime = (TimeSpan)time;
                return true;
            }

            return false;
        }

        public bool setEndTime(TimeSpan? time)
        {
            if (time != null)
            {
                EndTime = (TimeSpan) time;
                return true;
            }

            return false;
        }

        public bool setDays(int[] days)
        {
            if (days.Contains(1))
            {
                Days = days;
                return true;
            }

            return false;
        }

        public bool setPrefix(string prefix)
        {
            Prefix = prefix;
            return true;
        }

        public static void ScheduleMeeting(ZoomMeeting meeting)
        {
            List<ZoomMeeting> meetings = ReadMeetings();
            meetings.Add(meeting);
            
            string json = JsonConvert.SerializeObject(meetings);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZoomScheduler");
            Directory.CreateDirectory(path);
            
            using (FileStream fs = new FileStream(Path.Combine(path, "Meetings.json"), FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs)) 
                sw.WriteLine(json);
        }

        public static List<ZoomMeeting> ReadMeetings()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZoomScheduler");
            if (!Directory.Exists(path) || !File.Exists(Path.Combine(path, "Meetings.json"))) return new List<ZoomMeeting>();

            string json;
            using (FileStream fs = new FileStream(Path.Combine(path, "Meetings.json"), FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
                json = sr.ReadToEnd();
            
            return JsonConvert.DeserializeObject<List<ZoomMeeting>>(json) ?? new List<ZoomMeeting>();
        }

        public static void UnscheduleMeeting(int index)
        {
            List<ZoomMeeting> meetings = ReadMeetings();
            meetings.RemoveAt(index);
            
            string json = JsonConvert.SerializeObject(meetings);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZoomScheduler");
            Directory.CreateDirectory(path);
            
            using (FileStream fs = new FileStream(Path.Combine(path, "Meetings.json"), FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs)) 
                sw.WriteLine(json);
        }
    }
}