// <copyright file="GlobalSuppressions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Utilizing the MD5 to create hash of certain attributes to be used as unique identifier.")]