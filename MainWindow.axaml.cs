using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using JetBrains.Annotations;

namespace ZoomScheduler
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            
            Button scheduleMeeting = this.FindControl<Button>("ScheduleMeeting_Button");
            scheduleMeeting.Click += ScheduleMeeting_Button_OnClick;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ScheduleMeeting_Button_OnClick(object? sender, RoutedEventArgs e)
        {
            TextBox info = this.FindControl<TextBox>("MeetingInfo_TextBox");
            TextBox id = this.FindControl<TextBox>("MeetingId_TextBox");
            TextBox password = this.FindControl<TextBox>("MeetingPwd_TextBox");
            TimePicker time = this.FindControl<TimePicker>("MeetingSelectedTime");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays");

            string[] lbNames = {"Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"};
            int[] days = new int[lbNames.Length];
            for (int i = 0; i < lbNames.Length; i++)
                days[i] = this.FindControl<ListBoxItem>(lbNames[i]).IsSelected ? 1 : 0;

            ZoomMeeting meeting = new ZoomMeeting();
            if (!meeting.setName(info.Text))
                ScheduleMeetingInvalidArg(info);
            if (!meeting.setId(id.Text))
                ScheduleMeetingInvalidArg(id);
            if (!meeting.setPassword(password.Text))
                ScheduleMeetingInvalidArg(password);
            if (!meeting.setTime(time.SelectedTime))
                ScheduleMeetingInvalidArg(time);
            if (!meeting.setDays(days))
                ScheduleMeetingInvalidArg(meetingDays);
            
            ZoomMeeting.SaveMeeting(meeting);
        }

        private void ScheduleMeetingInvalidArg(object? sender)
        {
            //Red border around invalid arg?
        }
    }
}