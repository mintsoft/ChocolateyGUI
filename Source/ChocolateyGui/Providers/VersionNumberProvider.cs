﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="VersionNumberProvider.cs">
//   Copyright 2014 - Present Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Reflection;

namespace ChocolateyGui.Providers
{
    public class VersionNumberProvider : IVersionNumberProvider
    {
        private string _version;

        public virtual string Version
        {
            get
            {
                if (_version != null)
                {
                    return _version;
                }

                var assembly = GetType().Assembly;
                var informational =
                    ((AssemblyInformationalVersionAttribute[])
                        assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute)))
                        .First();

                _version = "Version: " + informational.InformationalVersion;
                return _version;
            }
        }
    }
}