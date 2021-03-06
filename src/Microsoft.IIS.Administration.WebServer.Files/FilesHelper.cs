﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Administration.Files;
    using Core;
    using Core.Utils;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using Web.Administration;

    public static class FilesHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id", "type", "path", "physical_path");

        private static IFileProvider _service = FileProvider.Default;
        private static IAccessControl _acessControl = AccessControl.Default;

        public static object ToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            var physicalPath = GetPhysicalPath(site, path);

            if (physicalPath != null) {

                var fileType = GetFileType(site, path, physicalPath);

                switch (fileType) {

                    case FileType.File:
                        return FileToJsonModel(site, path, fields, full);

                    case FileType.Directory:
                        return DirectoryToJsonModel(site, path, fields, full);

                    case FileType.VDir:
                        var app = ResolveApplication(site, path);
                        var vdir = ResolveVdir(site, path);
                        return VdirToJsonModel(new Vdir(site, app, vdir), fields, full);
                }
            }

            return null;
        }

        public static object ToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(site, path, RefFields, false);
            }
            else {
                return ToJsonModel(site, path, fields, false);
            }
        }

        internal static object DirectoryToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            path = path.Replace('\\', '/');
            var physicalPath = GetPhysicalPath(site, path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = _service.GetName(path);
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = FileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.Directory).ToLower();
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = path;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(site, path);
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {
                obj.file_info = Administration.Files.FilesHelper.ToJsonModelRef(physicalPath, fields.Filter("file_info"));
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        internal static object DirectoryToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return DirectoryToJsonModel(site, path, RefFields, false);
            }
            else {
                return DirectoryToJsonModel(site, path, fields, false);
            }
        }

        internal static object FileToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            path = path.Replace('\\', '/');
            var physicalPath = GetPhysicalPath(site, path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = _service.GetName(physicalPath);
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = FileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.File).ToLower();
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = path.Replace('\\', '/');
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(site, path);
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {
                obj.file_info = Administration.Files.FilesHelper.ToJsonModelRef(physicalPath, fields.Filter("file_info"));
            }


            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
        }

        internal static object VdirToJsonModel(Vdir vdir, Fields fields = null, bool full = true)
        {
            if (vdir == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var physicalPath = GetPhysicalPath(vdir.Site, vdir.Path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(vdir.Site.Id, vdir.Path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = vdir.Path.TrimStart('/');
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = FileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.VDir).ToLower();
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = vdir.Path.Replace('\\', '/');
            }

            //
            // parent
            if (fields.Exists("parent")) {
                if (vdir.VirtualDirectory.Path != "/") {
                    var rootVdir = vdir.Application.VirtualDirectories["/"];
                    obj.parent = rootVdir == null ? null : VdirToJsonModelRef(new Vdir(vdir.Site, vdir.Application, rootVdir));
                }
                else if (vdir.Application.Path != "/") {
                    var rootApp = vdir.Site.Applications["/"];
                    var rootVdir = rootApp == null ? null : rootApp.VirtualDirectories["/"];
                    obj.parent = rootApp == null || rootVdir == null ? null : VdirToJsonModel(new Vdir(vdir.Site, rootApp, rootVdir));
                }
                else {
                    obj.parent = null;
                }
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(vdir.Site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {
                obj.file_info = Administration.Files.FilesHelper.ToJsonModelRef(physicalPath, fields.Filter("file_info"));
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        internal static object VdirToJsonModelRef(Vdir vdir, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return VdirToJsonModel(vdir, RefFields, false);
            }
            else {
                return VdirToJsonModel(vdir, fields, false);
            }
        }

        internal static object FileToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return FileToJsonModel(site, path, RefFields, false);
            }
            else {
                return FileToJsonModel(site, path, fields, false);
            }
        }

        internal static string UpdateFile(dynamic model, string physicalPath)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);

            if (name != null) {

                if (!PathUtil.IsValidFileName(name)) {
                    throw new ApiArgumentException("name");
                }

                var newPath = Path.Combine(_service.GetParentPath(physicalPath), name);

                if (_service.FileExists(newPath)) {
                    throw new AlreadyExistsException("name");
                }

                _service.MoveFile(physicalPath, newPath);

                physicalPath = newPath;
            }

            return physicalPath;
        }

        internal static string UpdateDirectory(dynamic model, string directoryPath)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);

            if (name != null) {

                if (!PathUtil.IsValidFileName(name)) {
                    throw new ApiArgumentException("name");
                }

                if (_service.GetParentPath(directoryPath) != null) {

                    var newPath = Path.Combine(_service.GetParentPath(directoryPath), name);

                    if (_service.DirectoryExists(newPath)) {
                        throw new AlreadyExistsException("name");
                    }

                    _service.MoveDirectory(directoryPath, newPath);

                    directoryPath = newPath;
                }
            }

            return directoryPath;
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.FILES_PATH}/{id}";
        }

        internal static FileType GetFileType(Site site, string path, string physicalPath)
        {
            // A virtual directory is a directory who's physical path is not the combination of the sites physical path and the relative path from the sites root
            // and has same virtual path as app + vdir path
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);
            
            var differentPhysicalPath = !Path.Combine(GetPhysicalPath(site), path.Replace('/', '\\').TrimStart('\\')).Equals(physicalPath, StringComparison.OrdinalIgnoreCase);

            if (path == "/" || differentPhysicalPath && IsExactVdirPath(site, app, vdir, path)) {
                return FileType.VDir;
            }
            else if (_service.DirectoryExists(physicalPath)) {
                return FileType.Directory;
            }

            return FileType.File;
        }

        public static bool IsExactVdirPath(Site site, Application app, VirtualDirectory vdir, string path)
        {
            path = path.TrimEnd('/');
            var virtualPath = app.Path.TrimEnd('/') + vdir.Path.TrimEnd('/');
            return path.Equals(virtualPath, StringComparison.OrdinalIgnoreCase);
        }

        public static Application ResolveApplication(Site site, string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }
            
            Application parentApp = null;
            var maxMatch = 0;
            foreach (var app in site.Applications) {
                var matchingPrefix = PathUtil.PrefixSegments(app.Path, path);
                if (matchingPrefix > maxMatch) {
                    parentApp = app;
                    maxMatch = matchingPrefix;
                }
            }
            return parentApp;
        }

        public static VirtualDirectory ResolveVdir(Site site, string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }

            var parentApp = ResolveApplication(site, path);
            VirtualDirectory parentVdir = null;
            if (parentApp != null) {
                var maxMatch = 0;
                var testPath = path.TrimStart(parentApp.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                testPath = testPath == string.Empty ? "/" : testPath;
                foreach (var vdir in parentApp.VirtualDirectories) {
                    var matchingPrefix = PathUtil.PrefixSegments(vdir.Path, testPath);
                    if (matchingPrefix > maxMatch) {
                        parentVdir = vdir;
                        maxMatch = matchingPrefix;
                    }
                }
            }
            return parentVdir;
        }

        internal static Vdir ResolveFullVdir(Site site, string path)
        {
            VirtualDirectory vdir = null;

            var app = ResolveApplication(site, path);
            if (app != null) {
                vdir = ResolveVdir(site, path);
            }

            return vdir == null ? null : new Vdir(site, app, vdir);
        }

        public static string GetPhysicalPath(Site site, string path)
        {
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);
            string physicalPath = null;

            if (vdir != null) {
                var suffix = path.TrimStart(app.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase).TrimStart(vdir.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                physicalPath = Path.Combine(vdir.PhysicalPath, suffix.Trim(PathUtil.SEPARATORS).Replace('/', Path.DirectorySeparatorChar));
                physicalPath = PathUtil.GetFullPath(physicalPath);
            }

            return physicalPath;
        }

        internal static IEnumerable<Vdir> GetVdirs(Site site, string path)
        {
            var vdirs = new List<Vdir>();

            if (path == "/") {
                foreach (var app in site.Applications) {
                    if (app.Path != "/") {
                        foreach (var vdir in app.VirtualDirectories) {
                            if (vdir.Path == "/") {
                                vdirs.Add(new Vdir(site, app, vdir));
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var app in site.Applications) {
                if (app.Path.Equals(path, StringComparison.OrdinalIgnoreCase)) {
                    foreach (var vdir in app.VirtualDirectories) {
                        if (vdir.Path != "/") {
                            vdirs.Add(new Vdir(site, app, vdir));
                        }
                    }
                    break;
                }
            }
            return vdirs;
        }


        
        private static string GetPhysicalPath(Site site)
        {
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }

            string root = null;
            var rootApp = site.Applications["/"];
            if (rootApp != null && rootApp.VirtualDirectories["/"] != null) {
                root = PathUtil.GetFullPath(rootApp.VirtualDirectories["/"].PhysicalPath);
            }
            return root;
        }

        private static object GetParentJsonModelRef(Site site, string path)
        {
            object parent = null;
            if (path != "/") {
                var parentPath = PathUtil.RemoveLastSegment(path);
                var parentApp = ResolveApplication(site, parentPath);
                var parentVdir = ResolveVdir(site, parentPath);

                if (IsExactVdirPath(site, parentApp, parentVdir, parentPath)) {
                    parent = VdirToJsonModelRef(new Vdir(site, parentApp, parentVdir));
                }
                else {
                    var parentPhysicalPath = GetPhysicalPath(site, parentPath);
                    parent = _service.DirectoryExists(parentPhysicalPath) ? DirectoryToJsonModelRef(site, parentPath) : null;
                }
            }
            return parent;
        }
    }
}
