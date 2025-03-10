﻿using IAGrim.Backup.FileWriter;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.Arz;
using IAGrim.Utilities.HelperClasses;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EvilsoftCommons.Exceptions;
using IAGrim.Backup;
using IAGrim.Database;
using IAGrim.Parsers.TransferStash;
using IAGrim.Services;
using IAGrim.Utilities;
using Ionic.Zip;
using log4net;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace IAGrim.UI.Popups.ImportExport.Panels {
    partial class ImportMode : Form {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ImportMode));
        private readonly GDTransferFile[] _modSelection;
        private readonly IPlayerItemDao _playerItemDao;
        private string _filename;
        private readonly TransferStashService2 _sm;
        private volatile bool isLocked = false;

        public ImportMode(GDTransferFile[] modSelection, IPlayerItemDao playerItemDao, TransferStashService2 sm) {
            InitializeComponent();
            this._modSelection = modSelection;
            this._playerItemDao = playerItemDao;
            this._sm = sm;
        }

        private void ImportMode_Load(object sender, EventArgs e) {
            this.Dock = DockStyle.Fill;
            this.buttonImport.Enabled = false;
            cbItemSelection.Visible = false;
            cbItemSelection.Enabled = false;

            cbItemSelection.Items.AddRange(_modSelection);
            LocalizationLoader.ApplyLanguage(Controls, RuntimeSettings.Language);

            (this.Parent.Parent as Form).FormClosing += Form1_FormClosing;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            e.Cancel = isLocked;
        }

        private void radioIAStash_CheckedChanged(object sender, EventArgs e) {
            cbItemSelection.Visible = !(sender as Control).Enabled;
        }

        private void radioGDStash_CheckedChanged(object sender, EventArgs e) {
            cbItemSelection.Visible = (sender as Control).Enabled;
        }

        private void radioGameStash_CheckedChanged(object sender, EventArgs e) {
            cbItemSelection.Visible = !(sender as Control).Enabled;
        }

        private void buttonBrowse_Click(object sender, EventArgs e) {
            if (buttonBrowse.Enabled) {
                using (OpenFileDialog diag = new OpenFileDialog {
                    CheckFileExists = true,
                    CheckPathExists = true,
                    InitialDirectory = Path.Combine(GlobalPaths.SavePath, "Save"),
                    Multiselect = false,
                    Title = RuntimeSettings.Language.GetTag("iatag_ui_importexport_selectfile")
                }) {

                    if (radioGDStash.Checked) {
                        diag.DefaultExt = "gds";
                        diag.Filter = "GD Stash exports (*.gds)|*.gds";
                    }
                    else if (radioIAStash.Checked) {
                        diag.DefaultExt = "ias";
                        diag.Filter = "IA Stash exports (*.ias)|*.ias;*.zip";
                    }
                    else {
                        diag.DefaultExt = "gst";
                        diag.Filter = "Grim Dawn Stash files (*.gst)|*.gst|HD Hardcore Stash files (*.gsh)|*.gsh";
                        diag.FileName = "transfer.gst";
                    }

                    if (diag.ShowDialog() == DialogResult.OK) {
                        if (IsValid(diag.FileName)) {
                            radioGDStash.Enabled = false;
                            radioIAStash.Enabled = false;
                            buttonImport.Enabled = true;
                            cbItemSelection.Enabled = true;
                            this._filename = diag.FileName;
                        }
                        else {
                            MessageBox.Show(
                                RuntimeSettings.Language.GetTag("iatag_ui_importexport_nothinginzip_body"),
                                RuntimeSettings.Language.GetTag("iatag_ui_importexport_nothinginzip_title"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Asterisk
                            );
                        }
                    }
                }
            }
        }

        private static bool IsValid(string filename) {
            // Attempt to read ias/gds from zip file
            if (filename.ToLowerInvariant().EndsWith(".zip")) {
                using (ZipFile zip = ZipFile.Read(filename)) {
                    return zip.EntryFileNames.Any(fn => fn.EndsWith(".ias") || fn.EndsWith(".gds"));
                }
            }

            // Regular ias/gds file
            return true;
        }

        private static byte[] Read(string filename) {
            // Attempt to read ias/gds from zip file
            if (filename.ToLowerInvariant().EndsWith(".zip")) {
                using (ZipFile zip = ZipFile.Read(filename)) {
                    var candidates = zip.EntryFileNames.Where(fn => fn.EndsWith(".ias") || fn.EndsWith(".gds")).ToList();
                    foreach (var candidate in candidates) {
                        using (var ms = new MemoryStream()) {
                            zip[candidate].Extract(ms);
                            return ms.GetBuffer();
                        }
                    }
                }
            }

            // Regular ias/gds file
            return File.ReadAllBytes(filename);
        }

        private void buttonImport_Click(object sender, EventArgs e) {
            if (buttonImport.Enabled) {
                FileExporter io;

                if (radioIAStash.Checked) {
                    io = new IAFileExporter(_filename);
                }
                else if (radioGDStash.Checked) {
                    GDTransferFile settings = cbItemSelection.SelectedItem as GDTransferFile;
                    io = new GDFileExporter(_filename, settings?.Mod ?? string.Empty);
                }
                else {
                    _playerItemDao.Save(_sm.EmptyStash(_filename));

                    MessageBox.Show(
                        RuntimeSettings.Language.GetTag("iatag_ui_importexport_import_success"),
                        RuntimeSettings.Language.GetTag("iatag_ui_importexport_import_success"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                var items = io.Read(Read(_filename));
                Logger.Debug($"Storing {items.Count} items to db");
                progressBar1.Maximum = items.Count;
                progressBar1.Value = 0;
                buttonImport.Enabled = false;
                Thread t = new Thread(() => {
                    ExceptionReporter.EnableLogUnhandledOnThread();
                    isLocked = true;

                    var batches = BatchUtil.ToBatches<PlayerItem>(items);
                    foreach (var batch in batches) {
                        _playerItemDao.Import(batch);
                        Invoke((MethodInvoker)delegate { progressBar1.Value += batch.Count; });
                    }

                    isLocked = false;
                    MessageBox.Show(
                        RuntimeSettings.Language.GetTag("iatag_ui_importexport_import_success_body"),
                        RuntimeSettings.Language.GetTag("iatag_ui_importexport_import_success"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                });

                t.Start();
            }
        }

        private void helpRestoreBackup_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            new HelpService().ShowHelp(HelpService.HelpType.RestoreBackup); // TODO: Move into UI?
        }

        private void progressBar1_Click(object sender, EventArgs e) {

        }
    }
}