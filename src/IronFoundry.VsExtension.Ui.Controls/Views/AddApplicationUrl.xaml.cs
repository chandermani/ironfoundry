﻿using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using IronFoundry.VsExtension.Ui.Controls.Utilities;
using IronFoundry.VsExtension.Ui.Controls.ViewModel;

namespace IronFoundry.VsExtension.Ui.Controls.Views
{
	/// <summary>
    /// Interaction logic for AddApplicationUrl.xaml
	/// </summary>
	public partial class AddApplicationUrl : Window
	{
        public AddApplicationUrl()
		{
			this.InitializeComponent();
            this.DataContext = new AddApplicationUrlViewModel();
            this.Closed += (s, e) => Messenger.Default.Unregister(this);

            Messenger.Default.Register<NotificationMessage<bool>>(this,
                message =>
                {
                    if (message.Notification.Equals(Messages.AddApplicationUrlDialogResult))
                    {
                        this.DialogResult = message.Content;
                        this.Close();
                        Messenger.Default.Unregister(this);
                    }
                });
		}
	}
}