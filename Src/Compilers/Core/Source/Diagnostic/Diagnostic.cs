﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a diagnostic, such as a compiler error or a warning, along with the location where it occurred.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public abstract partial class Diagnostic : IEquatable<Diagnostic>
    {
        /// <summary>
        /// Creates a <see cref="Diagnostic"/> instance.
        /// </summary>
        /// <param name="descriptor">A <see cref="DiagnosticDescriptor"/> describing the diagnostic</param>
        /// <param name="location">An optional primary location of the diagnostic. If null, <see cref="Location"/> will return <see cref="Location.None"/>.</param>
        /// <param name="messageArgs">Arguments to the message of the diagnostic</param>
        /// <returns>The <see cref="Diagnostic"/> instance.</returns>
        /// <remarks>
        /// If severity is <see cref="DiagnosticSeverity.Warning"/>, <see cref="WarningLevel"/> will be 1; otherwise 0.
        /// <see cref="IsWarningAsError"/> will be false.
        /// </remarks>
        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            Location location,
            params object[] messageArgs)
        {
            return Create(descriptor, location, null, messageArgs);
        }

        /// <summary>
        /// Creates a <see cref="Diagnostic"/> instance.
        /// </summary>
        /// <param name="descriptor">A <see cref="DiagnosticDescriptor"/> describing the diagnostic</param>
        /// <param name="location">An optional primary location of the diagnostic. If null, <see cref="Location"/> will return <see cref="Location.None"/>.</param>
        /// <param name="additionalLocations">
        /// An optional set of additional locations related to the diagnostic.
        /// Typically, these are locations of other items referenced in the message.
        /// If null, <see cref="AdditionalLocations"/> will return an empty list.
        /// </param>
        /// <param name="messageArgs">Arguments to the message of the diagnostic</param>
        /// <returns>The <see cref="Diagnostic"/> instance.</returns>
        /// <remarks>
        /// If severity is <see cref="DiagnosticSeverity.Warning"/>, <see cref="WarningLevel"/> will be 1; otherwise 0.
        /// <see cref="IsWarningAsError"/> will be false.
        /// </remarks>
        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            Location location,
            IEnumerable<Location> additionalLocations,
            params object[] messageArgs)
            {
            if (descriptor == null)
            {
                throw new ArgumentNullException("descriptor");
            }

            var message = descriptor.MessageFormat;
            if (messageArgs != null)
            {
                message = string.Format(message, messageArgs);
            }

            return Create(descriptor.Id, descriptor.Category, message, descriptor.DefaultSeverity, warningLevel: descriptor.DefaultSeverity == DiagnosticSeverity.Warning ? 1 : 0, isWarningAsError: false, location: location ?? Location.None, additionalLocations: additionalLocations);
        }

        /// <summary>
        /// Creates a <see cref="Diagnostic"/> instance.
        /// </summary>
        /// <param name="id">An identifier for the diagnostic. For diagnostics generated by the compiler, this will be a numeric code with a prefix such as "CS1001".</param>
        /// <param name="category">The category of the diagnostic. For diagnostics generated by the compiler, the category will be "Compiler".</param>
        /// <param name="message">The diagnostic message text.</param>
        /// <param name="severity">The diagnostic severity.</param>
        /// <param name="warningLevel">The warning level, between 1 and 4 if severity is <see cref="DiagnosticSeverity.Warning"/>; otherwise 0.</param>
        /// <param name="isWarningAsError">True if the diagnostic is a warning and should be treated as an error; otherwise false.</param>
        /// <param name="location">An optional primary location of the diagnostic. If null, <see cref="Location"/> will return <see cref="Location.None"/>.</param>
        /// <param name="additionalLocations">
        /// An optional set of additional locations related to the diagnostic.
        /// Typically, these are locations of other items referenced in the message.
        /// If null, <see cref="AdditionalLocations"/> will return an empty list.
        /// </param>
        /// <returns>The <see cref="Diagnostic"/> instance.</returns>
        public static Diagnostic Create(
            string id,
            string category,
            string message,
            DiagnosticSeverity severity,
            int warningLevel,
            bool isWarningAsError,
            Location location = null,
            IEnumerable<Location> additionalLocations = null)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return new SimpleDiagnostic(id, category, message, severity, warningLevel, isWarningAsError, location ?? Location.None, additionalLocations: additionalLocations);
        }

        internal static Diagnostic Create(CommonMessageProvider messageProvider, int errorCode)
        {
            return new DiagnosticWithInfo(new DiagnosticInfo(messageProvider, errorCode), Location.None);
        }

        internal static Diagnostic Create(CommonMessageProvider messageProvider, int errorCode, params object[] arguments)
        {
            return new DiagnosticWithInfo(new DiagnosticInfo(messageProvider, errorCode, arguments), Location.None);
        }

        /// <summary>
        /// Gets the diagnostic identifier. For diagnostics generated by the compiler, this will be a numeric code with a prefix such as "CS1001".
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// Gets the category of diagnostic. For diagnostics generated by the compiler, the category will be "Compiler".
        /// </summary>
        public abstract string Category { get; }

        /// <summary>
        /// Get the text of the message.
        /// </summary>
        public abstract string GetMessage(CultureInfo culture = null);

        /// <summary>
        /// Gets the <see cref="DiagnosticSeverity"/>.
        /// </summary>
        /// <remarks>
        /// To determine if this is a warning treated as an error, use <see cref="IsWarningAsError"/>.
        /// </remarks>
        public abstract DiagnosticSeverity Severity { get; }

        /// <summary>
        /// Gets the warning level. This is an integer between 1 and 4 if severity
        /// is <see cref="DiagnosticSeverity.Warning"/>; otherwise 0.
        /// </summary>
        public abstract int WarningLevel { get; }

        /// <summary>
        /// Returns true if this is a warning treated as an error; otherwise false.
        /// </summary>
        /// <remarks>
        /// True implies <see cref="Severity"/> = <see cref="DiagnosticSeverity.Warning"/>.
        /// </remarks>
        public virtual bool IsWarningAsError
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the primary location of the diagnostic, or <see cref="Location.None"/> if no primary location.
        /// </summary>
        public abstract Location Location { get; }

        /// <summary>
        /// Gets an array of additional locations related to the diagnostic.
        /// Typically these are the locations of other items referenced in the message.
        /// </summary>
        public abstract IReadOnlyList<Location> AdditionalLocations { get; }

        public override string ToString()
        {
            return DiagnosticFormatter.Instance.Format(this, CultureInfo.CurrentUICulture);
        }

        public abstract bool Equals(Diagnostic obj);

        private string GetDebuggerDisplay()
        {
            switch (this.Severity)
            {
                case InternalDiagnosticSeverity.Unknown:
                    // If we called ToString before the diagnostic was resolved,
                    // we would risk infinite recursion (e.g. if we were still computing
                    // member lists).
                    return "Unresolved diagnostic at " + this.Location;

                case InternalDiagnosticSeverity.Void:
                    // If we called ToString on a void diagnostic, the MessageProvider
                    // would complain about the code.
                    return "Void diagnostic at " + this.Location;

                default:
                    return ToString();
            }
        }

        /// <summary>
        /// Create a new instance of this diagnostic with the Location property changed.
        /// </summary>
        internal abstract Diagnostic WithLocation(Location location);

        /// <summary>
        /// Create a new instance of this diagnostic with the IsWarningAsError property changed.
        /// </summary>
        internal abstract Diagnostic WithWarningAsError(bool isWarningAsError);

        // compatibility
        internal virtual int Code { get { return 0; } }

        internal virtual IReadOnlyList<object> Arguments
        {
            get { return SpecializedCollections.EmptyReadOnlyList<object>(); }
        }

        /// <summary>
        /// Returns true if the diagnostic location (or any additional location) is within the given tree and optional filterSpanWithinTree.
        /// </summary>
        internal bool ContainsLocation(SyntaxTree tree, TextSpan? filterSpanWithinTree = null)
        {
            var locations = this.GetDiagnosticLocationsWithinTree(tree);

            foreach (var location in locations)
            {
                if (!filterSpanWithinTree.HasValue || filterSpanWithinTree.Value.Contains(location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<Location> GetDiagnosticLocationsWithinTree(SyntaxTree tree)
        {
            if (this.Location.SourceTree == tree)
            {
                yield return this.Location;
            }

            if (this.AdditionalLocations != null)
            {
                foreach (var additionalLocation in this.AdditionalLocations)
                {
                    if (additionalLocation.SourceTree == tree)
                    {
                        yield return additionalLocation;
                    }
                }
            }
        }
    }
}
