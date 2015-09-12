﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using CameraControl.Classes;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;

namespace CameraControl.ViewModel
{
    public class MainMenuViewModel : ViewModelBase
    {
        ProgressWindow dlg;

        private bool _showFocusPoints;
        private bool _flipPreview;
        private Branding _branding;
        private List<IExportPlugin> _exportPlugins;
        private bool _cameraConnected;
        private Settings _settings;
        public GalaSoft.MvvmLight.Command.RelayCommand<string> SendCommand { get; set; }
        public RelayCommand SettingsCommand { get; set; }
        public GalaSoft.MvvmLight.Command.RelayCommand<int> ThumbSizeCommand { get; set; }
        public GalaSoft.MvvmLight.Command.RelayCommand<string> SetLayoutCommand { get; set; }

        public RelayCommand ToggleFocusCommand { get; set; }

        public RelayCommand NewSessionCommand { get; private set; }
        public RelayCommand EditSessionCommand { get; private set; }
        public RelayCommand DelSessionCommand { get; private set; }

        public RelayCommand RefreshSessionCommand { get; private set; }
        public RelayCommand ShowSessionCommand { get; private set; }

        public RelayCommand SelectLiked { get; private set; }
        public RelayCommand SelectUnLiked { get; private set; }
        public RelayCommand SelectNoneCommand { get; private set; }
        public RelayCommand SelectInvertCommand { get; private set; }
        public RelayCommand SelectSeries { get; private set; }
        public RelayCommand SelectAllCommand { get; private set; }
        
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand CameraPropertyCommand { get; private set; }
        public RelayCommand UseAsMasterCommand { get; private set; }
        
        public RelayCommand FlipPreviewCommand { get; set; }

        public RelayCommand ManualPageCommand { get; set; }
        public RelayCommand HomePageCommand { get; set; }
        public RelayCommand CheckUpdateCommand { get; set; }
        public RelayCommand ForumCommand { get; set; }
        public RelayCommand SendLogFileCommand { get; set; }
        public RelayCommand ShowChangeLogCommand { get; set; }
        public RelayCommand AboutCommand { get; set; }
        public RelayCommand ExportSessionCommand { get; set; }
        public RelayCommand ImportSessionCommand { get; set; }

        public AsyncObservableCollection<IExportPlugin> ExportPlugins
        {
            get { return ServiceProvider.PluginManager.ExportPlugins; }
        }

        public AsyncObservableCollection<IToolPlugin> ToolsPlugins
        {
            get { return ServiceProvider.PluginManager.ToolPlugins; }
        }


        public GalaSoft.MvvmLight.Command.RelayCommand<IExportPlugin> ExecuteExportPluginCommand { get; private set; }
        public GalaSoft.MvvmLight.Command.RelayCommand<IToolPlugin> ExecuteToolPluginCommand { get; private set; }

        public bool ShowFocusPoints
        {
            get { return ServiceProvider.Settings.ShowFocusPoints; }
            set
            {
                ServiceProvider.Settings.ShowFocusPoints = value;
                RaisePropertyChanged(() => ShowFocusPoints);
            }
        }

        public bool FlipPreview
        {
            get { return ServiceProvider.Settings.FlipPreview; }
            set
            {
                ServiceProvider.Settings.FlipPreview = value;
                RaisePropertyChanged(()=>FlipPreview);
            }
        }

        public Branding Branding
        {
            get { return ServiceProvider.Branding; }
        }

        public bool CameraConnected
        {
            get { return ServiceProvider.DeviceManager.SelectedCameraDevice != null && ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected; }
        }

        public Settings Settings
        {
            get { return ServiceProvider.Settings; }
        }


        public MainMenuViewModel()
        {
            SendCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(Send);
            SettingsCommand = new RelayCommand(EditSettings);
            ThumbSizeCommand = new GalaSoft.MvvmLight.Command.RelayCommand<int>(ThumbSize);
            SetLayoutCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(SetLayout);
            SelectAllCommand =new RelayCommand(delegate { ServiceProvider.Settings.DefaultSession.SelectAll(); });
            SelectNoneCommand =new RelayCommand(delegate { ServiceProvider.Settings.DefaultSession.SelectNone(); });
            SelectLiked = new RelayCommand(delegate { ServiceProvider.Settings.DefaultSession.SelectLiked(); });
            SelectUnLiked =new RelayCommand(delegate { ServiceProvider.Settings.DefaultSession.SelectUnLiked(); });
            SelectInvertCommand =new RelayCommand(delegate { ServiceProvider.Settings.DefaultSession.SelectInver(); });
            SelectSeries =
                new RelayCommand(delegate
                {
                    try
                    {
                        ServiceProvider.Settings.DefaultSession.SelectSameSeries(
                            ServiceProvider.Settings.SelectedBitmap.FileItem.Series);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("SelectSeries", ex);
                    }
                });
            NewSessionCommand = new RelayCommand(NewSession);
            EditSessionCommand = new RelayCommand(EditSession);
            DelSessionCommand=new RelayCommand(DelSession);
            RefreshSessionCommand = new RelayCommand(RefreshSession);
            ShowSessionCommand = new RelayCommand(ShowSession);
            RefreshCommand = new RelayCommand(Refresh);
            CameraPropertyCommand =
                new RelayCommand(
                    () => ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.CameraPropertyWnd_Show,
                        ServiceProvider.DeviceManager.SelectedCameraDevice));
            UseAsMasterCommand = new RelayCommand(UseAsMaster);

