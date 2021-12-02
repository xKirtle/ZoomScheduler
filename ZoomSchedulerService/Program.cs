using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ZoomScheduler;

namespace ZoomSchedulerService
{
    public class Program
    {
        private static Mutex mutex = null;
        private static List<Tuple<Process, string>> openedMeetings;
        private Program() { }
        
        static void Main(string[] args)
        {
            //Prevent multiple instances running
            bool singleInstance;
            mutex = new Mutex(true, "ZoomSchedulerService", out singleInstance);
            if (!singleInstance)
                Environment.Exit(0);

            openedMeetings = new List<Tuple<Process, string>>();
            
            while (true)
            {
                List<ZoomMeeting> meetings = ZoomMeeting.ReadMeetings();
                
                //Check if can start any meeting
                foreach (ZoomMeeting meeting in meetings)
                {
                    string meetingStartTime = $"{meeting.StartTime.ToString(@"hh\:mm")}";
                    string meetingEndTime = $"{meeting.EndTime.ToString(@"hh\:mm")}";
                    string currentTime = DateTime.Now.ToString("HH:mm");
                    int dayOfWeek = ((int)DateTime.Today.DayOfWeek + 6) % 7; //DayOfWeek enum starts on a Sunday...
                    
                    if (meeting.Days[dayOfWeek] == 1 && meetingStartTime == currentTime)
                    {
                        string url = $"https://{(!string.IsNullOrWhiteSpace(meeting.Prefix) ? $"{meeting.Prefix}." : "")}zoom.us/j/{meeting.ID}?pwd={meeting.Password}";
                        Process zoomMeeting = Process.Start(new ProcessStartInfo()
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                        
                        if (zoomMeeting != null)
                            openedMeetings.Add(new Tuple<Process, string>(zoomMeeting, currentTime));
                    }
                }
                
                //Check if can end any meeting
                foreach (Tuple<Process,string> openedMeeting in openedMeetings)
                {
                    string currentTime = DateTime.Now.ToString("HH:mm");
                    if (openedMeeting.Item2 == currentTime)
                        openedMeeting.Item1.Close();
                }
                
                Thread.Sleep(1000 * 60);
            }
        }
    }
}