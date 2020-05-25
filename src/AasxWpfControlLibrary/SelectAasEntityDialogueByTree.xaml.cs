﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using AdminShellNS;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für SelectAasEntityDialogue.xaml
    /// </summary>
    public partial class SelectAasEntityDialogueByTree : Window
    {
        private string theFilter = null;
        private AdminShellPackageEnv thePackage = null;
        private AdminShell.AdministrationShellEnv theEnv = null;
        private AdminShellPackageEnv[] theAuxPackages = null;

        public List<AdminShell.Key> ResultKeys = null;
        public VisualElementGeneric ResultVisualElement = null;

        public SelectAasEntityDialogueByTree(
            AdminShell.AdministrationShellEnv env,
            string filter = null,
            AdminShellPackageEnv package = null,
            AdminShellPackageEnv[] auxPackages = null)
        {
            InitializeComponent();
            thePackage = package;
            theAuxPackages = auxPackages;
            theEnv = env;
            theFilter = filter;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // fill combo box
            ComboBoxFilter.Items.Add("All");
            foreach (var x in AdminShell.Key.KeyElements)
                ComboBoxFilter.Items.Add(x);

            // select an item
            if (theFilter != null)
                foreach (var x in ComboBoxFilter.Items)
                    if (x.ToString().Trim().ToLower() == theFilter.Trim().ToLower())
                    {
                        ComboBoxFilter.SelectedItem = x;
                        break;
                    }

            // fill contents
            FilterFor(theFilter);
        }

        private void ComboBoxFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == ComboBoxFilter)
            {
                theFilter = ComboBoxFilter.SelectedItem.ToString();
                if (theFilter == "All")
                    theFilter = null;
                // fill contents
                FilterFor(theFilter);
            }
        }

        private bool PrepareResult()
        {
            // access
            if (DisplayElements == null || DisplayElements.SelectedItem == null)
                return false;
            var si = DisplayElements.SelectedItem;
            var siMdo = si.GetMainDataObject();

            // already one result
            this.ResultVisualElement = si;

            //
            // Referable
            //
            if (siMdo is AdminShell.Referable dataRef)
            {
                // check if a valuable item was selected
                var elemname = dataRef.GetElementName();
                var fullFilter = ApplyFullFilterString(theFilter);
                if (fullFilter != null && !(fullFilter.IndexOf(elemname + " ", StringComparison.Ordinal) >= 0))
                    return false;
                // ok, prepare list of keys
                this.ResultKeys = new List<AdminShell.Key>();
                var de = si;
                while (de != null)
                {
                    if (de.GetMainDataObject() is AdminShell.Identifiable)
                    {
                        // a Identifiable will terminate the list of keys
                        var data = de.GetMainDataObject() as AdminShell.Identifiable;
                        if (data != null)
                            this.ResultKeys.Insert(0, AdminShell.Key.CreateNew(dataRef.GetElementName(), true, data.identification.idType, data.identification.id));
                        break;
                    }
                    else
                    if (de.GetMainDataObject() is AdminShell.Referable data)
                    {
                        // add a key and go up ..
                        this.ResultKeys.Insert(0, AdminShell.Key.CreateNew(dataRef.GetElementName(), true, "IdShort", data.idShort));
                    }
                    else
                    // uups!
                    { }
                    // need to go up
                    de = de.Parent;
                }
                return true;
            }

            // 
            // other special cases
            //
            if (siMdo is AdminShell.SubmodelRef smref && ApplyFullFilterString(theFilter).ToLower().IndexOf("submodelref ", StringComparison.Ordinal) >= 0)
            {
                this.ResultKeys = new List<AdminShell.Key>();
                this.ResultKeys.AddRange(smref.Keys);
                return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
                this.DialogResult = true;
        }

        private string ApplyFullFilterString(string filter)
        {
            if (filter == null)
                return null;
            var res = filter;
            if (res.Trim().ToLower() == "submodelelement")
                foreach (var s in AdminShell.Key.SubmodelElements)
                    res += " " + s + " ";
            return " " + res + " ";
        }

        private void FilterFor(string filter)
        {
            filter = ApplyFullFilterString(filter);
            DisplayElements.RebuildAasxElements(theEnv, thePackage, theAuxPackages, true, filter);
        }

        private void DisplayElements_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
                this.DialogResult = true;
        }
    }
}
