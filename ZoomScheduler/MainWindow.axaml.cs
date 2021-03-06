using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Win32;
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
            //TODO: Read and update settings from Settings.json in Roaming?
            settingsCheckBoxes = new List<CheckBox>();
            
            StackPanel settings = this.FindControl<StackPanel>("SettingsSP");
            settings.LayoutUpdated += (sender, args) => SaveSettings(); 
            
            CheckBox startup = this.FindControl<CheckBox>("StartupCheckBox");
            startup.Tapped += (__, _) => StartupOnSystemBoot(startup.IsChecked ?? false);
            settingsCheckBoxes.Add(startup);
            
            CheckBox minimizeToTray = this.FindControl<CheckBox>("MinimizeToTrayCheckBox");
            settingsCheckBoxes.Add(minimizeToTray);
            
            // CheckBox popupNotif = this.FindControl<CheckBox>("PopUpNotificationsCheckBox");
            #endregion
            
            UpdateScheduledMeetings();
            
            TrayIcon trayIcon = new TrayIcon();
            trayIcon.Icon = new WindowIcon("Assets\\icon.ico");
            trayIcon.IsVisible = false;
            trayIcon.Clicked += (sender, args) =>
            {
                this.WindowState = WindowState.Normal;
                this.Show();
                trayIcon.IsVisible = false;
            };

            this.PropertyChanged += (sender, args) =>
            {
                if ((minimizeToTray.IsChecked ?? false) && WindowState == WindowState.Minimized)
                {
                    this.Hide();
                    trayIcon.IsVisible = true;
                }
            };
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

            bool[] options = JsonConvert.DeserializeObject<bool[]>(json) ?? new bool[settingsCheckBoxes.Count];
            for (int i = 0; i < settingsCheckBoxes.Count; i++)
                settingsCheckBoxes[i].IsChecked = options[i];
            
            //Check "run zoom scheduler on startup" based on registryKey values
            if (OperatingSystem.IsWindows())
            {
                #pragma warning disable
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", false);
                
                bool oldValue = settingsCheckBoxes[0].IsChecked.Value;
                settingsCheckBoxes[0].IsChecked = rk.GetValue("ZoomSchedulerService") != null;
                if (oldValue != (settingsCheckBoxes[0].IsChecked))
                    SaveSettings();
            }
        }

        private void OnClosing(object? sender, CancelEventArgs e) => SaveSettings();

        private void SaveSettings()
        {
            //Save CheckBoxes values in the Settings
            System.Nullable<bool>[] options = settingsCheckBoxes.Select(x => x.IsChecked).ToArray();
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
            
            // string text = ((TextBox) sender).Text;
            // Console.WriteLine(e.Key);
            
            //TODO: apply regex here?
        }

        private void ScheduleSP_OnLayoutUpdated(object? sender, EventArgs e)
        {
            Button scheduleMeeting = this.FindControl<Button>("ScheduleMeeting_Button");
            TextBox info = this.FindControl<TextBox>("MeetingInfo_TextBox");
            TextBox id = this.FindControl<TextBox>("MeetingId_TextBox");
            TextBox password = this.FindControl<TextBox>("MeetingPwd_TextBox");
            TextBox prefix = this.FindControl<TextBox>("MeetingPrefix_TextBox");
            TimePicker startTime = this.FindControl<TimePicker>("MeetingStartTime");
            TimePicker endTime = this.FindControl<TimePicker>("MeetingEndTime");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays");
            
            string[] lbNames = {"Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"};
            int[] days = new int[lbNames.Length];
            for (int i = 0; i < lbNames.Length; i++)
                days[i] = this.FindControl<ListBoxItem>(lbNames[i]).IsSelected ? 1 : 0;
            
            bool canSchedule = meetingToBeScheduled.setName(info.Text) && meetingToBeScheduled.setId(id.Text) &&
                               meetingToBeScheduled.setPassword(password.Text) && meetingToBeScheduled.setPrefix(prefix.Text) &&
                               meetingToBeScheduled.setStartTime(startTime.SelectedTime) && meetingToBeScheduled.setEndTime(endTime.SelectedTime) && 
                               meetingToBeScheduled.setPrefix(prefix.Text) && meetingToBeScheduled.setDays(days);

            scheduleMeeting.IsEnabled = canSchedule;
            //TODO: Include visual feedback of which fields are incorrectly filled?
        }

        private void StartupOnSystemBoot(bool isEnabled)
        {
            // switch (App.OSType)
            // {
            //     case OperatingSystemType.WinNT:
            //         Windows();
            //         break;
            //     
            //     case OperatingSystemType.Linux:
            //         Linux();
            //         break;
            //     
            //     default:
            //         break;
            // }
            
            if (OperatingSystem.IsLinux())
                Linux();
            else if (OperatingSystem.IsWindows())
                Windows();

            [SupportedOSPlatform("linux")]
            void Linux()
            {
                if (isEnabled)
                {
                    // //Copy startupScript.sh to /usr/bin and make sure it can execute it (chmod +X)
                    // //TODO: /usr/bin requires admin permissions so I just used the config folder for now
                    // string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    // string fileContent = $"dotnet {basePath}ZoomSchedulerService.dll";
                    // string scriptPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    //     "ZoomScheduler/startZoomSchedulerOnBoot.sh");
                    //
                    // using (FileStream fs = new FileStream(scriptPath, FileMode.Create, FileAccess.Write))
                    // using (StreamWriter sw = new StreamWriter(fs)) 
                    //     sw.WriteLine(fileContent);
                    //
                    // string cmd = $"chmod +x {scriptPath}";
                    // Process.Start("/bin/bash", $"-c \"{cmd}\"").WaitForExit();
                    //
                    // //Create unit file to define a systemd service in /lib/systemd/system/{$serviceName}.service
                    //
                    // //sudo systemctl enable {$serviceName}
                }
                else
                {
                    
                }
            }
            [SupportedOSPlatform("windows")]
            void Windows()
            {
                //TODO: Make a standalone console app that requires elevated perms to do this task?

                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (rk == null) return;
                
                if (isEnabled)
                    rk.SetValue("ZoomSchedulerService", Path.Combine(Environment.CurrentDirectory, "ZoomSchedulerService.exe"));
                else
                    rk.DeleteValue("ZoomSchedulerService",false);

                //Create shortcut linking to the service exe in shell:startup?
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
            TextBox prefix = this.FindControl<TextBox>("MeetingPrefix_TextBox");
            TimePicker startTime = this.FindControl<TimePicker>("MeetingStartTime");
            TimePicker endTime = this.FindControl<TimePicker>("MeetingEndTime");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays");

            info.Text = id.Text = password.Text = prefix.Text = "";
            startTime.SelectedTime = endTime.SelectedTime = TimeSpan.Zero;
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
            TextBox prefix = this.FindControl<TextBox>("MeetingPrefix_TextBox2");
            TimePicker startTime = this.FindControl<TimePicker>("MeetingStartTime2");
            TimePicker endTime = this.FindControl<TimePicker>("MeetingEndTime2");
            ListBox meetingDays = this.FindControl<ListBox>("MeetingDays2");
            
            info.Text = meeting.Name;
            id.Text = meeting.ID.ToString();
            password.Text = meeting.Password;
            startTime.SelectedTime = meeting.StartTime;
            endTime.SelectedTime = meeting.EndTime;
            prefix.Text = meeting.Prefix;

            List<ListBoxItem> selectedItems = new List<ListBoxItem>();
            string[] lbNames = {"Mon2", "Tue2", "Wed2", "Thu2", "Fri2", "Sat2", "Sun2"};
            for (int i = 0; i < lbNames.Length; i++)
                if (meeting.Days[i] == 1)
                    selectedItems.Add(this.FindControl<ListBoxItem>(lbNames[i]));
            meetingDays.SelectedItems = selectedItems;
        }
    }
}