            ToggleFocusCommand = new RelayCommand(() => ShowFocusPoints = !ShowFocusPoints);
            FlipPreviewCommand = new RelayCommand(() => FlipPreview = !FlipPreview);
            HomePageCommand = new RelayCommand(() => PhotoUtils.Run("http://www.digicamcontrol.com/", ""));
            CheckUpdateCommand = new RelayCommand(() => NewVersionWnd.CheckForUpdate(true));
            ForumCommand = new RelayCommand(() => PhotoUtils.Run("http://www.digicamcontrol.com/forum/", ""));
            SendLogFileCommand = new RelayCommand(() => new ErrorReportWnd("Log file").ShowDialog());
            ShowChangeLogCommand = new RelayCommand(NewVersionWnd.ShowChangeLog);
            AboutCommand = new RelayCommand(() => new AboutWnd().ShowDialog());
            ManualPageCommand = new RelayCommand(() => HelpProvider.Run(HelpSections.MainMenu));

            ExecuteExportPluginCommand = new GalaSoft.MvvmLight.Command.RelayCommand<IExportPlugin>(ExecuteExportPlugin);
            ExecuteToolPluginCommand = new GalaSoft.MvvmLight.Command.RelayCommand<IToolPlugin>(ExecuteToolPlugin);
            if (ServiceProvider.DeviceManager != null)
            {
                ServiceProvider.DeviceManager.CameraConnected += DeviceManager_CameraConnected;
                ServiceProvider.DeviceManager.CameraDisconnected += DeviceManager_CameraConnected;
                ServiceProvider.DeviceManager.CameraSelected += DeviceManager_CameraSelected;
            }
            ExportSessionCommand = new RelayCommand(ExportSession);
            ImportSessionCommand = new RelayCommand(ImportSession);
        }

        void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
        {
            RaisePropertyChanged(() => CameraConnected);
        }

        void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
        {
            RaisePropertyChanged(() => CameraConnected);
        }

