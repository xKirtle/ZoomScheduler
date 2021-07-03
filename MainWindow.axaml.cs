using System;
using System.Collections.Generic;
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

            ComboBox unscheduleComboBox = this.FindControl<ComboBox>("UnscheduleMeeting_ComboBox");
            unscheduleComboBox.SelectionChanged += UnscheduleComboBox_OnSelectionChanged;
            
            UpdateScheduledMeetings();
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
            UpdateScheduledMeetings();
            ClearInputFields();
        }

        private void ScheduleMeetingInvalidArg(object? sender)
        {
            //Red border around invalid arg?
            //Disable schedule button?
        }

        private void UpdateScheduledMeetings()
        {
            ComboBox unscheduleComboBox = this.FindControl<ComboBox>("UnscheduleMeeting_ComboBox");
            unscheduleComboBox.Items = ZoomMeeting.ReadMeetings();
        }

        private void ClearInputFields()
        {
            TextBox info = this.FindControl<TextBox>("MeetingInfo_TextBox");
            TextBox id = this.FindControl<TextBox>("MeetingId_TextBox");
            TextBox password = this.FindControl<TextBox>("MeetingPwd_TextBox");
            TimePicker time = this.FindControl<TimePicker>("MeetingSelectedTime");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays");

            info.Text = id.Text = password.Text = "";
            time.SelectedTime = TimeSpan.Zero;
            meetingDays.SelectedItems = null;
        }
        
        private void UnscheduleComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            UpdateScheduleMeetingValue(comboBox.SelectedIndex < 0 ? new ZoomMeeting() : ZoomMeeting.ReadMeetings()[comboBox.SelectedIndex]);
        }

        private void UpdateScheduleMeetingValue(ZoomMeeting meeting)
        {
            TextBox info = this.FindControl<TextBox>("MeetingInfo_TextBox2");
            TextBox id = this.FindControl<TextBox>("MeetingId_TextBox2");
            TextBox password = this.FindControl<TextBox>("MeetingPwd_TextBox2");
            TimePicker time = this.FindControl<TimePicker>("MeetingSelectedTime2");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays2");
            
            info.Text = meeting.Name;
            id.Text = meeting.ID.ToString();
            password.Text = meeting.Password;
            time.SelectedTime = meeting.Time;

            List<ListBoxItem> selectedItems = new List<ListBoxItem>();
            string[] lbNames = {"Mon2", "Tue2", "Wed2", "Thu2", "Fri2", "Sat2", "Sun2"};
            for (int i = 0; i < lbNames.Length; i++)
                if (meeting.Days[i] == 1)
                    selectedItems.Add(this.FindControl<ListBoxItem>(lbNames[i]));
            meetingDays.SelectedItems = selectedItems;
        }
    }
}