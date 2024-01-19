// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.InheritanceMargin
{
    internal static class InheritanceMarginLogger
    {
        // 1 sec per bucket, and if it takes more than 1 min, then this log is considered as time-out in the last bucket.
        private static readonly HistogramLogAggregator<ActionInfo> s_histogramLogAggregator = new(1000, 60000);

        private enum ActionInfo
        {
            GetInheritanceMarginMembers,
        }

        public static void LogGenerateBackgroundInheritanceInfo(TimeSpan elapsedTime)
            => s_histogramLogAggregator.LogTime(
                ActionInfo.GetInheritanceMarginMembers, elapsedTime);

        public static void LogInheritanceTargetsMenuOpen()
        {
            using var logMessage = KeyValueLogMessage.Create(LogType.UserAction);

            Logger.Log(FunctionId.InheritanceMargin_TargetsMenuOpen, logMessage);
        }

        public static void LogNavigateToTarget()
        {
            using var logMessage = KeyValueLogMessage.Create(LogType.UserAction);

            Logger.Log(FunctionId.InheritanceMargin_NavigateToTarget, logMessage);
        }

        public static void ReportTelemetry()
        {
            using var logMessage = KeyValueLogMessage.Create(
                m =>
                {
                    var histogramLogAggragator = s_histogramLogAggregator.GetValue(ActionInfo.GetInheritanceMarginMembers);
                    if (histogramLogAggragator != null)
                    {
                        histogramLogAggragator.WriteTelemetryPropertiesTo(m, nameof(ActionInfo.GetInheritanceMarginMembers) + ".");
                    }
                });

            Logger.Log(FunctionId.InheritanceMargin_GetInheritanceMemberItems,
                logMessage);
        }
    }
}
