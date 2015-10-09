﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Languages.Editor.Controller;
using Microsoft.Languages.Editor.Shell;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Interop;
using Microsoft.VisualStudio.R.Package.Plots.Commands;
using Microsoft.VisualStudio.R.Package.Utilities;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.R.Package.Plots
{
    [Guid(WindowGuid)]
    internal class PlotWindowPane : ToolWindowPane
    {
        internal const string WindowGuid = "970AD71C-2B08-4093-8EA9-10840BC726A3";

        private SavePlotCommand _saveCommand;

        public PlotWindowPane()
        {
            Caption = Resources.PlotWindowCaption;

            // set content with presenter
            PlotContentProvider = new PlotContentProvider();
            PlotContentProvider.PlotChanged += ContentProvider_PlotChanged; // TODO: when to unregister the event? for now, it's fine as 1:1 match Provider:Pane
            Content = new XamlPresenter(PlotContentProvider);

            // initialize toolbar
            this.ToolBar = new CommandID(RGuidList.RCmdSetGuid, RPackageCommandId.plotWindowToolBarId);
            Controller c = new Controller();
            c.AddCommandSet(GetCommands());
            this.ToolBarCommandTarget = new CommandTargetToOleShim(null, c);
        }

        public IPlotContentProvider PlotContentProvider { get; }

        private IEnumerable<ICommand> GetCommands()
        {
            List<ICommand> commands = new List<ICommand>();

            commands.Add(new OpenPlotCommand(this));

            _saveCommand = new SavePlotCommand(this);
            commands.Add(_saveCommand);

            commands.Add(new ExportPlotCommand(this));
            commands.Add(new FixPlotCommand(this));
            commands.Add(new CopyPlotCommand(this));
            commands.Add(new PrintPlotCommand(this));
            commands.Add(new ZoomInPlotCommand(this));
            commands.Add(new ZoomOutPlotCommand(this));

            return commands;
        }

        private void ContentProvider_PlotChanged(object sender, PlotChangedEventArgs e)
        {
            if (e.NewPlotElement == null)
            {
                _saveCommand.Disable();
            }
            else
            {
                _saveCommand.Enable();
            }
        }

        public void OpenPlot()
        {
            string filePath = GetLoadFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    PlotContentProvider.LoadFile(filePath);
                }
                catch(Exception ex)
                {
                    EditorShell.Current.ShowErrorMessage(
                        string.Format(CultureInfo.InvariantCulture, Resources.CannotOpenPlotFile, ex.Message));
                }
            }
        }

        public void SavePlot()
        {
            string destinationFilePath = GetSaveFilePath();
            if (!string.IsNullOrEmpty(destinationFilePath))
            {
                try
                {
                    PlotContentProvider.SaveFile(destinationFilePath);
                }
                catch (Exception ex)
                {
                    throw;
                    //EditorShell.Current.ShowErrorMessage(
                    //    string.Format(CultureInfo.InvariantCulture, Resources.CannotOpenPlotFile, ex.Message));
                }
            }
        }

        private string GetLoadFilePath()
        {
            return FileUtilities.BrowseForFileOpen(
                IntPtr.Zero,
                Resources.PlotFileFilter,
                // TODO: open in current project folder if one is active
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\",
                Resources.OpenPlotDialogTitle);
        }

        private string GetSaveFilePath()
        {
            return FileUtilities.BrowseForFileSave(
                IntPtr.Zero,
                Resources.PlotFileFilter,
                null,
                Resources.SavePlotDialogTitle);
        }
    }
}