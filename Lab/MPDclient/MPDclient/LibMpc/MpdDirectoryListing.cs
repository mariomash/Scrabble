/*
 * Copyright 2008 Matthias Sessler
 * 
 * This file is part of LibMpc.net.
 *
 * LibMpc.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 2.1 of the License, or
 * (at your option) any later version.
 *
 * LibMpc.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with LibMpc.net.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MPDclient.LibMpc
{
    /// <summary>
    /// The MpdDirectoryListing class contains the response of a MPD server to a list command.
    /// </summary>
    public class MpdDirectoryListing
    {
        /// <summary>
        /// The list of files in the directory.
        /// </summary>
        public ReadOnlyCollection<MpdFile> FileList { get; }

        /// <summary>
        /// The list of subdirectories in the directory.
        /// </summary>
        public ReadOnlyCollection<string> DirectoryList { get; }

        /// <summary>
        /// The list of playlists in the directory.
        /// </summary>
        public ReadOnlyCollection<string> PlaylistList { get; }

        /// <summary>
        /// Creates a new MpdDirectoryListing.
        /// </summary>
        /// <param name="file">The list of files in the directory.</param>
        /// <param name="directory">The list of subdirectories in the directory.</param>
        /// <param name="playlist">The list of playlists in the directory.</param>
        public MpdDirectoryListing(List<MpdFile> file, List<string> directory, List<string> playlist)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist));

            FileList = new ReadOnlyCollection<MpdFile>(file);
            DirectoryList = new ReadOnlyCollection<string>(directory);
            PlaylistList = new ReadOnlyCollection<string>(playlist);
        }
        /// <summary>
        /// Creates a new MpdDirectoryListing.
        /// </summary>
        /// <param name="file">The list of files in the directory.</param>
        /// <param name="directory">The list of subdirectories in the directory.</param>
        /// <param name="playlist">The list of playlists in the directory.</param>
        public MpdDirectoryListing(ReadOnlyCollection<MpdFile> file, ReadOnlyCollection<string> directory, ReadOnlyCollection<string> playlist)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist));

            FileList = file;
            DirectoryList = directory;
            PlaylistList = playlist;
        }
    }
}
