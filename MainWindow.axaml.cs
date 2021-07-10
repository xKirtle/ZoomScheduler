using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private ZoomMeeting meetingToBeScheduled;
        public MainWindow()
        {
            InitializeComponent();
            #if DEBUG
            this.AttachDevTools();
            #endif

            #region Schedule Meeting Tab
            meetingToBeScheduled = new ZoomMeeting();
            
            StackPanel scheduleSP = this.FindControl<StackPanel>("ScheduleSP");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays");
            scheduleSP.LayoutUpdated += ScheduleSP_OnLayoutUpdated;
            meetingDays.Tapped += ScheduleSP_OnLayoutUpdated;

            Button scheduleMeeting = this.FindControl<Button>("ScheduleMeeting_Button");
            scheduleMeeting.Click += ScheduleMeetingButton_OnClick;
            #endregion

            #region Unschedule Meeting Tab
            ComboBox unscheduleComboBox = this.FindControl<ComboBox>("UnscheduleMeeting_ComboBox");
            unscheduleComboBox.SelectionChanged += UnscheduleComboBox_OnSelectionChanged;
            
            Button unscheduleMeeting = this.FindControl<Button>("UnscheduleMeeting_Button");
            unscheduleMeeting.Click += UnscheduleMeetingButton_OnClick;
            #endregion
            
            #region Settings Tab
            CheckBox startup = this.FindControl<CheckBox>("StartupCheckBox");
            startup.Tapped += StartupCheckBox_OnTapped;
            
            // CheckBox popupNotif = this.FindControl<CheckBox>("PopUpNotificationsCheckBox");
            // CheckBox minimizeToTray = this.FindControl<CheckBox>("MinimizeToTrayCheckBox");
            #endregion
            
            UpdateScheduledMeetings();
        }

        private void ScheduleSP_OnLayoutUpdated(object? sender, EventArgs e)
        {
            Button scheduleMeeting = this.FindControl<Button>("ScheduleMeeting_Button");
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
            
            bool canSchedule = meetingToBeScheduled.setName(info.Text) && meetingToBeScheduled.setId(id.Text) &&
                               meetingToBeScheduled.setPassword(password.Text) && meetingToBeScheduled.setTime(time.SelectedTime) &&
                               meetingToBeScheduled.setPrefix(prefix.IsChecked) && meetingToBeScheduled.setDays(days);

            scheduleMeeting.IsEnabled = canSchedule;
            //TODO: Include visual feedback of which fields are incorrectly filled?
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

        private void ScheduleMeetingButton_OnClick(object? sender, RoutedEventArgs e)
        {
            ZoomMeeting.ScheduleMeeting(meetingToBeScheduled);
            meetingToBeScheduled = new ZoomMeeting();
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
            UpdateScheduledMeetingPreview(comboBox.SelectedIndex < 0 ? new ZoomMeeting() : ZoomMeeting.ReadMeetings()[comboBox.SelectedIndex]);
            
            Button unscheduleMeeting = this.FindControl<Button>("UnscheduleMeeting_Button");
            unscheduleMeeting.IsEnabled = comboBox.SelectedIndex >= 0;
        }

        private void UpdateScheduledMeetingPreview(ZoomMeeting meeting)
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