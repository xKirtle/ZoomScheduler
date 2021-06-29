using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

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
            ListBox days = this.FindControl<ListBox>("MeetingDays");
            //Todo: figure out days selected

            ZoomMeeting meeting = new ZoomMeeting();
            if (!meeting.setName(info.Text))
                ScheduleMeetingInvalidArg(info);
            if (!meeting.setId(id.Text))
                ScheduleMeetingInvalidArg(id);
            if (!meeting.setPassword(password.Text))
                ScheduleMeetingInvalidArg(password);
            if (!meeting.setTime(time.SelectedTime))
                ScheduleMeetingInvalidArg(time);
                
        }

        private void ScheduleMeetingInvalidArg(object? sender)
        {
            
        }
    }
}