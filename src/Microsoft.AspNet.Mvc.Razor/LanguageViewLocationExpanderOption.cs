﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Specifies the localized view format for <see cref="LanguageViewLocationExpander"/>.
    /// </summary>
    public enum LanguageViewLocationExpanderOption
    {
        /// <summary>
        /// Locale is a subfolder under which the view exisits.
        /// </summary>
        /// <example>
        /// Home->Views->en-US->Index.chtml
        /// </example>
        SubFolder,

        /// <summary>
        /// Locale is part of the view name as a suffix.
        /// </summary>
        /// <example>
        /// Home->Views>Index.en-US.chtml
        /// </example>
        Suffix
    }
}
