﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using alexbegh.Utility.Helpers.Logging;
using Microsoft.VisualStudio.Shell;
using alexbegh.vMerge.ViewModel.WorkItems;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.StudioIntegration.Framework;
using qbusSRL.vMerge;

namespace alexbegh.vMerge.StudioIntegration
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>s
    [Guid("ec10ec1c-57aa-4402-8e88-ed7a2b73ae4a")]
    public class vMergeWorkItemsToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public vMergeWorkItemsToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.WorkItemToolWindowTitle;
            try
            {
                // Set the image that will appear on the tab of the window frame
                // when docked with an other window
                // The resource ID correspond to the one defined in the resx file
                // while the Index is the offset in the bitmap strip. Each image in
                // the strip being 16x16.
                SimpleLogger.Checkpoint("vMergeWorkItemsToolWindow - Set Bitmaps");
                this.BitmapResourceID = 301;
                this.BitmapIndex = 1;
                SimpleLogger.Checkpoint("vMergeWorkItemsToolWindow - Set Bitmap finished");
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, false, false);
            }

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            try { 
                SimpleLogger.Checkpoint("vMergeWorkItemsToolWindow - Set ViewModels");
                var workItemViewModel = new WorkItemViewModel(vMergePackage.TfsItemCache);
                var workItemWindow = Repository.Instance.ViewManager.CreateViewFor(workItemViewModel);
                vMergePackage.ThemeWindow(workItemWindow.View);
                //MahApps.Metro.ThemeManager.ChangeTheme(workItemWindow.View.Resources, vMergePackage.DefaultAccent, vMergePackage.DefaultTheme);
                SimpleLogger.Checkpoint("vMergeWorkItemsToolWindow - Set Content");
                base.Content = workItemWindow.View;
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, false, false);
            }
            

        }
    }
}
