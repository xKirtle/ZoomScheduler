using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DK.WshRuntime;
using IWshRuntimeLibrary;
using Newtonsoft.Json;
using File = System.IO.File;

namespace ZoomScheduler
{
    public partial class MainWindow : Window
    {
        private ZoomMeeting meetingToBeScheduled;
        private List<CheckBox> settingsCheckBoxes;
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            this.Opened += OnOpened;
            this.Closing += OnClosing;

            #region Schedule Meeting Tab
            meetingToBeScheduled = new ZoomMeeting();
            
            StackPanel scheduleSP = this.FindControl<StackPanel>("ScheduleSP");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays");
            scheduleSP.LayoutUpdated += ScheduleSP_OnLayoutUpdated;
            meetingDays.Tapped += ScheduleSP_OnLayoutUpdated;

            TextBox id = this.FindControl<TextBox>("MeetingId_TextBox");
            id.KeyDown += Id_OnKeyDown;

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

            settingsCheckBoxes = new List<CheckBox>();
            CheckBox startup = this.FindControl<CheckBox>("StartupCheckBox");
            startup.Tapped += (__, _) => StartupOnSystemBoot((bool)startup.IsChecked);
            settingsCheckBoxes.Add(startup);

            // CheckBox popupNotif = this.FindControl<CheckBox>("PopUpNotificationsCheckBox");
            // CheckBox minimizeToTray = this.FindControl<CheckBox>("MinimizeToTrayCheckBox");
            #endregion
            
            UpdateScheduledMeetings();
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            //Load CheckBoxes values in the Settings
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZoomScheduler");
            if (!Directory.Exists(path) || !File.Exists(Path.Combine(path, "Settings.json"))) return;

            string json;
            using (FileStream fs = new FileStream(Path.Combine(path, "Settings.json"), FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
                json = sr.ReadToEnd();

            bool[] options = JsonConvert.DeserializeObject<bool[]>(json);

            for (int i = 0; i < settingsCheckBoxes.Count; i++)
                settingsCheckBoxes[i].IsChecked = options[i];
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            //Save CheckBoxes values in the Settings
            bool[] options = new bool[settingsCheckBoxes.Count];
            string json = JsonConvert.SerializeObject(options);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZoomScheduler");
            Directory.CreateDirectory(path);
            
            using (FileStream fs = new FileStream(Path.Combine(path, "Settings.json"), FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs)) 
                sw.WriteLine(json);
        }

        private void Id_OnKeyDown(object? sender, KeyEventArgs e)
        {
            //Happens before e.Key is added to the textbox
            
            string text = ((TextBox) sender).Text;
            Console.WriteLine(e.Key);
            
            //TODO: apply regex here?
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

        private void StartupOnSystemBoot(bool isEnabled)
        {
            //TODO: Make a standalone console app that requires elevated perms
            
            switch (App.OSType)
            {
                case OperatingSystemType.WinNT:
                    Windows();
                    break;
                
                case OperatingSystemType.Linux:
                    Linux();
                    break;
                
                default:
                    break;
            }

            void Linux()
            {
                if (isEnabled)
                {
                    //Copy startupScript.sh to /usr/bin and make sure it can execute it (chmod +X)
                    //TODO: /usr/bin requires admin permissions so I just used the config folder for now
                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    string fileContent = $"dotnet {basePath}ZoomSchedulerService.dll";
                    string scriptPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                        "ZoomScheduler/startZoomSchedulerOnBoot.sh");
                    
                    using (FileStream fs = new FileStream(scriptPath, FileMode.Create, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs)) 
                        sw.WriteLine(fileContent);

                    string cmd = $"chmod +x {scriptPath}";
                    Process.Start("/bin/bash", $"-c \"{cmd}\"").WaitForExit();
                    
                    //Create unit file to define a systemd service in /lib/systemd/system/{$serviceName}.service
                    
                    //sudo systemctl enable {$serviceName}
                }
                else
                {
                    
                }
            }
            
            void Windows()
            {
                if (isEnabled)
                {
                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    WshInterop.CreateShortcut(basePath, "ZoomSchedulerService", 
                        Path.Combine(basePath, "ZoomSchedulerService.exe"), "", Path.Combine(basePath, "Assets\\icon.ico"));
                }
                else
                {
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ZoomSchedulerService.lnk");
                    if (File.Exists(path))
                        File.Delete(path);
                }
            }
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