#region License
/* 
 * Copyright (C) 1999-2022 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using Reko.Core;
using Reko.Core.Configuration;
using Reko.Core.Loading;
using Reko.Gui;
using Reko.Gui.Controls;
using Reko.Gui.Forms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Reko.UserInterfaces.WindowsForms.Forms
{
    public partial class OpenAsDialog : Form, IOpenAsDialog
    {
        public OpenAsDialog()
        {
            InitializeComponent();

            AddressTextBox = new TextBoxWrapper(txtAddress);
            RawFileTypes = new ComboBoxWrapper(ddlRawFileTypes);
            Architectures = new ComboBoxWrapper(ddlArchitectures);
            ArchitectureModels = new ComboBoxWrapper(ddlModels);
            Platforms = new ComboBoxWrapper(ddlEnvironments);
            FileName = new TextBoxWrapper(textBox1);
            PropertyGrid = new PropertyGridWrapper(propertyGrid);
            BrowseButton = new ButtonWrapper(btnBrowse);
            OkButton = new ButtonWrapper(btnOk);

            new OpenAsInteractor().Attach(this);
        }

        public IServiceProvider Services { get; set; }

        public ITextBox FileName { get; private set; }

        public ITextBox AddressTextBox { get; private set; }
        public IComboBox RawFileTypes { get; private set; }
        public IComboBox Architectures { get; private set; }
        public IComboBox ArchitectureModels { get; private set; }
        public IComboBox Platforms { get; private set; }
        public IPropertyGrid PropertyGrid { get; private set; }
        public IButton BrowseButton { get; private set; }
        public IButton OkButton { get; private set; }

        public Dictionary<string, object> ArchitectureOptions { get; set; }

        public ArchitectureDefinition GetSelectedArchitecture()
        {
            return (ArchitectureDefinition)((ListOption)Architectures.SelectedValue).Value;
        }

        public ModelDefinition GetSelectedArchitectureModel()
        {
            return (ModelDefinition)((ListOption)ArchitectureModels.SelectedValue).Value;
        }

        public PlatformDefinition GetSelectedEnvironment()
        {
            return (PlatformDefinition)((ListOption)Platforms.SelectedValue).Value;
        }

        public RawFileDefinition GetSelectedRawFile()
        {
            var option = RawFileTypes.SelectedValue;
            if (option is null)
                return null;
            return ((ListOption) option).Value as RawFileDefinition;
        }

        public void SetPropertyGrid(Dictionary<string, object> architectureOptions, List<PropertyOption> options)
        {
            if (architectureOptions != null && options != null)
            {
                PropertyGrid.SelectedObject = new PropertyOptionsGridAdapter(architectureOptions, options);
            }
            else
            {
                PropertyGrid.SelectedObject = null;
            }
        }

        public void SetSelectedArchitecture(string archMoniker)
        {
            int c = Architectures.Items.Count;
            for (int i = 0; i < c; ++i)
            {
                var item = (ArchitectureDefinition) ((ListOption) Architectures.Items[i]).Value;
                if (archMoniker == item.Name)
                {
                    Architectures.SelectedIndex = i;
                    return;
                }
            }
        }

        public LoadDetails GetLoadDetails()
        {
            var config = Services.RequireService<IConfigurationService>();
            var rawFileOption = (ListOption) this.RawFileTypes.SelectedValue;
            string archName = null;
            string envName = null;
            string sAddr = null;
            string loader = null;
            EntryPointDefinition entry = null;
            if (rawFileOption != null && rawFileOption.Value != null)
            {
                var raw = (RawFileDefinition) rawFileOption.Value;
                loader = raw.Loader;
                archName = raw.Architecture;
                envName = raw.Environment;
                sAddr = raw.BaseAddress;
                entry = raw.EntryPoint;
            }
            ArchitectureDefinition archOption = this.GetSelectedArchitecture();
            PlatformDefinition envOption = this.GetSelectedEnvironment();
            archName = archName ?? archOption?.Name;
            envName = envName ?? envOption?.Name;
            sAddr = sAddr ?? this.AddressTextBox.Text.Trim();

            var arch = config.GetArchitecture(archName);
            if (arch is null)
                throw new InvalidOperationException($"Unable to load {archName} architecture.");
            arch.LoadUserOptions(this.ArchitectureOptions);
            if (!arch.TryParseAddress(sAddr, out var addrBase))
                throw new ApplicationException(string.Format("'{0}' doesn't appear to be a valid address.", sAddr));
            return new LoadDetails
            {
                LoaderName = loader,
                ArchitectureName = archName,
                ArchitectureOptions = this.ArchitectureOptions,
                PlatformName = envName,
                LoadAddress = sAddr,
                EntryPoint = entry,
            };
        }
    }
}
