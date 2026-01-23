namespace PreReleaseDelistLib;
﻿/*
    PreReleaseDelistLib
    Copyright (C) 2026 Alastair Lundy

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
     any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

public interface IPackageVersionService
{
    Task<NuGetVersion[]> GetPrereleasePackageVersionsAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        CancellationToken cancellationToken);
    
    Task<NuGetVersion[]> GetAllPackageVersionsAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        bool excludeUnlistedVersions,
        CancellationToken cancellationToken);
    
    Task<NuGetVersion[]> GetDelistedPackageVersionsAsync(string nugetApiUrl, string nugetApiKey, string packageId, CancellationToken cancellationToken);
}