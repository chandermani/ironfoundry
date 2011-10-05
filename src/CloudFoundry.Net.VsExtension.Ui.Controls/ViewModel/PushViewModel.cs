﻿namespace CloudFoundry.Net.VsExtension.Ui.Controls.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Threading;
    using CloudFoundry.Net.Types;
    using CloudFoundry.Net.VsExtension.Ui.Controls.Model;
    using CloudFoundry.Net.VsExtension.Ui.Controls.Utilities;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Messaging;
    using CloudFoundry.Net.VsExtension.Ui.Controls.Mvvm;

    [ExportViewModel("Push",false)]
    public class PushViewModel : ViewModelBase
    {
        private Cloud selectedCloud;
        private string errorMessage;
        private ObservableCollection<ProvisionedService> applicationServices;
        private CloudFoundryProvider provider;
        private string name;
        private string url;
        private int selectedMemory;
        private int instances;
        public RelayCommand ConfirmedCommand { get; private set; }
        public RelayCommand CancelledCommand { get; private set; }
        public RelayCommand ManageCloudsCommand { get; private set; }
        public RelayCommand AddAppServiceCommand { get; private set; }

        public PushViewModel()
        {
            Messenger.Default.Send<NotificationMessageAction<CloudFoundryProvider>>(new NotificationMessageAction<CloudFoundryProvider>(Messages.GetCloudFoundryProvider, p => this.provider = p));
            ConfirmedCommand = new RelayCommand(Confirmed);
            CancelledCommand = new RelayCommand(Cancelled);
            ManageCloudsCommand = new RelayCommand(ManageClouds);
            AddAppServiceCommand = new RelayCommand(AddAppService);
            InitializeData();
            RegisterGetData();
        }

        private void RegisterGetData()
        {
            Messenger.Default.Register<NotificationMessageAction<PushViewModel>>(this,
                message =>
                {
                    if (message.Notification.Equals(Messages.GetPushAppData))
                        message.Execute(this);
                    Messenger.Default.Unregister(this);
                });
        }

        private void InitializeData()
        {
            Messenger.Default.Send(new NotificationMessageAction<Guid>(Messages.SetPushAppData,
                (id) =>
                {                    
                    this.SelectedCloud = Clouds.SingleOrDefault(i => i.ID == id);
                }));
        }

        public string ErrorMessage
        {
            get { return this.errorMessage; }
            set { this.errorMessage = value; RaisePropertyChanged("ErrorMessage"); }
        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; RaisePropertyChanged("Name"); }
        }

        public string Url
        {
            get { return this.url; }
            set { this.url = value; RaisePropertyChanged("Url"); }
        }

        public ObservableCollection<Cloud> Clouds
        {
            get { return provider.Clouds; }            
        }

        public Cloud SelectedCloud
        {
            get { return this.selectedCloud; }
            set { this.selectedCloud = value; RaisePropertyChanged("SelectedCloud"); }
        }

        public int[] MemoryLimits { get { return Constants.MemoryLimits; } }      
        
        public int SelectedMemory
        {
            get { return this.selectedMemory; }
            set { this.selectedMemory = value; RaisePropertyChanged("SelectedMemory"); }
        }

        public int Instances
        {
            get { return this.instances; }
            set { this.instances = value; RaisePropertyChanged("Instances"); }
        }
        
        public ObservableCollection<ProvisionedService> ApplicationServices
        {
            get { return this.applicationServices; }
            set { this.applicationServices = value; RaisePropertyChanged("ApplicationServices"); }
        }

        private void Confirmed()
        {
            Messenger.Default.Send(new NotificationMessage<bool>(this, true, Messages.PushDialogResult));
        }

        private void Cancelled()
        {
            Messenger.Default.Send(new NotificationMessage<bool>(this, false, Messages.PushDialogResult));
        }

        private void ManageClouds()
        {
            Messenger.Default.Send(new NotificationMessageAction<bool>(Messages.ManageClouds, (confirmed) => { }));
        }

        private void AddAppService()
        {
            Messenger.Default.Send(new NotificationMessageAction<bool>(Messages.AddAppService, (confirmed) => { }));
        }
    }
}
