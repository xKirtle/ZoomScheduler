using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ZoomScheduler;

namespace ZoomSchedulerService
{
    class Program
    {
        private static Mutex mutex = null;
        static void Main(string[] args)
        {
            //Prevent multiple instances running
            bool singleInstance;
            mutex = new Mutex(true, "ZoomSchedulerService", out singleInstance);
            if (!singleInstance)
                Environment.Exit(0);
            
            
            //TODO: Implement close process on End Meeting
            while (true)
            {
                List<ZoomMeeting> meetings = ZoomMeeting.ReadMeetings();
                foreach (ZoomMeeting meeting in meetings)
                {
                    string meetingStartTime = $"{meeting.StartTime.ToString(@"hh\:mm")}";
                    string currentTime = DateTime.Now.ToString("HH:mm");
                    int dayOfWeek = ((int)DateTime.Today.DayOfWeek + 6) % 7; //DayOfWeek enum starts on a Sunday...
                    
                    if (meeting.Days[dayOfWeek] == 1 && meetingStartTime == currentTime)
                    {
                        string url = $"https://{(!string.IsNullOrWhiteSpace(meeting.Prefix) ? $"{meeting.Prefix}." : "")}zoom.us/j/{meeting.ID}?pwd={meeting.Password}";
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                }
                Thread.Sleep(1000 * 60);
            }
        }
    }
}