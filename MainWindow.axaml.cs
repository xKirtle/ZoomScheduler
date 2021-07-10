using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using IWshRuntimeLibrary;
using JetBrains.Annotations;
using File = System.IO.File;

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
            scheduleMeeting.Click += ScheduleMeetingButton_OnClick;

            ComboBox unscheduleComboBox = this.FindControl<ComboBox>("UnscheduleMeeting_ComboBox");
            unscheduleComboBox.SelectionChanged += UnscheduleComboBox_OnSelectionChanged;
            
            Button unscheduleMeeting = this.FindControl<Button>("UnscheduleMeeting_Button");
            unscheduleMeeting.Click += UnscheduleMeetingButton_OnClick;
            
            CheckBox startup = this.FindControl<CheckBox>("StartupCheckBox");
            startup.Tapped += StartupCheckBox_OnTapped;
            
            // CheckBox popupNotif = this.FindControl<CheckBox>("PopUpNotificationsCheckBox");
            // CheckBox minimizeToTray = this.FindControl<CheckBox>("MinimizeToTrayCheckBox");
            
            UpdateScheduledMeetings();
        }

        private void StartupCheckBox_OnTapped(object? sender, RoutedEventArgs e)
        {
            CheckBox startup = sender as CheckBox;
            if (startup.IsChecked == true)
            {
                WshShell shell = new WshShell();
                string shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\ZoomSchedulerService.lnk";

                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "ZoomScheduler";
                shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                shortcut.WorkingDirectory = @"C:\Windows\System32";
                shortcut.TargetPath = AppDomain.CurrentDomain.BaseDirectory + @"\ZoomSchedulerService.exe";
                shortcut.IconLocation = AppDomain.CurrentDomain.BaseDirectory + @"Assets\icon.ico";

                shortcut.Save();
                
                //Execute dotnet pathToFile\file.dll
                //Somewhat viable way for non Windows?
                //TODO: Lookup multi platform compatibility solutions?
            }
            else
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\ZoomSchedulerService.lnk";
                if (File.Exists(path))
                    File.Delete(path);
            }
            
            //TODO: Save checkbox value in WPF Settings?
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ScheduleMeetingButton_OnClick(object? sender, RoutedEventArgs e)
        {
            TextBox info = this.FindControl<TextBox>("MeetingInfo_TextBox");
            TextBox id = this.FindControl<TextBox>("MeetingId_TextBox");
            TextBox password = this.FindControl<TextBox>("MeetingPwd_TextBox");
            TimePicker time = this.FindControl<TimePicker>("MeetingSelectedTime");
            CheckBox prefix = this.FindControl<CheckBox>("MeetingPrefix");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays");

            string[] lbNames = {"Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"};
            int[] days = new int[lbNames.Length];
            for (int i = 0; i < lbNames.Length; i++)
                days[i] = this.FindControl<ListBoxItem>(lbNames[i]).IsSelected ? 1 : 0;

            ZoomMeeting meeting = new ZoomMeeting();
            //TODO: Remove this spaghetti :(
            if (!meeting.setName(info.Text))
                ScheduleMeetingInvalidArg(info);
            if (!meeting.setId(id.Text))
                ScheduleMeetingInvalidArg(id);
            if (!meeting.setPassword(password.Text))
                ScheduleMeetingInvalidArg(password);
            if (!meeting.setTime(time.SelectedTime))
                ScheduleMeetingInvalidArg(time);
            if (!meeting.setPrefix(prefix.IsChecked))
                ScheduleMeetingInvalidArg(prefix);
            if (!meeting.setDays(days))
                ScheduleMeetingInvalidArg(meetingDays);
            
            ZoomMeeting.ScheduleMeeting(meeting);
            UpdateScheduledMeetings();
            ClearInputFields();
        }
        
        private void UnscheduleMeetingButton_OnClick(object? sender, RoutedEventArgs e)
        {
            ComboBox unscheduleComboBox = this.FindControl<ComboBox>("UnscheduleMeeting_ComboBox");
            if (unscheduleComboBox.ItemCount > 0)
            {
                ZoomMeeting.UnscheduleMeeting(unscheduleComboBox.SelectedIndex);
                UpdateScheduledMeetings();
            }
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
            CheckBox prefix = this.FindControl<CheckBox>("MeetingPrefix");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays");

            info.Text = id.Text = password.Text = "";
            time.SelectedTime = TimeSpan.Zero;
            prefix.IsChecked = false;
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
            CheckBox prefix = this.FindControl<CheckBox>("MeetingPrefix2");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays2");
            
            info.Text = meeting.Name;
            id.Text = meeting.ID.ToString();
            password.Text = meeting.Password;
            time.SelectedTime = meeting.Time;
            prefix.IsChecked = meeting.Prefix;

            List<ListBoxItem> selectedItems = new List<ListBoxItem>();
            string[] lbNames = {"Mon2", "Tue2", "Wed2", "Thu2", "Fri2", "Sat2", "Sun2"};
            for (int i = 0; i < lbNames.Length; i++)
                if (meeting.Days[i] == 1)
                    selectedItems.Add(this.FindControl<ListBoxItem>(lbNames[i]));
            meetingDays.SelectedItems = selectedItems;
        }
    }
}