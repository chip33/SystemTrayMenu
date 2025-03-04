﻿// <copyright file="FileLnk.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SystemTrayMenu.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Threading;
    using Shell32;

    internal class FileLnk
    {
        public static string GetResolvedFileName(string shortcutFilename, out bool isFolder)
        {
            bool isFolderByShell = false;
            string resolvedFilename = string.Empty;
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                resolvedFilename = GetShortcutFileNamePath(shortcutFilename, out isFolderByShell);
            }
            else
            {
                Thread staThread = new(new ParameterizedThreadStart(StaThreadMethod));
                void StaThreadMethod(object obj)
                {
                    resolvedFilename = GetShortcutFileNamePath(shortcutFilename, out isFolderByShell);
                }

                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start(shortcutFilename);
                staThread.Join();
            }

            isFolder = isFolderByShell;
            return resolvedFilename;
        }

        public static bool IsNetworkRoot(string path)
        {
            return path.StartsWith(@"\\", StringComparison.InvariantCulture) &&
                !path[2..].Contains('\\', StringComparison.InvariantCulture);
        }

        private static string GetShortcutFileNamePath(object shortcutFilename, out bool isFolder)
        {
            string resolvedFilename = string.Empty;
            isFolder = false;
            string pathOnly = Path.GetDirectoryName((string)shortcutFilename);
            string filenameOnly = Path.GetFileName((string)shortcutFilename);

            Shell shell = new();
            Folder folder = shell.NameSpace(pathOnly);
            FolderItem folderItem = folder.ParseName(filenameOnly);
            if (folderItem != null)
            {
                try
                {
                    ShellLinkObject link = (ShellLinkObject)folderItem.GetLink;
                    isFolder = link.Target.IsFolder;
                    if (string.IsNullOrEmpty(link.Path))
                    {
                        // https://github.com/Hofknecht/SystemTrayMenu/issues/242
                        // do not set CLSID key (GUID) shortcuts as resolvedFilename
                        if (!link.Target.Path.Contains("::{"))
                        {
                            resolvedFilename = link.Target.Path;
                        }
                    }
                    else
                    {
                        resolvedFilename = link.Path;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"shortcutFilename:'{shortcutFilename}'", ex);
                }
            }

            return resolvedFilename;
        }
    }
}