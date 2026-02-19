using Avalonia.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace easySave_BMT.Avalonia.ViewModels
{
    public partial class MainWindowViewModel
    {
        private enum MessageArea
        {
            Dashboard,
            NewTask,
            Config
        }

        private static string GetMessageKeyFromCode(int code)
        {
            // Map the existing console codes to localized strings.
            return code switch
            {
                100 => "UiReady",
                101 => "FileAddedSuccess",
                103 => "FileDeletedSuccess",
                104 => "BackupSuccess",
                105 => "NoChanges",
                200 => "RestoreJSON",
                201 => "AddFailed",
                202 => "SaveFailed",
                203 => "DeleteFailed",
                204 => "ListEmpty",
                205 => "ListFull",
                206 => "InvalidOption",
                207 => "TransferFailed",
                208 => "BackupTypeNotExist",
                209 => "CopyFailed",
                210 => "CreateFolderFailed",
                211 => "DirectoryNotExist",
                212 => "ChooseDifferentPath",
                213 => "DestinationNotExist",
                214 => "NameTaken",
                215 => "EnterValidName",
                216 => "BackupCompletedWithErrors",
                217 => "DestinationInsideSource",
                218 => "ConfigUpdated",
                _ => "UnknownError"
            };
        }

        private void SetMessageFromCode(int code, MessageArea area)
        {
            string key = GetMessageKeyFromCode(code);
            SetTimedAreaMessage(area, Loc[key]);
        }

        private void CancelMessageAutoClear(MessageArea area)
        {
            if (_messageClearTokens.TryGetValue(area, out var cts))
            {
                try { cts.Cancel(); } catch { }
                cts.Dispose();
                _messageClearTokens.Remove(area);
            }
        }

        private void ScheduleMessageAutoClear(MessageArea area)
        {
            CancelMessageAutoClear(area);

            var cts = new CancellationTokenSource();
            _messageClearTokens[area] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(MessageAutoClearDelay, cts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    // Clear only if no newer message replaced it (token still current).
                    if (_messageClearTokens.TryGetValue(area, out var current) && ReferenceEquals(current, cts))
                    {
                        ClearAreaMessage(area);
                    }
                });
            });
        }

        private void ClearAreaMessage(MessageArea area)
        {
            CancelMessageAutoClear(area);

            switch (area)
            {
                case MessageArea.NewTask:
                    NewTaskMessage = "";
                    NewTaskStatusText = "";
                    break;

                case MessageArea.Config:
                    ConfigMessage = "";
                    break;

                default:
                    DashboardMessage = "";
                    DashboardStatusText = "";
                    break;
            }
        }

        private void SetTimedAreaMessage(MessageArea area, string message, string? statusText = null)
        {
            switch (area)
            {
                case MessageArea.NewTask:
                    NewTaskMessage = message ?? "";
                    if (statusText is not null) NewTaskStatusText = statusText;
                    break;

                case MessageArea.Config:
                    ConfigMessage = message ?? "";
                    break;

                default:
                    DashboardMessage = message ?? "";
                    if (statusText is not null) DashboardStatusText = statusText;
                    break;
            }

            // Dashboard must NOT auto-disappear.
            if (area == MessageArea.Dashboard)
            {
                CancelMessageAutoClear(area);
                return;
            }

            bool hasContent =
                !string.IsNullOrWhiteSpace(message) ||
                !string.IsNullOrWhiteSpace(statusText);

            if (hasContent) ScheduleMessageAutoClear(area);
            else CancelMessageAutoClear(area);
        }

        private void SetTimedDashboardStatusText(string statusText)
        {
            DashboardStatusText = statusText ?? "";
            CancelMessageAutoClear(MessageArea.Dashboard);
        }
    }
}