        private void ExportSession()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = ServiceProvider.Settings.DefaultSession.Name + ".dccsession";
            dialog.Filter = "Session files (*.dccsession)|*.dccsession|All files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string tempfile = Path.GetTempFileName();
                    ServiceProvider.Settings.SaveSession(ServiceProvider.Settings.DefaultSession,tempfile);
                    var session = ServiceProvider.Settings.LoadSession(tempfile);
                    session.Files.Clear();
                    ServiceProvider.Settings.SaveSession(session, dialog.FileName);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to export session", ex);
                    ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, "Unable to export session " + ex.Message);
                }
            }
        }

        private void ImportSession()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Session files (*.dccsession)|*.dccsession|All files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var session = ServiceProvider.Settings.LoadSession(dialog.FileName);
                    if (ServiceProvider.Settings.PhotoSessions.Any(photoSession => photoSession.Name.ToLower() == session.Name.ToLower()))
                    {
                        throw new Exception("Session with same name already exist !");
                    }
                    ServiceProvider.Settings.Add(session);
                    ServiceProvider.Settings.DefaultSession = session;
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to import session", ex);
                    ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, "Unable to import session " + ex.Message);
                }
            }
        }

        private void NewSession()
        {
            try
            {
                var defaultsessionfile = Path.Combine(Core.Classes.Settings.SessionFolder, "Default.xml");
                var session = new PhotoSession();
                // copy session with default name
                if (File.Exists(defaultsessionfile))
                {
                    session = ServiceProvider.Settings.LoadSession(defaultsessionfile);
                    session.Files.Clear();
                }
                var editSession = new EditSession(session);
                editSession.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
                ServiceProvider.Settings.ApplyTheme(editSession);
                if (editSession.ShowDialog() == true)
                {
                    ServiceProvider.Settings.Add(editSession.Session);
                    ServiceProvider.Settings.DefaultSession = editSession.Session;
                }   
            }
            catch (Exception ex)
            {
                Log.Error("Error create session ", ex);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, "Error create session " + ex.Message);
            }
        }

        private void EditSession()
        {
            try
            {
                EditSession editSession = new EditSession(ServiceProvider.Settings.DefaultSession);
                editSession.Owner = ServiceProvider.PluginManager.SelectedWindow as Window; ;
                ServiceProvider.Settings.ApplyTheme(editSession);
                editSession.ShowDialog();
                ServiceProvider.Settings.Save(ServiceProvider.Settings.DefaultSession);
            }
            catch (Exception ex)
            {
                Log.Error("Error refresh session ", ex);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, ex.Message);
            }
        }

        private void DelSession()
        {
            if (ServiceProvider.Settings.PhotoSessions.Count > 1)
            {
                try
                {
                    if (
                        MessageBox.Show(
                            string.Format(TranslationStrings.MsgDeleteSessionQuestion,
                                          ServiceProvider.Settings.DefaultSession.Name),
                            TranslationStrings.LabelDeleteSession, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        PhotoSession session = ServiceProvider.Settings.DefaultSession;
                        if (!string.IsNullOrEmpty(session.ConfigFile) && File.Exists(session.ConfigFile))
                            File.Delete(session.ConfigFile);
                        ServiceProvider.Settings.PhotoSessions.Remove(session);
                        ServiceProvider.Settings.DefaultSession = ServiceProvider.Settings.PhotoSessions[0];
                        ServiceProvider.Settings.Save();
                    }
                }
                catch (Exception exception)
                {
                    Log.Error("Unable to remove session", exception);
                    ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, TranslationStrings.LabelUnabletoDeleteSession + exception.Message);
                }
            }
            else
            {
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, TranslationStrings.MsgLastSessionCantBeDeleted);
            }
        }

        private void RefreshSession()
        {
            try
            {
                ServiceProvider.Settings.LoadData(ServiceProvider.Settings.DefaultSession);
            }
            catch (Exception ex)
            {
                Log.Error("Error refresh session ",ex);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, ex.Message);
            }
        }

        private void ShowSession()
        {
            try
            {
                PhotoUtils.Run(ServiceProvider.Settings.DefaultSession.Folder);
            }
            catch (Exception ex)
            {
                Log.Error("Error refresh session ", ex);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, ex.Message);
            }
        }

        private void UseAsMaster()
        {
            if (dlg == null)
                dlg = new ProgressWindow();
            dlg.Show();
            Thread thread = new Thread(SetAsMaster);
            thread.Start();
        }

        public void SetAsMaster()
        {
            try
            {
                int i = 0;
                dlg.MaxValue = ServiceProvider.DeviceManager.ConnectedDevices.Count;
                var preset = new CameraPreset();
                preset.Get(ServiceProvider.DeviceManager.SelectedCameraDevice);
                foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices)
                {
                    if (connectedDevice == null || !connectedDevice.IsConnected)
                        continue;
                    try
                    {
                        if (connectedDevice != ServiceProvider.DeviceManager.SelectedCameraDevice)
                        {
                            dlg.Label = connectedDevice.DisplayName;
                            dlg.Progress = i;
                            i++;
                            preset.Set(connectedDevice);
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Unable to set property ", exception);
                    }
                    Thread.Sleep(250);
                }
            }
            catch (Exception exception)
            {
                Log.Error("Unable to set as master ", exception);
            }
            dlg.Hide();
        }

        private void Refresh()
        {
            try
            {
                ServiceProvider.DeviceManager.ConnectToCamera();
            }
            catch (Exception exception)
            {
                Log.Error("Error to connect", exception);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, "Unable to connect \n" + exception.Message);
            }
        }

        private void Send(string command)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(command);
        }

        private void EditSettings()
        {
            SettingsWnd wnd = new SettingsWnd();
            wnd.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
            wnd.ShowDialog();
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                    true);

                if (rk == null) return;

                if (ServiceProvider.Settings.StartupWithWindows)
                {
                    rk.SetValue(Core.Classes.Settings.AppName, Assembly.GetExecutingAssembly().Location);
                }
                else
                    rk.DeleteValue(Core.Classes.Settings.AppName, false);
            }
            catch (Exception ex)
            {
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, "Usable to set startup\n"+ ex.Message);
                Log.Error("Usable to set startup", ex);
            }
        }

        private void ThumbSize(int size)
        {
            ServiceProvider.Settings.ThumbHeigh = size;
        }

        private void SetLayout(string type)
        {
            ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.SetLayout, type);
        }

        private void ExecuteExportPlugin(IExportPlugin obj)
        {
            try
            {
                obj.Execute();
            }
            catch (Exception ex)
            {
                Log.Error("Error refresh session ", ex);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, ex.Message);
            }
        }

        private void ExecuteToolPlugin(IToolPlugin obj)
        {
            try
            {
                obj.Execute();
            }
            catch (Exception ex)
            {
                Log.Error("Error refresh session ", ex);
                ServiceProvider.WindowsManager.ExecuteCommand(WindowsCmdConsts.MainWnd_Message, ex.Message);
            }
        }

    }
}
