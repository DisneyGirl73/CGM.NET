using System;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using CGM.Uwp.Services;

using Windows.ApplicationModel;
using CGM.Communication.Data;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Views;
using System.Collections.Generic;

namespace CGM.Uwp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {

        private bool _isLightThemeEnabled;
        private IDialogService _dialogService;
        public bool IsLightThemeEnabled
        {
            get { return _isLightThemeEnabled; }
            set { Set(ref _isLightThemeEnabled, value); }
        }

        private string _appDescription;
        public string AppDescription
        {
            get { return _appDescription; }
            set { Set(ref _appDescription, value); }
        }

        private Setting _setting;

        public string NightscoutUrl
        {
            get { return _setting.NightscoutUrl; }
            set
            {
                _setting.NightscoutUrl = value;
                SaveSetting();
            }
        }

        public string NotificationUrl
        {
            get { return _setting.NotificationUrl; }
            set
            {
                _setting.NotificationUrl = value;
                SaveSetting();
            }
        }

        public string NightscoutApiKey
        {
            get { return _setting.NightscoutSecretkey; }
            set
            {
                _setting.NightscoutSecretkey = value;
                SaveSetting();
                this.RaisePropertyChanged(nameof(NightscoutApiKey));
                this.RaisePropertyChanged(nameof(NightscoutApiKeyHashed));
            }
        }

        private string _localpath;

        public string LocalPath
        {
            get { return _localpath; }
            set { Set(ref _localpath, value); }
        }

        private DeviceItemViewModel _selectedDevice;

        public DeviceItemViewModel SelectedDevice
        {
            get { return _selectedDevice; }
            set { Set(

                ref _selectedDevice, value);
            }
        }

       
        public bool  IncludeHistory
        {
            get { return _setting.OtherSettings.IncludeHistory; }
            set
            {
                _setting.OtherSettings.IncludeHistory = value;
                RaisePropertyChanged(nameof(IncludeHistory));
                SaveSetting();
            }
        }

        public bool SendEventsToNotificationUrl
        {
            get { return _setting.OtherSettings.SendEventsToNotificationUrl; }
            set
            {
                _setting.OtherSettings.SendEventsToNotificationUrl = value;
                this.RaisePropertyChanged(nameof(SendEventsToNotificationUrl));
                SaveSetting();
            }
        }

        public int HistoryDaysBack
        {
            get { return _setting.OtherSettings.HistoryDaysBack; }
            set
            {
                _setting.OtherSettings.HistoryDaysBack = value;
                this.RaisePropertyChanged(nameof(HistoryDaysBack));
                SaveSetting();
            }
        }

        public int IntervalSeconds
        {
            get { return _setting.OtherSettings.IntervalSeconds; }
            set
            {
                _setting.OtherSettings.IntervalSeconds = value;
                this.RaisePropertyChanged(nameof(IntervalSeconds));
                SaveSetting();
            }
        }
        public int TimeoutSeconds
        {
            get { return _setting.OtherSettings.TimeoutSeconds; }
            set
            {
                _setting.OtherSettings.TimeoutSeconds = value;
                this.RaisePropertyChanged(nameof(TimeoutSeconds));
                SaveSetting();
            }
        }
        

        public bool AutoStartTask
        {
            get { return _setting.OtherSettings.AutoStartTask; }
            set
            {
                _setting.OtherSettings.AutoStartTask = value;
                this.RaisePropertyChanged(nameof(AutoStartTask));
                SaveSetting();
            }
        }

        public bool OnlyFromTheLastReading
        {
            get { return _setting.OtherSettings.OnlyFromTheLastReading; }
            set
            {
                _setting.OtherSettings.OnlyFromTheLastReading = value;
                this.RaisePropertyChanged(nameof(OnlyFromTheLastReading));
                SaveSetting();
            }
        }

        public bool UploadToNightscout
        {
            get { return _setting.OtherSettings.UploadToNightscout; }
            set
            {
                _setting.OtherSettings.UploadToNightscout = value;
                this.RaisePropertyChanged(nameof(UploadToNightscout));
                SaveSetting();
            }
        }

        public bool HandleAlert776
        {
            get { return _setting.OtherSettings.HandleAlert776; }
            set
            {
                _setting.OtherSettings.HandleAlert776 = value;
                this.RaisePropertyChanged(nameof(HandleAlert776));
                SaveSetting();
            }
        }

        public ObservableCollection<DeviceItemViewModel> Devices { get; set; } = new ObservableCollection<DeviceItemViewModel>();



        public string NightscoutApiKeyHashed { get { return _setting.ApiKeyHashed; } }


        public ICommand SwitchThemeCommand { get; private set; }
        public ICommand GetDevicesCommand { get; private set; }

        public ICommand ClearHistoryCommand { get; private set; }

        public SettingsViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            SwitchThemeCommand = new RelayCommand(async () => { await ThemeSelectorService.SwitchThemeAsync(); });
            this.GetDevicesCommand = new RelayCommand(() => this.GetDevices());

            this.ClearHistoryCommand = new RelayCommand(() => this.ClearHistory());
            using (CGM.Communication.Data.Repository.CgmUnitOfWork uow = new Communication.Data.Repository.CgmUnitOfWork())
            {
                _setting = uow.Setting.GetSettings();
            }
            GetDevices();
            this.LocalPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }

        private void ClearHistory()
        {
            using (CGM.Communication.Data.Repository.CgmUnitOfWork uow = new Communication.Data.Repository.CgmUnitOfWork())
            {
                uow.History.ResetHistory();
            }
            _dialogService.ShowMessage("History has been cleared.","Clear history");
        }

        private void GetDevices()
        {
            this.Devices.Clear();
            List<Device> devices=new List<Device>();
            using (CGM.Communication.Data.Repository.CgmUnitOfWork uow = new Communication.Data.Repository.CgmUnitOfWork())
            {
                devices = uow.Device.GetAllDevices();
            }

            foreach (var item in devices)
            {
                var device = new DeviceItemViewModel(item, _dialogService);
                device.Removed += Device_Removed;
                this.Devices.Add(device);
            }
            if (this.Devices.Count>0)
            {
                this.SelectedDevice = this.Devices[0];
            }
            else
            {
                this.SelectedDevice = null;
            }
        }

        private void Device_Removed(object sender, EventArgs e)
        {
            GetDevices();
        }

        private void SaveSetting()
        {
            using (CGM.Communication.Data.Repository.CgmUnitOfWork uow = new Communication.Data.Repository.CgmUnitOfWork())
            {
                uow.Setting.Update(_setting);
            }
        }

        public void Initialize()
        {
            IsLightThemeEnabled = ThemeSelectorService.IsLightThemeEnabled;
            AppDescription = GetAppDescription();
        }

        private string GetAppDescription()
        {
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{package.DisplayName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }


    }
}
