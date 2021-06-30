using System;
using System.Linq;
using System.Text;

namespace ZoomScheduler
{
    public class ZoomMeeting
    {
        public string Name { get; set; } = "";
        public ulong ID { get; set; }
        public string Password { get; set; } = "";
        public TimeSpan Time { get; set; }
        public int[] Days { get; set; } = new int[7];

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
            if (!string.IsNullOrWhiteSpace(password))
            {
                Password = password;
                return true;
            }

            return false;
        }

        public bool setTime(TimeSpan? time)
        {
            if (time != null)
            {
                Time = (TimeSpan)time;
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
    }
}