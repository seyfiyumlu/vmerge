using alexbegh.Utility.Managers.View;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.ViewModel.Merge;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using alexbegh.Utility.Helpers.Logging;

namespace alexbegh.vMerge.View
{
    /// <summary>
    /// Interaction logic for PrepareMergeDialogWindow.xaml
    /// </summary>
    [AssociatedViewModel(typeof(PrepareMergeViewModel), Key="Embedded")]
    public partial class PrepareMergeWindow : UserControl
    {
        public PrepareMergeWindow()
        {
            try
            {
                InitializeComponent();
                if (DesignerProperties.GetIsInDesignMode(this))
                    return;

                Loaded += (s, e) =>
                {
                    var data = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings
                        .PrepareMergeDialogWindowViewSettingsKey);
                    this.DeserializeFromString(data, false);
                    Window.GetWindow(this).Closing += PrepareMergeWindow_Closing;
                    var vm = (DataContext as PrepareMergeViewModel);
                    vm.SelectNewRowAction = () => Repository.Instance.BackgroundTaskManager.Send(() =>
                    {
                        SelectNextRow();
                        return true;
                    });
                };
                Unloaded += (s, e) =>
                {
                    var data = this.SerializeToString();
                    Repository.Instance.Settings.SetSettings(Constants.Settings.PrepareMergeDialogWindowViewSettingsKey,
                        data);
                };
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true, false);
            }
        }

        void PrepareMergeWindow_Closing(object sender, EventArgs e)
        {
            var data = Window.GetWindow(this).SerializeToString();
            Repository.Instance.Settings.SetSettings(Constants.Settings.PrepareMergeDialogWindowViewSettingsKey, data);
        }

        protected override void OnInitialized(EventArgs e)
        {
            try
            {
                base.OnInitialized(e);
                FocusManager.SetFocusedElement(this, ChangesetsTableLoader);
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true, false);
            }
        }

        private void ChangesetsTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = (DataContext as PrepareMergeViewModel);
            var changeset = (e.OriginalSource as FrameworkElement);
            vm.PerformMergeCommand.Execute(changeset.DataContext);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var vm = (DataContext as PrepareMergeViewModel);
                var changeset = (e.OriginalSource as FrameworkElement);
                if (vm != null && changeset != null)
                {
                    vm.PerformMergeCommand.Execute(changeset.DataContext);
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        private void ChangesetsGridLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DesignerProperties.GetIsInDesignMode(this))
                    return;

                ChangesetsGrid = sender as DataGrid;
                var vm = DataContext as PrepareMergeViewModel;
                BindToChangesetChanges();

                vm.PropertyChanged +=
                    (o, a) =>
                    {
                        if (a.PropertyName == "ChangesetList")
                        {
                            BindToChangesetChanges();
                        }
                    };
                    
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true, false);
            }
        }

        private void BindToChangesetChanges()
        {
            try { 
                var vm = DataContext as PrepareMergeViewModel;
                if (vm.ChangesetList != null)
                {
                    foreach (var item in vm.ChangesetList)
                    {
                        item.PropertyChanged +=
                            (o, a) =>
                            {
                                if (a.PropertyName == "TargetCheckinId")
                                {
                                    Repository.Instance.BackgroundTaskManager.Post(
                                        () =>
                                        {
                                            ChangesetsGrid.ScrollIntoView(o as PrepareMergeViewModel.ChangesetListElement, ChangesetsGrid.Columns.Where(col => col.SortMemberPath == "TargetCheckinId").FirstOrDefault());
                                            return true;
                                        }
                                    );
                                }
                            };
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true, false);
            }
        }

        private void SelectNextRow()
        {
            if (ChangesetsGrid.SelectedIndex >= 0
                && ChangesetsGrid.SelectedIndex < (ChangesetsGrid.Items.Count - 1))
            {
                ChangesetsGrid.Focus();
                ++ChangesetsGrid.SelectedIndex;
            }
        }

        private DataGrid ChangesetsGrid
        {
            get;
            set;
        }
    }
}
