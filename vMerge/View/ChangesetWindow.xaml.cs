using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.Helpers.WPFBindings;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.StudioIntegration.Helpers;
using alexbegh.vMerge.ViewModel;
using alexbegh.vMerge.ViewModel.Changesets;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace alexbegh.vMerge.View
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    [AssociatedViewModel(typeof(ChangesetViewModel), Key="Changesets")]
    public partial class ChangesetWindow : UserControl
    {
        public ChangesetWindow()
        {
            try
            {
                InitializeComponent();
            } catch (Exception ex)
            {
                SimpleLogger.Log(ex, true, true);
                throw;
            }
            try
            {
                Repository.Instance.Settings.LoadSettings(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vMerge.settings"));
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(SimpleLogLevel.Warn, ex.GetType().Name + " in create Changeset Window: " + ex.Message);
            }
            this.GotFocus += OnControlGotFocus;
        }

        private void OnControlGotFocus(object sender, RoutedEventArgs e)
        {
            var defaultStyle = (Style)this.FindResource(typeof(ContextMenu));
        }

        private void ShowLoadMergeProfilesMenu(object sender, RoutedEventArgs e)
        {
            try
            {
                /*LoadProfilesMenu.PlacementTarget = this;
                LoadProfilesMenu.DataContext = DataContext;
                LoadProfilesMenu.IsOpen = true;*/
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(SimpleLogLevel.Warn, ex.GetType().Name + " in ShowLoadMergeProfilesMenu: " + ex.Message);
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeContextMenuImageSourceAccordingToTheme.Process(sender as Microsoft.VisualStudio.PlatformUI.VsContextMenu);
            } catch (Exception ex)
            {
                SimpleLogger.Log(ex, true, true);
            }
        }
    }
}