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
        static void Main(string[] args)
        {
            while (true)
            {
                List<ZoomMeeting> meetings = ZoomMeeting.ReadMeetings();
                foreach (ZoomMeeting meeting in meetings)
                {
                    string meetingTime = $"{meetings[0].Time.Hours}:{meetings[0].Time.Minutes}";
                    string currentTime = DateTime.Now.ToString("HH:mm");
                    int dayOfWeek = ((int)DateTime.Today.DayOfWeek + 6) % 7; //DayOfWeek enum starts on a Sunday...
                    
                    if (meeting.Days[dayOfWeek] == 1 && meetingTime == currentTime)
                    {
                        string url = $"https://{(meeting.Prefix ? "videoconf-colibri." : "")}zoom.us/j/{meeting.ID}?pwd={meeting.Password}";
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