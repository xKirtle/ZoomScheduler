using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZoomScheduler;

namespace ZoomSchedulerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
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
                await Task.Delay(1000 * 60, stoppingToken);
            }
        }
    }
}