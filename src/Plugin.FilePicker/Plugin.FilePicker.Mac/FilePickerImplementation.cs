﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using MobileCoreServices;
using Plugin.FilePicker.Abstractions;

namespace Plugin.FilePicker
{
    public class FilePickerImplementation : NSObject, IFilePicker
    {
        public Task<FileData> PickFile()
        {
            // for consistency with other platforms, only allow selecting of a single file.
            // would be nice if we passed a "file options" to override picking multiple files & directories
            var openPanel = new NSOpenPanel(); ;
            openPanel.CanChooseFiles = true;
            openPanel.AllowsMultipleSelection = false;
            openPanel.CanChooseDirectories = false;

            FileData data = null;

            var result = openPanel.RunModal();
            if (result == 1)
            {
                // Nab the first file
                var url = openPanel.Urls[0];
                var fileName = openPanel.Filenames[0];

                if (url != null)
                {
                    var path = url.Path;
                    data = new FileData(path, fileName, false, () => File.OpenRead(path));
                }
            }

            return Task.FromResult(data);
        }

        public Task<FileData> PickFile(int maximumFileSize)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveFile(FileData fileToSave)
        {
            try
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                var savePanel = new NSSavePanel();
                savePanel.Title = $"Save {fileToSave.FileName}";
                savePanel.CanCreateDirectories = true;

                var result = savePanel.RunModal(documents, fileToSave.FileName);

                if (result == 1)
                {
                    var path = savePanel.Url.Path;

                    File.WriteAllBytes(path, fileToSave.DataArray);

                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Task.FromResult(false);
            }
        }

        public void OpenFile(string fileToOpen)
        {
            try
            {
                if (!NSWorkspace.SharedWorkspace.OpenFile(fileToOpen))
                {
                    Debug.WriteLine($"Unable to open file at path: {fileToOpen}.");
                }
            }
            catch (FileNotFoundException ex)
            {
            }
            catch (Exception ex)
            {
            }
        }

        public async void OpenFile(FileData fileToOpen)
        {
            try
            {
                if (!NSWorkspace.SharedWorkspace.OpenFile(fileToOpen.FileName))
                {
                    Debug.WriteLine($"Unable to open file at path: {fileToOpen}.");
                }
            }
            catch (FileNotFoundException ex)
            {
                // this could be some strange UI behavior.
                // user would get prompted to save the file in order to open the file
                await SaveFile(fileToOpen);
                OpenFile(fileToOpen);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
